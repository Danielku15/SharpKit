using System;
using System.Text;
using Microsoft.Build.Tasks;
using System.IO;
using Microsoft.Build.Framework;
using SharpKit.Compiler.JavaScript.MsBuild;

// ReSharper disable once CheckNamespace
namespace SharpKit.Compiler.MsBuild
{
    public class Skc : ManagedCompiler
    {
        public ITaskItem[] NoneFiles { get; set; }
        public ITaskItem[] ContentFiles { get; set; }
        public ITaskItem[] SkcPlugins { get; set; }

        [Output]
        public ITaskItem OutputGeneratedFile { get; set; }

        public bool UseHostCompilerIfAvailable { get; set; }

        public string TargetFrameworkVersion { get; set; }
        public bool UseBuildService { get; set; }

        public bool SkcRebuild { get; set; }

        string OutputGeneratedDir;


        protected override string GenerateFullPathToTool()
        {
            return Path.Combine(ToolPath, ToolExe);
        }

        private static bool IsLegalIdentifier(string identifier)
        {
            if (identifier.Length == 0)
            {
                return false;
            }
            if (!TokenChar.IsLetter(identifier[0]) && (identifier[0] != '_'))
            {
                return false;
            }
            for (int i = 1; i < identifier.Length; i++)
            {
                char c = identifier[i];
                if (((!TokenChar.IsLetter(c) && !TokenChar.IsDecimalDigit(c)) && (!TokenChar.IsConnecting(c) && !TokenChar.IsCombining(c))) && !TokenChar.IsFormatting(c))
                {
                    return false;
                }
            }
            return true;
        }

        internal string GetDefineConstantsSwitch(string originalDefineConstants)
        {
            if (originalDefineConstants != null)
            {
                StringBuilder builder = new StringBuilder();
                foreach (string str in originalDefineConstants.Split(new char[] { ',', ';', ' ' }))
                {
                    if (IsLegalIdentifier(str))
                    {
                        if (builder.Length > 0)
                        {
                            builder.Append(";");
                        }
                        builder.Append(str);
                    }
                    else if (str.Length > 0)
                    {
                        base.Log.LogWarningWithCodeFromResources("Csc.InvalidParameterWarning", new object[] { "/define:", str });
                    }
                }
                if (builder.Length > 0)
                {
                    return builder.ToString();
                }
            }
            return null;
        }

        protected override void AddResponseFileCommands(CommandLineBuilderExtension commandLine)
        {
            if (OutputGeneratedFile != null && !String.IsNullOrEmpty(OutputGeneratedFile.ItemSpec))
                commandLine.AppendSwitchIfNotNull("/outputgeneratedfile:", OutputGeneratedFile);
            commandLine.AppendSwitchUnquotedIfNotNull("/define:", this.GetDefineConstantsSwitch(base.DefineConstants));
            AddReferencesToCommandLine(commandLine);
            base.AddResponseFileCommands(commandLine);
            if (ResponseFiles != null)
            {
                foreach (ITaskItem item in ResponseFiles)
                {
                    commandLine.AppendSwitchIfNotNull("@", item.ItemSpec);
                }
            }
            if (ContentFiles != null)
            {
                foreach (var file in ContentFiles)
                {
                    commandLine.AppendSwitchIfNotNull("/contentfile:", file.ItemSpec);
                }
            }
            if (NoneFiles != null)
            {
                foreach (var file in NoneFiles)
                {
                    commandLine.AppendSwitchIfNotNull("/nonefile:", file.ItemSpec);
                }
            }
            if (SkcPlugins != null)
            {
                foreach (var file in SkcPlugins)
                {
                    commandLine.AppendSwitchIfNotNull("/plugin:", file.ItemSpec);
                }
            }
            if (SkcRebuild)
                commandLine.AppendSwitch("/rebuild");
            if (UseBuildService)
            {
                Log.LogMessage("CurrentDirectory is: " + Directory.GetCurrentDirectory());
                commandLine.AppendSwitchIfNotNull("/dir:", Directory.GetCurrentDirectory());
            }

            commandLine.AppendSwitchIfNotNull("/TargetFrameworkVersion:", TargetFrameworkVersion);
        }

        private void AddReferencesToCommandLine(CommandLineBuilderExtension commandLine)
        {
            if ((References != null) && (References.Length != 0))
            {
                foreach (ITaskItem item in References)
                {
                    string metadata = item.GetMetadata("Aliases");
                    if (string.IsNullOrEmpty(metadata))
                    {
                        commandLine.AppendSwitchIfNotNull("/reference:", item.ItemSpec);
                    }
                    else
                    {
                        foreach (string str2 in metadata.Split(new char[] { ',' }))
                        {
                            string str3 = str2.Trim();
                            if (str2.Length != 0)
                            {
                                if (str3.IndexOfAny(new char[] { ',', ' ', ';', '"' }) != -1)
                                {
                                    throw new ArgumentException("Csc.AssemblyAliasContainsIllegalCharacters" + item.ItemSpec + str3);
                                }
                                if (string.Compare("global", str3, StringComparison.OrdinalIgnoreCase) == 0)
                                {
                                    commandLine.AppendSwitchIfNotNull("/reference:", item.ItemSpec);
                                }
                                else
                                {
                                    throw new NotImplementedException("TODO: Implement");
                                    //commandLine.AppendSwitchAliased("/reference:", str3, item.ItemSpec);
                                }
                            }
                        }
                    }
                }
            }
        }

        protected override MessageImportance StandardOutputLoggingImportance
        {
            get
            {
                return MessageImportance.High;
            }
        }


        bool DetectBuildService()
        {
            try
            {
                var x = BuildClient();
                x.Test();
                return true;
            }
            catch// (Exception e)
            {
                return false;
            }
        }

        private CompilerServiceClient BuildClient()
        {
            return new CompilerServiceClient(MsBuildSettings.DefaultServiceUrl);
        }

        public override bool Execute()
        {
            UseBuildService = DetectBuildService();
            var outputAssembly = OutputAssembly.ItemSpec;
            OutputGeneratedDir = Path.GetDirectoryName(outputAssembly);
            OutputAssembly.ItemSpec = Path.Combine(OutputGeneratedDir, Path.GetFileName(outputAssembly));
            if (UseBuildService)
            {
                var ext = new CommandLineBuilderExtension();
                //var args = new CompilerToolArgs();
                AddResponseFileCommands(ext);

                var client = BuildClient();
                var res = client.Compile(new CompileRequest { CommandLineArgs = ext.ToString() });
                foreach (var s in res.Output)
                {
                    LogEventsFromTextOutput(s, MessageImportance.High);
                }
                return res.ExitCode == 0;
            }
            var success = base.Execute();
            return success;
        }

        protected override string ToolName
        {
            get
            {
                return MsBuildSettings.ToolName;
            }
        }
    }
}