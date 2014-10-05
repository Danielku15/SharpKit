using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.CSharp;
using SharpKit.Compiler.Ast;
using SharpKit.JavaScript.Ast;
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
        event Action BeforeMergeJsFiles;
        event Action BeforeInjectJsCode;
        event Action BeforeOptimizeJsFiles;
        event Action BeforeSaveJsFiles;
        event Action BeforeEmbedResources;
        event Action BeforeSaveNewManifest;
        event Action BeforeExit;

        event Action AfterParseCs;
        event Action AfterApplyExternalMetadata;
        event Action AfterConvertCsToTarget;
        event Action AfterMergeJsFiles;
        event Action AfterInjectJsCode;
        event Action AfterOptimizeJsFiles;
        event Action AfterSaveJsFiles;
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
        List<SkJsFile> SkJsFiles { get; }
        ICustomAttributeProvider CustomAttributeProvider { get; }
        #endregion

    }
}
