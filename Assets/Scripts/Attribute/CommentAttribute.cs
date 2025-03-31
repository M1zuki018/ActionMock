using UnityEngine;
using System;

/// <summary>
/// 変数名をコメントに書き換えるカスタム属性
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public class CommentAttribute : PropertyAttribute
{
    public string Text { get; }

    public CommentAttribute(string text)
    {
        Text = text;
    }
}