using System;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Server;
using UnityEditor;
using UnityEngine;

namespace Eval.Editor
{
    [CustomPropertyDrawer(typeof(Formula))]
    public class FormulaDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int i = 2; // input + options
            // if (property.propertyType == SerializedPropertyType.ManagedReference)
            // {
            //     var formulaObject = new SerializedObject(property.objectReferenceValue);
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
            // }
            // else
            //     return base.GetPropertyHeight(property, label);

            return EditorGUIUtility.singleLineHeight * i;
        }

        private string _lastInput, _lastColoredInput;
        // Draw the property inside the given rect
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
            }

            EditorGUI.BeginProperty(position, label, property);
            var formulaObject = property.serializedObject;

            var rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            EditorGUI.BeginChangeCheck();

            // formula.Input = GuiExtensions.RichTextField(rect, formula.Input, _lastColoredInput);
            EditorGUI.PropertyField(rect, property.FindPropertyRelative(nameof(Formula.Input)), label);
            rect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(rect, property.FindPropertyRelative(nameof(Formula.Options)));
            if (EditorGUI.EndChangeCheck())// || _lastInput != formula.Input)
            {
                // Debug.Log("CHANGE");
                // try
                // {
                //   var node = Parser.Parse(formula.Input, out _);
                //   if (node != null)
                //   {
                //       var formatted = Formatter.Format(node, Formatter.FormatFlags.NoVariablePrefix);
                //       bool diff = formatted != formula.Input;
                //       formula.Input = formatted;
                //       _lastColoredInput = Formatter.Format(node,
                //           Formatter.FormatFlags.DifferentColorPerNode |
                //           Formatter.FormatFlags.NoVariablePrefix);
                //       
                //       if(diff)
                //       {
                //           var editor =
                //               typeof(EditorGUI)
                //                   .GetField("activeEditor", BindingFlags.Static | BindingFlags.NonPublic)
                //                   .GetValue(null) as TextEditor;
                //               //(TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                //           Debug.LogWarning($"DIFF {editor.text}");// + EditorGUIUtility.sele;
                //           editor.SelectAll();
                //           editor.ReplaceSelection(formula.Input);
                //           // _lastInput = formula.Input;
                //           // EditorGUIUtility.ExitGUI();
                //       }
                //   }
                //   else
                //       _lastColoredInput = null;
                //   // formula.Input = _lastInput ?? formula.Input;
                // }
                // catch(Exception ex)
                // {
                //     if (ex is ExitGUIException)
                //         throw;
                //     _lastColoredInput = null;
                // }
                // _lastInput = formula.Input;

                // property.FindPropertyRelative(nameof(Formula.Input)).stringValue = _lastInput;
                UpdateInstance(formulaObject);
                // formulaObject.Update();
                // Debug.Log(EditorJsonUtility.ToJson(formulaObject.targetObject));
            }

            EditorGUI.indentLevel++;
            var e = ((Formula)formula)._error;
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
                var nameProp = elt.FindPropertyRelative(nameof(FormulaParam.Name));
                var valProp = elt.FindPropertyRelative(nameof(FormulaParam.Value));
                var flagProp = elt.FindPropertyRelative(nameof(FormulaParam.IsSingleFloat));
                var valueRect = rect;
                var flagsRect = rect;
                var flagPRopWidth = 100;
                valueRect.xMax -= flagPRopWidth;
                flagsRect.xMin = flagsRect.xMax - flagPRopWidth;
                switch (flagProp.enumValueIndex)
                {
                    case (int)FormulaParam.FormulaParamFlag.Vector3:
                        EditorGUI.BeginChangeCheck();
                        EditorGUI.PropertyField(valueRect, valProp, new GUIContent(nameProp.stringValue));
                        if (EditorGUI.EndChangeCheck()) UpdateInstance(formulaObject);
                        break;
                    case (int)FormulaParam.FormulaParamFlag.Float:
                        EditorGUI.BeginChangeCheck();
                        EditorGUI.PropertyField(valueRect, valProp.FindPropertyRelative(nameof(Vector3.x)),
                            new GUIContent(nameProp.stringValue));
                        if (EditorGUI.EndChangeCheck()) UpdateInstance(formulaObject);
                        break;
                    case (int)FormulaParam.FormulaParamFlag.Formula:
                        EditorGUI.BeginChangeCheck();
                        EditorGUI.PropertyField(valueRect, elt.FindPropertyRelative(nameof(FormulaParam.SubFormula)),
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

    public static class DrawerExtensions
    {
        public static object GetSerializedObject(this SerializedProperty property)
        {
            return property.serializedObject.GetChildObject(property.propertyPath);
        }

        private static readonly Regex matchArrayElement = new Regex(@"^data\[(\d+)\]$");
        public static object GetChildObject(this SerializedObject serializedObject, string path)
        {
            object propertyObject = serializedObject.targetObject;

            if (path != "" && propertyObject != null)
            {
                string[] splitPath = path.Split('.');
                FieldInfo field = null;

                foreach (string pathNode in splitPath)
                {
                    if (field != null && field.FieldType.IsArray)
                    {
                        if (pathNode.Equals("Array"))
                            continue;

                        Match elementMatch = matchArrayElement.Match(pathNode);
                        int index;
                        if (elementMatch.Success && int.TryParse(elementMatch.Groups[1].Value, out index))
                        {
                            field = null;
                            object[] objectArray = (object[])propertyObject;
                            if (objectArray != null && index < objectArray.Length)
                                propertyObject = ((object[])propertyObject)[index];
                            else
                                return null;
                        }
                    }
                    else
                    {
                        field = propertyObject.GetType().GetField(pathNode, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        propertyObject = field.GetValue(propertyObject);
                    }
                }
            }

            return propertyObject;
        }
    }
}