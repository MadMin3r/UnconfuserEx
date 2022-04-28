using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnConfuserEx.Protections.Renamer
{
    internal class TypeInfo
    {
        private readonly TypeDef Type;
        public string OriginalName;
        public string NewName;

        public Dictionary<UTF8String, string> FieldNames = new();
        public Dictionary<UTF8String, string> MethodNames = new();
        public Dictionary<UTF8String, string> PropNames = new();
        public Dictionary<UTF8String, string> GenericParamNames = new();

        public TypeInfo(TypeDef type)
        {
            Type = type;
            NewName = OriginalName = type.Name;


            RenameGenericParameters();
            RenameMethods();
            RenameFields();
            RenameProperties();

            if (Utils.IsInvalidName(OriginalName))
            {
                NewName = TypeRenamer.GetRenamer(type).Generate();
                if (type.HasGenericParameters)
                {
                    NewName += "`" + Type.GenericParameters.Count;
                }
                type.Name = NewName;
            }

        }

        private void RenameGenericParameters()
        {
            if (Type.HasGenericParameters)
            {
                if (Type.GenericParameters.Count == 1)
                {
                    GenericParamNames[Type.GenericParameters[0].Name] = "T";
                    Type.GenericParameters[0].Name = "T";
                }
                else
                {
                    var count = 0;
                    foreach (var param in Type.GenericParameters)
                    {
                        GenericParamNames[param.Name] = "T" + count;
                        param.Name = "T" + count++;
                    }
                }
            }
        }

        private void RenameMethods()
        {
            var staticCount = 0;
            var count = 0;
            foreach (var method in Type.Methods)
            {
                if (Utils.IsInvalidName(method.Name))
                {

                    string newName;
                    if (method.ImplMap != null)
                    {
                        newName = method.ImplMap.Name;
                    }
                    else if (method.IsStatic)
                    {
                        newName = "StaticMethod" + staticCount++;
                    }
                    else
                    {
                        newName = "Method" + count++;
                    }

                    MethodNames[method.Name] = newName;
                    method.Name = newName;
                }

                var paramCount = 0;
                foreach (var param in method.Parameters)
                {
                    if (param.Name == "")
                    {
                        if (!param.HasParamDef)
                        {
                            param.CreateParamDef();
                        }
                        param.ParamDef.Name = "A_" + paramCount++;
                    }
                }

                if (method.HasGenericParameters)
                {
                    if (method.GenericParameters.Count == 1)
                    {
                        method.GenericParameters[0].Name = "T";
                    }
                    else
                    {
                        var genericParamCount = 0;
                        foreach (var param in method.GenericParameters)
                        {
                            param.Name = "T" + genericParamCount++;
                        }
                    }
                }

            }
        }

        private void RenameFields()
        {
            var count = 0;
            foreach (var field in Type.Fields)
            {
                if (Utils.IsInvalidName(field.Name))
                {
                    var newName = "Field" + count++;

                    FieldNames[field.Name] = newName;
                    field.Name = newName;
                }
            }
        }

        private void RenameProperties()
        {
            var count = 0;
            foreach (var prop in Type.Properties)
            {
                if (Utils.IsInvalidName(prop.Name))
                {
                    var newName = "Prop" + count++;

                    PropNames[prop.Name] = newName;
                    prop.Name = newName;
                }
            }
        }

    }
}
