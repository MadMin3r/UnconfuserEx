using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSILEmulator
{
    public class Context
    {
        public Dictionary<int, object> Args = new();
        public Dictionary<int, object> Locals = new();
        public Stack<object> Stack = new();
    }
}
