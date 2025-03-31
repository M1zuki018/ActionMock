using UnityEditor;
using UnityEngine;

/// <summary>
/// ScriptableObjectをInspector上で展開して編集できるようにする
/// </summary>
[CustomPropertyDrawer(typeof(ExpandableSOAttribute))]
public class ExpandableSODrawer : PropertyDrawer
{
    private SerializedObject _cachedSerializedObject;
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // ScriptableObject のオブジェクトフィールド
        Rect objectFieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.objectReferenceValue = EditorGUI.ObjectField(objectFieldRect, label, property.objectReferenceValue, fieldInfo.FieldType, false);

        // オブジェクトが存在する場合、詳細を表示
        if (property.objectReferenceValue != null)
        {
            CacheSerializedObject(property);

            _cachedSerializedObject.Update(); // Inspector更新
            SerializedProperty iterator = _cachedSerializedObject.GetIterator();
            iterator.NextVisible(true); // スクリプトの表示をスキップする

            // 描画する領域の枠を描画
            Rect boxRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, position.width,
                GetPropertyHeight(property, label) - EditorGUIUtility.singleLineHeight - 4);
            GUI.Box(boxRect, GUIContent.none, EditorStyles.helpBox);

            // フィールドの描画
            float yOffset = position.y + EditorGUIUtility.singleLineHeight + 6;
            EditorGUI.indentLevel++; // インデントを増やす

            DrawFields(position, iterator, yOffset);

            EditorGUI.indentLevel--; // インデントを戻す
            _cachedSerializedObject.ApplyModifiedProperties(); // 変更を適用
        }

        EditorGUI.EndProperty();
    }

    /// <summary>
    /// フィールドを描画する
    /// </summary>
    private static void DrawFields(Rect position, SerializedProperty iterator, float yOffset)
    {
        while (iterator.NextVisible(false))
        {
            float propHeight = EditorGUI.GetPropertyHeight(iterator, true);
            Rect propRect = new Rect(position.x + 6, yOffset, position.width - 12, propHeight);
            EditorGUI.PropertyField(propRect, iterator, true);
            yOffset += propHeight + 2; // 次のフィールドのためのインデントを追加
        }
    }

    /// <summary>
    /// SerializedObjectをキャッシュして再利用できるようにする
    /// </summary>
    private void CacheSerializedObject(SerializedProperty property)
    {
        if (_cachedSerializedObject?.targetObject != property.objectReferenceValue)
        {
            _cachedSerializedObject = new SerializedObject(property.objectReferenceValue);
        }
    }
    
    /// <summary>
    /// スクリプタブルオブジェクトの高さを計算
    /// </summary>
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.objectReferenceValue == null) return EditorGUIUtility.singleLineHeight;

        CacheSerializedObject(property); // キャッシュを利用
        
        float height = EditorGUIUtility.singleLineHeight + 4; // 上のオブジェクトフィールドの高さ
        
        _cachedSerializedObject.Update(); // inspector更新
        SerializedProperty iterator = _cachedSerializedObject.GetIterator();
        iterator.NextVisible(true); // スクリプトの表示をスキップする
        
        // オブジェクトのフィールドを順番に高さを計算
        while (iterator.NextVisible(false))
        {
            height += EditorGUI.GetPropertyHeight(iterator, true) + 2; // 各フィールドの高さ+余白
        }
        
        return height + 6; // 余白を追加
    }
}