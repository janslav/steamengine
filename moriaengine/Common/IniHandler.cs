/*
Copyright (c) 2003-2004, Richard Dillingham
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
    * Neither the name of the copyright holder nor the names of contributors may be used to endorse or promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDER(S) AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

(For the curious, this is a BSD-style license)
*/

namespace SteamEngine.Common {
	using System;
	using System.Net;
	using System.Net.Sockets;
	using System.Text;
	using System.IO;
	using System.Collections;
	using System.Reflection;
	using System.Globalization;
	
	/*
		Class: IniHandler
		
		This is an interface to INI files. I originally wrote it for SteamEngine, and then modified it for Planets
		(All the code in Planets' version was written by me (Richard Dillingham)), and then sent the modified and
		commented source to Tartaros @ SteamEngine so he could use the improvements for SE if he wished.
		
		If an IniHandler instance is directly created, then it will use a default INI name (planets.ini for Planets)
		If a *subclass* of IniHandler is created, then the INI file name is scripts/config/ClassName.ini.
		Planets only uses planets.ini, at the moment. The subclass stuff is from the SE code.
		
		This requires the TagMath class for converting things to and from strings, and it has to be modified to add new
		class-types that're valid for saving. Specifically, ConvertTo has to be modified. For planets, to add support
		for the Key class, all that was added to ConvertTo was:
			*****************************************************************
			*	} else if (type.Equals(typeof(Key)) && obj is String) {		*
			*		return new Key((string)obj);							*
			*****************************************************************
		which came after this:
			*****************************************************************
			*	if (type.Equals(typeof(String))) {							*
			*		return obj.ToString();									*
			*****************************************************************
		(Additionally, Key has a ToString() method, which is also required.)
		
		Example usage of IniHandler follows. A few comments about the example code follow it.
		-------------------------------------------------------------------------------------
		
			public class Prefs {	
				
				static private bool _pref_pause_on_alt_tab;
				static internal bool pref_pause_on_alt_tab {
					get {
						return _pref_pause_on_alt_tab;
					} set {
						_pref_pause_on_alt_tab=value;
						ini.SetValue("main","Pause when not active",value);
					}
				}	
				static IniHandler ini;
				
				static internal void LoadIni() {
					ini = new IniHandler();
					IniDataSection main = ini.IniSection("main");
					_pref_pause_on_alt_tab=(bool) main.IniEntry("Pause when not active", true, "Whether or not to pause the game when the user alt-tabs or the game window otherwise loses focus.");
					
					ini.IniDone();
				}
				
				static internal void SaveIni() {
					ini.WriteFile();
				}
			}
		-------------------------------------------------------------------------------------
		
		The menus in Planets which change INI options attempt to set the property in question,
		which sets the value in the INI. SaveIni is called to save the INI file, and LoadINI to load it. 
		The 'cancel' button in Planets' options menu calls LoadIni to restore the options as they were last saved.
		That is perfectly valid to do (Since the old IniHandler instance is simply dropped, and cleaned
		up later by the .NET garbage collector). The 'okay' button calls SaveIni.
		

	*/
	public class IniHandler {
		protected string iniPath = null;
		private ArrayList iniSections = null;
		private bool fileExists = false;
		private bool fileCheckDone = false;
		private Hashtable sections = null;
		public bool Exists { get { return fileExists;} }
		private bool iniDone = false;
		
		//Constructor
		public IniHandler() {}
		
		//This can be used to force usage of a specific filename. It will not create folders if they're in the filename.
		public IniHandler(string filename) {
			iniPath=filename;
		}
		
		//Creates a new IniDataSection of the specific name (Case insensitive).
		public IniDataSection IniSection(string name) {
			if (iniSections==null) iniSections=new ArrayList();
			IniDataSection iniS=new IniDataSection(name, this);
			iniSections.Add(iniS);
			return iniS;
		}
		
		//Adds a comment to the file.
		public void Comment(string comment) {
			if (iniSections==null) iniSections=new ArrayList();
			IniComment ici = new IniComment(comment);
			iniSections.Add(ici);
		}
		
		//States that we're done specifying entries for the INI, but that it should remain readable.
		public void IniRemainReadable() {
			IniDone(false);
		}
		
		//This is identical to IniRemainReadable, actually. It used to call IniDone(true) but now calls
		//IniDone(false), just like IniRemainReadable.
		public void IniDone() {
			IniDone(false);
		}
		
		//States that we're done specifying entries for the INI. If 'close' is true, then the IniHandler will
		//expect NOT to be used any more, and will forget what it has loaded.
		public void IniDone(bool close) {
			if (iniPath==null) {
				Console.WriteLine("Error: IniDone called before instantiating! Aborting IniDone. Uh. This really should NOT happen.");
				return;
			}
			if (iniSections.Count==0) {
				Console.WriteLine("Error: No sections specified prior to call to IniDone.");
				return;
			}
			if (!fileCheckDone) {
				Console.WriteLine("Error: No entries specified prior to call to IniDone");
				return;
			}
			iniDone=true;
			if (!fileExists) {
				//write out the file, since it doesn't exist.
				WriteFile();
				if (File.Exists(iniPath)) {
					fileExists=true;
				}
				ReadFile();
			}
			if (close) {
				iniSections=null;	//we're done with it
				sections=null;		//done with that too, if it was used
			}
		}
		
		/*
		Important Note: Call this method to write out the INI file. That's right, WriteFile is intended for
		classes other than IniHandler, although ReadFile is not. I would rename them, but their names are just
		so darn accurate for what they do! :O
		
		This writes out the INI file. This will obviously overwrite the file if it already exists.
		Note that this writes the value that each entry has, which may or may not be its default value.
		This WILL erase anything that's been entered into the INI file but isn't noted in the data structures
		that IniHandler uses (IniDataSection, IniComment, IniDataEntry, etc).
		
		This is called by Planets after New Game is clicked, and also after the user hits the OK button
		on the options menu, or on the key configuration menu.
		*/
		public void WriteFile() {
			if (!iniDone) {
				throw new Exception("WriteFile called before IniDone or IniRemainReadable.");
			}
			Console.WriteLine("Writing "+iniPath+".");
			try {
				StreamWriter sw = File.CreateText(iniPath);

				for (int a=0; a<iniSections.Count; a++) {
					if (iniSections[a] is IniDataSection) {
						IniDataSection section = (IniDataSection) iniSections[a];
						section.Write(sw);
					} else if (iniSections[a] is IniComment) {
						IniComment comment = (IniComment) iniSections[a];
						comment.Write(sw);
					}
				}
				sw.Close();
			} catch(Exception e) {
				Console.WriteLine("Error: Exception raised while writing "+iniPath+".");
				Console.WriteLine(e);
			}
		}
		
		//This returns the value for a specific entry in a specific group. Case insensitive.
		//Entry/key names can have spaces in them.
		public object GetValue(string group, string key) {
			return GetValue(group, key, null);
		}

		public void RemoveGroup(string group) {
			group=group.ToLower();
			if (iniSections!=null) {
				int i;
				for (i=0;i<iniSections.Count;i++) {
					if (group==((IniDataSection)iniSections[i]).Name.ToLower()) {
						iniSections.RemoveAt(i);
						break;
					}
				}
			}
			if (sections!=null && sections.ContainsKey(group)) {
				sections.Remove(group);
			}
		}

		public bool ContainsGroup(string group) {
			group=group.ToLower();
			if (iniSections!=null) {
				foreach (IniDataSection section in iniSections) {
					if (group==section.Name) {
						return true;
					}
				}
			}
			if (sections!=null) {
				if (sections.ContainsKey(group)) {
					return true;
				}
			}
			return false;
		}

		public bool Contains(string group, string key) {
			if (sections==null)
				return false;
			if (sections.ContainsKey(group)) {
				IniTempGroup itg=sections[group] as IniTempGroup;
				if (itg!=null) {
					return itg.Contains(key);
				}
			}
			return false;
		}
		
		//This sets the value for a specific entry in a specific group (group.key=value).
		//Group and key are case insensitive.
		//Entry/key names can have spaces in them.
		public void SetValue(string group, string key, object value) {
			if (!iniDone) {
				throw new Exception("WriteFile called before IniDone or IniRemainReadable.");
			}
			if (fileExists) {
				//check sections
				IniTempGroup itg=(IniTempGroup) sections[group.ToLower()];
				if (itg==null) {
					itg=(IniTempGroup) sections["default"];
				}
				if (itg!=null) {
					itg.SetNoDupe(key.ToLower(),value.ToString());
//					return;
				}
				foreach (object o in iniSections) {
					if (o is IniDataSection) {
						IniDataSection ids=(IniDataSection) o;
						if (ids.Name.ToLower()==group.ToLower()) {
							ids.SetValue(key, value);
//							return;
						}
					}
				}
			}
//			Console.WriteLine("Did not find "+group+"."+key+"!");
		}
	
		//This returns the value for group.key, and if it is not set (or the element is not found), returns defaultVal.
		//This checks the 'default' section if it doesn't find the specified group. The 'default' section is where
		//items go which were specified before the first group specifier. I.E. If 'foo=bar' is at the top of the INI,
		//before any '[group]', foo would be be recorded as being in the 'default' group.
		//Entry/key names can have spaces in them.
		public object GetValue(string group, string key, object defaultVal) {
			if (!fileCheckDone) {
				fileCheckDone=true;
				if (File.Exists(iniPath)) {
					fileExists=true;
					//read file
					ReadFile();
				} else {
					fileExists=false;
					//do nothing for now, but write it later
				}
			}
			if (fileExists) {
				//check sections
				IniTempGroup itg=(IniTempGroup) sections[group.ToLower()];
				if (itg==null) {
					itg=(IniTempGroup) sections["default"];
				}
				if (itg!=null) {
					object val=itg.Get(key.ToLower());
					if (val!=null) {
						if (defaultVal==null) return val;
						try {
							object o=ConvertTo(defaultVal.GetType(), val);
							return o;
						} catch (Exception e) {
							Console.WriteLine("'"+key+"' in ['"+group+"'] in file "+iniPath+" is not a valid "+defaultVal.GetType().Name+". Its value: "+val+", of type "+val.GetType()+": "+e);
						}
					}
				}
			}
			return defaultVal;
		}

		public virtual object ConvertTo(Type type, object obj) {
			return ConvertTools.ConvertTo(type,obj);
		}
		
		//Reads the INI file and creates a map of its data. This is private for a reason, it is called when it
		//is needed, and not before or after.
		private void ReadFile() {
			sections = new Hashtable();
			StreamReader sr = File.OpenText(iniPath); 
			string group="default";
			IniTempGroup curGroup=null;
			try {
				while (true) {
					string s = sr.ReadLine();
					if (s!=null) {
						if (s.Length>0) {
							int sharp = s.IndexOf("#"); 
							if (sharp>-1) { 
								s = s.Substring(0,sharp);
							}
							s=s.Trim();
							int lbr=s.IndexOf("[");
							int rbr=s.IndexOf("]");
							if (lbr>-1 && rbr>-1 && lbr<rbr) {
								group=s.Substring(lbr+1,rbr-lbr-1).ToLower();
								curGroup=new IniTempGroup(group);
								sections[group]=curGroup;
							} else {
								int equals = s.IndexOf("=");
								if (equals>-1) {
									string param = s.Substring(0,equals).Trim().ToLower();
									string args = s.Substring(equals+1).Trim();
									if (curGroup==null) {
										curGroup=new IniTempGroup(group);
										sections[group]=curGroup;
									}
									curGroup.Set(param,args);
								}
							}
						}
					} else {
						break;	//end of file
					}
				}
			} catch (Exception) {
			}
			sr.Close();
		}
		
		
	}
	
	//This is an internal storage class for INI data, not intended to be used outside IniHandler.
	internal class IniTempGroup {
		private string name;
		private Hashtable entries;
		internal string Name { get { return name; } }
		
		internal IniTempGroup(string name) {
			this.name=name;
			entries=new Hashtable();
		}
		
		public override string ToString() {
			string str=name+"[";
			IDictionaryEnumerator enumerator = entries.GetEnumerator();
			while ( enumerator.MoveNext() ) {
				str+="'"+enumerator.Key+"':'"+enumerator.Value+"'";
			}
			str+="]";
			return str;
		}
		
		internal bool Contains(string key) {
			return entries.ContainsKey(key);
		}

		internal void SetNoDupe(string keyname, string value) {
			entries[keyname]=value;
		}
		internal void Set(string keyname, string value) {
			if (entries[keyname]==null) {
				entries[keyname]=value;
			} else {
				if (entries[keyname] is ArrayList) {
					((ArrayList)entries[keyname]).Add(value);
				} else {
					ArrayList al=new ArrayList();
					al.Add(entries[keyname]);
					al.Add(value);
					entries[keyname]=al;
				}
			}
		}
		
		internal object Get(string keyname) {
			return entries[keyname];
		}
	}
	
	//Class: IniDataEntry
	//This represents an individual key=value entry in a data section in an INI file.
	//This doesn't need to be used by users, just call the appropriate IniEntry method on an IniDataSection
	//to specify a data entry and its default value and commment, and if you want to change it later, use
	//myIniHandler.SetValue(group, key, value).
	//Entry/key names can have spaces in them.
	internal class IniDataEntry {
		private string name;
		private object value;
		private string comment;
		private IniDataSection section;
		private IniHandler iniFile;
		public string Name { get { return name; } }
		private bool disabled;
		
		//Not for use except by IniHandler and its worker classes.
		internal IniDataEntry(string name, object value, string comment, IniDataSection section, IniHandler iniFile) {
			this.name=name;
			this.value=value;
			this.comment=comment;
			this.section=section;
			this.iniFile=iniFile;
			disabled=false;
		}
		
		//Not for use except by IniHandler and its worker classes.
		internal IniDataEntry(string name, object value, string comment, IniDataSection section, IniHandler iniFile, bool disabled) {
			this.name=name;
			this.value=value;
			this.comment=comment;
			this.section=section;
			this.iniFile=iniFile;
			this.disabled=disabled;
		}
		
		//Returns the value of this entry. This is not for use except by IniHandler and its worker classes.
		internal object GetValue() {
			return iniFile.GetValue(section.Name, Name, value);
		}
		
		//Sets the value of this entry. This is not for use except by IniHandler and its worker classes. Use
		//myIniHandler.SetValue(group, key, value) (which will eventually call this itself).
		internal void SetValue(object o) {
			value=o;
		}
		
		//Used internally by IniHandler to write out the components of the INI file.
		internal void Write(StreamWriter sw) {
			string strOut="";
			if (comment!=null && comment.Length>0) {
				sw.WriteLine("");
				strOut="# ("+name+": "+comment+")";
				while (strOut.Length>80) {
					int space=strOut.LastIndexOf(' ',80);
					if (space>-1) {
						string s=strOut.Substring(0,space);
						strOut="# "+strOut.Substring(space);
						sw.WriteLine(s);
					} else {
						break;
					}
				}
				sw.WriteLine(strOut);
			}
			if (disabled) {
				strOut="#";
			} else {
				strOut="";
			}
			strOut+=name+"="+GetValue().ToString();
			/*for (int a=strOut.Length; a<59; a++) {
				strOut+=" ";
			}
			strOut+=" #"+comment;*/
			sw.WriteLine(strOut);
		}
	}
	
	//Class: IniComment
	//This is used to represent a comment in an INI file.
	//(This exists so that they're written out every time the file is written)
	//This doesn't need to be used by users, just call the appropriate Comment method (On an IniHandler or an IniDataSection).
	internal class IniComment {
		private string comment;
		
		//Not for use except by IniHandler and its worker classes.
		internal IniComment(string comment) {
			this.comment=comment;
		}
		
		//Used internally by IniHandler to write out the components of the INI file.
		internal void Write(StreamWriter sw) {
			string strOut="";
			//sw.WriteLine("");
			strOut="# "+comment;
			while (strOut.Length>80) {
				int space=strOut.LastIndexOf(' ',80);
				if (space>-1) {
					string s=strOut.Substring(0,space);
					strOut="# "+strOut.Substring(space+1);
					sw.WriteLine(s);
				} else {
					break;
				}
			}
			sw.WriteLine(strOut);
			//sw.WriteLine("");
		}
	}
	
	//Class: IniDataSection
	//This represents a group/section in an INI.
	public class IniDataSection {
		private string name;
		private ArrayList entries;
		private IniHandler iniFile;
		public string Name { get { return name; } }
		
		//This isn't intended for usage outside of IniHandler - Call the IniSection(name) method on IniHandler.
		internal IniDataSection(string name, IniHandler iniFile) {
			this.name=name;
			this.iniFile=iniFile;
			this.entries=new ArrayList();
		}
		
		//Add an entry to this data section. Having a comment for every entry is strongly encouraged.
		public object IniEntry(string name, object value, string comment) {
			IniDataEntry idi = new IniDataEntry(name, value, comment, this, iniFile);
			
			this.entries.Add(idi);
			return idi.GetValue();
		}
			
		//Add a disabled-by-default entry to this data section.
		//Having a comment for every entry is strongly encouraged.
		//This entry will be commented out by default. The user can uncomment it in the INI file if they choose.
		//Entry/key names can have spaces in them.
		public object IniEntry(string name, object value, string comment, bool disabled) {
			IniDataEntry idi = new IniDataEntry(name, value, comment, this, iniFile, disabled);
			
			this.entries.Add(idi);
			return idi.GetValue();
		}
		
		//Changes the value of the specified entry. This can be called directly, but it's usually encouraged
		//to use myIniHandler.SetValue("Keys","Turn left",value) instead (That was an example from Planets' code).
		public void SetValue(string key, object value) {
			string lower_key=key.ToLower();
			foreach (object o in entries) {
				if (o is IniDataEntry) {
					IniDataEntry ide = (IniDataEntry) o;
					if (ide.Name.ToLower()==lower_key) {
						ide.SetValue(value);
						return;
					}
				}
			}
			IniDataEntry idi=new IniDataEntry(key,value,"",this,iniFile);
			entries.Add(idi);
		}

		public bool Contains(string key) {
			if (iniFile!=null)
				return iniFile.Contains(this.Name.ToLower(),key.ToLower());
			return false;
		}
		
		//Adds a comment to this section. When/if the INI file is written, comments and entries in each group will be 
		//written to the INI in the order that they were specified in, with each group written in the order in which
		//they were created. (You won't find a comment for one group written out in another group, even if you specify
		//it after making the other group. Besides, if that happened, it would be a bug. :O)
		public void Comment(string comment) {
			IniComment ici = new IniComment(comment);
			this.entries.Add(ici);
		}
		
		//Used internally by IniHandler to write out the components of the INI file.
		internal void Write(StreamWriter sw) {
			sw.WriteLine();
			string s="";
			while (s.Length<80) {
				s+="-";
			}
			sw.WriteLine(s);
			sw.WriteLine();
			sw.WriteLine("["+Name+"]");
			for (int a=0; a<entries.Count; a++) {
				if (entries[a] is IniDataEntry) {
					IniDataEntry entry = (IniDataEntry) entries[a];
					entry.Write(sw);
				} else if (entries[a] is IniComment) {
					IniComment entry = (IniComment) entries[a];
					entry.Write(sw);
				}
			}
		}
	}
}