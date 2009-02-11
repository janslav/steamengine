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
	public class OpNode_Lazy_Indexer : OpNode, IOpNodeHolder {
		private OpNode index;
		internal OpNode arg;

		internal static OpNode Construct(IOpNodeHolder parent, Node code) {
			int line = code.GetStartLine() + LScript.startLine;
			int column = code.GetStartColumn();
			OpNode_Lazy_Indexer constructed = new OpNode_Lazy_Indexer(
				parent, LScript.GetParentScriptHolder(parent).filename, line, column, code);

			//LScript.DisplayTree(code);
			//skipped "["
			constructed.index = LScript.CompileNode(constructed, code.GetChildAt(1));
			//skipped "]"
			if (IsAssigner(code.GetChildAt(3))) {
				Node assigner = code.GetChildAt(3);
				constructed.arg = LScript.CompileNode(constructed, assigner.GetChildAt(1));
			}
			//LScript.DisplayTree(code);			
			return constructed;
		}

		internal static OpNode_Lazy_Indexer Construct(IOpNodeHolder parent, Node code, OpNode index, OpNode arg) {
			int line = code.GetStartLine() + LScript.startLine;
			int column = code.GetStartColumn();
			OpNode_Lazy_Indexer constructed = new OpNode_Lazy_Indexer(
				parent, LScript.GetParentScriptHolder(parent).filename, line, column, code);
			constructed.index = index;
			if (index != null) {
				index.parent = constructed;
			}
			constructed.arg = arg;
			if (arg != null) {
				arg.parent = constructed;
			}
			return constructed;
		}

		private OpNode_Lazy_Indexer(IOpNodeHolder parent, string filename, int line, int column, Node origNode)
			: base(parent, filename, line, column, origNode) {
		}

		public void Replace(OpNode oldNode, OpNode newNode) {
			if (index == oldNode) {
				index = newNode;
			} else if (arg == oldNode) {
				arg = newNode;
			} else {
				throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			}
		}

		internal override object Run(ScriptVars vars) {
			OpNode newNode = null; //return this one to my parent if something failed
			if (vars.self == null) {
				throw new InterpreterException("Attempted to index null...",
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName());
			}
			if (vars.self is ArrayList) {//arraylist is special :)
				if (arg != null) {
					newNode = new OpNode_ArrayListIndex(parent, filename, line, column,
						origNode, arg, index);
					arg.parent = (IOpNodeHolder) newNode;
					index.parent = (IOpNodeHolder) newNode;
					ReplaceSelf(newNode);
					newNode.Run(vars);
					return null;
				}
			}

			List<MethodInfo> matches = new List<MethodInfo>();
			PropertyInfo[] properties = vars.self.GetType().GetProperties();
			foreach (PropertyInfo pi in properties) {
				if (pi.Name == "Item") {
					if (arg == null) {//getting indexed item
						MethodInfo getter = pi.GetGetMethod();
						if (getter != null) {
							ParameterInfo[] pars = getter.GetParameters();
							if (pars.Length == 1) {
								matches.Add(getter);
							}
						}
					} else {
						MethodInfo setter = pi.GetSetMethod();
						if (setter != null) {
							ParameterInfo[] pars = setter.GetParameters();
							if (pars.Length == 2) {
								matches.Add(setter);
							}
						}
					}
				}
			}
			if (matches.Count == 0) {
				throw new InterpreterException("The type " + vars.self.GetType() + " is not indexable",
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName());
			} else {
				if (arg == null) {//getter
					object indexResult;
					object oSelf = vars.self;
					vars.self = vars.defaultObject;
					try {
						indexResult = index.Run(vars);
					} finally {
						vars.self = oSelf;
					}
					List<MethodInfo> exactMatches = new List<MethodInfo>();
					List<MethodInfo> stringMatches = new List<MethodInfo>();
					foreach (MethodInfo mi in matches) {
						Type desiredIndexType = mi.GetParameters()[0].ParameterType;
						if (MemberResolver.IsCompatibleType(desiredIndexType, indexResult)) {
							exactMatches.Add(mi);
						} else if (desiredIndexType == typeof(string)) {
							stringMatches.Add(mi);
						}
					}
					if (exactMatches.Count == 1) {
						MethodInfo method = MemberWrapper.GetWrapperFor((MethodInfo) exactMatches[0]);
						newNode = new OpNode_MethodWrapper(parent, filename, line, column, origNode,
							method, new OpNode[] { index });
						index.parent = (IOpNodeHolder) newNode;
						ReplaceSelf(newNode);
						return ((ITriable) newNode).TryRun(vars, new object[] { indexResult });
					} else if (exactMatches.Count == 0) {
						if (stringMatches.Count == 1) {
							OpNode_ToString toStringNode = new OpNode_ToString(parent, filename, line, column, origNode, index);
							index.parent = toStringNode;
							MethodInfo method = MemberWrapper.GetWrapperFor((MethodInfo) stringMatches[0]);
							newNode = new OpNode_MethodWrapper(parent, filename, line, column, origNode,
								method, new OpNode[] { index });
							ReplaceSelf(newNode);
							return ((ITriable) newNode).TryRun(vars, new object[] { string.Concat(indexResult) });
						} else {
							throw new InterpreterException("No suitable indexer found for type " + vars.self.GetType(),
								this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName());
						}
					} else {//exactMatches.Count >1
						StringBuilder sb = new StringBuilder("Ambiguity detected when resolving this indexer. There were following possibilities:");
						foreach (object obj in exactMatches) {
							sb.Append(Environment.NewLine).Append(obj.ToString());
						}
						throw new InterpreterException(sb.ToString(),
							this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName());
					}
				} else {
					object indexResult;
					object argResult;
					object oSelf = vars.self;
					vars.self = vars.defaultObject;
					try {
						indexResult = index.Run(vars);
						argResult = arg.Run(vars);
					} finally {
						vars.self = oSelf;
					}

					List<MethodInfo> exactMatches = new List<MethodInfo>();
					List<MethodInfo> argStringMatches = new List<MethodInfo>();
					List<MethodInfo> indexStringMatches = new List<MethodInfo>();
					List<MethodInfo> bothStringMatches = new List<MethodInfo>();

					foreach (MethodInfo mi in matches) {
						ParameterInfo[] pars = mi.GetParameters();
						Type desiredIndexType = pars[0].ParameterType;
						Type desiredArgType = pars[1].ParameterType;
						bool indexIsCompatible = MemberResolver.IsCompatibleType(desiredIndexType, indexResult);
						bool argIsCompatible = MemberResolver.IsCompatibleType(desiredArgType, argResult);

						if (indexIsCompatible && argIsCompatible) {
							exactMatches.Add(mi);
						} else if (indexIsCompatible && (desiredArgType == typeof(string))) {
							argStringMatches.Add(mi);
						} else if ((desiredIndexType == typeof(string)) && argIsCompatible) {
							indexStringMatches.Add(mi);
						} else if ((desiredIndexType == typeof(string)) && (desiredArgType == typeof(string))) {
							bothStringMatches.Add(mi);
						}
					}

					if (exactMatches.Count == 1) {
						MethodInfo method = MemberWrapper.GetWrapperFor((MethodInfo) exactMatches[0]);
						newNode = new OpNode_MethodWrapper(parent, filename, line, column, origNode,
							method, new OpNode[] { index, arg });
						index.parent = (IOpNodeHolder) newNode;
						arg.parent = (IOpNodeHolder) newNode;
						ReplaceSelf(newNode);
						return ((ITriable) newNode).TryRun(vars, new object[] { indexResult, argResult }); ;
					} else if (exactMatches.Count == 0) {
						if (argStringMatches.Count == 1) {
							OpNode_ToString toStringNode = new OpNode_ToString(parent, filename, line, column, origNode, arg);
							arg.parent = toStringNode;
							MethodInfo method = MemberWrapper.GetWrapperFor((MethodInfo) argStringMatches[0]);
							newNode = new OpNode_MethodWrapper(parent, filename, line, column, origNode,
								method, new OpNode[] { index, toStringNode });
							index.parent = (IOpNodeHolder) newNode;
							toStringNode.parent = (IOpNodeHolder) newNode;
							ReplaceSelf(newNode);
							return ((ITriable) newNode).TryRun(vars, new object[] { indexResult, string.Concat(argResult) }); ;
						} else if (indexStringMatches.Count == 1) {
							OpNode_ToString toStringNode = new OpNode_ToString(parent, filename, line, column, origNode, index);
							index.parent = toStringNode;
							MethodInfo method = MemberWrapper.GetWrapperFor((MethodInfo) indexStringMatches[0]);
							newNode = new OpNode_MethodWrapper(parent, filename, line, column, origNode,
								method, new OpNode[] { toStringNode, arg });
							toStringNode.parent = (IOpNodeHolder) newNode;
							arg.parent = (IOpNodeHolder) newNode;
							ReplaceSelf(newNode);
							return ((ITriable) newNode).TryRun(vars, new object[] { string.Concat(indexResult), argResult }); ;
						} else if (bothStringMatches.Count == 1) {
							OpNode_ToString indexToStringNode = new OpNode_ToString(parent, filename, line, column, origNode, index);
							index.parent = indexToStringNode;
							OpNode_ToString argToStringNode = new OpNode_ToString(parent, filename, line, column, origNode, arg);
							arg.parent = argToStringNode;
							MethodInfo method = MemberWrapper.GetWrapperFor((MethodInfo) indexStringMatches[0]);
							newNode = new OpNode_MethodWrapper(parent, filename, line, column, origNode,
								method, new OpNode[] { indexToStringNode, argToStringNode });
							indexToStringNode.parent = (IOpNodeHolder) newNode;
							argToStringNode.parent = (IOpNodeHolder) newNode;
							ReplaceSelf(newNode);
							return ((ITriable) newNode).TryRun(vars, new object[] { string.Concat(indexResult), string.Concat(argResult) });
						} else {
							throw new InterpreterException("No suitable indexer found for type " + vars.self.GetType(),
								this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName());
						}
					} else { //exactMatches.Count >1

						//List<MethodInfo> resolvedAmbiguities;
						//if (TryResolveAmbiguity(ambiguities, results, out resolvedAmbiguities)) {

						//}

						StringBuilder sb = new StringBuilder("Ambiguity detected when resolving this indexer. There were following possibilities:");
						foreach (object obj in exactMatches) {
							sb.Append(Environment.NewLine).Append(obj.ToString());
						}
						throw new InterpreterException(sb.ToString(),
							this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName());
					}
				}
			}
		}

		public override string ToString() {
			if (arg != null) {
				return string.Concat("INDEX(", index, ") = ", arg);
			} else {
				return ("INDEX(" + index + ")");
			}
		}
	}
}