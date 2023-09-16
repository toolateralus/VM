using System;
using System.Collections.Generic;
using System.Linq;
namespace VM.Lang; 
public delegate bool CommandAction(Computer computer, string commandLineInput, string[]? args);
public class CommandNode 
{
    public CommandAction Action;
    public List<CommandNode> Nodes = new();
    public CommandNode(CommandAction commandAction, params CommandNode[] nodes)
    {
        Action = commandAction;
        Nodes.AddRange(nodes);
    }
    public bool Try(Computer computer, Stack<string> words)
    {
        if (words.Count > 0)
        {
            var word = words.Peek();
            
            var remainder = words.ToArray();

            if (Action(computer, word, remainder))
                return true;

            foreach (var node in Nodes)
                if (node.Try(computer, words))
                {
                    words.Pop();
                    return true;
                }
        }

        return false; 
    }

}
