using ICSharpCode.NRefactory.TypeSystem;

namespace SharpKit.Compiler.Targets
{
    interface ISupportSourceAttribute
    {
        IAttribute SourceAttribute { get; set; }
    }

    interface ISupportSharpKitVersion
    {
        string SharpKitVersion { get; set; }
    }
}
