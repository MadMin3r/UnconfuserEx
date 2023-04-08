using dnlib.DotNet.Emit;
using MSILEmulator;
using System;
using System.Collections.Generic;
using System.Linq;
using SRE = System.Reflection.Emit;

namespace UnConfuserEx.Protections.Constants
{
    internal class DynamicDecryptor : IDecryptor
    {
        private IList<Instruction> decryptInstructions;
        private ILMethod decryptMethod;
        private int[] arrayIndices;

        public DynamicDecryptor(IList<Instruction> decryptInstructions)
        {
            this.decryptInstructions = decryptInstructions;
            SetupDecryptMethod();
        }

        private void SetupDecryptMethod()
        {
            SortedSet<int> arrays = new();
            for (int i = 0; i < decryptInstructions.Count - 2; i++)
            {
                if (decryptInstructions[i + 2].OpCode == OpCodes.Ldelem_U4)
                {
                    if (decryptInstructions[i].IsLdloc())
                    {
                        arrays.Add(Utils.GetLoadLocalIndex(decryptInstructions[i]));
                    }
                }
            }
            arrayIndices = arrays.ToArray();
            if (arrayIndices.Length != 2)
            {
                throw new Exception("There should be two key arrays used in a dynamic derivation");
            }

            decryptMethod = new ILMethod(decryptInstructions);
        }

        public byte[] DecryptData(uint[] data, uint[] key)
        {
            uint[] temp = new uint[key.Length];
            byte[] ret = new byte[data.Length << 2];
            int s = 0, d = 0;

            decryptMethod.SetLocal(arrayIndices[0], key);
            decryptMethod.SetLocal(arrayIndices[1], temp);

            while (s < data.Length)
            {
                for (int j = 0; j < 16; j++)
                {
                    temp[j] = data[s + j];
                }

                decryptMethod.Emulate();

                for (int j = 0; j < 16; j++)
                {
                    uint t = temp[j];
                    ret[d++] = (byte)t;
                    ret[d++] = (byte)(t >> 8);
                    ret[d++] = (byte)(t >> 16);
                    ret[d++] = (byte)(t >> 24);
                    key[j] ^= t;
                }
                s += 16;
            }

            return ret;
        }

    }
}
