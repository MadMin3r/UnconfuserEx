using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnConfuserEx.Protections.Renamer
{
    internal class TypeRenamer
    {
        private static TypeRenamer UnkRenamer = new("Type");
        private static TypeRenamer ClassRenamer = new("Class");
        private static TypeRenamer EnumRenamer = new("Enum");
        private static TypeRenamer DelegateRenamer = new("Delegate");
        private static TypeRenamer StructRenamer = new("Struct");
        private static TypeRenamer InterfaceRenamer = new("Interface");


        private int Count = 0;
        private string Prefix;

        public TypeRenamer(string prefix)
        {
            Prefix = prefix;
        }

        public string Generate()
        {
            return Prefix + Count++;
        }

        public static TypeRenamer GetRenamer(TypeDef type)
        {
            if (type.IsInterface)
            {
                return InterfaceRenamer;
            }
            else if (type.IsEnum)
            {
                return EnumRenamer;
            }
            else if (type.IsClass)
            {
                if (type.IsDelegate)
                {
                    return DelegateRenamer;
                }
                else if (type.IsValueType)
                {
                    return StructRenamer;
                }
                return ClassRenamer;
            }

            return UnkRenamer;
        }

    }
}
