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
using System.Globalization;
using SteamEngine.Common;
using System.Configuration;
using SteamEngine;

namespace SteamEngine.Converter {

	public static class ConverterMain  {
		public static bool AdditionalConverterMessages = true; //AdditionalConverterMessages = TagMath.ToBoolean(ConfigurationSettings.AppSettings["Additional converter Messages"]);
		//private static ArrayList convertedDefs = new ArrayList();
		public static List<ConvertedFile> memFiles = new List<ConvertedFile>();
		public static string convertToPath = null;
		public static string convertPath = null;
		
		public static ConvertedFile currentIFile;
		
		private static StringToSend consoleDelegate;
		
		public static void CreateFolders() {
			convertPath = Path.Combine("SphereShardConverter", "convert");
			Tools.EnsureDirectory(convertPath, true);

			//convertToPath = Path.Combine("SphereShardConverter", "converted");

			convertToPath = Path.Combine("scripts", "converted");
			Tools.EnsureDirectory(convertToPath, true);

			//foreach (string path in Directory.GetFileSystemEntries(convertToPath)) {
			//    try {
			//        if (Directory.Exists(path)) {
			//            Directory.Delete(path, true);
			//        } else {
			//            File.Delete(path);
			//        }
			//    } catch {}
			//}

		}
		
		public static void WinStart(StringToSend consSend) {
			//this is run if the converter is started by the WinConsole
			try {
				//SteamEngine.MainClass.winConsole=new ConsConn(consSend);
				consoleDelegate = consSend;
				Logger.OnConsoleWriteLine += new StringToSend(ConsoleWriteLine);
				Logger.OnConsoleWrite += new StringToSend(ConsoleWrite);
			} catch (Exception e) {
				Console.WriteLine (e.Message);
				return;
			}
			Main();
		}
		
		private static void ConsoleWriteLine(string str) {
			consoleDelegate(str+Environment.NewLine);
		}
		
		private static void ConsoleWrite(string str) {
			consoleDelegate(str);
		}
		
		public static void Main() {
			Tools.ExitBinDirectory();
			//Directory.SetCurrentDirectory("SphereShardConverter");
			ConverterLogger.Init();

			TileData.Init();

			try {
				CreateFolders();
				string origCurDir = Path.GetFullPath(Directory.GetCurrentDirectory());
				Directory.SetCurrentDirectory(convertPath);
				CreateFileList(".");
				Directory.SetCurrentDirectory(origCurDir);
				Console.WriteLine("Converting "+memFiles.Count+" Sphere script files.");
				
				foreach (ConvertedFile file in memFiles) {
					//Logger.WriteDebug("Reading file "+file.origPath);
					ConvertFile(file);
				}
				Console.WriteLine("Files loaded and parsed.");
	
				foreach (ConvertedFile file in memFiles) {
					//Logger.WriteDebug("Working on file "+file.origPath);
					foreach (ConvertedDef def in file.defs) {
						def.FirstStage();
					}
				}
				InvokeStaticMethodOnDefClasses("FirstStageFinished");
				Console.WriteLine("First stage finished.");
				
				foreach (ConvertedFile file in memFiles) {
					//Logger.WriteDebug("Working on file "+file.origPath);
					foreach (ConvertedDef def in file.defs) {
						def.SecondStage();
					}
				}
				InvokeStaticMethodOnDefClasses("SecondStageFinished");
				Console.WriteLine("Second stage finished.");
				
				foreach (ConvertedFile file in memFiles) {
					//Logger.WriteDebug("Working on file "+file.origPath);
					foreach (ConvertedDef def in file.defs) {
						def.ThirdStage();
					}
				}
				InvokeStaticMethodOnDefClasses("ThirdStageFinished");
				Console.WriteLine("Third stage finished.");

				foreach (ConvertedFile mf in memFiles) {
					mf.Flush();
				}
				Console.WriteLine("Files written to disk.");
			} catch (Exception e) {
				Logger.WriteError(e);
			} finally {
				Console.WriteLine("Converting finished.");
			}
		}

		private static void InvokeStaticMethodOnDefClasses(string methodname) {
			foreach (Type type in Assembly.GetExecutingAssembly().GetTypes()) {
				if (typeof(ConvertedDef).IsAssignableFrom(type)) {
					MethodInfo m = type.GetMethod(methodname, BindingFlags.Static|BindingFlags.Public|BindingFlags.DeclaredOnly ); 
					if (m!=null) {
						m.Invoke(null, null);
					}
				}
			}
		}
		
		private static void CreateFileList(string folder) {
			//Console.WriteLine("converting from folder "+LogStr.File(folder));
			
			foreach (string subfolder in Directory.GetDirectories(folder)) {
				CreateFileList(subfolder);
			}
			
			string[] filenames = Directory.GetFiles(folder, "*.scp");
			foreach (string filename in filenames) {
				ConvertedFile file = new ConvertedFile(filename);
				memFiles.Add(file);
			}
		}

		public static void ConvertFile(ConvertedFile f) {
			currentIFile = f;

			using (StreamReader stream = File.OpenText(f.origPath)) {
				foreach (PropsSection input in
						PropsFileParser.Load(f.origPath, stream, StartsAsScript)) {

					ConvertedDef cd = null;
					try {
						string type = input.headerType.ToLower();
						string name = input.headerName;
						if ((name == "")&&(type == "eof")) {
							continue;
						}

						switch (type) {
							case "itemdef":
								cd = new ConvertedItemDef(input);
								break;
							case "chardef":
								cd = new ConvertedCharDef(input);
								break;
							case "area":
								cd = new ConvertedRegion(input);
								break;
							case "template":
								cd = new ConvertedTemplateDef(input);
								break;
							case "defname":
							case "defnames":
								cd = new ConvertedConstants(input);
								break;
						}
					} catch (FatalException) {
						throw;
					} catch (SEException se) {
						se.TryAddFileLineInfo(input.filename, input.headerLine);
						Logger.WriteError(se.NiceMessage);
						continue;
					} catch (Exception e) {
						Logger.WriteError(input.filename, input.headerLine, e);
						continue;
					}
					if (cd == null) {
						cd = new ConvertedDef(input);
						cd.DontDump();
						//Logger.WriteWarning(WorldSaver.currentfile, input.headerLine, "Unknown section "+LogStr.Ident(input));
					}
					currentIFile.AddDef(cd);
					//convertedDefs.Add(cd);
					continue;

					//							if (sectionName=="itemdef") {
					//								string id=SphereNumberCheck(param, false);
					//								curDef=new convertedItemDef(id);
					//								convertedDefs.Add(curDef);
					//							} else if (sectionName=="chardef") {
					//								string id=SphereNumberCheck(param, false);
					//								curDef=new convertedCharDef(id);
					//								convertedDefs.Add(curDef);
					//							//} else if (sectionName=="template") {
					//							//	string id=SphereNumberCheck(param, false);
					//							//	curDef=new convertedTemplateDef(id);
					//							//	convertedDefs.Add(curDef);
					//							} else if (sectionName=="area") {
					//								curDef = new convertedRegion(param);
					//								convertedDefs.Add(curDef);
					//							} else {
					//								curDef=null;
					//							}

				}
			}
		}

		internal static bool StartsAsScript(string headerType) {
			switch (headerType.ToLower()) {
				case "function":
				case "dialog":
				case "template":
				case "defname":
				case "defnames":
					return true;
				case "itemdef":
				case "chardef":
				case "area":
					return false;
			}
			return true;//temp
		}
	}
}