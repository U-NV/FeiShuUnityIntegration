using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace U0UGames.FeiShu.Editor
{
    public class FeiShuEditorWindow : EditorWindow
    {
        public const string FeiShuToolBarModeIndex = "FeiShuToolBarModeIndex";

        private readonly FeiShuFileConfigEditorWindow _fileConfigEditorWindow = new FeiShuFileConfigEditorWindow();
        private readonly FeiShuFileSyncEditorWindow _fileSyncEditorWindow = new FeiShuFileSyncEditorWindow();
        private readonly FeiShuFileUploadConfigEditorWindow _fileUploadConfigEditorWindow = new FeiShuFileUploadConfigEditorWindow();
        [MenuItem("工具/飞书")]
        static void Open()
        {
            FeiShuEditorWindow window = EditorWindow.GetWindow<FeiShuEditorWindow>("飞书");
            window.minSize = new Vector2(100, 100);
        }

        private int _mode = 0;
        private void Init()
        {
            _mode = EditorPrefs.GetInt(FeiShuToolBarModeIndex);
            _fileConfigEditorWindow.Init();
            _fileSyncEditorWindow.Init();
            _fileUploadConfigEditorWindow.Init();
        }
        private void OnEnable()
        {
            Init();
        }
        private string[] toolBarOption = new string[]
        {
            "应用配置","下载配置","上传配置","同步"
        };
        
        private void DrawFeiShuConfigEditorWindow(){
            var _config = FeiShuConfig.GetOrCreateConfig();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("飞书配置：");
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUILayout.BeginHorizontal();
                    _config.feiShuAppId = EditorGUILayout.TextField("飞书AppId:", _config.feiShuAppId);
                    // if (GUILayout.Button("复原",GUILayout.Width(50))){
                    //     _config.feiShuAppId = FeiShuConfig.FEISHU_APP_ID;
                    // }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    _config.feiShuAppSecret = EditorGUILayout.TextField("飞书AppSecret:", _config.feiShuAppSecret);
                    // if (GUILayout.Button("复原",GUILayout.Width(50))){
                    //     _config.feiShuAppSecret = FeiShuConfig.FEISHU_APP_SECRET;
                    // }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    _config._scope = EditorGUILayout.TextField("权限范围:", _config._scope);
                    if (GUILayout.Button("复原",GUILayout.Width(50))){
                        _config._scope = FeiShuConfig.SCOPE;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndVertical();
        }
        public void OnGUI()
        {
            _mode = GUILayout.Toolbar(_mode,toolBarOption);
            EditorPrefs.SetInt(FeiShuToolBarModeIndex, _mode);

            EditorGUILayout.BeginVertical();
            switch (_mode)
            {
                case 0:
                    DrawFeiShuConfigEditorWindow();
                    break;
                case 1:
                    _fileConfigEditorWindow.OnGUI();
                    break;
                case 2:
                    _fileUploadConfigEditorWindow.OnGUI();

                    break;
                case 3:
                    _fileSyncEditorWindow.OnGUI();

                    break;
                default:
                    break;
            }
            EditorGUILayout.EndVertical();
            
        }
    }
}