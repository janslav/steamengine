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
using System.Reflection;
using System.Text;
using System.Globalization; 
using System.IO;
using System.Net;
using SteamEngine.Common;
using SteamEngine.CompiledScripts;
using SteamEngine.Packets;
using SteamEngine.Persistence;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using System.Diagnostics;
using System.ComponentModel;

namespace SteamEngine {
	public interface ISrc {
		byte Plevel { get; }
		byte MaxPlevel { get; }
		void WriteLine(string line);
		//void WriteLine(LogStr data);
		//bool IsNativeConsole { get; }
		//AbstractCharacter Character { get; }
		//GameAccount Account { get; }
		//Conn ConnObj { get; }
		//bool IsLoggedIn { get; }
	}

	public class Globals : TagHolder {
		//The minimum and maximum ranges that a client can get packets within
		public const int MaxUpdateRange=18;
		public const int MaxUpdateRangeSquared=MaxUpdateRange*MaxUpdateRange;
		public const int MinUpdateRange=5;
		public const int defaultWarningLevel=4;

		private static SEIniHandler iniH;
		public static ushort port;   //Changing this after initialization has no effect. 
		public static ushort consoleport;   //Changing this after initialization has no effect. 
		public static string serverName;  //Can be changed while the server is running.
		public static string adminEmail;  //Can be changed while the server is running.
		public override string Name {
			get {
				return serverName;
			}
			set {
				serverName = value;
			}
		}
		public static string commandPrefix;
		public static string alternateCommandPrefix;
		public static string logPath;      //Can be changed while the server is running. (altough it has an effect only when creating a new log file)
		public static string savePath;        //Can be changed while the server is running.
		public static string mulPath;
		public static string scriptsPath;
		//public static string ctOutputPath;
		public static string docsPath;
		public static string ndocExe;
		public static bool logToFiles;
		//public static uint saveInterval;

		public readonly static byte maximalPlevel;
		public readonly static int plevelOfGM;
		public readonly static int plevelToLscriptCommands;

		public readonly static bool kickOnSuspiciousErrors;
		public readonly static bool allowUnencryptedClients;

		public static ushort reachRange;
		public static int squaredReachRange;
		public static uint sightRange;
		public static int squaredSightRange;
		public static ushort serverMessageColor;
		public static ushort defaultUnicodeMessageColor;
		//public static uint savesPerDay;
		//public static uint savesInBackup;
		public static bool useMap;
		//public static bool useTileData;
		public static bool generateMissingDefs;
		public static bool useMultiItems;
		public static bool readBodyDefs;
		public static bool sendTileDataSpam;
		public static bool fastStartUp;
		public static bool writeMulDocsFiles;

		public static bool resolveEverythingAtStart;

		public static string importTo;
		public static bool importCodeAndComments;

		//public static bool amountPrecedingName;
		//public static bool showTitleInStatusBar;
		public static bool autoAccountCreation;
		//public static bool fastWalkPackets;
		public static bool blockOSI3DClient;
		public static bool alwaysUpdateRouterIPOnStartup;
		public static sbyte timeZone;
		public static int maxConnections;
		//public static int useCommasInXDigitNumbers;
		public static bool supportUnicode;
		public static uint speechDistance;
		public static uint emoteDistance;
		public static uint whisperDistance;
		public static uint yellDistance;
		public static bool asciiForNames;
		public static uint loginFlags;
		public static ushort featuresFlags;
		//public static int sectorSize = 4;

		public static ushort defaultItemModel;
		public static ushort defaultCharModel;

		//public static short minEffStat;

		public static bool hashPasswords;
		//public static bool allowReferencingImpliedTriggerGroup;
		//public static bool overrideIdenticallyNamedScripts;

		//public static bool dynamicMapCaching=false;
		//public static bool staticsStatistics;

		//public static bool alwaysCompileScripts;

		public static bool scriptFloats;

		//public static bool showCoreExceptions;
		//public static bool strictCompiling;
		//public static int warningLevel;

		private static bool aos;
		public static bool AOS {
			get {
				return aos;
			}
		}

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

		public static AbstractCharacter SrcChar {
			get {
				return src as AbstractCharacter;
			}
		}

		public static AbstractCharacter SrcCharacter {
			get {
				return src as AbstractCharacter;
			}
		}

		public static GameAccount SrcAccount {
			get {
				AbstractCharacter ch = src as AbstractCharacter;
				if (ch != null) {
					return ch.Account;
				}
				ConsConn console = src as ConsConn;
				if (console != null) {
					return console.Account;
				}
				return null;
			}
		}

		public static GameConn SrcGameConn {
			get {
				AbstractCharacter ch = src as AbstractCharacter;
				if (ch != null) {
					return ch.Conn;
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
			//basically does what MainClass.LoadIni previously did 
			try {
				iniH=new SEIniHandler();
				IniDataSection setup=iniH.IniSection("setup");

				serverName=(string) setup.IniEntry("name", "Unnamed SteamEngine Shard", "The name of your shard");
				adminEmail=(string) setup.IniEntry("adminEmail", "admin@email.com", "Your Email to be displayed in status web, etc.");
				hashPasswords=(bool) setup.IniEntry("hashPasswords", false, "This hashes passwords (for accounts) using SHA512, which isn't reversable. Instead of writing passwords to the save files, the hash is written. If you disable this, then passwords will be recorded instead of the hashes, and will be stored instead of the hashes, which means that if someone obtains access to your accounts file, they will be able to read the passwords. It's recommended to leave this on. Note: If you switch this off after it's been on, passwords that are hashed will stay hashed, because hashing is one-way, until that account logs in, at which point the password (if it matches) will be recorded again in place of the hash. If you use text saves, you should be able to write password=whatever in the account save, and the password will be changed to that when that save is loaded, even if you're using hashed passwords.");

				kickOnSuspiciousErrors=(bool) setup.IniEntry("kickOnSuspiciousErrors", true, "Kicks the user if a suspiciously erroneous value is recieved in a packet from their client.");

				allowUnencryptedClients=(bool) setup.IniEntry("allowUnencryptedClients", true, "Allow clients with no encryption to connect. There's no problem with that, except for lower security.");

				IniDataSection files=iniH.IniSection("files");
				logPath=((string) files.IniEntry("logPath", "logs", "Path to the log files")).Trim('/', '\\');
				savePath=((string) files.IniEntry("savePath", "saves", "Path to the save files")).Trim('/', '\\');
				scriptsPath=((string) files.IniEntry("scriptsPath", "scripts", "Path to the scripts")).Trim('/', '\\');
				//ctOutputPath=((string) files.IniEntry("classTemplatesOutputPath",Tools.CombineMultiplePaths("scripts", "generated"),"Output the files generated from classtemplates to this directory. It should be inside the scripts directory, otherwise it wont get compiled :)")).Trim('/','\\');
#if MSWIN
				ndocExe=((string) files.IniEntry("ndocExe", "C:\\Program Files\\NDoc\\bin\\.net-1.1\\NDocConsole.exe", "Command for NDoc invocation (leave it blank, if you don't want use NDoc).")).Trim('/', '\\');
#elif LINUX
				ndocExe=((string) files.IniEntry("ndocExe","","NDoc cannot be used under Linux")).Trim('/','\\');
#endif
				docsPath=((string) files.IniEntry("docsPath", "docs", "Path to the docs (Used when writing out some information from MUL files, like map tile info)")).Trim('/', '\\');

				files.Comment("If you change any of the above paths, everything which uses them should instead use the path(s) you have chosen, but comments in this file will not reflect the change, and output on the console may not, since it is simpler to display 'scripts/whatever/whatever' rather than 'C:/documents and settings/bob/documents/my scripts' (That's an example, but you could set scriptsPath to that if you really wanted to).");
				logToFiles=(bool) files.IniEntry("logToFiles", true, "Whether to log console output to a file");
				//savesPerDay=(uint)files.IniEntry("savesPerDay",(uint)15,"Maximal number of save files SE will keep in the save directory. I.E. if the amount of save subfolders oversteppes this number, SE deletes the one which covers a minimal time among the others.");
				//savesInBackup=(uint)files.IniEntry("savesInBackup",(uint)3,"When save files are copied from the main save folder to backup subfolders, SE can reduce the amount of files to this number. If 0, no backing up is done (not recommended).");


				useMap=(bool) files.IniEntry("useMap", true, "Whether to load map0.mul and statics0.mul and use them or not.");
				//dynamicMapCaching=(bool)files.IniEntry("dynamicMapCaching",true,"Whether to load and cache only parts of the map which are or recently were in use. If false, loads the entire map into RAM. False is good if you are OSI. True is good otherwise. :P");
				//useTileData=(bool)files.IniEntry("useTileData",true,"Whether to load tiledata.mul and use it or not.");
				generateMissingDefs = (bool) files.IniEntry("generateMissingDefs", false, "Whether to generate missing scripts based on tiledata.");

				useMultiItems= (bool) files.IniEntry("useMultiItems", true, "Whether to use multi items...");
				readBodyDefs=(bool) files.IniEntry("readBodyDefs", true, "Whether to read Bodyconv.def (a client file) in order to determine what character models lack defs (and then to write new ones for them to 'scripts/defaults/chardefs/newCharDefsFromMuls.def').");
				//readSoundData=(bool)files.IniEntry("readSoundData",false,"Whether to read sound.mul and dump filenames and ids to 'docs/MUL File Docs/Sounds.txt', just for reference.");
				writeMulDocsFiles=(bool) files.IniEntry("writeMulDocsFiles", false, "If this is true/on/1, then SteamEngine will write out some files with general information gathered from various MUL files into the 'docs/MUL file docs' folder. These should be distributed with SteamEngine anyways, but this is useful sometimes (like when a new UO expansion is released).");
				files.Comment("You can have more than one of these import options set to true. If you do, scripts converted from the import folder will be written out to all folders you have chosen.");
				importTo=(string) files.IniEntry("importTo", "imported", "Write scripts converted from the import folder to what folder? (Defaults, custom, or imported, or you can specify your own)");
				importCodeAndComments=(bool) files.IniEntry("importCodeAndComments", false, "True to copy code and comments from scripts being imported, and write them out again into the generated scripts. With this off, only actual defs are copied, no scripts. Only comments inside or after imported defs are copied.");
				if (importTo.LastIndexOf("/")==importTo.Length-1) {
					importTo=importTo.Substring(0, importTo.Length-1);
				} else if (importTo.LastIndexOf("\\")==importTo.Length-1) {
					importTo=importTo.Substring(0, importTo.Length-1);
				}

				IniDataSection login=iniH.IniSection("login");
				alwaysUpdateRouterIPOnStartup=(bool) login.IniEntry("alwaysUpdateRouterIPOnStartup", false, "Automagically determine the routerIP every time SteamEngine is run, instead of using the setting for it in steamengine.ini.");
				object omit=login.IniEntry("omitip", "5.0.0.0", "An IP to omit from server lists, for example, omitIP=5.0.0.0. You can have multiple omitIP lines.");
				if (omit is ArrayList) {
					ArrayList al=(ArrayList) omit;
					foreach (string ip in al) {
						Server.AddOmitIP(ip);
					}
				} else {
					Server.AddOmitIP((string) omit);
				}
				bool exists;
				string msgBox="";
				if (iniH.Exists) {
					exists=true;
					string routerIP=(string) login.IniEntry("routerIP", "", "The IP to show to people who are outside your LAN");
					//if (routerIP.Length>0) {
					Server.SetRouterIP(routerIP);
					//}
					mulPath=(string) files.IniEntry("mulPath", "muls", "Path to the mul files");
				} else {
					string mulsPath=Server.GetMulsPath();
					if (mulsPath==null) {
						msgBox+="Unable to locate the UO MUL files. Please either place them in the 'muls' folder or specify the proper path in steamengine.ini (change mulPath=muls to the proper path)\n\n";
						mulsPath=(string) files.IniEntry("mulPath", "muls", "Path to the mul files");
					} else {
						mulsPath=(string) files.IniEntry("mulPath", mulsPath, "Path to the mul files");
					}
					exists=false;
					string[] ret=Server.FindMyIP();
					string routerIP=ret[1];
					msgBox+=ret[0];
					if (routerIP==null) {
						login.IniEntry("routerIP", "", "The IP to show to people who are outside your LAN", true);
					} else {
						login.IniEntry("routerIP", routerIP, "The IP to show to people who are outside your LAN");
					}
				}
				timeZone=(sbyte) login.IniEntry("timeZone", (sbyte) 5, "What time-zone you're in. 0 is GMT, 5 is EST, etc.");
				maxConnections=(int) login.IniEntry("maxConnections", (int) 100, "The cap on # of connections. Affects the percentage-full number sent to UO client.");
				autoAccountCreation=(bool) login.IniEntry("autoAccountCreation", false, "Automatically create accounts when someone attempts to log in");
				//fastWalkPackets=(bool) login.IniEntry("fastWalkPackets", false, "Use fastwalk-check packets. Requires 9 more bytes server->client per step when on, and kills Krrios' Client because it can't handle it.");
				blockOSI3DClient=(bool) login.IniEntry("blockOSI3DClient", true, "Block the OSI 3D client from connecting. Said client is not supported, since it tends to do things in a stupid manner.");

				IniDataSection ports=iniH.IniSection("ports");
				port=(ushort) ports.IniEntry("game", (ushort) 2593, "The port to listen on for client connections");
				consoleport=(ushort) ports.IniEntry("console", (ushort) 2594, "The port to listen for (remote) console connections on");


				IniDataSection text=iniH.IniSection("text");
				commandPrefix=(string) text.IniEntry("commandPrefix", ".", "The command prefix. You can make it 'Computer, ' if you really want.");
				alternateCommandPrefix=(string) text.IniEntry("alternateCommandPrefix", "[", "The command prefix. Defaults to [. In the god-client, . is treated as an internal client command, and anything starting with . is NOT sent to the server.");
				supportUnicode=(bool) text.IniEntry("supportUnicode", true, "If you turn this off, all messages, speech, etc sent TO clients will take less bandwidth, but nobody'll be able to speak in unicode (I.E. They can only speak using normal english characters, not russian, chinese, etc.)");
				//amountPrecedingName=(bool)text.IniEntry("amountPrecedingName",false,"If true, \"5 gold coins\". If false, \"gold coins: 5\"");
				//showTitleInStatusBar=(bool)text.IniEntry("showTitleInStatusBar",true,"Include titles in status bars, after the name");
				//useCommasInXDigitNumbers=(int)text.IniEntry("useCommasInXDigitNumbers",(int)5,"Use commas only in numbers with at least this many digits. Set to 0 to never use commas in numbers.");
				asciiForNames=(bool) text.IniEntry("asciiForNames", false, "If this is on, names are always sent in ASCII regardless of what supportUnicode is set to. NOTE: Names in paperdolls and status bars can only be shown in ASCII, and this ensures that name colors come out right.");
				serverMessageColor=(ushort) text.IniEntry("serverMessageColor", (ushort) 0x0000, "The color to use for server messages (Welcome to **, pause for worldsave, etc). Can be in hex, but it doesn't have to be.");
				defaultUnicodeMessageColor=(ushort) text.IniEntry("defaultUnicodeMessageColor", (ushort) 0x0394, "The color to use for unicode messages with no specified color (or a specified color of 0, which is not really valid for unicode messages).");

				IniDataSection ranges=iniH.IniSection("ranges");
				reachRange=(ushort) ranges.IniEntry("reachRange", (ushort) 5, "The distance (in spaces) a character can reach.");
				squaredReachRange=reachRange*reachRange;
				sightRange=(uint) ranges.IniEntry("sightRange", (uint) 15, "The distance (in spaces) a character can see.");
				speechDistance=(uint) ranges.IniEntry("speechDistance", (uint) 10, "The maximum distance from which normal speech can be heard.");
				emoteDistance=(uint) ranges.IniEntry("emoteDistance", (uint) 10, "The maximum distance from which an emote can be heard/seen.");
				whisperDistance=(uint) ranges.IniEntry("whisperDistance", (uint) 2, "The maximum distance from which a whisper can be heard.");
				yellDistance=(uint) ranges.IniEntry("yellDistance", (uint) 20, "The maximum distance from which a yell can be heard.");

				IniDataSection plevels=iniH.IniSection("plevels");
				maximalPlevel=(byte) plevels.IniEntry("maximalPlevel", (byte) 7, "Maximal plevel - the highest possible plevel (the owner's plevel)");
				plevelOfGM=(int) plevels.IniEntry("plevelOfGM", (int) 4, "Plevel needed to do all the cool stuff GM do. See invis, walk thru walls, ignore line of sight, own all animals, etc.");
				plevelToLscriptCommands=(int) plevels.IniEntry("plevelToLscriptCommands", (int) 2, "With this (or higher) plevel, the client's commands are parsed and executed as LScript statements. Otherwise, much simpler parser is used, for speed and security.");

				IniDataSection scripts=iniH.IniSection("scripts");
				resolveEverythingAtStart = (bool) scripts.IniEntry("resolveEverythingAtStart", false, "If this is false, Constants and fields of scripted defs (ThingDef,Skilldef, etc.) will be resolved from the text on demand (and probably at the first save). Otherwise, everything is resolved on the start. Leave this to false on your development server, but set it to true for a live shard, because it's more secure.");
				//this has nothing to do in core, omg! -tar
				//minEffStat=(short) scripts.IniEntry("minimumEffectiveStat",(short)10,"Combat, skills, etc will treat any stat (str/int/dex/etc) that is below this as if it were this. Stats can be reduced lower than this, and lower than 0 even, this simply ensures that things still work properly.");
				defaultItemModel=(ushort) scripts.IniEntry("defaultItemModel", (ushort) 0xeed, "The item model # to use when an itemdef has no model specified.");
				defaultCharModel=(ushort) scripts.IniEntry("defaultCharModel", (ushort) 0x0190, "The character body/model # to use when a chardef has no model specified.");
				//allowReferencingImpliedTriggerGroup=(bool) scripts.IniEntry("allowReferencingImpliedTriggerGroup",(bool)true,"All itemdefs and chardefs with triggers in their scripts have an implied TriggerGroup named after themselves. If this is set to true, then you can reference those TriggerGroup by that itemdef or chardef's name. I.E. triggerGroup=i_my_cool_item in an itemdef or chardef scripts would give that itemdef or chardef all the triggers that're on i_my_cool_item.");
				//overrideIdenticallyNamedScripts=(bool) scripts.IniEntry("overrideIdenticallyNamedScripts",(bool)false,"If this is true, then itemdef/chardef/etc scripts with the same names will not cause an error. Instead, the one loaded later will override the one loaded earlier. (This is standard behavior for Sphere)");
				//alwaysCompileScripts=(bool) scripts.IniEntry("alwaysCompileScripts",(bool)false,"If this is off, SteamEngine will not recompile C#, JScript, or VB.NET scripts when none of the script files are newer than the already compiled scripts DLL (if one exists).");
				scriptFloats = (bool) scripts.IniEntry("scriptFloats", (bool) true, "If this is off, dividing/comparing 2 numbers in Lscript is treated as if they were 2 integers (rounds before the computing if needed), effectively making the scripting engine it backward compatible to old spheres. Otherwise, the precision of the .NET Double type is used in scripts.");
				Logger.showCoreExceptions = (bool) scripts.IniEntry("showCoreExceptions", (bool) true, "If this is off, only the part of Exception stacktrace that occurs in the scripts is shown. If you're debugging core, have it on. If you're debugging scripts, have it off to save some space on console.");
				//strictCompiling = (bool) scripts.IniEntry("strictCompiling",(bool)true, "If this is on, any compiler warnings are treated as errors while compiling scripts.");
				//warningLevel=(int) scripts.IniEntry("warningLevel",(int)defaultWarningLevel,"The minimum warning level you want displayed for compiled scripts. Valid values are 0-4. Greater value means more pedantic compiler. 0 means no warnings are reported at all.");
				//if (warningLevel>4 || warningLevel<0) {
				//	Logger.WriteWarning("warningLevel in "+LogStr.File("steamengine.ini")+" (section '"+scripts.Name+"') is out of range. Valid values are "+LogStr.Number("0-4")+" but current warningLevel is "+LogStr.Number(warningLevel)+". Using default level "+LogStr.Number(defaultWarningLevel)+".");
				//	warningLevel=defaultWarningLevel;
				//}

				//IniDataSection debug=iniH.IniSection("statistics");
				//debug.Comment("These are INI settings for gathering statistics purposes.");
				//staticsStatistics=(bool)debug.IniEntry("statics",false,"If dynamicMapCaching is off, will gather and display some information on the amount of statics, etc.");

				loginFlags=0;
				featuresFlags=0;

				IniDataSection features=iniH.IniSection("features");
				features.Comment("These are features which can be toggled on or off.");
				aos = (bool) features.IniEntry("AOS Features", (bool) false, "If this is on, AOS features (like objprops) are enabled.");
				//OneCharacterOnly = (bool) features.IniEntry("OneCharacterOnly", (bool)false, "Limits accounts to one character each (except GMs)).");

				//TODO:
				//OneCharacterOnly
				//SixCharacters
				//RightClickMenus
				//Perhaps Chat and LBR too? Right now they default to enabled.

				featuresFlags|=0x3;
				if (aos) {
					loginFlags|=0x20;
					//featuresFlags|=0x801c;
					featuresFlags = 0xFFFF;
				}

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


				IniDataSection temporary=iniH.IniSection("temporary");
				temporary.Comment("These are temporary INI settings, which will be going away in future versions.");
				fastStartUp=(bool) temporary.IniEntry("fastStartUp", false, "If set to true, some time consuming steps in the server init phase will be skipped (like loading of defs and scripts), for faster testing of other functions. In this mode, the server will be of course not usable for game serving.");
				sendTileDataSpam=(bool) temporary.IniEntry("sendTileDataSpam", false, "Set this to true, and you'll be sent lots of spam when you walk. Yeah, this is temporary. I need it for testing tiledata stuff. -SL");
				temporary.Comment("");
				temporary.Comment("SteamEngine determines if someone is on your LAN by comparing their IP with all of yours. If the first three parts of the IPs match, they're considered to be on your LAN. Note that this will probably not work for people on very big LANs. If you're on one, please post a feature request on our SourceForge site.");
				temporary.Comment("http://steamengine.sf.net/");

				iniH.IniDone();
				if (!exists) {
					MainClass.keepRunning = false;
					throw new ShowMessageAndExitException(msgBox+"SteamEngine has written a default 'steamengine.ini' for you. Please take a look at it, change whatever you want, and then run SteamEngine again to get started.", "Getting started");
				}
			} catch (ShowMessageAndExitException smaee) {
				Logger.Show(smaee.Message);
				smaee.Show();
				MainClass.Exit();
			} catch (Exception globalexp) {
				Console.WriteLine();
				Logger.WriteFatal(globalexp);
				MainClass.Exit();
			}
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

		public static int statClients {
			get {
				return Server.clients;
			}
		}

		public static uint statAccounts {
			get {
				return GameAccount.Instances;
			}
		}

		public static uint statItems {
			get {
				return AbstractItem.Instances;
			}
		}

		public static uint statChars {
			get {
				return AbstractCharacter.Instances;
			}
		}

		internal static IPAddress[] ips;

		public string IP {
			get {
				return Tools.ObjToString(ips);
			}
		}

		public static void Exit() {
			MainClass.keepRunning = false;
		}

		public static void SetConsoleTitle(string title) {
			Console.WriteLine("Reseting console title to '"+title+"'"+LogStr.Title(title));
		}

		public static void Save() {
			MainClass.saveFlag=true;
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

		public static Type type(string typename) {
			//global type, you can actually get any type with this
			return Type.GetType(typename, true, true);//true for case insensitive
		}

		public static Type se(string typename) {
			//Console.WriteLine("type: "+Type.GetType("SteamEngine."+typename));
			return Type.GetType("SteamEngine."+typename, true, true);
		}

		public static Type m() {
			return typeof(MainClass);
		}

		//sphere compatibility
		public static Type AccMgr() {
			return typeof(GameAccount);
		}

		public static GameAccount FindAccount(string name) {
			return GameAccount.Get(name);
		}

		public static string Hex(int numb) {
			return "0x"+Convert.ToString(numb, 16);
		}

		public static string Dec(int numb) {
			return Convert.ToString(numb, 10);
		}

		public static void Information() {
			Globals.Src.WriteLine(string.Format(
				@"Steamengine ver. {0}, Name = ""{1}"", Clients = {2}{6}Items = {3}, Chars = {4}, Mem = {5} kB",
				version, serverName, Server.clients, AbstractItem.Instances, AbstractCharacter.Instances,
				GC.GetTotalMemory(false)/1024, Environment.NewLine));
		}

		public static void I() {
			Information();
		}

		public static void B(string msg) {
			Server.BroadCast(msg);
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
			instance=new Globals();
		}

		internal static void SaveGlobals(SaveStream output) {
			Logger.WriteDebug("Saving globals.");
			output.WriteComment("Textual SteamEngine save");
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
				lastMarkServerTime = value;
			} else {
				Logger.WriteWarning("The Globals section of save is missing the "+LogStr.Ident("Time")+" value or the value is invalid, setting server time to 0");
				lastMarkServerTime = 0;
			}

			instance.LoadSectionLines(input);
		}

#if MSWIN
		private static void ndocExited(object source, EventArgs e) {
			if (ndocProcess.ExitCode!=0) {
				Logger.WriteError("NDOC was not finished correctly (exit code: "+ndocProcess.ExitCode+").");
			} else {
				Console.WriteLine("NDoc finished successfuly");
			}
			ndocProcess=null;
		}
#endif

		[Summary("First documentation in Steamengine.")]
		[Remark("This is first documentation in Steamengine, that uses SteamDoc attributes.")]
		[Return("Nothing")]
		public static void CompileDocs() {
			if (ndocProcess!=null) {
				Logger.WriteError("NDoc is already running.");
				return;
			}
			try {
				Console.WriteLine("Generating Common XML documentation file");
				DocScanner scanner=new DocScanner();
				Assembly asm = ClassManager.CommonAssembly;
				scanner.ScanAssembly(asm);
				scanner.WriteToFile("bin\\"+asm.GetName().Name+".xml");

				Console.WriteLine("Generating Core XML documentation file");
				asm = ClassManager.CoreAssembly;
				scanner=new DocScanner();
				scanner.ScanAssembly(asm);
				scanner.WriteToFile("bin\\"+asm.GetName().Name+".xml");

				Console.WriteLine("Generating Scripts XML documentation file");
				asm = ClassManager.ScriptsAssembly;
				scanner=new DocScanner();
				scanner.ScanAssembly(asm);
				scanner.WriteToFile("bin\\"+asm.GetName().Name+".xml");

			} catch (Exception e) {
				Logger.WriteError(e);
			}

#if MSWIN
			if (ndocExe.Length!=0) {
				Console.WriteLine("Invoking NDOC, documentation will be generated to docs/sourceDoc");
				try {
#if DEBUG
					string project="debug.ndoc";
#elif SANE
					string project="sane.ndoc";
#elif OPTIMIZED
					string project="optimized.ndoc";
#endif
					ProcessStartInfo info=new ProcessStartInfo(ndocExe, "-project="+project);
					info.WorkingDirectory=".\\distrib\\";
					info.WindowStyle=ProcessWindowStyle.Normal;
					info.UseShellExecute=true;
					ndocProcess=new Process();
					ndocProcess.StartInfo=info;
					ndocProcess.Exited+=new EventHandler(ndocExited);
					ndocProcess.EnableRaisingEvents=true;
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

		public static void NetStats() {
			GameConn.NetStats();
		}

		public static string GetMulDocPathFor(string filename) {
			string docPath = Path.Combine(Globals.docsPath, "defaults");
			Tools.EnsureDirectory(docPath, true);

			return Path.Combine(docPath, filename);
		}

		public static void CompressionStats() {
			PacketStats.CompressionStats();
		}

		public static void Logout() {
			ConsConn conn = Globals.Src as ConsConn;
			if (conn!=null) {
				if (!conn.IsNativeConsole) {
					conn.Close("Commanded to log out.");
				} else {
					conn.WriteLine("Native console cannot be logged out.");
				}
			}
		}

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
			PacketSender.DiscardAll();
		}

		//public static string CurrentFile { get {
		//	return WorldSaver.CurrentFile; 
		//} }

		[Summary("returns a number based on the bounds and ratio")]
		[Remark("When ratio == 0, returns min; when ratio == 1.0, returns max.<br>"
		+ "Otherwise, returns a number based on simple linear interpolation. "
		+ "Note that ratio can also be negative or >1")]
		public static double EvalRangeDouble(double ratio, double min, double max) {
			double range = max-min;
			return min+(range*ratio);
		}

		[Summary("Works like EvalRangeDouble, only the ratio parameter is in per mille (a.e. typical for skills).")]
		public static double EvalRangePermille(double pmratio, double min, double max) {
			return EvalRangeDouble(pmratio/1000.0, min, max);
		}


		public static double EvalRangePermille(double pmratio, params double[] arr) {
			return EvalRangeDouble(pmratio/1000.0, arr);
		}


		public static double EvalRangeDouble(double ratio, params double[] arr) {
			double segSize;
			int minIdx;

			int len = arr.Length;
			switch (len) {
				case 0:
					return 0;
				case 1:
					return arr[0];
				case 2:
					minIdx = 0;
					segSize = 1;
					break;
				case 3:
					//optimisation
					if (ratio >= 0.5) {
						minIdx = 1;
						ratio -= 0.5;
					} else {
						minIdx = 0;
					}
					segSize = 0.5;
					break;
				default:
					//generic array
					minIdx = (int) (ratio * len);
					len--;
					if (minIdx < 0) {
						minIdx = 0;
					}
					if (minIdx >= len) {
						minIdx = len-1;
					}
					segSize = 1.0/len;
					ratio -= minIdx * segSize;
					break;
			}

			double min = arr[minIdx];
			double max = arr[minIdx+1];
			return min + (((max - min)*ratio)/segSize);
		}

		public static int EvalRandomFaktor(int ratio, int min, int max) {
			int randomValue = Globals.dice.Next(ratio * min, ratio * max);
			return randomValue / 1000;
		}


		private static long lastMarkRealTime;
		private static long lastMarkServerTime;
		private static bool paused = true;


		public static long TimeInTicks {
			get {
				if (paused) {
					return lastMarkServerTime;
				} else {
					long current = HighPerformanceTimer.TickCount;
					return lastMarkServerTime + (current - lastMarkRealTime);
				}
			}
		}

		public static double TimeInSeconds {
			get {
				return HighPerformanceTimer.TicksToSeconds(TimeInTicks);
			}
		}

		public static TimeSpan TimeAsSpan {
			get {
				return HighPerformanceTimer.TicksToTimeSpan(TimeInTicks);
			}
		}

		[Remark("For sphere compatibility, this returns servertime in tenths of second")]
		public long Time {
			get {
				return (long) (HighPerformanceTimer.TicksToDMilliseconds(TimeInTicks)/100.0);
			}
		}

		internal static void PauseServerTime() {
			if (!paused) {
				long current = HighPerformanceTimer.TickCount;
				lastMarkServerTime = lastMarkServerTime + (current - lastMarkRealTime);
				lastMarkRealTime = current;
				paused = true;
			}
		}

		internal static void UnPauseServerTime() {
			if (paused) {
				lastMarkRealTime = HighPerformanceTimer.TickCount;
				paused = false;
			}
		}
	}
}