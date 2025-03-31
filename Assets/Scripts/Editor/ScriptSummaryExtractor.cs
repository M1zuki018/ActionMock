using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

/// <summary>
/// Summary コメントを取得するクラス
/// </summary>
public static class ScriptSummaryExtractor
{
    // スクリプトから <summary> コメントを取得
    public static string GetFormattedSummary(string scriptPath)
    {
        if (!File.Exists(scriptPath))
            return string.Empty;

        string scriptContent = File.ReadAllText(scriptPath);
        // <summary> コメントを正規表現で抽出
        Match match = Regex.Match(scriptContent, @"<summary>([\s\S]*?)</summary>", RegexOptions.Singleline);
        if (match.Success)
        {
            // コメント内の改行を空白に置き換え、/// を削除
            string rawSummary = match.Groups[1].Value;
            string formattedSummary = Regex.Replace(rawSummary, @"\r\n|\r|\n", " ").Trim(); 
            formattedSummary = formattedSummary.Replace("///", "").Trim();
            return formattedSummary;
        }
        return string.Empty;
    }
}