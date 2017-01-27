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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security;
using System.Text;

namespace SteamEngine.Common {
	public delegate void StringToSendEventHandler(string data);

	public class Logger : TextWriter {
		private static readonly TextWriter origError = Console.Out;
		private static readonly TextWriter console = Console.Error;
		private static TextWriter file;
		private static DateTime filedate;
		private static bool fileopen;
		private static Logger instance;

		[SuppressMessage("Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible")]
		public static string indentation = "";

		private const string timeFormat = "HH:mm:ss";

		private static readonly object lockObject = new object();

		[SuppressMessage("Microsoft.Design", "CA1009:DeclareEventHandlersCorrectly")]
		public static event StringToSendEventHandler OnConsoleWriteLine;
		[SuppressMessage("Microsoft.Design", "CA1009:DeclareEventHandlersCorrectly")]
		public static event StringToSendEventHandler OnConsoleWrite;

		static Logger() {

			instance = new Logger();
		}

		protected Logger() : base(CultureInfo.InvariantCulture) {
			Console.SetError(this);
			Console.SetOut(this);
			filedate = DateTime.Today;
			instance = this;
		}

		//Used by Statistics.cs to output pretty messages.
		public static void WriteLogStr(LogStr data) {
			WriteLine(data);
		}

		public static void Show(string comment, object toshow) {
			instance.WriteLine(comment + " : " + Tools.ObjToString(toshow));
		}
		public static void Show(object toshow) {
			instance.WriteLine(Tools.ObjToString(toshow));
		}

		#region WriteFatal
		public static void WriteFatal(string file, object line, object data) {
			WriteLine(LogStr.Concat(LogStr.Fatal("FATAL: "), LogStrFileLine(file, line), RenderText(data)));
		}

		public static void WriteFatal(string file, object line, string data) {
			WriteLine(LogStr.Concat(LogStr.Fatal("FATAL: "), LogStrFileLine(file, line), instance.RenderText(data)));
		}

		public static void WriteFatal(string file, object line, LogStr data) {
			WriteLine(LogStr.Concat(LogStr.Fatal("FATAL: "), LogStrFileLine(file, line), instance.RenderText(data)));
		}

		public static void WriteFatal(string file, object line, Exception data) {
			WriteLine(LogStr.Concat(LogStr.Fatal("FATAL: "), LogStrFileLine(file, line), instance.RenderText(data)));
		}

		public static void WriteFatal(string file, object line, StackTrace data) {
			WriteLine(LogStr.Concat(LogStr.Fatal("FATAL: "), LogStrFileLine(file, line), instance.RenderText(data)));
		}

		public static void WriteFatal(LogStr msg, object data) {
			WriteLine(LogStr.Concat(LogStr.Fatal("FATAL: "), msg, LogStr.Raw(": "), RenderText(data)));
		}

		public static void WriteFatal(LogStr msg, string data) {
			WriteLine(LogStr.Concat(LogStr.Fatal("FATAL: "), msg, LogStr.Raw(": "), instance.RenderText(data)));
		}

		public static void WriteFatal(LogStr msg, LogStr data) {
			WriteLine(LogStr.Concat(LogStr.Fatal("FATAL: "), msg, LogStr.Raw(": "), instance.RenderText(data)));
		}

		public static void WriteFatal(LogStr msg, Exception data) {
			WriteLine(LogStr.Concat(LogStr.Fatal("FATAL: "), msg, LogStr.Raw(": "), instance.RenderText(data)));
		}

		public static void WriteFatal(LogStr msg, StackTrace data) {
			WriteLine(LogStr.Concat(LogStr.Fatal("FATAL: "), msg, LogStr.Raw(": "), instance.RenderText(data)));
		}

		public static void WriteFatal(string msg, object data) {
			WriteLine(LogStr.Concat(LogStr.Fatal("FATAL: "), LogStr.Raw(msg + ": "), RenderText(data)));
		}

		public static void WriteFatal(string msg, string data) {
			WriteLine(LogStr.Concat(LogStr.Fatal("FATAL: "), LogStr.Raw(msg + ": "), instance.RenderText(data)));
		}

		public static void WriteFatal(string msg, LogStr data) {
			WriteLine(LogStr.Concat(LogStr.Fatal("FATAL: "), LogStr.Raw(msg + ": "), instance.RenderText(data)));
		}

		public static void WriteFatal(string msg, Exception data) {
			WriteLine(LogStr.Concat(LogStr.Fatal("FATAL: "), LogStr.Raw(msg + ": "), instance.RenderText(data)));
		}

		public static void WriteFatal(string msg, StackTrace data) {
			WriteLine(LogStr.Concat(LogStr.Fatal("FATAL: "), LogStr.Raw(msg + ": "), instance.RenderText(data)));
		}

		public static void WriteFatal(object data) {
			WriteLine(LogStr.Concat(LogStr.Fatal("FATAL: "), RenderText(data)));
		}

		public static void WriteFatal(string data) {
			WriteLine(LogStr.Concat(LogStr.Fatal("FATAL: "), instance.RenderText(data)));
		}

		public static void WriteFatal(LogStr data) {
			WriteLine(LogStr.Concat(LogStr.Fatal("FATAL: "), instance.RenderText(data)));
		}

		public static void WriteFatal(Exception data) {
			WriteLine(LogStr.Concat(LogStr.Fatal("FATAL: "), instance.RenderText(data)));
		}

		public static void WriteFatal(StackTrace data) {
			WriteLine(LogStr.Concat(LogStr.Fatal("FATAL: "), instance.RenderText(data)));
		}

		#endregion WriteFatal

		#region WriteCritical
		public static void WriteCritical(string file, object line, object data) {
			WriteLine(LogStr.Concat(LogStr.Critical("CRITICAL: "), LogStrFileLine(file, line), RenderText(data)));
		}

		public static void WriteCritical(string file, object line, string data) {
			WriteLine(LogStr.Concat(LogStr.Critical("CRITICAL: "), LogStrFileLine(file, line), instance.RenderText(data)));
		}

		public static void WriteCritical(string file, object line, LogStr data) {
			WriteLine(LogStr.Concat(LogStr.Critical("CRITICAL: "), LogStrFileLine(file, line), instance.RenderText(data)));
		}

		public static void WriteCritical(string file, object line, Exception data) {
			WriteLine(LogStr.Concat(LogStr.Critical("CRITICAL: "), LogStrFileLine(file, line), instance.RenderText(data)));
		}

		public static void WriteCritical(string file, object line, StackTrace data) {
			WriteLine(LogStr.Concat(LogStr.Critical("CRITICAL: "), LogStrFileLine(file, line), instance.RenderText(data)));
		}

		public static void WriteCritical(LogStr msg, object data) {
			WriteLine(LogStr.Concat(LogStr.Critical("CRITICAL: "), msg, LogStr.Raw(": "), RenderText(data)));
		}

		public static void WriteCritical(LogStr msg, string data) {
			WriteLine(LogStr.Concat(LogStr.Critical("CRITICAL: "), msg, LogStr.Raw(": "), instance.RenderText(data)));
		}

		public static void WriteCritical(LogStr msg, LogStr data) {
			WriteLine(LogStr.Concat(LogStr.Critical("CRITICAL: "), msg, LogStr.Raw(": "), instance.RenderText(data)));
		}

		public static void WriteCritical(LogStr msg, Exception data) {
			WriteLine(LogStr.Concat(LogStr.Critical("CRITICAL: "), msg, LogStr.Raw(": "), instance.RenderText(data)));
		}

		public static void WriteCritical(LogStr msg, StackTrace data) {
			WriteLine(LogStr.Concat(LogStr.Critical("CRITICAL: "), msg, LogStr.Raw(": "), instance.RenderText(data)));
		}

		public static void WriteCritical(string msg, object data) {
			WriteLine(LogStr.Concat(LogStr.Critical("CRITICAL: "), LogStr.Raw(msg + ": "), RenderText(data)));
		}

		public static void WriteCritical(string msg, string data) {
			WriteLine(LogStr.Concat(LogStr.Critical("CRITICAL: "), LogStr.Raw(msg + ": "), instance.RenderText(data)));
		}

		public static void WriteCritical(string msg, LogStr data) {
			WriteLine(LogStr.Concat(LogStr.Critical("CRITICAL: "), LogStr.Raw(msg + ": "), instance.RenderText(data)));
		}

		public static void WriteCritical(string msg, Exception data) {
			WriteLine(LogStr.Concat(LogStr.Critical("CRITICAL: "), LogStr.Raw(msg + ": "), instance.RenderText(data)));
		}

		public static void WriteCritical(string msg, StackTrace data) {
			WriteLine(LogStr.Concat(LogStr.Critical("CRITICAL: "), LogStr.Raw(msg + ": "), instance.RenderText(data)));
		}

		public static void WriteCritical(object data) {
			WriteLine(LogStr.Concat(LogStr.Critical("CRITICAL: "), RenderText(data)));
		}

		public static void WriteCritical(string data) {
			WriteLine(LogStr.Concat(LogStr.Critical("CRITICAL: "), instance.RenderText(data)));
		}

		public static void WriteCritical(LogStr data) {
			WriteLine(LogStr.Concat(LogStr.Critical("CRITICAL: "), instance.RenderText(data)));
		}

		public static void WriteCritical(Exception data) {
			WriteLine(LogStr.Concat(LogStr.Critical("CRITICAL: "), instance.RenderText(data)));
		}

		public static void WriteCritical(StackTrace data) {
			WriteLine(LogStr.Concat(LogStr.Critical("CRITICAL: "), instance.RenderText(data)));
		}

		#endregion WriteCritical

		#region WriteError
		public static void WriteError(string file, object line, object data) {
			WriteLine(LogStr.Concat(LogStr.Error("ERROR: "), LogStrFileLine(file, line), RenderText(data)));
		}

		public static void WriteError(string file, object line, string data) {
			WriteLine(LogStr.Concat(LogStr.Error("ERROR: "), LogStrFileLine(file, line), instance.RenderText(data)));
		}

		public static void WriteError(string file, object line, LogStr data) {
			WriteLine(LogStr.Concat(LogStr.Error("ERROR: "), LogStrFileLine(file, line), instance.RenderText(data)));
		}

		public static void WriteError(string file, object line, Exception data) {
			WriteLine(LogStr.Concat(LogStr.Error("ERROR: "), LogStrFileLine(file, line), instance.RenderText(data)));
		}

		public static void WriteError(string file, object line, StackTrace data) {
			WriteLine(LogStr.Concat(LogStr.Error("ERROR: "), LogStrFileLine(file, line), instance.RenderText(data)));
		}

		public static void WriteError(LogStr msg, object data) {
			WriteLine(LogStr.Concat(LogStr.Error("ERROR: "), msg, LogStr.Raw(": "), RenderText(data)));
		}

		public static void WriteError(LogStr msg, string data) {
			WriteLine(LogStr.Concat(LogStr.Error("ERROR: "), msg, LogStr.Raw(": "), instance.RenderText(data)));
		}

		public static void WriteError(LogStr msg, LogStr data) {
			WriteLine(LogStr.Concat(LogStr.Error("ERROR: "), msg, LogStr.Raw(": "), instance.RenderText(data)));
		}

		public static void WriteError(LogStr msg, Exception data) {
			WriteLine(LogStr.Concat(LogStr.Error("ERROR: "), msg, LogStr.Raw(": "), instance.RenderText(data)));
		}

		public static void WriteError(LogStr msg, StackTrace data) {
			WriteLine(LogStr.Concat(LogStr.Error("ERROR: "), msg, LogStr.Raw(": "), instance.RenderText(data)));
		}

		public static void WriteError(string msg, object data) {
			WriteLine(LogStr.Concat(LogStr.Error("ERROR: "), LogStr.Raw(msg + ": "), RenderText(data)));
		}

		public static void WriteError(string msg, string data) {
			WriteLine(LogStr.Concat(LogStr.Error("ERROR: "), LogStr.Raw(msg + ": "), instance.RenderText(data)));
		}

		public static void WriteError(string msg, LogStr data) {
			WriteLine(LogStr.Concat(LogStr.Error("ERROR: "), LogStr.Raw(msg + ": "), instance.RenderText(data)));
		}

		public static void WriteError(string msg, Exception data) {
			WriteLine(LogStr.Concat(LogStr.Error("ERROR: "), LogStr.Raw(msg + ": "), instance.RenderText(data)));
		}

		public static void WriteError(string msg, StackTrace data) {
			WriteLine(LogStr.Concat(LogStr.Error("ERROR: "), LogStr.Raw(msg + ": "), instance.RenderText(data)));
		}

		public static void WriteError(object data) {
			WriteLine(LogStr.Concat(LogStr.Error("ERROR: "), RenderText(data)));
		}

		public static void WriteError(string data) {
			WriteLine(LogStr.Concat(LogStr.Error("ERROR: "), instance.RenderText(data)));
		}

		public static void WriteError(LogStr data) {
			WriteLine(LogStr.Concat(LogStr.Error("ERROR: "), instance.RenderText(data)));
		}

		public static void WriteError(Exception data) {
			WriteLine(LogStr.Concat(LogStr.Error("ERROR: "), instance.RenderText(data)));
		}

		public static void WriteError(StackTrace data) {
			WriteLine(LogStr.Concat(LogStr.Error("ERROR: "), instance.RenderText(data)));
		}

		#endregion WriteError

		#region WriteWarning
		public static void WriteWarning(string file, object line, object data) {
			WriteLine(LogStr.Concat(LogStr.Warning("WARNING: "), LogStrFileLine(file, line), RenderText(data)));
		}

		public static void WriteWarning(string file, object line, string data) {
			WriteLine(LogStr.Concat(LogStr.Warning("WARNING: "), LogStrFileLine(file, line), instance.RenderText(data)));
		}

		public static void WriteWarning(string file, object line, LogStr data) {
			WriteLine(LogStr.Concat(LogStr.Warning("WARNING: "), LogStrFileLine(file, line), instance.RenderText(data)));
		}

		public static void WriteWarning(string file, object line, Exception data) {
			WriteLine(LogStr.Concat(LogStr.Warning("WARNING: "), LogStrFileLine(file, line), instance.RenderText(data)));
		}

		public static void WriteWarning(string file, object line, StackTrace data) {
			WriteLine(LogStr.Concat(LogStr.Warning("WARNING: "), LogStrFileLine(file, line), instance.RenderText(data)));
		}

		public static void WriteWarning(LogStr msg, object data) {
			WriteLine(LogStr.Concat(LogStr.Warning("WARNING: "), msg, LogStr.Raw(": "), RenderText(data)));
		}

		public static void WriteWarning(LogStr msg, string data) {
			WriteLine(LogStr.Concat(LogStr.Warning("WARNING: "), msg, LogStr.Raw(": "), instance.RenderText(data)));
		}

		public static void WriteWarning(LogStr msg, LogStr data) {
			WriteLine(LogStr.Concat(LogStr.Warning("WARNING: "), msg, LogStr.Raw(": "), instance.RenderText(data)));
		}

		public static void WriteWarning(LogStr msg, Exception data) {
			WriteLine(LogStr.Concat(LogStr.Warning("WARNING: "), msg, LogStr.Raw(": "), instance.RenderText(data)));
		}

		public static void WriteWarning(LogStr msg, StackTrace data) {
			WriteLine(LogStr.Concat(LogStr.Warning("WARNING: "), msg, LogStr.Raw(": "), instance.RenderText(data)));
		}

		public static void WriteWarning(string msg, object data) {
			WriteLine(LogStr.Concat(LogStr.Warning("WARNING: "), LogStr.Raw(msg + ": "), RenderText(data)));
		}

		public static void WriteWarning(string msg, string data) {
			WriteLine(LogStr.Concat(LogStr.Warning("WARNING: "), LogStr.Raw(msg + ": "), instance.RenderText(data)));
		}

		public static void WriteWarning(string msg, LogStr data) {
			WriteLine(LogStr.Concat(LogStr.Warning("WARNING: "), LogStr.Raw(msg + ": "), instance.RenderText(data)));
		}

		public static void WriteWarning(string msg, Exception data) {
			WriteLine(LogStr.Concat(LogStr.Warning("WARNING: "), LogStr.Raw(msg + ": "), instance.RenderText(data)));
		}

		public static void WriteWarning(string msg, StackTrace data) {
			WriteLine(LogStr.Concat(LogStr.Warning("WARNING: "), LogStr.Raw(msg + ": "), instance.RenderText(data)));
		}

		public static void WriteWarning(object data) {
			WriteLine(LogStr.Concat(LogStr.Warning("WARNING: "), RenderText(data)));
		}

		public static void WriteWarning(string data) {
			WriteLine(LogStr.Concat(LogStr.Warning("WARNING: "), instance.RenderText(data)));
		}

		public static void WriteWarning(LogStr data) {
			WriteLine(LogStr.Concat(LogStr.Warning("WARNING: "), instance.RenderText(data)));
		}

		public static void WriteWarning(Exception data) {
			WriteLine(LogStr.Concat(LogStr.Warning("WARNING: "), instance.RenderText(data)));
		}

		public static void WriteWarning(StackTrace data) {
			WriteLine(LogStr.Concat(LogStr.Warning("WARNING: "), instance.RenderText(data)));
		}

		#endregion WriteWarning

		#region WriteDebug
		[Conditional("DEBUG")]
		public static void WriteDebug(object data) {
			WriteLine(LogStr.Debug("(D) " + RenderText(data)));
		}

		[Conditional("DEBUG")]
		public static void WriteDebug(string data) {
			WriteLine(LogStr.Debug("(D) " + instance.RenderText(data)));
		}

		[Conditional("DEBUG")]
		public static void WriteDebug(LogStr data) {
			WriteLine(LogStr.Debug("(D) " + instance.RenderText(data)));
		}

		[Conditional("DEBUG")]
		public static void WriteDebug(Exception data) {
			WriteLine(LogStr.Debug("(D) " + instance.RenderText(data)));
		}

		[Conditional("DEBUG")]
		public static void WriteDebug(StackTrace data) {
			WriteLine(LogStr.Debug("(D) " + instance.RenderText(data)));
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
				WriteLine("Info: " + LogStr.Highlight(RenderText(txt)));
			}
		}
		[Conditional("TRACE")]
		public static void WriteInfo(bool ifTrue, string txt) {
			if (ifTrue) {
				WriteLine("Info: " + LogStr.Highlight(instance.RenderText(txt)));
			}
		}
		[Conditional("TRACE")]
		public static void WriteInfo(bool ifTrue, LogStr txt) {
			if (ifTrue) {
				WriteLine("Info: " + LogStr.Highlight(instance.RenderText(txt)));
			}
		}
		[Conditional("TRACE")]
		public static void WriteInfo(bool ifTrue, Exception txt) {
			if (ifTrue) {
				WriteLine("Info: " + LogStr.Highlight(instance.RenderText(txt)));
			}
		}
		[Conditional("TRACE")]
		public static void WriteInfo(bool ifTrue, StackTrace txt) {
			if (ifTrue) {
				WriteLine("Info: " + LogStr.Highlight(instance.RenderText(txt)));
			}
		}
		#endregion WriteInfo

		#region RenderText
		private static LogStr RenderText(object data) {
			Exception e = data as Exception;
			if (e != null) {
				return instance.RenderText(e);
			}
			LogStr ls = data as LogStr;
			if (ls != null) {
				return instance.RenderText(ls);
			}
			StackTrace stackTrace = data as StackTrace;
			if (stackTrace != null) {
				return instance.RenderText(stackTrace);
			}
			return instance.RenderText(string.Concat(data));
		}

		protected virtual LogStr RenderText(LogStr data) {
			return data;
		}

		protected virtual LogStr RenderText(string data) {
			return LogStr.Raw(data);
		}

		protected virtual LogStr RenderText(Exception e) {
			if (e != null) {
				LogStrBuilder builder = new LogStrBuilder();
				string str = "\t";
				RenderException(ref str, builder, e);
				return builder.ToLogStr();
			}
			return LogStr.Raw("");
		}

		public virtual LogStr RenderText(StackTrace stackTrace) {
			if (stackTrace != null) {
				LogStrBuilder builder = new LogStrBuilder();
				RenderStackTrace("\t", builder, stackTrace);
				return builder.ToLogStr();
			}
			return LogStr.Raw("");
		}

		internal static void RenderException(ref string leftPad, LogStrBuilder builder, Exception e) {
			Exception innerEx = e.InnerException;
			if (innerEx != null) {
				RenderException(ref leftPad, builder, innerEx);
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
			RenderStackTrace(leftPad, builder, trace);
			leftPad += "\t";
		}

		private static void RenderStackTrace(string leftPad, LogStrBuilder builder, StackTrace trace) {
			int n = trace.FrameCount;

			for (int i = 0; i < n; i++) {
				StackFrame frame = trace.GetFrame(i);
				MethodBase methodBase = frame.GetMethod();
				if (methodBase != null) {
					builder.Append(Environment.NewLine);
					builder.Append(leftPad);
					builder.Append("at ");

					Type declaringType = methodBase.DeclaringType;
					if (declaringType != null) {
						builder.Append(Tools.TypeToString(declaringType));
						builder.Append(".");
					}
					builder.Append(methodBase.Name);

					MethodInfo method = methodBase as MethodInfo;
					if ((method != null) && method.IsGenericMethod) {
						Type[] genericArguments = method.GetGenericArguments();
						builder.Append("<");
						int index = 0;
						bool displayComma = true;
						while (index < genericArguments.Length) {
							if (!displayComma) {
								builder.Append(",");
							} else {
								displayComma = false;
							}
							builder.Append(Tools.TypeToString(genericArguments[index]));
							index++;
						}
						builder.Append(">");
					}
					builder.Append("(");
					ParameterInfo[] parameters = methodBase.GetParameters();
					bool commaDisplayed = true;
					for (int j = 0; j < parameters.Length; j++) {
						if (!commaDisplayed) {
							builder.Append(", ");
						} else {
							commaDisplayed = false;
						}
						string name = "<UnknownType>";
						if (parameters[j].ParameterType != null) {
							name = Tools.TypeToString(parameters[j].ParameterType);
						}
						builder.Append(name + " " + parameters[j].Name);
					}
					builder.Append(") ");

					if (frame.GetILOffset() != -1) {
						string fileName = null;
						try {
							fileName = frame.GetFileName();
						} catch (SecurityException) {
						}
						if (fileName != null) {
							builder.Append(' ');
							builder.Append(LogStr.FileLine(fileName, frame.GetFileLineNumber()));
						}
					}
				}
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public static LogStr LogStrFileLine(string file, object line) {
			try {
				int lineInt = Convert.ToInt32(line, CultureInfo.InvariantCulture);
				return LogStr.FileLine(file, lineInt);
			} catch {
			}
			return LogStr.Raw(FileString(file, line));
		}

		static string FileString(string filename, object line) {
			return "(" + filename + ", " + line + ") ";
		}
		#endregion RenderText

		public static void SetTitle(string title) {
			Write(LogStr.Title(title));
		}

		protected static void OpenFile() {
			string pathname = instance.GetFilepath();
			string dirname = Path.GetDirectoryName(pathname);
			Tools.EnsureDirectory(dirname, true);
			try {
				file = File.AppendText(pathname);
				fileopen = true;
			} catch (FatalException fe) {
				throw new FatalException("Re-throwing", fe);
			} catch (Exception e) {
				throw new ShowMessageAndExitException("Unable to open log file - SteamEngine is already running? (" + e.Message + ")", "This is NOT an Error -9! *wink*");
				//some general I/O exception
			}
			filedate = DateTime.Today;
			console.WriteLine("");
			console.WriteLine("");
			console.WriteLine("Log file open - " + pathname);
			//console - with file name
			file.WriteLine("Log file open");
			//log - without file names...
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		protected override void Dispose(bool disposing) {
			try {
				if (fileopen) {
					file.Close();
					fileopen = false;
				}
			} catch (FatalException) {
				throw;
			} catch {
				//sometimes the file is disposed ahead of time without fileopen being set to false.
			} finally {
				base.Dispose(disposing);
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

		protected virtual string GetFilepath() {
			throw new InvalidOperationException("Logger not initalized.");
		}

		static void Rotate() {
			file.Flush();
			if (filedate != DateTime.Today) {
				file.Close();
				fileopen = false;
				OpenFile();
			}
		}

		//method: Log
		//similar to WriteLine, only it does not show the string on the console - 
		//it only writes it to file
		public static void Log(string data) {
			lock (lockObject) {
				if (fileopen) {
					file.WriteLine(DateTime.Now.ToString(timeFormat, CultureInfo.InvariantCulture) + ": " + data);
					Rotate();
				}
			}
		}

		public static void StaticWriteLine(object value) {
			instance.WriteLine(value);
		}

		public override void WriteLine(string value) {
			lock (lockObject) {
				string printline = string.Concat(DateTime.Now.ToString(timeFormat, CultureInfo.InvariantCulture), ": ", indentation, value);
				console.WriteLine(printline);
				if (OnConsoleWriteLine != null) {
					OnConsoleWriteLine(printline);
				}
				if (fileopen) {
					file.WriteLine(printline);
					Rotate();
				}
			}
		}

		public override void WriteLine(object value) {
			WriteLine(RenderText(value));
		}

		public static void WriteLine(LogStr value) {
			lock (lockObject) {
				LogStr printline = LogStr.Concat((LogStr) DateTime.Now.ToString(timeFormat, CultureInfo.InvariantCulture), (LogStr) ": ", (LogStr) indentation, value);
				if (console != null) {
					console.WriteLine(printline.RawString);
					if (OnConsoleWriteLine != null) {
						OnConsoleWriteLine(printline.NiceString);
					}
					if (fileopen) {
						file.WriteLine(printline.RawString);
						Rotate();
					}
				} else { //console == null -> Logger not yet initialised
					Console.WriteLine(printline.RawString);
				}
			}
		}

		public override void Write(string value) {
			lock (lockObject) {
				console.Write(value);
				if (OnConsoleWrite != null) {
					OnConsoleWrite(value);
				}
				if (fileopen) {
					file.Write(value);
					Rotate();
				}
			}
		}

		public override void Write(object value) {
			Write(RenderText(value));
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public static void Write(LogStr value) {
			lock (lockObject) {
				console.Write(value.RawString);
				if (OnConsoleWrite != null) {
					OnConsoleWrite(value.NiceString);
				}
				if (fileopen) {
					file.Write(value.RawString);
					Rotate();
				}
			}
		}

		public override Encoding Encoding {
			//they made me override it
			get { return (null); }
		}
	}
}
