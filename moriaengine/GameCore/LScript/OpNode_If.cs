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
using System.Text;
using PerCederberg.Grammatica.Parser;

namespace SteamEngine.LScript {

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_If : OpNode, IOpNodeHolder {
		//accepts
		private OpNode[] conditions;
		private OpNode[] blocks;
		private OpNode elseBlock;
		//if (conditions[0])
		//	block[0]
		//elseif (conditions[1])
		//	block[1]
		//...
		//else
		//	elseBlock


		internal static OpNode Construct(IOpNodeHolder parent, Node code) {
			int line = code.GetStartLine() + LScriptMain.startLine;
			int column = code.GetStartColumn();
			OpNode_If constructed = new OpNode_If(
				parent, LScriptMain.GetParentScriptHolder(parent).filename, line, column, code);

			//LScript.DisplayTree(code);

			Production ifProduction = (Production) code;
            List<OpNode> conditionsList = new List<OpNode>();
            List<OpNode> blocksList = new List<OpNode>();
			int n = code.GetChildCount();
			for (int i = 0; i < n; i++) {
				Node node = ifProduction.GetChildAt(i);
				if ((IsType(node, StrictConstants.IF_BEGIN)) || (IsType(node, StrictConstants.ELSE_IF_BLOCK))) {
					Production prod = (Production) node;//type IF_BEGIN or ELSE_IF_BLOCK, which are in this context equal
					//skipping IF / ELSEIF
					Node condition = prod.GetChildAt(1);
					OpNode condNode = LScriptMain.CompileNode(constructed, condition, true);
					conditionsList.Add(condNode);
					//skipping EOL
					if (prod.GetChildCount() == 4) {
						Node block = prod.GetChildAt(3);
						blocksList.Add(LScriptMain.CompileNode(constructed, block, true));
					} else {
						blocksList.Add(null);
					}
				} else {
					break;
				}
			}
			if (conditionsList.Count != blocksList.Count) {
				throw new SEException("assertion: conditionsList.Count != blocksList.Count.   This should not happen!");
			}
			Node elseNode = ifProduction.GetChildAt(n - 3);
			if (IsType(elseNode, StrictConstants.ELSE_BLOCK)) {
				Production elseProd = (Production) elseNode;
				Node elseCode = elseProd.GetChildAt(2);
				constructed.elseBlock = LScriptMain.CompileNode(constructed, elseCode, true);
			}

            constructed.blocks = blocksList.ToArray();
            constructed.conditions = conditionsList.ToArray();

			return constructed;
		}

		protected OpNode_If(IOpNodeHolder parent, string filename, int line, int column, Node origNode)
			: base(parent, filename, line, column, origNode) {

		}

		public void Replace(OpNode oldNode, OpNode newNode) {
			int index = Array.IndexOf(blocks, oldNode);
			if (index < 0) {
				index = Array.IndexOf(conditions, oldNode);
				if (index < 0) {
					if (elseBlock == oldNode) {
						elseBlock = newNode;
						return;
					}
				} else {
					conditions[index] = newNode;
					return;
				}
			} else {
				blocks[index] = newNode;
				return;
			}
			throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
		}

		internal override object Run(ScriptVars vars) {
			bool wasRun = false;
			object retVal = null;
			for (int i = 0, n = conditions.Length; i < n; i++) {
				if (TagMath.ToBoolean(conditions[i].Run(vars))) {
					if (blocks[i] != null) {
						retVal = blocks[i].Run(vars);
					}
					wasRun = true;
					break;
				}
			}
			if ((!wasRun) && (!vars.returned) && (elseBlock != null)) {
				retVal = elseBlock.Run(vars);
			}
			return retVal;
		}

		public override string ToString() {
			StringBuilder str = new StringBuilder("If (");
			str.Append(conditions[0].ToString()).Append(")").Append(Environment.NewLine);
			if (blocks[0] != null) {
				str.Append(blocks[0].ToString());
			}
			for (int i = 1, n = conditions.Length; i < n; i++) {
				str.Append("ElseIf (").Append(conditions[i].ToString()).Append(")").Append(Environment.NewLine);
				if (blocks[i] != null) {
					str.Append(blocks[i].ToString());
				}
			}
			if (elseBlock != null) {
				str.Append("Else").Append(Environment.NewLine);
				str.Append(elseBlock.ToString());
			}
			str.Append("Endif");
			return str.ToString();
		}
	}
}