using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;
using System;
using System.Text;
using System.Net.Http;
using System.Security.Cryptography;

namespace FeiShu.Editor
{
    public class FeiShuAccessTokenManager
    {
        private const string FEISHU_AUTH_BASE = "https://accounts.feishu.cn/open-apis";
        private const string FEISHU_API_BASE = "https://open.feishu.cn/open-apis";

        private string CLIENT_ID => _config?.feiShuAppId ?? "";
        private string CLIENT_SECRET => _config?.feiShuAppSecret ?? "";
        private string SCOPE => _config.Scope ?? "";

        private const string REDIRECT_URI = "http://localhost:8080/callback"; // 固定本地回调地址

        private FeiShuConfig _config;
        
        private string currentCodeVerifier;
        private string currentState;
        private LocalHttpServer httpServer;
        
        public FeiShuAccessTokenManager(){
            _config = FeiShuConfig.GetOrCreateConfig();
        }
        ~FeiShuAccessTokenManager(){
            httpServer?.Stop();
            httpServer?.Dispose();
        }
        private bool ValidateConfiguration()
        {
            var issues = new List<string>();
            
            if (string.IsNullOrEmpty(CLIENT_ID))
                issues.Add("飞书应用ID未配置");
            
            if (string.IsNullOrEmpty(CLIENT_SECRET))
                issues.Add("飞书应用密钥未配置");
            
            if (string.IsNullOrEmpty(SCOPE))
                issues.Add("权限范围未配置");
            
            if (issues.Count == 0)
            {
            //  EditorUtility.DisplayDialog("配置验证", 
            //      $"所有配置项验证通过！\n\n" +
            //      $"应用ID: {CLIENT_ID}\n" +
            //      $"权限范围: {SCOPE}\n" +
            //      $"重定向URL: {REDIRECT_URI}\n\n" +
            //      $"请确保在飞书开发者后台配置了重定向URL：\n{REDIRECT_URI}", "确定");
                Debug.Log("配置验证通过");
                return true;
            }
            else
            {
                var message = "发现以下配置问题：\n\n" + string.Join("\n", issues) + "\n\n请在FeiShuConfig中设置这些值。";
            //  EditorUtility.DisplayDialog("配置验证失败", message, "确定");
                Debug.LogError($"配置验证失败: {string.Join(", ", issues)}");
                return false;
            }
        }
        /// <summary>
        /// 检查访问令牌是否过期
        /// 直接从配置文件中读取时间进行判断
        /// </summary>
        public bool IsAccessTokenExpired(string tokenExpiryTime)
        {
            if (tokenExpiryTime == null)
            {
                Debug.LogWarning("tokenExpiryTime为空，无法检查令牌状态");
                return true;
            }
            
            // // 从配置文件中读取令牌过期时间
            // if (string.IsNullOrEmpty(_config.feiShuTokenExpiryTime))
            // {
            //     Debug.LogWarning("配置中未找到令牌过期时间，建议重新授权");
            //     return true;
            // }
            
            try
            {
                if (DateTime.TryParse(tokenExpiryTime, out DateTime expiryTime))
                {
                    var now = DateTime.UtcNow;
                    var timeUntilExpiry = expiryTime - now;
                    
                    if (timeUntilExpiry <= TimeSpan.Zero)
                    {
                        Debug.LogWarning($"访问令牌已过期，过期时间: {expiryTime:yyyy-MM-dd HH:mm:ss}");
                        return true;
                    }
                    
                    // 如果令牌在5分钟内过期，也认为即将过期
                    if (timeUntilExpiry <= TimeSpan.FromMinutes(5))
                    {
                        Debug.LogWarning($"访问令牌即将过期，剩余时间: {timeUntilExpiry.TotalMinutes:F1}分钟");
                        return true;
                    }
                    
                    Debug.Log($"访问令牌有效，剩余时间: {timeUntilExpiry.TotalMinutes:F1}分钟");
                    return false;
                }
                else
                {
                    Debug.LogWarning("配置中的令牌过期时间格式无效，建议重新授权");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"解析令牌过期时间失败: {ex.Message}");
                return true;
            }
        }

        /// <summary>
        /// 生成随机字符串
        /// </summary>
        private string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";
            var random = new System.Random();
            var result = new StringBuilder(length);
            
            for (int i = 0; i < length; i++)
            {
                result.Append(chars[random.Next(chars.Length)]);
            }
            
            return result.ToString();
        }

        /// <summary>
        /// 生成PKCE code_challenge
        /// </summary>
        private string GenerateCodeChallenge(string codeVerifier)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
                return Convert.ToBase64String(hash)
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .TrimEnd('=');
            }
        }

        /// <summary>
        /// 启动HTTP服务器接收授权回调
        /// </summary>
        private void StartHttpServer()
        {
            try
            {
                // 停止之前的服务器
                httpServer?.Stop();
                httpServer?.Dispose();
                
                                // 创建新的服务器，直接使用固定端口
                var baseUrl = "localhost:8080";
                Debug.Log($"启动本地HTTP服务器，监听地址: {baseUrl}");
                httpServer = new LocalHttpServer(baseUrl, OnAuthorizationReceived);
                httpServer.Start();
                
                Debug.Log($"HTTP服务器已启动，等待授权回调...");
            }
            catch (Exception ex)
            {
                Debug.LogError($"启动HTTP服务器失败: {ex.Message}");
                EditorUtility.DisplayDialog("错误", $"启动HTTP服务器失败: {ex.Message}\n\n请检查端口是否被占用。", "确定");
            }
        }
            /// <summary>
         /// 处理授权回调
         /// </summary>
        private async void OnAuthorizationReceived(string code, string state)
        {
            try
            {
                Debug.Log($"收到授权回调 - Code: {code}, State: {state}");
                
                // 验证state
                if (state != currentState)
                {
                    Debug.LogError($"State验证失败，期望: {currentState}, 实际: {state}");
                    return;
                }
                
                // 使用授权码获取访问令牌
                var tokenResult = await ExchangeCodeForTokenAsync(code, currentCodeVerifier);
                
                if (tokenResult != null && !string.IsNullOrEmpty(tokenResult.access_token))
                {
                    if (_config != null)
                    {
                        _config.FeiShuUserAccessToken = tokenResult.access_token; // 保存访问令牌
                        _config.FeiShuRefreshToken = tokenResult.refresh_token; // 保存刷新令牌
                        
                        // 注意：飞书响应中没有refresh_token，无法实现自动刷新

                        
                        // 记录令牌过期时间
                        var tokenExpiryTime = DateTime.UtcNow.AddSeconds(tokenResult.expires_in);
                        // 保存令牌过期时间到配置文件
                        _config.FeiShuTokenExpiryTime = tokenExpiryTime.ToString("O"); // ISO 8601格式

                        var refreshTokenExpiryTime = DateTime.UtcNow.AddSeconds(tokenResult.refresh_token_expires_in);
                        _config.FeiShuRefreshTokenExpiryTime = refreshTokenExpiryTime.ToString("O");
                            
                        EditorUtility.SetDirty(_config);
                        AssetDatabase.SaveAssetIfDirty(_config);

                        Debug.Log($"已保存访问令牌到配置");
                        Debug.Log($"访问令牌: {tokenResult.access_token.Substring(0, Math.Min(20, tokenResult.access_token.Length))}...");
                        Debug.Log($"令牌过期时间: {tokenExpiryTime:yyyy-MM-dd HH:mm:ss}");
                        Debug.LogWarning($"飞书响应中没有refresh_token，访问令牌将在{tokenResult.expires_in}秒后过期");
                    }
                }
                else
                {
                    Debug.LogError("获取访问令牌失败");
                    EditorUtility.DisplayDialog("授权失败", "获取访问令牌失败，请重新尝试授权流程。", "确定");
                }
                
                // 停止HTTP服务器
                httpServer?.Stop();
            }
            catch (Exception ex)
            {
                Debug.LogError($"处理授权回调失败: {ex.Message}");
                EditorUtility.DisplayDialog("错误", $"处理授权回调失败: {ex.Message}", "确定");
            }
        }

        /// <summary>
        /// 使用授权码获取访问令牌
        /// </summary>
        public async Task<OAuthTokenResponse> ExchangeCodeForTokenAsync(string authorizationCode, string codeVerifier)
        {
            try
            {
                var url = $"{FEISHU_API_BASE}/authen/v2/oauth/token";
                
                var requestData = new Dictionary<string, string>
                {
                    ["grant_type"] = "authorization_code",
                    ["client_id"] = CLIENT_ID,
                    ["client_secret"] = CLIENT_SECRET,
                    ["code"] = authorizationCode,
                    ["redirect_uri"] = REDIRECT_URI,
                    ["code_verifier"] = codeVerifier
                    // 注意：令牌交换请求中不需要再次指定scope
                };

                var content = new FormUrlEncodedContent(requestData);
                using (var httpClient = new HttpClient())
                {
                    // 设置请求头 - FormUrlEncodedContent会自动设置Content-Type
                    httpClient.DefaultRequestHeaders.Clear();

                    var response = await httpClient.PostAsync(url, content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    Debug.Log($"飞书API响应状态: {response.StatusCode}");
                    Debug.Log($"飞书API响应内容: {responseContent}");

                    if (response.IsSuccessStatusCode)
                    {
                        var tokenResponse = JsonUtility.FromJson<OAuthTokenResponse>(responseContent);
                        
                        if (tokenResponse != null && tokenResponse.success && !string.IsNullOrEmpty(tokenResponse.access_token))
                        {
                            Debug.Log($"成功获取访问令牌");
                            Debug.Log($"访问令牌: {tokenResponse.access_token.Substring(0, Math.Min(20, tokenResponse.access_token.Length))}...");
                            Debug.Log($"过期时间: {tokenResponse.expires_in}秒");
                            Debug.Log($"报文内容: \n{tokenResponse}");
                            
                            return tokenResponse;
                        }
                        else
                        {
                            Debug.LogError($"获取访问令牌失败: 响应格式不正确");
                            Debug.LogError($"完整响应: {responseContent}");
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
                Debug.LogError($"交换授权码失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取用户授权
        /// 参考飞书文档：https://open.feishu.cn/document/authentication-management/access-token/get-user-access-token
        /// </summary>
        public Task GetUserAuthorizationAsync()
        {
            try
            {
                // 验证配置
                if (string.IsNullOrEmpty(CLIENT_ID))
                {
                    EditorUtility.DisplayDialog("配置错误", "请在FeiShuConfig中设置飞书应用ID (feiShuAppId)", "确定");
                    Debug.LogError("飞书应用ID未配置，请在FeiShuConfig中设置feiShuAppId");
                    return Task.CompletedTask;
                }
                
                if (string.IsNullOrEmpty(CLIENT_SECRET))
                {
                    EditorUtility.DisplayDialog("配置错误", "请在FeiShuConfig中设置飞书应用密钥 (feiShuAppSecret)", "确定");
                    Debug.LogError("飞书应用密钥未配置，请在FeiShuConfig中设置feiShuAppSecret");
                    return Task.CompletedTask;
                }
                
                // REDIRECT_URI是常量，不需要验证
                
                                Debug.Log($"使用应用ID: {CLIENT_ID}");
                Debug.Log($"使用应用密钥: {CLIENT_SECRET.Substring(0, Math.Min(8, CLIENT_SECRET.Length))}...");
                Debug.Log($"使用重定向URL: {REDIRECT_URI}");
                Debug.Log($"使用权限范围: {SCOPE}");
                
                // 生成随机state和PKCE code_verifier
                currentState = GenerateRandomString(32);
                currentCodeVerifier = GenerateRandomString(64);
                
                // 生成code_challenge (SHA256哈希)
                var codeChallenge = GenerateCodeChallenge(currentCodeVerifier);
                
                // 启动HTTP服务器接收回调
                StartHttpServer();
                
                                // 构建授权URL
                var authUrl = $"{FEISHU_AUTH_BASE}/authen/v1/authorize?" +
                    $"client_id={CLIENT_ID}&" +
                    $"redirect_uri={Uri.EscapeDataString(REDIRECT_URI)}&" +
                    $"state={currentState}&" +
                    $"code_challenge={codeChallenge}&" +
                    $"code_challenge_method=S256&" +
                    $"scope={Uri.EscapeDataString(SCOPE)}";
            

                Debug.Log($"正在启动授权流程...");
                Debug.Log($"授权URL: {authUrl}");
                
                // 在Unity中打开浏览器（仅限Editor模式）
                if (Application.isEditor)
                {
                    Application.OpenURL(authUrl);
                }
                
                EditorUtility.DisplayDialog("飞书授权", 
                    $"浏览器已自动打开飞书授权页面。\n\n请完成授权，系统将自动获取授权码。", "确定");
            }
            catch (Exception ex)
            {
                Debug.LogError($"获取用户授权失败: {ex.Message}");
            }
            
            return Task.CompletedTask;
        }
        public async Task RefreshTokenAsync(){
            try
            {
                Debug.Log($"开始刷新访问令牌");

                // 验证配置
                if (string.IsNullOrEmpty(CLIENT_ID))
                {
                    EditorUtility.DisplayDialog("配置错误", "请在FeiShuConfig中设置飞书应用ID (feiShuAppId)", "确定");
                    Debug.LogError("飞书应用ID未配置，请在FeiShuConfig中设置feiShuAppId");
                    return;
                }

                var tokenResult = await RefreshAccessTokenAsync(_config.FeiShuRefreshToken);
                        
                if (tokenResult != null && !string.IsNullOrEmpty(tokenResult.access_token))
                {
                    if (_config != null)
                    {
                        _config.FeiShuUserAccessToken = tokenResult.access_token; // 保存访问令牌
                        _config.FeiShuRefreshToken = tokenResult.refresh_token; // 保存刷新令牌
                        
                        // 记录令牌过期时间
                        var tokenExpiryTime = DateTime.UtcNow.AddSeconds(tokenResult.expires_in);
                        // 保存令牌过期时间到配置文件
                        _config.FeiShuTokenExpiryTime = tokenExpiryTime.ToString("O"); // ISO 8601格式

                        var refreshTokenExpiryTime = DateTime.UtcNow.AddSeconds(tokenResult.refresh_token_expires_in);
                        _config.FeiShuRefreshTokenExpiryTime = refreshTokenExpiryTime.ToString("O");
                            
    

                        Debug.Log($"已保存访问令牌到配置");
                        Debug.Log($"访问令牌: {tokenResult.access_token.Substring(0, Math.Min(20, tokenResult.access_token.Length))}...");
                        Debug.Log($"令牌过期时间: {tokenExpiryTime:yyyy-MM-dd HH:mm:ss}");

                        EditorUtility.SetDirty(_config);
                        AssetDatabase.SaveAssetIfDirty(_config);
                    }
                }
                else
                {
                    Debug.LogError("获取访问令牌失败");
                    EditorUtility.DisplayDialog("授权失败", "获取访问令牌失败，请重新尝试授权流程。", "确定");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"刷新令牌失败: {ex.Message}");
            }
        }
        /// <summary>
        /// 使用刷新令牌获取新的访问令牌
        /// </summary>
        public async Task<OAuthTokenResponse> RefreshAccessTokenAsync(string refreshToken)
        {
            try
            {
                var url = $"{FEISHU_API_BASE}/authen/v2/oauth/token";
                
                var requestData = new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["client_id"] = CLIENT_ID,
                    ["client_secret"] = CLIENT_SECRET,
                    ["refresh_token"] = refreshToken
                };

                var content = new FormUrlEncodedContent(requestData);
                using (var httpClient = new HttpClient())
                {
                    // 设置请求头 - FormUrlEncodedContent会自动设置Content-Type
                    httpClient.DefaultRequestHeaders.Clear();

                    var response = await httpClient.PostAsync(url, content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var tokenResponse = JsonUtility.FromJson<OAuthTokenResponse>(responseContent);
                        Debug.Log($"飞书RefreshToken API响应: {response}");
                        if (tokenResponse != null && tokenResponse.success && !string.IsNullOrEmpty(tokenResponse.access_token))
                        {
                            Debug.Log($"成功刷新访问令牌");
                            return tokenResponse;
                        }
                        else
                        {
                            Debug.LogError($"刷新访问令牌失败: 响应格式不正确");
                            Debug.LogError($"完整响应: {responseContent}");
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
                Debug.LogError($"刷新访问令牌失败: {ex.Message}");
                return null;
            }
        }

        // /// <summary>
        // /// 使用刷新令牌更新访问令牌和配置
        // /// </summary>
        // /// <param name="refreshToken">刷新令牌</param>
        // /// <returns>更新是否成功</returns>
        // private async Task<bool> UpdateTokensFromRefreshTokenAsync(string refreshToken)
        // {

        //     var tokenResult = await RefreshAccessTokenAsync(refreshToken);
        //     if(tokenResult != null && !string.IsNullOrEmpty(tokenResult.access_token)){
        //         _config.FeiShuUserAccessToken = tokenResult.access_token;
        //         _config.FeiShuRefreshToken = tokenResult.refresh_token;
        //         _config.FeiShuTokenExpiryTime = tokenResult.expires_in.ToString("O");
        //         _config.FeiShuRefreshTokenExpiryTime = tokenResult.refresh_token_expires_in.ToString("O");
        //         EditorUtility.SetDirty(_config);
        //         AssetDatabase.SaveAssetIfDirty(_config);
        //         return true;
        //     }
        //     return false;
        // }

        public async Task UpdateTokenStatus()
        {
            if(_config == null){
                _config = FeiShuConfig.GetOrCreateConfig();
                return;
            }

            // 判断token和refreshToken是否为空
            if (string.IsNullOrEmpty(_config?.FeiShuUserAccessToken) 
                || string.IsNullOrEmpty(_config?.FeiShuRefreshToken))
            {
                EditorUtility.DisplayDialog("配置错误", "访问令牌或刷新令牌为空，请重新授权", "确定");
                await GetUserAuthorizationAsync();
                return;
            }
            


            bool needNewToken = false;
            bool needNewRefreshToken = false;
            var tokenExpiryTime = _config?.FeiShuTokenExpiryTime;
            // 判断token是否过期
            if(IsAccessTokenExpired(tokenExpiryTime)){
                var reflashTokenExpiryTime = _config?.FeiShuRefreshTokenExpiryTime;
                // 判断refreshToken是否过期
                if(IsAccessTokenExpired(reflashTokenExpiryTime)){
                    // 这里添加刷新token代码
                    needNewToken = true;
                }else{
                    needNewRefreshToken = true;
                }
            }



            if (needNewToken)
            {
                EditorUtility.DisplayDialog("配置错误", "访问令牌已过期，请重新授权", "确定");
                await GetUserAuthorizationAsync();
            }
            if (needNewRefreshToken)
            {
                await RefreshTokenAsync();
            }
        }

        public async Task ForceUpdateAccessToken(){
            await GetUserAuthorizationAsync();
        }

        public async Task UpdateAccessToken(){
            if(!ValidateConfiguration()){
                return;
            }
            await UpdateTokenStatus();
        }
    }

}