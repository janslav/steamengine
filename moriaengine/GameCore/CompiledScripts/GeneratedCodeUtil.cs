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
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.CodeDom;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Text;
using System.Globalization;
using SteamEngine.Timers;
using SteamEngine.Common;
using SteamEngine.Persistence;
using NAnt.Core;

namespace SteamEngine.CompiledScripts {

	[Summary("Implement this interface to use the core's service of generating an assembly, additional to the scripts "
		+ "(it will most likely be somehow based on the scripts...)")]
	[Remark("In the method \"WriteSources\", the implementor is supposed to generate the code using CodeDom facilities, in C#.")]
	public interface ISteamCSCodeGenerator {
		string FileName { get; }
		CodeCompileUnit WriteSources();
		void HandleAssembly(Assembly compiledAssembly);
	}

	public static class GeneratedCodeUtil {
		internal static Assembly generatedAssembly;

		internal static Dictionary<string, ISteamCSCodeGenerator> generators = new Dictionary<string, ISteamCSCodeGenerator>(StringComparer.OrdinalIgnoreCase);

		private static CodeDomProvider provider;
		private static CodeGeneratorOptions options;

		static GeneratedCodeUtil() {
			options = new CodeGeneratorOptions();
			options.IndentString = "\t";
			options.ElseOnClosing = true;
			provider = new Microsoft.CSharp.CSharpCodeProvider();
		}

		internal static void RegisterGenerator(ISteamCSCodeGenerator generator) {
			string fileName = generator.FileName;
			if (generators.ContainsKey(fileName)) {
				throw new OverrideNotAllowedException("There is already a ISteamCSCodeGenerator (" + generators[fileName] + ") registered for the file name " + fileName);//hopefully this will never display cos it would make no sense
			}
			generators[fileName] = generator;
		}

		//removes all non-core references
		internal static void UnLoadScripts() {
			ISteamCSCodeGenerator[] allGens = new ISteamCSCodeGenerator[generators.Count];
			generators.Values.CopyTo(allGens, 0);
			generators.Clear();
			foreach (ISteamCSCodeGenerator gen in allGens) {
				if (ClassManager.CoreAssembly == gen.GetType().Assembly) {
					generators[gen.FileName] = gen;
				}
			}
		}

		internal static bool DumpAndCompile() {
			foreach (ISteamCSCodeGenerator generator in generators.Values) {
				CodeCompileUnit codeCompileUnit = generator.WriteSources();
				if (codeCompileUnit == null) {
					Logger.WriteFatal(generator + " failed to generate scripts...?");
					return false;
				}

				string sourceFileName = Tools.CombineMultiplePaths(
					Path.GetDirectoryName(ClassManager.CoreAssembly.Location),
					"Generated",
					generator.FileName);
				Tools.EnsureDirectory(Path.GetDirectoryName(sourceFileName));
				StreamWriter outFile = new StreamWriter(sourceFileName, false);
				provider.GenerateCodeFromCompileUnit(codeCompileUnit, outFile, options);
				outFile.Close();
			}

			if (!CompileUsingNAnt()) {
				return false;
			}

			foreach (ISteamCSCodeGenerator generator in generators.Values) {
				generator.HandleAssembly(generatedAssembly);
			}
			return true;
		}

		private static bool CompileUsingNAnt() {
			NantLauncher nant = new NantLauncher();
			nant.SetLogger(new CompilerInvoker.CoreNantLogger());
			nant.SetPropertiesAsSelf();
#if SANE
			nant.SetProperty("cmdLineParams", "/debug+"); //in sane builds, scripts should still have debug info
#endif
			nant.SetTarget("buildGeneratedCode");

			nant.SetProperty("scriptsNumber", CompilerInvoker.compilenumber.ToString());
			nant.SetProperty("scriptsReferencesListPath",
				Path.Combine(Globals.scriptsPath, "referencedAssemblies.txt"));

			Logger.StopListeningConsole();//stupid defaultlogger writes to Console.Out
			nant.Execute();
			Logger.ResumeListeningConsole();

			if (nant.WasSuccess()) {
				Console.WriteLine("Done compiling generated C# code.");
				generatedAssembly = nant.GetCompiledAssembly("generatedCodeFileName");
				return true;
			} else {
				return false;
			}
		}

		public static CodeExpression GenerateSimpleLoadExpression(Type dataType, CodeExpression inputStringExpression) {
			if (dataType == typeof(string)) {
				return new CodeCastExpression(
					typeof(string),
					new CodeMethodInvokeExpression(
						new CodeTypeReferenceExpression(typeof(ObjectSaver)),
						"OptimizedLoad_String",
						inputStringExpression));
			} else if (ConvertTools.IsNumberType(dataType)) {
				Type enumType = null;
				if (dataType.IsEnum) {
					enumType = dataType;
					dataType = Enum.GetUnderlyingType(enumType);
				}

				MethodInfo parseMethod = typeof(ConvertTools).GetMethod("Parse" + dataType.Name, BindingFlags.Static | BindingFlags.Public);
				if (parseMethod != null) {
					CodeMethodInvokeExpression parseExp = new CodeMethodInvokeExpression(
						new CodeMethodReferenceExpression(
							new CodeTypeReferenceExpression(typeof(ConvertTools)),
							parseMethod.Name),
						inputStringExpression);

					if (enumType != null) {
						return new CodeCastExpression(
							enumType,
							parseExp);
					} else {
						return parseExp;
					}
				} else {
					throw new SEException("ConvertTools class missing a method for parsing number type " + dataType + ". This shoud not happen.");
				}
			} else {
				CodeMethodInvokeExpression loadMethod = new CodeMethodInvokeExpression(
					new CodeTypeReferenceExpression(typeof(ObjectSaver)),
					"OptimizedLoad_SimpleType",
					inputStringExpression,
					new CodeTypeOfExpression(dataType));

				MethodInfo parseMethod = typeof(Convert).GetMethod("To" + dataType.Name, BindingFlags.Static | BindingFlags.Public, null,
					new Type[] { typeof(object) }, null);

				//this will probably be true for datetime only
				if (parseMethod != null) {
					return new CodeMethodInvokeExpression(
						new CodeTypeReferenceExpression(typeof(Convert)),
						parseMethod.Name,
						loadMethod);
				} else if (dataType == typeof(object)) {//unprobable, because Object is not simple
					return new CodeMethodInvokeExpression(
						new CodeTypeReferenceExpression(typeof(ObjectSaver)),
						"Load",
						inputStringExpression);
				} else {
					return new CodeCastExpression(
						dataType,
						loadMethod);
				}
			}
		}

		public static CodeExpression GenerateDelayedLoadExpression(Type dataType, CodeExpression inputObjectExpression) {
			if (dataType == typeof(object)) {
				return inputObjectExpression;
			} else if (ConvertTools.IsNumberType(dataType)) {
				CodeMethodInvokeExpression toNumberExp = new CodeMethodInvokeExpression(
					new CodeTypeReferenceExpression(typeof(Convert)),
					"To" + dataType.Name,
					inputObjectExpression);
				if (dataType.IsEnum) {
					return new CodeCastExpression(
						dataType,
						toNumberExp);
				} else {
					return toNumberExp;
				}
			} else {
				MethodInfo convertMethod = typeof(Convert).GetMethod("To" + dataType.Name, BindingFlags.Static | BindingFlags.Public, null,
					new Type[] { typeof(object) }, null);

				//this will probably be true for datetime only
				if (convertMethod != null) {
					return new CodeMethodInvokeExpression(
						new CodeTypeReferenceExpression(typeof(Convert)),
						convertMethod.Name,
						inputObjectExpression);
				} else {
					return new CodeCastExpression(
						dataType,
						inputObjectExpression);
				}
			}
		}
	}
}