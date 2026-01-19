using System.IO;
using U0UGames.FeiShu.Editor;
using UnityEditor;
using UnityEngine;

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