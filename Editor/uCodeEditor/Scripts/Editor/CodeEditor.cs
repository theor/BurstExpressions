using System;
using UnityEngine;
using UnityEditor;

namespace uCodeEditor
{

    public class CodeEditor
    {
        private GUIStyle _backStyle;
        private GUIStyle _foreStyle;
        public Color backgroundColor { get; set; }
        public Color textColor { get; set; }

        string cachedCode { get; set; }
        string cachedHighlightedCode { get; set; }
        public Func<string, string> highlighter { get; set; }

        public CodeEditor(GUIStyle style)
        {
            backgroundColor = Color.black;
            textColor = Color.white;
            highlighter = code => code;

            _backStyle = new GUIStyle(style)
            {
                normal =
                {
                    textColor = Color.clear
                },
                hover =
                {
                    textColor = Color.clear
                },
                active =
                {
                    textColor = Color.clear
                },
                focused =
                {
                    textColor = Color.clear
                }
            };
            _foreStyle = new GUIStyle(style)
            {
                richText = true,
                normal =
                {
                    textColor = textColor
                },
                hover =
                {
                    textColor = textColor
                },
                active =
                {
                    textColor = textColor
                },
                focused =
                {
                    textColor = textColor
                }
            };
        }

        struct WithoutSelectAllScope : IDisposable
        {
            private bool _preventSelection;
            private Color _oldCursorColor;

            public static WithoutSelectAllScope Scope()
            {
                var s = new WithoutSelectAllScope
                {
                    _preventSelection = (Event.current.type == EventType.MouseDown),

                    _oldCursorColor = GUI.skin.settings.cursorColor,
                };

                if (s._preventSelection)
                    GUI.skin.settings.cursorColor = new Color(0, 0, 0, 0);
                return s;
            }


            public void Dispose()
            {
                if (_preventSelection)
                    GUI.skin.settings.cursorColor = _oldCursorColor;
            }
        }

        public string Draw(string code, Rect rect, string controlName)
        {
            var preBackgroundColor = GUI.backgroundColor;
            var preColor = GUI.color;

            GUI.backgroundColor = backgroundColor;
            GUI.color = textColor;


            GUI.SetNextControlName(controlName);

            // IMPORTANT: 
            // Sadly, we cannot use TextEditor with (EditorGUILayout|EditorGUI).TextArea()... X(
            // And GUILayout.TextArea() cannot handle TAB key... ;_;
            // GUI.TextArea needs a lot of tasks to implement absic functions... T_T
            string editedCode;

            editedCode = EditorGUI.TextArea(rect, code, _backStyle);

            // So, this does not work...
            var editor = GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) as TextEditor;
            CheckEvents(editor);

            code = editedCode;

            if (string.IsNullOrEmpty(cachedHighlightedCode) || (cachedCode != code))
            {
                cachedCode = code;
                cachedHighlightedCode = highlighter(code);
            }

            GUI.backgroundColor = Color.clear;

            EditorGUI.TextArea(rect, cachedHighlightedCode, _foreStyle);

            GUI.backgroundColor = preBackgroundColor;
            GUI.color = preColor;

            return code;
        }

        void CheckEvents(TextEditor editor)
        {
            // ...
        }
    }

}