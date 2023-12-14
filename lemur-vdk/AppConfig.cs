using System.Collections.Generic;

namespace Lemur
{
    public class AppConfig
    {
        public string @class { get; set; } = "no class found";
        public string title { get; set; } = "no title";
        public string version { get; set; } = "0.0.0a";
        public string description { get; set; } = "An undescribed app";
        public bool isWpf { get; set; }
        public bool terminal { get; set; }
        public string? entryPoint { get; set; }
        public string? frontEnd { get; set; }
        public List<Dictionary<string, string[]>> requires { get; set; }  = [[]];
    } 
}
