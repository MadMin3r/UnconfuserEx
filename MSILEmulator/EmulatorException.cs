using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSILEmulator
{
    internal class EmulatorException : Exception
    {

        public EmulatorException()
        {
        }

        public EmulatorException(string message) : base(message)
        {
        }

        public EmulatorException(string message, Exception inner) : base(message, inner)
        {
        }

    }
}
