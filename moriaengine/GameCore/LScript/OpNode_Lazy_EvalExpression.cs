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

namespace SteamEngine.LScript {
	public class OpNode_Lazy_EvalExpression : OpNode, IOpNodeHolder {
		//accepts EvalExpression, StrongEvalExpression
		protected OpNode arg;

		internal static OpNode Construct(IOpNodeHolder parent, Node code) {
			int line = code.GetStartLine() + LScript.startLine;
			int column = code.GetStartColumn();
			OpNode_Lazy_EvalExpression constructed = new OpNode_Lazy_EvalExpression(
				parent, LScript.GetParentScriptHolder(parent).filename, line, column, code);

			//todo ?

			if (IsType(code, StrictConstants.STRONG_EVAL_EXPRESSION)) {
				constructed.arg = LScript.CompileNode(constructed, code.GetChildAt(2), true);
			} else {
				constructed.arg = LScript.CompileNode(constructed, code.GetChildAt(1), true);
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
			if (arg != oldNode) {
				throw new Exception("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			} else {
				arg = newNode;
			}
		}

		internal override object Run(ScriptVars vars) {
			object retVar = arg.Run(vars);
			ReplaceSelf(arg);
			return retVar;
		}

		public override string ToString() {
			return "<" + arg + ">";
		}
	}
}