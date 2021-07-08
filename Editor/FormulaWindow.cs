using Eval.Runtime;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Eval.Editor
{
    class FormulaWindow : EditorWindow
    {
        [MenuItem("Formula/Test window")]
        public static void Open()
        {
            GetWindow<FormulaWindow>().Show();
        }

        [SerializeField]
        private Formula Formula;

        private UnityEditor.Editor _e;
        private EvalGraph _evalgraph;
        private Vector3 ParamA;
        private Vector3 Result;
        private bool _dirty;
        private string _text;

        private void OnEnable()
        {
            _e = UnityEditor.Editor.CreateEditor(this);
            if (Formula == null)
                Formula = new Formula();
            Formula.SetParameters("a");
            _dirty = true;
            _text = "test";
        }

        string Format(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            var oldValue = input.Substring(0,1);
            return input.Replace(oldValue, $"<color=#ff0000>{oldValue}</color>");
        }

        private void OnGUI()
        {
            EditorGUIUtility.wideMode = true;

            // _text = GuiExtensions.RichTextField(_text, Format(_text));
            // EditorGUILayout.LabelField(Formula.Input);
            // _text = EditorGUILayout.TextField(_text, transparentTextFieldStyle);
            // var r = GUILayoutUtility.GetLastRect();
            // var s = new GUIStyle(EditorStyles.textField) {richText = true};
            // EditorGUILayout.TextField(Format(_text), s);
            // EditorGUI.LabelField(r, Format(_text), new GUIStyle(EditorStyles.label){richText = true, padding = s.padding});
            
            _e.OnInspectorGUI();
            // Formula.LiveEdit(ref _evalgraph, (graph, newGraph) => _dirty = true);
            var c = Formula.Content;
            EditorGUILayout.LabelField("OpCodes");
            if(c != null)
                for (var i = 0; i < c.Length; i++)
                {
                    var node = c[i];
                    EditorGUILayout.LabelField(i.ToString(), node.ToString() ?? "null");
                }
            EditorGUILayout.LabelField("Evaluation Tester");
            
            EditorGUI.BeginChangeCheck();
            ParamA = EditorGUILayout.Vector3Field("Parameter A", ParamA);
            if (EditorGUI.EndChangeCheck())
                _dirty = true;
            if(_dirty && _evalgraph.Length > 0)
            {
                EvalState.Run(_evalgraph, (float3)ParamA, out var res);
                Result = res;
                _dirty = false;
            }
            EditorGUILayout.LabelField("Result", Result.ToString("F4"));
        }

        private void OnDestroy()
        {
            _evalgraph.Dispose();
        }
    }
}