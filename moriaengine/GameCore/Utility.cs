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
using System.Collections;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SteamEngine.Common;

namespace SteamEngine {

	public static class Utility {

		/**
			Splits a string which may be comma-delimited or space-delimited. (Tabs are also allowed,
			and are treated exactly like spaces)

			This actually allows mixing commas, spaces, etc, but spaces adjacent to commas are trimmed,
			and multiple spaces one after another only count as one space, whereas multiple commas
			count separately. So a string like "foo   bar \t moo" (if \t were a tab) would be returned
			as {"foo", "bar", "moo"}. And a string like "foo,,,bar,\t moo" (if \t were a tab) would be
			returned as {"foo","","","bar","moo"} - nothingness between commas is returned as "".
		
			Spaces, tabs, and commas inside ""s are {}s are disregarded.
		*/
		public static string[] SplitSphereString(string input) {
			ArrayList results = new ArrayList();
			char lastChar = 'a';	//nothing meaningful
			bool inQuote = false;
			int inCurlyBraces = 0;
			int startPos = 0;
			for (int pos = 0; pos < input.Length; pos++) {
				char c = input[pos];
				if (c == '\t') c = ' ';
				if (c == '\"') {
					inQuote = !inQuote;
				} else if (!inQuote) {
					if (c == '{') {
						inCurlyBraces++;
					} else if (c == '}') {
						inCurlyBraces--;
						Sanity.IfTrueThrow(inCurlyBraces < 0, "Mismatched {}s.");
					} else if (inCurlyBraces == 0) {
						if (c == ',') {
							if (lastChar != ' ') {
								results.Add(input.Substring(startPos, pos - startPos).Trim());
								startPos = pos + 1;
							}
						} else if (c == ' ') {
							if (lastChar != ' ' && lastChar != ',') {
								results.Add(input.Substring(startPos, pos - startPos).Trim());
								startPos = pos + 1;
							}
						}
					}
				}
				lastChar = c;
			}
			if (startPos < input.Length) {
				results.Add(input.Substring(startPos).Trim());
			} else if (lastChar == ',') {
				results.Add("");
			}
			return (string[]) results.ToArray(typeof(string));
		}

		/**
			Capitalizes the first letter of the string. If the string is null, that is an error.
			If the string is "", nothing is done to it. Otherwise, the first letter is capitalized,
			and the modified string is returned.
		*/
		public static string Capitalize(string s) {
			if (s == null) throw new SanityCheckException("Capitalize was called on a null string.");
			if (s.Length > 1) {
				s = Char.ToUpper(s[0]) + s.Substring(1);
			} else if (s.Length == 1) {
				s = s.ToUpper();
			}
			return s;
		}
		/**
			Uncapitalizes the first letter of the string. If the string is null, that is an error.
			If the string is "", nothing is done to it. Otherwise, the first letter is uncapitalized,
			and the modified string is returned. (Uncapitalized meaning it is made lowercase)
		*/
		public static string Uncapitalize(string s) {
			if (s == null) throw new SanityCheckException("Uncapitalize was called on a null string.");
			if (s.Length > 1) {
				s = Char.ToLower(s[0]) + s.Substring(1);
			} else if (s.Length == 1) {
				s = s.ToLower();
			}
			return s;
		}

		/**
			Get null terminated ascii string from binary reader stream and return encoded string.
			The value count means number of bytes to get from binary reader which will be converted to string.
		*/
		public static string GetCAsciiString(BinaryReader br, int count) {
			byte[] buffer = br.ReadBytes(count);
			int len;
			for (len = 0; len < count && buffer[len] != 0; len++) ;
			return Encoding.ASCII.GetString(buffer, 0, len);
		}


		private static Regex uncommentRE = new Regex(@"^\s*(?<value>.*?)\s*(//(?<comment>.*))?$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		public static string UnComment(string input) {
			Match m = uncommentRE.Match(input);
			if (m.Success) {
				return m.Groups["value"].Value;
			}
			return input.Trim();
		}

		public static int NormalizeDyedColor(int color, int defaultColor) {
			if ((color < 2) || (color > 1001)) {
				return defaultColor;
			} else {
				return color;
			}
		}

		public static ClientFont NormalizeClientFont(ClientFont font) {
			if ((font < ClientFont.Server) || (font > ClientFont.BorderLess)) {
				return ClientFont.Unified;
			} else {
				return font;
			}
		}

		[Summary("Count the arithmetic mean of the given values")]
		public static double ArithmeticMean(params double[] values) {
			if (values.Length == 0)
				throw new SanityCheckException("ArithmeticMean called on no values");

			double sum = 0;
			foreach (double val in values) {
				sum += val;
			}
			return sum / values.Length;
		}
	}
}