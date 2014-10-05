using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;

namespace SharpKit.Compiler
{
    class ExternalAttribute
    {
        public ResolvedAttribute Entity { get; set; }
        public IType TargetType { get; set; }
        public string TargetTypeName { get; set; }
    }

    interface ICsExternalMetadata
    {
        IEnumerable<ITypeDefinition> TypesWithExternalAttributes { get; }

        void Process();
    }
}
