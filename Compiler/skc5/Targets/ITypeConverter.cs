using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;
using SharpKit.Compiler.Plugin;
using SharpKit.Targets.Ast;

namespace SharpKit.Targets
{
    public interface ITypeConverter
    {
        ICompiler Compiler { get; set; }
        Action<Dictionary<TargetFile, List<ITypeDefinition>>> BeforeExportTypes { get; set; }
        IEnumerable<TargetFile> TargetFiles { get; }
        event Action<IMemberConverter> ConfigureMemberConverter;
        void Process();
    }
}
