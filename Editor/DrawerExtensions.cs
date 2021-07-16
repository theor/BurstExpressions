using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;

namespace BurstExpressions.Editor
{
    public static class DrawerExtensions
    {
        public static object GetSerializedObject(this SerializedProperty property)
        {
            return property.serializedObject.GetChildObject(property.propertyPath);
        }

        private static readonly Regex MatchArrayElement = new Regex(@"^data\[(\d+)\]$");
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

                        Match elementMatch = MatchArrayElement.Match(pathNode);
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