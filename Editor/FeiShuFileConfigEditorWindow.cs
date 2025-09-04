using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FeiShu.Editor
{
    public class FeiShuFileConfigEditorWindow
    {
        private FeiShuConfig _config;
        
        public void Init()
        {
            if (_config == null)
            {
                _config = FeiShuConfig.GetOrCreateConfig();
            }
        }


        private string SelectPathBtn(string oldPath, string defaultText, bool isFolderPath, bool relativeToRoot = true)
        {
            string buttonText = null;
            string openPath = null;
            string selectPath = oldPath;

            if (string.IsNullOrEmpty(oldPath))
            {
                buttonText = defaultText;
                openPath = relativeToRoot? UnityPathUtility.RootFolderPath: Application.dataPath;
            }
            else
            {
               buttonText = oldPath;
               openPath = relativeToRoot?
                   UnityPathUtility.RootFolderPathToFullPath(oldPath):
                   UnityPathUtility.AssetPathToFullPath(oldPath);
            }
            if (GUILayout.Button(buttonText))
            {
                if (isFolderPath)
                {
                    selectPath = EditorUtility.OpenFolderPanel("选择数据文件夹", openPath, null);
                }
                else
                {
                    selectPath = EditorUtility.OpenFilePanel("选择数据文件", Path.GetDirectoryName(openPath), "xlsx");
                }
                if (!string.IsNullOrEmpty(selectPath))
                {
                    selectPath = relativeToRoot?
                        UnityPathUtility.FullPathToRootFolderPath(selectPath):
                        UnityPathUtility.FullPathToAssetPath(selectPath);
                }
            }

            return selectPath;
        }
        
        private void DrawConfigLine(FeiShuFileSyncConfig generateConfig)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                generateConfig.file_extension = (FeiShuFileSyncConfig.ExtensionType)EditorGUILayout.EnumPopup("文件类型:", generateConfig.file_extension);
                generateConfig.type = (FeiShuFileSyncConfig.ExportType)EditorGUILayout.EnumPopup("导出类型:", generateConfig.type);

                generateConfig.token = EditorGUILayout.TextField("token:", generateConfig.token);
                if (generateConfig.type == FeiShuFileSyncConfig.ExportType.sheet || generateConfig.type == FeiShuFileSyncConfig.ExportType.bitable)
                {
                    generateConfig.sub_id = EditorGUILayout.TextField("sheet名:", generateConfig.sub_id);
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("本地文件路径:", GUILayout.Width(120));
                generateConfig.localFilePath = 
                    SelectPathBtn(generateConfig.localFilePath, "选择本地文件", false,true);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private Vector2 _scrollPos;
        public void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("飞书配置：");
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    _config.feiShuAppId = EditorGUILayout.TextField("飞书AppId:", _config.feiShuAppId);
                    _config.feiShuAppSecret = EditorGUILayout.TextField("飞书AppSecret:", _config.feiShuAppSecret);
                    _config._scope = EditorGUILayout.TextField("权限范围:", _config._scope);
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.LabelField("当前所有配置文件：");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            var configList = _config.fileSyncConfigs;
            for (var index = 0; index < configList.Count; index++)
            {
                var generateConfig = _config.fileSyncConfigs[index];
                bool stopDrawing = false;

                EditorGUILayout.BeginHorizontal();
                DrawConfigLine(generateConfig);
                if (GUILayout.Button("-", GUILayout.Width(20))) 
                {
                    configList.RemoveAt(index);
                    stopDrawing = true;
                }
                EditorGUILayout.EndHorizontal();
                
                if (stopDrawing)
                {
                    break;
                }
            }

            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("+"))
                {
                    var newConfig = new FeiShuFileSyncConfig();
                    configList.Add(newConfig);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();

            if (GUI.changed)
            {
                // 标记对象为已修改
                EditorUtility.SetDirty(_config);
                // 保存已修改的 Asset
                AssetDatabase.SaveAssetIfDirty(_config);
            }
        }
    }
}