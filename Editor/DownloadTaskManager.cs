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
    public class DownloadTaskManager
    {
        private FeiShuAccessTokenManager tokenManager;

        public DownloadTaskManager(FeiShuAccessTokenManager token)
        {
            tokenManager = token;
        }
           /// <summary>
        /// 查询表格数据获取sub_id
        /// 模仿Python代码的C#实现
        /// </summary>
        private async Task<List<SheetInfo>> QuerySpreadsheetSheet(string spreadsheetToken, string userAccessToken)
        {
            try
            {
                var url = $"{FeiShuFileSyncEditorWindow.FEISHU_API_BASE}/sheets/v3/spreadsheets/{spreadsheetToken}/sheets/query";
                
                // 创建新的HttpClient实例，避免共享状态问题
                using (var client = new HttpClient())
                {
                    // 设置请求头
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {userAccessToken}");

                    var response = await client.GetAsync(url);
                    var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var sheetResponse = JsonUtility.FromJson<QuerySpreadsheetSheetResponse>(responseContent);
                    
                    if (sheetResponse.success && sheetResponse.data?.sheets != null && sheetResponse.data.sheets.Count > 0)
                    {
                        return sheetResponse.data.sheets;
                        // 返回第一个表格的sheet_id
                        // var firstSheet = sheetResponse.data.sheets[0];
                        // Debug.Log($"查询到表格: {firstSheet.title}, sheet_id: {firstSheet.sheet_id}");
                        // return firstSheet.sheet_id;
                    }
                    else
                    {
                        Debug.LogError($"查询表格失败，响应: {responseContent}");
                        return null;
                    }
                }
                else
                {
                    Debug.LogError($"HTTP请求失败: {response.StatusCode}, 响应内容: {responseContent}");
                    return null;
                }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"查询表格数据异常: {ex.Message}");
                return null;
            }
        }



        public async Task SheetExportTaskAsync(FeiShuFileSyncConfig syncConfig, string accessToken, SheetInfo sheetInfo)
        {
            // 构造请求对象，从配置中获取数据
            var exportTask = new ExportTask
            {
                file_extension = syncConfig.file_extension.ToString(),
                token = syncConfig.fileToken,
                type = syncConfig.type.ToString(),
                sub_id = sheetInfo.sheet_id
            };


            
            Debug.Log($"开始导出任务: {sheetInfo.sheet_id ?? "未命名"}, 类型: {syncConfig.type}, 格式: {syncConfig.file_extension}");
            
            // 步骤一：创建导出任务
            var createResponse = await CreateExportTask(exportTask, accessToken);
            if (!createResponse.success)
            {
                Debug.LogError($"创建导出任务失败，错误码: {createResponse.code}, 消息: {createResponse.msg}, 日志ID: {createResponse.log_id}");
                return;
            }

            var ticket = createResponse.data?.ticket;
            if (string.IsNullOrEmpty(ticket))
            {
                Debug.LogError("创建导出任务成功但未获取到ticket");
                return;
            }

            Debug.Log($"导出任务创建成功，票据: {ticket}");
            Debug.Log(JsonUtility.ToJson(createResponse.data, true));

            // 步骤二：查询导出任务结果
            Debug.Log($"开始查询导出任务结果，ticket: {ticket}");
            var maxRetries = 10; // 最大重试次数
            var retryCount = 0;
            GetExportTaskResponse resultResponse = null;

            while (retryCount < maxRetries)
            {
                // 等待一段时间再查询（导出需要时间）
                if (retryCount > 0)
                {
                    var waitTime = Math.Min(5 * retryCount, 30); // 递增等待时间，最大30秒
                    Debug.Log($"等待 {waitTime} 秒后重试查询...");
                    await Task.Delay(waitTime * 1000);
                }

                resultResponse = await GetExportTaskResult(ticket, syncConfig.fileToken, accessToken);
                if (resultResponse.success && resultResponse.data != null)
                {
                    var status = resultResponse.data.result.job_status;
                    var stateMessage = resultResponse.data.result.job_error_msg;
                    Debug.Log($"导出任务状态: {status} : {stateMessage}");
                    
                if (status == 0)
                {
                    Debug.Log("导出任务完成！");
                    break;
                }
                }
                else
                {
                    Debug.LogError($"查询导出任务结果失败，重试次数: {retryCount + 1}");
                    retryCount++;
                }
            }

            if (retryCount >= maxRetries)
            {
                Debug.LogError($"查询导出任务结果超时，已达到最大重试次数: {maxRetries}");
                return;
            }

            if (resultResponse?.data?.result?.job_status != 0)
            {
                Debug.LogError("导出任务未成功完成，跳过下载");
                return;
            }

            // 步骤三：下载导出文件
            var fileToken = resultResponse.data.result.file_token;
            if (string.IsNullOrEmpty(fileToken))
            {
                Debug.LogError("导出任务完成但未获取到文件token");
                return;
            }
            
            Debug.Log($"开始下载导出文件，file_token: {fileToken}");
            
            // 传递文件后缀信息，用于生成正确的文件名
            var downloadResponse = await DownloadExportFile(fileToken, accessToken, syncConfig.file_extension.ToString());
            
            if (downloadResponse.success && downloadResponse.data != null)
            {
                Debug.Log($"文件下载成功！");
                Debug.Log($"文件名: {resultResponse.data.result.file_name}");
                Debug.Log($"文件大小: {downloadResponse.data.file_size}");
                Debug.Log($"文件类型: {downloadResponse.data.file_type}");
                
                var folderPath = syncConfig.localFolderPath;
                var fullFolderPath = UnityPathUtility.AssetPathToFullPath(folderPath);
                var fileName = $"{resultResponse.data.result.file_name}_{sheetInfo.title}.{syncConfig.file_extension.ToString()}";
                var fullFilePath = Path.Combine(fullFolderPath, fileName);
                // 直接保存文件到本地，因为飞书API已经返回了文件内容
                await SaveFileToLocal(downloadResponse.data, fullFilePath);
            }
            else
            {
                Debug.LogError($"文件下载失败: {downloadResponse.msg}");
            }
        }
        
        private async Task NewWikiExprotTask(string accessToken, FeiShuFileSyncConfig syncConfig, List<Task> downloadTasks)
        {
            var realSyncConfig = new FeiShuFileSyncConfig();
            realSyncConfig.Clone(syncConfig);
            NodeInfo nodeInfo = await FeiShuWikiUtility.GetWikiNodeInfo(accessToken, syncConfig.fileToken);
            realSyncConfig.fileToken = nodeInfo.obj_token;
            // Debug.LogError("nodeInfo: " + JsonUtility.ToJson(nodeInfo, true));
            if(string.IsNullOrEmpty(realSyncConfig.fileToken)){
                Debug.LogError("无法获得有效的文件token");
                return;
            }
            await NewExportTask(accessToken, realSyncConfig, downloadTasks);
        }

        private async Task NewExportTask(string accessToken, FeiShuFileSyncConfig syncConfig, List<Task> downloadTasks){
            if(syncConfig.type == FeiShuFileSyncConfig.ExportType.sheet)
            {
                List<SheetInfo> sheetInfoList = await QuerySpreadsheetSheet(syncConfig.fileToken, accessToken);

                if(sheetInfoList == null && sheetInfoList.Count <= 0)
                {
                    return;
                }

                foreach(var sheetInfo in sheetInfoList){
                    // 将异步任务添加到列表中，而不是直接调用
                    var task = SheetExportTaskAsync(syncConfig, accessToken, sheetInfo);
                    downloadTasks.Add(task);
                }
            }
        }

        /// <summary>
        /// 发起飞书导出任务
        /// 从FeiShuConfig获取配置数据
        /// </summary>
        public async void CreateExportTaskAsync(FeiShuConfig config)
        {
            try
            {
                if (config.fileSyncConfigs.Count == 0)
                {
                    Debug.LogWarning("没有配置文件同步任务，请先在FeiShuConfig中添加配置");
                    return;
                }

                // 显示进度条标题
                EditorUtility.DisplayProgressBar("飞书文件导出", "正在验证访问令牌...", 0f);
                
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


                // 遍历所有配置的同步任务
                var totalTasks = config.fileSyncConfigs.Count;
                var currentTask = 0;
                var downloadTasks = new List<Task>(); // 收集所有下载任务
                
                foreach (var syncConfig in config.fileSyncConfigs)
                {
                    currentTask++;
                    var progress = (float)currentTask / totalTasks;
                    
                    if (string.IsNullOrEmpty(syncConfig.fileToken))
                    {
                        Debug.LogWarning($"跳过无效配置: token={syncConfig.fileToken}");
                        continue;
                    }

                    EditorUtility.DisplayProgressBar("飞书文件导出", 
                        $"正在准备导出任务... ({currentTask}/{totalTasks})", progress);

                    var fileToken = syncConfig.fileToken;
                    if(syncConfig.isWikiNode){
                        await NewWikiExprotTask(accessToken, syncConfig, downloadTasks);
                    }else{
                        await NewExportTask(accessToken, syncConfig, downloadTasks);
                    }
                }
                
                // 等待所有下载任务完成
                if (downloadTasks.Count > 0)
                {
                    EditorUtility.DisplayProgressBar("飞书文件导出", 
                        "正在等待所有下载任务完成...", 0.9f);
                    
                    Debug.Log($"等待 {downloadTasks.Count} 个下载任务完成...");
                    await Task.WhenAll(downloadTasks);
                    Debug.Log("所有下载任务已完成！");
                }
                
                AssetDatabase.SaveAssetIfDirty(config);
                
                // 清除进度条
                EditorUtility.ClearProgressBar();
                
                // 显示完成消息
                EditorUtility.DisplayDialog("导出完成", $"所有文件导出任务已完成！", "确定");
            }
            catch (Exception ex)
            {
                Debug.LogError($"创建导出任务时发生异常: {ex.Message}");
                // 发生异常时也要清除进度条
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("导出失败", $"导出过程中发生异常: {ex.Message}", "确定");
            }
        }
         /// <summary>
        /// 查询导出任务结果
        /// 步骤二：根据导出任务ID获取导出结果和文件信息
        /// </summary>
        private async Task<GetExportTaskResponse> GetExportTaskResult(string ticket, string token, string userAccessToken)
        {
            try
            {
                var url = $"{FeiShuFileSyncEditorWindow.FEISHU_API_BASE}/drive/v1/export_tasks/{ticket}?token={token}";
                Debug.Log($"查询导出任务结果: {url}");

                // 创建新的HttpClient实例，避免共享状态问题
                using (var client = new HttpClient())
                {
                    // 设置请求头
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {userAccessToken}");

                    var response = await client.GetAsync(url);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    Debug.Log($"查询导出任务结果响应状态: {response.StatusCode}");
                    Debug.Log($"查询导出任务结果响应内容: {responseContent}");

                    if (response.IsSuccessStatusCode)
                    {
                        return JsonUtility.FromJson<GetExportTaskResponse>(responseContent);
                    }
                    else
                    {
                        Debug.LogError($"查询导出任务结果失败: {response.StatusCode}, 响应内容: {responseContent}");
                        return new GetExportTaskResponse { code = (int)response.StatusCode, msg = responseContent };
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"查询导出任务结果异常: {ex.Message}");
                Debug.LogError($"异常类型: {ex.GetType().Name}");
                Debug.LogError($"异常堆栈: {ex.StackTrace}");
                return new GetExportTaskResponse { code = -1, msg = ex.Message };
            }
        }

        /// <summary>
        /// 下载导出文件
        /// 步骤三：根据文件token下载导出文件
        /// 参考飞书官方API文档和演示代码
        /// </summary>
        private async Task<DownloadExportResponse> DownloadExportFile(string fileToken, string userAccessToken, string fileExtension)
        {
            try
            {
                // 根据cURL示例，使用正确的API端点：/drive/v1/export_tasks/file/{file_token}/download
                var url = $"{FeiShuFileSyncEditorWindow.FEISHU_API_BASE}/drive/v1/export_tasks/file/{fileToken}/download";
                Debug.Log($"下载导出文件: {url}");
                Debug.Log($"file_token: {fileToken}");

                // 创建新的HttpClient实例，避免共享状态问题
                using (var client = new HttpClient())
                {
                    // 设置请求头 - 参考官方演示代码
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {userAccessToken}");
                    
                    // 设置超时时间（参考官方代码的Timeout = -1，这里设置为5分钟）
                    client.Timeout = TimeSpan.FromMinutes(5);

                    // 根据Python代码，使用GET请求，不需要请求体
                    var response = await client.GetAsync(url);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    Debug.Log($"下载导出文件响应状态: {response.StatusCode}");
                    Debug.Log($"下载导出文件响应内容: {responseContent}");

                    if (response.IsSuccessStatusCode)
                    {
                        // 飞书API直接返回文件内容，不是JSON响应
                        // 我们需要从响应头中获取文件信息
                        var fileName = GetFileNameFromHeaders(response.Headers);
                        var fileSize = response.Content.Headers.ContentLength ?? 0;
                        
                        // 读取文件内容
                        var fileBytes = await response.Content.ReadAsByteArrayAsync();
                        Debug.Log($"文件下载成功，大小: {fileBytes.Length} 字节");

                        // 构造下载响应对象，使用指定的文件后缀
                        if (string.IsNullOrEmpty(fileName))
                        {
                            // 如果没有从响应头获取到文件名，使用配置的文件后缀生成
                            fileName = $"exported_file_{DateTime.Now:yyyyMMdd_HHmmss}.{fileExtension}";
                        }
                        else if (!fileName.EndsWith($".{fileExtension}"))
                        {
                            // 如果文件名没有正确的后缀，添加后缀
                            fileName = $"{fileName}.{fileExtension}";
                        }
                        
                        var downloadData = new DownloadFileData
                        {
                            file_name = fileName,
                            file_size = fileSize.ToString(),
                            file_type = GetFileTypeFromHeaders(response.Headers),
                            file_content = fileBytes // 添加文件内容字段
                        };

                        return new DownloadExportResponse
                        {
                            code = 0,
                            msg = "success",
                            data = downloadData
                        };
                    }
                    else
                    {
                        Debug.LogError($"下载导出文件失败: {response.StatusCode}, 响应内容: {responseContent}");
                        return new DownloadExportResponse { code = (int)response.StatusCode, msg = responseContent };
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"下载导出文件异常: {ex.Message}");
                Debug.LogError($"异常类型: {ex.GetType().Name}");
                Debug.LogError($"异常堆栈: {ex.StackTrace}");
                return new DownloadExportResponse { code = -1, msg = ex.Message };
            }
        }

        /// <summary>
        /// 调用飞书API创建导出任务
        /// </summary>
        private async Task<CreateExportTaskResponse> CreateExportTask(ExportTask exportTask, string userAccessToken)
        {
            try
            {
                var url = $"{FeiShuFileSyncEditorWindow.FEISHU_API_BASE}/drive/v1/export_tasks";
                
                var jsonContent = JsonUtility.ToJson(exportTask);
                Debug.Log($"导出任务请求内容: {jsonContent}");
                
                // 使用StringContent，它会自动设置正确的Content-Type
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                Debug.Log($"Content-Type: {content.Headers.ContentType}");

                // 创建新的HttpClient实例，避免共享状态问题
                using (var client = new HttpClient())
                {
                    // 设置请求头
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {userAccessToken}");
                    // 注意：StringContent会自动设置Content-Type，不需要手动添加

                    Debug.Log($"发送请求到: {url}");
                    Debug.Log($"使用访问令牌: {userAccessToken.Substring(0, Math.Min(20, userAccessToken.Length))}...");
                    
                    var response = await client.PostAsync(url, content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        return JsonUtility.FromJson<CreateExportTaskResponse>(responseContent);
                    }
                    else
                    {
                        Debug.LogError($"HTTP请求失败: {response.StatusCode}, 响应内容: {responseContent}");
                        return new CreateExportTaskResponse { code = (int)response.StatusCode, msg = responseContent };
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"HTTP请求异常: {ex.Message}");
                Debug.LogError($"异常类型: {ex.GetType().Name}");
                Debug.LogError($"异常堆栈: {ex.StackTrace}");
                return new CreateExportTaskResponse { code = -1, msg = ex.Message };
            }
        }

        /// <summary>
        /// 从响应头中获取文件名
        /// </summary>
        /// <param name="headers">HTTP响应头</param>
        /// <returns>文件名，如果未找到则返回null</returns>
        private string GetFileNameFromHeaders(System.Net.Http.Headers.HttpResponseHeaders headers)
        {
            try
            {
                // 尝试从Content-Disposition头获取文件名
                if (headers.TryGetValues("Content-Disposition", out var contentDispositionValues))
                {
                    string contentDisposition = null;
                    foreach (var value in contentDispositionValues)
                    {
                        contentDisposition = value;
                        break;
                    }
                    
                    if (!string.IsNullOrEmpty(contentDisposition))
                    {
                        // 解析Content-Disposition头，查找filename参数
                        var filenameMatch = System.Text.RegularExpressions.Regex.Match(contentDisposition, @"filename\*?=(?:UTF-8'')?([^;]+)");
                        if (filenameMatch.Success)
                        {
                            var filename = filenameMatch.Groups[1].Value.Trim('"');
                            Debug.Log($"从Content-Disposition头获取到文件名: {filename}");
                            return filename;
                        }
                    }
                }

                // 尝试从Content-Type头获取文件名
                if (headers.TryGetValues("Content-Type", out var contentTypeValues))
                {
                    string contentType = null;
                    foreach (var value in contentTypeValues)
                    {
                        contentType = value;
                        break;
                    }
                    
                    if (!string.IsNullOrEmpty(contentType))
                    {
                        Debug.Log($"Content-Type: {contentType}");
                    }
                }

                Debug.LogWarning("未从响应头中找到文件名");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"解析响应头失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 从响应头中获取文件类型
        /// </summary>
        /// <param name="headers">HTTP响应头</param>
        /// <returns>文件类型，如果未找到则返回默认值</returns>
        private string GetFileTypeFromHeaders(System.Net.Http.Headers.HttpResponseHeaders headers)
        {
            try
            {
                if (headers.TryGetValues("Content-Type", out var contentTypeValues))
                {
                    string contentType = null;
                    foreach (var value in contentTypeValues)
                    {
                        contentType = value;
                        break;
                    }
                    
                    if (!string.IsNullOrEmpty(contentType))
                    {
                        // 从Content-Type中提取文件类型
                        var typeMatch = System.Text.RegularExpressions.Regex.Match(contentType, @"^([^/]+/[^;]+)");
                        if (typeMatch.Success)
                        {
                            var fileType = typeMatch.Groups[1].Value;
                            Debug.Log($"从Content-Type头获取到文件类型: {fileType}");
                            return fileType;
                        }
                    }
                }

                Debug.LogWarning("未从响应头中找到文件类型，使用默认值");
                return "application/octet-stream";
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"解析响应头失败: {ex.Message}");
                return "application/octet-stream";
            }
        }

           /// <summary>
        /// 将下载的文件保存到本地
        /// 根据配置的本地文件路径保存文件
        /// </summary>
        /// <param name="fileData">文件数据</param>
        /// <param name="syncConfig">同步配置</param>
        private async Task SaveFileToLocal(DownloadFileData fileData, string fileFullPath)
        {
            try
            {
                if (fileData?.file_content == null || fileData.file_content.Length == 0)
                {
                    Debug.LogError("文件内容为空，无法保存");
                    return;
                }

                // 确定保存路径
                
                if (string.IsNullOrEmpty(fileFullPath))
                {
                    Debug.LogError("文件保存路径为空，无法保存");
                    return; 
                }
                string savePath= fileFullPath;

                // 确保目录存在
                var directory = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 保存文件
                await File.WriteAllBytesAsync(savePath, fileData.file_content);
                Debug.Log($"文件已保存到本地: {savePath}");
                Debug.Log($"文件大小: {fileData.file_content.Length} 字节");
                
                // 更新进度条显示保存完成
                EditorUtility.DisplayProgressBar("飞书文件导出", 
                    $"文件保存完成: {Path.GetFileName(savePath)}", 1.0f);

                // 在Unity中显示成功消息
                // EditorUtility.DisplayDialog("文件保存成功", 
                //     $"文件已成功保存到本地！\n\n保存路径: {savePath}\n文件大小: {fileData.file_size}", "确定");

                // 更新配置中的本地文件路径（如果之前为空）
                // if (string.IsNullOrEmpty(syncConfig.localFilePath))
                // {
                //     syncConfig.localFilePath = savePath;
                //     // 注意：syncConfig 不是 ScriptableObject，所以不需要调用 SetDirty
                //     // 如果需要保存配置，应该在主流程中调用 AssetDatabase.SaveAssetIfDirty
                // }
            }
            catch (Exception ex)
            {
                Debug.LogError($"保存文件到本地失败: {ex.Message}");
                // EditorUtility.DisplayDialog("保存失败", $"文件保存失败: {ex.Message}", "确定");
            }
        }
    }
}