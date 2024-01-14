using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Lemur.OS.Language
{
    public static class JavaScriptPreProcessor
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
            ArgumentNullException.ThrowIfNull(jsCode);
            if (jsCode.Contains(ArgsArrayReplacement))
            {
                var argsJson = JsonConvert.SerializeObject(inputArgs, Formatting.Indented);
                jsCode = jsCode.Replace(ArgsArrayReplacement, argsJson);
            }
            return jsCode;
        }
    }
}

