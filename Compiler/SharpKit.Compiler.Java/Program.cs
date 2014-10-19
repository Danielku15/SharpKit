using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace SharpKit.Compiler.Java
{
    class Program
    {
        public static int Main(string[] args)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            CollectionExtensions.Parallel = ConfigurationManager.AppSettings["Parallel"] == "true";
            CollectionExtensions.ParallelPreAction = () => Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            //Console.AutoFlush = true;
            System.Console.WriteLine("Parallel=" + CollectionExtensions.Parallel);
            var skc = new JavaCompiler() { CommandLineArguments = args };
            skc.Init();
            var res = skc.Run();
            stopwatch.Stop();
            System.Console.WriteLine("Total: {0}ms", stopwatch.ElapsedMilliseconds);
            //System.Console.Flush();
            return res;
        }
    }
}
