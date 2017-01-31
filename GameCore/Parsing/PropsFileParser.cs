/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine.Parsing
{
    public static class PropsFileParser
    {
        //possible targets:
        //GameAccount.Load
        //Thing.Load
        //ThingDef.LoadDefsSection
        //Globals.LoadGlobals

        //regular expressions for stream loading
        //[type name]//comment
        internal static readonly Regex headerRE = new Regex(@"^\[\s*(?<type>.*?)(\s+(?<name>.*?))?\s*\]\s*(//(?<comment>.*))?$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        //"triggerkey=@triggername//comment"
        //"triggerkey @triggername//comment"
        //triggerkey can be "ON", "ONTRIGGER", "ONBUTTON", or ""
        internal static readonly Regex triggerRE = new Regex(@"^\s*(?<triggerkey>(on|ontrigger|onbutton))((\s*=\s*)|(\s+))@?\s*(?<triggername>.+?)\s*(//(?<comment>.*))?$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        //private static int line;

        //private static void Warning(string s) {
        //	Logger.WriteWarning(WorldSaver.CurrentFile,line,s);
        //}
        //
        //private static void Error(string s) {
        //	Logger.WriteError(WorldSaver.CurrentFile,line,s);
        //}

        public static IEnumerable<PropsSection> Load(string filename, StreamReader stream, CanStartAsScript isScript, bool displayPercentage)
        {
            var line = 0;
            MutablePropsSection curSection = null;
            MutableTriggerSection curTrigger = null; //these are also added to curSection...

            var streamLen = stream.BaseStream.Length;
            long lastSentPercentage = -1;
            var fileNameToDisplay = Path.GetFileName(filename);

            while (true)
            {
                var curLine = stream.ReadLine();
                line++;
                if (curLine != null)
                {
                    if (displayPercentage)
                    {
                        var currentPercentage = (stream.BaseStream.Position * 100) / streamLen;
                        if (currentPercentage > lastSentPercentage)
                        {
                            Logger.SetTitle(string.Concat("Loading ", fileNameToDisplay, ": ", currentPercentage.ToString(CultureInfo.InvariantCulture), "%"));
                            lastSentPercentage = currentPercentage;
                        }
                    }

                    curLine = curLine.Trim();
                    if ((curLine.Length == 0) || (curLine.StartsWith("//")))
                    {
                        //it is a comment or a blank line
                        if (curTrigger != null)
                        {
                            //in script compiler do also blank lines count, so we can`t ignore them.
                            curTrigger.code.AppendLine(curLine);
                        }//else the comment gets lost... :\
                        continue;
                    }
                    var m = headerRE.Match(curLine);
                    if (m.Success)
                    {
                        if (curSection != null)
                        {//send the last section
                            yield return curSection.CreateShieldedTriggerSection();
                        }
                        var gc = m.Groups;
                        curSection = new MutablePropsSection(filename, gc["type"].Value, gc["name"].Value, line, gc["comment"].Value);
                        if (isScript(curSection.headerType))
                        {
                            //if it is something like [function xxx]
                            curTrigger = new MutableTriggerSection(filename, line, curSection.headerType, curSection.headerName, gc["comment"].Value);
                            curSection.triggerSections.Add(curTrigger);
                        }
                        else
                        {
                            curTrigger = null;
                        }
                        continue;
                    }
                    m = triggerRE.Match(curLine);
                    //on=@blah
                    if (m.Success)
                    {
                        //create a new triggersection
                        var gc = m.Groups;
                        curTrigger = new MutableTriggerSection(filename, line, gc["triggerkey"].Value, gc["triggername"].Value, gc["comment"].Value);
                        if (curSection == null)
                        {
                            //a trigger section without real section?
                            Logger.WriteWarning(filename, line, "No section for this trigger...?");
                        }
                        else
                        {
                            //Console.WriteLine("Trigger section: " + curTrigger.TriggerName);
                            curSection.triggerSections.Add(curTrigger);
                        }
                        continue;
                    }
                    if (curTrigger != null)
                    {
                        if (curSection != null)
                        {
                            curTrigger.code.AppendLine(curLine);
                        }
                        else
                        {
                            //this shouldnt be, a trigger without section...?
                            Logger.WriteWarning(filename, line, "Skipping line '" + curLine + "'.");
                        }
                        continue;
                    }
                    m = LocStringCollection.valueRE.Match(curLine);
                    if (m.Success)
                    {
                        if (curSection != null)
                        {
                            var gc = m.Groups;
                            curSection.AddPropsLine(gc["name"].Value, gc["value"].Value, line, gc["comment"].Value);
                        }
                        else
                        {
                            //this shouldnt be, a property without header...?
                            Logger.WriteWarning(filename, line, "No section for this value. Skipping line '" + curLine + "'.");
                        }
                        continue;
                    }
                    Logger.WriteError(filename, line, "Unrecognizable data '" + curLine + "'.");
                }
                else
                {
                    //end of file
                    if (curSection != null)
                    {
                        yield return curSection.CreateShieldedTriggerSection();
                    }
                    break;
                }
            } //end of (while (true)) - for each line of the file

            if (displayPercentage)
            {
                Logger.SetTitle("");
            }
        }

        private class MutableTriggerSection
        {
            private readonly string triggerComment;
            private readonly string triggerKey; //"on", "ontrigger", "onbutton", or just ""
            private readonly int startLine;
            private readonly string triggerName;    //"create", etc
            private readonly string filename;
            public StringBuilder code; //code

            internal MutableTriggerSection(string filename, int startline, string key, string name, string comment)
            {
                this.filename = filename;
                this.triggerKey = key;
                this.triggerName = name;
                this.startLine = startline;
                this.code = new StringBuilder();
                this.triggerComment = comment;
            }

            public TriggerSection CreateImmutableTriggerSection()
            {
                return new TriggerSection(filename: this.filename, startline: this.startLine,
                    key: this.triggerKey, name: this.triggerName, comment: this.triggerComment, code: this.code.ToString());
            }
        }

        private class MutablePropsSection
        {
            public readonly string headerComment;
            public readonly string headerType;
            public string headerName;//[headerType headerName]
            public readonly int headerLine;
            public readonly string filename;
            public readonly Dictionary<string, PropsLine> props = new Dictionary<string, PropsLine>(StringComparer.OrdinalIgnoreCase);
            public readonly List<MutableTriggerSection> triggerSections = new List<MutableTriggerSection>();

            internal MutablePropsSection(string filename, string type, string name, int line, string comment)
            {
                this.filename = filename;
                this.headerType = type;
                this.headerName = name;
                this.headerLine = line;
                this.headerComment = comment;
            }

            internal void AddPropsLine(string name, string value, int line, string comment)
            {
                var p = new PropsLine(name, value, line, comment);
                var origKey = name;
                var key = origKey;
                for (var a = 0; this.props.ContainsKey(key); a++)
                {
                    key = origKey + a.ToString(CultureInfo.InvariantCulture);
                    //duplicite properties get a counted name
                    //like if there is more "events=..." lines, they are in the hashtable with keys
                    //events, events0, events1, etc. 
                    //these entries wont be probably looked up by their name anyways.
                }
                this.props[key] = p;
            }

            public override string ToString()
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "[{0} {1}]", this.headerType, this.headerName);
            }

            public PropsSection CreateShieldedTriggerSection()
            {
                return new PropsSection(filename: this.filename, type: this.headerType, name: this.headerName, line: this.headerLine, comment: this.headerComment,
                    propLines: this.props, sections: this.triggerSections.Select(t => t.CreateImmutableTriggerSection()));
            }
        }
    }
}
