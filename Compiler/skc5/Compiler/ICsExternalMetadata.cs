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
        CompilerLogger Log { get; set; }
        SkProject Project { get; set; }
        IEnumerable<ITypeDefinition> TypesWithExternalAttributes { get; }

        void Process();
    }
}
