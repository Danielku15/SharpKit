using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using SharpKit.Targets;
using SharpKit.Targets.Ast;
using SharpKit.Targets.JavaScript;

namespace SharpKit.Compiler.Plugin
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

        CompilerLogger Log { get; }
        ICompilation Compilation { get; }

        SkProject Project { get; }
        List<SkFile> SkFiles { get; }

        ICustomAttributeProvider CustomAttributeProvider { get; }
        PathMerger PathMerger { get; }
        Dictionary<string, object> TargetData { get; }
        List<string> Defines { get; }
        CompilerSettings Settings { get; }
        string SkcVersion { get; }
        ITypeConverter TypeConverter { get; }

        #endregion

    }
}
