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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Configuration;
using SteamEngine.Common;
using SteamEngine.Networking;
using SteamEngine.LScript;
using SteamEngine.Communication.TCP;

namespace SteamEngine {
	public static class Commands {
		//method: PlayerCommand
		//this is invoked when a player types a command in game

		internal static bool commandRunning;

		public static void PlayerCommand(GameState state, string command) {
			string lower = command.ToLower();
			string noprefix;

			AbstractCharacter commandSrc = state.CharacterNotNull;

			if ((lower.StartsWith("x ") || lower.StartsWith("x."))) {
				noprefix = command.Substring(2);
			} else if ((lower.StartsWith("set ") || lower.StartsWith("set."))) {
				noprefix = command.Substring(4);
			} else {
#if DEBUG
				long ticksBefore = HighPerformanceTimer.TickCount;
#endif
				InvokeCommand(commandSrc, commandSrc, command);
#if DEBUG
				Logger.WriteDebug("Command took (in ms): " + HighPerformanceTimer.TicksToMilliseconds(HighPerformanceTimer.TickCount - ticksBefore));
#endif
				return;
			}

			if (AuthorizeCommand(commandSrc, "x")) {
				state.WriteLine("Command who or what?");
				state.Target(false, Commands.xCommand_Targon, Commands.xCommand_Cancel,
					new XCommandParameter(commandSrc, noprefix));
				LogCommand(commandSrc, command, true, null);
			} else {
				LogCommand(commandSrc, command, false, commandAuthorisationFailed);
			}
		}

		//        public static void PlayerCommand(GameConn c, string command) {
		//            string lower = command.ToLower();
		//            string noprefix;

		//            AbstractCharacter commandSrc = c.CurCharacter;

		//            if ((lower.StartsWith("x ") || lower.StartsWith("x."))) {
		//                noprefix = command.Substring(2);
		//            } else if ((lower.StartsWith("set ") || lower.StartsWith("set."))) {
		//                noprefix = command.Substring(4);
		//            } else {
		//#if DEBUG
		//                long ticksBefore = HighPerformanceTimer.TickCount;
		//#endif
		//                InvokeCommand(commandSrc, commandSrc, command);
		//#if DEBUG
		//                Logger.WriteDebug("Command took (in ms): "+ HighPerformanceTimer.TicksToMilliseconds(HighPerformanceTimer.TickCount - ticksBefore));
		//#endif
		//                return;
		//            }

		//            if (AuthorizeCommand(commandSrc, "x")) {
		//                c.WriteLine("Command who or what?");
		//                c.Target(false, Commands.xCommand_Targon, Commands.xCommand_Cancel, 
		//                    new XCommandParameter(commandSrc, noprefix));
		//                LogCommand(commandSrc, command, true, null);
		//            } else {
		//                LogCommand(commandSrc, command, false, commandAuthorisationFailed);
		//            }
		//        }

		private class XCommandParameter {
			internal readonly ISrc commandSrc;
			internal readonly string commandWithoutPrefix;

			internal XCommandParameter(ISrc commandSrc, string commandWithoutPrefix) {
				this.commandSrc = commandSrc;
				this.commandWithoutPrefix = commandWithoutPrefix;
			}
		}

		//method: ConsoleCommand
		//this is invoked directly by consoles
		public static void ConsoleCommand(ConsoleDummy c, string command) {
			if (RunLevelManager.IsAwaitingRetry) {
				if (command == "exit") {//check if we can run it?
					MainClass.signalExit.Set();
				} else {
					MainClass.RetryRecompilingScripts();
				}
				return;
			}
			Globals.SetSrc(c);
#if DEBUG
			long ticksBefore = HighPerformanceTimer.TickCount;
#endif
			InvokeCommand(c, Globals.instance, command);
#if DEBUG
			Logger.WriteDebug("Command took (in ms): " + HighPerformanceTimer.TicksToMilliseconds(HighPerformanceTimer.TickCount - ticksBefore));
#endif
		}

		private static void LogCommand(ISrc commandSrc, string command, bool success, object err) {
			if (success) {
				Console.WriteLine("'" + commandSrc.Account.Name + "' commands '" + command + "'. OK");
			} else {
				string errText = "";
				Exception e = err as Exception;
				if (e != null) {
					errText = e.Message;
				} else {
					errText = string.Concat(err);
				}
				Console.WriteLine("'" + commandSrc.Account.Name + "' commands '" + command + "'. ERR: " + errText);
				commandSrc.WriteLine("Command '" + command + "' failed - " + errText);
			}
		}

		public const string commandAuthorisationFailed = "No permission to run that command";
		//passing just the argument name may be too primitive, but I think it should be enough
		public static bool AuthorizeCommand(ISrc commandSrc, string name) {
			if (commandRunning) {
				ScriptArgs sa = new ScriptArgs(commandSrc, name);
				if (Globals.instance.TryCancellableTrigger(TriggerKey.command, sa)) {
					return false;
				}
			}
			return true;
		}

		public static void AuthorizeCommandThrow(ISrc commandSrc, string name) {
			if (commandRunning) {
				ScriptArgs sa = new ScriptArgs(commandSrc, name);
				if (Globals.instance.TryCancellableTrigger(TriggerKey.command, sa)) {
					throw new Exception(commandAuthorisationFailed);
				}
			}
		}

		static CacheDictionary<string, LScriptHolder> gmCommandsCache = new CacheDictionary<string, LScriptHolder>(1000, false, StringComparer.Ordinal);

		public static void ClearGmCommandsCache() {
			gmCommandsCache.Clear();
		}

		private static void InvokeCommand(ISrc commandSrc, TagHolder self, string code) {
			Sanity.IfTrueThrow(commandSrc == null, "commandSrc cannot be null in Commands.InvokeCommand");

			Globals.SetSrc(commandSrc);
			try {
				commandRunning = true;
				if (commandSrc.MaxPlevel < Globals.plevelToLscriptCommands) {
					string errText;
					bool success = SimpleCommandParser.TryRunSnippet(commandSrc, self, code, out errText);
					LogCommand(commandSrc, code, success, errText);
				} else {
					string codeAsKey = String.Concat(self == null ? typeof(void).FullName : self.GetType().FullName, code);
					LScriptHolder scriptHolder;
					if (!gmCommandsCache.TryGetValue(codeAsKey, out scriptHolder)) {
						try {
							scriptHolder = LScript.LScript.GetNewSnippetRunner("<command>", 0, self, code);
						} catch (FatalException) {
							throw;
						} catch (Exception e) {
							LogCommand(commandSrc, code, false, e);
							return;
						}
					}
					scriptHolder.TryRun(self, (ScriptArgs) null);
					if (scriptHolder.lastRunSuccesful) {
						gmCommandsCache[codeAsKey] = scriptHolder;
					} else {
						gmCommandsCache.Remove(codeAsKey);
					}
					LogCommand(commandSrc, code, scriptHolder.lastRunSuccesful, scriptHolder.lastRunException);
				}
			} finally {
				commandRunning = false;
			}

			////performance test
			//string errText;
			//int n = 1000;
			//long start = HighPerformanceTimer.TickCount;
			//for (int i = 0; i<n; i++) {
			//	SimpleCommandParser.TryRunSnippet(self, code, out errText);
			//}
			//Console.WriteLine("simpleparser: "+HighPerformanceTimer.TicksToMilliseconds(HighPerformanceTimer.TickCount - start)+" ms");
			//start = HighPerformanceTimer.TickCount;
			//for (int i = 0; i<n; i++) {
			//	LScript.LScript.TryRunSnippet("<command>", 0, self, code);
			//}
			//Console.WriteLine("lscript: "+HighPerformanceTimer.TicksToMilliseconds(HighPerformanceTimer.TickCount - start)+" ms");
		}

		private static SteamEngine.Networking.OnTargon xCommand_Targon = XCommand_Targon;
		private static SteamEngine.Networking.OnTargon_Cancel xCommand_Cancel = XCommand_Cancel;

		public static void XCommand_Cancel(GameState state, object parameter) {
			//?
		}

		public static void XCommand_Targon(GameState state, IPoint3D getback, object parameter) {
			TagHolder self = getback as TagHolder;
			if (self != null) {
				XCommandParameter xcp = (XCommandParameter) parameter;
				InvokeCommand(xcp.commandSrc, self, xcp.commandWithoutPrefix);
			}
		}
	}

	class SimpleCommandParser {
		static Regex commandRE = new Regex(@"(?<name>\w+)(\s+(?<arg>.+))?",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);


		[Summary("Runs a method or function of the object self, given by name, and possible "
		+ "one argument of numeric or string type, separated by space from the name")]
		public static bool TryRunSnippet(ISrc commandSrc, TagHolder self, string code, out string errText) {
			Match m = commandRE.Match(code);
			if (m.Success) {
				string name = m.Groups["name"].Value;
				string arg = m.Groups["arg"].Value;
				bool haveArg = false;
				bool argIsNumber = false;
				object argAsNumber = null;

				if (!Commands.AuthorizeCommand(commandSrc, name)) {
					errText = Commands.commandAuthorisationFailed;
					return false;
				}

				if (arg.Length > 0) {
					haveArg = true;
					argIsNumber = TagMath.TryParseAnyNumber(arg, out argAsNumber);
				}

				ScriptHolder func = ScriptHolder.GetFunction(name);
				if (func != null) {
					if (argIsNumber) {
						func.TryRun(self, new ScriptArgs(argAsNumber));
					} else {
						func.TryRun(self, new ScriptArgs(arg));
					}
					if (func.lastRunException != null) {
						errText = func.lastRunException.Message;
					} else {
						errText = "";
					}
					return func.lastRunSuccesful;
				}

				Type argType;
				bool nameMatched;
				MethodInfo mi = FindMethod(self.GetType(), name, haveArg, argIsNumber, out argType, out nameMatched);
				if (mi != null) {
					try {
						if (haveArg) {
							if (argIsNumber) {
								mi.Invoke(self, new object[] { Convert.ChangeType(argAsNumber, argType) });
							} else {
								mi.Invoke(self, new object[] { arg });
							}
						} else {
							mi.Invoke(self, null);
						}
						errText = "";
						return true;
					} catch (FatalException) {
						throw;
					} catch (Exception e) {
						Logger.WriteError(e);
						errText = e.Message;
					}
				} else if (nameMatched) {
					errText = "Wrong argument for that method";
				} else {
					errText = "Unknown method/function " + name;
				}
			} else {
				errText = "Unrecognized command format";
			}
			return false;
		}

		private static MethodInfo FindMethod(Type type, string name, bool hasArg, bool argIsNumber, out Type argType, out bool nameMatched) {
			nameMatched = false;
			argType = null;
			foreach (MethodInfo mi in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)) {
				if (string.Compare(name, mi.Name, true) == 0) { //true for case insensitive
					ParameterInfo[] pis = mi.GetParameters();
					if (hasArg) {
						if (pis.Length == 1) {
							argType = pis[0].ParameterType;
							if (argIsNumber) {
								if (TagMath.IsNumberType(argType)) {
									return mi;
								}
							} else if (argType.Equals(typeof(string))) {
								return mi;
							}
						}
					} else if (pis.Length == 0) {
						return mi;
					}
					nameMatched = true;
				}
			}
			return null;
		}
	}
}