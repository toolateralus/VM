using System.Runtime.Intrinsics.X86;
using VM;
using VM.Terminal;

using VirtualEnvironment = VM.Computer;

// This is a cli app.
uint ID = 0;
var env = Terminal.GetVirtualEnvironment(true, null!);

env.Boot(ID);

_ = Task.Run(()=>Terminal.RunRepl(env));

namespace VM.Terminal
{
    public static class Terminal
    {

        public static async Task RunRepl(Computer computer)
        {
            IO.WriteLine("Welcome to the command line. type help for info.");
                
            while (true)
            {
                var input = IO.ReadLine()!;
                
                if (input.Trim().ToLower() == "restart"){
                    break;
                }

                if (!computer.CommandLine.TryCommand(input)){
                    await computer.JavaScriptEngine.Execute(input);
                }
            }
        }
        public static Computer GetVirtualEnvironment(bool forPC, string? root)
        {
            while(true)
            {

                var computer = new Computer();
                computer.FS_ROOT = root!;
                computer.WORKING_DIR = root!;
                if (forPC) 
                {
                    IO.WriteLine("Enter a computer index, (probably just 0, unless you're multi-booting)");

                    if(!uint.TryParse(IO.ReadLine()?.Trim(), out uint id))
                    {
                        IO.WriteLine("Invalid ID. it must be an integer ie 1 , or 2, not 0xF or 1.1. press any key to continue");
                        IO.ReadLine();
                        Console.Clear();
                        continue;
                    }

                    return computer;
                    IO.WriteLine($"Computer booted on id {id}");
                }
                return computer;
            }
        }
    }

   
}
