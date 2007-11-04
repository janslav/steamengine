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
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.LScript {
	
	public class OpNode_Lazy_Expression : OpNode, IOpNodeHolder {
		//accepts STRING, SimpleExpression, 
		private string name;
		protected OpNode[] args;
		private string formatString;
		private object[] results = null;
		private bool mustEval = false;
		private bool noArgs = false;
		private string classOrNamespaceName = "";
		private bool isClass;
		
		internal static OpNode Construct(IOpNodeHolder parent, Node code) {
			return Construct(parent, code, false);
		}
		
		internal static OpNode Construct(IOpNodeHolder parent, Node code, bool mustEval) {
			int line = code.GetStartLine()+LScript.startLine;
			int column = code.GetStartColumn();
			string filename = LScript.GetParentScriptHolder(parent).filename;
			OpNode_Lazy_Expression constructed = new OpNode_Lazy_Expression(
				parent, filename, line, column, code);
			
			constructed.mustEval = mustEval;
			//LScript.DisplayTree(code);
			
			if (IsType(code, StrictConstants.STRING)) {
				string identifier = ((Token) code).GetImage();
				Commands.AuthorizeCommandThrow(Globals.Src, identifier);
//some "keywords"
				if (string.Compare(identifier, "args", true) == 0) { //true for case insensitive
/*args*/			return new OpNode_GetArgs(parent, filename, line, column, code);
				} else if (string.Compare(identifier, "this", true) == 0) { //true for case insensitive
/*this*/			return new OpNode_This(parent, filename, line, column, code);
				} else if (string.Compare(identifier, "argvcount", true) == 0) { //true for case insensitive
/*argvcount*/		return new OpNode_ArgvCount(parent, filename, line, column, code);
				} else if (string.Compare(identifier, "true", true) == 0) { //true for case insensitive
/*true*/			return OpNode_Object.Construct(parent, true);
				} else if (string.Compare(identifier, "false", true) == 0) { //true for case insensitive
/*false*/			return OpNode_Object.Construct(parent, false);
				} else if (string.Compare(identifier, "null", true) == 0) { //true for case insensitive
/*null*/			return OpNode_Object.Construct(parent, (object) null);
				}
				
				
				//Console.WriteLine("identifier: "+identifier+", token "+code);
				constructed.name = identifier;
				constructed.args = new OpNode[0];
				constructed.noArgs = true;
				return constructed;
			} else if (IsType(code, StrictConstants.SIMPLE_EXPRESSION)) {
				constructed.name = LScript.GetFirstTokenString(code);
				Commands.AuthorizeCommandThrow(Globals.Src, constructed.name);
				int current = 1;
				Node caller = code.GetChildAt(current);
				if (IsType(caller, StrictConstants.CALLER)) {
					constructed.GetArgsFrom(caller);
					current++;
				}

				ArrayList indexersList = new ArrayList();
				while (IsType(code.GetChildAt(current), StrictConstants.INDEXER)) {
					Production indexer =  (Production)code.GetChildAt(current);
					//"identifier[...] = ..." - then the assignment "belongs" to the indexer
					if (IsAssigner(code.GetChildAt(current+1))) {
						indexer.AddChild(((Production) code).PopChildAt(current+1));
						//this is last one, because assigner being next means there are no nodes anymore
					}
					indexersList.Add(OpNode_Lazy_Indexer.Construct(constructed, indexer));
					current++;
				}
				
				Node assigner = code.GetChildAt(current);
				if (IsAssigner(assigner)) {
					if (constructed.args == null) {
						constructed.GetArgsFrom(assigner);
					} else {
						throw new InterpreterException("This expression is invalid : 'identifier(...) = ... '", 
							LScript.startLine+assigner.GetStartLine(), assigner.GetStartColumn(),
							filename, LScript.GetParentScriptHolder(parent).GetDecoratedName());
						//this could also (maybe?) be a parser error, but I wasn`t yet able to make the grammar that way :)
						//for the scripter is it irrelevant - it`s anyway an error at "compile" time.
					}
				} else if (constructed.args == null) {
					constructed.args = new OpNode[0];
				}
				if (indexersList.Count>0) {
					OpNode[] chain = new OpNode[indexersList.Count+1];
					chain[0] = constructed;
					for (int i = 0, n = indexersList.Count; i<n; i++) {
						chain[i+1] = (OpNode) indexersList[i];
					}
					return OpNode_Lazy_ExpressionChain.ConstructFromArray(parent, code, chain);
				}
				return constructed;
			} else {
				throw new Exception("Passed bad Node type construct. This should not happen.");
			}
		}
		
		protected void GetArgsFrom(Node caller) {
			//caller / assigner
			Node arg = caller.GetChildAt(1); //ArgsList or just one expression
			//skipped the caller`s first token - "(" / "=" / " "
			if (IsType(arg, StrictConstants.RIGHT_PAREN)) {
				args = new OpNode[0];
				return;
			}
			if (IsType(arg, StrictConstants.ARGS_LIST)) {
				ArrayList argsList = new ArrayList();
				//ArrayList stringList = new ArrayList();
				StringBuilder sb = new StringBuilder();
				for (int i = 0, n = arg.GetChildCount(); i<n; i+=2) { //step 2 - skipping argsseparators
					sb.Append("{"+i/2+"}");
					if (i+1 < n) {
						Node separator = arg.GetChildAt(i+1);
						sb.Append(LScript.GetString(separator));
					}
					Node node = arg.GetChildAt(i);
					argsList.Add(LScript.CompileNode(this, node));
				}
				args = new OpNode[argsList.Count];
				argsList.CopyTo(args);
				
				object[] stringTokens = new object[arg.GetChildCount()];
				((Production)arg).children.CopyTo(stringTokens);
				formatString = sb.ToString();
			} else {
				OpNode compiled = LScript.CompileNode(this, arg);
				args = new OpNode[] {compiled};
				formatString = "{0}";
			}
		}
		
		protected OpNode_Lazy_Expression(IOpNodeHolder parent, string filename, int line, int column, Node origNode)
			: base(parent, filename, line, column, origNode) {
		}
		
		public virtual void Replace(OpNode oldNode, OpNode newNode) {
			int index = Array.IndexOf(args, oldNode);
			if (index < 0) {
				throw new Exception("Nothing to replace the node "+oldNode+" at "+this+"  with. This should not happen.");
			} else {
				args[index] = newNode;
			}
		}
		
		internal override object Run(ScriptVars vars) {
			//Console.WriteLine("OpNode_Lazy_Expression.Run on "+vars.self);
			//Console.WriteLine("Running as lazyexpression: "+LScript.GetString(origNode));
			
			bool memberNameMatched = false;
			bool skillKeyMatched = false;

			bool tryInstance = true; //are the members only static or also instance
			bool haveBaseInstance = true; //is here some base instance/type?
			Type seType = null;
			
			OpNode finalOpNode;
			MemberResolver resolver = null;

			Type type =	null;
			if (vars.self == null) {
				haveBaseInstance = false;
			}
			
			//ArrayList matches = new ArrayList(1); //list of possible OpNodes. only the first one is really used.
			
//return
			if (string.Compare(name, "return", true) == 0) { //true for case insensitive
				OpNode newNode;
				if (args.Length == 1) {
					newNode = new OpNode_Return(parent, filename, line, column, origNode, args[0]);
					SetNewParentToArgs((IOpNodeHolder) newNode);
				} else if (args.Length > 1) {
					newNode = new OpNode_Return_String(parent, filename, line, column, origNode, args, formatString);
					SetNewParentToArgs((IOpNodeHolder) newNode);
				} else { //args.Length == 0
					//Console.WriteLine("OpNode_Object.Construct from return");
					OpNode nullNode = OpNode_Object.Construct(null, (object) null);
					newNode = new OpNode_Return(parent, filename, line, column, origNode, nullNode);
				}
				ReplaceSelf(newNode);

				object oSelf = vars.self;
				vars.self = vars.defaultObject;
				try {
					return newNode.Run(vars);
				} finally {
					vars.self = oSelf;
				}				
			}
			
//scripts
			AbstractScript ad = AbstractScript.Get(name);
			if (ad != null) {
				finalOpNode = OpNode_Object.Construct(parent, ad);
				goto runit;
			}

//self is namespace
			if (vars.self is NameRef) {
				classOrNamespaceName += ((NameRef) vars.self).name;
				if (vars.self is NameSpaceRef) {
					isClass = false;
				} else { //vars.self is ClassNameRef
					isClass = true;
				}
			}

			if (haveBaseInstance) {
				if (classOrNamespaceName != "") {
					//noArgs
					if (!isClass) {
						string className = classOrNamespaceName+"."+name;
						type = Type.GetType(className, false, true);
						if (type == null) {
							throw new NameRefException(this.line, this.column, this.filename, 
								new NameSpaceRef(className), ParentScriptHolder.GetDecoratedName());
							//in fact we dunno if it`s a _valid_ namespace name, but there seems to be no way to confirm that
						} else {
							if (noArgs) {//it`s just reference to class, so that the next in chain is a static member, or is it wrong
								throw new NameRefException(this.line, this.column, 
									this.filename, new ClassNameRef(className), ParentScriptHolder.GetDecoratedName());
							} else {//it`s a constructor/member with same name
								tryInstance = false;
								//Console.WriteLine("tryInstance = false: {0} ({1}) at {2}:{3}", type, className, this.line, this.column);
							}
						}
					} else {
						ClassNameRef classRef = (ClassNameRef) vars.self;
						type = Type.GetType(classRef.name, false, true);
						if (type == null) {
							type = SteamEngine.CompiledScripts.ClassManager.GetType(classRef.name);
						}
						if (type == null) {
							throw new Exception("We have not found class named "+classRef.name+", thought we were supposed to. This should not happen.");
						}
						//Console.WriteLine("Type.GetType: {0} ({1}) at {2}:{3}", type, classRef.name, this.line, this.column);
						tryInstance = false;//we look for a static member
					}
				} else if (vars.self == vars.defaultObject) {
					seType = SteamEngine.CompiledScripts.ClassManager.GetType(name);//some SteamEngine class from our ClassManager class
					type = vars.self.GetType();
				} else {
					type = vars.self.GetType();
				}
			}
//I am namespace name ("system")
			if (string.Compare(name, "system", true) == 0) { //true for case insensitive
				throw new NameRefException(this.line, this.column, this.filename, 
					new NameSpaceRef(name), ParentScriptHolder.GetDecoratedName());
				//this exception being thrown doesnt really mean something is wrong. 
				//It may be also harmlessly caught by superior ExpressionChain. but if there is no such present, it becomes really an error
			}
//arg/local
			if (args.Length == 0) {
				if (ParentScriptHolder.registerNames.ContainsKey(name)) {
					finalOpNode = new OpNode_GetArg(parent, filename, line, column, origNode, name);
					goto runit;
				}
			}


			resolver = MemberResolver.GetInstance(
				vars, parent, name, args, line, column, filename);
			MemberDescriptor desc = null;
//methods/properties /fields/constructors

			//Console.WriteLine("resolving "+name);
			if (haveBaseInstance) {
				if ((vars.defaultObject is Thing) && 
						((String.Compare(name, "item", true) == 0) ||
						(String.Compare(name, "itemnewbie", true) == 0) ||
						(String.Compare(name, "sell", true) == 0) ||
						(String.Compare(name, "buy", true) == 0))) {
					int argsLength = args.Length;
					if ((argsLength == 1) || (argsLength == 2)) {
						resolver.RunArgs();
						if ((resolver.results[0] is IThingFactory) && ((argsLength == 1) || 
								((argsLength == 2) && (TagMath.IsNumberType(resolver.results[1].GetType()))))) {
							OpNode amountNode;
							if (argsLength == 1) {
								amountNode = OpNode_Object.Construct(this, (uint) 1);
							} else {
								amountNode = args[1];
							}
							bool newbie = (String.Compare(name, "itemnewbie", true) == 0);
							finalOpNode = new OpNode_TemplateItem(parent, filename, line, column, origNode,
								newbie, args[0], amountNode);
							goto runit;
						}
					}
				}
				MemberTypes typesToResolve = (tryInstance ? MemberTypes.All : MemberTypes.Constructor);
				memberNameMatched |= resolver.Resolve(type, BindingFlags.Instance, typesToResolve, out desc);
				if (!ResolveAsClassMember(desc, out finalOpNode)) {//in other words, if desc was null
					memberNameMatched |= resolver.Resolve(type, BindingFlags.Static, MemberTypes.All, out desc);
					if (ResolveAsClassMember(desc, out finalOpNode)) {
						goto runit;
					}
				} else {
					goto runit;
				}
			}
			if (seType != null) {//try to resolve as constructor
				memberNameMatched |= resolver.Resolve(seType, BindingFlags.Static, MemberTypes.Constructor, out desc);
				if (ResolveAsClassMember(desc, out finalOpNode)) {
					goto runit;
				}

				throw new NameRefException(this.line, this.column, this.filename, 
					new ClassNameRef(name), ParentScriptHolder.GetDecoratedName());
			}

			memberNameMatched |= resolver.Resolve(typeof(IntrinsicMethods), BindingFlags.Static, MemberTypes.Method|MemberTypes.Property, out desc);
			if (ResolveAsClassMember(desc, out finalOpNode)) {
				goto runit;
			}

//var
			if (args.Length == 0) {
				if (Globals.instance.HasTag(TagKey.Get(name))) {
					finalOpNode = new OpNode_GetVar(parent, filename, line, column, origNode, name);
					goto runit;
				}
			}
//function
			ScriptHolder function = ScriptHolder.GetFunction(name);
			if (function != null) {
				finalOpNode = new OpNode_Function(parent, filename, line, column, origNode, 
					function, args, formatString);
				goto runit;
			}
					
//skillkey
			AbstractSkillDef sd = AbstractSkillDef.ByKey(name);
			if ((sd != null) && (vars.self is AbstractCharacter)) {
				skillKeyMatched = true;
				if (args.Length == 0) {
					finalOpNode = new OpNode_SkillKey_Get(parent, filename, line, column, origNode, 
						sd.Id);
					goto runit;
				} else if (args.Length == 1) {
					resolver.RunArgs();
					try {
						Convert.ToUInt16(resolver.results[0]);
						finalOpNode = new OpNode_SkillKey_Set(parent, filename, line, column, origNode, 
							sd.Id, args[0]);
						goto runit;
					} catch (Exception) {//cos of the convert
					}
				}
			}

			if (args.Length == 0) {
//constant (defnames)
				Constant con = Constant.Get(name);
				if (con != null) {
					finalOpNode = new OpNode_Constant(parent, filename, line, column, origNode, con);
					goto runit;
				}

//regions
				StaticRegion reg = StaticRegion.GetByDefname(name);
				if (reg != null) {
					finalOpNode = OpNode_Object.Construct(parent, reg);
					goto runit;
				}
			}

//a little hack for gumps - to make possible to use dialog layout methods without the "argo."
			if ((vars.self == vars.defaultObject)&&(vars.scriptArgs != null)&&(vars.scriptArgs.argv.Length > 0)) {
				ScriptedGumpInstance sgi = vars.scriptArgs.argv[0] as ScriptedGumpInstance;
				if (sgi != null) {
					if (ScriptedGumpInstance.IsMethodName(name)) {
						desc = null;
						memberNameMatched = resolver.Resolve(typeof(ScriptedGumpInstance), BindingFlags.Instance, MemberTypes.Method, out desc);
						ResolveAsClassMember(desc, out finalOpNode);
						if (finalOpNode != null) {
							OpNode_MethodWrapper onmw = (OpNode_MethodWrapper) finalOpNode;
							OpNode_RunOnArgo newNode = new OpNode_RunOnArgo(parent, filename, line, column, origNode, onmw);
							onmw.parent = newNode;
							finalOpNode = newNode;
							goto runit;
						}
					}
				}
			}
			
			

//whateva else?


//check matches:

//			if (matches.Count > 1) {
//				StringBuilder sb = new StringBuilder
//					("Ambiguity detected when resolving expression. From the following possibilities, "
//					+"the first one has been chosen as the one with highest priority. Try to write your code more descriptive.");
//				foreach (OpNode opNode in matches) {
//					sb.Append(Environment.NewLine);
//					sb.Append(opNode.ToString());
//				}
//				Logger.WriteWarning(filename, line, sb.ToString());
//			} else if (matches.Count < 1) {
			if (mustEval) {
				if (memberNameMatched) {
					throw new InterpreterException(
						"Class member (method/property/field/constructor) '"+LogStr.Ident(name)+"' is getting wrong arguments", 
						this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName());
				} else if (skillKeyMatched) {
					throw new InterpreterException(
						"The skill is getting wrong number or type of arguments", 
						this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName());
				} else {
					throw new InterpreterException("Undefined identifier '"+LogStr.Ident(name)+"'", 
						this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName());
				}
			} else {
				string thisNodeString = this.OrigString;
				//Console.WriteLine("OpNode_Object.Construct from !mustEval");
				OpNode finalStringOpNode = OpNode_Object.Construct(parent, thisNodeString);
				ReplaceSelf(finalStringOpNode);
				return thisNodeString;
			}
//			}

			
			
runit:	//I know that goto is usually considered dirty, but I find this case quite suitable for it...
			if (resolver != null) {
				results = resolver.results;
				MemberResolver.ReturnInstance(resolver);
			}
//finally run it
			IOpNodeHolder finalAsHolder = finalOpNode as IOpNodeHolder;
			if (finalAsHolder != null) {
				SetNewParentToArgs(finalAsHolder);
			}
			ReplaceSelf(finalOpNode);
			if ((results != null) && (results.Length > 0)) {
				return ((ITriable) finalOpNode).TryRun(vars, results);
			} else {
				return finalOpNode.Run(vars);
			}
		}

		private bool ResolveAsClassMember(MemberDescriptor desc, out OpNode finalOpNode) {
			//Console.WriteLine("ResolveAsClassMember: "+desc);
			finalOpNode = null;
			if (desc == null) {
				return false;
			}
			MemberInfo info = desc.info;
			MethodInfo methodInfo;
			ConstructorInfo constructorInfo;
			FieldInfo fieldInfo;
			
			switch (desc.specialType) {
				case SpecialType.Normal:
					if (MemberResolver.IsMethod(info)) {
						methodInfo = MemberWrapper.GetWrapperFor((MethodInfo) info);
						finalOpNode = new OpNode_MethodWrapper(parent, filename, line, column, 
							origNode, methodInfo, args);
					} else if (MemberResolver.IsConstructor(info)) {
						constructorInfo = MemberWrapper.GetWrapperFor((ConstructorInfo) info);
						finalOpNode = new OpNode_ConstructorWrapper(parent, filename, line, column, 
							origNode, constructorInfo, args);
					} else if (MemberResolver.IsField(info)) {
						fieldInfo = (FieldInfo) info;
						if ((fieldInfo.Attributes & FieldAttributes.Literal) == FieldAttributes.Literal) {
							object retVal = fieldInfo.GetValue(null);
							finalOpNode = OpNode_Object.Construct(parent, retVal);
						} else {
							fieldInfo = MemberWrapper.GetWrapperFor((FieldInfo) info);
							if (args.Length == 0) {
								finalOpNode = new OpNode_GetField(parent, filename, line, column,
									origNode, fieldInfo);
							} else {
								finalOpNode = new OpNode_SetField(parent, filename, line, column,
									origNode, fieldInfo, args[0]);
							}
						}
					}
					break;
				case SpecialType.String:
					if ((args.Length == 1)&&(MemberResolver.ReturnsString(args[0]))) {
						goto case SpecialType.Normal; //is it not nice? :)
					}
					if (MemberResolver.IsMethod(info)) {
						methodInfo = MemberWrapper.GetWrapperFor((MethodInfo) info);
						finalOpNode = new OpNode_MethodWrapper_String(parent, filename, line, column, 
							origNode, methodInfo, args, formatString);
					} else if (MemberResolver.IsConstructor(info)) {
						constructorInfo = MemberWrapper.GetWrapperFor((ConstructorInfo) info);
						finalOpNode = new OpNode_ConstructorWrapper_String(parent, filename, line, column, 
							origNode, constructorInfo, args, formatString);
					} else if (MemberResolver.IsField(info)) {
						fieldInfo = MemberWrapper.GetWrapperFor((FieldInfo) info);
						finalOpNode = new OpNode_InitField_String(parent, filename, line, column, 
							origNode, fieldInfo, args, formatString);
					}
					break;
				case SpecialType.Params:
					MethodBase methOrCtor = (MethodBase) info;
					ParameterInfo[] pars = methOrCtor.GetParameters();//these are guaranteed to have the right number of arguments...
					int methodParamLength = pars.Length;
					Type paramsElementType = pars[methodParamLength-1].ParameterType.GetElementType();
					int normalArgsLength = methodParamLength-1;
					int paramArgsLength = args.Length - normalArgsLength;
					OpNode[] normalArgs = new OpNode[normalArgsLength];
					OpNode[] paramArgs = new OpNode[paramArgsLength];
					Array.Copy(args, normalArgs, normalArgsLength);
					Array.Copy(args, normalArgsLength, paramArgs, 0, paramArgsLength);
				
					if (MemberResolver.IsMethod(info)) {
						methodInfo = MemberWrapper.GetWrapperFor((MethodInfo) info);
						finalOpNode = new OpNode_MethodWrapper_Params(parent, filename, line, column, 
							origNode, methodInfo, normalArgs, paramArgs, paramsElementType);
					} else if (MemberResolver.IsConstructor(info)) {
						constructorInfo = MemberWrapper.GetWrapperFor((ConstructorInfo) info);
						finalOpNode = new OpNode_ConstructorWrapper_Params(parent, filename, line, column, 
							origNode, constructorInfo, normalArgs, paramArgs, paramsElementType);
					}

					break;
			}
			return true;
		}
		
		private void SetNewParentToArgs(IOpNodeHolder newParent) {
			for (int i = 0, n = args.Length; i<n; i++) {
				OpNode node = args[i];
				//Console.WriteLine("Setting new parent {0} (type {1}) to {2} ({3})", newParent, newParent.GetType(), node, node.GetType());
				node.parent = newParent;
			}
			//str.parent = newParent;
		}
				
		public override string ToString() {
			StringBuilder str = new StringBuilder(name).Append("(");
			for (int i = 0, n = args.Length; i<n; i++) {
				str.Append(args[i].ToString()).Append(", ");
			}
			return str.Append(")").ToString();
		}
	}
}	