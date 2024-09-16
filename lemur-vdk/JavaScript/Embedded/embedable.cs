using System;

namespace Lemur.JS.Embedded {
    public class embedable(Computer computer)
    {
        private readonly WeakReference<Computer> computer = new(computer);
        public Computer GetComputer()
        {
            if (computer.TryGetTarget(out var comp))
                return comp;
            throw new ComputerNotFoundException("Failed to fetch computer in an embedded resource");
        }
    }
}