using System;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using SharpKit.Compiler.Targets.Java;

namespace SharpKit.Compiler.Java.Utils
{
    static class Sk
    {
        public static JTypeAttribute GetJTypeAttribute(this ITypeDefinition ce)
        {
            if (ce == null)
                return null;
            var att = ce.GetMetadata<JTypeAttribute>();
            if (att == null && ce.ParentAssembly != null)
            {
                att = ce.ParentAssembly.GetMetadatas<JTypeAttribute>()
                    .FirstOrDefault(t => t.TargetType == ce);

                if (att == null)
                {
                    att = GetDefaultJTypeAttribute(ce);
                }
            }
            return att;
        }

        private static JTypeAttribute GetDefaultJTypeAttribute(ITypeDefinition ce)
        {
            if (ce == null)
                return null;
            return ce.ParentAssembly.GetMetadatas<JTypeAttribute>().FirstOrDefault(t => t.TargetType == null && t.TargetTypeName.IsNullOrEmpty());
        }

    }
}
