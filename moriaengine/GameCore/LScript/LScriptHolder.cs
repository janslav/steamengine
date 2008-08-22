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
using SteamEngine;
using SteamEngine.Packets;
using SteamEngine.Common;
using PerCederberg.Grammatica.Parser;

namespace SteamEngine.LScript {
	public class LScriptHolder : ScriptHolder, IOpNodeHolder, IUnloadable {
		internal string filename = "<default>";
		internal int line;
		internal OpNode code = null;
		internal Hashtable registerNames = new Hashtable(StringComparer.OrdinalIgnoreCase);
		//this gets set by the nodes, when an arg gets declared, so that we know how many ARGs can appear in this function
		internal OpNode nodeToReturn = null;

		//used by TriggerGroup/GumpDef/templatedef/... loading, and LoadAsFunction here
		public LScriptHolder(TriggerSection input) : base(input.triggerName) {
			Compile(input);
		}
		
		//a little hack for gump response triggers.
		//internal LScriptHolder(TriggerSection input, params string[] addedLocals) : base(input.triggerName) {
		//    //registerNames = new Hashtable(StringComparer.OrdinalIgnoreCase);
		//    for (int i = 0, n = addedLocals.Length; i<n; i++) {
		//        GetRegisterIndex(addedLocals[i]);
		//    }
		//    Compile(input);
		//}
		
		internal LScriptHolder() : base("temporary") {
		}

		public void Replace(OpNode oldNode, OpNode newNode) {
			if (code == oldNode) {
				code = newNode;
			} else {
				throw new Exception("Nothing to replace the node "+oldNode+" at "+this+"  with. This should not happen.");
			}
		}
		
		internal int GetRegisterIndex(string name) {
			if (!registerNames.ContainsKey(name)) {
				registerNames[name] = registerNames.Count;
			}
			return (int) registerNames[name];
		}


		internal bool containsRandom = false;
		public bool ContainsRandomExpression { get {
			return containsRandom;
		} }
		
		internal void Compile(TriggerSection input) {
			filename = input.filename;
			line = input.startline;
			//if (registerNames == null) {//else it got already created by the constructor
			//	registerNames = new Hashtable(StringComparer.OrdinalIgnoreCase);
			//}
			//Console.WriteLine("compiling text "+input.code);
			code = LScript.TryCompile(this, new StringReader(input.code.ToString()), input.startline);
			unloaded = (code == null);
			//Logger.WriteDebug("the code is: "+code);
		}

		public sealed override object Run(object self, ScriptArgs sa) {
			if (unloaded) {
				throw new UnloadedException("Function/trigger "+LogStr.Ident(name)+" is unloaded, can not be run.");
			}
			lastRunSuccesful = false;
			try {
				ScriptVars sv = new ScriptVars(sa, self, registerNames.Count);
				object retVal = code.Run(sv);
				lastRunSuccesful = true;
				if (sv.returned) {
					return retVal;
				} else {
					return null;//we should not randomly return the last expression...
				}
			} catch (Exception e) {
				this.lastRunException = e;
				throw;
			}
		}
		
		protected sealed override void Error(Exception e) {
			Logger.WriteError(filename, line, e);
		}
		
		public void Unload() {
			code = null;
			registerNames = new Hashtable(StringComparer.OrdinalIgnoreCase);
			unloaded = true;
		}

		public bool IsUnloaded {
			get {
				return unloaded;
			}
		}

		public override string Description {
			get {
				return filename + "(L" + line + ")";
			}
		}

	}
	
	public class ScriptVars {
		internal ScriptArgs scriptArgs;
		internal object self;
		internal object[] localVars;
		internal bool returned;
		internal readonly object defaultObject;
		internal readonly int uid;
		
		private static int uids;
		
		internal ScriptVars(ScriptArgs scriptArgs, object self, int capacity) {
			this.scriptArgs = scriptArgs;
			this.self = self;
			this.uid = uids++;
			this.defaultObject = self;
			this.localVars = new object[capacity];
			this.returned = false;
		}
	}
}