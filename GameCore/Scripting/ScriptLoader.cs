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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Shielded;
using SteamEngine.Common;
using SteamEngine.Networking;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Scripting.Compilation;
using SteamEngine.Scripting.Interpretation;
using SteamEngine.Scripting.Objects;

namespace SteamEngine.Scripting {
	public static class ScriptLoader {
		private static readonly ScriptFileCollection allFiles = new ScriptFileCollection(Globals.ScriptsPath, ".scp", ".def");

		private static readonly ShieldedDictNc<string, RegisteredScript> scriptTypesByName =
			new ShieldedDictNc<string, RegisteredScript>(comparer: StringComparer.OrdinalIgnoreCase);

		//the method that is called on server initialisation by MainClass.
		internal static void Load() {
			ICollection<ScriptFile> files = allFiles.GetAllFiles();
			long lengthSum = allFiles.LengthSum;

			using (StopWatch.StartAndDisplay("Loading " + LogStr.Number(files.Count) + " *.def and *.scp script files. (" + LogStr.Number(lengthSum) + " bytes)...")) {

				ThingDef.StartingLoading();
				InterpretedTriggerGroup.StartingLoading();
				Constant.StartingLoading();
				Map.StartingLoading();
				//ObjectSaver.StartingLoading();
				AbstractSkillDef.StartingLoading();

				long loadedBytes = 0;

				//load either paralell or sequentially, depending on .ini setting
				Action<long> showLoaded = alreadyloaded => Logger.SetTitle("Loading scripts: " + ((alreadyloaded * 100) / lengthSum) + " %");

				if (Globals.ParallelStartUp) {
					loadedBytes = files.AsParallel().Aggregate(
						() => 0L,
						(alreadyloaded, sf) => {
							Logger.WriteDebug("Loading " + sf.Name);
							LoadFile(sf);
							return alreadyloaded + sf.Length;
						},
						(a, b) => {
							var alreadyloaded = a + b;
							showLoaded(alreadyloaded);
							return alreadyloaded;
						},
						a => a);
				} else {
					foreach (var sf1 in files) {
						Logger.WriteDebug("Loading " + sf1.Name);
						LoadFile(sf1);
						loadedBytes += sf1.Length;
						showLoaded(loadedBytes);
					}
				}

				Sanity.IfTrueSay(lengthSum != loadedBytes, "ScriptLoader.Load: lengthSum != loadedBytes");

				Logger.WriteDebug("Script files loaded.");
				Logger.SetTitle("");//reset title of the console

				//file = new ScriptFile(new FileInfo(Globals.scriptsPath+"\\defaults\\lscript.scp"));
				//LoadFile();

				Constant.LoadingFinished();
				AbstractItemDef.LoadingFinished();
				InterpretedTriggerGroup.LoadingFinished();
				Map.LoadingFinished();
				//ObjectSaver.LoadingFinished();
				AbstractSkillDef.LoadingFinished();
				AbstractDef.LoadingFinished();

				AbstractDefTriggerGroupHolder.LoadingFinished();
				InterpretedGumpDef.LoadingFinished();

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

				Sanity.IfTrueThrow(!RunLevelManager.IsRunning, "!RunLevelManager.IsRunning @ Resync");

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

				ObjectSaver.LoadingFinished();
				AbstractDef.LoadingFinished();

				AbstractDefTriggerGroupHolder.LoadingFinished();
				InterpretedGumpDef.LoadingFinished();
				//foreach (Thing t in Thing.AllThings) {
				//    t.region = null;
				//}

				RunLevelManager.UnsetStartup();

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

		private static void LoadFile(ScriptFile file) {
			var scripts = LoadScriptsFromFile(file);
			foreach (var script in scripts) {
				file.Add(script);
			}
		}

		private static IEnumerable<IUnloadable> LoadScriptsFromFile(ScriptFile file) {
			if (file.Exists) //this may not be true on rare circumstances (basically, delete script and recompile) not gonna do any better fix
			{
				using (StreamReader stream = file.OpenText()) {
					foreach (var script in PropsFileParser.Load(file.FullName, stream, StartsAsScript, false).SelectMany(LoadSection)) {
						yield return script;
					}
				}
			}
		}

		private static IEnumerable<IUnloadable> LoadSection(PropsSection section) {
			return SeShield.InTransaction(() => {
				try {
					string type = section.HeaderType.ToLowerInvariant();
					string name = section.HeaderName;
					if ((string.IsNullOrEmpty(name)) && (type == "eof")) {
						return Enumerable.Empty<IUnloadable>();
					}

					switch (type) {
						case "function":
							if (section.TriggerCount > 1) {
								Logger.WriteWarning(section.Filename, section.HeaderLine, "Triggers in a function are nonsensual (and ignored).");
							}
							return new[] { LScriptMain.LoadAsFunction(section.GetTrigger(0)) };
						case "typedef":
						case "triggergroup":
						case "events":
						case "event":
							return new[] { InterpretedTriggerGroup.Load(section) };
						case "defname":
						case "defnames":
						case "constants":
						case "constant":
							return Constant.Load(section);
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
							return new[] { InterpretedLocStringCollection.Load(section) };
						case "dialog":
						case "gump":
							IUnloadable gump = InterpretedGumpDef.Load(section);
							if (gump != null) {
								//it could have been a "subsection" of dialog, i.e. TEXT or BUTTON part
								return new[] { gump };
							}
							return Enumerable.Empty<IUnloadable>();
						default:
							RegisteredScript rs;
							if (scriptTypesByName.TryGetValue(type, out rs)) {
								return new[] { rs.deleg(section) };
							}

							Logger.WriteError(section.Filename, section.HeaderLine, "Unknown section " + LogStr.Ident(section));
							return Enumerable.Empty<IUnloadable>();
					}
				} catch (FatalException) {
					throw;
				} catch (TransException) {
					throw;
				} catch (Exception e) {
					Logger.WriteError(section.Filename, section.HeaderLine, e);
					return Enumerable.Empty<IUnloadable>();
				}
			});
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


		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
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
					InterpretedGumpDef.LoadingFinished();

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
			SeShield.AssertInTransaction();
			RegisteredScript scp;
			if (!scriptTypesByName.TryGetValue(name, out scp)) {
				scp = new RegisteredScript(deleg, startAsScript);
				scriptTypesByName.Add(name, scp);
			} else {
				if (scp.deleg.Method != deleg.Method) {
					throw new OverrideNotAllowedException("There is already a script section loader (" + LogStr.Ident(scp) +
														  ") registered for handling the section name " + LogStr.Ident(name));
				}
				scp.startAsScript = scp.startAsScript || startAsScript;
				//if any wants true, it stays true. This is here because of AbstractDef and TemplateDef... yeah not exactly clean
			}
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
			SeShield.AssertInTransaction();
			allFiles.Clear();

			Assembly coreAssembly = ClassManager.CoreAssembly;

			var origScripts = scriptTypesByName.ToArray();
			scriptTypesByName.Clear();
			foreach (KeyValuePair<string, RegisteredScript> pair in origScripts) {
				if (coreAssembly == pair.Value.deleg.Method.DeclaringType.Assembly) {
					scriptTypesByName[pair.Key] = pair.Value;
				}
			}
		}
	}
}
