using Lemur.JS.Embedded;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Documents;

namespace Lemur {

    [System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    sealed class ApiDocAttribute(string description) : Attribute {
        public string Description { get;  } = description;
    }


    public static class ApiDocParser {
        public static Dictionary<string, List<string>> Parse() {
            var infos = new Dictionary<string, List<string>>();
            Assembly assembly = Assembly.GetExecutingAssembly();
            Type[] types = assembly.GetTypes();
            foreach (Type type in types.Where(t => t.IsSubclassOf(typeof(embedable)))) {
                var sanitized = type.Name.Replace("_t", "");
                sanitized = char.ToUpper(sanitized[0], System.Globalization.CultureInfo.CurrentCulture) + sanitized[1..sanitized.Length];
                var classInfo = sanitized;
                var methodInfos = new List<string>();
                MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (MethodInfo method in methods) {
                    var attributes = method.GetCustomAttributes(typeof(ApiDocAttribute), false);
                    foreach (ApiDocAttribute attribute in attributes) {
                        var methodSignature = $"{method.ReturnType.Name} {method.Name}(";
                        ParameterInfo[] parameters = method.GetParameters();
                        for (int i = 0; i < parameters.Length; i++) {
                            methodSignature += $"{parameters[i].ParameterType.Name} {parameters[i].Name}";
                            if (i < parameters.Length - 1) {
                                methodSignature += ", ";
                            }
                        }
                        methodSignature += ")";
                        methodInfos.Add($"{methodSignature}:\n{attribute.Description}");
                    }
                }
                infos.Add(classInfo, methodInfos);
            }
            return infos;
        }

    }
}
