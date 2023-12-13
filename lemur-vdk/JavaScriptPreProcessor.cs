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
        public static string InjectCommandLineArgs(string[] str_args, string jsCode)
        {
            const string ArgsArrayReplacement = "[/***/]";

            var index = jsCode.IndexOf(ArgsArrayReplacement);

            if (index != -1)
            {
                var args = jsCode.Substring(index, ArgsArrayReplacement.Length);

                var newArgs = $"[{string.Join("' ,'", str_args)}]";

                jsCode = jsCode.Replace(args, newArgs);
            }

            return jsCode;
        }
    }
}

