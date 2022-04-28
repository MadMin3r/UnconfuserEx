using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnConfuserEx.Protections.Resources
{
    internal interface IDecryptor
    {
        public byte[] Decrypt(uint[] key, uint[] data);

    }
}
