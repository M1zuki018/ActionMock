using UnityEditor;
using UnityEngine;
using System.Reflection;

/// <summary>
/// インスペクターにボタンを表示しメソッドを実行できるようにする
/// （ContextMenuの代わりなどに）
/// </summary>
[CustomEditor(typeof(MonoBehaviour), true)]
public class MethodButtonInspectorDrawer : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // デフォルトのプロパティを描画
        DrawButtonsForMethods(); // ボタンを描画
    }

    /// <summary>
    /// メソッドを探してボタンを描画
    /// </summary>
    private void DrawButtonsForMethods()
    {
        // 現在のターゲットオブジェクトを取得
        MonoBehaviour targetObject = (MonoBehaviour)target;
        
        // メソッドを取得
        MethodInfo[] methods = targetObject.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        // [Button] 属性が付いたメソッドを探す
        foreach (MethodInfo method in methods)
        {
            MethodButtonInspectorAttribute methodButtonInspectorAttribute = method.GetCustomAttribute<MethodButtonInspectorAttribute>();
            if (methodButtonInspectorAttribute != null)
            {
                string buttonText = string.IsNullOrEmpty(methodButtonInspectorAttribute.Label) ? method.Name : methodButtonInspectorAttribute.Label;

                // ボタンを描画
                if (GUILayout.Button("MethodTest: " + buttonText))
                {
                    // メソッドを呼び出す
                    method.Invoke(targetObject, null);
                }
            }
        }
    }
}