using System;
using System.IO;
using VM.GUI;
using Newtonsoft.Json.Linq;

namespace VM.OS
{
    internal class OSConfigLoader
    {
        internal static JObject Load()
        {
            if (Runtime.GetResourcePath("config.json") is string AbsPath)
            {
                if (File.Exists(AbsPath))
                {
                    string json = File.ReadAllText(AbsPath);

                    try
                    {
                        return JObject.Parse(json);
                    }
                    catch (Exception ex)
                    {
                        Notifications.Now($"Error loading JSON: {ex.Message}");
                    }
                }
                else
                {
                    Notifications.Now("JSON file not found.");
                }
            }

            return null;
        }
    }
}
