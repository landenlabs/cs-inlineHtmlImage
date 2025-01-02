using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


namespace InlineHtmlImages_NS
{
    /// <summary>
    /// ConvertData - Helper class with static method to perform base64 conversion
    /// on one or more files.
    /// 
    /// Author: Dennis Lang - 2014
    /// https://landenlabs.com/
    /// 
    /// This file is part of InlineHtmlImages.
    ///
    /// InlineHtmlImages is free software: you can redistribute it and/or modify
    /// it under the terms of the GNU General Public License as published by
    /// the Free Software Foundation, either version 3 of the License, or
    /// (at your option) any later version.
    ///
    /// InlineHtmlImages is distributed in the hope that it will be useful,
    /// but WITHOUT ANY WARRANTY; without even the implied warranty of
    /// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    /// GNU General Public License for more details.
    ///
    /// The GNU General Public License: <http://www.gnu.org/licenses/>.

    /// </summary>
    class ConvertData
    {
        /// <summary>
        /// Encode/Decode data files into base64 character stream.  
        /// Input can contain simple wildcard '*' or '?'.
        /// If output contains '*' it is replaced with input filename, excluding extension.
        // Ex:  InlineHtmlImages -toB64 -in *.png -out b64\\*.png  
        /// </summary>
        public static int B64Convert(string inFilePat, string outFilePat, bool toB64)
        {
            if (outFilePat.Contains('*'))
                outFilePat = outFilePat.Replace("*", "{0}");
            else
                outFilePat += "{0}";

            if (!outFilePat.Contains('.'))
                outFilePat += "{1}";

            string inDir = Path.GetDirectoryName(inFilePat);
            string inPat = Path.GetFileName(inFilePat);
            if (string.IsNullOrEmpty(inDir))
                inDir = ".\\";  //  Environment.CurrentDirectory;
            foreach (string inFile in Directory.GetFiles(inDir, inPat))
            {
                string inFileName = Path.GetFileNameWithoutExtension(inFile);
                string inFileExt = Path.GetExtension(inFile);
                string outFile = string.Format(outFilePat, inFileName, inFileExt);

                try
                {
                    if (toB64)
                    {
                        byte[] inBytes = File.ReadAllBytes(inFile);
                        char[] outBytes = null;
                        int outScale = 2;
                        int outLen = 0;
                        do
                        {
                            outBytes = new char[inBytes.Length * outScale++];
                            outLen = Convert.ToBase64CharArray(inBytes, 0, inBytes.Length, outBytes, 0);
                        } while (outLen == outBytes.Length);

                        if (outLen > 0)
                        {
                            StreamWriter writer = new StreamWriter(outFile);
                            writer.Write(outBytes, 0, outLen);
                            writer.Close();

                            double percent = (double)outLen / inBytes.Length;
                            ConWriter.ColorizeLine(string.Format(" %0aEncoded%0f {0} to {1} inSize:{2:N0}  outSize:{3:N0}  percent:{4:N1}",
                                inFile, outFile, inBytes.Length, outLen, percent));
                        }
                    }
                    else
                    {
                        // byte[] imageBytes = Convert.FromBase64String(base64String);
                        string inBytes = File.ReadAllText(inFile);
                        byte[] outBytes = Convert.FromBase64String(inBytes);
                        if (outBytes.Length > 0)
                        {
                            int outLen = outBytes.Length;
                            File.WriteAllBytes(outFile, outBytes);

                            double percent = (double)outLen / inBytes.Length;
                            ConWriter.ColorizeLine(string.Format(" %0aDecoded%0f {0} to {1} inSize:{2:N0}  outSize:{3:N0}  percent:{4:N1}",
                                inFile, outFile, inBytes.Length, outLen, percent));
                        }
                    }
                }
                catch (System.IO.DirectoryNotFoundException dex)
                {
                    ConWriter.ColorizeLine(string.Format("%0c  {0}%0f\n  Verify directory path exists", dex.Message));
                }
                catch (Exception ex)
                {
                    ConWriter.ColorizeLine(string.Format("%0c  Failed to convert {0} to {1}, error:{2}%0f\n", inFile, outFile, ex.Message));
                }
            }
            return 0;   
        }

      
    }
}
