using System;
using BurstExpressions.Runtime;
using BurstExpressions.Runtime.Parsing;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BurstExpressions.Editor
{
    [CustomPropertyDrawer(typeof(Formula))]
    public class FormulaDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int i = 2; // input + options
            var namedValues = property.FindPropertyRelative(nameof(Formula.NamedValues));
            i += namedValues.arraySize;
            var paramsProp = property.FindPropertyRelative(nameof(Formula.Params));
            i += paramsProp.arraySize;

            var formula = ((Formula)property.GetSerializedObject());
            if (!string.IsNullOrEmpty(formula._error))
                i++;
            if (formula.NamedValues != null)
                foreach (var formulaNamedValue in formula.NamedValues)
                {
                    if (!String.IsNullOrEmpty(formulaNamedValue.SubFormulaError))
                        i++;
                }

            return EditorGUIUtility.singleLineHeight * i;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var formula = (Formula)property.GetSerializedObject();

            void UpdateInstance(SerializedObject serializedObject, int subFormulaIndexToParse = -1)
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
                if (subFormulaIndexToParse != -1)
                {
                    var formulaNamedValue = formula.NamedValues[subFormulaIndexToParse];
                    formulaNamedValue.ParseSubFormula();
                    formula.NamedValues[subFormulaIndexToParse] = formulaNamedValue;
                    if (!String.IsNullOrEmpty(formulaNamedValue.SubFormulaError))
                        return;
                }
                formula.Init();
                formula._dirty = true;
                GUIUtility.ExitGUI();
            }

            EditorGUI.BeginProperty(position, label, property);
            var formulaObject = property.serializedObject;

            var rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            EditorGUI.BeginChangeCheck();

            var inputRect = rect;
            inputRect.width -= 15;
            EditorGUI.PropertyField(inputRect, property.FindPropertyRelative(nameof(Formula.Input)), label);
            inputRect.x += inputRect.width;
            inputRect.width = 15;
            if (GUI.Button(inputRect, new GUIContent("?", "Available functions and constants")))
                DocumentationGenerator.OpenDocumentation();
            rect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(rect, property.FindPropertyRelative(nameof(Formula.Options)));
            if (EditorGUI.EndChangeCheck())
            {
                UpdateInstance(formulaObject);
            }

            EditorGUI.indentLevel++;
            var e = formula._error;
            if (!string.IsNullOrEmpty(e))
            {
                rect.y += EditorGUIUtility.singleLineHeight;
                var r = rect;
                r.xMin += 16;
                EditorGUI.HelpBox(r, e, MessageType.Error);
            }

            var namedValues = property.FindPropertyRelative(nameof(Formula.NamedValues));
            var paramsProp = property.FindPropertyRelative(nameof(Formula.Params));
            for (int i = 0; i < paramsProp.arraySize; i++)
            {
                var elt = paramsProp.GetArrayElementAtIndex(i);
                rect.y += EditorGUIUtility.singleLineHeight;
                EditorGUI.SelectableLabel(rect, elt.stringValue);
                var r2 = rect;
                r2.x += EditorGUIUtility.fieldWidth;
                EditorGUI.LabelField(r2, "Parameter");
            }

            for (int i = 0; i < namedValues.arraySize; i++)
            {
                var elt = namedValues.GetArrayElementAtIndex(i);
                rect.y += EditorGUIUtility.singleLineHeight;
                var nameProp = elt.FindPropertyRelative(nameof(NamedValue.Name));
                var valProp = elt.FindPropertyRelative(nameof(NamedValue.Value));
                var flagProp = elt.FindPropertyRelative(nameof(NamedValue.IsSingleFloat));
                var valueRect = rect;
                var flagsRect = rect;
                var flagPRopWidth = 100;
                valueRect.xMax -= flagPRopWidth;
                flagsRect.xMin = flagsRect.xMax - flagPRopWidth;
                switch (flagProp.enumValueIndex)
                {
                    case (int)NamedValue.FormulaParamFlag.Vector3:
                        EditorGUI.BeginChangeCheck();
                        EditorGUI.PropertyField(valueRect, valProp, new GUIContent(nameProp.stringValue));
                        if (EditorGUI.EndChangeCheck()) UpdateInstance(formulaObject);
                        break;
                    case (int)NamedValue.FormulaParamFlag.Float:
                        EditorGUI.BeginChangeCheck();
                        EditorGUI.PropertyField(valueRect, valProp.FindPropertyRelative(nameof(Vector3.x)),
                            new GUIContent(nameProp.stringValue));
                        if (EditorGUI.EndChangeCheck()) UpdateInstance(formulaObject);
                        break;
                    case (int)NamedValue.FormulaParamFlag.Formula:
                        EditorGUI.BeginChangeCheck();
                        EditorGUI.PropertyField(valueRect, elt.FindPropertyRelative(nameof(NamedValue.SubFormula)),
                            new GUIContent(nameProp.stringValue));
                        if (EditorGUI.EndChangeCheck()) UpdateInstance(formulaObject, i);
                        break;
                }

                var w = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 1;
                EditorGUI.PropertyField(flagsRect, flagProp);
                EditorGUIUtility.labelWidth = w;

                if (!string.IsNullOrEmpty(formula.NamedValues[i].SubFormulaError))
                {
                    var r = rect;
                    r.y += EditorGUIUtility.singleLineHeight;
                    r.xMin += 16;
                    EditorGUI.HelpBox(r, formula.NamedValues[i].SubFormulaError, MessageType.Error);
                }


            }

            EditorGUI.indentLevel--;

            EditorGUI.EndProperty();
        }
    }
}