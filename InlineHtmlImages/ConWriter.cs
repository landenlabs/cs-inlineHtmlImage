using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace InlineHtmlImages_NS
{
    /// <summary>
    /// Console colorized output writer.
    ///     
    /// Two main methods to colorize console output:
    ///    public static void ColorizeLine(string str)
    ///    public void WriteLine(ConColors conColors, string outMsg)
    ///    
    /// Use the first as a static method to colorize text on the console.
    /// Color is specified using %bf  where b=background and f=foreground color.
    /// See ColorizeLine for color codes.
    /// 
    /// Use WriteLine and optionally CreateFile to use to stream either to 
    /// the console or redirect to a file.
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
    public class ConWriter
    {
        TextWriter writer = null;
        public class ConColors
        {
            public ConsoleColor fg;
            public ConsoleColor bg;
            public virtual void Apply()
            {
                Console.ForegroundColor = fg;
                Console.BackgroundColor = bg;
            }
        };
        public class ConFgColor : ConColors
        {
            public override void Apply()
            {
                Console.ForegroundColor = fg;
            }
        };

        static ConColors defColors = new ConColors() { fg = Console.ForegroundColor, bg = Console.BackgroundColor };
        bool isColorChanged = false;

        public ConWriter()
        {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);
        }

        void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            // Reset console colors when control-c pressed.
            Console.BackgroundColor = defColors.bg;
            Console.ForegroundColor = defColors.fg;
            Console.WriteLine(" ");
        }

        public void CreateFile(string outFile)
        {
            writer = new StreamWriter(new FileStream(outFile, FileMode.Create, FileAccess.Write));
        }

        public void WriteLine(string outMsg)
        {
            if (writer == null)
            {
                if (isColorChanged)
                {
                    isColorChanged = false;
                    defColors.Apply();
                }
                System.Console.WriteLine(outMsg);
            }
            else
                writer.WriteLine(outMsg);
        }

        public void WriteLine(ConColors conColors, string outMsg)
        {
            if (writer == null)
            {
                isColorChanged = true;
                conColors.Apply();
                System.Console.WriteLine(outMsg);
            }
            else
                writer.WriteLine(outMsg);
        }

        /// <summary>
        /// Output console text with inline colorization via %BF B=background, F=foreground
        ///  
        /// Color hex codes (%0c = red foreground, %1c - red foreground dark blue background)
        ///     ---- Hex Color Values ---
        ///     00 Black           08 DarkGray    
        ///     01 DarkBlue        09 Blue        
        ///     02 DarkGreen       0a Green      
        ///     03 DarkCyan        0b Cyan       
        ///     04 DarkRed         0c Red        
        ///     05 DarkMagenta     0d Magenta    
        ///     06 DarkYellow      0e Yellow     
        ///     07 Gray            0f White   
        ///     
        ///  Example (Red Hello, Green World):
        ///    ConWriter.ColorizeLine("%0cHello %aWorld%0f");
        /// </summary>
        public static void ColorizeLine(string str)
        {
            ConsoleColor bg = Console.BackgroundColor;
            ConsoleColor fg = Console.ForegroundColor;

            int begIdx = 0;
            int endIdx;
            while ((endIdx = str.IndexOf('%', begIdx)) != -1)
            {
                Console.Write(str.Substring(begIdx, endIdx - begIdx));
                begIdx = endIdx + 1;
                if (str[begIdx] == '%')
                    Console.Write('%');
                else
                {
                    int hex = int.Parse(str.Substring(begIdx, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                    begIdx += 2;
                    Console.BackgroundColor = (ConsoleColor)(hex / 16);
                    Console.ForegroundColor = (ConsoleColor)(hex & 15);
                }
            }

            Console.WriteLine(str.Substring(begIdx));
            Console.BackgroundColor = bg;
            Console.ForegroundColor = fg;
        }

    };
}
