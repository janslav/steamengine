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
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using SteamEngine.Common;
using Microsoft.Win32;	//for RegistryKey

namespace SteamClients {
	public class WinConsoleForm : Form, IConsoleListener {
		#region Internal stuff
		internal enum WinConsoleStates {Basic,Remote,RemoteConnecting,RemoteConnected,Native,NativeInit,NativeRunning,NativePaused,
			NativeDestroyed};
		#endregion

		private readonly string[] defaultSearchDirs=new string[] {"scripts\\","saves\\"};
		private const bool defaultRecurseDirectories=true;
		private const string defaultEditor="";
		private const string defaultEditorFileParams="";
		private const string defaultEditorLineParams="";
		private const int firstConnectionNumber=0;
		private const string defaultConnectionName="New connection";
		private const string defaultConnectionHost="localhost";
		private const int defaultConnectionPort=2594;
		private const string defaultConnectionUserName="";
		private const string defaultConnectionPassword="";
		private const int defaultMaxCmdHistory=25;
		private const bool defaultSavePasswords=false;
		private const bool defaultSimpleCopy=false;
		private ComboBox editBox;
		private RichTextBox displayBox;
		private System.Windows.Forms.Timer timer;
		private MainMenu mainMenu;
		private MenuItem menuConsole;
		// Console subitems
		private MenuItem menuConnect;
		private MenuItem menuDisconnect;
		private MenuItem menuAbortConnection;
		private MenuItem menuSetStyles;
		private MenuItem menuClearOutput;
		private MenuItem menuExit;

		// Steamengine subitems
		private MenuItem menuSteam;
		private MenuItem menuResync;
		private MenuItem menuRecompile;
		private MenuItem menuGC;
		private ConAttrs conAttrs;
		private StyleForm styleForm;
		private ConnectionForm connForm;
		private FileViewForm viewForm;
		private StatusBar statusBar;
		private WinConsoleStates consoleState;
		private int maxCmdHistory;
		private bool savePasswords;
		private List<SteamConnection> connections;
		private LogStyles currentStyle;
		private string cmdEditor;
		private string cmdEditorFileParams;
		private string cmdEditorLineParams;
		private Point insanePoint;
		private IniHandler iniFile;
		private SteamConsole steamConsole;
		private ConsoleLinks links;
		private bool runMainLoop;
		private System.ComponentModel.IContainer components;
		private bool simpleCopy;

		internal bool runConverter = false;

		public static WinConsoleForm winForm;
		public static void Main(string[] args) {
			System.Threading.Thread.CurrentThread.CurrentCulture=CultureInfo.InvariantCulture;

			winForm=new WinConsoleForm();

			if (args.Length > 0) {
				if (args[0].Equals("C") || args[0].Equals("Convert") || args[0].Equals("Converter")) {
					winForm.runConverter = true;
				}
			}

			winForm.Show();
			winForm.Focus();
			winForm.MainLoop();
		}


		private void MainLoop() {
			if (runConverter) {
				steamConsole.StartNative("buildConverter", "converterFileName");
			} else {
				steamConsole.StartNative("buildCore", "coreFileName");
			}
			iniFile.IniDone();
			runMainLoop=true;
			timer.Enabled=true;
			while (runMainLoop) {
				System.Threading.Thread.Sleep(1);
				if (steamConsole.DispatchMessages()) {
					ScrollToBottom();
				}
				steamConsole.DispatchNotifications();
				Application.DoEvents();
			}
			timer.Enabled=false;
		}

		public WinConsoleForm() {
			winForm = this;
			SteamEngine.Common.Tools.ExitBinDirectory();
			links=new ConsoleLinks();
			connections = new List<SteamConnection>();
			conAttrs=new ConAttrs();
			styleForm=null;
			connForm=null;
			viewForm=null;
			insanePoint=new Point(0,0);
			iniFile=new IniHandler("gui.ini");

			steamConsole=new SteamConsole((IConsoleListener)this);

			InitializeComponent();
			try {
				Icon=new Icon("bin\\seicon.ico");
			} catch (Exception) { }	//just nevermind if it doesn't exist

			conAttrs.StyleChanged+=new ConAttrs.StyleChangedHandler(LogStyleChanged);
			conAttrs.TitleChanged+=new ConAttrs.TitleChangedHandler(TitleChanged);
			conAttrs.NextChunk+=new ConAttrs.NextChunkHandler(NextChunk);
			LoadConfiguration();
			consoleState=WinConsoleStates.Basic;
			currentStyle=LogStyles.Default;
		}


		#region Public methods
		public void ScrollToBottom() {
			int start,len;

			start=editBox.SelectionLength;
			len=editBox.SelectionStart;
			displayBox.Focus();
			displayBox.SelectionLength=0;
			displayBox.SelectionStart=displayBox.Text.Length;
			displayBox.ScrollToCaret();
			editBox.Focus();
			//editBox.SelectionLength=len;
			//editBox.SelectionStart=start;
		}

		public void ShowConnectionForm() {
			if (connForm==null) {
				connForm=new ConnectionForm(savePasswords);
				connForm.Closed+=new EventHandler(connFormClosed);
				connForm.Show();
				connForm.ImportConnections(connections);
				connForm.Focus();
				this.Enabled=false;
			} else {
				connForm.Focus();
			}
		}

		public void SetMessageStyles() {
			if (styleForm==null) {
				styleForm=new StyleForm(conAttrs);
				styleForm.Closed+=new EventHandler(styleFormClosed);
				styleForm.Show();
				styleForm.Focus();
				this.Enabled=false;
			} else {
				styleForm.Focus();
			}
		}

		public void DisplayString(string data) {
			try {
				Color color=displayBox.SelectionColor;
				Font font=displayBox.Font;
				conAttrs.SetString(data);
				conAttrs.Process();

				displayBox.SelectionColor=color;
				displayBox.SelectionFont=font;
			}
			catch (Exception) { }
		}
		
		public void Clear() {
			displayBox.Clear();
			links.RemoveAllLinks();
		}
		#endregion

		#region Configuration
		private void LoadConfiguration() {
			IniDataSection section=iniFile.IniSection("General");
			savePasswords=ConvertTools.ToBoolean(section.IniEntry("SavePasswords", defaultSavePasswords, ""));
			cmdEditor=section.IniEntry("Editor",defaultEditor,"").ToString();
			cmdEditorFileParams=section.IniEntry("EditorFileParams",defaultEditorFileParams,"").ToString();
			cmdEditorLineParams=section.IniEntry("EditorLineParams",defaultEditorLineParams,"").ToString();
			simpleCopy=ConvertTools.ToBoolean(section.IniEntry("SimpleCopy", defaultSimpleCopy, ""));
			runConverter= ConvertTools.ToBoolean(section.IniEntry("runConverter", false, "Run Converter instead of SteamEngine"));

			int i=0;
			string name="SearchDir"+i.ToString();
			if (!section.Contains(name)) {
				for (i=0;i<defaultSearchDirs.Length;i++) {
					name="SearchDir"+i.ToString();
					section.IniEntry(name,defaultSearchDirs[i].ToString(),"");
					links.AddSearchDir(defaultSearchDirs[i]);
				}
			} else {
				do {
					string dir=section.IniEntry(name,defaultSearchDirs[0],"").ToString();
					links.AddSearchDir(dir);
					i++;
					name="SearchDir"+i.ToString();
				} while (section.Contains(name));
			}
			links.recurseDirectories=ConvertTools.ToBoolean(section.IniEntry("RecurseDirectories",defaultRecurseDirectories,""));

			LoadStyles();
			LoadHistory();
			LoadConnections();
		}

		private void LoadStyles() {
			LogStyles style;
			object name,color;
			object size;
			object fnt;

			IniDataSection section=iniFile.IniSection("Styles");
			Array styles=Enum.GetValues(typeof(LogStyles));
			IEnumerator ie=styles.GetEnumerator();

			while (ie.MoveNext()) {
				if (ie.Current is LogStyles) {
					style=(LogStyles)ie.Current;
					color=section.IniEntry(style.ToString()+"Color",conAttrs.GetColor(style).Name,"");
					size=section.IniEntry(style.ToString()+"Size",conAttrs.GetSize(style),"");
					fnt=section.IniEntry(style.ToString()+"Style",((int)conAttrs.GetFontStyle(style)),"");
					name=section.IniEntry(style.ToString()+"Font",conAttrs.GetFontFamilyName(style),"");
					try {conAttrs.SetColor(style,Color.FromName(color.ToString()));} catch {}
					try {conAttrs.SetFontFamily(style,new FontFamily(name.ToString()));} catch {}
					try {conAttrs.SetSize(style,(float)size);} catch {}
					try {conAttrs.SetFontStyle(style,(FontStyle)fnt);} catch {}
				}
			}
		}

		private void LoadConnections() {
			int i=firstConnectionNumber;
			string name="Connection"+i.ToString();
			do {
				IniDataSection section=iniFile.IniSection(name);
				SteamConnection con=new SteamConnection();
				con.Name=section.IniEntry("Name",defaultConnectionName,"").ToString();
				con.Address=section.IniEntry("Host",defaultConnectionHost,"").ToString();
				con.Port=ConvertTools.ParseInt32(section.IniEntry("Port",defaultConnectionPort,"").ToString());
				con.UserName=section.IniEntry("UserName",defaultConnectionUserName,"").ToString();
				con.Password=section.IniEntry("Password",defaultConnectionPassword,"").ToString();
				connections.Add(con);
				i++;
				name="Connection"+i.ToString();
			} while (iniFile.ContainsGroup(name));
		}

		private void LoadHistory() {
			IniDataSection section=iniFile.IniSection("History");
			try {
				maxCmdHistory=int.Parse(section.IniEntry("MaxCmdHistory",defaultMaxCmdHistory,"how many items can be contained in history").ToString());
			} catch {}

			int i=0;
			string key="Item0";
			while (section.Contains(key)) {
				editBox.Items.Add(section.IniEntry(key,"",""));
				i++;
				key="Item"+i.ToString();
			}
		}

		private void SaveStyles() {
			Array styles=Enum.GetValues(typeof(LogStyles));
			IEnumerator ie=styles.GetEnumerator();

			while (ie.MoveNext()) {
				if (ie.Current is LogStyles) {
					LogStyles style=(LogStyles)ie.Current;
					iniFile.SetValue("Styles",style.ToString()+"Color",conAttrs.GetColor(style).Name);
					iniFile.SetValue("Styles",style.ToString()+"Size",conAttrs.GetSize(style));
					iniFile.SetValue("Styles",style.ToString()+"Style",((int)conAttrs.GetFontStyle(style)));
					iniFile.SetValue("Styles",style.ToString()+"Font",conAttrs.GetFontFamilyName(style));
				}
			}
		}

		private void SaveConnections() {
			int i=firstConnectionNumber;
			string name="Connection"+i.ToString();
			foreach (SteamConnection con in connections) {
				if (!iniFile.ContainsGroup(name)) {
					IniDataSection section=iniFile.IniSection(name);
					section.SetValue("Name",con.Name);
					section.SetValue("Host",con.Address);
					section.SetValue("Port",con.Port.ToString());
					section.SetValue("UserName",con.UserName);
					if (savePasswords) {
						section.SetValue("Password",con.Password);
					} else {
						section.SetValue("Password","");
					}
				} else {
					iniFile.SetValue(name,"Name",con.Name);
					iniFile.SetValue(name,"Host",con.Address);
					iniFile.SetValue(name,"Port",con.Port.ToString());
					iniFile.SetValue(name,"UserName",con.UserName);
					if (savePasswords) {
						iniFile.SetValue(name,"Password",con.Password);
					} else {
						iniFile.SetValue(name,"Password","");
					}
				}

				i++;
				name="Connection"+i.ToString();
			}
			while (iniFile.ContainsGroup(name)) {
				iniFile.RemoveGroup(name);
				i++;
				name="Connection"+i.ToString();
			}
		}

		private void SaveHistory() {
			int i;

			for (i=0;i<editBox.Items.Count;i++) {
				string key="item"+i.ToString();
				iniFile.SetValue("history",key,editBox.Items[i]);
			}
		}

		private void SaveConfiguration() {
			iniFile.SetValue("General","SavePasswords",savePasswords);
		}
		#endregion

		void InitializeComponent() {
			this.components = new System.ComponentModel.Container();
			this.editBox = new System.Windows.Forms.ComboBox();
			this.displayBox = new System.Windows.Forms.RichTextBox();
			this.mainMenu = new System.Windows.Forms.MainMenu();
			this.menuConsole = new System.Windows.Forms.MenuItem();
			this.menuSetStyles = new System.Windows.Forms.MenuItem();
			this.menuClearOutput = new System.Windows.Forms.MenuItem();
			this.menuExit = new System.Windows.Forms.MenuItem();
			this.menuSteam = new System.Windows.Forms.MenuItem();
			this.menuResync = new System.Windows.Forms.MenuItem();
			this.menuRecompile = new System.Windows.Forms.MenuItem();
			this.menuGC = new System.Windows.Forms.MenuItem();
			this.menuConnect = new System.Windows.Forms.MenuItem();
			this.menuDisconnect = new System.Windows.Forms.MenuItem();
			this.menuAbortConnection = new System.Windows.Forms.MenuItem();
			this.statusBar = new System.Windows.Forms.StatusBar();
			this.timer = new System.Windows.Forms.Timer(this.components);
			this.SuspendLayout();
			// 
			// editBox
			// 
			this.editBox.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.editBox.Enabled = false;
			this.editBox.Location = new System.Drawing.Point(0, 390);
			this.editBox.Name = "editBox";
			this.editBox.Size = new System.Drawing.Size(680, 21);
			this.editBox.TabIndex = 1;
			this.editBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.editBoxKeyPress);
			// 
			// displayBox
			// 
			this.displayBox.BackColor = System.Drawing.Color.White;
			this.displayBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.displayBox.Location = new System.Drawing.Point(0, 0);
			this.displayBox.Name = "displayBox";
			this.displayBox.ReadOnly = true;
			this.displayBox.Size = new System.Drawing.Size(680, 390);
			this.displayBox.TabIndex = 2;
			this.displayBox.Text = "";
			this.displayBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.displayBoxMouseMove);
			this.displayBox.MouseUp += new System.Windows.Forms.MouseEventHandler(this.displayBoxMouseUp);
			this.displayBox.LinkClicked += new LinkClickedEventHandler(this.displayBoxLinkClicked);
			// 
			// mainMenu
			// 
			this.mainMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																											this.menuConsole,
																											this.menuSteam});
			// 
			// menuConsole
			// 
			this.menuConsole.Index = 0;
			this.menuConsole.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																												this.menuSetStyles,
																												this.menuClearOutput,
																												this.menuExit});
			this.menuConsole.Text = "&Console";
			// 
			// menuSetStyles
			// 
			this.menuSetStyles.Index = 0;
			this.menuSetStyles.Text = "&Set message styles";
			this.menuSetStyles.Click += new System.EventHandler(this.menuSetStylesClick);
			// 
			// menuClearOutput
			// 
			this.menuClearOutput.Index = 1;
			this.menuClearOutput.Text = "&Clear console";
			this.menuClearOutput.Click += new System.EventHandler(this.menuClearOutputClick);
			// 
			// menuExit
			// 
			this.menuExit.Index = 2;
			this.menuExit.Text = "&Exit";
			this.menuExit.Click += new System.EventHandler(this.menuExitClick);
			// 
			// menuSteam
			// 
			this.menuSteam.Index = 1;
			this.menuSteam.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
							this.menuResync,
							this.menuRecompile,
							this.menuGC});
			this.menuSteam.Text = "&Steamengine";
			// 
			// menuResync
			// 
			this.menuResync.Index = 0;
			this.menuResync.Text = "&Resync";
			this.menuResync.Click += new System.EventHandler(this.menuResyncClick);
			// 
			// menuRecompile
			// 
			this.menuRecompile.Index = 1;
			this.menuRecompile.Text = "R&ecompile";
			this.menuRecompile.Click += new System.EventHandler(this.menuRecompileClick);
			// 
			// menuGC
			// 
			this.menuGC.Index = 2;
			this.menuGC.Text = "Collect garbage";
			this.menuGC.Click += new System.EventHandler(this.menuGCClick);
			// 
			// menuConnect
			// 
			this.menuConnect.Index = -1;
			this.menuConnect.Text = "&Connect";
			this.menuConnect.Visible = false;
			this.menuConnect.Click += new System.EventHandler(this.menuConnectClick);
			// 
			// menuDisconnect
			// 
			this.menuDisconnect.Index = -1;
			this.menuDisconnect.Text = "Disconnect";
			this.menuDisconnect.Visible = false;
			this.menuDisconnect.Click += new System.EventHandler(this.menuDisconnectClick);
			// 
			// menuAbortConnection
			// 
			this.menuAbortConnection.Index = -1;
			this.menuAbortConnection.Text = "Abort connection";
			this.menuAbortConnection.Click += new System.EventHandler(this.menuDisconnectClick);
			// 
			// statusBar
			// 
			this.statusBar.Location = new System.Drawing.Point(0, 411);
			this.statusBar.Name = "statusBar";
			this.statusBar.Size = new System.Drawing.Size(680, 22);
			this.statusBar.TabIndex = 0;
			this.statusBar.Text = "Steamengine console";
			// 
			// timer
			// 
			this.timer.Interval = 250;
			this.timer.Tick += new System.EventHandler(this.timerTick);
			// 
			// WinConsoleForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(680, 433);
			this.Controls.Add(this.displayBox);
			this.Controls.Add(this.editBox);
			this.Controls.Add(this.statusBar);
			this.Menu = this.mainMenu;
			this.Name = "WinConsoleForm";
			this.Text = "SteamEngine console";
			this.Closed += new System.EventHandler(this.consoleFormClosed);
			this.Activated += new System.EventHandler(this.consoleFormActivate);
			this.ResumeLayout(false);

		}
		
		#region Links
		private FileLink GetLinkFromPosition(int x,int y) {
			if (links.Count<=0)
				return null;
			insanePoint.X=x;
			insanePoint.Y=y;
			int pos=displayBox.GetCharIndexFromPosition(insanePoint);
			return links.GetLinkFromPosition(pos);
		}

		private void ShowFile(string file,int line) {
			if (viewForm==null) {
				viewForm=new FileViewForm();
				viewForm.Closed+=new EventHandler(viewFormClosed);
				viewForm.Show();
				viewForm.Focus();
				viewForm.ShowFile(file,line);
				this.Enabled=false;
			} else {
				viewForm.Focus();
			}
		}

		private string TranslateParams(string param,string file,int line) {
			string result=param.Replace("{file}",file);
			result=result.Replace("{line}",line.ToString());

			return result;
		}
		private void LaunchLink(string file) {
			LaunchLink(file,0);
		}

		private void LaunchLink(string file,int line) {
			if (!links.ResolveLink(ref file)) {
				MessageBox.Show(this,"Cannot find "+file,"Link");
				return;
			}
			
			try {
				if (cmdEditor.Length!=0 && cmdEditorLineParams.Length!=0 && line>0) {
					Process.Start(cmdEditor,TranslateParams(cmdEditorLineParams,file,line));
				} else {
					if (cmdEditor.Length !=0 && cmdEditorFileParams.Length!=0) {
						Process.Start(cmdEditor,TranslateParams(cmdEditorFileParams,file,line));
					} else {
						ShowFile(file,line);
					}
				}
			} catch (Exception e) {
				MessageBox.Show(this,e.Message,"Editor cannot be started");
			}
		}

		private void ActivateLink(FileLink link) {
			string link_text=displayBox.Text.Substring(link.Start,link.Length);
			switch (link.Type) {
				case FileLinkType.FileOnly :
					LaunchLink(link_text);
					break;
				case FileLinkType.FileLine :
					int line;
					string file;
					LogStr.ParseFileLine(link_text,out file, out line);
					LaunchLink(file,line);
					break;
			}
		}
		#endregion

		#region State switching
		public void SwitchToBasic() {
			if (consoleState==WinConsoleStates.Basic || consoleState==WinConsoleStates.NativeDestroyed)
				return; 

			StripRemoteFeatures();
			StripNativeFeatures();

			menuResync.Enabled=false;
			menuRecompile.Enabled=false;
			menuGC.Enabled=false;

			this.Text="SteamEngine console";
			statusBar.Text="SteamEngine console";
			editBox.Enabled=false;
         
			consoleState=WinConsoleStates.Basic;
		}

		public void SwitchToRemote() {
			if (consoleState==WinConsoleStates.Remote || consoleState==WinConsoleStates.NativeDestroyed)
				return; 

			if (!IsRemoteState(consoleState)) {
				StripNativeFeatures();
				AddRemoteFeatures();
			}

			menuConnect.Visible=true;
			menuDisconnect.Visible=false;
			menuAbortConnection.Visible=false;

			menuResync.Enabled=false;
			menuRecompile.Enabled=false;
			menuGC.Enabled=false;

			this.Text="SteamEngine remote console";
			statusBar.Text="Not connected";
			editBox.Enabled=false;

			consoleState=WinConsoleStates.Remote;
		}

		public void SwitchToRemoteConnecting() {
			if (consoleState==WinConsoleStates.RemoteConnecting || consoleState==WinConsoleStates.NativeDestroyed)
				return; 

			if (!IsRemoteState(consoleState)) {
				StripNativeFeatures();
				AddRemoteFeatures();
			}

			menuConnect.Visible=false;
			menuDisconnect.Visible=false;
			menuAbortConnection.Visible=true;

			menuResync.Enabled=false;
			menuRecompile.Enabled=false;
			menuGC.Enabled=false;

			this.Text="SteamEngine remote console";
			statusBar.Text="connecting...";
			editBox.Enabled=false;

			consoleState=WinConsoleStates.RemoteConnecting;
		}

		public void SwitchToRemoteConnected() {
			if (consoleState==WinConsoleStates.RemoteConnected || consoleState==WinConsoleStates.NativeDestroyed)
				return; 

			if (!IsRemoteState(consoleState)) {
				StripNativeFeatures();
				AddRemoteFeatures();
			}

			menuConnect.Visible=false;
			menuDisconnect.Visible=true;
			menuAbortConnection.Visible=false;

			menuResync.Enabled=true;
			menuRecompile.Enabled=true;
			menuGC.Enabled=true;

			this.Text="SteamEngine remote console";
			statusBar.Text="connected";
			editBox.Enabled=true;
			if (displayBox.Focused)
				editBox.Focus();

			consoleState=WinConsoleStates.RemoteConnected;
		}

		public void SwitchToNative() {
			if (consoleState==WinConsoleStates.Native || consoleState==WinConsoleStates.NativeDestroyed)
				return; 

			if (!IsNativeState(consoleState)) {
				StripRemoteFeatures();
				AddNativeFeatures();
			}

			this.Text="SteamEngine native console";
			statusBar.Text="SteamEngine is not running";
			editBox.Enabled=false;
			menuResync.Enabled=false;
			menuRecompile.Enabled=false;
			menuGC.Enabled=false;

			consoleState=WinConsoleStates.Native;
		}

		public void SwitchToNativeStartup() {
			if (consoleState==WinConsoleStates.NativeInit || consoleState==WinConsoleStates.NativeDestroyed)
				return; 

			if (!IsNativeState(consoleState)) {
				StripRemoteFeatures();
				AddNativeFeatures();
			}

			this.Text="SteamEngine native console";
			statusBar.Text="SteamEngine is starting";
			editBox.Enabled=false;
			menuResync.Enabled=false;
			menuRecompile.Enabled=false;
			menuGC.Enabled=false;

			consoleState=WinConsoleStates.NativeInit;
		}

		public void SwitchToNativeRunning() {
			if (consoleState==WinConsoleStates.NativeRunning || consoleState==WinConsoleStates.NativeDestroyed)
				return; 

			if (!IsNativeState(consoleState)) {
				StripRemoteFeatures();
				AddNativeFeatures();
			}

			this.Text="SteamEngine native console";
			statusBar.Text="SteamEngine is running";
			editBox.Enabled=true;
			if (displayBox.Focused)
				editBox.Focus();
			menuResync.Enabled=true;
			menuRecompile.Enabled=true;
			menuGC.Enabled=true;

			consoleState=WinConsoleStates.NativeRunning;
		}

		public void SwitchToNativeDestroyed() {
			if (consoleState==WinConsoleStates.NativeDestroyed)
				return; 

			if (!IsNativeState(consoleState)) {
				StripRemoteFeatures();
				AddNativeFeatures();
			}

			editBox.Enabled=false;
			menuResync.Enabled=false;
			menuRecompile.Enabled=false;
			menuGC.Enabled=false;
			this.Text="SteamEngine native console";
			statusBar.Text="SteamEngine is not running, close this dialog to exit.";

			consoleState=WinConsoleStates.NativeDestroyed;
		}
		
		public void SwitchToNativePaused() {
			if (consoleState==WinConsoleStates.NativePaused || consoleState==WinConsoleStates.NativeDestroyed)
				return; 

			if (!IsNativeState(consoleState)) {
				StripRemoteFeatures();
				AddNativeFeatures();
			}

			this.Text="SteamEngine native console";
			statusBar.Text="SteamEngine is paused";
			editBox.Enabled=false;
			menuResync.Enabled=true;
			menuRecompile.Enabled=true;
			menuGC.Enabled=false;

			consoleState=WinConsoleStates.NativePaused;
		}

		private void AddRemoteFeatures() {
			this.SuspendLayout();
			menuConsole.MenuItems.Add(0,menuConnect);
			menuConsole.MenuItems.Add(0,menuDisconnect);
			menuConsole.MenuItems.Add(0,menuAbortConnection);
			this.ResumeLayout();
		}

		private void StripRemoteFeatures() {
			this.SuspendLayout();
			if (menuConsole.MenuItems.Contains(menuConnect)) {
				menuConsole.MenuItems.Remove(menuConnect);
			}
			this.ResumeLayout();
		}

		private void AddNativeFeatures() {
			// No native features
		}

		private void StripNativeFeatures() {
			// No native features
		}

		private bool IsRemoteState(WinConsoleStates state) {
			return (state==WinConsoleStates.Remote || state==WinConsoleStates.RemoteConnected || state==WinConsoleStates.RemoteConnecting);
		}

		private bool IsNativeState(WinConsoleStates state) {
			return (state==WinConsoleStates.Native || state==WinConsoleStates.NativeInit ||
				state==WinConsoleStates.NativePaused || state==WinConsoleStates.NativeRunning);
		}
		#endregion

		#region Events
		private void consoleFormClosed(object sender,EventArgs e) {
			runMainLoop=false;
			SaveHistory();
			SaveConfiguration();
			steamConsole.Exit();
			iniFile.WriteFile();
		}

		private void viewFormClosed(object sender,System.EventArgs e) {
			FileViewForm form=sender as FileViewForm;
			if (form!=null && form==viewForm) {
				this.Enabled=true;
				this.Focus();
				viewForm=null;
			}
		}

		private void connFormClosed(object sender,System.EventArgs e) {
			ConnectionForm form=sender as ConnectionForm;
			if (form!=null && form==connForm) {
				form.ExportConnections(ref connections);
				savePasswords=form.SavePasswords;
				SaveConnections();
				if (form.ShouldConnect) {
					steamConsole.StartRemote(form.SelectedConnection);
				}
				this.Enabled=true;
				this.Focus();
				connForm=null;
			}
		}

		private void styleFormClosed(object sender,System.EventArgs e) {
			StyleForm form=sender as StyleForm;
			if (form!=null && form==styleForm) {
				if (form.UpdateAttributes) {
					conAttrs=form.ConsoleAttributes;
					SaveStyles();
				}
				this.Enabled=true;
				styleForm=null;
			}
		}
		
		void editBoxKeyPress(object sender, KeyPressEventArgs e) {
			if (e.KeyChar=='\r') {
				string cmd=editBox.Text;
				int indexOf=editBox.Items.IndexOf(cmd);
				if (indexOf>-1)
					editBox.Items.RemoveAt(indexOf);
				if (editBox.Items.Count>maxCmdHistory)
					editBox.Items.RemoveAt(editBox.Items.Count-1);
				editBox.Items.Insert(0,cmd);
				
				if (cmd.Trim()!="") {
					steamConsole.Send(cmd);
					editBox.Text="";
				}
				e.Handled=true;
			} else if (e.KeyChar==27) {
				editBox.Text="";
				e.Handled=true;
			}
		}
		
		private string GetDefaultBrowserPath() {
			string defaultBrowserPath="iexplore.exe";
			try {
				/*
				Instead of repeatedly checking for OpenSubKey returning a null, we will assume it is not null and
				simply chain a bunch of calls. If it DOES return null, then it'll get caught by our exception handler -
				it shouldn't normally return null, although it probably will on computers that aren't running
				Win2k SP3, or WinXP, or newer OSes. This would be easy enough to test, for someone who is running
				an older OS.
				
				HKEY_LOCAL_MACHINE/Software/Clients/StartMenuInternet/(default) holds the name of the default browser.
				
				Each browser has a subkey (like a subfolder) under StartMenuInternet. The name of the default browser
				is really the name of its registry sub-key.
				Each browser's subkey contains some other subkeys, and the one we're interested in is
				shell/open/command/(default), which holds the path & name of the browser's EXE (or whatever
				you would call to start the browser). That's what we return from this method.
				
				-SL
				*/
				RegistryKey rk = Registry.LocalMachine;
								
				rk=rk.OpenSubKey("SOFTWARE");
				rk=rk.OpenSubKey("Clients");
				rk=rk.OpenSubKey("StartMenuInternet");
				string defaultbrowser = (string) rk.GetValue(null);	//null means to get the default value for this key
				rk=rk.OpenSubKey(defaultbrowser);
				rk=rk.OpenSubKey("shell");
				rk=rk.OpenSubKey("open");
				rk=rk.OpenSubKey("command");
				defaultBrowserPath=(string) rk.GetValue(null);
			#if DEBUG
			} catch (Exception e) {
				WriteLine("Unable to determine default web browser; Assuming Internet Explorer.");
				WriteLine("[Don't panic] The caught exception was: "+e);
			#else
			} catch (Exception) {	//to suppress a warning about 'e' not being used
				WriteLine("Unable to determine default web browser; Assuming Internet Explorer.");
			#endif
			}
			return defaultBrowserPath;
		}
		
		void displayBoxLinkClicked(object sender, LinkClickedEventArgs e) {
			//the text of the link is in e
			
			Process p = new Process();
			ProcessStartInfo pStartInfo = new ProcessStartInfo(GetDefaultBrowserPath(), e.LinkText);
			p.StartInfo=pStartInfo;
			p.Start();
			
			/*
			The .NET documentation suggested doing 'Process.Start(e.LinkText)', which didn't actually work
			(It threw some exception whose error message made no sense whatsoever to me). But,
			of course, OTHER Microsoft documentation (in the platform SDK) stated quite clearly that ShellExecute,
			which is what Process uses normally to run things, does NOT treat URLs as documents, and thus will NOT
			run them or open them if they are passed directly to ShellExecute (which is what Process.Start()
			basically does).
			
			-SL
			*/
		}
		
		bool displayBoxCopy() {
			if(displayBox.SelectionLength > 0) {
				displayBox.Copy();
				displayBox.Select(displayBox.SelectionStart,0);
				return true;
			}
			return false;
		}

		void displayBoxMouseUp(object sender, MouseEventArgs e) {
			bool textCopied=false;
			if (simpleCopy) {
				textCopied=displayBoxCopy();
			}
			if (!textCopied) {
				FileLink link=GetLinkFromPosition(e.X,e.Y);
				if (link!=null) {
					ActivateLink(link);
				}
			}
		}
		
		void consoleFormActivate(object sender, EventArgs e) {
			if (connForm!=null) {
				connForm.Focus();
			} else {
				if (styleForm!=null) {
					styleForm.Focus();
				} else {
					if (viewForm!=null) {
						viewForm.Focus();
					} else {
						editBox.Focus();
					}
				}
			}
		}

		private void LogStyleChanged(object sender,LogStyles style) {
			displayBox.SelectionFont=new Font(conAttrs.GetFontFamily(style),conAttrs.GetSize(style),conAttrs.GetFontStyle(style));
			displayBox.SelectionColor=conAttrs.GetColor(style);
			currentStyle=style;
		}

		private void TitleChanged(object sender,string title) {
			if ((title==null) || (title.Length==0)) {
				//reset to basic console title
				if (IsNativeState(consoleState)) {
					this.Text="SteamEngine native console";
				} else if (IsRemoteState(consoleState)) {
					this.Text="SteamEngine remote console";
				} else {
					this.Text="SteamEngine console";
				}
			} else {
				this.Text=title;
			}
		}

		private void NextChunk(object sender,string chunk) {
//			if (chunk.Length < 3) {
//				return;
//			}
			try {
				while(displayBox.TextLength+chunk.Length>displayBox.MaxLength) {
					displayBox.Text=displayBox.Text.Substring(displayBox.Text.IndexOf(Environment.NewLine)+Environment.NewLine.Length);
				}
				switch (currentStyle) {
					case LogStyles.File :
						links.AddLink(displayBox.Text.Length,chunk.Length,FileLinkType.FileOnly);
						break;
					case LogStyles.FileLine :
						links.AddLink(displayBox.Text.Length,chunk.Length,FileLinkType.FileLine);
						break;
				}
				displayBox.AppendText(chunk);
			} 
			catch(Exception) { 
				//sometimes one is thrown while shutting down
			}
		}
		
		void Exit(object sender, KeyPressEventArgs e) {
			DestroyHandle();
		}

		private void menuSetStylesClick(object sender,System.EventArgs e) {
			SetMessageStyles();
		}
		private void menuClearOutputClick(object sender,System.EventArgs e) {
			Clear();
		}
		private void menuExitClick(object sender,System.EventArgs e) {
			Close();
		}
		private void menuResyncClick(object sender,System.EventArgs e) {
			steamConsole.Send("resync");
		}
		private void menuRecompileClick(object sender,System.EventArgs e) {
			steamConsole.Send("recompile");
		}
		private void menuGCClick(object sender,System.EventArgs e) {
			steamConsole.Send("g");
		}
		private void menuNativeAbortClick(object sender,System.EventArgs e) {
			steamConsole.Send("exit");
		}
		private void menuShutdownClick(object sender,System.EventArgs e) {
			steamConsole.Abort();
		}
		private void menuConnectClick(object sender,System.EventArgs e) {
			ShowConnectionForm();
		}
		private void menuDisconnectClick(object sender,System.EventArgs e) {
			steamConsole.Logout();
		}

		private void displayBoxMouseMove(object sender,System.Windows.Forms.MouseEventArgs e) {
			if (GetLinkFromPosition(e.X,e.Y)!=null) {
				displayBox.Cursor=Cursors.Hand;
			} else {
				displayBox.Cursor=Cursors.IBeam;
			}
		}

		private void timerTick(object sender,EventArgs e) {
			switch(steamConsole.CurrentState) {
				case ConsoleStates.NativeConnected :
				switch (steamConsole.RunLevel) {
					case RunLevels.Running:
					case RunLevels.AwaitingRetry:
						SwitchToNativeRunning();
						break;
					case RunLevels.Startup:
						SwitchToNativeStartup();
						break;
					case RunLevels.Paused:
						SwitchToNativePaused();
						break;
					case RunLevels.Dead:
						SwitchToNativeDestroyed();
						break;
					default:
						break;
				}
					break;
			}
		}
		#endregion

		#region IConsoleListener
		public void NativeFailed() {
		}

		public void RemoteFailed() {
		}

		public void StateChanged() {
			switch (steamConsole.CurrentState) {
				case ConsoleStates.Remote :
					SwitchToRemote();
					break;
				case ConsoleStates.RemoteConnecting :
					SwitchToRemoteConnecting();
					break;
				case ConsoleStates.RemoteConnected :
					SwitchToRemoteConnected();
					break;
				case ConsoleStates.Native :
					SwitchToNative();
					break;
				case ConsoleStates.NativeConnected :
					SwitchToNativeStartup();
					break;
				case ConsoleStates.NativeDestroyed :
					SwitchToNativeDestroyed();
					break;
			}
		}

		public void WriteLine(string data) {
			Write(data.Trim()+Environment.NewLine);
		}

		public void Write(string data) {
			int start=0,pos;

			pos=data.IndexOf(Environment.NewLine,start);
			while(pos>0) {
				pos+=Environment.NewLine.Length;
				DisplayString(data.Substring(start,pos-start));
				start=pos;
				pos=data.IndexOf(Environment.NewLine,start);
			}
			if (start<data.Length) {
				DisplayString(data.Substring(start,data.Length-start));
			}
		}

		public void WriteLine(object data) {
			if (data is LogStr) {
				WriteLine((data as LogStr).NiceString);
			} else {
				WriteLine(data.ToString());
			}
		}

		public void Write(object data) {
			if (data is LogStr) {
				Write((data as LogStr).NiceString);
			} else {
				Write(data.ToString());
			}
		}

		public void WriteLine(LogStr data) {
			WriteLine(data.NiceString);
		}

		public void Write(LogStr data) {
			Write(data.NiceString);
		}
		#endregion
	}
}
