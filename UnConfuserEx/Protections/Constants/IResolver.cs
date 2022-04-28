using dnlib.DotNet;
using System.Collections.Generic;

namespace UnConfuserEx.Protections.Constants
{
    internal interface IResolver
    {

        public void Resolve(MethodDef method, IList<MethodDef> instances);

    }
}
