using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FeiShu.Editor
{
    public class FeiShuEditorWindow : EditorWindow
    {
        public const string FeiShuToolBarModeIndex = "FeiShuToolBarModeIndex";

        private readonly FeiShuFileConfigEditorWindow _fileConfigEditorWindow = new FeiShuFileConfigEditorWindow();
        private readonly FeiShuFileSyncEditorWindow _fileSyncEditorWindow = new FeiShuFileSyncEditorWindow();
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
        }
        private void OnEnable()
        {
            Init();
        }
        private string[] toolBarOption = new string[]
        {
            "配置","同步"
        };
         
        public void OnGUI()
        {
            _mode = GUILayout.Toolbar(_mode,toolBarOption);
            EditorPrefs.SetInt(FeiShuToolBarModeIndex, _mode);

            EditorGUILayout.BeginVertical();
            switch (_mode)
            {
                case 0:
                    _fileConfigEditorWindow.OnGUI();
                    break;
                case 1:
                    _fileSyncEditorWindow.OnGUI();
                    break;
                default:
                    break;
            }
            EditorGUILayout.EndVertical();
            
        }
    }
}