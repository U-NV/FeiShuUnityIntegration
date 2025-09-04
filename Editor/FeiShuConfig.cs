using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FeiShu.Editor
{
    public class FeiShuUserToken:ScriptableObject
    {
        public static string FeiShuUserTokenAssetPath = "Assets/Settings/FeiShuUserToken.asset";

        public string feiShuUserAccessToken = "";
        public string feiShuRefreshToken = "";
        public string feiShuTokenExpiryTime = "";
        public string feiShuRefreshTokenExpiryTime = "";
        public static FeiShuUserToken GetOrCreateConfig()
        {
            var assetDirectoryPath = Path.GetDirectoryName(FeiShuUserTokenAssetPath);
            var realDirectoryPath = UnityPathUtility.AssetPathToFullPath(assetDirectoryPath);
            if (!Directory.Exists(realDirectoryPath))
            {
                Directory.CreateDirectory(realDirectoryPath);
            }

            var config = AssetDatabase.LoadAssetAtPath<FeiShuUserToken>(FeiShuUserTokenAssetPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<FeiShuUserToken>();
                AssetDatabase.CreateAsset(config, FeiShuUserTokenAssetPath);
                return config;
            }
      
            return config;
        }
    }


    [System.Serializable]
    public class FeiShuConfig:ScriptableObject
    {
        public string feiShuAppId = "";
        public string feiShuAppSecret = "";
        // public string feiShuAuthorizationCode = "";
        public string _scope = SCOPE;
        public const string SCOPE = "offline_access sheets:spreadsheet:read drive:export:readonly docs:document:export vc:export"; // 权限范围
        public string Scope{
            get{
                if(string.IsNullOrEmpty(_scope)){
                    return SCOPE;
                }
                return _scope;
            }
        }

        private FeiShuUserToken _feiShuUserToken;
        public FeiShuUserToken FeiShuUserToken{
        get{
            if(_feiShuUserToken == null){
                _feiShuUserToken = FeiShuUserToken.GetOrCreateConfig();
            }
            return _feiShuUserToken;
        }
}
        public string FeiShuUserAccessToken {
            get{
                return FeiShuUserToken.feiShuUserAccessToken;
            }
            set{
                FeiShuUserToken.feiShuUserAccessToken = value;
            }
        }
        public string FeiShuRefreshToken {
            get{
                return FeiShuUserToken.feiShuRefreshToken;
            }
            set{
                FeiShuUserToken.feiShuRefreshToken = value;
            }
        }
        
        // 令牌过期时间（ISO 8601格式字符串，便于序列化）
        public string FeiShuTokenExpiryTime {
            get{
                return FeiShuUserToken.feiShuTokenExpiryTime;
            }
            set{
                FeiShuUserToken.feiShuTokenExpiryTime = value;
            }
        }
        public string FeiShuRefreshTokenExpiryTime {
            get{
                return FeiShuUserToken.feiShuRefreshTokenExpiryTime;
            }
            set{
                FeiShuUserToken.feiShuRefreshTokenExpiryTime = value;
            }
        }

        public List<FeiShuFileSyncConfig> fileSyncConfigs = new List<FeiShuFileSyncConfig>();
        public static string FeiShuConfigAssetPath = "Assets/Settings/FeiShuConfig.asset";
        public static FeiShuConfig GetOrCreateConfig()
        {
            var assetDirectoryPath = Path.GetDirectoryName(FeiShuConfigAssetPath);
            var realDirectoryPath = UnityPathUtility.AssetPathToFullPath(assetDirectoryPath);
            if (!Directory.Exists(realDirectoryPath))
            {
                Directory.CreateDirectory(realDirectoryPath);
            }

            var config = AssetDatabase.LoadAssetAtPath<FeiShuConfig>(FeiShuConfigAssetPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<FeiShuConfig>();
                AssetDatabase.CreateAsset(config, FeiShuConfigAssetPath);
                AssetDatabase.ImportAsset(FeiShuConfigAssetPath);
                AssetDatabase.SaveAssetIfDirty(config);
                return config;
            }
      
            return config;
        }
    }
    [System.Serializable]
    public class FeiShuFileSyncConfig
    {
        public enum ExtensionType
        {
            docx,
            pdf,
            xlsx,
            csv
        }
        public enum ExportType
        {
            doc,
            sheet,
            bitable,
            docx
        }
        public ExtensionType file_extension = ExtensionType.xlsx;
        public ExportType type = ExportType.sheet;
        public string token;
        public string sub_id;
        public string localFilePath;
    }
}