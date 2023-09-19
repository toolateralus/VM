using VM;
using VM.Terminal;

// This is a cli app.
await Terminal.Run(true, null!);

namespace VM.Terminal
{
    public static class Terminal
    {
        public static async Task Run(bool forPC, string? root)
        {
            while(true)
            {

                var computer = new Computer();
                computer.FS_ROOT = root!;
                
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

                    computer.Boot(id);
                    IO.WriteLine($"Computer booted on id {id}");
                }
                else 
                computer.Boot(0);
                
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
        }
    }
}
