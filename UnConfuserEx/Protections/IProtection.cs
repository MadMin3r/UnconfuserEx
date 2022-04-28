using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnConfuserEx.Protections
{
    internal interface IProtection
    {
        string Name { get; }

        public bool IsPresent(ref ModuleDefMD module);

        public bool Remove(ref ModuleDefMD module);
    }
}
