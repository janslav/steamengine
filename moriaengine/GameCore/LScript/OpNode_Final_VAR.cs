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
using PerCederberg.Grammatica.Parser;

namespace SteamEngine.LScript {
	public class OpNode_SetVar : OpNode, IOpNodeHolder, ITriable {
		private readonly TagKey name;
		private OpNode arg;

		internal OpNode_SetVar(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, string name, OpNode arg)
			: base(parent, filename, line, column, origNode) {
			this.arg = arg;
			this.name = TagKey.Get(name);
		}

		public virtual void Replace(OpNode oldNode, OpNode newNode) {
			if (arg == oldNode) {
				arg = newNode;
			} else {
				throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			}
		}

		internal override object Run(ScriptVars vars) {
			object oSelf = vars.self;
			vars.self = vars.defaultObject;
			try {
				Globals.instance.SetTag(name, arg.Run(vars));
			} finally {
				vars.self = oSelf;
			}
			return null;
		}

		public object TryRun(ScriptVars vars, object[] results) {
			Globals.instance.SetTag(name, results[0]);
			return null;
		}

		public override string ToString() {
			return string.Concat("VAR ", name, " = ", arg);
		}
	}

	public class OpNode_GetVar : OpNode, ITriable {
		private readonly TagKey name;

		internal OpNode_GetVar(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, string name)
			: base(parent, filename, line, column, origNode) {
			this.name = TagKey.Get(name);
		}

		internal override object Run(ScriptVars vars) {
			return Globals.instance.GetTag(name);
		}

		public object TryRun(ScriptVars vars, object[] results) {
			return Globals.instance.GetTag(name);
		}

		public override string ToString() {
			return string.Concat("VAR(", name, ")");
		}
	}

	public class OpNode_VarExists : OpNode, ITriable {
		private readonly TagKey name;

		internal OpNode_VarExists(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, string name)
			: base(parent, filename, line, column, origNode) {
			this.name = TagKey.Get(name);
		}

		internal override object Run(ScriptVars vars) {
			return Globals.instance.HasTag(name);
		}

		public object TryRun(ScriptVars vars, object[] results) {
			return Globals.instance.HasTag(name);
		}

		public override string ToString() {
			return string.Concat("VAR.EXISTS(", name, ")");
		}
	}

	public class OpNode_RemoveVar : OpNode, ITriable {
		private readonly TagKey name;

		internal OpNode_RemoveVar(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, string name)
			: base(parent, filename, line, column, origNode) {
			this.name = TagKey.Get(name);
		}

		internal override object Run(ScriptVars vars) {
			Globals.instance.RemoveTag(name);
			return null;
		}

		public object TryRun(ScriptVars vars, object[] results) {
			Globals.instance.RemoveTag(name);
			return null;
		}

		public override string ToString() {
			return string.Concat("VAR.remove(", name, ")");
		}
	}
}