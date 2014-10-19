using System;
using System.Collections.Generic;

namespace SharpKit.Compiler.Utils
{
    //FIX FOR ISSUE 306. Only the casing of the first path will be used, to make is compatible to windows.
    public class PathMerger
    {
        private static Dictionary<string, string> _exportedPaths;

        public string ConvertRelativePath(string path)
        {
            if (_exportedPaths == null)
                _exportedPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            _exportedPaths.TryAdd(path, path);
            return _exportedPaths[path];
        }

        public void Reset()
        {
            _exportedPaths = null;
        }
    }
}