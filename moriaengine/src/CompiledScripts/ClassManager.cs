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
using System.Collections;
using System.Reflection;
using SteamEngine;
using SteamEngine.Common;
using SteamEngine.Timers;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts { 
	
	public static class ClassManager {
		public readonly static Hashtable allTypesbyName = new Hashtable(StringComparer.OrdinalIgnoreCase);
		//String-Type pairs.
		//private static Type[] allTypes;
		
		private static Hashtable registerTGmethods = new Hashtable(StringComparer.OrdinalIgnoreCase);
		//string - MethodInfo pairs

		private static Assembly commonAssembly = typeof(ConAttrs).Assembly;
		public static Assembly CommonAssembly { get {
			return commonAssembly;
		} }

		private static Assembly coreAssembly = typeof(ClassManager).Assembly;
		public static Assembly CoreAssembly { get {
			return coreAssembly;
		} }

		public static Assembly ScriptsAssembly { get {
			return CompilerInvoker.compiledScripts.assembly;
		} }

		public static Assembly GeneratedAssembly { get {
			return GeneratedCodeUtil.generatedAssembly;
		} }

		//removes all non-core references
		internal static void UnLoadScripts() {
			Type[] types = new Type[allTypesbyName.Count];
			allTypesbyName.Values.CopyTo(types, 0);
			allTypesbyName.Clear();
			foreach (Type type in types) {
				if (coreAssembly == type.Assembly) {
					allTypesbyName[type.Name] = type;
				}
			}
			
			//allTypes = new Type[allTypesbyName.Count];
			//allTypesbyName.Values.CopyTo(allTypes, 0);
		}
		
		public static Type GetType(string name) {
			return (Type) allTypesbyName[name];
		}
		
		internal static MethodInfo GetRegisterTGmethod(string name) {
			return (MethodInfo) registerTGmethods[name];
		}
		
		internal static bool InitClasses(Assembly assembly) {
			Type[] types = assembly.GetTypes();
			if (!InitClasses(types, assembly.GetName().Name, (coreAssembly == assembly) )) {
				Logger.WriteCritical("Scripts invalid.");
				return false;
			}
			//allTypes = new Type[allTypesbyName.Count];
			//allTypesbyName.Values.CopyTo(allTypes, 0);
			return true;
		}
			
		private static bool InitClasses(Type[] types, string assemblyName, bool isCoreAssembly) {
			bool success = true;
			for (int i=0; i<types.Length; i++) {
				try {
					InitClass(types[i], isCoreAssembly);
				} catch (FatalException) {
					throw;
				} catch (Exception e) {
					Logger.WriteError(assemblyName,types[i].Name, e);
					success = false;
				}
			}
			return success;
		}

		private static void InitClass(Type type, bool isCoreAssembly) {
			allTypesbyName[type.Name] = type;
			
			if (type.IsSubclassOf(typeof(TagHolder))) {
				MethodInfo rtgmi = type.GetMethod("RegisterTriggerGroup", 
					BindingFlags.Public | BindingFlags.Static, null, new Type[] {typeof(TriggerGroup)}, null);
				if (rtgmi != null) {
					MethodInfo mw = MemberWrapper.GetWrapperFor(rtgmi);
					registerTGmethods[type.Name] = mw;
				}
			}

			if (type.IsSubclassOf(typeof(AbstractSkillDef))) {
				AbstractSkillDef.RegisterSkillDefType(type);
			}
			
			if (Attribute.IsDefined(type, typeof(SaveableClassAttribute), false)) {
				DecoratedClassesSaveImplementorGenerator.AddDecoratedClass(type);
				if (Attribute.IsDefined(type, typeof(DeepCopySaveableClassAttribute))) {
					AutoDeepCopyImplementorGenerator.AddDecoratedClass(type);
				}
			}

			if (Attribute.IsDefined(type, typeof(ManualDeepCopyClassAttribute), false)) {
				ManualDeepCopyImplementorGenerator.AddDecoratedClass(type);
			}

			//if (typeof(IImportable).IsAssignableFrom(type)) {
			//    ExportImport.RegisterExportClass(type);
			//}
			
			//from scripts only, because we have a special one in ObjectSaver and stuff.
			if (typeof(ISaveImplementor).IsAssignableFrom(type) && (!isCoreAssembly) ) {
				if (!type.IsAbstract) {
					ConstructorInfo ci = type.GetConstructor(Type.EmptyTypes);
					if (ci!=null) {
						ISaveImplementor si = (ISaveImplementor) ci.Invoke(new object[0] {});
						ObjectSaver.RegisterImplementor(si);
					} else {
						throw new Exception("No proper constructor.");
					}
				}
			}

			if (typeof(IBaseClassSaveCoordinator).IsAssignableFrom(type)) {
				if (!type.IsAbstract) {
					ConstructorInfo ci = type.GetConstructor(Type.EmptyTypes);
					if (ci!=null) {
						IBaseClassSaveCoordinator ibcsc = (IBaseClassSaveCoordinator) ci.Invoke(new object[0] { });
						ObjectSaver.RegisterCoordinator(ibcsc);
					} else {
						throw new Exception("No proper constructor.");
					}
				}
			}
			
			if (typeof(ISimpleSaveImplementor).IsAssignableFrom(type)) {
				if (!type.IsAbstract) {
					ConstructorInfo ci = type.GetConstructor(Type.EmptyTypes);
					if (ci!=null) {
						ISimpleSaveImplementor ssi = (ISimpleSaveImplementor) ci.Invoke(new object[0] {});
						ObjectSaver.RegisterSimpleImplementor(ssi);
					} else {
						throw new Exception("No proper constructor.");
					}
				}
			}

			if (typeof(ISteamCSCodeGenerator).IsAssignableFrom(type)) {
				if (!type.IsAbstract) {
					ConstructorInfo ci = type.GetConstructor(Type.EmptyTypes);
					if (ci!=null) {
						ISteamCSCodeGenerator sccg = (ISteamCSCodeGenerator) ci.Invoke(new object[0] { });
						GeneratedCodeUtil.RegisterGenerator(sccg);
					} else {
						throw new Exception("No proper constructor.");
					}
				}
			}

			if (typeof(IDeepCopyImplementor).IsAssignableFrom(type)) {
				if ((!type.IsAbstract) && (type.IsPublic)) {
					ConstructorInfo ci = type.GetConstructor(Type.EmptyTypes);
					if (ci!=null) {
						IDeepCopyImplementor dci = (IDeepCopyImplementor) ci.Invoke(new object[0] { });
						DeepCopyFactory.RegisterImplementor(dci);
					} else {
						throw new Exception("No proper constructor.");
					}
				}
			}

			foreach (MethodInfo meth in type.GetMethods(BindingFlags.Static|BindingFlags.Public|BindingFlags.DeclaredOnly)) {
				if (Attribute.IsDefined(meth, typeof(RegisterWithRunTestsAttribute))) {
					TestSuite.AddTest(meth);
				}
				if (Attribute.IsDefined(meth, typeof(SteamFunctionAttribute))) {
					CompiledScriptHolderGenerator.AddCompiledSHType(meth);
				}
			}

			if (type.IsSubclassOf(typeof(AbstractDef))) {
				AbstractDef.RegisterSubtype(type);
			}

			if (type.IsSubclassOf(typeof(CompiledTriggerGroup))) {
				if ((!type.IsAbstract) && (!type.IsSealed)) {//they will be overriden anyway by generated code (so they _could_ be abstract), 
					//but the abstractness means here that they're utility code and not actual TGs (like GroundTileType)
					CompiledTriggerGroupGenerator.AddCompiledTGType(type);
					goto bootstrap;
				}
			}
			if (type.IsSubclassOf(typeof(CompiledScriptHolder))
					|| type.IsSubclassOf(typeof(AbstractScript))) {
				//Instansiate it!
				if (!type.IsAbstract) {
					ConstructorInfo ci=type.GetConstructor(Type.EmptyTypes);
					if (ci!=null) {
						ci.Invoke(new object[0] {});
					//} else {
					//	throw new Exception("No proper constructor.");
					}
				}
			//} else if (typeof(Region).IsAssignableFrom(type)) {
			//    Region.RegisterRegionType(type);
			//} else if (type.IsSubclassOf(typeof(Timer))) {
			//    Timer.RegisterSubClass(type);
			//} else if (type.IsSubclassOf(typeof(Thing))) {
			//	ThingDef.RegisterThingSubtype(type);
			}

bootstrap:
			MethodInfo m = type.GetMethod("Bootstrap", BindingFlags.Static|BindingFlags.Public|BindingFlags.DeclaredOnly ); 
			if (m!=null) {
				m.Invoke(null, null);
			}
		}

		//called by Main on the end of startup/recompile process
		internal static void InitScripts() { 
			Type[] types = commonAssembly.GetTypes();
			if (!ClassManager.InitClasses(types, commonAssembly.GetName().Name, false)) {
				throw new SEException("Common library invalid.");
			}
			
			Logger.WriteDebug("Initializing Scripts.");
			foreach (Type type in allTypesbyName.Values) {
				Assembly a = type.Assembly;
				if ((coreAssembly != a) && (commonAssembly != a)) {
					MethodInfo m = type.GetMethod("Init", BindingFlags.Static|BindingFlags.Public|BindingFlags.DeclaredOnly );
					if (m!=null) {
						try {
							m.Invoke(null, null);
						} catch (FatalException) {
							throw;
						} catch (Exception e) {
							Logger.WriteError(type.Name,m.Name, e);
						}
					}
				}
			}
			Logger.WriteDebug("Initializing Scripts done.");
		}
	}
}
