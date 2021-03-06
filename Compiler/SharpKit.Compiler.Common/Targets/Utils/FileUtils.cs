﻿using System.IO;
using SharpKit.Compiler.Utils;

namespace SharpKit.Compiler.Targets.Utils
{
    public class FileUtils
    {
        public static void CompareAndSaveFile(string file, string tmpFile)
        {
            var fi = new FileInfo(file);
            if (fi.Exists)// && fi.IsReadOnly)
            {
                if (!FileCompare(file, tmpFile))
                {
                    fi.IsReadOnly = false;
                    fi.Delete();
                    File.Move(tmpFile, file);
                }
                else
                {
                    File.Delete(tmpFile);
                }
            }
            else
            {
                if (fi.Exists)
                    fi.Delete();
                File.Move(tmpFile, file);
            }
        }

        /// <summary>
        /// This method accepts two strings the represent two files to 
        /// compare. A return value of 0 indicates that the contents of the files
        /// are the same. A return value of any other value indicates that the 
        /// files are not the same.
        /// </summary>
        /// <param name="file1"></param>
        /// <param name="file2"></param>
        /// <returns></returns>
        static bool FileCompare(string file1, string file2)
        {
            int file1byte;
            int file2byte;
            FileStream fs1;
            FileStream fs2;

            // Determine if the same file was referenced two times.
            if (file1 == file2)
            {
                // Return true to indicate that the files are the same.
                return true;
            }

            // Open the two files.
            fs1 = new FileStream(file1, FileMode.Open, FileAccess.Read);
            fs2 = new FileStream(file2, FileMode.Open, FileAccess.Read);

            // Check the file sizes. If they are not the same, the files 
            // are not the same.
            if (fs1.Length != fs2.Length)
            {
                // Close the file
                fs1.Close();
                fs2.Close();

                // Return false to indicate files are different
                return false;
            }

            // Read and compare a byte from each file until either a
            // non-matching set of bytes is found or until the end of
            // file1 is reached.
            do
            {
                // Read one byte from each file.
                file1byte = fs1.ReadByte();
                file2byte = fs2.ReadByte();
            }
            while ((file1byte == file2byte) && (file1byte != -1));

            // Close the files.
            fs1.Close();
            fs2.Close();

            // Return the success of the comparison. "file1byte" is 
            // equal to "file2byte" at this point only if the files are 
            // the same.
            return ((file1byte - file2byte) == 0);
        }

        public static void CssMinify(string file)
        {
            var tmpFile = file + ".minify";
            var css = CssCompressor.Compress(File.ReadAllText(file));
            File.WriteAllText(tmpFile, css);
            File.Delete(file);
            File.Move(tmpFile, file);
        }
    }
}