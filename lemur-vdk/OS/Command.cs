using System;

namespace lemur.OS
{
    public struct Command
    {
        public string id = "NULL";
        public Action<object[]?> Method;
        internal string[] infos = Array.Empty<string>();
        public Command(string id, Action<object[]?> method, params string[] infos)
        {
            this.id = id;
            Method = method;

            if (infos != null)
                this.infos = infos;
        }
    }
}

