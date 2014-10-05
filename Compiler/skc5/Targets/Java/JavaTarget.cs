using System.Collections.Generic;
using System.Linq;
using SharpKit.Compiler;
using SharpKit.Compiler.Java;
using SharpKit.Compiler.JavaScript;

namespace SharpKit.Targets.Java
{
    class JavaTarget : CompilerTargetBase
    {
        public override CompilerTarget Target
        {
            get { return CompilerTarget.Java; }
        }

        public override string OutputSuffix
        {
            get { return ".java"; }
        }

        public override IEnumerable<string> ManifestFiles
        {
            get { return Enumerable.Empty<string>(); }
        }

        public override ICsExternalMetadata BuildExternalMetadata()
        {
            return new JExternalMetadata();
        }

        public override ITypeConverter BuildTypeConverter()
        {
            return new JTypeConverter();
        }

        public override void MergeTargetFiles()
        {
        }

        public override void InjectTargetCode()
        {
        }

        public override void OptimizeTargetFiles()
        {
        }

        public override void SaveTargetFiles()
        {
        }

        public override void EmbedResources()
        {
        }
    }
}
