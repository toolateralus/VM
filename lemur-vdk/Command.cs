using Lemur.Types;
using System;

namespace Lemur.OS.Language
{
    public record Command(string Identifier, CommandAction Action, params string[] Info); // oh well!
    public delegate void CommandAction(SafeList<string> args);

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CommandAttribute(string identifier, params string[] Info) : Attribute
    {
        public readonly string[] Info = Info;
        public readonly string Identifier = identifier;
    }
}

