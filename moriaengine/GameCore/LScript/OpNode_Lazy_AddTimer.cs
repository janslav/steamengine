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
using System.Text;
using System.Collections;
using System.Reflection;
using System.Globalization;
using PerCederberg.Grammatica.Parser;
using SteamEngine.Timers;
using SteamEngine.Common;

namespace SteamEngine.LScript {
	public class OpNode_Lazy_AddTimer : OpNode, IOpNodeHolder {
		//accepts AddTimerExpression
		//STRING , SimpleCode , (STRING | TRIGGERNAME) [, ArgsList];  

		protected TimerKey name;
		protected OpNode secondsNode;
		protected OpNode[] args;// = new OpNode[0]; //
		protected OpNode str; //as if it was a quoted string
		protected string funcName;
		protected string formatString;
		private object[] results = null;

		internal static OpNode Construct(IOpNodeHolder parent, Node code) {
			Commands.AuthorizeCommandThrow(Globals.Src, "addtimer");

			int line = code.GetStartLine() + LScript.startLine;
			int column = code.GetStartColumn();
			string filename = LScript.GetParentScriptHolder(parent).filename;
			OpNode_Lazy_AddTimer constructed;

			Production body = (Production) code.GetChildAt(2);
			//LScript.DisplayTree(body);
			Node triggerOrName = body.GetChildAt(4);
			if (IsType(triggerOrName, StrictConstants.STRING)) {
				constructed = new OpNode_Lazy_AddTimer(parent, filename, line, column, code);
				constructed.funcName = ((Token) triggerOrName).GetImage();
				Commands.AuthorizeCommandThrow(Globals.Src, constructed.funcName);
			} else {//it is Triggerkey
				string tkName = ((Token) triggerOrName.GetChildAt(triggerOrName.GetChildCount() - 1)).GetImage();
				TriggerKey tk = TriggerKey.Get(tkName);
				constructed = new OpNode_AddTriggerTimer(parent, filename, line, column, code, tk);
			}//

			Node timerKeyNode = body.GetChildAt(0);
			constructed.name = TimerKey.Get(((Token) timerKeyNode.GetChildAt(timerKeyNode.GetChildCount() - 1)).GetImage());
			constructed.secondsNode = LScript.CompileNode(constructed, body.GetChildAt(2));
			if (body.GetChildCount() > 5) {
				constructed.GetArgsFrom(body.GetChildAt(6));
			} else {
				constructed.args = new OpNode[0];
				constructed.str = OpNode_Object.Construct(constructed, "");
				constructed.formatString = "";
			}
			return constructed;
		}

		protected OpNode_Lazy_AddTimer(IOpNodeHolder parent, string filename, int line, int column, Node origNode)
			: base(parent, filename, line, column, origNode) {
		}

		internal override object Run(ScriptVars vars) {
			if (vars.self is PluginHolder) {
				bool memberNameMatched = false;

				OpNode finalOpNode;
				Type type = vars.self.GetType();

				MemberResolver resolver = MemberResolver.GetInstance(
					vars, parent, funcName, args, line, column, filename);
				MemberDescriptor desc = null;

				//as Method
				memberNameMatched &= resolver.Resolve(type, BindingFlags.Instance, MemberTypes.Method, out desc);
				if (!ResolveAsMethod(desc, out finalOpNode)) {//in other words, if desc was null
					memberNameMatched &= resolver.Resolve(type, BindingFlags.Static, MemberTypes.Method, out desc);
					if (ResolveAsMethod(desc, out finalOpNode)) {
						goto runit;
					}
				} else {
					goto runit;
				}
				//as function
				ScriptHolder function = ScriptHolder.GetFunction(funcName);//no, this is not unreachable, whatever the mono compjiler says...
				if (function != null) {
					finalOpNode = new OpNode_AddFunctionTimer(parent, filename, line, column, origNode,
						name, function, formatString, secondsNode, args);
					goto runit;
				}

				//as intrinsic method
				//intrinsic methods really as last... they are not too usual to be run delayed
				memberNameMatched &= resolver.Resolve(typeof(IntrinsicMethods), BindingFlags.Static, MemberTypes.Method | MemberTypes.Property, out desc);
				if (ResolveAsMethod(desc, out finalOpNode)) {
					goto runit;
				}


				//if (matches.Count > 1) {
				//	StringBuilder sb = new StringBuilder
				//		("Ambiguity detected when resolving expression. From the following possibilities, "
				//		+"the first one has been chosen as the one with highest priority. Try to write your code more descriptive.");
				//	foreach (OpNode opNode in matches) {
				//		sb.Append(Environment.NewLine);
				//		sb.Append(opNode.ToString());
				//	}
				//	Logger.WriteWarning(filename, line, sb.ToString());
				//} else if (matches.Count < 1) {
				if (memberNameMatched) {
					throw new InterpreterException("Method '" + LogStr.Ident(funcName) + "' is getting bad arguments",
						this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName());
				} else {
					throw new InterpreterException("Undefined identifier '" + LogStr.Ident(funcName) + "'",
						this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName());
				}
			//}

runit:	//I know that goto is usually considered dirty, but I find this case quite suitable for it...
				if (resolver != null) {
					MemberResolver.ReturnInstance(resolver);
				}

				IOpNodeHolder finalAsHolder = finalOpNode as IOpNodeHolder;
				if (finalAsHolder != null) {
					SetNewParentToArgs(finalAsHolder);
				}
				ReplaceSelf(finalOpNode);
				if (results != null) {
					return ((ITriable) finalOpNode).TryRun(vars, results);
				} else {
					return finalOpNode.Run(vars);
				}
			} else {
				throw new InterpreterException("AddTimer must be called on PluginHolder, not " + vars.self,
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName());
			}
		}

		private void SetNewParentToArgs(IOpNodeHolder newParent) {
			//Console.WriteLine("setting new parent "+newParent+"(type "+newParent.GetType()+") to arguments of "+this+"("+this.GetType()+")");
			for (int i = 0, n = args.Length; i < n; i++) {
				args[i].parent = newParent;
			}
			str.parent = newParent;
			secondsNode.parent = newParent;
		}

		private bool ResolveAsMethod(MemberDescriptor desc, out OpNode finalOpNode) {
			finalOpNode = null;
			if (desc == null) {
				return false;
			}
			MethodInfo MethodInfo = MemberWrapper.GetWrapperFor((MethodInfo) desc.info);

			switch (desc.specialType) {
				case SpecialType.Normal:
					finalOpNode = new OpNode_AddMethodTimer(parent, filename, line, column,
						origNode, name, MethodInfo, secondsNode, args);
					break;
				case SpecialType.String:
					if ((args.Length == 1) && (MemberResolver.ReturnsString(args[0]))) {
						goto case SpecialType.Normal; //is it not nice? :)
					}
					finalOpNode = new OpNode_AddMethodTimer_String(parent, filename, line, column,
						origNode, name, MethodInfo, secondsNode, args, formatString);
					break;
				case SpecialType.Params:
					ParameterInfo[] pars = MethodInfo.GetParameters();//these are guaranteed to have the right number of arguments...
					int methodParamLength = pars.Length;
					Type paramsElementType = pars[methodParamLength - 1].ParameterType.GetElementType();
					int normalArgsLength = methodParamLength - 1;
					int paramArgsLength = args.Length - normalArgsLength;
					OpNode[] normalArgs = new OpNode[normalArgsLength];
					OpNode[] paramArgs = new OpNode[paramArgsLength];
					Array.Copy(args, normalArgs, normalArgsLength);
					Array.Copy(args, normalArgsLength, paramArgs, 0, paramArgsLength);

					finalOpNode = new OpNode_AddMethodTimer_Params(parent, filename, line, column,
						origNode, name, MethodInfo, secondsNode, normalArgs, paramArgs, paramsElementType);
					break;
			}
			return true;
		}

		private void GetArgsFrom(Node arg) {
			//caller / assigner
			if (IsType(arg, StrictConstants.ARGS_LIST)) {
				ArrayList argsList = new ArrayList();
				//ArrayList stringList = new ArrayList();
				StringBuilder sb = new StringBuilder();
				for (int i = 0, n = arg.GetChildCount(); i < n; i += 2) { //step 2 - skipping argsseparators
					sb.Append("{" + i / 2 + "}");
					if (i + 1 < n) {
						Node separator = arg.GetChildAt(i + 1);
						sb.Append(LScript.GetString(separator));
					}
					Node node = arg.GetChildAt(i);
					argsList.Add(LScript.CompileNode(this, node));
				}
				args = new OpNode[argsList.Count];
				argsList.CopyTo(args);

				object[] stringTokens = new object[arg.GetChildCount()];
				((Production) arg).children.CopyTo(stringTokens);
				str = OpNode_Lazy_QuotedString.ConstructFromArray(
					this, arg, stringTokens);
				formatString = sb.ToString();
			} else {
				OpNode compiled = LScript.CompileNode(this, arg);
				args = new OpNode[] { compiled };
				str = OpNode_ToString.Construct(this, arg);
				formatString = "{0}";
			}
		}

		public virtual void Replace(OpNode oldNode, OpNode newNode) {
			int index = Array.IndexOf(args, oldNode);
			if (index >= 0) {
				args[index] = newNode;
			} else if (secondsNode == oldNode) {
				secondsNode = newNode;
			} else if (str == oldNode) {
				str = newNode;
			} else {
				throw new Exception("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			}
		}

		//private void RunArgs(ScriptVars vars) {
		//	if (results == null) {
		//		object oSelf = vars.self;
		//		results = new object[args.Length];
		//		vars.self = vars.defaultObject;
		//		try {
		//			for (int i = 0, n = args.Length; i<n; i++) {
		//				results[i] = args[i].Run(vars);
		//			}
		//		} finally {
		//			vars.self = oSelf;
		//		}
		//	}
		//}

		public override string ToString() {
			StringBuilder sb = new StringBuilder("AddTimer(");
			sb.Append("(").Append(name.name).Append(", ").Append(secondsNode.ToString());
			sb.Append(funcName).Append(", ");
			int n = args.Length;
			if (n > 0) {
				sb.Append(", ");
				for (int i = 0; i < n; i++) {
					sb.Append(args[i].ToString()).Append(", ");
				}
			}
			return sb.Append(")").ToString();
		}
	}
}