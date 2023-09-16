using System;
using System.Collections.Generic;

namespace VM
{
    public class IO 
    {

        public static void WriteLine(object? o) => OSTREAM?.Invoke(o);
        public static string? ReadLine() => ISTREAM?.Invoke();
        
        public static Action? CSTREAM { get; private set; }
        public static Func<string?>? ISTREAM { get; private set; }= Console.ReadLine;
        public static Action<object?>? OSTREAM { get; private set; } = Console.WriteLine;

        static Dictionary<long, Action> InputHandlerDisconnects = new();
        static Dictionary<long, Action> ClearHandlerDisconnects = new();
        static Dictionary<long, Action> OutputStreamDisconnects = new();
        public static long AddOutput(Action<object> value)
        {
            var id = Random.Shared.NextInt64();
            
            // setup disconnecter to prevent memory leaks
            // from closing / opening sources
            OutputStreamDisconnects[id] = 
            () =>
            {
                IO.OSTREAM -= value;
            };
                

            IO.OSTREAM += value;

            return id;
        }
        public static void RemoveOutput(long io_handle)
        {
            if (OutputStreamDisconnects.TryGetValue(io_handle, out var disconnect)){
                disconnect?.Invoke();
                OutputStreamDisconnects.Remove(io_handle);
            }
        }
        public static void RequestClear()
        {
            CSTREAM?.Invoke();
            Console.Clear();
        }
        public static long AddClearHandler(Action value)
        {

            var id = Random.Shared.NextInt64();
            
            // setup disconnecter to prevent memory leaks
            // from closing / opening sources
            ClearHandlerDisconnects[id] = 
            () =>
            {
                IO.CSTREAM -= value;
            };
                
            CSTREAM += value;


            return id;
        }
        public static void RemoveClearHandler(long io_handle_clear)
        {
           if (ClearHandlerDisconnects.TryGetValue(io_handle_clear, out var disconnect)){
                disconnect?.Invoke();
                ClearHandlerDisconnects.Remove(io_handle_clear);
            }
        }
        public static long AddInput(Func<string?> value)
        {
           var id = Random.Shared.NextInt64();
            
            // setup disconnecter to prevent memory leaks
            // from closing / opening sources
            InputHandlerDisconnects[id] = 
            () =>
            {
                IO.ISTREAM -= value;
            };
                

            IO.ISTREAM += value;

            return id;

        }
        public static void UnhookInput(long handle)
        {
            if (InputHandlerDisconnects.TryGetValue(handle, out var disconnect)){
                disconnect?.Invoke();
                InputHandlerDisconnects.Remove(handle);
            }
        }

    }

    
}



