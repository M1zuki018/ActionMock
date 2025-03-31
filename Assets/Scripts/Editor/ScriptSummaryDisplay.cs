using UnityEditor;
using UnityEngine;

/// <summary>
/// プロジェクトウィンドウにSummaryの内容を表示する
/// </summary>
[InitializeOnLoad]
public class ScriptSummaryDisplay
{
    static ScriptSummaryDisplay()
    {
        EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
    }

    private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
    {
        string assetPath = AssetDatabase.GUIDToAssetPath(guid);

        // スクリプトファイルのみ処理
        if (!assetPath.EndsWith(".cs"))
            return;
        
        // プロジェクトウィンドウがリストビューで表示されているかチェック
        if (selectionRect.width < 100) // 横幅が狭ければアイコン表示とみなす
        {
            // アイコン形式の場合、ラベルは表示しない
            return;
        }
        
        // Summary コメントを取得
        string summary = ScriptSummaryExtractor.GetFormattedSummary(assetPath);
        if (string.IsNullOrEmpty(summary))
            return;

        // ファイル名の描画幅を計算
        GUIStyle style = EditorStyles.label;
        string fileName = System.IO.Path.GetFileName(assetPath);
        Vector2 fileNameSize = style.CalcSize(new GUIContent(fileName));

        // ファイル名の右側にラベルを表示
        Rect labelRect = new Rect(
            selectionRect.x + fileNameSize.x + 10, // ファイル名の右側に10pxの余白を追加
            selectionRect.y,
            400, // ラベルの最大幅
            selectionRect.height
        );

        GUI.Label(labelRect, summary, style);
    }
}