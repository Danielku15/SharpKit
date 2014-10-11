using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;
using SharpKit.Compiler;

namespace SharpKit.Targets
{
    class ExternalAttribute
    {
        public ResolvedAttribute Entity { get; set; }
        public IType TargetType { get; set; }
        public string TargetTypeName { get; set; }
    }

    public interface ICsExternalMetadata
    {
        IEnumerable<ITypeDefinition> TypesWithExternalAttributes { get; }

        void Process();
    }
}
