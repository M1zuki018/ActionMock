using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

/// <summary>
/// テンプレートを選択しスクリプトを作成するウィンドウを提供するエディター拡張
/// </summary>
public class ScriptCreationWindow : EditorWindow
{
    private string _scriptName = "NewScript"; // スクリプトの名前
    private string _savePath = "Assets"; // 保存パス
    private int _templateIndex = 0; // テンプレートのインデックス
    private string[] _templates = new string[0]; // テンプレートの配列
    private readonly string _templateFolderPath = "Assets/Script/ScriptTemplates";
    
    [MenuItem("Creeping Red/Script Creation Window")]
    public static void ShowWindow()
    {
        // ウィンドウを表示
        ScriptCreationWindow window = EditorWindow.GetWindow<ScriptCreationWindow>("Script Creation");
        window.Show();
    }

    private void OnEnable()
    {
        LoadTemplates(); // スクリプトのテンプレートをロード
    }

    private void OnGUI()
    {
        // スクリプト名を入力するフィールド
        _scriptName = EditorGUILayout.TextField("Script Name", _scriptName);
        
        // 保存フォルダ選択フィールド
        GUILayout.Label("Select Save Folder", EditorStyles.boldLabel);
        _savePath = EditorGUILayout.TextField("Folder Path", _savePath);
        
        // パス選択ボタン
        if (GUILayout.Button("Select Folder"))
        {
            string folderPath = EditorUtility.OpenFolderPanel("Select Folder", Application.dataPath, "");

            if (!string.IsNullOrEmpty(folderPath))
            {
                if (folderPath.StartsWith(Application.dataPath))
                {
                    folderPath = "Assets" + folderPath.Substring(Application.dataPath.Length);
                }
                _savePath = folderPath;
            }
        }
        
        // タブのレイアウト
        EditorGUILayout.BeginHorizontal();

        ShowScriptCreationGUI();

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// スクリプトのテンプレートから新しいスクリプトを作成するGUI
    /// </summary>
    private void ShowScriptCreationGUI()
    {
        GUILayout.Label("Create a New Script", EditorStyles.boldLabel);

        // テンプレートを選ぶドロップダウンメニュー
        if (_templates != null && _templates.Length > 0)
        {
            // テンプレートが存在したらポップアップに含めて表示する
            _templateIndex = EditorGUILayout.Popup("Template", _templateIndex, _templates);
        }
        else
        {
            // テンプレートが存在しなかったらメッセージを出す
            EditorGUILayout.HelpBox("'ScriptTemplates' フォルダーにテンプレートが見つかりません！", MessageType.Warning);
        }
        
        // スクリプト作成ボタン
        if (GUILayout.Button("Create Script"))
        {
            ScriptCreator.CreateScript(_savePath, _scriptName, _templateIndex, _templates);
        }
    }

    /// <summary>
    /// テンプレートの格納フォルダからテンプレートをロードする
    /// </summary>
    private void LoadTemplates()
    {
        if (Directory.Exists(_templateFolderPath))
        {
            // フォルダ内のtxtファイルをすべて取得
            string[] templateFiles = Directory.GetFiles(_templateFolderPath, "*.txt");
            
            // テンプレート名をファイル名（拡張子なし）でリスト化
            _templates = templateFiles
                .Select(Path.GetFileNameWithoutExtension)
                .ToArray();
        }
        else
        {
            _templates = new string[] { }; // フォルダがない場合は空のリスト
        }
    }
}