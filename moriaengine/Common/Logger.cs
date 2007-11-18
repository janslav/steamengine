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
using System.Reflection;
using System.IO;
using System.Text;
using System.Diagnostics;
using SteamEngine.Common;

namespace SteamEngine.Common {
	public delegate void StringToSend(string data);
	
	public abstract class Logger : TextWriter {
		static TextWriter origError;
		static TextWriter console;
		static TextWriter file;
		static DateTime filedate;
		static bool fileopen=false;
		static Logger instance;
		public static bool showCoreExceptions = true;
		public static Assembly scriptsAssembly;
		public static string indentation = "";

		public static readonly string timeFormat = "HH:mm:ss";
		
		public static event StringToSend OnConsoleWriteLine;
		public static event StringToSend OnConsoleWrite;

		protected Logger() {
			if (instance == null) {
				console=Console.Out;
				origError = Console.Error;
				filedate=DateTime.Today;
				Console.SetError(this);
				Console.SetOut(this);
				instance = this;
			}
		}

		//Used by Statistics.cs to output pretty messages.
		public static void WriteLogStr(LogStr data) {
			instance.WriteLine(data);
		}
		
		public static void Show(string comment,object toshow) {
			instance.WriteLine(comment+" : "+Tools.ObjToString(toshow));
		}
		public static void Show(object toshow) {
			instance.WriteLine(Tools.ObjToString(toshow));
		}

		#region WriteFatal
		public static void WriteFatal(string file, object line, object data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Fatal("FATAL: "), LogStrFileLine(file, line), ErrText(data)));
		}

		public static void WriteFatal(string file, object line, string data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Fatal("FATAL: "), LogStrFileLine(file, line), ErrText(data)));
		}

		public static void WriteFatal(string file, object line, LogStr data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Fatal("FATAL: "), LogStrFileLine(file, line), ErrText(data)));
		}

		public static void WriteFatal(string file, object line, Exception data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Fatal("FATAL: "), LogStrFileLine(file, line), ErrText(data)));
		}

		public static void WriteFatal(string file, object line, SEException data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Fatal("FATAL: "), LogStrFileLine(file, line), ErrText(data)));
		}

		public static void WriteFatal(LogStr msg, object data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Fatal("FATAL: "), msg, LogStr.Raw(": "), ErrText(data)));
		}

		public static void WriteFatal(LogStr msg, string data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Fatal("FATAL: "), msg, LogStr.Raw(": "), ErrText(data)));
		}

		public static void WriteFatal(LogStr msg, LogStr data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Fatal("FATAL: "), msg, LogStr.Raw(": "), ErrText(data)));
		}

		public static void WriteFatal(LogStr msg, Exception data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Fatal("FATAL: "), msg, LogStr.Raw(": "), ErrText(data)));
		}

		public static void WriteFatal(LogStr msg, SEException data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Fatal("FATAL: "), msg, LogStr.Raw(": "), ErrText(data)));
		}

		public static void WriteFatal(string msg, object data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Fatal("FATAL: "), LogStr.Raw(msg+": "), ErrText(data)));
		}

		public static void WriteFatal(string msg, string data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Fatal("FATAL: "), LogStr.Raw(msg+": "), ErrText(data)));
		}

		public static void WriteFatal(string msg, LogStr data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Fatal("FATAL: "), LogStr.Raw(msg+": "), ErrText(data)));
		}

		public static void WriteFatal(string msg, Exception data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Fatal("FATAL: "), LogStr.Raw(msg+": "), ErrText(data)));
		}

		public static void WriteFatal(string msg, SEException data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Fatal("FATAL: "), LogStr.Raw(msg+": "), ErrText(data)));
		}

		public static void WriteFatal(object data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Fatal("FATAL: "), ErrText(data)));
		}

		public static void WriteFatal(string data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Fatal("FATAL: "), ErrText(data)));
		}

		public static void WriteFatal(LogStr data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Fatal("FATAL: "), ErrText(data)));
		}

		public static void WriteFatal(Exception data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Fatal("FATAL: "), ErrText(data)));
		}

		public static void WriteFatal(SEException data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Fatal("FATAL: "), ErrText(data)));
		}

		#endregion WriteFatal

		#region WriteCritical
		public static void WriteCritical(string file, object line, object data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Critical("CRITICAL: "), LogStrFileLine(file, line), ErrText(data)));
		}

		public static void WriteCritical(string file, object line, string data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Critical("CRITICAL: "), LogStrFileLine(file, line), ErrText(data)));
		}

		public static void WriteCritical(string file, object line, LogStr data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Critical("CRITICAL: "), LogStrFileLine(file, line), ErrText(data)));
		}

		public static void WriteCritical(string file, object line, Exception data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Critical("CRITICAL: "), LogStrFileLine(file, line), ErrText(data)));
		}

		public static void WriteCritical(string file, object line, SEException data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Critical("CRITICAL: "), LogStrFileLine(file, line), ErrText(data)));
		}

		public static void WriteCritical(LogStr msg, object data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Critical("CRITICAL: "), msg, LogStr.Raw(": "), ErrText(data)));
		}

		public static void WriteCritical(LogStr msg, string data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Critical("CRITICAL: "), msg, LogStr.Raw(": "), ErrText(data)));
		}

		public static void WriteCritical(LogStr msg, LogStr data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Critical("CRITICAL: "), msg, LogStr.Raw(": "), ErrText(data)));
		}

		public static void WriteCritical(LogStr msg, Exception data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Critical("CRITICAL: "), msg, LogStr.Raw(": "), ErrText(data)));
		}

		public static void WriteCritical(LogStr msg, SEException data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Critical("CRITICAL: "), msg, LogStr.Raw(": "), ErrText(data)));
		}

		public static void WriteCritical(string msg, object data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Critical("CRITICAL: "), LogStr.Raw(msg+": "), ErrText(data)));
		}

		public static void WriteCritical(string msg, string data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Critical("CRITICAL: "), LogStr.Raw(msg+": "), ErrText(data)));
		}

		public static void WriteCritical(string msg, LogStr data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Critical("CRITICAL: "), LogStr.Raw(msg+": "), ErrText(data)));
		}

		public static void WriteCritical(string msg, Exception data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Critical("CRITICAL: "), LogStr.Raw(msg+": "), ErrText(data)));
		}

		public static void WriteCritical(string msg, SEException data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Critical("CRITICAL: "), LogStr.Raw(msg+": "), ErrText(data)));
		}

		public static void WriteCritical(object data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Critical("CRITICAL: "), ErrText(data)));
		}

		public static void WriteCritical(string data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Critical("CRITICAL: "), ErrText(data)));
		}

		public static void WriteCritical(LogStr data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Critical("CRITICAL: "), ErrText(data)));
		}

		public static void WriteCritical(Exception data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Critical("CRITICAL: "), ErrText(data)));
		}

		public static void WriteCritical(SEException data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Critical("CRITICAL: "), ErrText(data)));
		}

		#endregion WriteCritical

		#region WriteError
		public static void WriteError(string file, object line, object data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Error("ERROR: "), LogStrFileLine(file, line), ErrText(data)));
		}

		public static void WriteError(string file, object line, string data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Error("ERROR: "), LogStrFileLine(file, line), ErrText(data)));
		}

		public static void WriteError(string file, object line, LogStr data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Error("ERROR: "), LogStrFileLine(file, line), ErrText(data)));
		}

		public static void WriteError(string file, object line, Exception data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Error("ERROR: "), LogStrFileLine(file, line), ErrText(data)));
		}

		public static void WriteError(string file, object line, SEException data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Error("ERROR: "), LogStrFileLine(file, line), ErrText(data)));
		}

		public static void WriteError(LogStr msg, object data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Error("ERROR: "), msg, LogStr.Raw(": "), ErrText(data)));
		}

		public static void WriteError(LogStr msg, string data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Error("ERROR: "), msg, LogStr.Raw(": "), ErrText(data)));
		}

		public static void WriteError(LogStr msg, LogStr data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Error("ERROR: "), msg, LogStr.Raw(": "), ErrText(data)));
		}

		public static void WriteError(LogStr msg, Exception data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Error("ERROR: "), msg, LogStr.Raw(": "), ErrText(data)));
		}

		public static void WriteError(LogStr msg, SEException data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Error("ERROR: "), msg, LogStr.Raw(": "), ErrText(data)));
		}

		public static void WriteError(string msg, object data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Error("ERROR: "), LogStr.Raw(msg+": "), ErrText(data)));
		}

		public static void WriteError(string msg, string data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Error("ERROR: "), LogStr.Raw(msg+": "), ErrText(data)));
		}

		public static void WriteError(string msg, LogStr data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Error("ERROR: "), LogStr.Raw(msg+": "), ErrText(data)));
		}

		public static void WriteError(string msg, Exception data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Error("ERROR: "), LogStr.Raw(msg+": "), ErrText(data)));
		}

		public static void WriteError(string msg, SEException data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Error("ERROR: "), LogStr.Raw(msg+": "), ErrText(data)));
		}

		public static void WriteError(object data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Error("ERROR: "), ErrText(data)));
		}

		public static void WriteError(string data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Error("ERROR: "), ErrText(data)));
		}

		public static void WriteError(LogStr data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Error("ERROR: "), ErrText(data)));
		}

		public static void WriteError(Exception data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Error("ERROR: "), ErrText(data)));
		}

		public static void WriteError(SEException data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Error("ERROR: "), ErrText(data)));
		}

		#endregion WriteError

		#region WriteWarning
		public static void WriteWarning(string file, object line, object data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Warning("WARNING: "), LogStrFileLine(file, line), ErrText(data)));
		}

		public static void WriteWarning(string file, object line, string data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Warning("WARNING: "), LogStrFileLine(file, line), ErrText(data)));
		}

		public static void WriteWarning(string file, object line, LogStr data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Warning("WARNING: "), LogStrFileLine(file, line), ErrText(data)));
		}

		public static void WriteWarning(string file, object line, Exception data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Warning("WARNING: "), LogStrFileLine(file, line), ErrText(data)));
		}

		public static void WriteWarning(string file, object line, SEException data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Warning("WARNING: "), LogStrFileLine(file, line), ErrText(data)));
		}

		public static void WriteWarning(LogStr msg, object data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Warning("WARNING: "), msg, LogStr.Raw(": "), ErrText(data)));
		}

		public static void WriteWarning(LogStr msg, string data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Warning("WARNING: "), msg, LogStr.Raw(": "), ErrText(data)));
		}

		public static void WriteWarning(LogStr msg, LogStr data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Warning("WARNING: "), msg, LogStr.Raw(": "), ErrText(data)));
		}

		public static void WriteWarning(LogStr msg, Exception data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Warning("WARNING: "), msg, LogStr.Raw(": "), ErrText(data)));
		}

		public static void WriteWarning(LogStr msg, SEException data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Warning("WARNING: "), msg, LogStr.Raw(": "), ErrText(data)));
		}

		public static void WriteWarning(string msg, object data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Warning("WARNING: "), LogStr.Raw(msg+": "), ErrText(data)));
		}

		public static void WriteWarning(string msg, string data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Warning("WARNING: "), LogStr.Raw(msg+": "), ErrText(data)));
		}

		public static void WriteWarning(string msg, LogStr data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Warning("WARNING: "), LogStr.Raw(msg+": "), ErrText(data)));
		}

		public static void WriteWarning(string msg, Exception data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Warning("WARNING: "), LogStr.Raw(msg+": "), ErrText(data)));
		}

		public static void WriteWarning(string msg, SEException data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Warning("WARNING: "), LogStr.Raw(msg+": "), ErrText(data)));
		}

		public static void WriteWarning(object data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Warning("WARNING: "), ErrText(data)));
		}

		public static void WriteWarning(string data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Warning("WARNING: "), ErrText(data)));
		}

		public static void WriteWarning(LogStr data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Warning("WARNING: "), ErrText(data)));
		}

		public static void WriteWarning(Exception data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Warning("WARNING: "), ErrText(data)));
		}

		public static void WriteWarning(SEException data) {
			instance.WriteLine(LogStr.Concat(
				LogStr.Warning("WARNING: "), ErrText(data)));
		}

		#endregion WriteWarning

		#region WriteDebug
		[Conditional("DEBUG")]
		public static void WriteDebug(object data) {
			instance.WriteLine(LogStr.Debug("(D) ")+LogStr.DebugData(ErrText(data)));
		}

		[Conditional("DEBUG")]
		public static void WriteDebug(string data) {
			instance.WriteLine(LogStr.Debug("(D) ")+LogStr.DebugData(ErrText(data)));
		}

		[Conditional("DEBUG")]
		public static void WriteDebug(LogStr data) {
			instance.WriteLine(LogStr.Debug("(D) ")+LogStr.DebugData(ErrText(data)));
		}

		[Conditional("DEBUG")]
		public static void WriteDebug(Exception data) {
			instance.WriteLine(LogStr.Debug("(D) ")+LogStr.DebugData(ErrText(data)));
		}

		[Conditional("DEBUG")]
		public static void WriteDebug(SEException data) {
			instance.WriteLine(LogStr.Debug("(D) ")+LogStr.DebugData(ErrText(data)));
		}
		#endregion WriteDebug

		#region WriteInfo
		//An informational message, intended for use with something like: bool GameConnTracingOn = TagMath.GetBool(ConfigurationSettings.AppSettings["GameConn Tracing"]));
		//And something in the app config file like:
		//<appdata>
		//	<add key="GameConn Tracing" value="on" />
		//</appdata>
		[Conditional("TRACE")]
		public static void WriteInfo(bool ifTrue, object txt) {
			if (ifTrue) {
				instance.WriteLine("Info: "+LogStr.Highlight(txt));
			}
		}
		[Conditional("TRACE")]
		public static void WriteInfo(bool ifTrue, string txt) {
			if (ifTrue) {
				instance.WriteLine("Info: "+LogStr.Highlight(ErrText(txt)));
			}
		}
		[Conditional("TRACE")]
		public static void WriteInfo(bool ifTrue, LogStr txt) {
			if (ifTrue) {
				instance.WriteLine("Info: "+LogStr.Highlight(txt));
			}
		}
		[Conditional("TRACE")]
		public static void WriteInfo(bool ifTrue, Exception txt) {
			if (ifTrue) {
				instance.WriteLine("Info: "+LogStr.Highlight(ErrText(txt)));
			}
		}
		[Conditional("TRACE")]
		public static void WriteInfo(bool ifTrue, SEException txt) {
			if (ifTrue) {
				instance.WriteLine("Info: "+LogStr.Highlight(txt));
			}
		}
		#endregion WriteInfo

		#region ErrText
		private static LogStr ErrText(object data) {
			Exception e = data as Exception;
			if (e != null) {
				return ErrText(e);
			}
			LogStr ls = data as LogStr;
			if (ls != null) {
				return ErrText(ls);
			}
			return ErrText(String.Concat(data));
		}

		private static LogStr ErrText(LogStr data) {
			return data;
		}

		private static LogStr ErrText(string data) {
			return LogStr.Raw(data);
		}

		private static LogStr ErrText(Exception e) {
			if (e != null) {
				LogStrBuilder builder = new LogStrBuilder();
				string str = "\t";
				RenderStackTrace(ref str, builder, e);
				return builder.ToLogStr();
			} else {
				return LogStr.Raw("");
			}
		}

		private static void RenderStackTrace(ref string leftPad, LogStrBuilder builder, Exception e) {
			Exception innerEx = e.InnerException;
			if (innerEx != null) {
				RenderStackTrace(ref leftPad, builder, innerEx);
				//builder.Append(Environment.NewLine);
				//builder.Append(leftPad);
				builder.Append(" ---> ");
			}

			SEException see = e as SEException;
			if (see != null) {
				builder.Append(see.NiceMessage);
			} else {
				builder.Append(e.Message);
			}

			StackTrace trace = new StackTrace(e, true);
			int n = trace.FrameCount;
			bool showedCoreSign = false;
			for (int i = 0; i < n; i++) {
				StackFrame frame = trace.GetFrame(i);
				MethodBase method = frame.GetMethod();
				if (method != null) {
					if (!showCoreExceptions) {
						if (method.DeclaringType.Assembly != scriptsAssembly) {
							if (!showedCoreSign) {
								showedCoreSign = true;
								builder.Append(LogStr.DebugData("  [...]  "));
							}
							continue;
						}
					}
					builder.Append(Environment.NewLine);
					builder.Append(leftPad);
					builder.Append("at ");

					Type declaringType = method.DeclaringType;
					if (declaringType != null) {
						builder.Append(declaringType.FullName.Replace('+', '.'));
						builder.Append(".");
					}
					builder.Append(method.Name);
					if ((method is MethodInfo) && ((MethodInfo) method).IsGenericMethod) {
						Type[] genericArguments = ((MethodInfo) method).GetGenericArguments();
						builder.Append("[");
						int index = 0;
						bool displayComma = true;
						while (index < genericArguments.Length) {
							if (!displayComma) {
								builder.Append(",");
							} else {
								displayComma = false;
							}
							builder.Append(genericArguments[index].Name);
							index++;
						}
						builder.Append("]");
					}
					builder.Append("(");
					ParameterInfo[] parameters = method.GetParameters();
					bool commaDisplayed = true;
					for (int j = 0; j < parameters.Length; j++) {
						if (!commaDisplayed) {
							builder.Append(", ");
						} else {
							commaDisplayed = false;
						}
						string name = "<UnknownType>";
						if (parameters[j].ParameterType != null) {
							name = parameters[j].ParameterType.Name;
						}
						builder.Append(name + " " + parameters[j].Name);
					}
					builder.Append(") ");

					if (frame.GetILOffset() != -1) {
						string fileName = null;
						try {
							fileName = frame.GetFileName();
						} catch (System.Security.SecurityException) {
						}
						if (fileName != null) {
							builder.Append(' ');
							builder.Append(LogStr.FileLine(fileName, frame.GetFileLineNumber()));
						}
					}
				}
			}
			leftPad += "\t";
		}

		public static LogStr LogStrFileLine(string file, object line) {
			try {
				int lineInt = Convert.ToInt32(line);
				return LogStr.FileLine(file, lineInt);
			} catch { }
			return LogStr.Raw(FileString(file, line));
		}

		static string FileString(string filename,object line) {
			return "("+filename+", "+line+") ";
		}
		#endregion ErrText

		public static void SetTitle(string title) {
			instance.Write(LogStr.Title(title));
		}

		protected static void OpenFile() {
			string pathname= instance.GetFilepath();
			string dirname=Path.GetDirectoryName(pathname);
			Tools.EnsureDirectory(dirname, true);
			try {
				file=File.AppendText(pathname);
				fileopen=true;
			} catch (FatalException fe) {
				throw new FatalException("Re-throwing", fe);
			} catch (Exception e) {
				throw new ShowMessageAndExitException("Unable to open log file - SteamEngine is already running? ("+e.Message+")","This is NOT an Error -9! *wink*"); //some general I/O exception
			}
			filedate=DateTime.Today;
			console.WriteLine("");
			console.WriteLine("");
			console.WriteLine("Log file open - "+pathname); //console - with file name
			file.WriteLine("Log file open"); //log - without file names...
		}
		
		~Logger() {
			try {
				if (fileopen)
					file.Close();
			} catch (FatalException fe) {
				throw new FatalException("Re-throwing", fe);
			} catch (Exception) {	//sometimes the file is disposed ahead of time without fileopen being set to false.
			}
		}

		public static void StopListeningConsole() {
			Console.SetOut(console);
			Console.SetError(origError);
		}

		public static void ResumeListeningConsole() {
			Console.SetError(instance);
			Console.SetOut(instance);
		}
	  
	  	protected abstract string GetFilepath();
	
		static void Rotate() {
			file.Flush();
			if (filedate!=DateTime.Today) {
				file.Close();
				fileopen=false;
				OpenFile();
			}
		}
		
		//method: Log
		//similar to WriteLine, only it does not show the string on the console - 
		//it only writes it to file
		public static void Log(string data) {
			lock (instance) {
				if (fileopen) {
					file.WriteLine(DateTime.Now.ToString(timeFormat)+": "+data);
					Rotate();
				}
			}
		}

		public static void StaticWriteLine(object data) {
			instance.WriteLine(data);
		}

		public override void WriteLine(string data) {
			lock (this) {
				string printline=String.Concat(DateTime.Now.ToString(timeFormat), ": ", indentation, data);
				console.WriteLine(printline);
				if (OnConsoleWriteLine!=null) {
					OnConsoleWriteLine(printline);
				}
				if (fileopen) {
					file.WriteLine(printline);
					Rotate();
				}
			}
		}

		public override void WriteLine(object data) {
			WriteLine(ErrText(data));
		}
		
		public void WriteLine(LogStr data) {
			lock (this) {
				LogStr printline=LogStr.Concat((LogStr) DateTime.Now.ToString(timeFormat), (LogStr) ": ", (LogStr) indentation, data);
				console.WriteLine(printline.RawString);
				if (OnConsoleWriteLine != null) {
					OnConsoleWriteLine(printline.NiceString);
				}
				if (fileopen) {
					file.WriteLine(printline.RawString);
					Rotate();
				}
			}
		}
		
		public override void Write(string data) {
			lock (this) {
				console.Write(data);
				if (OnConsoleWrite!=null) {
					OnConsoleWrite(data);
				}
				if (fileopen) {
					file.Write(data);
					Rotate();
				}
			}
		}

		public override void Write(object data) {
			Write(ErrText(data));
		}

		public void Write(LogStr data) {
			lock (this) {
				console.Write(data.RawString);
				if (OnConsoleWrite!=null) {
					OnConsoleWrite(data.NiceString);
				}
				if (fileopen) {
					file.Write(data.RawString);
					Rotate();
				}
			}
		}
		
		public override Encoding Encoding { //they made me override it
			get{return(null);}
		}
	}
}