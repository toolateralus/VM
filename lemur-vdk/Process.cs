using Lemur.GUI;
using System;

namespace Lemur {
    public record Process(Computer computer, UserWindow UI, string ID, string Class) {
        public Action? OnProcessTermination { get; internal set; }
        public readonly Computer computer = computer;

        /// <summary>
        /// Anything that wants to close a process MUST call this method to do so.
        /// </summary>
        internal void Terminate() {
            // destroy event handlers mostly.
            OnProcessTermination?.Invoke();

            // close visual UI.
            UI.Close();

            // dispose of the js execution context
            UI.Engine?.Dispose();

            // TODO: put in process manager.
            // remove the process and or type from process table.
            var procList = computer.ProcessManager.ProcessClassTable[Class];
            procList.Remove(this);

            if (procList.Count == 0)
                computer.ProcessManager.ProcessClassTable.Remove(Class);
            else computer.ProcessManager.ProcessClassTable[Class] = procList; // unnecessary? probably.

        }
    }
}
