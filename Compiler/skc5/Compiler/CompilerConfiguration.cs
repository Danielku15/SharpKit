﻿using System;
using System.Configuration;
using System.Collections.Specialized;
using System.Globalization;
using System.Threading;

namespace SharpKit.Compiler
{
    class CompilerConfiguration
    {
        public bool Parallel { get; set; }
        public bool GenerateSourceMapsDebugFiles { get; set; }
        public static CompilerConfiguration Current { get; set; }
        public bool EnableLogging { get; set; }
        public bool CreateNativeImage { get; set; }
        public string SharpKitServiceUrl { get; set; }
        //public string LogFilename { get; set; }

        public static void LoadCurrent()
        {
            var x = new CompilerConfiguration();
            x.Load(ConfigurationManager.AppSettings);
            Current = x;
        }

        private void Load(NameValueCollection x)
        {
            Parallel = x["Parallel"] == "true";
            GenerateSourceMapsDebugFiles = x["GenerateSourceMapsDebugFiles"] == "true";
            EnableLogging = x["EnableLogging"] == "true";
            CreateNativeImage = x["CreateNativeImage"] == "true";
            SharpKitServiceUrl = x["SharpKitServiceUrl"] ?? "http://localhost:7667/";
        }
    }
}
