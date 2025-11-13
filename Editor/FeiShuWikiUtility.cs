using System;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

namespace U0UGames.FeiShu.Editor
{
    public static class FeiShuWikiUtility
    {
        /// <summary>
        /// 获取知识空间节点信息
        /// </summary>
        /// <param name="userAccessToken">用户访问令牌</param>
        /// <param name="nodeToken">节点Token（Wiki Token或文档Token）</param>
        /// <param name="objType">对象类型（可选，当使用文档Token时需要指定，如"doc"）</param>
        /// <returns>节点信息，失败返回null</returns>
        public static async Task<NodeInfo> GetWikiNodeInfo(string userAccessToken, string nodeToken, string objType = null)
        {
            try
            {
                // 构建URL，添加token查询参数
                var url = $"{FeiShuFileSyncEditorWindow.FEISHU_API_BASE}/wiki/v2/spaces/get_node?token={Uri.EscapeDataString(nodeToken)}";
                
                // 如果指定了obj_type，添加到查询参数
                if (!string.IsNullOrEmpty(objType))
                {
                    url += $"&obj_type={Uri.EscapeDataString(objType)}";
                }
                
                // 创建新的HttpClient实例，避免共享状态问题
                using (var client = new HttpClient())
                {
                    // 设置请求头
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {userAccessToken}");

                    var response = await client.GetAsync(url);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var nodeResponse = JsonUtility.FromJson<GetNodeSpaceResponse>(responseContent);
                        
                        if (nodeResponse.success && nodeResponse.data?.node != null)
                        {
                            Debug.Log($"获取节点信息成功: {nodeResponse.data.node.title}, node_token: {nodeResponse.data.node.node_token}");
                            return nodeResponse.data.node;
                        }
                        else
                        {
                            Debug.LogError($"获取节点信息失败，响应: {responseContent}");
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
                Debug.LogError($"获取节点信息异常: {ex.Message}");
                return null;
            }
        }


        public static async Task<string> GetWikiNodeFileToken(string userAccessToken, string nodeToken, string objType = null){
            var nodeInfo = await GetWikiNodeInfo(userAccessToken, nodeToken, objType);
            if(nodeInfo == null){
                return null;
            }

            return nodeInfo.obj_token;
        }
    }
}