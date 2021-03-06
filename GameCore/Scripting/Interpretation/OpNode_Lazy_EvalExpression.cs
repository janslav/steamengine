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

using System.Diagnostics.CodeAnalysis;
using PerCederberg.Grammatica.Parser;

namespace SteamEngine.Scripting.Interpretation {
	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_Lazy_EvalExpression : OpNode, IOpNodeHolder {
		//accepts EvalExpression, StrongEvalExpression
		private OpNode arg;

		internal static OpNode Construct(IOpNodeHolder parent, Node code, LScriptCompilationContext context) {
			var line = code.GetStartLine() + context.startLine;
			var column = code.GetStartColumn();
			var constructed = new OpNode_Lazy_EvalExpression(
				parent, LScriptMain.GetParentScriptHolder(parent).Filename, line, column, code);

			//todo ?

			if (IsType(code, StrictConstants.STRONG_EVAL_EXPRESSION)) {
				constructed.arg = LScriptMain.CompileNode(constructed, code.GetChildAt(2), true, context);
			} else {
				constructed.arg = LScriptMain.CompileNode(constructed, code.GetChildAt(1), true, context);
			}

			if (constructed.arg is OpNode_Object) {
				return constructed.arg;
			}

			return constructed;
		}

		protected OpNode_Lazy_EvalExpression(IOpNodeHolder parent, string filename, int line, int column, Node origNode)
			: base(parent, filename, line, column, origNode) {
		}

		public void Replace(OpNode oldNode, OpNode newNode) {
			if (this.arg != oldNode) {
				throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			}
			this.arg = newNode;
		}

		internal override object Run(ScriptVars vars) {
			var retVar = this.arg.Run(vars);
			this.ReplaceSelf(this.arg);
			return retVar;
		}

		public override string ToString() {
			return "<" + this.arg + ">";
		}
	}
}