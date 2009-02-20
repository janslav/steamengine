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
using System.Text;
using System.Globalization;
using System.IO;
using System.Net;
using SteamEngine.Common;
using SteamEngine.CompiledScripts;
using SteamEngine.Packets;
using SteamEngine.Networking;
using SteamEngine.Persistence;
using SteamEngine.Communication.TCP;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using System.Diagnostics;
using System.ComponentModel;
#if !MONO
using Microsoft.Win32;	//for RegistryKey
#endif

namespace SteamEngine {
	public interface ISrc {
		byte Plevel { get; }
		byte MaxPlevel { get; }
		void WriteLine(string line);
		//void WriteLine(LogStr data);
		//bool IsNativeConsole { get; }
		//AbstractCharacter Character { get; }
		AbstractAccount Account { get; }
		//Conn ConnObj { get; }
		//bool IsLoggedIn { get; }
	}

	public class Globals : PluginHolder {
		//The minimum and maximum ranges that a client can get packets within
		public const int MaxUpdateRange = 18;
		public const int MaxUpdateRangeSquared = MaxUpdateRange * MaxUpdateRange;
		public const int MinUpdateRange = 5;
		public const int defaultWarningLevel = 4;

		public readonly static ushort port;   //Changing this after initialization has no effect. 
		public readonly static string serverName;  //Can be changed while the server is running.
		public readonly static string adminEmail;  //Can be changed while the server is running.

		public override string Name {
			get {
				return serverName;
			}
			set {
				//serverName = value;
				throw new SEException("Can't set server name directly. It's read from steamengine.ini");
			}
		}
		public readonly static string commandPrefix;
		public readonly static string alternateCommandPrefix;
		public readonly static string logPath;
		public readonly static string savePath;
		public readonly static string mulPath;
		public readonly static string scriptsPath = Path.GetFullPath(".\\scripts\\");

		public readonly static string docsPath;
		public readonly static string ndocExe;

		public readonly static bool logToFiles;
		public static bool logToConsole;

		public readonly static byte maximalPlevel;
		public readonly static int plevelOfGM;
		public readonly static int plevelToLscriptCommands;

		//public readonly static bool kickOnSuspiciousErrors;
		public readonly static bool allowUnencryptedClients;

		public readonly static ushort reachRange;
		public readonly static int squaredReachRange;
		//public readonly static ushort sightRange;
		//public readonly static int squaredSightRange;
		public readonly static ushort defaultASCIIMessageColor;
		public readonly static ushort defaultUnicodeMessageColor;

		internal static bool useMap;
		public static bool UseMap {
			get {
				return useMap;
			}
		}

		public readonly static bool generateMissingDefs;
		public readonly static bool useMultiItems;
		internal static bool readBodyDefs;
		public static bool ReadBodyDefs {
			get {
				return readBodyDefs;
			}
		}

		public readonly static bool sendTileDataSpam;
		public readonly static bool fastStartUp;
		public readonly static bool netSyncingTracingOn;

		public readonly static bool writeMulDocsFiles;

		public readonly static bool resolveEverythingAtStart;

		public readonly static bool autoAccountCreation;
		public readonly static bool blockOSI3DClient;
		//public readonly static bool alwaysUpdateRouterIPOnStartup;
		//public readonly static sbyte timeZone;
		public readonly static int maxConnections;

		public readonly static bool supportUnicode;
		public readonly static int speechDistance;
		public readonly static int emoteDistance;
		public readonly static int whisperDistance;
		public readonly static int yellDistance;
		public readonly static bool asciiForNames;
		public readonly static uint loginFlags;
		public readonly static ushort featuresFlags;

		public readonly static ushort defaultItemModel;
		public readonly static ushort defaultCharModel;

		public readonly static bool hashPasswords;

		public readonly static bool scriptFloats;

		public readonly static bool aosToolTips;

		//public readonly static IList<IPAddress> omitip;

		[Summary("The last new item or character or memory or whatever created.")]
		public static TagHolder lastNew = null;

		[Summary("The last new Character created.")]
		public static AbstractCharacter lastNewChar = null;

		[Summary("The last new Item created.")]
		public static AbstractItem lastNewItem = null;

		[Summary("The source of the current action.")]
		private static ISrc src = null;

		public static ISrc Src {
			get {
				return src;
			}
		}

		public static AbstractCharacter SrcCharacter {
			get {
				return src as AbstractCharacter;
			}
		}

		public static AbstractAccount SrcAccount {
			get {
				AbstractCharacter ch = src as AbstractCharacter;
				if (ch != null) {
					return ch.Account;
				}
				ConsoleDummy console = src as ConsoleDummy;
				if (console != null) {
					return console.Account;
				}
				return null;
			}
		}

		//public static GameConn SrcGameConn {
		//    get {
		//        AbstractCharacter ch = src as AbstractCharacter;
		//        if (ch != null) {
		//            return ch.Conn;
		//        }
		//        return null;
		//    }
		//}

		public static GameState SrcGameState {
			get {
				AbstractCharacter ch = src as AbstractCharacter;
				if (ch != null) {
					return ch.GameState;
				}
				return null;
			}
		}

		public static TCPConnection<GameState> SrcTCPConnection {
			get {
				AbstractCharacter ch = src as AbstractCharacter;
				if (ch != null) {
					return ch.GameState.Conn;
				}
				return null;
			}
		}

		public static void SrcWriteLine(string str) {
			if (src != null) {
				src.WriteLine(str);
			}
		}

		internal static void SetSrc(ISrc newSrc) {
			src = newSrc;
		}

		//public static Conn srcConn = null;


		/*
		 * Field: dice
		 * Call dice.Next(int min, int max) to generate a random number.
		 */
		public static readonly Random dice = new Random();

		internal static Globals instance;
		public static Globals Instance {
			get {
				return instance;
			}
		}

		private static Process ndocProcess;

		private Globals() {

		}

		internal static void Init() {
			instance = new Globals();
		}

		static Globals() {
			try {
				IniFile iniH = new IniFile("steamengine.ini");
				IniFileSection setup = iniH.GetNewOrParsedSection("setup");

				serverName = setup.GetValue<string>("name", "Unnamed SteamEngine Shard", "The name of your shard");
				adminEmail = setup.GetValue<string>("adminEmail", "admin@email.com", "Your Email to be displayed in status web, etc.");
				hashPasswords = setup.GetValue<bool>("hashPasswords", false, "This hashes passwords (for accounts) using SHA512, which isn't reversable. Instead of writing passwords to the save files, the hash is written. If you disable this, then passwords will be recorded instead of the hashes, and will be stored instead of the hashes, which means that if someone obtains access to your accounts file, they will be able to read the passwords. It's recommended to leave this on. Note: If you switch this off after it's been on, passwords that are hashed will stay hashed, because hashing is one-way, until that account logs in, at which point the password (if it matches) will be recorded again in place of the hash. If you use text saves, you should be able to write password=whatever in the account save, and the password will be changed to that when that save is loaded, even if you're using hashed passwords.");

				//kickOnSuspiciousErrors = setup.GetValue<bool>("kickOnSuspiciousErrors", true, "Kicks the user if a suspiciously erroneous value is recieved in a packet from their client.");

				allowUnencryptedClients = setup.GetValue<bool>("allowUnencryptedClients", true, "Allow clients with no encryption to connect. There's no problem with that, except for lower security.");

				IniFileSection files = iniH.GetNewOrParsedSection("files");
				logPath = Path.GetFullPath(files.GetValue<string>("logPath", ".\\logs\\", "Path to the log files"));
				savePath = Path.GetFullPath(files.GetValue<string>("savePath", ".\\saves\\", "Path to the save files"));
#if MSWIN
				ndocExe = Path.GetFullPath(files.GetValue<string>("ndocExe", "C:\\Program Files\\NDoc\\bin\\.net-1.1\\NDocConsole.exe", "Command for NDoc invocation (leave it blank, if you don't want use NDoc)."));
#elif LINUX
				ndocExe= Path.GetFullPath(files.GetValue<string>("ndocExe","","NDoc cannot be used under Linux"));
#endif
				docsPath = Path.GetFullPath(files.GetValue<string>("docsPath", ".\\docs\\", "Path to the docs (Used when writing out some information from MUL files, like map tile info)"));

				logToFiles = files.GetValue<bool>("logToFiles", true, "Whether to log console output to a file");
				useMap = files.GetValue<bool>("useMap", true, "Whether to load map0.mul and statics0.mul and use them or not.");
				generateMissingDefs = files.GetValue<bool>("generateMissingDefs", false, "Whether to generate missing scripts based on tiledata.");
				useMultiItems = files.GetValue<bool>("useMultiItems", true, "Whether to use multi items...");
				readBodyDefs = files.GetValue<bool>("readBodyDefs", true, "Whether to read Bodyconv.def (a client file) in order to determine what character models lack defs (and then to write new ones for them to 'scripts/defaults/chardefs/newCharDefsFromMuls.def').");
				writeMulDocsFiles = files.GetValue<bool>("writeMulDocsFiles", false, "If this is true/on/1, then SteamEngine will write out some files with general information gathered from various MUL files into the 'docs/MUL file docs' folder. These should be distributed with SteamEngine anyways, but this is useful sometimes (like when a new UO expansion is released).");

				IniFileSection login = iniH.GetNewOrParsedSection("login");
				//alwaysUpdateRouterIPOnStartup = (bool) login.GetValue<bool>("alwaysUpdateRouterIPOnStartup", false, "Automagically determine the routerIP every time SteamEngine is run, instead of using the setting for it in steamengine.ini.");
				//string omit = login.GetValue<string>("omitip", "5.0.0.0", "IP to omit from server lists, for example, omitIP=5.0.0.0. You can have multiple omitIP values, separated by comma.");
				//omitip = new List<IPAddress>();
				//foreach (string ip in omit.Split(',')) {
				//    Server.AddOmitIP(IPAddress.Parse(ip));
				//    omitip.Add(IPAddress.Parse(ip));
				//}
				//omitip = new System.Collections.ObjectModel.ReadOnlyCollection<IPAddress>(omitip);

				bool exists;
				string msgBox = "";
				if (iniH.FileExists) {
					exists = true;
					//string routerIP = login.GetValue<string>("routerIP", "", "The IP to show to people who are outside your LAN");
					////if (routerIP.Length>0) {
					//Server.SetRouterIP(routerIP);
					////}
					mulPath = files.GetValue<string>("mulPath", "muls", "Path to the mul files");
				} else {
					string mulsPath = GetMulsPath();
					if (mulsPath == null) {
						msgBox += "Unable to locate the UO MUL files. Please either place them in the 'muls' folder or specify the proper path in steamengine.ini (change mulPath=muls to the proper path)\n\n";
						mulsPath = files.GetValue<string>("mulPath", "muls", "Path to the mul files");
					} else {
						mulsPath = files.GetValue<string>("mulPath", mulsPath, "Path to the mul files");
					}
					exists = false;
					//string[] ret = Server.FindMyIP();
					//string routerIP = ret[1];
					//msgBox += ret[0];
					//if (routerIP == null) {
					//    login.SetValue<string>("routerIP", "", "The IP to show to people who are outside your LAN");
					//} else {
					//    login.SetValue<string>("routerIP", routerIP, "The IP to show to people who are outside your LAN");
					//}
				}
				//timeZone = login.GetValue<sbyte>("timeZone", 5, "What time-zone you're in. 0 is GMT, 5 is EST, etc.");
				maxConnections = login.GetValue<int>("maxConnections", 100, "The cap on # of connections. Affects the percentage-full number sent to UO client.");
				autoAccountCreation = login.GetValue<bool>("autoAccountCreation", false, "Automatically create accounts when someone attempts to log in");
				blockOSI3DClient = login.GetValue<bool>("blockOSI3DClient", true, "Block the OSI 3D client from connecting. Said client is not supported, since it tends to do things in a stupid manner.");

				IniFileSection ports = iniH.GetNewOrParsedSection("ports");
				port = ports.GetValue<ushort>("game", 2595, "The port to listen on for client connections");

				IniFileSection text = iniH.GetNewOrParsedSection("text");
				commandPrefix = text.GetValue<string>("commandPrefix", ".", "The command prefix. You can make it 'Computer, ' if you really want.");
				alternateCommandPrefix = text.GetValue<string>("alternateCommandPrefix", "[", "The command prefix. Defaults to [. In the god-client, . is treated as an internal client command, and anything starting with . is NOT sent to the server.");
				supportUnicode = text.GetValue<bool>("supportUnicode", true, "If you turn this off, all messages, speech, etc sent TO clients will take less bandwidth, but nobody'll be able to speak in unicode (I.E. They can only speak using normal english characters, not russian, chinese, etc.)");
				asciiForNames = text.GetValue<bool>("asciiForNames", false, "If this is on, names are always sent in ASCII regardless of what supportUnicode is set to. NOTE: Names in paperdolls and status bars can only be shown in ASCII, and this ensures that name colors come out right.");
				defaultASCIIMessageColor = text.GetValue<ushort>("serverMessageColor", 0x0000, "The color to use for server messages (Welcome to **, pause for worldsave, etc). Can be in hex, but it doesn't have to be.");
				defaultUnicodeMessageColor = text.GetValue<ushort>("defaultUnicodeMessageColor", 0x0394, "The color to use for unicode messages with no specified color (or a specified color of 0, which is not really valid for unicode messages).");

				IniFileSection ranges = iniH.GetNewOrParsedSection("ranges");
				reachRange = ranges.GetValue<ushort>("reachRange", 5, "The distance (in spaces) a character can reach.");
				squaredReachRange = reachRange * reachRange;
				//sightRange = ranges.GetValue<ushort>("sightRange", 15, "The distance (in spaces) a character can see.");

				speechDistance = ranges.GetValue<int>("speechDistance", 10, "The maximum distance from which normal speech can be heard.");
				emoteDistance = ranges.GetValue<int>("emoteDistance", 10, "The maximum distance from which an emote can be heard/seen.");
				whisperDistance = ranges.GetValue<int>("whisperDistance", 2, "The maximum distance from which a whisper can be heard.");
				yellDistance = ranges.GetValue<int>("yellDistance", 20, "The maximum distance from which a yell can be heard.");

				IniFileSection plevels = iniH.GetNewOrParsedSection("plevels");
				maximalPlevel = plevels.GetValue<byte>("maximalPlevel", 7, "Maximal plevel - the highest possible plevel (the owner's plevel)");
				plevelOfGM = plevels.GetValue<int>("plevelOfGM", 4, "Plevel needed to do all the cool stuff GM do. See invis, walk thru walls, ignore line of sight, own all animals, etc.");
				plevelToLscriptCommands = plevels.GetValue<int>("plevelToLscriptCommands", 2, "With this (or higher) plevel, the client's commands are parsed and executed as LScript statements. Otherwise, much simpler parser is used, for speed and security.");

				IniFileSection scripts = iniH.GetNewOrParsedSection("scripts");
				resolveEverythingAtStart = scripts.GetValue<bool>("resolveEverythingAtStart", false, "If this is false, Constants and fields of scripted defs (ThingDef,Skilldef, etc.) will be resolved from the text on demand (and probably at the first save). Otherwise, everything is resolved on the start. Leave this to false on your development server, but set it to true for a live shard, because it's more secure.");
				defaultItemModel = scripts.GetValue<ushort>("defaultItemModel", 0xeed, "The item model # to use when an itemdef has no model specified.");
				defaultCharModel = scripts.GetValue<ushort>("defaultCharModel", 0x0190, "The character body/model # to use when a chardef has no model specified.");
				scriptFloats = scripts.GetValue<bool>("scriptFloats", true, "If this is off, dividing/comparing 2 numbers in Lscript is treated as if they were 2 integers (rounds before the computing if needed), effectively making the scripting engine it backward compatible to old spheres. Otherwise, the precision of the .NET Double type is used in scripts.");
				Logger.showCoreExceptions = scripts.GetValue<bool>("showCoreExceptions", true, "If this is off, only the part of Exception stacktrace that occurs in the scripts is shown. If you're debugging core, have it on. If you're debugging scripts, have it off to save some space on console.");

				loginFlags = 0;
				featuresFlags = 0;

				IniFileSection features = iniH.GetNewOrParsedSection("features");
				//features.Comment("These are features which can be toggled on or off.");
				aosToolTips = features.GetValue<bool>("aosToolTips", true, "If this is on, AOS tooltips (onmouseover little windows instead of onclick texts) are enabled. Applies for clients > 3.0.8o");
				//OneCharacterOnly = (bool) features.IniEntry("OneCharacterOnly", (bool)false, "Limits accounts to one character each (except GMs)).");

				featuresFlags |= 0x2;
				if (aosToolTips) {
					loginFlags |= 0x20;
					featuresFlags |= 0x0008 | 0x8000;
				}
				//for now we only set whether tooltips work.

				//TODO?:
				//OneCharacterOnly
				//SixCharacters
				//RightClickMenus
				//Chat? (now disabled)
				//LBR? (now enabled)

				//'loginFlags' variable:
				//0x14 = One character only
				//0x08 = Right-click menus
				//0x20 = AOS features
				//0x40 = Six characters instead of five

				//'featuresFlags' variable
				//0x0001 = Chat for pre-AOS clients
				//0x0002 = LBR for pre-AOS clients
				//0x0004 = Chat for AOS clients
				//0x0008 = LBR for AOS clients
				//0x0010 = Allow creating Paladin/Necromancer.
				//0x0020 = Six characters instead of five.
				//0x8000 = AOS Feature Enable (Allow features 0x04 - 0x20)

				//So normally that would be 0x800f, 0x801f to allow pal/nec, and also include 0x20 for six characters.
				//So this can't quite be a constant. Oh well!

				//Variables:
				//One character only	: LF 0x14 FF 0x0000
				//Chat					: LF 0x00 FF 0x8005
				//LBR					: LF 0x00 FF 0x800a
				//Right-click menus		: LF 0x08 FF 0x0000
				//AOS					: LF 0x20 FF 0x8010
				//Six Characters		: LF 0x40 FF 0x8020


				IniFileSection temporary = iniH.GetNewOrParsedSection("temporary");
				temporary.AddComment("These are temporary INI settings, which will be going away in future versions.");
				fastStartUp = temporary.GetValue<bool>("fastStartUp", false, "If set to true, some time consuming steps in the server init phase will be skipped (like loading of defs and scripts), for faster testing of other functions. In this mode, the server will be of course not usable for game serving.");
				sendTileDataSpam = temporary.GetValue<bool>("sendTileDataSpam", false, "Set this to true, and you'll be sent lots of spam when you walk. Yeah, this is temporary. I need it for testing tiledata stuff. -SL");
				netSyncingTracingOn = temporary.GetValue<bool>("netSyncingTracingOn", false, "Networking.SyncQueue info messages");


				temporary.AddComment("");
				temporary.AddComment("SteamEngine determines if someone is on your LAN by comparing their IP with all of yours. If the first three parts of the IPs match, they're considered to be on your LAN. Note that this will probably not work for people on very big LANs. If you're on one, please post a feature request on our SourceForge site.");
				temporary.AddComment("http://steamengine.sf.net/");


				if (!exists) {
					iniH.WriteToFile();
					MainClass.signalExit.Set();
					throw new ShowMessageAndExitException(msgBox + "SteamEngine has written a default 'steamengine.ini' for you. Please take a look at it, change whatever you want, and then run SteamEngine again to get started.", "Getting started");
				}

				PauseServerTime();

			} catch (ShowMessageAndExitException smaee) {
				Logger.Show(smaee.Message);
				smaee.Show();
				MainClass.signalExit.Set();
			} catch (Exception globalexp) {
				Console.WriteLine();
				Logger.WriteFatal(globalexp);
				MainClass.signalExit.Set();
			}
		}

		/**
			Looks in the registry to find the path to the MUL files. If both 2d and 3d are installed,
			it isn't specified which this will find.
		*/
		public static string GetMulsPath() {
#if !MONO
			RegistryKey rk = Registry.LocalMachine;
			rk = rk.OpenSubKey("SOFTWARE");
			if (rk != null) {
				rk = rk.OpenSubKey("Origin Worlds Online");
				if (rk != null) {
					string[] names = rk.GetSubKeyNames();
					foreach (string name in names) {
						RegistryKey uoKey = rk.OpenSubKey(name);
						if (uoKey != null) {
							string[] names2 = uoKey.GetSubKeyNames();
							foreach (string name2 in names2) {
								RegistryKey verKey = uoKey.OpenSubKey(name2);
								object s = verKey.GetValue("InstCDPath");
								if (s != null && s is string) {
									return (string) s;
									//} else {
									//Console.WriteLine("It ain't a string (Type is "+s.GetType().ToString()+") : "+s.ToString());
								}
							}
						} else {
							Logger.WriteWarning("Unable to open 'uoKeys' in registry");
						}
					}
				} else {
					Logger.WriteWarning("Unable to open 'Origin Worlds Online' in registry");
				}
			} else {
				Logger.WriteWarning("Unable to open 'SOFTWARE' in registry");
			}
#else
			Logger.WriteWarning("TODO: Implement some way to find the muls when running from MONO?");
#endif
			return null;
		}

		//public static readonly string version="1.0.0"; 
		public static readonly string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

		public static readonly DateTime startedAt = DateTime.Now;
		public static TimeSpan timeUp {
			get {
				return DateTime.Now - startedAt;
			}
		}

		public static bool ShowCoreExceptions {
			get { return Logger.showCoreExceptions; }
			set { Logger.showCoreExceptions = value; }
		}

		public static int StatClients {
			get {
				return GameServer.AllClients.Count;
			}
		}

		public static uint StatItems {
			get {
				return AbstractItem.Instances;
			}
		}

		public static uint StatChars {
			get {
				return AbstractCharacter.Instances;
			}
		}

		//internal static IPAddress[] ips;

		//public string IP {
		//    get {
		//        return Tools.ObjToString(ips);
		//    }
		//}

		public static void Exit() {
			MainClass.signalExit.Set();
		}

		public static void SetConsoleTitle(string title) {
			Console.WriteLine("Reseting console title to '" + title + "'" + LogStr.Title(title));
		}

		public static void Save() {
			//Packets.NetState.ProcessAll();
			SyncQueue.ProcessAll();

			Globals.PauseServerTime();
			WorldSaver.Save();
			Globals.UnPauseServerTime();
		}

		public static void G() {
			MainClass.CollectGarbage();
		}

		public static void ReCompile() {
			//forced complete recompiling
			MainClass.RecompileScripts();//can block for some time
		}

		public static void Resync() {
			if (!MainClass.TryResyncCompiledScripts()) {//can block for some time
				ScriptLoader.Resync();
				MainClass.CollectGarbage();
			}
		}

		public static void R() {
			Resync();
		}

		public static void SvnUpdate() {
			VersionControl.SVNUpdateProject();
		}

		public static void SvnCleanUp() {
			VersionControl.SVNCleanUpProject();
		}

		public static Type type(string typename) {
			//global type, you can actually get any type with this
			return Type.GetType(typename, true, true);//true for case insensitive
		}

		public static Type se(string typename) {
			//Console.WriteLine("type: "+Type.GetType("SteamEngine."+typename));
			return Type.GetType("SteamEngine." + typename, true, true);
		}

		//sphere compatibility
		public static Type AccMgr() {
			return typeof(AbstractAccount);
		}

		public static AbstractAccount FindAccount(string name) {
			return AbstractAccount.Get(name);
		}

		public static string Hex(int numb) {
			return "0x" + Convert.ToString(numb, 16);
		}

		public static string Dec(int numb) {
			return Convert.ToString(numb, 10);
		}

		public static void Information() {
			Globals.Src.WriteLine(string.Format(
				@"Steamengine ver. {0}, Name = ""{1}"", Clients = {2}{6}Items = {3}, Chars = {4}, Mem = {5} kB",
				version, serverName, GameServer.AllClients.Count, AbstractItem.Instances, AbstractCharacter.Instances,
				GC.GetTotalMemory(false) / 1024, Environment.NewLine));
		}

		public static void I() {
			Information();
		}

		public static void B(string msg) {
			PacketSequences.BroadCast(msg);
		}

		//public static void Echo(ParsedCommandsList commandlist) {
		//	if (Commands.InvokeCommand(typeof(GlobalCommands),null, BindingFlags.Static, commandlist)) {
		//		Globals.srcConn.WriteLine(Tools.ObjToString(Commands.result));
		//	} else {
		//		throw new Exception(Commands.reason);
		//	}
		//}
		//
		//public static void Show(ParsedCommandsList commandlist) {
		//	int tempindex=commandlist.index;
		//	if (Commands.InvokeCommand(typeof(GlobalCommands),null, BindingFlags.Static, commandlist)) {
		//		commandlist.index=tempindex;
		//		Globals.srcConn.WriteLine("'"+commandlist.ToString()+"' for '"+Globals.serverName+"' is "+Tools.ObjToString(Commands.result)+".");
		//	} else {
		//		throw new Exception(Commands.reason);
		//	}
		//}

		public void SysMessage(object o) {
			Console.WriteLine(Tools.ObjToString(o));
		}

		public void Log(object o) {
			Console.WriteLine(Tools.ObjToString(o));
		}

		public override string ToString() {
			return serverName;
		}

		internal static void ClearAll() {
			instance = new Globals();
		}

		internal static void SaveGlobals(SaveStream output) {
			Logger.WriteDebug("Saving globals.");
			output.WriteComment("globals");
			output.WriteLine();
			output.WriteSection("Globals", serverName.Replace(' ', '_'));//the header name is in fact ignored
			output.WriteValue("time", TimeInTicks);
			instance.Save(output);
			output.WriteLine();
			ObjectSaver.FlushCache(output);
			AbstractDef.SaveAll(output);
		}

		internal static void LoadGlobals(PropsSection input) {
			//Console.WriteLine("["+input.headerType+" "+input.headerName+"]");
			PropsLine timeLine = input.TryPopPropsLine("time");
			long value;
			if ((timeLine != null) && (ConvertTools.TryParseInt64(timeLine.value, out value))) {
				lastMarkServerTime = TimeSpan.FromTicks(value);
			} else {
				Logger.WriteWarning("The Globals section of save is missing the " + LogStr.Ident("Time") + " value or the value is invalid, setting server time to 0");
				lastMarkServerTime = TimeSpan.Zero;
			}

			instance.LoadSectionLines(input);
		}

#if MSWIN
		private static void ndocExited(object source, EventArgs e) {
			if (ndocProcess.ExitCode != 0) {
				Logger.WriteError("NDOC was not finished correctly (exit code: " + ndocProcess.ExitCode + ").");
			} else {
				Console.WriteLine("NDoc finished successfuly");
			}
			ndocProcess = null;
		}
#endif

		[Summary("First documentation in Steamengine.")]
		[Remark("This is first documentation in Steamengine, that uses SteamDoc attributes.")]
		[Return("Nothing")]
		public static void CompileDocs() {
			if (ndocProcess != null) {
				Logger.WriteError("NDoc is already running.");
				return;
			}
			try {
				Console.WriteLine("Generating Common XML documentation file");
				DocScanner scanner = new DocScanner();
				Assembly asm = ClassManager.CommonAssembly;
				scanner.ScanAssembly(asm);
				scanner.WriteToFile("bin\\" + asm.GetName().Name + ".xml");

				Console.WriteLine("Generating Core XML documentation file");
				asm = ClassManager.CoreAssembly;
				scanner = new DocScanner();
				scanner.ScanAssembly(asm);
				scanner.WriteToFile("bin\\" + asm.GetName().Name + ".xml");

				Console.WriteLine("Generating Scripts XML documentation file");
				asm = ClassManager.ScriptsAssembly;
				scanner = new DocScanner();
				scanner.ScanAssembly(asm);
				scanner.WriteToFile("bin\\" + asm.GetName().Name + ".xml");

			} catch (Exception e) {
				Logger.WriteError(e);
			}

#if MSWIN
			if (ndocExe.Length != 0) {
				Console.WriteLine("Invoking NDOC, documentation will be generated to docs/sourceDoc");
				try {
#if DEBUG
					string project = "debug.ndoc";
#elif SANE
					string project="sane.ndoc";
#elif OPTIMIZED
					string project="optimized.ndoc";
#endif
					ProcessStartInfo info = new ProcessStartInfo(ndocExe, "-project=" + project);
					info.WorkingDirectory = ".\\distrib\\";
					info.WindowStyle = ProcessWindowStyle.Normal;
					info.UseShellExecute = true;
					ndocProcess = new Process();
					ndocProcess.StartInfo = info;
					ndocProcess.Exited += new EventHandler(ndocExited);
					ndocProcess.EnableRaisingEvents = true;
					ndocProcess.Start();
				} catch (Exception e) {
					Logger.WriteError(e);
				}
			}
#elif LINUX
			Console.WriteLine ("NDoc is not supported under Linux.");
#endif
		}

		public static void Load(string filename) {
			ScriptLoader.LoadNewFile(filename);
			Globals.Src.WriteLine("Script file loaded.");
		}

		//public static void NetStats() {
		//    GameConn.NetStats();
		//}

		public static string GetMulDocPathFor(string filename) {
			string docPath = Path.Combine(Globals.docsPath, "defaults");
			Tools.EnsureDirectory(docPath, true);

			return Path.Combine(docPath, filename);
		}

		//public static void CompressionStats() {
		//    PacketStats.CompressionStats();
		//}

		//public static void Logout() {
		//    ConsoleDummy conn = Globals.Src as ConsoleDummy;
		//    if (conn!=null) {
		//        if (!conn.IsNativeConsole) {
		//            conn.Close("Commanded to log out.");
		//        } else {
		//            conn.WriteLine("Native console cannot be logged out.");
		//        }
		//    }
		//}

		//public static void ConTest() {
		//	Globals.src.WriteLine(">>>>>> Text Console Test"+Environment.NewLine);
		//	Globals.src.WriteLine(LogStr.Warning("Warning: ")+LogStr.WarningData("this is warning message"+Environment.NewLine));
		//	Globals.src.WriteLine(LogStr.Error("Error: ")+LogStr.ErrorData("this is error message"+Environment.NewLine));
		//	Globals.src.WriteLine(LogStr.Fatal("Fatal: ")+LogStr.FatalData("this is fatal message"+Environment.NewLine));
		//	Globals.src.WriteLine(LogStr.Critical("Critical: ")+LogStr.CriticalData("this is critical message"+Environment.NewLine));
		//	Globals.src.WriteLine("FileLine: "+LogStr.FileLine("Globals.cs",540)+" (and this is default style)"+Environment.NewLine);
		//	Globals.src.WriteLine("Highlight: "+LogStr.Highlight("highlighted text")+ " (and this is default style)"+Environment.NewLine);
		//	Globals.src.WriteLine("Ident: "+LogStr.Ident("identifiers")+ " (and this is default style)"+Environment.NewLine);
		//	Globals.src.WriteLine("FilePos: "+LogStr.FilePos("position in file")+ " (and this is default style)"+Environment.NewLine);
		//	Globals.src.WriteLine("File: "+LogStr.File("file and directory names")+ " (and this is default style)"+Environment.NewLine);
		//	Globals.src.WriteLine("Number: "+LogStr.Number("numbers")+ " (and this is default style)"+Environment.NewLine);
		//	Globals.src.WriteLine("Global style test..."+Environment.NewLine);
		//	Globals.src.WriteLine(LogStr.SetStyle(LogStyles.Highlight)+LogStr.Number("LogStr.SetStyle(LogStyles.Highlight)")+" called, this text is produced by "+LogStr.Ident("LogStr.Raw()")+" (and highlighted style again)"+Environment.NewLine);
		//	Globals.src.WriteLine("style selected by "+LogStr.Ident("LogStr.SetStyle()")+" is reseted to default style on next line"+Environment.NewLine);
		//	Globals.src.WriteLine("<<<<<< End of Console Test"+Environment.NewLine);
		//}

		public static void RunTests() {
			TestSuite.RunAllTests();
			//PacketSender.DiscardAll();
		}

		//public static string CurrentFile { get {
		//	return WorldSaver.CurrentFile; 
		//} }

		private static DateTime lastMarkRealTime = DateTime.Now;
		private static TimeSpan lastMarkServerTime;
		private static int paused = 0;


		public static long TimeInTicks {
			get {
				return TimeAsSpan.Ticks;
			}
		}

		public static double TimeInSeconds {
			get {
				return TimeAsSpan.TotalSeconds;
			}
		}

		public static TimeSpan TimeAsSpan {
			get {
				if (paused > 0) {
					return lastMarkServerTime;
				} else {
					DateTime current = DateTime.Now;
					return lastMarkServerTime + (current - lastMarkRealTime);
				}
			}
		}

		[Summary("For sphere compatibility, this returns servertime in tenths of second")]
		public long Time {
			get {
				return (long) (TimeAsSpan.TotalSeconds / 10d);
			}
		}

		internal static void PauseServerTime() {
			paused++;
			if (paused == 1) {
				DateTime current = DateTime.Now;

				lastMarkServerTime = lastMarkServerTime + (current - lastMarkRealTime);
				lastMarkRealTime = current;

				RunLevelManager.SetPaused();
			}
		}

		internal static void UnPauseServerTime() {
			paused--;
			Sanity.IfTrueThrow(paused < 0, "Can't UnPause when not paused");
			if (paused == 0) {
				lastMarkRealTime = DateTime.Now;
				RunLevelManager.UnsetPaused();
			}
		}
	}
}