using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using Mirrored.SharpKit.JavaScript;
using System.IO;
using SharpKit.JavaScript.Ast;
using SharpKit.Targets.JavaScript;

namespace SharpKit.Compiler
{
    class JsFileMerger
    {
        public JsFileMerger()
        {
            ExternalFiles = new List<SkJsFile>();
        }
        public CompilerTool Compiler { get; set; }
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
            return Path.GetFullPath(file1).EqualsIgnoreCase(Path.GetFullPath(file2));
        }

        public SkJsFile GetJsFile(string filename, bool isExternal)
        {
            filename = filename.Replace("/", Sk.DirectorySeparator);
            var file = Files.Where(t => FileEquals(t.TargetFile.Filename, filename)).FirstOrDefault();
            if (file == null)
                file = ExternalFiles.Where(t => FileEquals(t.TargetFile.Filename, filename)).FirstOrDefault();
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

    //FIX FOR ISSUE 306. Only the casing of the first path will be used, to make is compatible to windows.
    class PathMerger
    {

        private static Dictionary<string, string> exportedPaths;
        public string ConvertRelativePath(string path)
        {
            if (exportedPaths == null)
                exportedPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            exportedPaths.TryAdd(path, path);
            return exportedPaths[path];
        }

        public void Reset()
        {
            exportedPaths = null;
        }

    }

}
