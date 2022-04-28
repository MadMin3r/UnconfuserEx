using dnlib.DotNet;
using dnlib.DotNet.Writer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnConfuserEx.Protections.Renamer;

namespace UnConfuserEx.Protections
{
    internal class UnicodeRemover : IProtection
    {
        public string Name => "Renamer";

        private ModuleDefMD? Module = null;
        private Dictionary<TypeDef, TypeInfo> NewTypeInfo = new();

        public bool IsPresent(ref ModuleDefMD module)
        {
            return true;
        }

        public bool Remove(ref ModuleDefMD module)
        {
            Module = module;

            RenameTypeDefs();
            RenameTypeRefs();
            RenameMemberRefs();

            return true;
        }

        private void RenameTypeDefs()
        {
            foreach (var type in Module!.GetTypes())
            {
                NewTypeInfo[type] = new TypeInfo(type);
            }
        }

        private void RenameTypeRefs()
        {
            foreach (var typeRef in Module!.GetTypeRefs())
            {
                if (Utils.IsInvalidName(typeRef.Name))
                {
                    throw new NotImplementedException();
                }
            }
        }

        private void RenameMemberRefs()
        {
            foreach (var methodDef in Module!.GetTypes().SelectMany(type => type.Methods))
            {
                foreach (var ov in methodDef.Overrides)
                {
                    RenameMemberRef(ov.MethodBody);
                    RenameMemberRef(ov.MethodDeclaration);
                }

                if (!methodDef.HasBody)
                {
                    continue;
                }

                foreach (var instr in methodDef.Body.Instructions)
                {
                    if (instr.Operand is MemberRef || instr.Operand is MethodSpec)
                    {
                        RenameMemberRef((IMemberRef)instr.Operand);
                    }
                }
            }
        }

        private void RenameMemberRef(IMemberRef memberRef)
        {
            if (Utils.IsInvalidName(memberRef.Name))
            {
                var declaringType = memberRef.DeclaringType.ResolveTypeDefThrow();
                var typeInfo = NewTypeInfo[declaringType];

                if (memberRef.IsField)
                {
                    memberRef.Name = typeInfo.FieldNames[memberRef.Name];
                }
                else if (memberRef.IsMethod)
                {
                    memberRef.Name = typeInfo.MethodNames[memberRef.Name];
                }
                else
                {
                    throw new NotImplementedException();
                }

            }
        }
    }
}
