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
using System.Collections.Generic;
using System.IO;
using SteamEngine.Timers;
using SteamEngine.Common;
using SteamEngine.Networking;

namespace SteamEngine.Persistence {
	public delegate IUnloadable LoadSection(PropsSection data);
	public delegate bool CanStartAsScript(string data);

	public static class WorldSaver {

		//internal static string currentfile;
		//public static string CurrentFile {
		//    get {
		//        return currentfile;
		//    }
		//}

		internal static bool Save() {

			using (StopWatch.StartAndDisplay("Saving world data...")) {
				PacketSequences.BroadCast("Server is pausing for worldsave...");

				WeakRefDictionaryUtils.PurgeAll();

				//-1 because we will save new files now.
				if (!TrySave()) {
					PacketSequences.BroadCast("Saving failed!");//we do not throw an exception to kill the server, 
					//the admin should do that, after he tries to fix the problem somehow...
					return false;
				} else {
					PacketSequences.BroadCast("Saving finished.");
					return true;
				}
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		static bool TrySave() {
			string path = Globals.SavePath;

			ScriptArgs sa = new ScriptArgs(path);
			Globals.Instance.TryTrigger(TriggerKey.beforeSave, sa);
			path = string.Concat(sa.Argv[0]);
			if (path.Length < 1) {//scripts error or something...
				path = Globals.SavePath;
			}


			Tools.EnsureDirectory(path, true);

			ObjectSaver.StartingSaving();

			bool success = false;
			try {
				foreach (IBaseClassSaveCoordinator coordinator in ObjectSaver.AllCoordinators) {
					string name = coordinator.FileNameToSave;
					sa = new ScriptArgs(path, name);
					Globals.Instance.TryTrigger(TriggerKey.openSaveStream, sa);
					SaveStream saveStream = GetSaveStream(path, sa.Argv[1]);
					saveStream.WriteComment("Textual SteamEngine save");
					coordinator.SaveAll(saveStream);
					saveStream.WriteLine("[EOF]");
					try {
						saveStream.Close();
					} catch { }
				}

				sa = new ScriptArgs(path, "globals");
				Globals.Instance.TryTrigger(TriggerKey.openSaveStream, sa);
				globalsSaver = GetSaveStream(path, sa.Argv[1]);
				//currentfile = "globals.sav";
				Globals.SaveGlobals(globalsSaver);
				//Region.SaveRegions(globalsSaver);
				globalsSaver.WriteLine("[EOF]");
				success = true;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				Logger.WriteCritical(e);
			} finally {
				Globals.Instance.TryTrigger(TriggerKey.afterSave, new ScriptArgs(path, success));
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
			string filepath = Path.Combine(path, filename + ".sav");
			return new SaveStream(new StreamWriter(File.Create(filepath)));
		}

		private static SaveStream globalsSaver;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		static void CloseSaveStreams() {
			try {
				globalsSaver.Close();
			} catch (Exception e) {
				Logger.WriteDebug(e);
			}
		}

		////////////////////////////////////////////////////////////////////////////////

		public static void Load() {
			using (StopWatch.StartAndDisplay("Loading world data...")) {
				while (!TryLoad()) {
				}
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		private static bool TryLoad() {
			Timer.StartingLoading();
			ObjectSaver.StartingLoading();

			string path = "";

			try {
				path = Globals.SavePath;
				ScriptArgs sa = new ScriptArgs(path);
				Globals.Instance.Trigger(TriggerKey.beforeLoad, sa);
				path = string.Concat(sa.Argv[0]);
				if (path.Length < 1) {//scripts error or something...
					path = Globals.SavePath;
				}
				Tools.EnsureDirectory(path, true);

				HashSet<string> nameSet = new HashSet<string>();
				foreach (IBaseClassSaveCoordinator coordinator in ObjectSaver.AllCoordinators) {
					string name = coordinator.FileNameToSave;
					if (nameSet.Contains(name.ToLower(System.Globalization.CultureInfo.InvariantCulture))) {
						//we already loaded this file.
						continue;
					} else {
						nameSet.Add(name.ToLower(System.Globalization.CultureInfo.InvariantCulture));
					}
					sa = new ScriptArgs(path, name);
					Globals.Instance.Trigger(TriggerKey.openLoadStream, sa);
					StreamReader loadStream = GetLoadStream(path, sa.Argv[1]);
					InvokeLoad(loadStream, Path.Combine(path, name + ".sav"));
					try {
						loadStream.Close();
					} catch { }
				}

				sa = new ScriptArgs(path, "globals");
				Globals.Instance.Trigger(TriggerKey.openLoadStream, sa);
				globalsLoader = GetLoadStream(path, sa.Argv[1]);
				InvokeLoad(globalsLoader, Path.Combine(path, "globals.sav"));
				Globals.Instance.TryTrigger(TriggerKey.afterLoad, new ScriptArgs(path, true));
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
				Globals.Instance.TryTrigger(TriggerKey.afterLoad, new ScriptArgs(path, false));
				return false;
			} finally {
				CloseLoadStreams();
			}

			ObjectSaver.LoadingFinished();
			//GameAccount.LoadingFinished();
			Timer.LoadingFinished();
			//Region.LoadingFinished();
			//Thing.LoadingFinished();//Things must come after regions
			return true;
		}

		private static void InvokeLoad(StreamReader stream, string filename) {
			//currentfile = filename;
			EOFMarked = false;
			foreach (PropsSection section in PropsFileParser.Load(
					filename, stream, new CanStartAsScript(StartsAsScript), true)) {

				string type = section.HeaderType.ToLower(System.Globalization.CultureInfo.InvariantCulture);
				string name = section.HeaderName;
				if (EOFMarked) {
					Logger.WriteWarning(section.Filename, section.HeaderLine, "[EOF] reached. Skipping " + section);
					continue;
				}
				if (string.IsNullOrEmpty(name)) {
					if (type == "eof") {
						EOFMarked = true;
						continue;
						//} else {
						//    GameAccount.Load(input);
						//    return null;
					}
				}
				if (type == "globals") {
					Globals.LoadGlobals(section);
					continue;
				} else if (ObjectSaver.IsKnownSectionName(type)) {
					ObjectSaver.LoadSection(section);
					continue;
				} else if (AbstractDef.ExistsDefType(type)) {
					AbstractDef.LoadSectionFromSaves(section);
					continue;
				}
				Logger.WriteError(section.Filename, section.HeaderLine, "Unknown section " + LogStr.Ident(section));
			}
			if (!EOFMarked) {
				throw new SEException("EOF Marker not reached!");
			}
		}

		public static bool StartsAsScript(string headerType) {
			return false;
		}

		private static bool EOFMarked;

		static StreamReader GetLoadStream(string path, object file) {
			//object file is either string or Stream or TextWriter
			StreamReader sr = file as StreamReader;
			if (sr != null) {
				return sr;
			}
			Stream s = file as Stream;
			if (s != null) {
				return new StreamReader(s);
			}
			string filename = string.Concat(file);
			string filepath = Path.Combine(path, filename + ".sav");
			return new StreamReader(File.OpenRead(filepath));
		}

		private static StreamReader globalsLoader;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		static void CloseLoadStreams() {
			try {
				globalsLoader.Close();
			} catch (Exception e) {
				Logger.WriteDebug(e);
			}
		}
	}
}