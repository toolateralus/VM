using System.Net.WebSockets;
using VM;
using VM.FS;



while(true)
{

    System.Console.WriteLine("Enter a computer index, (probably just 0, unless you're multi-booting)");

    if(!uint.TryParse(Console.ReadLine()?.Trim(), out uint id))
    {
        Console.WriteLine("Invalid ID. Try again, it must be an integer ie 1 , or 2, not 0xF or 1.1");
        break;
    }

    var computer = new Computer();

    computer.Boot(id);

    System.Console.WriteLine($"Computer booted on id {id}");
    System.Console.WriteLine("Welcome to the command line. type help for info.");

    while (true)
    {
        var input = Console.ReadLine()!;
        computer.CommandLine.TryCommand(input);
    }
}