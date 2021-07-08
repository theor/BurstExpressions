using UnityEditor;
using UnityEngine;

internal static class GuiExtensions
{
    private static GUIStyle _transparentTextFieldStyle;
    private static GUIStyle _richLabelStyle;
    private static bool _init;

    static void Init()
    {
        if(_init)
            return;
        _init = true;
        var transparent = new Color(1,1,0,0);
        _transparentTextFieldStyle = new GUIStyle(EditorStyles.textField) {richText = true,
            normal = {textColor = transparent},
            hover = {textColor = transparent},
            active = {textColor = transparent},
            focused = {textColor = transparent},
        };
        _richLabelStyle = new GUIStyle(EditorStyles.label){richText = true, padding = _transparentTextFieldStyle.padding};
    }

    public static string RichTextField(string value, string richDisplayValue)
    {
        Init();
        var result = EditorGUILayout.DelayedTextField(value, _transparentTextFieldStyle);
        var r = GUILayoutUtility.GetLastRect();
        EditorGUI.LabelField(r, richDisplayValue ?? value, _richLabelStyle);
        return result;
    }

    public static string RichTextField(Rect r, string value, string richDisplayValue)
    {
        Init();
        var result = EditorGUI.TextField(r, value, _transparentTextFieldStyle);
        EditorGUI.LabelField(r, richDisplayValue ?? value, _richLabelStyle);
        return result;
    }
}