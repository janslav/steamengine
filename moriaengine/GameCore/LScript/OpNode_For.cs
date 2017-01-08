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
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
	internal class OpNode_For : OpNode, IOpNodeHolder {
		private int localIndex;
		private string localName;//just for the ToString()
		private OpNode leftBoundNode;
		private OpNode rightBoundNode;
		private OpNode blockNode;//can be null

		internal static OpNode Construct(IOpNodeHolder parent, Node code) {
			int line = code.GetStartLine() + LScriptMain.startLine;
			int column = code.GetStartColumn();
			OpNode_For constructed = new OpNode_For(
				parent, LScriptMain.GetParentScriptHolder(parent).filename, line, column, code);

			//LScript.DisplayTree(code);
			constructed.localName = "localName";

			Production mainProd = (Production) code;
			Production headProd = GetHeaderCode(mainProd.GetChildAt(1));//FOR_HEADER_CODE or FOR_HEADER_IN_PARENS
			string localName = GetLocalName(headProd.GetChildAt(0));
			constructed.localName = localName;
			constructed.localIndex = constructed.ParentScriptHolder.GetLocalVarIndex(localName);


			constructed.leftBoundNode = LScriptMain.CompileNode(constructed, headProd.GetChildAt(2));
			constructed.rightBoundNode = LScriptMain.CompileNode(constructed, headProd.GetChildAt(4));


			if (mainProd.GetChildCount() == 6) {//has the Script node inside?
				constructed.blockNode = LScriptMain.CompileNode(constructed, mainProd.GetChildAt(3), true);
			}
			return constructed;
		}

		private static string GetLocalName(Node node) {
			Token asToken = node as Token;
			if (asToken != null) {
				return asToken.GetImage().Trim();
			} else {
				return ((Token) node.GetChildAt(2)).GetImage().Trim();
			}
		}

		private static Production GetHeaderCode(Node node) {
			if (IsType(node, StrictConstants.FOR_HEADER_CODE)) {
				return (Production) node;
			} else if (IsType(node, StrictConstants.FOR_HEADER_IN_PARENS)) {
				return GetHeaderCode(node.GetChildAt(1));
			} else {
				throw new SEException("Unexpected node. This should not happen.");
			}
		}

		private OpNode_For(IOpNodeHolder parent, string filename, int line, int column, Node origNode)
			: base(parent, filename, line, column, origNode) {

		}

		public void Replace(OpNode oldNode, OpNode newNode) {
			if (this.leftBoundNode == oldNode) {
				this.leftBoundNode = newNode;
				return;
			}
			if (this.rightBoundNode == oldNode) {
				this.rightBoundNode = newNode;
				return;
			}
			if (this.blockNode == oldNode) {
				this.blockNode = newNode;
				return;
			}
			throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
		}

		internal override object Run(ScriptVars vars) {
			if (this.blockNode != null) {
				try {
					int leftBound = Convert.ToInt32(this.leftBoundNode.Run(vars), CultureInfo.InvariantCulture);
					int rightBound = Convert.ToInt32(this.rightBoundNode.Run(vars), CultureInfo.InvariantCulture);
					int step;
					if (leftBound < rightBound) {
						step = 1;
					} else if (leftBound > rightBound) {
						step = -1;
					} else { //leftBound == rightBound
						step = 0;
					}

					object retVal = null;
					leftBound -= step;
					do {
						leftBound += step;
						vars.localVars[this.localIndex] = leftBound;
						retVal = this.blockNode.Run(vars);
					} while ((!vars.returned) && (leftBound != rightBound));

					return retVal;
				} catch (InterpreterException) {
					throw;
				} catch (FatalException) {
					throw;
				} catch (Exception e) {
					throw new InterpreterException("Expression while evaluating FOR statement",
						this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
				}
			} else {
				return null;//if there is no code to run, we dont do anything.
			}
		}

		public override string ToString() {
			return String.Concat("For (", this.localName, ", ", this.leftBoundNode, ", ", this.rightBoundNode, ")",
				Environment.NewLine, this.blockNode, Environment.NewLine, "endfor");
		}
	}
}