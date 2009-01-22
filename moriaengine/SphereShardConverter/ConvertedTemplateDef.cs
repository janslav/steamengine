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
using System.Reflection;
using System.IO;
using System.Globalization;
using SteamEngine.Common;
using System.Configuration;
using System.Text.RegularExpressions;

namespace SteamEngine.Converter {
	public class ConvertedTemplateDef : ConvertedDef {

		bool hasNumericDefname = false;

		public ConvertedTemplateDef(PropsSection input)
			: base(input) {
		}

		public override void FirstStage() {
			int defnum;
			if (ConvertTools.TryParseInt32(headerName, out defnum)) {
				headerName = "td_0x" + defnum.ToString("x");
				hasNumericDefname = true;
			}
		}

		public override void SecondStage() {
			StringWriter writer = new StringWriter();
			StringReader reader = new StringReader(origData.GetTrigger(0).code.ToString());
			string line;
			int linenum = origData.headerLine;
			while ((line = reader.ReadLine()) != null) {
				linenum++;
				Match m = PropsFileParser.valueRE.Match(line);
				if (m.Success) {
					GroupCollection gc = m.Groups;
					string name = gc["name"].Value;
					string value = gc["value"].Value;
					string comment = gc["comment"].Value;

					switch (name.ToLower()) {
						case "defname":
							if (hasNumericDefname) {
								Info(linenum, "Ignoring the numeric defname of TemplateDef '" + value + "'.");
								headerName = value;
								//origData.headerComment = origData.headerComment+" // "+comment;
							} else {
								Set(name, value, comment);
							}
							break;
						case "category":
						case "subsection":
						case "description":
							Set(name, value, comment);
							break;
						default:
							writer.WriteLine(line);
							break;

					}
					//writer.WriteLine();
				} else {
					writer.WriteLine(line);
				}
			}

			reader = new StringReader(writer.GetStringBuilder().ToString());
			writer = new StringWriter();
			while ((line = reader.ReadLine()) != null) {
				Match m = PropsFileParser.valueRE.Match(line);
				if (m.Success) {
					GroupCollection gc = m.Groups;
					string name = gc["name"].Value;
					string value = gc["value"].Value;
					string comment = gc["comment"].Value;

					switch (name.ToLower()) {
						case "item":
						case "itemnewbie":
						case "buy":
						case "sell":

							Set(name, FixRandomExpression(value), comment);
							break;
					}
				}
			}
		}

		internal static string FixRandomExpression(string input) {
			while (true) {
				string temp = Regex.Replace(input, @"{(?<prefix>.*?)(?<first>[a-zA-Z0-9_-]+)\s+(?<second>[a-zA-Z0-9_-]+)(?<postfix>.*?)}",
					"{${prefix}${first}, ${second}${postfix}}");
				if (temp.Equals(input)) {
					break;
				}
				input = temp;
			}
			return input;
		}
	}
}