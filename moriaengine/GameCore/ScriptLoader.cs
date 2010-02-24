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
using System.Text.RegularExpressions;
using System.Text;
using SteamEngine.LScript;
using SteamEngine.Timers;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Networking;

namespace SteamEngine {
	public static class ScriptLoader {
		private static ScriptFileCollection allFiles = InitScpCollection();

		private static Dictionary<string, RegisteredScript> scriptTypesByName =
			new Dictionary<string, RegisteredScript>(StringComparer.OrdinalIgnoreCase);

		static ScriptFileCollection InitScpCollection() {
			ScriptFileCollection retVal = new ScriptFileCollection(Globals.ScriptsPath, ".scp");
			retVal.AddExtension(".def");
			//allFiles.AddAvoided("import");
			return retVal;
		}

		//the method that is called on server initialisation by MainClass.
		internal static void Load() {
			ICollection<ScriptFile> files = allFiles.GetAllFiles();
			long lengthSum = allFiles.LengthSum;
			long alreadyloaded = 0;
			using (StopWatch.StartAndDisplay("Loading " + LogStr.Number(files.Count) + " *.def and *.scp script files. (" + LogStr.Number(lengthSum) + " bytes)...")) {

				ThingDef.StartingLoading();
				ScriptedTriggerGroup.StartingLoading();
				Constant.StartingLoading();
				Map.StartingLoading();
				//ObjectSaver.StartingLoading();
				AbstractSkillDef.StartingLoading();

				foreach (ScriptFile f in files) {
					if (f.Exists) {
						Logger.SetTitle("Loading scripts: " + ((alreadyloaded * 100) / lengthSum) + " %");
						Logger.WriteDebug("Loading " + f.Name);
						LoadFile(f);
						alreadyloaded += f.Length;
					}
				}
				Logger.WriteDebug("Script files loaded.");
				Logger.SetTitle("");//reset title of the console

				//file = new ScriptFile(new FileInfo(Globals.scriptsPath+"\\defaults\\lscript.scp"));
				//LoadFile();

				Constant.LoadingFinished();
				ThingDef.LoadingFinished();
				ScriptedTriggerGroup.LoadingFinished();
				Map.LoadingFinished();
				//ObjectSaver.LoadingFinished();
				AbstractSkillDef.LoadingFinished();

				AbstractDefTriggerGroupHolder.LoadingFinished();
				ScriptedGumpDef.LoadingFinished();

				if (Globals.ResolveEverythingAtStart) {
					Constant.ResolveAll();
					AbstractDef.ResolveAll();
				}
			}
		}

		public static void Resync() {
			ICollection<ScriptFile> files = allFiles.GetChangedFiles();//this makes the entities unload
			if (files.Count > 0) {
				PacketSequences.BroadCast("Server is pausing for script resync...");

				Sanity.IfTrueThrow(!RunLevelManager.IsRunning, "RunLevel != Running @ Resync");

				Globals.PauseServerTime();
				RunLevelManager.SetStartup();

				ObjectSaver.StartingLoading();

				foreach (ScriptFile f in files) {
					if (f.Exists) {
						Console.WriteLine("Resyncing file '" + LogStr.File(f.FullName) + "'.");
						try {
							LoadFile(f);
						} catch (IOException e) {
							Logger.WriteWarning(e);
						}
					} else {
						f.Unload();
					}
				}

				if (Globals.ResolveEverythingAtStart) {
					Constant.ResolveAll();
					AbstractDef.ResolveAll();
				}

				AbstractDefTriggerGroupHolder.LoadingFinished();
				ScriptedGumpDef.LoadingFinished();
				ObjectSaver.LoadingFinished();

				//foreach (Thing t in Thing.AllThings) {
				//    t.region = null;
				//}

				Globals.UnPauseServerTime();
				RunLevelManager.SetRunning();

				PacketSequences.BroadCast("Script resync finished.");
			} else {
				ISrc src = Globals.Src;
				if (src != null) {
					src.WriteLine("No files to resync.");
				} else {
					Console.WriteLine("No files to resync.");
				}
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal static void LoadFile(ScriptFile file) {
			//string filepath = file.Name;
			//WorldSaver.currentfile = filepath;
			if (file.Exists) { //this may not be true on rare circumstances (basically, delete script and recompile) not gonna do any better fix

				using (StreamReader stream = file.OpenText()) {
					foreach (PropsSection section in PropsFileParser.Load(
							file.FullName, stream, new CanStartAsScript(StartsAsScript), false)) {

						try {
							string type = section.HeaderType.ToLowerInvariant();
							string name = section.HeaderName;
							if ((string.IsNullOrEmpty(name)) && (type == "eof")) {
								continue;
							}

							switch (type) {
								case "function":
									file.Add(SteamEngine.LScript.LScriptMain.LoadAsFunction(section.GetTrigger(0)));
									if (section.TriggerCount > 1) {
										Logger.WriteWarning(section.Filename, section.HeaderLine, "Triggers in a function are nonsensual (and ignored).");
									}
									continue;
								case "typedef":
								case "triggergroup":
								case "events":
								case "event":
									file.Add(ScriptedTriggerGroup.Load(section));
									continue;
								case "defname":
								case "defnames":
								case "constants":
								case "constant":
									foreach (Constant constant in Constant.Load(section)) {
										file.Add(constant);
									}
									continue;

								case "loc":
								case "localisation":
								case "language":
								case "languages":
								case "servloc":
								case "scriptloc":
								case "scriptedloc":
								case "scriptedloccollection":
								case "scriptedlocstringcollection":
								case "locstringcollection":
								case "locstrings":
									file.Add(ScriptedLocStringCollection.Load(section));
									continue;
								//case "template":
								//	
								//	//file.Add(ThingDef.LoadFromScripts(input));
								//	return null;
								case "dialog":
								case "gump":
									IUnloadable gump = ScriptedGumpDef.Load(section);
									if (gump != null) {//it could have been a "subsection" of dialog, i.e. TEXT or BUTTON part
										file.Add(gump);
									}
									continue;
								//case "skill":
								//	Skills.Load(input);
								//	return null;
								default:
									RegisteredScript rs;
									if (scriptTypesByName.TryGetValue(type, out rs)) {
										file.Add(rs.deleg(section));
										continue;
									}

									break;
							}
						} catch (FatalException) {
							throw;
						} catch (Exception e) {
							Logger.WriteError(section.Filename, section.HeaderLine, e);
							continue;
						}
						Logger.WriteError(section.Filename, section.HeaderLine, "Unknown section " + LogStr.Ident(section));
					}
				}
			}
		}

		internal static bool StartsAsScript(string headerType) {
			switch (headerType.ToLowerInvariant()) {
				case "function":
				case "dialog":
				case "gump":
				case "defname":
				case "defnames":
				case "constants":
					return true;
			}
			RegisteredScript rs;
			if (scriptTypesByName.TryGetValue(headerType, out rs)) {
				return rs.startAsScript;
			}
			return false;
		}


		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal static void LoadNewFile(string filename) {
			FileInfo fi = new FileInfo(filename);
			if (!fi.Exists) {
				try {
					fi = new FileInfo(Path.Combine(Globals.ScriptsPath, filename));
				} catch { }
			}

			if (fi.Exists) {
				if (!allFiles.HasFile(fi)) {
					PacketSequences.BroadCast("Server is pausing for script file loading...");
					Globals.PauseServerTime();

					ScriptFile sf = allFiles.AddFile(fi);
					Console.WriteLine("Loading " + LogStr.File(fi.FullName));
					LoadFile(sf);

					AbstractDefTriggerGroupHolder.LoadingFinished();
					ScriptedGumpDef.LoadingFinished();

					Globals.UnPauseServerTime();
					PacketSequences.BroadCast("Script loading finished.");
				} else {
					throw new SEException("This file is already loaded.");
				}
			} else {
				throw new SEException("Such file does not exist.");
			}
		}

		public static void RegisterScriptType(string name, LoadSection deleg, bool startAsScript) {
			RegisteredScript rs;
			if (scriptTypesByName.TryGetValue(name, out rs)) {
				if (rs.deleg.Method != deleg.Method) {
					throw new OverrideNotAllowedException("There is already a script section loader (" + LogStr.Ident(rs) + ") registered for handling the section name " + LogStr.Ident(name));
				} else {
					rs.startAsScript = rs.startAsScript || startAsScript; //if any wants true, it stays true. This is here because of AbstractDef and TemplateDef... yeah not exactly clean
					return;
				}
			}
			scriptTypesByName[name] = new RegisteredScript(deleg, startAsScript);
		}

		public static void RegisterScriptType(string[] names, LoadSection deleg, bool startAsScript) {
			foreach (string name in names) {
				RegisterScriptType(name, deleg, startAsScript);
			}
		}

		private class RegisteredScript {
			internal LoadSection deleg;
			internal bool startAsScript;

			internal RegisteredScript(LoadSection deleg, bool startAsScript) {
				this.deleg = deleg;
				this.startAsScript = startAsScript;
			}
		}

		//forgets stuff that come from scripts.
		internal static void ForgetScripts() {
			allFiles.Clear();

			Assembly coreAssembly = CompiledScripts.ClassManager.CoreAssembly;

			Dictionary<string, RegisteredScript> origScripts = scriptTypesByName;
			scriptTypesByName = new Dictionary<string, RegisteredScript>(StringComparer.OrdinalIgnoreCase);
			foreach (KeyValuePair<string, RegisteredScript> pair in origScripts) {
				if (coreAssembly == pair.Value.deleg.Method.DeclaringType.Assembly) {
					scriptTypesByName[pair.Key] = pair.Value;
				}
			}
		}
	}
}
