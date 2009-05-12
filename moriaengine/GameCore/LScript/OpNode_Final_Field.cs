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
using PerCederberg.Grammatica.Parser;
using SteamEngine.Common;

namespace SteamEngine.LScript {
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_SetField : OpNode, IOpNodeHolder, ITriable, IKnownRetType {
		internal readonly FieldInfo field;
		private OpNode arg;

		internal OpNode_SetField(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, FieldInfo field, OpNode arg)
			: base(parent, filename, line, column, origNode) {
			this.arg = arg;
			this.field = field;
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
			object result;
			try {
				result = arg.Run(vars);
			} finally {
				vars.self = oSelf;
			}
			try {
				field.SetValue(oSelf, result);
				return null;
			} catch (Exception e) {
				throw new InterpreterException("Exception while setting field '" + field.Name + "'",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				field.SetValue(vars.self, results[0]);
				return null;
			} catch (Exception e) {
				throw new InterpreterException("Exception while setting field '" + field.Name + "'",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public override string ToString() {
			return string.Format(System.Globalization.CultureInfo.InvariantCulture, 
			"({0} {1}.{2} = {3})", field.FieldType, field.DeclaringType, field.Name, arg);
		}

		public Type ReturnType {
			get {
				return typeof(void);
			}
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_GetField : OpNode, ITriable, IKnownRetType {
		private readonly FieldInfo field;

		internal OpNode_GetField(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, FieldInfo field)
			: base(parent, filename, line, column, origNode) {
			this.field = field;
		}

		internal override object Run(ScriptVars vars) {
			try {
				return field.GetValue(vars.self);
			} catch (Exception e) {
				throw new InterpreterException("Exception while getting field '" + field.Name + "'",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return field.GetValue(vars.self);
			} catch (Exception e) {
				throw new InterpreterException("Exception while getting field '" + field.Name + "'",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public override string ToString() {
			return string.Format(System.Globalization.CultureInfo.InvariantCulture, 
				"({0} {1}.{2})", field.FieldType, field.DeclaringType, field.Name);
		}

		public Type ReturnType {
			get {
				return field.FieldType;
			}
		}
	}


	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_InitField_String : OpNode, IOpNodeHolder, ITriable, IKnownRetType {
		internal readonly FieldInfo field;
		private OpNode[] args;
		private readonly string formatString;

		internal OpNode_InitField_String(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, FieldInfo field, OpNode[] args, string formatString)
			: base(parent, filename, line, column, origNode) {
			this.args = args;
			this.field = field;
			this.formatString = formatString;
		}

		public virtual void Replace(OpNode oldNode, OpNode newNode) {
			int index = Array.IndexOf(args, oldNode);
			if (index >= 0) {
				args[index] = newNode;
				return;
			}
			throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
		}

		internal override object Run(ScriptVars vars) {
			object oSelf = vars.self;
			vars.self = vars.defaultObject;
			int argsCount = args.Length;
			object[] results = new object[argsCount];
			try {
				for (int i = 0; i < argsCount; i++) {
					results[i] = args[i].Run(vars);
				}
			} finally {
				vars.self = oSelf;
			}
			try {
				string resultString = String.Format(System.Globalization.CultureInfo.InvariantCulture, 
					formatString, results);
				field.SetValue(oSelf, resultString);
				return null;
			} catch (Exception e) {
				throw new InterpreterException("Exception while setting field '" + field.Name + "'",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				string resultString = String.Format(System.Globalization.CultureInfo.InvariantCulture, 
					formatString, results);
				field.SetValue(vars.self, resultString);
				return null;
			} catch (Exception e) {
				throw new InterpreterException("Exception while setting field '" + field.Name + "'",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public override string ToString() {
			StringBuilder str = new StringBuilder("(");
			str.AppendFormat("({0} {1}.{2} = (", field.FieldType, field.DeclaringType, field.Name);
			for (int i = 0, n = args.Length; i < n; i++) {
				str.Append(args[i].ToString()).Append(", ");
			}
			return str.Append(").TOSTRING())").ToString();
		}

		public Type ReturnType {
			get {
				return typeof(void);
			}
		}
	}
}