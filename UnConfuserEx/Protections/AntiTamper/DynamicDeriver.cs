using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MSILEmulator;

namespace UnConfuserEx.Protections.AntiTamper
{
    internal class DynamicDeriver : IKeyDeriver
    {
        private IList<Instruction> derivation;

        public DynamicDeriver(IList<Instruction> derivation)
        {
            this.derivation = derivation;
        }

        public uint[] DeriveKey(uint[] dst, uint[] src)
        {
            SortedSet<int> arrays = new();
            for (int i = 0; i < derivation.Count - 2; i++)
            {
                if (derivation[i].OpCode == OpCodes.Ldloc_S
                    && derivation[i + 2].OpCode == OpCodes.Ldelem_U4)
                {
                    arrays.Add(((Local)derivation[i].Operand).Index);
                }
            }
            int[] arrayIndices = arrays.ToArray();
            if (arrayIndices.Length != 2)
            {
                throw new Exception("There should be two key arrays used in a dynamic derivation");
            }

            var ilMethod = new ILMethod(derivation);

            ilMethod.SetLocal(arrayIndices[0], dst);
            ilMethod.SetLocal(arrayIndices[1], src);

            ilMethod.Emulate();

            return dst;
        }

    }
}
