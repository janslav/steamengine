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
using System.Reflection;
using System.CodeDom;
using System.CodeDom.Compiler;
using SteamEngine.Common;
using SteamEngine.CompiledScripts;

namespace SteamEngine.CompiledScripts { 

	/*
		Class: CompiledTriggerGroup
			.NET scripts should extend this class, and make use of its features.
			This class provides automatic linking of methods intended for use as triggers and
			creates a TriggerGroup that your triggers are in.
	*/
	public abstract class CompiledTriggerGroup : TriggerGroup {
		protected CompiledTriggerGroup()
			: base() {
		}

		public override object Run(object self, TriggerKey tk, ScriptArgs sa) {
			throw new InvalidOperationException("CompiledTriggerGroup without overriden Run method?! This should not happen.");
		}

		public override sealed object TryRun(object self, TriggerKey tk, ScriptArgs sa) {
			try {
				return Run(self, tk, sa);
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				Logger.WriteError(e);
			}
			return null;
		}

		protected override string GetName() {
			return this.GetType().Name;
		}

		public override void Unload() {
			//we do nothing. Throwing exception is rude to AbstractScript.UnloadAll
			//and doing base.Unload() would be a lie cos we can't really unload.
		}
	}

	internal class CompiledTriggerGroupGenerator : ISteamCSCodeGenerator {
		static List<Type> compiledTGs = new List<Type>();

		internal static void AddCompiledTGType(Type t) {
			compiledTGs.Add(t);
		}

		public CodeCompileUnit WriteSources() {
			try {
				CodeCompileUnit codeCompileUnit = new CodeCompileUnit();
				if (compiledTGs.Count > 0) {
					Logger.WriteDebug("Generating compiled Triggergroups");

					CodeNamespace ns = new CodeNamespace("SteamEngine.CompiledScripts");
					codeCompileUnit.Namespaces.Add(ns);

					foreach (Type decoratedClass in compiledTGs) {
						try {
							GeneratedInstance gi = new GeneratedInstance(decoratedClass);
							CodeTypeDeclaration ctd = gi.GetGeneratedType();
							ns.Types.Add(ctd);
						} catch (FatalException) {
							throw;
						} catch (Exception e) {
							Logger.WriteError(decoratedClass.Assembly.GetName().Name, decoratedClass.Name, e);
							return null;
						}
					}
					Logger.WriteDebug("Done generating "+compiledTGs.Count+" compiled Triggergroups");
				}
				return codeCompileUnit;
			} finally {
				compiledTGs.Clear();
			}
		}

		public void HandleAssembly(Assembly compiledAssembly) {

		}


		public string FileName {
			get { return "CompiledTriggerGroups.Generated.cs"; }
		}

		/*
			Constructor: CompiledTriggerGroup
			Creates a triggerGroup named after the class, and then finds and sets up triggers 
			defined in the script (by naming them on_whatever).
		*/
		private class GeneratedInstance {
			List<MethodInfo> triggerMethods = new List<MethodInfo>();
			Type tgType;

			internal CodeTypeDeclaration GetGeneratedType() {
				CodeTypeDeclaration codeTypeDeclatarion = new CodeTypeDeclaration(tgType.Name+"_Generated");
				codeTypeDeclatarion.TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed;
				codeTypeDeclatarion.BaseTypes.Add(tgType);
				codeTypeDeclatarion.IsClass = true;

				codeTypeDeclatarion.Members.Add(GenerateRunMethod());
				codeTypeDeclatarion.Members.Add(GenerateGetNameMethod());

				return codeTypeDeclatarion;
			}

			internal GeneratedInstance(Type tgType) {
				this.tgType = tgType;
				MemberTypes memberType=MemberTypes.Method;		//Only find methods.
				BindingFlags bindingAttr = BindingFlags.IgnoreCase|BindingFlags.Instance|BindingFlags.Public;

				MemberInfo[] mis = tgType.FindMembers(memberType, bindingAttr, StartsWithString, "on_");	//Does it's name start with "on_"?
				foreach (MemberInfo m in mis) {
					MethodInfo mi = m as MethodInfo;
					if (mi != null) {
						triggerMethods.Add(mi);
					}
				}
			}

			private CodeMemberMethod GenerateRunMethod() {
				CodeMemberMethod retVal = new CodeMemberMethod();
				retVal.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				retVal.Name = "Run";
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "self"));
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(TriggerKey), "tk"));
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(ScriptArgs), "sa"));
				retVal.ReturnType = new CodeTypeReference(typeof(object));

				if (triggerMethods.Count > 0) {
					retVal.Statements.Add(new CodeVariableDeclarationStatement(
						typeof(object[]),
						"argv"));

					retVal.Statements.Add(new CodeSnippetStatement("\t\t\tswitch (tk.uid) {"));
					foreach (MethodInfo mi in triggerMethods) {
						TriggerKey tk = TriggerKey.Get(mi.Name.Substring(3));
						retVal.Statements.Add(new CodeSnippetStatement("\t\t\t\tcase("+tk.uid+"): //"+tk.name));
						retVal.Statements.AddRange(
							CompiledScriptHolderGenerator.GenerateMethodInvocation(mi,
								new CodeThisReferenceExpression()));
					}
					retVal.Statements.Add(new CodeSnippetStatement("\t\t\t}"));
				}

				retVal.Statements.Add(
					new CodeMethodReturnStatement(
						new CodePrimitiveExpression(null)));

				return retVal;
			}

			private CodeMemberMethod GenerateGetNameMethod() {
				CodeMemberMethod retVal = new CodeMemberMethod();
				retVal.Attributes = MemberAttributes.Family | MemberAttributes.Override;
				retVal.Name = "GetName";
				retVal.ReturnType = new CodeTypeReference(typeof(string));

				retVal.Statements.Add(
					new CodeMethodReturnStatement(
						new CodePrimitiveExpression(tgType.Name)));


				return retVal;
			}

			private static bool StartsWithString(MemberInfo m, object filterCriteria) {
				string s=((string) filterCriteria).ToLower();
				return m.Name.ToLower().StartsWith(s);
			}
		}
	}
	
	//Implemented by the types which can represent map tiles
	//like t_water and such
	//more in the Map class
	//if someone has a better idea about how to do this ...
	public abstract class GroundTileType : CompiledTriggerGroup {
		private static Dictionary<string, GroundTileType> byName = new Dictionary<string, GroundTileType>(StringComparer.OrdinalIgnoreCase);

		protected GroundTileType() {
			byName[this.defname] = this;
		}
		
		public static new GroundTileType Get(string name) {
			GroundTileType gtt;
			byName.TryGetValue(name, out gtt);
			return gtt;
		}

		public static bool IsMapTileInRange(int tileId, int aboveOrEqualTo, int below) {
			return (tileId>=aboveOrEqualTo && tileId<=below);
		}

		public abstract bool IsTypeOfMapTile(int mapTileId);
		
		internal static void UnLoadScripts() {
			byName.Clear();
		}
	}
}