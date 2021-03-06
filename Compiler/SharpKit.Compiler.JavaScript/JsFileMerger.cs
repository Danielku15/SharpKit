﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using SharpKit.Compiler.JavaScript.Ast;
using SharpKit.Compiler.JavaScript.Utils;
using SharpKit.Compiler.Plugin;

namespace SharpKit.Compiler.JavaScript
{
    class JsFileMerger
    {
        public JsFileMerger()
        {
            ExternalFiles = new List<SkJsFile>();
        }
        public ICompiler Compiler { get; set; }
        public SkProject Project { get; set; }
        /// <summary>
        /// This collection may be updated to include new files
        /// </summary>
        public List<SkJsFile> Files { get; set; }
        public List<SkJsFile> ExternalFiles { get; set; }
        public CompilerLogger Log { get; set; }
        public void MergeFiles()
        {
            var atts = GetJsMergedFileAttributes(Project.Compilation.MainAssembly);//.getAssemblyEntity());
            var includedFiles = new HashSet<string>();
            foreach (var att in atts)
            {
                MergeFiles(Compiler.PathMerger.ConvertRelativePath(att.Filename), att.Sources, att.Minify);
            }
        }

        bool FileEquals(string file1, string file2)
        {
            return CssCompressorExtensions.EqualsIgnoreCase(Path.GetFullPath(file1), Path.GetFullPath(file2));
        }

        public SkJsFile GetJsFile(string filename, bool isExternal)
        {
            filename = filename.Replace("/", Utils.Sk.DirectorySeparator);
            var file = Files.FirstOrDefault(t => FileEquals(t.TargetFile.Filename, filename));
            if (file == null)
                file = ExternalFiles.FirstOrDefault(t => FileEquals(t.TargetFile.Filename, filename));
            if (file == null)
            {
                file = new SkJsFile { TargetFile = new JsFile { Filename = filename, Units = new List<JsUnit>() }, Compiler = Compiler };
                if (isExternal)
                {
                    file.TargetFile.Units.Add(new JsExternalFileUnit { Filename = filename });
                    ExternalFiles.Add(file);
                }
                else
                {
                    Files.Add(file);
                }
            }
            return file;
        }

        void MergeFiles(string target, string[] sources, bool minify)
        {
            var target2 = GetJsFile(target, false);
            if (minify)
                target2.Minify = minify;
            var sources2 = sources.Select(t => GetJsFile(t, true)).ToList();
            MergeFiles(target2, sources2);
        }
        
        public void MergeFiles(SkJsFile target, List<SkJsFile> sources)
        {
            foreach (var source2 in sources)
            {
                Compiler.Log.WriteLine("    Adding {0} units to merged file", source2.TargetFile.Units.Count);
                target.TargetFile.Units.AddRange(source2.TargetFile.Units);
            }
        }

        JsMergedFileAttribute[] GetJsMergedFileAttributes(IAssembly asm)
        {
            var list = new List<JsMergedFileAttribute>();
            if (asm != null && asm.AssemblyAttributes != null)
            {
                var list2 = asm.GetMetadatas<JsMergedFileAttribute>();
                list.AddRange(list2);
            }
            return list.ToArray();
        }

    }

}
