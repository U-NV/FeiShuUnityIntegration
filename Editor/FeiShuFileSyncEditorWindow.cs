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

     public class FeiShuFileSyncEditorWindow 
     {
        public const string FEISHU_API_BASE = "https://open.feishu.cn/open-apis";
        private FeiShuAccessTokenManager tokenManager;
        private DownloadTaskManager downloadTaskManager;
        private UploadTaskManager uploadTaskManager;
        private FeiShuConfig _config;
         
        public void Init()
        {
            _config = FeiShuConfig.GetOrCreateConfig();
            tokenManager = new FeiShuAccessTokenManager();
            downloadTaskManager = new DownloadTaskManager(tokenManager);
            uploadTaskManager = new UploadTaskManager(tokenManager);
        }


        public void OnGUI()
        {
            GUILayout.Label("同步云文档", EditorStyles.boldLabel);
            
            if(!_config)
            {
                _config = FeiShuConfig.GetOrCreateConfig();
            }
            var config = _config;
            
            // 显示配置信息
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label($"云文档下载任务数量: {config.fileSyncConfigs.Count}");
            // 点击按钮，开始同步数据
            if (GUILayout.Button("开始下载", GUILayout.Width(100)))
            {
                downloadTaskManager.CreateExportTaskAsync(config);
            }
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label($"云文档上传任务数量: {config.fileUploadConfigs.Count}");
            // 点击按钮，开始上传数据
            if (GUILayout.Button("开始上传", GUILayout.Width(100)))
            {
                uploadTaskManager.CreateUploadTaskAsync(config);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            ShowTokenStateInfo();
        }


    
        private void ShowTokenStateInfo(){
            var config = _config;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                            // 显示固定回调地址
            EditorGUILayout.LabelField("回调地址:", "http://localhost:8080/callback (固定)");
            EditorGUILayout.LabelField("权限范围:", config.Scope);
                
                            // 显示访问令牌状态
                if (!string.IsNullOrEmpty(config.FeiShuUserAccessToken))
                {
                    var tokenStatus = "未知";
                    var tokenColor = Color.white;
                    
                    if (!string.IsNullOrEmpty(config.FeiShuTokenExpiryTime))
                    {
                        if (DateTime.TryParse(config.FeiShuTokenExpiryTime, out DateTime expiryTime))
                        {
                            var now = DateTime.UtcNow;
                            var timeUntilExpiry = expiryTime - now;
                            
                            if (timeUntilExpiry <= TimeSpan.Zero)
                            {
                                tokenStatus = "已过期";
                                tokenColor = Color.red;
                            }
                            else if (timeUntilExpiry <= TimeSpan.FromMinutes(5))
                            {
                                tokenStatus = $"即将过期 ({timeUntilExpiry.TotalMinutes:F1}分钟)";
                                tokenColor = Color.yellow;
                            }
                            else
                            {
                                tokenStatus = $"有效 ({timeUntilExpiry.TotalMinutes:F1}分钟)";
                                tokenColor = Color.green;
                            }
                        }
                    }
                    
                    var originalColor = GUI.color;
                    GUI.color = tokenColor;
                    EditorGUILayout.LabelField("访问令牌状态:", tokenStatus);
                    GUI.color = originalColor;
                }
                else
                {
                    EditorGUILayout.LabelField("访问令牌状态:", "未授权");
                }
                
            // 显示刷新令牌状态
            if (!string.IsNullOrEmpty(config.FeiShuRefreshToken))
            {
                var refreshTokenStatus = "未知";
                var refreshTokenColor = Color.white;
                
                if (!string.IsNullOrEmpty(config.FeiShuRefreshTokenExpiryTime))
                {
                    if (DateTime.TryParse(config.FeiShuRefreshTokenExpiryTime, out DateTime refreshExpiryTime))
                    {
                        var now = DateTime.UtcNow;
                        var timeUntilRefreshExpiry = refreshExpiryTime - now;
                        
                        if (timeUntilRefreshExpiry <= TimeSpan.Zero)
                        {
                            refreshTokenStatus = "已过期";
                            refreshTokenColor = Color.red;
                        }
                        else if (timeUntilRefreshExpiry <= TimeSpan.FromDays(7))
                        {
                            refreshTokenStatus = $"即将过期 ({timeUntilRefreshExpiry.TotalDays:F1}天)";
                            refreshTokenColor = Color.yellow;
                        }
                        else
                        {
                            refreshTokenStatus = $"有效 ({timeUntilRefreshExpiry.TotalDays:F1}天)";
                            refreshTokenColor = Color.green;
                        }
                    
                    }
                }
                
                var originalColor = GUI.color;
                GUI.color = refreshTokenColor;
                EditorGUILayout.LabelField("刷新令牌状态:", refreshTokenStatus);
                GUI.color = originalColor;
            }
            else
            {
                EditorGUILayout.LabelField("刷新令牌状态:", "未获取");
            }

            if (GUILayout.Button("重新授权"))
            {
                _ = tokenManager.ForceUpdateAccessToken();
            }
            EditorGUILayout.EndVertical();
        }


     

       

        /// <summary>
        /// 清理和验证file_token格式
        /// 参考飞书官方API要求，确保file_token格式正确
        /// </summary>
        /// <param name="fileToken">原始file_token</param>
        /// <returns>清理后的file_token，如果无效则返回null</returns>
        private string CleanAndValidateFileToken(string fileToken)
        {
            if (string.IsNullOrEmpty(fileToken))
            {
                return null;
            }

            // 清理空白字符
            var cleanedToken = fileToken.Trim();
            
            // 检查长度
            if (cleanedToken.Length < 10 || cleanedToken.Length > 200)
            {
                Debug.LogWarning($"file_token长度异常: {cleanedToken.Length}, token: {cleanedToken}");
                return null;
            }

            // 检查是否包含无效字符
            var invalidChars = new[] { ' ', '\n', '\r', '\t', '<', '>', '"', '&', '\'', '\\' };
            bool hasInvalidChar = false;
            foreach (var c in cleanedToken)
            {
                if (Array.IndexOf(invalidChars, c) >= 0)
                {
                    hasInvalidChar = true;
                    break;
                }
            }
            if (hasInvalidChar)
            {
                Debug.LogWarning($"file_token包含无效字符: {cleanedToken}");
                return null;
            }

            // 尝试URL解码，因为飞书的token可能包含URL编码字符
            try
            {
                var decodedToken = Uri.UnescapeDataString(cleanedToken);
                if (decodedToken != cleanedToken)
                {
                    Debug.Log($"file_token已URL解码: {decodedToken}");
                    cleanedToken = decodedToken;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"file_token URL解码失败: {ex.Message}");
                // 解码失败不是致命错误，继续使用原始token
            }

            Debug.Log($"file_token清理完成: {cleanedToken}");
            return cleanedToken;
        }

     
    }
}