using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnConfuserEx.Protections.AntiTamper
{
    internal class NormalDeriver : IKeyDeriver
    {
        public uint[] DeriveKey(uint[] dst, uint[] src)
        {
            var ret = new uint[0x10];
            for (int i = 0; i < 0x10; i++)
            {
                switch (i % 3)
                {
                    case 0:
                        ret[i] = dst[i] ^ src[i];
                        break;
                    case 1:
                        ret[i] = dst[i] * src[i];
                        break;
                    case 2:
                        ret[i] = dst[i] + src[i];
                        break;
                }
            }
            return ret;
        }

    }
}
