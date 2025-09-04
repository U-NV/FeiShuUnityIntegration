using System;
using UnityEngine;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;

namespace FeiShu.Editor
{
    /// <summary>
    /// 简单的HTTP服务器，用于接收飞书授权回调
    /// </summary>
    public class LocalHttpServer : IDisposable
    {
        private HttpListener listener;
        private bool isRunning;
        private string callbackUrl;
        private Action<string, string> onAuthorizationReceived;
        public LocalHttpServer(string url, Action<string, string> callback)
        {
            callbackUrl = url;
            onAuthorizationReceived = callback;
            listener = new HttpListener();
            
            // 确保URL格式正确，HttpListener需要完整的URL格式
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = "http://" + url;
            }
            
            // 确保URL以/结尾
            if (!url.EndsWith("/"))
            {
                url += "/";
            }
            
            listener.Prefixes.Add(url);
            Debug.Log($"HTTP服务器将监听: {url}");
        }
        
        public void Start()
        {
            if (!isRunning)
            {
                listener.Start();
                isRunning = true;
                Task.Run(ListenForRequests);
                Debug.Log($"HTTP服务器已启动，监听地址: {callbackUrl}");
            }
        }
        
        public void Stop()
        {
            if (isRunning)
            {
                isRunning = false;
                listener.Stop();
                Debug.Log("HTTP服务器已停止");
            }
        }
        
        private async Task ListenForRequests()
        {
            while (isRunning)
            {
                try
                {
                    var context = await listener.GetContextAsync();
                    _ = HandleRequest(context);
                }
                catch (Exception ex)
                {
                    if (isRunning)
                    {
                        Debug.LogError($"HTTP服务器错误: {ex.Message}");
                    }
                }
            }
        }
        
        private Task HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            
            try
            {
                if (request.Url.AbsolutePath == "/callback")
                {
                    var query = request.QueryString;
                    var code = query["code"];
                    var state = query["state"];
                    var error = query["error"];
                    
                    if (!string.IsNullOrEmpty(error))
                    {
                        var errorHtml = GenerateErrorHtml(error);
                        var buffer = System.Text.Encoding.UTF8.GetBytes(errorHtml);
                        response.StatusCode = 400;
                        response.ContentType = "text/html; charset=utf-8";
                        response.ContentLength64 = buffer.Length;
                        response.OutputStream.Write(buffer, 0, buffer.Length);
                    }
                    else if (!string.IsNullOrEmpty(code))
                    {
                        // 先发送成功响应，避免"headers already sent"错误
                        var successHtml = GenerateSuccessHtml();
                        var buffer = System.Text.Encoding.UTF8.GetBytes(successHtml);
                        response.StatusCode = 200;
                        response.ContentType = "text/html; charset=utf-8";
                        response.ContentLength64 = buffer.Length;
                        response.OutputStream.Write(buffer, 0, buffer.Length);
                        
                        // 在响应发送后调用回调函数，使用延迟调用确保在主线程执行
                        var capturedCode = code;
                        var capturedState = state;
                        EditorApplication.delayCall += () => onAuthorizationReceived?.Invoke(capturedCode, capturedState);
                    }
                    else
                    {
                        response.StatusCode = 400;
                        response.OutputStream.Close();
                    }
                }
                else
                {
                    response.StatusCode = 404;
                    response.OutputStream.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"处理HTTP请求时发生错误: {ex.Message}");
                try
                {
                    response.StatusCode = 500;
                    response.OutputStream.Close();
                }
                catch
                {
                    // 忽略关闭输出流时的错误
                }
            }
            
            return Task.CompletedTask;
        }
        private string GenerateSuccessHtml()
        {
            return @"<!DOCTYPE html>
                <html>
                <head>
                <meta charset='utf-8'>
                <title>授权成功</title>
                <style>
                    body { font-family: Arial, sans-serif; background: #f4f4f4; margin: 0; display: flex; justify-content: center; align-items: center; height: 100vh; }
                    .container { text-align: center; background: #fff; padding: 30px; border-radius: 10px; box-shadow: 0 0 10px rgba(0,0,0,0.1); }
                    h2 { color: #28a745; }
                    p { color: #666; }
                    .close-btn { padding: 10px 20px; font-size: 16px; color: #fff; background: #007bff; border: none; border-radius: 5px; cursor: pointer; }
                    .close-btn:hover { background: #0056b3; }
                </style>
                </head>
                <body>
                <div class='container'>
                    <h2>✅ 授权成功！</h2>
                    <p>你已成功完成飞书授权登录流程。</p>
                    <p>授权码已自动获取，可以关闭此窗口。</p>
                    <button class='close-btn' onclick='window.close()'>关闭窗口</button>
                </div>
                </body>
                </html>";
        }
        
        private string GenerateErrorHtml(string error)
        {
            return $@"<!DOCTYPE html>
                <html>
                <head>
                <meta charset='utf-8'>
                <title>授权失败</title>
                <style>
                    body {{ font-family: Arial, sans-serif; background: #f4f4f4; margin: 0; display: flex; justify-content: center; align-items: center; height: 100vh; }}
                    .container {{ text-align: center; background: #fff; padding: 30px; border-radius: 10px; box-shadow: 0 0 10px rgba(0,0,0,0.1); }}
                    h2 {{ color: #dc3545; }}
                    p {{ color: #666; }}
                    .error {{ color: #dc3545; font-weight: bold; }}
                </style>
                </head>
                <body>
                <div class='container'>
                    <h2>❌ 授权失败</h2>
                    <p>飞书授权过程中发生错误：</p>
                    <p class='error'>{error}</p>
                    <p>请重新尝试授权流程。</p>
                </div>
                </body>
                </html>";
        }
        
        public void Dispose()
        {
            Stop();
            listener?.Close();
        }
    }
}