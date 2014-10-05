using System;
using System.Collections.Generic;
using System.IO;
using Corex.Helpers;

namespace SharpKit.Compiler
{
    //class Skc5CacheData
    //{
    //    public string VersionKey { get; set; }
    //    public int? NGenRetries { get; set; }
    //    public bool? CreatedNativeImage { get; set; }
    //    static Dictionary<string, string> ReadIniFile(string filename)
    //    {
    //        var dic = new Dictionary<string, string>();
    //        foreach (var line in File.ReadAllLines(filename))
    //        {
    //            if (line.StartsWith("#"))
    //                continue;
    //            var index = line.IndexOf("=");
    //            if (index <= 0)
    //                continue;
    //            var name = line.Substring(0, index);
    //            var value = line.Substring(index + 1);
    //            dic[name] = value;
    //        }
    //        return dic;
    //    }
    //    public void Load(string filename)
    //    {
    //        var dic = ReadIniFile(filename);
    //        VersionKey = dic.TryGetValue("VersionKey");
    //        NGenRetries = ParseHelper.TryInt(dic.TryGetValue("NGenRetries"));
    //        CreatedNativeImage = ParseHelper.TryBoolean(dic.TryGetValue("CreatedNativeImage"));
    //    }
    //    public void Save(string filename)
    //    {
    //        var dir = Path.GetDirectoryName(filename);
    //        if (dir.IsNotNullOrEmpty() && !Directory.Exists(dir))
    //            Directory.CreateDirectory(dir);
    //        File.WriteAllLines(filename, new[]
    //        {
    //            "VersionKey="+VersionKey,
    //            "NGenRetries="+NGenRetries,
    //            "CreatedNativeImage="+CreatedNativeImage,
    //        });
    //    }
    //}

    //class CompilerEvent
    //{
    //    public CompilerEvent(Action before, Action action, Action after)
    //    {
    //        Before = before;
    //        Action = action;
    //        After = after;
    //    }
    //    public CompilerEvent(Action action)
    //    {
    //        Action = action;
    //    }
    //    public Action Before { get; set; }
    //    public Action Action { get; set; }
    //    public Action After { get; set; }
    //}
}
