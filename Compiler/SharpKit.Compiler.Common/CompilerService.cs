using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Corex.IO.Tools;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Extensions;
using ICSharpCode.NRefactory.TypeSystem;
using SharpKit.Compiler.Plugin;

namespace SharpKit.Compiler
{
    public abstract class CompilerService 
    {
        public void PreLoad()
        {
            new SkProject();
            new NFile();
            new CSharpParser();
            AssemblyLoader.Create();
        }

        protected abstract ICompiler CreateCompiler(); 

        public CompileResponse Compile(CompileRequest req)
        {
            ICompiler skc = CreateCompiler();
            skc.Settings = req.Args;
            skc.Log = new CompilerLogger {Console = {AutoFlush = false}};
            if (req.CommandLineArgs.IsNotNullOrEmpty())
            {
                skc.CommandLineArguments = new ToolArgsTokenizer().Tokenize(req.CommandLineArgs);
            }
            skc.Init();
            var x = skc.Run();
            var xx = new CompileResponse { Output = skc.Log.Console.Items.ToList(), ExitCode = x };
            return xx;
        }

        public void Test()
        {
        }
    }

    [DataContract]
    public class CompileRequest
    {
        [DataMember]
        public string CommandLineArgs { get; set; }
        [DataMember]
        public CompilerSettings Args { get; set; }
    }

    [DataContract]
    public class CompileResponse
    {
        [DataMember]
        public List<string> Output { get; set; }
        [DataMember]
        public int ExitCode { get; set; }
    }
}