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
using SteamEngine.Timers;
using SteamEngine.Common;

namespace SteamEngine.Persistence {
	public delegate IUnloadable LoadSection(PropsSection data);
	public delegate bool CanStartAsScript(string data);
	
	public class WorldSaver {

		//internal static string currentfile;
		//public static string CurrentFile {
		//    get {
		//        return currentfile;
		//    }
		//}
		
		public static void Save() {
			Server.BroadCast("Server is pausing for worldsave...");
			Globals.PauseServerTime();
			WeakRefDictionaryUtils.PurgeAll();

			//-1 because we will save new files now.
			if (!TrySave()) {
				Server.BroadCast("Saving failed!");//we do not throw an exception to kill the server, 
				//the admin should do that, after he tries to fix the problem somehow...
			} else {
				Server.BroadCast("Saving finished.");
			}

			Globals.UnPauseServerTime();
		}
		
		static bool TrySave() {
			string path = Globals.savePath;
			
			ScriptArgs sa = new ScriptArgs(path);
			Globals.instance.TryTrigger(TriggerKey.beforeSave, sa);
			path = string.Concat(sa.Argv[0]);
			if (path.Length < 1) {//scripts error or something...
				path = Globals.savePath;
			}

			
			Tools.EnsureDirectory(path, true);

			ObjectSaver.StartingSaving();
			
			bool success = false;
			try {
				sa = new ScriptArgs(path, "things");
				Globals.instance.TryTrigger(TriggerKey.openSaveStream, sa);
				thingsSaver = GetSaveStream(path, sa.Argv[1]);
				//currentfile = "things.sav";//this may not be exact, it can be any other file, but it's for user info only anyway.
				Thing.SaveAll(thingsSaver);
				
				sa = new ScriptArgs(path, "accounts");
				Globals.instance.TryTrigger(TriggerKey.openSaveStream, sa);
				accountsSaver = GetSaveStream(path, sa.Argv[1]);
				//currentfile = "accounts.sav";
				GameAccount.SaveAll(accountsSaver);
				
				sa = new ScriptArgs(path, "globals");
				Globals.instance.TryTrigger(TriggerKey.openSaveStream, sa);
				globalsSaver = GetSaveStream(path, sa.Argv[1]);
				//currentfile = "globals.sav";
				Globals.SaveGlobals(globalsSaver);
				Region.SaveRegions(globalsSaver);
				globalsSaver.WriteLine("[EOF]");
				success = true;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				Logger.WriteCritical(e);
			} finally {
				Globals.instance.TryTrigger(TriggerKey.afterSave, new ScriptArgs(path, success));
				CloseSaveStreams();
			}
			
			ObjectSaver.SavingFinished();
			return success;
		}
		
		static SaveStream GetSaveStream(string path, object file) {
			//object file is either string or Stream or TextWriter
			TextWriter tw = file as TextWriter;
			if (tw != null) {
				//WriteLine("GetSaveStream got TextWriter: "+tw);
				return new SaveStream(tw);
			}
			Stream s = file as Stream;
			if (s != null) {
				//Console.WriteLine("GetSaveStream got Stream: "+s);
				return new SaveStream(new StreamWriter(s));
			}
			string filename = string.Concat(file);
			string filepath = Path.Combine(path, filename+".sav");
			return new SaveStream(new StreamWriter(File.Create(filepath)));
		}
		
		private static SaveStream thingsSaver;
		private static SaveStream accountsSaver;
		private static SaveStream globalsSaver;
		
		static void CloseSaveStreams() {
			try {
				thingsSaver.Close(); 
			} catch (Exception e) {
				Logger.WriteDebug(e);
			}
			try {
				accountsSaver.Close(); 
			} catch (Exception e) {
				Logger.WriteDebug(e);
			}
			try {
				globalsSaver.Close(); 
			} catch (Exception e) {
				Logger.WriteDebug(e);
			}
		}
		
////////////////////////////////////////////////////////////////////////////////

		public static void Load() {
			while (!TryLoad()) {
			}
		}
		
		private static bool TryLoad() {
			Thing.StartingLoading();
			GameAccount.StartingLoading();
			Timer.StartingLoading();
			ObjectSaver.StartingLoading();
			Region.StartingLoading();
			
			string path = "";
						
			try {
				path = Globals.savePath;
				ScriptArgs sa = new ScriptArgs(path);
				Globals.instance.Trigger(TriggerKey.beforeLoad, sa);
				path = string.Concat(sa.Argv[0]);
				if (path.Length < 1) {//scripts error or something...
					path = Globals.savePath;
				}
				Tools.EnsureDirectory(path, true);
				
				sa = new ScriptArgs(path, "things");
				Globals.instance.Trigger(TriggerKey.openLoadStream, sa);
				thingsLoader = GetLoadStream(path, sa.Argv[1]);
				InvokeLoad(thingsLoader, Path.Combine(path, "things.sav"));
				
				sa = new ScriptArgs(path, "accounts");
				Globals.instance.Trigger(TriggerKey.openLoadStream, sa);
				accountsLoader = GetLoadStream(path, sa.Argv[1]);
				InvokeLoad(accountsLoader, Path.Combine(path, "accounts.sav"));
				
				sa = new ScriptArgs(path, "globals");
				Globals.instance.Trigger(TriggerKey.openLoadStream, sa);
				globalsLoader = GetLoadStream(path, sa.Argv[1]);
				InvokeLoad(globalsLoader, Path.Combine(path, "globals.sav"));

				//throw new Exception("simulated load fail");
				Console.WriteLine("Loading successful");
			} catch (FileNotFoundException e) {
				//this is not critical, just a warning.
				Logger.WriteWarning(e.Message);
				Logger.WriteWarning("No proper save files or backups found; Starting with a blank worldfile, no accounts, etc. The first account to log in will be set as admin.");
				MainClass.ClearWorld();
				TriggerGroup.ReAddGlobals();
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				Logger.WriteCritical(e);
				Logger.WriteCritical("Loading failed.");
				MainClass.ClearWorld();
				TriggerGroup.ReAddGlobals();
				Globals.instance.TryTrigger(TriggerKey.afterLoad, new ScriptArgs(path, true));
				return false;
			} finally {
				CloseLoadStreams();
			}

			ObjectSaver.LoadingFinished();
			GameAccount.LoadingFinished();
			Timer.LoadingFinished();
			Region.LoadingFinished();
			Thing.LoadingFinished();//Things must come after regions
			return true;
		}
		
		private static void InvokeLoad(TextReader stream, string filename) {
			//currentfile = filename;
			EOFMarked = false;
			PropsFileParser.Load(filename, stream, new LoadSection(LoadSection), new CanStartAsScript(StartsAsScript));
		}
		
		public static bool StartsAsScript(string headerType) {
			return false;
		}
		
		private static bool EOFMarked;
		
		private static IUnloadable LoadSection(PropsSection input) {
			if (input == null) {
				if (!EOFMarked) {
					throw new Exception("EOF Marker not reached!");
				}
				return null;
			}
			string type = input.headerType.ToLower();
			string name = input.headerName;
			if (EOFMarked) {
				Logger.WriteWarning(input.filename,input.headerLine,"[EOF] reached. Skipping "+input);
				return null;
			}
			if (name == "") {
				if (type == "eof") {
					EOFMarked = true;
					return null;
				} else {
					GameAccount.Load(input);
					return null;
				}
			}
			if (type == "globals") {
				Globals.LoadGlobals(input);
				return null;
			} else if (ThingDef.ExistsThingSubtype(type)) {
				Thing.Load(input);
				return null;
			} else if (Timer.IsTimerName(type)) {
				Timer.Load(input);
				return null;
			} else if (ObjectSaver.IsKnownSectionName(type)) {
				ObjectSaver.LoadSection(input);
				return null;
			} else if (AbstractDef.ExistsDefType(type)) {
				AbstractDef.LoadSectionFromSaves(input);
				return null;
			} else if (Region.IsRegionHeaderName(type)) {
				Region.Load(input);
				return null;
			}
			Logger.WriteError(input.filename,input.headerLine,"Unknown section "+LogStr.Ident(input));
			return null;
		}
		
		static TextReader GetLoadStream(string path, object file) {
			//object file is either string or Stream or TextWriter
			TextReader tr = file as TextReader;
			if (tr != null) {
				return tr;
			}
			Stream s = file as Stream;
			if (s != null) {
				return new StreamReader(s);
			}
			string filename = string.Concat(file);
			string filepath = Path.Combine(path, filename+".sav");
			return new StreamReader(File.OpenRead(filepath));
		}

		private static TextReader thingsLoader;
		private static TextReader accountsLoader;
		private static TextReader globalsLoader;
		
		static void CloseLoadStreams() {
			try {
				thingsLoader.Close(); 
			} catch { }
			try {
				accountsLoader.Close(); 
			} catch { }
			try {
				globalsLoader.Close(); 
			} catch { }
		}
	}
}