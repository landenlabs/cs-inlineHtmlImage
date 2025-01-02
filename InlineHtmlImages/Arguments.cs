using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace InlineHtmlImages_NS
{
    /// <summary>
    /// Arguments class - Parse command line arguments into a dictonary. 
    /// 
    /// Use properties Keys and Values and/or methods TryGetValue and ContainsKey 
    /// to access parsed commands.
    /// 
    /// Valid input command parameters syntax:
    ///  [-,/,--]param[ :=][["']value["']]
    ///  
    /// Examples: 
    ///  -param1 value1 --param2 /param3:"Test-:-work"  /param4=happy -param5 '--=nice=--'
    /// 
    /// Code from CodeProject "C#/.NET Command Line Arguments Parser"
    /// By GriffonRL, covered by MIT license.
    /// 
    /// http://www.codeproject.com/KB/recipes/command_line.aspx
    /// 
    /// Modified by Dennis Lang - 2014
    /// https://landenlabs.com/
    /// 
    /// ========== License ==========
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
    public class Arguments : Dictionary<string, List<string>>
    {
        /// <summary>
        /// Parse arguments into two dictionaries.
        /// This dictionary gets the parsed arguments and rawArgs gets any arguments with no switch.
        /// The rawArgs dictionary is stored with key of the previous valid parameters.
        /// 
        /// Ex:    /par1 value1  value2 value3 /par2 value4 value5
        ///   Arguments Dictionary
        ///      [par1] = value1
        ///      [par2] = value4
        ///   RawArgs Dictionary
        ///      [par1] = List[value2, value3]
        ///      [par2] = value5
        /// </summary>
        /// <param name="Args"></param>
        /// <param name="foldArgToLowercase"></param>
        /// <param name="rawArgs"></param>
        public Arguments(string[] Args, bool foldArgToLowercase, ref Dictionary<string, List<string>> rawArgs)
        {
            Regex Spliter = new Regex(@"^-{1,2}|^/|[=]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            Regex Remover = new Regex(@"^'(?<a>[^']+)'|^""(?<a>[^""]+)""|^(?<a>[^'""]+)$" , RegexOptions.IgnoreCase | RegexOptions.Compiled);

            string parameter = null;
            string lastParameter = string.Empty;
            string[] parts;

            // Valid parameters forms:
            //  [-,/,--]param[ =][["']value["']]

            // Examples: 
            //  -param1 value1 --param2 "value with spaces"
            //   /param4=happy -param5 '--=nice=--'

            foreach (string Txt in Args)
            {
                // Look for new parameters (-,/ or --) and a
                // possible enclosed value (=)

                parts = Spliter.Split(Txt, 3);
#if false
// requires net v4.5
                var goodParts = parts.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
#else
                var goodParts = parts.Where(s => !string.IsNullOrEmpty(s)).ToArray();
#endif

                if (goodParts.Length != 0)
                switch (parts.Length)
                {
                    case 1:
                        // Found a value (for the last parameter 
                        // found (space separator))
                        if (parameter != null)
                        {
                            goodParts[0] = Remover.Replace(goodParts[0], "${a}");
                            this.Append(parameter, goodParts[0]);
                            parameter = null;
                        }
                        else
                        {
                            if (rawArgs != null)
                            {
                               if (!rawArgs.ContainsKey(lastParameter))
                                   rawArgs.Add(lastParameter, new List<string>());

                               rawArgs[lastParameter].Add(Txt);
                            }
                        }
                        break;
                        

                    case 2:
#if false
                        // Found just a parameter
                        // The last parameter is still waiting. 
                        // With no value, set it to true.

                        if (parameter != null)
                        {
                            if (!this.ContainsKey(parameter))
                                this.Append(parameter, "true");
                        }
                        lastParameter = parameter = foldArgToLowercase ? goodParts[0].ToLower() : goodParts[0];
                        break;
#endif

                    case 3:

                        // Parameter with enclosed value
                        // The last parameter is still waiting. 
                        // With no value, set it to true.

                        if (parameter != null)
                        {
                            if (!this.ContainsKey(parameter))
                                this.Append(parameter, "true");
                        }

                        lastParameter = parameter = foldArgToLowercase ? goodParts[0].ToLower() : goodParts[0];

                        // -parm:value --parm:value /parm:value
                        // -parm=value --parm=value /parm=value
                        // where value is:  'value' or "value" or just value

                        // Remove possible enclosing characters (",')
                        if (goodParts.Length > 1)
                        {
                            goodParts[1] = Remover.Replace(goodParts[1], "${a}");
                            this.Append(parameter, goodParts[1]);
                            parameter = null;
                        }
                       
                        break;
                }
            }
            // In case a parameter is still waiting

            if (parameter != null)
            {
                if (!this.ContainsKey(parameter))
                    this.Append(parameter, "true");
            }
        }

        void Append(string key, string value)
        {
            List<string> oldList;
            if (!TryGetValue(key, out oldList))
                oldList = new List<string>();
            oldList.Add(value);
            this[key] = oldList;
        }

        //
        // Summary:
        //     Gets the value associated with the specified key.
        //     If found, also removes it from Dictionary.
        //
        // Parameters:
        //   key:
        //     The key of the value to get.
        //
        //   value:
        //     When this method returns, contains the value associated with the specified
        //     key, if the key is found; otherwise, the default value for the type of the
        //     value parameter. This parameter is passed uninitialized.
        //
        // Returns:
        //     true if the System.Collections.Generic.Dictionary<TKey,TValue> contains an
        //     element with the specified key; otherwise, false.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     key is null.
        public bool TryGetValueAndRemove(string key, out List<string> value)
        {
            bool got = TryGetValue(key, out value);
            if (got)
                this.Remove(key);
            return got;
        }
    }

}
