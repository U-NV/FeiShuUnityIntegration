using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace U0UGames.FeiShu.Editor
{
    public class FeiShuFileUploadConfigEditorWindow
    {
        private FeiShuConfig _config;
        
        public void Init()
        {
            if (_config == null)
            {
                _config = FeiShuConfig.GetOrCreateConfig();
            }
        }


        private string SelectPathBtn(string oldPath, bool isFolderPath, bool relativeToRoot = true)
        {
            string selectPath = oldPath;

            var newPath = EditorGUILayout.TextField("相对文件路径:", selectPath);
            if(newPath != selectPath)
            {
                return newPath;
            }

            string openPath = null;
            if (string.IsNullOrEmpty(oldPath))
            {
                openPath = relativeToRoot? UnityPathUtility.RootFolderPath: Application.dataPath;
            }
            else
            {
               openPath = relativeToRoot?
                   UnityPathUtility.RootFolderPathToFullPath(oldPath):
                   UnityPathUtility.AssetPathToFullPath(oldPath);
            }
            if (GUILayout.Button("浏览",GUILayout.Width(50)))
            {
                if (isFolderPath)
                {
                    selectPath = EditorUtility.OpenFolderPanel("选择文件夹", openPath, null);
                }
                else
                {
                    selectPath = EditorUtility.OpenFilePanel("选择文件", Path.GetDirectoryName(openPath),"*");
                }
                if (!string.IsNullOrEmpty(selectPath))
                {
                    selectPath = relativeToRoot?
                        UnityPathUtility.FullPathToRootFolderPath(selectPath):
                        UnityPathUtility.FullPathToAssetPath(selectPath);
                }else{
                    selectPath = oldPath;
                }
            }

            return selectPath;
        }
        
        private void DrawConfigLine(FeiShuFileUploadConfig uploadConfig)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                uploadConfig.parent_node = EditorGUILayout.TextField("ParentNode:", uploadConfig.parent_node);
                EditorGUILayout.BeginHorizontal();
                // EditorGUILayout.LabelField("本地文件路径:", GUILayout.Width(120));
                uploadConfig.localFilePath = 
                    SelectPathBtn(uploadConfig.localFilePath, false,true);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private Vector2 _scrollPos;
        public void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.LabelField("当前所有上传配置文件：");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            var configList = _config.fileUploadConfigs;
            for (var index = 0; index < configList.Count; index++)
            {
                var generateConfig = _config.fileUploadConfigs[index];
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
                    var newConfig = new FeiShuFileUploadConfig();
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