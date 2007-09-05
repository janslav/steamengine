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
	
namespace SteamEngine {
	public class ScriptLoader {
		private static ScriptFile file;
		
		private static ScriptFileCollection allFiles;

		private static Dictionary<string, RegisteredScript> scriptTypesByName = 
			new Dictionary<string, RegisteredScript>(StringComparer.OrdinalIgnoreCase);
		
		static ScriptLoader() {
			allFiles = new ScriptFileCollection(Globals.scriptsPath, ".scp");
			allFiles.AddExtension(".def");
			allFiles.AddAvoided("import");
            /*
             *For low level development purposes - disabling all Moria scripts for now (which will
             *make the rebuilding much much faster avoiding all those "kecy_a_drby.def"s), 
             *if you need some of the old imported scripts then place it to the "converted_used"
             *directory.
             *When all development will be finished, the converted directory will be made
             *available again.
             */
			allFiles.AddAvoided("converted");            
		}
		
		//the method that is called on server initialisation by MainClass.
		internal static void Load() {
			ThingDef.StartingLoading();
			ScriptedTriggerGroup.StartingLoading();
			Constant.StartingLoading();
			Map.StartingLoading();
			//ObjectSaver.StartingLoading();
			AbstractSkillDef.StartingLoading();
			
			
			ICollection<ScriptFile> files = allFiles.GetAllFiles();
			long lengthSum = allFiles.LengthSum;
			long alreadyloaded = 0;
			Console.WriteLine("Loading "+LogStr.Number(files.Count)+" *.def and *.scp script files. ("+LogStr.Number(lengthSum)+" bytes)");
			foreach (ScriptFile f in files) {
				Logger.SetTitle("Loading scripts: "+((alreadyloaded*100)/lengthSum)+" %");
				Logger.WriteDebug("Loading "+f.Name);
				LoadFile(f);
				alreadyloaded += f.Length;
			}
			Console.WriteLine("Script files loaded.");
			Logger.SetTitle("");//reset title of the console
			
			//file = new ScriptFile(new FileInfo(Globals.scriptsPath+"\\defaults\\lscript.scp"));
			//LoadFile();
			
			Constant.LoadingFinished();
			ThingDef.LoadingFinished();
			ScriptedTriggerGroup.LoadingFinished();
			Map.LoadingFinished();
			//ObjectSaver.LoadingFinished();
			AbstractSkillDef.LoadingFinished();
			
			DelayedResolver.ResolveAll();
			
			if (Globals.resolveEverythingAtStart) {
				Constant.ResolveAll();
				AbstractDef.ResolveAll();
			}
		}
		
		public static void Resync() {
			ICollection<ScriptFile> files = allFiles.GetChangedFiles();//this makes the entities unload
			if (files.Count > 0) {
				Server.BroadCast("Server is pausing for script resync...");
				MainClass.SetRunLevel(RunLevels.Paused);
				Globals.PauseServerTime();
				
				bool loadingWas = MainClass.loading;
				MainClass.loading = true;
				ObjectSaver.StartingLoading();
				
				foreach (ScriptFile f in files) {
					if (f.Exists) {
						Console.WriteLine("Resyncing file '"+LogStr.File(f.FullName)+"'.");
						try {
							LoadFile(f);
						} catch (IOException e) {
							Logger.WriteWarning(e);
						}
					}
				}

				MainClass.loading = loadingWas;
				
				if (Globals.resolveEverythingAtStart) {
					Constant.ResolveAll();
					AbstractDef.ResolveAll();
				}
				
				DelayedResolver.ResolveAll();
				ObjectSaver.LoadingFinished();

				foreach (Thing t in Thing.AllThings) {
					t.region = null;
				}
				
				MainClass.SetRunLevel(RunLevels.Running);
				Globals.UnPauseServerTime();
				Server.BroadCast("Script resync finished.");
			} else {
				ISrc src = Globals.Src;
				if (src != null) {
					src.WriteLine("No files to resync.");
				} else {
					Console.WriteLine("No files to resync.");
				}
			}
		}

		internal static void LoadFile(ScriptFile f) {
			file = f;
			//string filepath = file.Name;
			//WorldSaver.currentfile = filepath;
			if (file.Exists) { //this may not be true on rare circumstances (basically, delete script and recompile) not gonna do any better fix
				using (StreamReader stream = file.OpenText()) {
					PropsFileParser.Load(file.FullName, stream, new LoadSection(LoadSection), new CanStartAsScript(StartsAsScript));
				}
			}
		}

		internal static bool StartsAsScript(string headerType) {
			switch (headerType.ToLower()) {
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
		
		private static IUnloadable LoadSection(PropsSection input) {
			if (input == null) {
				return null;
			}
			try {
				string type = input.headerType.ToLower();
				string name = input.headerName;
				if ((name == "")&&(type == "eof")) {
					return null;
				}
				
				switch (type) {
					case "function":
						file.Add(SteamEngine.LScript.LScript.LoadAsFunction(input.GetTrigger(0)));
						if (input.TriggerCount>1) {
							Logger.WriteWarning(input.filename, input.headerLine, "Triggers in a function are nonsensual (and ignored).");
						}
						return null;
					case "typedef":
					case "triggergroup":
					case "events":
					case "event":
						file.Add(ScriptedTriggerGroup.Load(input));
						return null;
					case "defname":
					case "defnames":
					case "constants":
						foreach (Constant constant in Constant.Load(input)) {
							file.Add(constant);
						}
						return null;
					//case "template":
					//	
					//	//file.Add(ThingDef.LoadFromScripts(input));
					//	return null;
					case "dialog":
					case "gump":
						IUnloadable gump = ScriptedGump.Load(input);
						if (gump != null) {//it could have been a "subsection" of dialog, i.e. TEXT or BUTTON part
							file.Add(gump);
						}
						return null;
					//case "skill":
					//	Skills.Load(input);
					//	return null;
					default:
						//"itemdef", "characterdef", etc.
						if (string.Compare(type, "chardef", true) == 0) {
							type = "CharacterDef";
						}
						if (ThingDef.ExistsDefType(type)) {
							file.Add(ThingDef.LoadFromScripts(input));
							return null;
						}
						if (PluginDef.ExistsDefType(type)) {
							file.Add(PluginDef.LoadFromScripts(input));
							return null;
						}
						if (AbstractSkillDef.ExistsDefType(type)) {
							file.Add(AbstractSkillDef.LoadFromScripts(input));
							return null;
						}
						RegisteredScript rs;
						if (scriptTypesByName.TryGetValue(type, out rs)) {
							file.Add(rs.deleg(input));
							return null;
						}
						
						break;
				}
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				Logger.WriteError(input.filename, input.headerLine, e);
				return null;
			}
			Logger.WriteError(input.filename,input.headerLine,"Unknown section "+LogStr.Ident(input));
			return null;
		}
		
		internal static void LoadNewFile(string filename) {
			FileInfo fi = new FileInfo(filename);
			if (!fi.Exists) {
				try {
					fi = new FileInfo(Path.Combine(Globals.scriptsPath, filename));
				} catch { }
			}

			if (fi.Exists) {
				if (!allFiles.HasFile(fi)) {
					Server.BroadCast("Server is pausing for script file loading...");
					MainClass.SetRunLevel(RunLevels.Paused);
					
					ScriptFile sf = allFiles.AddFile(fi);
					Console.WriteLine("Loading "+LogStr.File(fi.FullName));
					LoadFile(sf);
					
					DelayedResolver.ResolveAll();
					MainClass.SetRunLevel(RunLevels.Running);
					Server.BroadCast("Script loading finished.");
				} else {
					throw new Exception("This file is already loaded.");
				}
			} else {
				throw new Exception("Such file does not exist.");
			}
		}
		
		public static void RegisterScriptType(string name, LoadSection deleg, bool startAsScript) {
			RegisteredScript rs;
			if (scriptTypesByName.TryGetValue(name, out rs)) {
				throw new OverrideNotAllowedException("There is already a script section loader ("+LogStr.Ident(rs)+") registered for handling the section name "+LogStr.Ident(name));  
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
		
		//unloads instances that come from scripts.
		internal static void UnloadScripts() {
			Assembly coreAssembly = CompiledScripts.ClassManager.CoreAssembly;

			Dictionary<string, RegisteredScript> origScripts = scriptTypesByName;
			scriptTypesByName = new Dictionary<string,RegisteredScript>(StringComparer.OrdinalIgnoreCase);
			foreach (KeyValuePair<string, RegisteredScript> pair in origScripts) {
				if (coreAssembly == pair.Value.deleg.Method.DeclaringType.Assembly) {
					scriptTypesByName[pair.Key] = pair.Value;
				}
			}
		}
	}
}
