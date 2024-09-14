using Newtonsoft.Json;
using System;

namespace Lemur.OS.Language {
    public static class JavaScriptPreProcessor {
        public static string InjectCommandLineArgs(string[] inputArgs, string jsCode) {
            const string ArgsArrayReplacement = "[/***/]";
            ArgumentNullException.ThrowIfNull(jsCode);
            if (jsCode.Contains(ArgsArrayReplacement)) {
                var argsJson = JsonConvert.SerializeObject(inputArgs, Formatting.Indented);
                jsCode = jsCode.Replace(ArgsArrayReplacement, argsJson);
            }
            return jsCode;
        }
    }
}

