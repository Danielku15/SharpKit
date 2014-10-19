using System;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using SharpKit.Compiler.Targets.Ast;

namespace SharpKit.Compiler.Targets
{
    public interface IMemberConverter
    {
        event Action<IEntity> BeforeVisitEntity;
        event Action<IEntity, ITargetNode> AfterVisitEntity;

        event Action<AstNode> BeforeConvertCsToTargetAstNode;
        event Action<AstNode, ITargetNode> AfterConvertCsToTargetAstNode;

        event Action<ResolveResult> BeforeConvertCsToTargetResolveResult;
        event Action<ResolveResult, ITargetNode> AfterConvertCsToTargetResolveResult;
    }
}