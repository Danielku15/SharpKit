using SharpKit.Targets.Utils;

namespace SharpKit.Targets.Ast
{
    public abstract class TargetFile
    {
        public string Filename { get; set; }
        public abstract void SaveAs(string filename);

        public void CompareAndSave()
        {
            var tmpFile = Filename + ".tmp";
            SaveAs(tmpFile);
            FileUtils.CompareAndSaveFile(Filename, tmpFile);
        }
    }
}
