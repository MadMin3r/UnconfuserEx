using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnConfuserEx.Protections.AntiTamper
{
    internal interface IKeyDeriver
    {

        public uint[] DeriveKey(uint[] dst, uint[] src);

    }
}
