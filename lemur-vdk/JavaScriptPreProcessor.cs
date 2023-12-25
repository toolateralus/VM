using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Lemur.OS.Language
{
    public class JavaScriptPreProcessor
    {
        public static string MangleNames(string input)
        {
            var nameMap = new Dictionary<string, string>();
            string pattern = @"\b(class|function|var|let|const)\s+(\w+)";
            var counter = 0;
            return Regex.Replace(input, pattern, m =>
            {
                string key = m.Groups[2].Value;
                if (!nameMap.ContainsKey(key))
                    nameMap[key] = $"x{counter++}";
                return $"{m.Groups[1].Value} {nameMap[key]}";
            });
        }
        public static string InjectCommandLineArgs(string[] inputArgs, string jsCode)
        {
            const string ArgsArrayReplacement = "[/***/]";

            string[] validArgs = new string[inputArgs.Length];

            Array.Copy(inputArgs, validArgs, validArgs.Length);

            for (int i = 0; i < validArgs.Length; i++)
            {
                string? arg = inputArgs[i];
                validArgs[i] = arg.Replace("\"", string.Empty).Replace("\'", string.Empty).Replace("`", string.Empty);
            }

            var index = jsCode.IndexOf(ArgsArrayReplacement);

            if (index != -1)
            {
                var args = jsCode.Substring(index, ArgsArrayReplacement.Length);

                var newArgs = "";

                if (validArgs.Length > 1) {
                    var args_InQuotes = string.Join("' ,'", validArgs);
                    newArgs = $"[{args_InQuotes}]";
                } 
                else
                {
                    newArgs = "['" + string.Join(" ,", validArgs) + "']"; 
                }

                jsCode = jsCode.Replace(args, newArgs);
            }

            return jsCode;
        }
    }
}

