using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;


namespace InlineHtmlImages_NS
{
	class Program
    {
        enum Command { eEncode, eExtract, eSummary, eToBase64, eFromBase64 };

		/// <summary>
		/// InlineHtmlImages - convert html image references to inlined base64 character streams.
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
		[STAThread]
        static void Main(string[] args)
        {
            bool exit = false;
            Dictionary<string, List<string>> rawArgs = new Dictionary<string, List<string>>();
            Arguments cmdArgs = new Arguments(args, true, ref rawArgs);

            string sBanner = "\n\n" + ConvertHtml.GetTitle() + "\n\n";
            string sHelp = sBanner
                    + "%0aDes:  Convert HTML image references to inlined base64 images %0f\n"
                    + "%0eOptions:%0f\n"
                    + "   -in <inputFile.html>   ; \n"
                    + "   -out <outputFile.html> ; \n"
                    + "   -embed                 ; Encode images into html (default) \n"
                    + "   -exclude <pat>         ; Used with embed to exclude some images \n"
                    + "                          ;  from being impeded, ex -exclude 'foo.png|*.gif' \n"
                    + "                          ;  Can specify multiple exclude options \n"
                    + "   -extract               ; Extract encoded image from html \n" 
                    + "   -summary               ; List image tag info \n"
                    + "   -ok                    ; Okay to over write output files \n"
                    + "   -help                  ; help \n"
                    + "\n"
                    + "   -toB64                 ; Encode raw input data to base64 \n"
                    + "   -fromB64               ; Decode base64 input data "
                    + "\n";

            sHelp += "\n%0eExamples:%0f\n"
                + " ; Inline HTML images referenced in helpOrg.html \n"
                + " %0aInlineHtmlImages%0f -embed    -in helpOrg.html -out helpInlined.html \n\n"
                + " ; Reverse inlined HTML and get original images back \n"
                + " %0aInlineHtmlImages%0f -extract  -in helpInlined.html -out helpNew.html \n\n"
                + " ; Encode PNG images into base64 character stream \n"
                + " %0aInlineHtmlImages%0f -toB64   -in *.png -out b64\\*.png  \n"
                + " %0aInlineHtmlImages%0f -toB64   -in *.png -out *.b64  \n"
                + " %0aInlineHtmlImages%0f -fromB64 -in *.b64 -out *.png \n\n"
                + "\n";

            if (cmdArgs.Count == 0 || cmdArgs.ContainsKey("help"))
            {
                cmdArgs.Remove("help");
                ConWriter.ColorizeLine(sHelp);
                // System.Console.WriteLine(sHelp);
                exit = true;
            }

            List<string> inFiles = null;
            List<string> outFiles = null;

            if (!exit && !cmdArgs.TryGetValueAndRemove("in", out inFiles))
            {
                ConWriter.ColorizeLine("\n %0cError%0f - All commands require %0a-in <inHtml>%0f parameter, see -help\n");
                exit = true;
            }

            cmdArgs.TryGetValueAndRemove("out", out outFiles);

            // TODO - expand any wildcards in input/output

            if (exit)
            {
                Console.WriteLine("\n\n");
                Console.Out.Flush();
                Environment.Exit(0);
            }

            ConvertHtml convertHtml = new ConvertHtml();
            bool okayToOverwrite = false;
            
            Command command = Command.eEncode;
            List<string> excludeList = null; //  = new List<string>();
            foreach (string extraArgs in cmdArgs.Keys)
            {
                string extraLwr = extraArgs.ToLower();
                if (extraLwr.StartsWith("extract") || extraLwr.StartsWith("decode"))
                    command = Command.eExtract;
                else if (extraLwr.StartsWith("embed") || extraLwr.StartsWith("encode"))
                    command = Command.eEncode;
                else if (extraLwr.StartsWith("summary"))
                    command = Command.eSummary;
                else if (extraLwr.StartsWith("tob64"))
                    command = Command.eToBase64;
                else if (extraLwr.StartsWith("fromb64"))
                    command = Command.eFromBase64;
                else if (extraLwr.StartsWith("exclud"))
                    excludeList = cmdArgs[extraArgs];
                else if (extraLwr.StartsWith("ok"))
                    okayToOverwrite = convertHtml.okayToOverwrite = true;
            }

            // TODO - add decode images and encoding images.
            int exitStatus = -1;
            switch (command)
            {
            case Command.eEncode:
                if (outFiles.Count == 0)
                    ConWriter.ColorizeLine("\n %0cError%0f - Inline(encoding) HTML images requires %0a-out <outHtml>%0f parameter, see -help\n");
                else
                    for (int fidx = 0; fidx < Math.Min(inFiles.Count, outFiles.Count); fidx++)
                    {
                        string inFile = inFiles[fidx];
                        string outFile = outFiles[fidx];

                        // TODO - let user select these two booleans.
                        // TODO - let user decide if shared image should be inlined, how many shares are too many to inline.

                        bool setImageAlt = true;
                        bool optimizeImage = true;
                        exitStatus = convertHtml.EncodeImagesInHTML(inFile, outFile, excludeList, setImageAlt, optimizeImage);
                    }
                break;
            case Command.eExtract:
                if (outFiles.Count == 0)
                    ConWriter.ColorizeLine("\n %0cError%0f - Extracting(decoding) HTML images requires %0a-out <outHtml>%0f parameter, see -help\n");
                else
                    for (int fidx = 0; fidx < Math.Min(inFiles.Count, outFiles.Count); fidx++)
                    {
                        string inFile = inFiles[fidx];
                        string outFile = outFiles[fidx];
                        exitStatus = convertHtml.ExtractImagesInHTML(inFile, outFile);
                    }
                break;
            case Command.eSummary:
                for (int fidx = 0; fidx < inFiles.Count; fidx++)
                {
                    string inFile = inFiles[fidx];
                    exitStatus = convertHtml.SummaryOfImagesInHTML(inFile);
                }
                break;

            case Command.eToBase64:
                if (outFiles.Count == 0)
                    ConWriter.ColorizeLine("\n %0cError%0f  -toB64 requires %0a-out <outPathPattern>%0f parameter, see -help\n");
                else
                    for (int fidx = 0; fidx < Math.Min(inFiles.Count, outFiles.Count); fidx++)
                    {
                        string inFile = inFiles[fidx];
                        string outFile = outFiles[fidx];
                        exitStatus = ConvertData.B64Convert(inFile, outFile, true);
                    }
                break;
            case Command.eFromBase64:
                if (outFiles.Count == 0)
                    ConWriter.ColorizeLine("\n %0cError%0f  -fromB64requires %0a-out <outPathPattern>%0f parameter, see -help\n");
                else
                    for (int fidx = 0; fidx < Math.Min(inFiles.Count, outFiles.Count); fidx++)
                    {
                        string inFile = inFiles[fidx];
                        string outFile = outFiles[fidx];
                        exitStatus = ConvertData.B64Convert(inFile, outFile, false);
                    }
                break;
            }

            Environment.Exit(exitStatus);
        }

      
    }
}
