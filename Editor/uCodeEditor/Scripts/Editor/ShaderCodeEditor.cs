using UnityEngine;
using UnityEditor;

namespace uCodeEditor
{

    public class ShaderCodeEditor
    {
        public string name { get; private set; }
        // public SerializedProperty value { get; private set; }
        // public SerializedProperty folded { get; private set; }

        CodeEditor editor_;
        Vector2 scrollPos_;
        Font font_;
        private Rect _lastRect;



        public ShaderCodeEditor(string name)
        {
            this.name = name;

            font_ = Resources.Load<Font>(Common.Editor.font);

            ColorUtility.TryParseHtmlString(Common.Color.background, out var bgColor);
            ColorUtility.TryParseHtmlString(Common.Color.color, out var color);

            var style = new GUIStyle(GUI.skin.textArea)
            {
                padding = new RectOffset(2, 2, 2, 2),
                font = font_,
                fontSize = Common.Editor.fontSize,
                wordWrap = Common.Editor.wordWrap
            };
            editor_ = new CodeEditor(style)
            {
                backgroundColor = bgColor,
                textColor = color,
                highlighter = ShaderHighlighter.Highlight
            };
        }

        public void Draw(SerializedProperty value, Rect rect, string controlName, GUIContent label = default)
        {
            if (label != default)
            {
                EditorGUI.PrefixLabel(rect, label);
                var labelWidth = EditorGUIUtility.labelWidth - EditorGUI.indentLevel * 15;
                rect.x += labelWidth;
                rect.width -= labelWidth;
            }
            var code = value.stringValue;
            var editedCode = editor_.Draw(code, rect, controlName);
            if (editedCode != code)
            {
                value.stringValue = editedCode;
            }
        }
    }

}