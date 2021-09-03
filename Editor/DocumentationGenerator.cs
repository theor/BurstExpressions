using System;
using System.IO;
using System.Linq;
using System.Text;
using BurstExpressions.Runtime.Parsing;
using BurstExpressions.Runtime.Runtime;
using UnityEditor;
using UnityEngine;

namespace BurstExpressions.Editor
{
    public static class DocumentationGenerator
    {
        [MenuItem("BurstExpressions/Generate documentation")]
        public static void Generate()
        {
            string path = "../Doc/functions.md";

            StringBuilder text = new StringBuilder();
            text.AppendLine(@"Function | Parameters | Description
-------- | ---------- | -----------");
            foreach (var overloads in Functions.AllFunctions.OrderBy(x => x.Key))
            {
                bool writeFunctionOnce = true;
                string lastDesc = null;
                foreach (var overload in overloads.OrderBy(x => x.ArgumentCount))
                {
                    if (overload.OpCode.GetAttributeOfType<OpHideInDocumentation>() != null)
                        continue;
                    var @params = String.Join(", ",
                        Enumerable.Repeat(0, overload.ArgumentCount).Select((_, i) => (char)('a' + i)));
                    var signature = $@"{overloads.Key.ToLowerInvariant()}({@params})";
                    text.AppendLine($"*{(writeFunctionOnce ? overloads.Key : "")}* | `{(overload.Signature ?? signature)}` | {lastDesc = overload.HelpText ?? lastDesc}");
                    writeFunctionOnce = false;
                }
            }

            text.AppendLine();
            text.AppendLine(@"Constant | Value
-------- | ----------");

            foreach (var keyValuePair in Constants.AllConstants().OrderBy(x => x.Key))
            {
                text.AppendLine($@"{keyValuePair.Key} | {keyValuePair.Value.Item2 ?? keyValuePair.Value.Item1.ToString()}");
            }


            Debug.Log(text);
            File.WriteAllText(path, text.ToString());
        }

        public static void OpenDocumentation()
        {
            Application.OpenURL("https://github.com/theor/BurstExpressions/tree/main/Doc/functions.md");
        }
    }
}