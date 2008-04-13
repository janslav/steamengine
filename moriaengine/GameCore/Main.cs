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
using System.IO;
using System.Collections;
using System.Reflection;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Text;
using System.Globalization; 
using System.Threading;
using SteamEngine.Common;
using SteamEngine.Timers;
using SteamEngine.CompiledScripts;
using SteamEngine.CompiledScripts.ClassTemplates;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.AuxServerPipe;

namespace SteamEngine {  
	
	//Do not add an access modifier to MainClass - Scripts should not be able to see INI settings, since they're
	//all changeable right now, and changing them could have unintended effects.
	
	public static class MainClass {
		
		//public static Logger logger;

		public static readonly object globalLock = new object();

		public static ManualResetEvent keepRunning = new ManualResetEvent(false);
		
		internal static bool loading=true;
		

		public static bool Loading { get {
			return loading;
		} }
		 
		public static bool nativeConsole = false;
		public static bool saveFlag = false;
		
		private static RunLevels runLevel=RunLevels.Unknown;
		private static Queue nativeCommands=null;
		
		internal static ConsConn winConsole;
		
		public static RunLevels RunLevel {
			get { 
				lock (globalLock) { 
					return runLevel; 
				}
			}
		}

		internal static void SetRunLevel(RunLevels level) {
			lock (globalLock) {
				runLevel=level;
			}
		}

		//method: winConsoleCommand
		//caled by the wincosole via reflection
		public static void winConsoleCommand(string data) {
			if (RunLevel==RunLevels.Running || RunLevel==RunLevels.AwaitingRetry) {
				if (nativeConsole && nativeCommands!=null) {
					lock (nativeCommands.SyncRoot) {
						nativeCommands.Enqueue(data);
					}
				} else {
					Logger.WriteWarning("SteamEngine.MainClass.winConsoleCommand() method called, even if SteamEngine is not runnig under native console!");
				}
			}
		}

		//method: WinStart
		//invoked instead of the Main() by the Winconsole if the server is run as console
		//as the argument it gets Methodinfo of a method that sends its string argument to the console
		//this also creates a ConsConn instance with an fake acc with admin plevel to represent the console
		public static void WinStart(object consSend) {
			//this is run if the server is started by the WinConsole
			try {
				nativeCommands=new Queue();
				nativeConsole=true;
				winConsole=new ConsConn((StringToSend) consSend);
			} catch (Exception e) {
				Console.WriteLine (e);
				return;
			}
			SteamMain();
		}

		public static void Main(string[] args) {
			SteamMain();
		}

		private static void SteamMain() {
			keepRunning.Reset();
			try {
				long time = HighPerformanceTimer.TickCount; //initializing HighPerformanceTimer
				Common.Tools.ExitBinDirectory();
				SetRunLevel(RunLevels.Startup);
				if (!Init()) {
					SetRunLevel(RunLevels.Dead);
					return;
				}

				Cycle();

			} catch (ShowMessageAndExitException smaee) {
				Logger.WriteFatal(smaee);
				smaee.Show();
			} catch (ThreadAbortException) {
				SetRunLevel(RunLevels.Shutdown);
				Logger.WriteFatal("Initialization process aborted");
			} catch (Exception globalexp) {
				Logger.WriteFatal(globalexp);
			} finally {
				if (RunLevel==RunLevels.Running)
					SetRunLevel(RunLevels.Shutdown);
				FastDLL.ShutDownFastDLL();
				Console.WriteLine("Shutdown...");
				Exit();
				SetRunLevel(RunLevels.Dead);
			}
		}
		
		private static bool Init() {
			System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

			CoreLogger.Init();

			AuxServerPipeClient.Init();
			System.Threading.Thread.Sleep(1000);//wait to conne

#if DEBUG
			Console.WriteLine("Starting SteamEngine ver. "+Globals.version+" (DEBUG build)"+" - "+Globals.serverName);
#elif SANE
			Console.WriteLine("Starting SteamEngine ver. "+Globals.version+" (SANE build)"+" - "+Globals.serverName);
#elif OPTIMIZED
			Console.WriteLine("Starting SteamEngine ver. "+Globals.version+" (OPTIMIZED build)"+" - "+Globals.serverName);
#else
			throw new SanityCheckException("None of these flags were set: DEBUG, SANE, or OPTIMIZED?");
#endif
			Console.WriteLine("http://steamengine.sourceforge.net");
			Console.WriteLine("Running under "+Environment.OSVersion+", Framework version: "+Environment.Version+".");

			Globals.Init();
			if (keepRunning.WaitOne(0, false)) {
				return false;
			}

			Packets.Prepared.Init();

			ClassTemplateParser.Init();
			
			if (!CompilerInvoker.CompileScripts(true)) {
				return false;
			}
			Tools.EnsureDirectory(Globals.mulPath, true);
			//ExportImport.Init();

			if (!Globals.fastStartUp) {
				using (StopWatch.StartAndDisplay("Loading .idx and .mul files from "+LogStr.File(Globals.mulPath)+"...")) {
					TileData.Init();
					SoundMul.Init();
					MultiData.Init();
				}
				ScriptLoader.Load();

				TileData.GenerateMissingDefs();
				CharData.GenerateMissingDefs();


				WorldSaver.Load();
			}
			
			Server.Init();
					
			//Console.WriteLine("Resolving object references...");
			DelayedResolver.ResolveAll();
			//Region.ResolveLoadedRegions();
			Map.Init();   //Sectors are created and items sorted on startup. 
			ClassManager.InitScripts();
			PluginDef.Init();
			DelayedResolver.ResolveAll();	//Resolve anything scripts needed resolved.
			loading = false;
			Globals.UnPauseServerTime();
			Logger.WriteDebug("triggering @startup");
			Globals.instance.TryTrigger(TriggerKey.startup, new ScriptArgs(true));

			if (!Globals.fastStartUp) {
				Console.WriteLine("Checking to see if any scripts have changed.");
				Globals.Resync();
			}

			return true;
		}
		
		public static void CollectGarbage() {
			GC.Collect();
			GC.WaitForPendingFinalizers();
			WeakRefDictionaryUtils.PurgeAll();
		}
		
		//this clears any static fields that could reference any game objects.
		//this is used for clearing a failed load attempt.
		public static void ClearWorld() {
			//Logger.WriteWarning("Clearing the world.");
			StaticRegion.ClearAll(); //puvodni vycisteni vsech loadnuthych regionu...
			Thing.ClearAll();
			AbstractAccount.ClearAll();
			Globals.ClearAll();
			Map.ClearAll();
			DelayedResolver.ClearAll();
			ObjectSaver.ClearJobs();
			OpenedContainers.ClearAll();
			Commands.ClearGmCommandsCache();
			ObjectPropertiesContainer.ClearCache();
		}
		
		//returns false if nothing was changing, otherwise true
		internal static bool TryResyncCompiledScripts() {
			ClassTemplateParser.Resync();
			if (CompilerInvoker.SourcesHaveChanged) {
				RecompileScripts();
				return true;
			}
			return false;
		}

		internal static void RecompileScripts() {
			using (StopWatch.StartAndDisplay("Recompiling...")) {
				Commands.commandRunning = false;
				Server.BroadCast("Server is pausing for script recompiling...");
				Logger.WriteDebug("triggering @shutdown");
				if (Globals.instance != null) { //is null when first run (and writing steamengine.ini)
					Globals.instance.TryTrigger(TriggerKey.shutdown, new ScriptArgs(false));
				}
				SetRunLevel(RunLevels.Paused);
				Globals.PauseServerTime();
				WorldSaver.Save();
				Server.AboutToRecompile();
				UnLoadAll();
				if (!LoadAll()) {
					//RunLevels.AwaitingRetry pauses everything except console connections & listening for console
					//connections & native commands, though SE doesn't care what they type, and whatever it is,
					//SE calls RetryRecompilingScripts. So, "retry", "recompile", "resync", "r", and "die you evil compiler"
					//(and whatever else they try) would all make SE attempt to recompile. Except for "exit", which, well,
					//exits.
					SetRunLevel(RunLevels.AwaitingRetry);
					Server.BroadCast("Script recompiling failed, pausing until it has been retried and succeeds.");
					/*while ((keepRunning) && commandsPaused) {
						System.Windows.Forms.Application.DoEvents();
						System.Threading.Thread.Sleep(20);
					}*/
				} else {	//If recompiling fails, we do not run this section. Instead, after a recompile succeeds,
					//the same code will be run in RetryRecompilingScripts.
					ScriptRecompilingSucceeded();
				}
			}
		}
		
		//This is only for use by RetryRecompilingScripts and RecompileScripts, and exists only because
		//they run the same code, and it's always good to avoid unnecessary duplication, which could lead
		//to the separate versions getting out of sync when someone changes one but misses the other.
		private static void ScriptRecompilingSucceeded() {
			CollectGarbage();
			Server.RecompilingFinished();
			SetRunLevel(RunLevels.Running);
			Globals.UnPauseServerTime();
			Server.BroadCast("Script recompiling finished.");
		}
		
		internal static void RetryRecompilingScripts() {
			UnLoadAll();
			if (!LoadAll()) {
				SetRunLevel(RunLevels.AwaitingRetry);
				Server.BroadCast("Script recompiling failed, remaining paused.");
			} else {
				SetRunLevel(RunLevels.Paused);	//Switch to paused until relinking & garbage collection have completed.
				ScriptRecompilingSucceeded();
			}
		}
		
		private static void UnLoadAll() {
			ClearWorld();
			Timers.Timer.Clear();
			//CompilerInvoker.UnLoadScripts();//bye-bye to all stored assemblies and such that are not core-related
			ClassManager.UnLoadScripts();//bye-bye to all storec types
			GeneratedCodeUtil.UnLoadScripts();//bye-bye to scripted code generators
			TriggerGroup.UnloadAll();//bye-bye to all triggergroups and their triggers
			ScriptHolder.UnloadAll();//bye-bye to all scripted functions
			ThingDef.ClearAll();//clear thingdef constructors etc.
			PluginDef.ClearAll();//clear plugindef constructors etc.
			GroundTileType.UnLoadScripts();			//unload all the Script objects which Script itself keeps (for getting scripts by name - used by Map for asking t_rock, etc, if it is the type of a specific map tileID).
			AbstractScript.UnloadAll();//all abstractscripts go bye-bye. This includes triggergroups, gumps, etc.
			Constant.UnloadAll();
			TestSuite.UnloadAll();
			ObjectSaver.UnloadScripts();
			DeepCopyFactory.UnloadScripts();
			ScriptLoader.UnloadScripts();//unload scripted loaders :)
			AbstractDef.UnloadScripts();//unload scripted defGetters
			AbstractSkillDef.UnloadScripts();
			//Region.UnloadScripts();
			//ExportImport.UnloadScripts();
			
			Console.WriteLine("Definitions unloaded");
		}
		
		//reload everything, including recompile
		//this does basically the same as Init(), only less :)
		private static bool LoadAll() {
			loading = true;
			DelayedResolver.ClearAll();
			ObjectSaver.ClearJobs();
			if (!CompilerInvoker.CompileScripts(false)) {
				return false;
			}
			//ExportImport.Init();
			if (!Globals.fastStartUp) {
				ScriptLoader.Load();
				TriggerGroup.ReAddGlobals();
			
				WorldSaver.Load();
				DelayedResolver.ResolveAll();
			}
			Server.ReLinkCharacters();
			Map.Init();
			ClassManager.InitScripts();
			PluginDef.Init();
			DelayedResolver.ResolveAll();	//Resolve anything scripts needed resolved.
			//Region.ResolveLoadedRegions();
			loading = false;
			Logger.WriteDebug("triggering @startup");
			Globals.instance.TryTrigger(TriggerKey.startup, new ScriptArgs(false));
			return true;
		}

		private static void HandleNativeCmds() {
			if (nativeCommands!=null && nativeCommands.Count>0) {
				string cmd;
				lock (nativeCommands.SyncRoot) {
					cmd=nativeCommands.Dequeue() as string;
				}
				if (cmd!=null) {
					winConsole.DoCommand(cmd);
				}
			}
		}

		private static void Cycle() {
			SetRunLevel(RunLevels.Running);
			AuxServerPipe.AuxServerPipeClient.AnnounceStartupFinished();

			Console.WriteLine("Starting Main Loop");

			while (!keepRunning.WaitOne(5, false)) {
				lock (globalLock) {
					HandleNativeCmds();

					Server.Cycle();
					if (MainClass.RunLevel==RunLevels.Running) {
						SteamEngine.Timers.Timer.Cycle();
						SteamEngine.Packets.NetState.ProcessAll();
						if (saveFlag) {
							WorldSaver.Save();
							saveFlag=false;
						}
					}
				}
			}

			Console.WriteLine("Leaving Main Loop");
		}
		
		internal static void Exit() {
			Logger.WriteDebug("triggering @shutdown");
			if (Globals.instance != null) { //is null when first run (and writing steamengine.ini)
				Globals.instance.TryTrigger(TriggerKey.shutdown, new ScriptArgs(true));
			}
			keepRunning.Set();
			Timers.Timer.Clear();
			Server.Exit();
		}
	}
}
