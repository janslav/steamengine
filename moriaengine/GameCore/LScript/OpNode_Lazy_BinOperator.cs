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
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using PerCederberg.Grammatica.Parser;

namespace SteamEngine.LScript {
	public class OpNode_Lazy_BinOperator : OpNode, IOpNodeHolder {
		internal OpNode left;
		internal OpNode right;

		private object leftResult;
		private object rightResult;

		internal static OpNode_Lazy_BinOperator Construct(IOpNodeHolder parent, Node code) {
			return new OpNode_Lazy_BinOperator(parent, code);
		}

		internal OpNode_Lazy_BinOperator(IOpNodeHolder parent, Node code)
			:
			base(parent, LScript.GetParentScriptHolder(parent).filename, code.GetStartLine() + LScript.startLine,
			code.GetStartColumn(), code) {
		}

		public void Replace(OpNode oldNode, OpNode newNode) {
			if (left == oldNode) {
				left = newNode;
			} else if (right == oldNode) {
				right = newNode;
			} else {
				throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			}
		}

		internal override object Run(ScriptVars vars) {
			string opString = LScript.GetString(origNode).Trim().ToLower();
			leftResult = left.Run(vars);
			rightResult = right.Run(vars);

			try {
				ITriable newNode = null;

				switch (opString) {
					case ("+"):
						if (OperandsAreNumbers()) {
							newNode = new OpNode_AddOperator(parent, origNode);
						} else if ((leftResult is string) || (rightResult is string)) {
							newNode = new OpNode_ConcatOperator(parent, origNode);
						} else {
							newNode = FindOperatorMethod("op_Addition");
						}
						break;
					case ("-"):
						if (OperandsAreNumbers()) {
							newNode = new OpNode_SubOperator(parent, origNode);
						} else {
							newNode = FindOperatorMethod("op_Subtraction");
						}
						break;
					case ("/"):
						if (OperandsAreNumbers()) {
							if (Globals.ScriptFloats) {
								newNode = new OpNode_DivOperator_Double(parent, origNode);
							} else {
								newNode = new OpNode_DivOperator_Int(parent, origNode);
							}
						} else {
							newNode = FindOperatorMethod("op_Division");
						}
						break;
					case ("div"):
						if (OperandsAreNumbers()) {
							newNode = new OpNode_DivOperator_Int(parent, origNode);
						}
						break;
					case ("*"):
						if (OperandsAreNumbers()) {
							newNode = new OpNode_MulOperator(parent, origNode);
						} else {
							newNode = FindOperatorMethod("op_Multiply");
						}
						break;
					case ("%"):
						if (OperandsAreNumbers()) {
							newNode = new OpNode_ModOperator(parent, origNode);
						} else {
							newNode = FindOperatorMethod("op_Modulus");
						}
						break;
					case ("&"):
						if (OperandsAreNumbers()) {
							newNode = new OpNode_BinaryAndOperator(parent, origNode);
						} else {
							newNode = FindOperatorMethod("op_BitwiseAnd");
						}
						break;
					case ("|"):
						if (OperandsAreNumbers()) {
							newNode = new OpNode_BinaryOrOperator(parent, origNode);
						} else {
							newNode = FindOperatorMethod("op_BitwiseOr");
						}
						break;
					case ("=="):
						if (OperandsAreNumbers()) {
							if (Globals.ScriptFloats) {
								newNode = new OpNode_EqualityOperator_Double(parent, origNode);
							} else {
								newNode = new OpNode_EqualityOperator_Int(parent, origNode);
							}
						} else {
							newNode = FindOperatorMethod("op_Equality");
							if (newNode == null) {
								newNode = new OpNode_EqualsOperator(parent, origNode);
							}
						}
						break;
					case ("!="):
						if (OperandsAreNumbers()) {
							if (Globals.ScriptFloats) {
								newNode = new OpNode_InEqualityOperator_Double(parent, origNode);
							} else {
								newNode = new OpNode_InEqualityOperator_Int(parent, origNode);
							}
						} else {
							newNode = FindOperatorMethod("op_Inequality");
							if (newNode == null) {
								newNode = new OpNode_EqualsNotOperator(parent, origNode);
							}
						}
						break;
					case ("<="):
						if (OperandsAreNumbers()) {
							if (Globals.ScriptFloats) {
								newNode = new OpNode_LessThanOrEqualOperator_Double(parent, origNode);
							} else {
								newNode = new OpNode_LessThanOrEqualOperator_Int(parent, origNode);
							}
						} else {
							newNode = FindOperatorMethod("op_LessThanOrEqual");
						}
						break;
					case (">="):
						if (OperandsAreNumbers()) {
							if (Globals.ScriptFloats) {
								newNode = new OpNode_GreaterThanOrEqualOperator_Double(parent, origNode);
							} else {
								newNode = new OpNode_GreaterThanOrEqualOperator_Int(parent, origNode);
							}
						} else {
							newNode = FindOperatorMethod("op_GreaterThanOrEqual");
						}
						break;
					case ("<"):
						if (OperandsAreNumbers()) {
							if (Globals.ScriptFloats) {
								newNode = new OpNode_LessThanOperator_Double(parent, origNode);
							} else {
								newNode = new OpNode_LessThanOperator_Int(parent, origNode);
							}
						} else {
							newNode = FindOperatorMethod("op_LessThan");
						}
						break;
					case (">"):
						if (OperandsAreNumbers()) {
							if (Globals.ScriptFloats) {
								newNode = new OpNode_GreaterThanOperator_Double(parent, origNode);
							} else {
								newNode = new OpNode_GreaterThanOperator_Int(parent, origNode);
							}
						} else {
							newNode = FindOperatorMethod("op_GreaterThan");
						}
						break;
					default:
						throw new SEException("Operator " + opString + " unkown or not implemented.");
				}

				if (newNode != null) {
					object retVal;
					if (newNode is OpNode_Lazy_BinOperator) {
						((OpNode_Lazy_BinOperator) newNode).left = left;
						((OpNode_Lazy_BinOperator) newNode).right = right;
					}
					ReplaceSelf((OpNode) newNode);
					retVal = newNode.TryRun(vars, new object[] { leftResult, rightResult });
					if ((left is OpNode_Object) && (right is OpNode_Object)) {
						//both operands are constant -> result is also constant
						OpNode constNode = OpNode_Object.Construct(parent, retVal);
						parent.Replace((OpNode) newNode, constNode);
					}
					return retVal;
				}
				throw new SEException(string.Format("Operator {0} is not applicable to these operands(type {1} and {2}).",
					opString,
					(leftResult == null ? "<null>" : leftResult.GetType().Name),
					(rightResult == null ? "<null>" : rightResult.GetType().Name)));
			} catch (InterpreterException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating binary operator",
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		private bool OperandsAreNumbers() {
			if (leftResult == null) {
				if (rightResult == null) {
					return false;
				}
				return TagMath.IsNumberType(rightResult.GetType());
			} else if (rightResult == null) {
				return TagMath.IsNumberType(leftResult.GetType());
			}
			return ((TagMath.IsNumberType(leftResult.GetType())) && (TagMath.IsNumberType(rightResult.GetType())));
		}

		private OpNode_MethodWrapper FindOperatorMethod(string methodName) {
			if (leftResult == null) {
				if (rightResult == null) {
					return null;
				}
				return FindOperatorMethodOnType(methodName, rightResult.GetType());
			} else if (rightResult == null) {
				return FindOperatorMethodOnType(methodName, leftResult.GetType());
			} else {
				OpNode_MethodWrapper method = FindOperatorMethodOnType(methodName, leftResult.GetType());
				if (method != null) {
					return method;
				}
				return FindOperatorMethodOnType(methodName, rightResult.GetType());
			}
		}

		private OpNode_MethodWrapper FindOperatorMethodOnType(string methodName, Type type) {
			List<MethodInfo> matches = new List<MethodInfo>();

			MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
			foreach (MethodInfo mi in methods) {
				if (mi.Name.Equals(methodName)) { //true for case insensitive
					ParameterInfo[] pars = mi.GetParameters();
					if (pars.Length == 2) {
						if (MemberResolver.IsCompatibleType(pars[0].ParameterType, leftResult)
								&& MemberResolver.IsCompatibleType(pars[1].ParameterType, rightResult)) {
							matches.Add(mi);
						}
					}
				}
			}
			if (matches.Count == 1) {
				MethodInfo method = MemberWrapper.GetWrapperFor((MethodInfo) matches[0]);
				OpNode_MethodWrapper newNode = new OpNode_MethodWrapper(parent, filename,
					line, column, origNode, method, new OpNode[] { left, right });
				return newNode;
			} else if (matches.Count > 1) {
				//List<MethodInfo> resolvedAmbiguities;
				//if (TryResolveAmbiguity(ambiguities, results, out resolvedAmbiguities)) {

				//}

				StringBuilder sb = new StringBuilder("Ambiguity detected when resolving operator. There were following possibilities:");
				foreach (MethodInfo mi in matches) {
					sb.Append(Environment.NewLine);
					sb.AppendFormat("{0} {1}.{2}", mi.ReturnType, mi.DeclaringType, mi);
				}
				throw new InterpreterException(sb.ToString(),
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName());
			}
			return null;
		}

		internal override string OrigString {
			get {
				return left.OrigString + LScript.GetString(origNode) + left.OrigString;
			}
		}

		public override string ToString() {
			StringBuilder str = new StringBuilder("(");
			str.Append(left.ToString());
			str.Append(" ").Append(LScript.GetString(origNode).Trim()).Append(" ");
			str.Append(right.ToString());
			str.Append(")");
			return str.ToString();
		}
	}
}