using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VM
{
    public class OS
    {
        private static OS current;
        public static OS Current => current;
        public OS()
        {
            if (current != null)
            {
                throw new InvalidOperationException("Cannot instantiate several instances of the operating system");
            }
            else current = this;
        }

    }
}
