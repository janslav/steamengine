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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using PerCederberg.Grammatica.Parser;
using Shielded;
using SteamEngine.Common;

//using PerCederberg.Grammatica.Parser;

namespace SteamEngine.Scripting.Interpretation {
	public class LScriptHolder : ScriptHolder, IOpNodeHolder, IUnloadable {

		private readonly Shielded<State> shieldedState = new Shielded<State>(initial: new State {
			filename = "<default>",
			localsNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
		});

		struct State {
			internal string filename;
			internal int line;
			internal OpNode code;
			internal bool unloaded;
			internal bool containsRandom;
			internal Dictionary<string, int> localsNames;
		}

		private readonly string contTriggerGroupName;

		//internal OpNode nodeToReturn;

		//used by TriggerGroup/GumpDef/templatedef/... loading, and LoadAsFunction here
		public LScriptHolder(TriggerSection input, string contTriggerGroupName = null)
			: base(input.TriggerName) {
			this.TrySetMetadataAndCompile(input);
			this.contTriggerGroupName = contTriggerGroupName;
		}

		//a little hack for gump response triggers.
		//internal LScriptHolder(TriggerSection input, params string[] addedLocals) : base(input.triggerName) {
		//    //registerNames = new Hashtable(StringComparer.OrdinalIgnoreCase);
		//    for (int i = 0, n = addedLocals.Length; i<n; i++) {
		//        GetRegisterIndex(addedLocals[i]);
		//    }
		//    Compile(input);
		//}

		internal LScriptHolder(string filename)
			: base("temporary") {
			this.shieldedState.Modify((ref State s) => s.filename = filename);
		}

		internal LScriptHolder()
			: base("temporary") {
		}

		internal int LocalVarsCount => this.shieldedState.Value.localsNames.Count;

		public bool ContainsRandomExpression => this.ContainsRandom;

		public sealed override bool IsUnloaded => this.shieldedState.Value.unloaded;

		public sealed override string Description => this.Filename + "(L" + this.Line + ")";

		public string Filename => this.shieldedState.Value.filename;

		public int Line => this.shieldedState.Value.line;

		internal OpNode Code => this.shieldedState.Value.code;

		internal bool ContainsRandom {
			get { return this.shieldedState.Value.containsRandom; }
			set { this.shieldedState.Modify((ref State s) => s.containsRandom = value); }
		}

		void IOpNodeHolder.Replace(OpNode oldNode, OpNode newNode) {
			Shield.AssertInTransaction();
			if (this.Code == oldNode) {
				this.shieldedState.Modify((ref State s) => s.code = newNode);
			} else {
				throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			}
		}

		internal int GetLocalVarIndex(string name) {
			Shield.AssertInTransaction();
			int index;
			if (!this.shieldedState.Value.localsNames.TryGetValue(name, out index)) {
				this.shieldedState.Modify((ref State s) => {
					index = s.localsNames.Count;
					s.localsNames[name] = index;
				});
			}
			return index;
		}

		internal bool ContainsLocalVarName(string name) {
			Shield.AssertInTransaction();
			return this.shieldedState.Value.localsNames.ContainsKey(name);
		}

		internal void TrySetMetadataAndCompile(TriggerSection input) {
			this.shieldedState.Modify((ref State s) => {
				s.filename = input.Filename;
				s.line = input.StartLine;
				var stringBuilder = input.Code.ToString();

				using (StringReader reader = new StringReader(stringBuilder)) {
					s.code = LScriptMain.TryCompile(this, reader, input.StartLine);
				}
				s.unloaded = (this.Code == null);
				//Logger.WriteDebug("the code is: "+code);
			});
		}

		internal void SetMetadataAndCompile(string inputFilename, int inputStartLine, string inputCode) {
			this.shieldedState.Modify((ref State s) => {
				s.filename = inputFilename;
				s.line = inputStartLine;
				var stringBuilder = inputCode;

				using (StringReader reader = new StringReader(stringBuilder)) {
					s.code = LScriptMain.Compile(this, reader, inputStartLine);
				}
				s.unloaded = (this.Code == null);
				//Logger.WriteDebug("the code is: "+code);
			});
		}

		internal object RunAsSnippet(string filename, int line, TagHolder self, string script) {
			try {
				this.SetMetadataAndCompile(inputFilename: filename, inputStartLine: line, inputCode: script);

				object retVal = this.Code.Run(
					new ScriptVars(null, self, this.LocalVarsCount, new LScriptCompilationContext { startLine = line }));
				return retVal;
			} catch (ParserLogException ple) {
				LogStr lstr = (LogStr) "";
				for (int i = 0, n = ple.GetErrorCount(); i < n; i++) {
					ParseException pe = ple.GetError(i);
					int curline = pe.GetLine() + line;
					if (i > 0) {
						lstr = lstr + Environment.NewLine;
					}
					lstr = lstr + LogStr.FileLine(filename, curline)
						   + pe.GetErrorMessage();
					//Logger.WriteError(WorldSaver.currentfile, curline, pe.GetErrorMessage());
				}
				throw new SEException(lstr);
			} catch (RecursionTooDeepException rtde) {
				throw rtde; // we really do want to rethrow it, so that its useless stack is lost.
			}
		}

		public sealed override object Run(object self, ScriptArgs sa) {
			Shield.AssertInTransaction();
			var state = this.shieldedState.Value;
			if (state.unloaded) {
				throw new UnloadedException("Function/trigger " + LogStr.Ident(this.Name) + " is unloaded, can not be run.");
			}
			ScriptVars sv = new ScriptVars(sa, self, this.LocalVarsCount, new LScriptCompilationContext { startLine = state.line });
			object retVal = state.code.Run(sv);
			if (sv.returned) {
				return retVal;
			}
			return null;//we should not randomly return the last expression...
		}

		protected sealed override void Error(Exception e) {
			Logger.WriteError(this.Filename, this.Line, e);
		}

		public void Unload() {
			this.shieldedState.Modify((ref State s) => {
				s.code = null;
				s.localsNames.Clear();
				s.unloaded = true;
			});
		}

		public override string GetDecoratedName() {
			if (string.IsNullOrWhiteSpace(this.contTriggerGroupName)) {
				return this.Name;
			}
			return this.contTriggerGroupName + ": @" + this.Name;
		}
	}

	public class ScriptVars {
		internal ScriptArgs scriptArgs;
		internal object self;
		internal readonly LScriptCompilationContext compilationContext;
		internal object[] localVars;
		internal bool returned;
		internal readonly object defaultObject;
		//internal readonly int uid;

		//private static int uids;

		internal ScriptVars(ScriptArgs scriptArgs, object self, int capacity, LScriptCompilationContext compilationContext) {
			this.scriptArgs = scriptArgs;
			this.self = self;
			this.compilationContext = compilationContext;
			//this.uid = uids++;
			this.defaultObject = self;
			this.localVars = new object[capacity];
			//this.returned = false;
		}
	}

	public class LScriptCompilationContext {
		public int startLine;
		public string indent = "";
	}
}