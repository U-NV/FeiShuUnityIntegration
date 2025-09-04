using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using System.Net.Http;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Net;
using System.Net.Sockets;
using System.IO;


namespace U0UGames.FeiShu.Editor
{
    public class UploadTaskManager
    {
        // 飞书API端点URL
        private const string UPLOAD_PREPARE_URL = "/drive/v1/files/upload_prepare";
        private const string UPLOAD_PART_URL = "/drive/v1/files/upload_part";
        private const string UPLOAD_FINISH_URL = "/drive/v1/files/upload_finish";
        
        private FeiShuAccessTokenManager tokenManager;

        public UploadTaskManager(FeiShuAccessTokenManager token)
        {
            tokenManager = token;
        }

        /// <summary>
        /// 发起飞书文件上传任务
        /// 从FeiShuConfig获取配置数据
        /// </summary>
        public async void CreateUploadTaskAsync(FeiShuConfig config)
        {
            try
            {
                if (config.fileUploadConfigs.Count == 0)
                {
                    Debug.LogWarning("没有配置文件上传任务，请先在FeiShuConfig中添加配置");
                    return;
                }

                // 显示进度条标题
                EditorUtility.DisplayProgressBar("飞书文件上传", "正在验证访问令牌...", 0f);
                
                await tokenManager.UpdateTokenStatus();
                if(tokenManager.IsAccessTokenExpired(config.FeiShuTokenExpiryTime)){
                    Debug.LogWarning("无法获得有效的访问令牌");
                    EditorUtility.ClearProgressBar();
                    return;
                }
                if(string.IsNullOrEmpty(config.FeiShuUserAccessToken)){
                    Debug.LogWarning("无法获得有效的访问令牌");
                    EditorUtility.ClearProgressBar();
                    return;
                }

                // 获取访问令牌
                string accessToken = config.FeiShuUserAccessToken;

                // 遍历所有配置的上传任务
                var totalTasks = config.fileUploadConfigs.Count;
                var currentTask = 0;
                var successCount = 0;
                var failCount = 0;
                var skipCount = 0;
                
                foreach (var uploadConfig in config.fileUploadConfigs)
                {
                    currentTask++;
                    var progress = (float)currentTask / totalTasks;
                    
                    if (string.IsNullOrEmpty(uploadConfig.localFilePath))
                    {
                        Debug.LogWarning($"跳过无效配置: localFilePath 为空");
                        skipCount++;
                        continue;
                    }

                    if (string.IsNullOrEmpty(uploadConfig.parent_node))
                    {
                        Debug.LogWarning($"跳过无效配置: parent_node 为空");
                        skipCount++;
                        continue;
                    }

                    if (!File.Exists(uploadConfig.localFilePath))
                    {
                        Debug.LogWarning($"文件不存在: {uploadConfig.localFilePath}");
                        skipCount++;
                        continue;
                    }

                    EditorUtility.DisplayProgressBar("飞书文件上传", 
                        $"正在上传文件... ({currentTask}/{totalTasks})", progress);
                    
                    Debug.Log($"开始上传文件: {uploadConfig.localFilePath}");
                    
                    // 执行分片上传
                    var uploadResult = await UploadFileWithMultipart(uploadConfig, accessToken);
                    
                    if (uploadResult.success)
                    {
                        Debug.Log($"文件上传成功！");
                        Debug.Log($"文件Token: {uploadResult.data.file_token}");
                        successCount++;
                    }
                    else
                    {
                        Debug.LogError($"文件上传失败: {uploadResult.msg}");
                        failCount++;
                    }
                }
                
                // 清除进度条
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("上传完成", 
                    $"上传完成！\n成功: {successCount} 个\n失败: {failCount} 个\n跳过: {skipCount} 个", "确定");

            }
            catch (Exception ex)
            {
                Debug.LogError($"创建上传任务时发生异常: {ex.Message}");
                // 发生异常时也要清除进度条
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("上传失败", $"上传过程中发生异常: {ex.Message}", "确定");
            }
        }

        /// <summary>
        /// 使用分片上传方式上传文件
        /// </summary>
        private async Task<UploadFinishResponse> UploadFileWithMultipart(FeiShuFileUploadConfig uploadConfig, string userAccessToken)
        {
            try
            {
                var filePath = uploadConfig.localFilePath;
                var fileName = Path.GetFileName(filePath);
                var fileSize = (int)new FileInfo(filePath).Length;
                
                Debug.Log($"准备上传文件: {fileName}, 大小: {fileSize} 字节");

                // 步骤一：预上传，获取上传事务ID和分片策略
                var prepareRequest = new UploadPrepareRequest
                {
                    file_name = fileName,
                    parent_node = uploadConfig.parent_node,
                    parent_type = "explorer", // 默认上传到云空间
                    size = fileSize
                };

                var prepareResponse = await UploadPrepare(prepareRequest, userAccessToken);
                if (!prepareResponse.success)
                {
                    Debug.LogError($"预上传失败: {prepareResponse.msg}");
                    return new UploadFinishResponse { code = prepareResponse.code, msg = prepareResponse.msg };
                }

                var uploadId = prepareResponse.data.upload_id;
                var blockSize = prepareResponse.data.block_size;
                var blockNum = prepareResponse.data.block_num;

                Debug.Log($"预上传成功，upload_id: {uploadId}, block_size: {blockSize}, 分片数量: {blockNum}");

                // 步骤二：上传分片
                var fileBytes = await File.ReadAllBytesAsync(filePath);
                var totalBlocks = blockNum;
                var uploadedBlocks = 0;

                // 根据文件大小和分片大小计算实际需要的分片数量
                // var actualBlocks = (int)Math.Ceiling((double)fileSize / blockSize);
                
                for (int seq = 0; seq < totalBlocks; seq++)
                {
                    // 计算当前分片的数据
                    var startIndex = seq * blockSize;
                    var endIndex = Math.Min(startIndex + blockSize, fileBytes.Length);
                    var blockData = new byte[endIndex - startIndex];
                    Array.Copy(fileBytes, startIndex, blockData, 0, blockData.Length);

                    Debug.Log($"上传分片 {seq + 1}/{totalBlocks}, 大小: {blockData.Length} 字节");

                    var partResponse = await UploadPart(uploadId, seq, blockData.Length, blockData, userAccessToken);
                    if (!partResponse.success)
                    {
                        Debug.LogError($"上传分片 {seq + 1} 失败: {partResponse.msg}");
                        return new UploadFinishResponse { code = partResponse.code, msg = partResponse.msg };
                    }

                    uploadedBlocks++;
                    var partProgress = (float)uploadedBlocks / totalBlocks;
                    EditorUtility.DisplayProgressBar("飞书文件上传", 
                        $"正在上传分片... ({uploadedBlocks}/{totalBlocks})", partProgress);
                }


                // 步骤三：完成上传
                var finishRequest = new UploadFinishRequest
                {
                    upload_id = uploadId,
                    block_num = totalBlocks
                };

                var finishResponse = await UploadFinish(finishRequest, userAccessToken);

                Debug.Log("所有分片上传完成");
                return finishResponse;
            }
            catch (Exception ex)
            {
                Debug.LogError($"分片上传异常: {ex.Message}");
                return new UploadFinishResponse { code = -1, msg = ex.Message };
            }
        }

        /// <summary>
        /// 步骤一：预上传，获取上传事务ID和分片策略
        /// </summary>
        private async Task<UploadPrepareResponse> UploadPrepare(UploadPrepareRequest request, string userAccessToken)
        {
            try
            {
                var url = $"{FeiShuFileSyncEditorWindow.FEISHU_API_BASE}{UPLOAD_PREPARE_URL}";
                
                var jsonContent = JsonUtility.ToJson(request);
                Debug.Log($"预上传请求内容: {jsonContent}");
                
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {userAccessToken}");

                    var response = await client.PostAsync(url, content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    Debug.Log($"预上传响应状态: {response.StatusCode}");
                    Debug.Log($"预上传响应内容: {responseContent}");

                    if (response.IsSuccessStatusCode)
                    {
                        return JsonUtility.FromJson<UploadPrepareResponse>(responseContent);
                    }
                    else
                    {
                        Debug.LogError($"预上传失败: {response.StatusCode}, 响应内容: {responseContent}");
                        return new UploadPrepareResponse { code = (int)response.StatusCode, msg = responseContent };
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"预上传异常: {ex.Message}");
                return new UploadPrepareResponse { code = -1, msg = ex.Message };
            }
        }

        /// <summary>
        /// 步骤二：上传分片
        /// 根据飞书官方示例代码实现
        /// </summary>
        private async Task<UploadPartResponse> UploadPart(string uploadId, int seq, int size, byte[] blockData, string userAccessToken)
        {
            try
            {
                var url = $"{FeiShuFileSyncEditorWindow.FEISHU_API_BASE}{UPLOAD_PART_URL}";
                // 计算分片数据的校验和
                var checksum = CalculateChecksum(blockData);
                
                // 创建multipart/form-data请求
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {userAccessToken}");
                    client.Timeout = TimeSpan.FromMinutes(1);

                    var formData = new MultipartFormDataContent();
                    
                    // 添加upload_id
                    formData.Add(new StringContent(uploadId), "upload_id");
                    
                    // 添加seq（分片序号，从0开始）
                    formData.Add(new StringContent(seq.ToString()), "seq");
                    
                    // 添加size（分片大小）
                    formData.Add(new StringContent(size.ToString()), "size");
                    
                    // 添加checksum（分片数据校验和）
                    formData.Add(new StringContent(checksum), "checksum");
                    
                    // 添加文件数据
                    var fileContent = new ByteArrayContent(blockData);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                    formData.Add(fileContent, "file", "part");

                    Debug.Log($"上传分片请求: upload_id={uploadId}, seq={seq}, size={size}, checksum={checksum}");

                    var response = await client.PostAsync(url, formData);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    Debug.Log($"上传分片响应状态: {response.StatusCode}");
                    Debug.Log($"上传分片响应内容: {responseContent}");

                    if (response.IsSuccessStatusCode)
                    {
                        return new UploadPartResponse { code = 0, msg = "success" };
                    }
                    else
                    {
                        Debug.LogError($"上传分片失败: {response.StatusCode}, 响应内容: {responseContent}");
                        return new UploadPartResponse { code = (int)response.StatusCode, msg = responseContent };
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"上传分片异常: {ex.Message}");
                return new UploadPartResponse { code = -1, msg = ex.Message };
            }
        }

        /// <summary>
        /// 计算分片数据的Adler-32校验和
        /// 根据飞书API要求使用Adler-32算法
        /// </summary>
        private string CalculateChecksum(byte[] data)
        {
            const uint MOD_ADLER = 65521;
            uint a = 1, b = 0;
            
            foreach (byte c in data)
            {
                a = (a + c) % MOD_ADLER;
                b = (b + a) % MOD_ADLER;
            }
            
            return ((b << 16) | a).ToString();
        }

        /// <summary>
        /// 步骤三：完成上传
        /// </summary>
        private async Task<UploadFinishResponse> UploadFinish(UploadFinishRequest request, string userAccessToken)
        {
            try
            {
                var url = $"{FeiShuFileSyncEditorWindow.FEISHU_API_BASE}{UPLOAD_FINISH_URL}";
                
                var jsonContent = JsonUtility.ToJson(request);
                Debug.Log($"完成上传请求内容: {jsonContent}");
                
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {userAccessToken}");

                    var response = await client.PostAsync(url, content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    Debug.Log($"完成上传响应状态: {response.StatusCode}");
                    Debug.Log($"完成上传响应内容: {responseContent}");

                    if (response.IsSuccessStatusCode)
                    {
                        return JsonUtility.FromJson<UploadFinishResponse>(responseContent);
                    }
                    else
                    {
                        Debug.LogError($"完成上传失败: {response.StatusCode}, 响应内容: {responseContent}");
                        return new UploadFinishResponse { code = (int)response.StatusCode, msg = responseContent };
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"完成上传异常: {ex.Message}");
                return new UploadFinishResponse { code = -1, msg = ex.Message };
            }
        }
    }
}