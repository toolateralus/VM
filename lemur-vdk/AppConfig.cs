using System.Collections.Generic;

namespace Lemur {
    public class AppConfig {
        public string? @class { get; set; } // class name of wpf app in js.
        public string title { get; set; } = "no title";
        public string version { get; set; } = "0.0.0a";
        public string description { get; set; } = "An undescribed app.";
        public bool isWpf { get; set; } // is a wpf application?
        public bool terminal { get; set; } // is a terminal application?
        public string? entryPoint { get; set; } // app.js file path
        public string? frontEnd { get; set; } // .xaml.js file path
        public Dictionary<string, string[]> requires { get; set; } = []; // auto included requires
    }
}
