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
using System.Text;
using PerCederberg.Grammatica.Parser;
using SteamEngine.Common;

namespace SteamEngine.Scripting.Interpretation {

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
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


		internal static OpNode Construct(IOpNodeHolder parent, Node code, LScriptCompilationContext context) {
			var line = code.GetStartLine() + context.startLine;
			var column = code.GetStartColumn();
			var constructed = new OpNode_If(
				parent, LScriptMain.GetParentScriptHolder(parent).Filename, line, column, code);

			//LScript.DisplayTree(code);

			var ifProduction = (Production) code;
			var conditionsList = new List<OpNode>();
			var blocksList = new List<OpNode>();
			var n = code.GetChildCount();
			for (var i = 0; i < n; i++) {
				var node = ifProduction.GetChildAt(i);
				if ((IsType(node, StrictConstants.IF_BEGIN)) || (IsType(node, StrictConstants.ELSE_IF_BLOCK))) {
					var prod = (Production) node;//type IF_BEGIN or ELSE_IF_BLOCK, which are in this context equal
														//skipping IF / ELSEIF
					var condition = prod.GetChildAt(1);
					var condNode = LScriptMain.CompileNode(constructed, condition, true, context);
					conditionsList.Add(condNode);
					//skipping EOL
					if (prod.GetChildCount() == 4) {
						var block = prod.GetChildAt(3);
						blocksList.Add(LScriptMain.CompileNode(constructed, block, true, context));
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
			var elseNode = ifProduction.GetChildAt(n - 3);
			if (IsType(elseNode, StrictConstants.ELSE_BLOCK)) {
				var elseProd = (Production) elseNode;
				var elseCode = elseProd.GetChildAt(2);
				constructed.elseBlock = LScriptMain.CompileNode(constructed, elseCode, true, context);
			}

			constructed.blocks = blocksList.ToArray();
			constructed.conditions = conditionsList.ToArray();

			return constructed;
		}

		protected OpNode_If(IOpNodeHolder parent, string filename, int line, int column, Node origNode)
			: base(parent, filename, line, column, origNode) {

		}

		public void Replace(OpNode oldNode, OpNode newNode) {
			var index = Array.IndexOf(this.blocks, oldNode);
			if (index < 0) {
				index = Array.IndexOf(this.conditions, oldNode);
				if (index < 0) {
					if (this.elseBlock == oldNode) {
						this.elseBlock = newNode;
						return;
					}
				} else {
					this.conditions[index] = newNode;
					return;
				}
			} else {
				this.blocks[index] = newNode;
				return;
			}
			throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
		}

		internal override object Run(ScriptVars vars) {
			var wasRun = false;
			object retVal = null;
			for (int i = 0, n = this.conditions.Length; i < n; i++) {
				if (ConvertTools.ToBoolean(this.conditions[i].Run(vars))) {
					if (this.blocks[i] != null) {
						retVal = this.blocks[i].Run(vars);
					}
					wasRun = true;
					break;
				}
			}
			if ((!wasRun) && (!vars.returned) && (this.elseBlock != null)) {
				retVal = this.elseBlock.Run(vars);
			}
			return retVal;
		}

		public override string ToString() {
			var str = new StringBuilder("If (");
			str.Append(this.conditions[0]).Append(")").Append(Environment.NewLine);
			if (this.blocks[0] != null) {
				str.Append(this.blocks[0]);
			}
			for (int i = 1, n = this.conditions.Length; i < n; i++) {
				str.Append("ElseIf (").Append(this.conditions[i]).Append(")").Append(Environment.NewLine);
				if (this.blocks[i] != null) {
					str.Append(this.blocks[i]);
				}
			}
			if (this.elseBlock != null) {
				str.Append("Else").Append(Environment.NewLine);
				str.Append(this.elseBlock);
			}
			str.Append("Endif");
			return str.ToString();
		}
	}
}