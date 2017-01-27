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
using System.Diagnostics.CodeAnalysis;
using System.Text;
using PerCederberg.Grammatica.Parser;
using SteamEngine.Scripting.Compilation;

namespace SteamEngine.Scripting.Interpretation {

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_Is : OpNode, IOpNodeHolder {
		private Type type;
		internal OpNode opNode;

		internal static OpNode_Is Construct(IOpNodeHolder parent, Node code, int typeNameFromIndex, LScriptCompilationContext context) {
			var line = code.GetStartLine() + context.startLine;
			var column = code.GetStartColumn();
			var filename = LScriptMain.GetParentScriptHolder(parent).Filename;

			var constructed = new OpNode_Is(
				parent, LScriptMain.GetParentScriptHolder(parent).Filename, line, column, code);

			//LScript.DisplayTree(code);

			var sb = new StringBuilder();
			for (int i = typeNameFromIndex, n = code.GetChildCount(); i < n; i++) {
				var node = code.GetChildAt(i);
				sb.Append(((Token) node).GetImage().Trim());
			}
			var typeName = sb.ToString();

			var type = ClassManager.GetType(typeName);
			if (type == null) {
				type = Type.GetType(typeName, false, true);
			}
			if (type == null) {
				throw new InterpreterException("Type '" + typeName + "' not recognised.",
					line, column, filename, LScriptMain.GetParentScriptHolder(parent).GetDecoratedName());
			}

			constructed.type = type;

			return constructed;
		}

		protected OpNode_Is(IOpNodeHolder parent, string filename, int line, int column, Node origNode)
			: base(parent, filename, line, column, origNode) {

		}

		public void Replace(OpNode oldNode, OpNode newNode) {
			if (oldNode == this.opNode) {
				this.opNode = newNode;
				return;
			}
			throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
		}

		internal override object Run(ScriptVars vars) {
			var obj = this.opNode.Run(vars);

			return this.type.IsInstanceOfType(obj);
		}

		public override string ToString() {
			return string.Concat(this.opNode.ToString(), " IS ", this.type.Name);
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	public static class OpNode_Typeof {
		internal static OpNode Construct(IOpNodeHolder parent, Node code, LScriptCompilationContext context) {
			var line = code.GetStartLine() + context.startLine;
			var column = code.GetStartColumn();
			var filename = LScriptMain.GetParentScriptHolder(parent).Filename;

			var n = code.GetChildCount();
			if (OpNode.IsType(code.GetChildAt(1), StrictConstants.LEFT_PAREN)) {
				n--;
			}

			var sb = new StringBuilder();
			for (var i = 2; i < n; i++) {
				var node = code.GetChildAt(i);
				sb.Append(((Token) node).GetImage().Trim());
			}
			var typeName = sb.ToString();
			var type = TryRecognizeType(typeName);
			if (type == null) {
				type = TryRecognizeType(typeName + "`1"); //this is how generic types are internally named
			}
			if (type == null) {
				throw new InterpreterException("Type '" + typeName + "' not recognised.",
					line, column, filename, LScriptMain.GetParentScriptHolder(parent).GetDecoratedName());
			}

			return OpNode_Object.Construct(parent, type);
		}

		private static Type TryRecognizeType(string typeName) {
			var type = ClassManager.GetType(typeName);
			if (type == null) {
				type = Type.GetType(typeName, false, true);
			}
			return type;
		}
	}


}