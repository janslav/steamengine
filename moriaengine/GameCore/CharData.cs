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
using System.IO;
using SteamEngine.Common;

namespace SteamEngine {
	class CharData {
		private static Dictionary<int, bool> knownDispids = new Dictionary<int, bool>();

		public static void AddDispid(int uid) {
			knownDispids[uid] = true;
		}

		public static bool Exists(int num) {
			//Sanity.IfTrueThrow(num>ThingDef.MaxCharModels, "CharData.Exists("+num+") called: "+num+" is an invalid value (valid values are 0-"+ThingDef.MaxCharModels+")");
			return knownDispids.ContainsKey(num);
		}

		public static void GenerateMissingDefs() {
			if (Globals.readBodyDefs) {
				StreamReader sr;
				string fPath = Path.Combine(Globals.mulPath, "Bodyconv.def");
				if (File.Exists(fPath)) {
					sr = new StreamReader(fPath);
				} else {
					Logger.WriteCritical("Unable to locate Bodyconv.def.");
					Globals.readBodyDefs = false;
					return;
				}

				string chardefsPath = Tools.CombineMultiplePaths(Globals.scriptsPath, "defaults", "chardefs");
				Tools.EnsureDirectory(chardefsPath, true);

				StreamWriter scr = File.AppendText(Path.Combine(chardefsPath, "newCharDefsFromMuls.def"));
				Console.WriteLine("Checking " + LogStr.File("Bodyconv.def") + " to find character models for which we lack chardefs.");
				int numWritten = 0;
				while (true) {
					string oline = sr.ReadLine();
					string line = oline;
					if (line == null) break;
					int comment = line.IndexOf("#");
					if (comment > -1) {
						line = line.Substring(0, comment);
					}
					line = line.Trim();
					if (line.Length > 0) {
						string[] args = Utility.SplitSphereString(line);
						if (args.Length != 4) {
							Logger.WriteWarning("Bodyconv.def contains a line in an unexpected format. That line is '" + line + "'");
							break;
						} else {
							int model = TagMath.ParseInt32(args[0]);
							if (!Exists(model)) {
								AddDispid(model);
								AbstractCharacterDef def = ThingDef.FindCharDef(model);
								if (def == null) {
									if (numWritten == 0) {
										scr.WriteLine("\n\n\n//-------------- Written " + DateTime.Now.ToString() + " --------------//\n");
									}
									numWritten++;
									scr.WriteLine("");
									scr.WriteLine("[Character 0x" + model.ToString("x") + "]");
									scr.WriteLine("model=0x" + model.ToString("x"));
									scr.WriteLine("name=Unknown");
								}
							}
						}
					}
				}
				scr.Close();
				if (numWritten > 0) {
					Console.WriteLine("Wrote " + numWritten + " basic chardefs (for which there were no chardefs in the scripts), to " + chardefsPath + "/newChardefs.def.");
				}
			}
		}
	}
}
