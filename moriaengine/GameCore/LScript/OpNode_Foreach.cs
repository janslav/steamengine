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
using SteamEngine.Common;
using PerCederberg.Grammatica.Parser;

namespace SteamEngine.LScript {
	public class OpNode_Foreach : OpNode, IOpNodeHolder {
		private int localIndex;
		private string localName;//just for the ToString()
		private OpNode enumerableNode;
		private OpNode blockNode;//can be null

		internal static OpNode Construct(IOpNodeHolder parent, Node code) {
			int line = code.GetStartLine() + LScript.startLine;
			int column = code.GetStartColumn();
			OpNode_Foreach constructed = new OpNode_Foreach(
				parent, LScript.GetParentScriptHolder(parent).filename, line, column, code);

			//LScript.DisplayTree(code);

			Production mainProd = (Production) code;
			Production headProd = GetHeaderCode(mainProd.GetChildAt(1));//FOREACH_HEADER_CODE or FOREACH_HEADER_IN_PARENS
			string localName = GetLocalName(headProd.GetChildAt(0));
			constructed.localName = localName;
			constructed.localIndex = constructed.ParentScriptHolder.GetRegisterIndex(localName);
			constructed.enumerableNode = LScript.CompileNode(constructed, headProd.GetChildAt(2), true);
			if (mainProd.GetChildCount() == 6) {//has the Script node inside?
				constructed.blockNode = LScript.CompileNode(constructed, mainProd.GetChildAt(3), true);
			}
			return constructed;
		}

		private static string GetLocalName(Node node) {
			if (node is Token) {
				return ((Token) node).GetImage().Trim();
			} else {
				return ((Token) node.GetChildAt(2)).GetImage().Trim();
			}
		}

		private static Production GetHeaderCode(Node node) {
			if (IsType(node, StrictConstants.FOREACH_HEADER_CODE)) {
				return (Production) node;
			} else if (IsType(node, StrictConstants.FOREACH_HEADER_IN_PARENS)) {
				return GetHeaderCode(node.GetChildAt(1));
			} else {
				throw new SEException("Unexpected node. This should not happen.");
			}
		}

		private OpNode_Foreach(IOpNodeHolder parent, string filename, int line, int column, Node origNode)
			: base(parent, filename, line, column, origNode) {

		}

		public void Replace(OpNode oldNode, OpNode newNode) {
			if (enumerableNode == oldNode) {
				enumerableNode = newNode;
				return;
			}
			if (blockNode == oldNode) {
				blockNode = newNode;
				return;
			}
			throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
		}

		internal override object Run(ScriptVars vars) {
			if (blockNode != null) {
				object retVal = null;
				IEnumerable enumerable = enumerableNode.Run(vars) as IEnumerable;
				if (enumerable != null) {
					IEnumerator enumerator = enumerable.GetEnumerator();
					while ((!vars.returned) && (enumerator.MoveNext())) {
						vars.localVars[localIndex] = enumerator.Current;
						retVal = blockNode.Run(vars);
					}
				} else {
					throw new InterpreterException("Result of the expression '" + LogStr.Ident(enumerableNode) + "' is not an IEnumerable instance",
						this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName());
				}
				return retVal;
			} else {
				return null;//if there is no code to run, we dont do anything.
			}
		}

		public override string ToString() {
			return String.Concat("Foreach (", localName, " in ", enumerableNode, ")",
				Environment.NewLine, blockNode, Environment.NewLine, "endforeach");
		}
	}
}