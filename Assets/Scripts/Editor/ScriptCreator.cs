using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// スクリプトを作成補助用の静的クラス
/// </summary>
public static class ScriptCreator
{
    private static readonly string _templateFolderPath = "Assets/Script/ScriptTemplates"; // スクリプトテンプレートが置かれているフォルダ
    
    public static void CreateScript(string savePath, string scriptName, int templateIndex, string[] templates)
    {
        // フォルダパスが空でないかチェック
        if (string.IsNullOrEmpty(savePath))
        {
            Debug.LogError("フォルダパスが空です");
            return;
        }
        
        // スクリプト名を確保
        string path = Path.Combine(savePath, $"{scriptName}.cs");
        
        // スクリプトが既に存在するか確認
        if (File.Exists(path))
        {
            Debug.LogError($"Script {scriptName} は既に存在します！");
            return;
        }
        
        // 選択されたテンプレートを読み込む
        string selectedTemplate = templates[templateIndex];
        string templatePath = $"{_templateFolderPath}/{selectedTemplate}.txt";

        if (!File.Exists(templatePath))
        {
            Debug.LogError($"テンプレートファイル {selectedTemplate}.txt が見つかりません");
            return;
        }

        // テンプレートファイルの内容を読み込む
        string templateContent = File.ReadAllText(templatePath);

        // スクリプト内容をファイルに書き込む
        string scriptContent = templateContent.Replace("{ClassName}", scriptName); // テンプレート内の {ClassName} を置き換え

        // スクリプトファイルを作成
        File.WriteAllText(path, scriptContent);
        AssetDatabase.Refresh();
        Debug.Log($"Script {scriptName} created at {path}");
    }
}