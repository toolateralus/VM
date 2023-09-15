using System;
using System.Collections.Generic;
using System.Linq;

namespace VM.Lang;
public struct Command
{
    public string id = "NULL";
    public Action<object[]?> Method;
    public string[] infos = Array.Empty<string>();
    public Command(string id, Action<object[]?> method, params string[]? infos)
    {
        this.id = id;
        this.Method = method;

        if (infos != null)
        {
            this.infos = this.infos.Concat(infos).ToArray();
        }

    }
}