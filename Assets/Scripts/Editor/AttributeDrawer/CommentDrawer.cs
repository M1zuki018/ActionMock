using UnityEditor;
using UnityEngine;

/// <summary>
/// 変数名の代わりに任意のコメントを表示する
/// </summary>
[CustomPropertyDrawer(typeof(CommentAttribute))]
public class CommentDrawer : PropertyDrawer
{
    private float _labelWidth;
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // CommentAttributeを取得
        CommentAttribute commentAttribute = (CommentAttribute)attribute;
        
        // ラベルの幅を取得
        _labelWidth = EditorGUIUtility.labelWidth; 
        
        // ラベルの領域とフィールドの領域に分割
        Rect labelRect = new Rect(position.x, position.y, _labelWidth, position.height);
        Rect fieldRect = new Rect(position.x + _labelWidth, position.y, position.width - _labelWidth, position.height);

        // ラベルとプロパティを描画
        EditorGUI.LabelField(labelRect, commentAttribute.Text);
        EditorGUI.PropertyField(fieldRect, property, GUIContent.none);
    }
    
    /// <summary>
    /// プロパティの高さを取得
    /// </summary>
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}