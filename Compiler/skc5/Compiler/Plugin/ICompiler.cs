using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.CSharp;
using SharpKit.Compiler.Ast;
using ICSharpCode.NRefactory.TypeSystem;

namespace SharpKit.Compiler
{
    /// <summary>
    /// Compiler events by order:
    ///     ParseCs
    ///     ApplyExternalMetadata
    ///     ConvertCsToJs
    ///     MergeJsFiles
    ///     InjectJsCode
    ///     OptimizeJsFiles
    ///     SaveJsFiles
    ///     EmbedResources
    ///     SaveNewManifest
    /// </summary>
    public interface ICompiler
    {
        #region Events

        event Action BeforeParseCs;
        event Action BeforeApplyExternalMetadata;
        event Action BeforeConvertCsToTarget;
        event Action BeforeMergeTargetFiles;
        event Action BeforeInjectTargetCode;
        event Action BeforeOptimizeTargetFiles;
        event Action BeforeSaveTargetFiles;
        event Action BeforeEmbedResources;
        event Action BeforeSaveNewManifest;
        event Action BeforeExit;

        event Action AfterParseCs;
        event Action AfterApplyExternalMetadata;
        event Action AfterConvertCsToTarget;
        event Action AfterMergeTargetFiles;
        event Action AfterInjectTargetCode;
        event Action AfterOptimizeTargetFiles;
        event Action AfterSaveTargetFiles;
        event Action AfterEmbedResources;
        event Action AfterSaveNewManifest;


        event Action<IEntity> BeforeConvertCsToTargetEntity;
        event Action<IEntity, ITargetNode> AfterConvertCsToTargetEntity;

        event Action<AstNode> BeforeConvertCsToTargetAstNode;
        event Action<AstNode, ITargetNode> AfterConvertCsToTargetAstNode;

        event Action<ResolveResult> BeforeConvertCsToTargetResolveResult;
        event Action<ResolveResult, ITargetNode> AfterConvertCsToTargetResolveResult;

        #endregion

        #region Properties

        ICompilation CsCompilation { get; }
        List<SkFile> SkFiles { get; }
        ICustomAttributeProvider CustomAttributeProvider { get; }

        #endregion

    }
}
