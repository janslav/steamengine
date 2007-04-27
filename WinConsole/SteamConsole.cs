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
using System.Collections;
using System.Text;
using System.IO;
using System.Threading;
using System.Reflection;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Net;
using System.Net.Sockets;
using System.Globalization;
using Microsoft.CSharp; 
using SteamEngine.Common;
using NAnt.Core;

namespace SteamClients {
	public enum ConsoleStates {Plain,Remote,Native,NativeDestroyed,
		NativeConnected,RemoteConnected,RemoteConnecting};

	internal delegate void Notify();

	public struct SteamConnection {
		private int port;
		private string name;
		private string address;
		private string userName;
		private string password;

		public string Name {
			set { name=value; }
			get { return name; }
		}
		public string Address {
			set { address=value; }
			get { return address; }
		}
		public string UserName {
			set { userName=value; }
			get { return userName; }
		}
		public string Password {
			set { password=value; }
			get { return password; }
		}

		public int Port {
			set { 
				Debug.Assert((value>=0 && value<65536),"Port number out of range");
				if (value<0 || value > 65535) {
					throw new Exception("Port number out of range");
				}
				port=value;
			}
			get { return port; }
		}

		public override string ToString() {
			return name;
		}
	}

	public interface IConsoleListener {
		void NativeFailed();
		void RemoteFailed();
		void StateChanged();
		void WriteLine(string data);
		void WriteLine(object data);
		void WriteLine(LogStr data);
		void Write(string data);
		void Write(object data);
		void Write(LogStr data);
	}

	public class SteamConsole {
		protected enum Notifications {NativeFailed=0,RemoteFailed,StateChanged};

		protected static SteamConsole consoleInstance;
		protected IConsoleListener consoleListener;
		protected string loginName;
		protected string loginPassword;
		protected string hostAddress;
		protected int hostPort;
		protected bool isLogged;
		protected bool isRunning;
		protected bool tryingConnect;

		private ConsoleStates currentState;
		private ConsoleStates previousState;
		private Queue msgQueue;
		private Queue notificationQueue;
		private bool[] notificationTable;
		private Notify[] callTable;
		private string nAntTaskName;
		private string nAntResultProperty;
		private SteamConnection remoteConnection;
		private Assembly steamCore;
		private StringToSend stringSender;
		private MethodInfo serverHandleCommand;
		private PropertyInfo serverRunLevel;
		private Thread workerThread;
		private string nativeThreadName="SteamConsole native thread";
		private string remoteThreadName="SteamConsole remote thread";
		private Socket steamSocket;
		private IPEndPoint endPoint;
		private Encoding steamEncoding;

		public static void NativeReceiver(string msg) {
			Debug.Assert(consoleInstance!=null,"NativeReceive called before console instantiation");
			consoleInstance.EnqueueMessage(msg);
		}

		public SteamConsole(IConsoleListener listener) {
			steamCore=null;
			isRunning=false;
			isLogged=false;

			consoleListener=listener;
			msgQueue=new Queue();
			notificationQueue=new Queue();
			callTable=new Notify[] {new Notify(OnNativeFailed),new Notify(OnRemoteFailed),
									   new Notify(OnStateChanged)};
			notificationTable=new bool[Enum.GetValues(typeof(Notifications)).Length];
			ResetNotificationTable();
			consoleInstance=this;
		}

		#region Properties
		public ConsoleStates CurrentState {
			get { lock (typeof(ConsoleStates)) {return currentState;} }
		}

		public ConsoleStates PreviousState {
			get { lock (typeof(ConsoleStates)) {return previousState;}}
		}

		public RunLevels RunLevel {
			get {
				if (serverRunLevel!=null) {
					object o=serverRunLevel.GetValue(null,null);
					if (o is RunLevels)
						return ((RunLevels) o);
				}
				return RunLevels.Unknown;
			}
		}

		protected Assembly Core {
			get { return steamCore; }
		}
		protected bool IsConnected {
			get { return (steamSocket!=null && steamSocket.Connected); }
		}
		protected bool IsLogged {
			get { return (IsConnected && isLogged); }
		}
		protected bool IsRemote {
			get { return (currentState==ConsoleStates.Remote || currentState==ConsoleStates.RemoteConnected
					  || currentState==ConsoleStates.RemoteConnecting); }
		}
		#endregion

		#region Public methods
		public void Logout() {
			switch (currentState) {
				case ConsoleStates.RemoteConnected :
					Send("logout");
					CloseConnection();
					break;
				case ConsoleStates.RemoteConnecting :
					Abort();
					CloseConnection();
					break;
			}
		}

		public virtual void Send(string str) {
			if (stringSender!=null)
				stringSender(str);
		}

		public void Abort() {
			if (workerThread!=null && workerThread.IsAlive) {
				workerThread.Abort();
			}
		}

		public void Exit() {
			Logout();
		}

		public void StartNative(string nAntTaskName,string nAntResultProperty) {
			try {
				this.nAntTaskName=nAntTaskName;
				this.nAntResultProperty=nAntResultProperty;
				isRunning=false;
				ThreadStart nativeStarter;
				if (WinConsoleForm.winForm.runConverter) {
					nativeStarter=new ThreadStart(this.TryConverter);
				} else {
					nativeStarter=new ThreadStart(this.TryNative);
				}
				workerThread=new Thread(nativeStarter);
				workerThread.Name=nativeThreadName;
				workerThread.IsBackground=true;
				workerThread.Start();
			} catch (Exception e) {
				WriteLine(e.Message);
			}
		}

		public void StartRemote(SteamConnection con) {
			try {
				workerThread=new Thread(new ThreadStart(this.TryRemote));
				workerThread.Name=remoteThreadName;
				isRunning=false;
				remoteConnection=con;
				tryingConnect=true;
				workerThread.Start();
			} catch (Exception e) {
				WriteLine(e.Message);
			}
		}

		public virtual void DispatchNotifications() {
			bool done;
			Notifications not;

			lock(notificationQueue.SyncRoot) {
				done=(notificationQueue.Count<=0);
			}
			while (!done) {
				lock(notificationQueue.SyncRoot) {
					not=(Notifications)notificationQueue.Dequeue();
					notificationTable[(int)not]=false;
					done=(notificationQueue.Count<=0);
				}
				if (callTable[(int)not]!=null) {
					callTable[(int)not]();
				}
			}
		}

		public virtual bool DispatchMessages() {
			bool done, any_messages;
			object msg;

			lock(msgQueue.SyncRoot) {
				done=(msgQueue.Count<=0);
			}
			any_messages=!done;
			while (!done) {
				lock(msgQueue.SyncRoot) {
					msg=msgQueue.Dequeue();
					done=(msgQueue.Count<=0);
				}
				if (msg is string) {
					Write(msg as string);
				} else {
					if (msg is LogStr) {
						Write(msg as LogStr);
					} else {
						Write(msg);
					}
				}
			}
			return any_messages;
		}
		#endregion

		#region Protected methods
		protected virtual void NativeSend(string data) {
			if ((RunLevel==RunLevels.Running) || (RunLevel==RunLevels.AwaitingRetry)) {
				serverHandleCommand.Invoke(null, new object[] {data} );
			}
		}

		protected virtual void ConsoleLoop() {
			byte[] receieved = new byte[255]; 
			int len;

			while(IsConnected && isLogged) {
				if (steamSocket.Poll(0,SelectMode.SelectRead)) {
					try {
						len=steamSocket.Receive(receieved);
					} catch (Exception) {
						len=-1;
					}
					if (len>0) {
						string s=steamEncoding.GetString(receieved).Substring(0,len);
						if (s.Trim() != "") {
							EnqueueMessage(s);
						}
					}
					else {
						EnqueueMessage("Connection lost."+Environment.NewLine);
						CloseConnection();
					}
				}

				Thread.Sleep(1);
			}
		}

		protected virtual void EnqueueMessage(object msg) {
			lock(msgQueue.SyncRoot) {
				msgQueue.Enqueue(msg);
			}
		}

		private void ResetNotificationTable() {
			int i;
			for (i=0;i<notificationTable.Length;i++) {
				notificationTable[i]=false;
			}
		}

		protected virtual void Write(LogStr msg) {
			consoleListener.Write(msg);
		}

		protected virtual void WriteLine(LogStr msg) {
			consoleListener.WriteLine(msg);
		}

		protected virtual void Write(string msg) {
			consoleListener.Write(msg);
		}

		protected virtual void WriteLine(string msg) {
			consoleListener.WriteLine(msg);
		}

		protected virtual void Write(object msg) {
			consoleListener.Write(msg);
		}

		protected virtual void WriteLine(object msg) {
			consoleListener.WriteLine(msg);
		}

		protected virtual void OnNativeFailed() {
			consoleListener.NativeFailed();
		}

		protected virtual void OnRemoteFailed() {
			consoleListener.RemoteFailed();
		}

		protected virtual void OnStateChanged() {
			consoleListener.StateChanged();
		}
		#endregion

		#region Private methods
		private void CloseConnection() {
			if (IsConnected) {
				steamSocket.Close();
				steamSocket=null;
				isLogged=false;
			}
		}

		private void WaitForAndSend(string wait,string send) {
			byte[] receieved = new byte[255]; 
			int len;

			while (!steamSocket.Poll(0,SelectMode.SelectRead)) {
				Thread.Sleep(0);
			}
			len=steamSocket.Receive(receieved);
			if (len>0) {
				string s=steamEncoding.GetString(receieved);
				s=s.Trim();
				if (s.StartsWith(wait)) {
					Send(send);
				} else {
					throw new Exception(wait+" expected but "+s+" received, connection lost.");
				}
			} else {
				throw new Exception(wait+" expected but nothing received, connection lost.");
			}
		}

		private bool Login(string name,string password) {
			if (!IsConnected && !isLogged)
				return false;

			isLogged=false;
			Send(" ");
			try {
				WaitForAndSend("Username?:",name);
				WaitForAndSend("Password?:",password);
			}
			catch (ThreadAbortException e) {
				throw e;
			}
			catch (Exception e) {
				EnqueueMessage(e.Message+Environment.NewLine);
				return false;
			}
			isLogged=true;

			return true;
		}

		private void Connect(SteamConnection con) {
			if (IsConnected) {
				CloseConnection();
			}
			EnqueueMessage("Creating Socket... "+Environment.NewLine);

			try {
				EnterState(ConsoleStates.RemoteConnecting);
				if (steamEncoding==null) {
					steamEncoding=new UTF8Encoding();
				}

				steamSocket=new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
				EnqueueMessage("Resolving remote address "+con.Address+Environment.NewLine);
				if (endPoint!=null) {
					endPoint.Address=Dns.GetHostEntry(con.Address).AddressList[0];
					endPoint.Port=con.Port;
				} else {
					endPoint=new IPEndPoint(Dns.GetHostEntry(con.Address).AddressList[0], con.Port);
				}

				EnqueueMessage("Connecting to "+endPoint);
				steamSocket.Connect(endPoint);

				stringSender = new StringToSend(RemoteSend);
				hostAddress=con.Address;
				hostPort=con.Port;
				Login(con.UserName,con.Password);
				EnterState(ConsoleStates.RemoteConnected);
			}
			catch (ThreadAbortException e) {
				throw e;
			}
			catch (Exception e) {
				EnqueueMessage("Unable to connect to "+endPoint+": "+e.Message);
			}
		}

		private void RemoteSend(string data) {
			if (IsConnected) {
				steamSocket.Send(steamEncoding.GetBytes(data+Environment.NewLine));
			}
		}

		private void TryRemote() {
			try {
				EnqueueMessage("Trying to start as remote console"+Environment.NewLine);
				Connect(remoteConnection);
				if (IsConnected && isLogged) {
					ConsoleLoop();
				}
				EnterState(ConsoleStates.Remote);
			} catch (ThreadAbortException) {
				EnqueueMessage("Connection aborted."+Environment.NewLine);
				Notify(Notifications.RemoteFailed);
			} catch (Exception e) {
				EnqueueMessage(e.Message+Environment.NewLine);
				Notify(Notifications.RemoteFailed);
			}
			finally {
				EnterState(ConsoleStates.Remote);
			}
		}

		private void TryNative() {
			MethodInfo m=null;
			StringToSend disp=null;

			try {
				isRunning=true;
				EnqueueMessage("Trying to start as native console."+Environment.NewLine);

				EnterState(ConsoleStates.Native);
				
				bool core_loaded=LoadCore();
				if (core_loaded) {
					disp=new StringToSend(NativeReceiver);
					stringSender=new StringToSend(this.NativeSend);
					serverHandleCommand=steamCore.GetType("SteamEngine.MainClass").GetMethod("winConsoleCommand");
					serverRunLevel=steamCore.GetType("SteamEngine.MainClass").GetProperty("RunLevel");										

					m=steamCore.GetType("SteamEngine.MainClass").GetMethod("WinStart");
				} else {
					EnqueueMessage("Cannot start in native mode, use remote connection instead."+Environment.NewLine);
					Notify(Notifications.NativeFailed);
					EnterState(ConsoleStates.Remote);
					return;
				}
			} catch (Exception e) {
				EnqueueMessage(e.Message+Environment.NewLine);
				EnqueueMessage("Cannot start in native mode, use remote connection instead."+Environment.NewLine);
				Notify(Notifications.NativeFailed);
				EnterState(ConsoleStates.Remote);
				return;
			}

			try {
				Debug.Assert(disp!=null,"SteamConsole.TryNative: disp cannot be nul");
				Debug.Assert(disp!=null,"SteamConsole.TryNative: m cannot be nul");
				isRunning=true;
				EnterState(ConsoleStates.NativeConnected);
				m.Invoke(null, new object[] {disp});
			} catch (Exception e) {
				EnqueueMessage(e.Message+Environment.NewLine);
				EnqueueMessage("Cannot start in native mode, use remote connection instead."+Environment.NewLine);
				Notify(Notifications.NativeFailed);
				EnterState(ConsoleStates.Remote);
				return;
			} finally {
				EnterState(ConsoleStates.NativeDestroyed);
			}
		}
		
		private void TryConverter() {
			MethodInfo m=null;
			StringToSend disp=null;

			try {
				isRunning=true;
				EnqueueMessage("Trying to start Converter."+Environment.NewLine);

				EnterState(ConsoleStates.Native);
				
				bool core_loaded=LoadCore();
				if (core_loaded) {
					disp=new StringToSend(NativeReceiver);
					m=steamCore.GetType("SteamEngine.Converter.ConverterMain").GetMethod("WinStart");
				} else {
					EnqueueMessage("Cannot start the converter..."+Environment.NewLine);
					Notify(Notifications.NativeFailed);
					return;
				}
			} catch (Exception e) {
				EnqueueMessage(e.Message+Environment.NewLine);
				EnqueueMessage("Cannot start the converter..."+Environment.NewLine);
				Notify(Notifications.NativeFailed);
				return;
			}

			try {
				Debug.Assert(disp!=null,"SteamConsole.TryNative: disp cannot be nul");
				Debug.Assert(disp!=null,"SteamConsole.TryNative: m cannot be nul");
				isRunning=true;
				EnterState(ConsoleStates.NativeConnected);
				m.Invoke(null, new object[] {disp});
			} catch (Exception e) {
				EnqueueMessage(e+Environment.NewLine);
				EnqueueMessage("Cannot start the converter..."+Environment.NewLine);
				Notify(Notifications.NativeFailed);
				return;
			} finally {
				EnterState(ConsoleStates.NativeDestroyed);
			}
		}
		
		private class GuiNantLogger : DefaultLogger {
			SteamConsole sc;
			
			public override void BuildFinished(object sender, BuildEventArgs e) {}
			public override void BuildStarted(object sender, BuildEventArgs e) {}
			public override void TargetFinished(object sender, BuildEventArgs e) {}
			public override void TargetStarted(object sender, BuildEventArgs e) {}
			public override void TaskFinished(object sender, BuildEventArgs e) {}
			public override void TaskStarted(object sender, BuildEventArgs e) {}

			
			public GuiNantLogger(SteamConsole sc) {
				this.sc = sc;
			}
			
			protected override void Log(string pMessage) {
				object o = NantLauncher.GetDecoratedLogMessage(pMessage);
				if (o != null) {
					sc.EnqueueMessage(o);
					sc.EnqueueMessage(Environment.NewLine);
				}
			}
		}

		private bool LoadCore() {
			NantLauncher nant = new NantLauncher();
			nant.SetLogger(new GuiNantLogger(this));
			nant.SetPropertiesAsSelf();
			nant.SetTarget(this.nAntTaskName);
			nant.Execute();
			if (nant.WasSuccess()) {
				steamCore = nant.GetCompiledAssembly(this.nAntResultProperty);
				EnqueueMessage("Done loading/compiling core."+Environment.NewLine);
				return true;
			} else {
				EnqueueMessage("Compiling core failed."+Environment.NewLine);
				return false;
			}
			
//			try {
//				CodeDomProvider compiler = new CSharpCodeProvider();
//				SourceNames sourceNames = GetSourceNames(sourcePath, ".cs");
//				string[] fileNames = new string[sourceNames.fileNames.Count];
//				sourceNames.fileNames.CopyTo(fileNames); 
//				
//				//now compare the creationtimes of the dll and the scripts
//				bool loadedDll = false;
//				if (!WinConsoleForm.winForm.runConverter && File.Exists(exePath)) {
//					if (File.GetLastWriteTime(exePath) > sourceNames.newestFileDate) {
//						try {
//							loadedDll=true;
//							steamCore=Assembly.LoadFrom(exePath);
//						} catch (Exception e) {
//							EnqueueMessage("Exception when loading core executable:"+Environment.NewLine+e+Environment.NewLine);
//						}
//					}
//				}
//				if (!loadedDll) {
//					if (fileNames.Length==0) {
//						throw new FileNotFoundException("None found.");
//					}
//					EnqueueMessage("Compiling "+fileNames.Length+" source file(s)."+Environment.NewLine);
//
//					CompilerParameters cp = new CompilerParameters();
//					cp.ReferencedAssemblies.Add("System.dll");
//					cp.ReferencedAssemblies.Add("Microsoft.JScript.dll");
//					cp.ReferencedAssemblies.Add("System.Windows.Forms.dll");
//					cp.ReferencedAssemblies.Add("System.Drawing.dll");
//					cp.ReferencedAssemblies.Add("bin\\SteamDoc.dll");
//					cp.ReferencedAssemblies.Add("System.Xml.dll");
//					cp.ReferencedAssemblies.Add("System.configuration.dll");
//
//					
//					cp.TreatWarningsAsErrors=true; //compiling should stop when warnings appear
//					cp.CompilerOptions="/t:exe /win32icon:bin\\seicon.ico /d:MSWIN ";
//					cp.WarningLevel=4;
//#if TESTRUNUO
//						cp.CompilerOptions+="/d:TESTRUNUO ";
//						cp.ReferencedAssemblies.Add("RunUO.exe");
//#endif
//
//#if DEBUG
//					cp.IncludeDebugInformation=true;
//					cp.CompilerOptions+="/debug+ /d:DEBUG /d:TRACE ";
//					if (File.Exists("bin\\Debug_fastdll.dll")) {
//						cp.CompilerOptions+="/resource:bin\\Debug_fastdll.lib /d:USEFASTDLL /unsafe ";
//					}
//					string commondllname = "bin\\Debug_Common.dll";
//					string exeNameForConverter = "bin\\Debug_SteamEngine.exe";
//#elif SANE
//					string commondllname = "bin\\Sane_Common.dll";
//					string exeNameForConverter = "bin\\Sane_SteamEngine.exe";
//					cp.CompilerOptions+="/d:TRACE /d:SANE ";
//					if (File.Exists("bin\\fastdll.dll")) {
//						cp.CompilerOptions+="/resource:bin\\fastdll.lib /d:USEFASTDLL /unsafe ";
//					}
//#elif OPTIMIZED
//					cp.CompilerOptions+="/d:OPTIMIZED /o+ ";
//					string commondllname = "bin\\Optimized_Common.dll";
//					string exeNameForConverter = "bin\\Optimized_SteamEngine.exe";
//					if (File.Exists("bin\\fastdll.dll")) {
//						cp.CompilerOptions+="/resource:bin\\fastdll.lib /d:USEFASTDLL /unsafe ";
//					}
//#else
//					throw new SanityCheckException("None of these flags were set: DEBUG, SANE, or OPTIMIZED?");
//#endif
//#if MSVS
//					commondllname = "bin\\MSVS_Common.dll";
//					exeNameForConverter = "bin\\MSVS_SteamEngine.exe";
//#endif
//
//					cp.ReferencedAssemblies.Add(commondllname);
//					
//					if (WinConsoleForm.winForm.runConverter) {
//						cp.ReferencedAssemblies.Add(exeNameForConverter);
//					}
//					
//					cp.GenerateExecutable=true;
//					cp.OutputAssembly=exePath;
//
//					//					EnqueueMessage(cp.CompilerOptions);
//					CompilerResults results = compiler.CompileAssemblyFromFile(cp, fileNames); 
//					CompilerErrorCollection errors = results.Errors;
//					if (errors.HasErrors) {
//						EnqueueMessage("Compiling failed:"+Environment.NewLine);
//					} 
//
//					foreach (CompilerError err in errors) { 
//						LogStr msg;
//						if (!err.IsWarning) {
//							msg = LogStr.Error("Error: ")+LogStr.SetStyle(LogStyles.ErrorData);
//						} else {
//							msg = LogStr.Warning("Warning: ")+LogStr.SetStyle(LogStyles.WarningData);
//						}
//						if (err.FileName.Length!=0) {
//							msg += LogStr.FileLine(err.FileName,err.Line)+": "+err.ErrorText;
//						}
//						else {
//							msg += err.ErrorText;
//						}
//						EnqueueMessage(msg+Environment.NewLine);
//					}
//
//					if (errors.HasErrors) {
//						return false;
//					}
//
//					steamCore = results.CompiledAssembly;
//					EnqueueMessage("Assembly written to '"+results.PathToAssembly+"'"+Environment.NewLine);
//				}
//				if (steamCore == null) {
//					return false;
//				}
//			} catch (Exception e) {
//				EnqueueMessage("Exception when compiling core:"+Environment.NewLine+e);
//				return false;
//			}
//			EnqueueMessage("Done loading/compiling core."+Environment.NewLine);
//
//			return true;
		}

		//private class SourceNames {
		//    internal ArrayList fileNames = new ArrayList();
		//    internal DateTime newestFileDate;
		//}
		
		//private SourceNames GetSourceNames(string sourcePath, string extension) {
		//    SourceNames names = new SourceNames();
		//    DirectoryInfo dir = new DirectoryInfo(sourcePath);
		//    GetSourceNamesIn(names, dir, extension);
		//    return names;
		//}
		
		//private void GetSourceNamesIn(SourceNames names, DirectoryInfo dir, string extension) {
		//    FileSystemInfo[] fileSystemInfo = dir.GetFileSystemInfos();
		//    foreach ( FileSystemInfo entry in fileSystemInfo)  { 
		//        if ((entry.Attributes&FileAttributes.Directory)==FileAttributes.Directory) {
		//            GetSourceNamesIn(names, (DirectoryInfo)entry, extension);
		//        } else {
		//            if (entry.Extension==extension) {
		//                names.fileNames.Add(entry.FullName);
		//                if (names.newestFileDate<entry.LastWriteTime) {
		//                    names.newestFileDate=entry.LastWriteTime;
		//                }
		//            }
		//        }
		//    }
		//}

		private void EnterState(ConsoleStates state) {
			lock (typeof(ConsoleStates)) {
				if (state==currentState)
					return;
				previousState=currentState;
				currentState=state;
			}
			Notify(Notifications.StateChanged);
		}

		protected void Notify(Notifications not) {
			lock (notificationQueue.SyncRoot) {
				if (!notificationTable[(int)not]) {
					notificationQueue.Enqueue(not);
					notificationTable[(int)not]=true;
				}
			}
		}
		#endregion
	}
}