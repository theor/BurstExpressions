using UnityEngine;
using UnityEditor;

namespace uCodeEditor
{

    public class ShaderCodeEditor
    {
        public string name { get; private set; }
        public SerializedProperty value { get; private set; }
        public SerializedProperty folded { get; private set; }

        CodeEditor editor_;
        Vector2 scrollPos_;
        Font font_;
        private Rect _lastRect;

        public string code
        {
            get { return value != null ? value.stringValue : ""; }
            private set { this.value.stringValue = value; }
        }

        public ShaderCodeEditor(string name, SerializedProperty value, SerializedProperty folded = null)
        {
            this.name = name;
            this.value = value;
            this.folded = folded;

            font_ = Resources.Load<Font>(Common.Editor.font);

            Color color, bgColor;
            ColorUtility.TryParseHtmlString(Common.Color.background, out bgColor);
            ColorUtility.TryParseHtmlString(Common.Color.color, out color);

            editor_ = new CodeEditor(name);
            editor_.backgroundColor = bgColor;
            editor_.textColor = color;
            editor_.highlighter = ShaderHighlighter.Highlight;
        }

        public void Draw(Rect rect)
        {
            // if (Event.current.type == EventType.Repaint)
            // _lastRect = rect;

            // Debug.Log($"{Event.current.type} {rect}");
            // var viewRe2ct = new Rect(0, 0, rect.width - 20, viewHeight);
            // scrollPos_ = GUI.BeginScrollView(rect, scrollPos_, viewRect);
            {
                var style = new GUIStyle(GUI.skin.textArea);
                style.padding = new RectOffset(6, 6, 6, 6);
                style.font = font_;
                style.fontSize = Common.Editor.fontSize;
                style.wordWrap = Common.Editor.wordWrap;

                var editedCode = editor_.Draw(code, style, rect);
                if (editedCode != code)
                {
                    code = editedCode;
                }
            }
            // GUI.EndScrollView();
        }
        public void Draw()
        {
            if (folded != null)
            {
                var preFolded = folded.boolValue;
                folded.boolValue = Utils.Foldout(name, folded.boolValue);

                if (!folded.boolValue)
                {
                    if (preFolded)
                    {
                        GUI.FocusControl("");
                    }
                    return;
                }

                if (!preFolded)
                {
                    GUI.FocusControl(name);
                }
            }

            var minHeight = GUILayout.MinHeight(Common.Editor.minHeight);
            var maxHeight = GUILayout.MaxHeight(Screen.height);
            scrollPos_ = EditorGUILayout.BeginScrollView(scrollPos_, minHeight, maxHeight);
            {
                var style = new GUIStyle(GUI.skin.textArea);
                style.padding = new RectOffset(6, 6, 6, 6);
                style.font = font_;
                style.fontSize = Common.Editor.fontSize;
                style.wordWrap = Common.Editor.wordWrap;

                var editedCode = editor_.Draw(code, style);
                if (editedCode != code)
                {
                    code = editedCode;
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
        }
    }

}