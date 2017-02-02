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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using EQATEC.Profiler;
using PerCederberg.Grammatica.Parser;
using Shielded;
using SteamEngine.Common;
using SteamEngine.Parsing;
using SteamEngine.Timers;
using SteamEngine.Transactionality;

namespace SteamEngine.Scripting.Interpretation {

	public static class LScriptMain {
		public static object RunSnippet(TagHolder self, string script) {
			LScriptHolder snippetRunner;
			return RunSnippet("<snippet>", 0, self, script, out snippetRunner);
		}

		public static object TryRunSnippet(TagHolder self, string script, out Exception exception) {
			LScriptHolder snippetRunner;
			return TryRunSnippet("<snippet>", 0, self, script, out exception, out snippetRunner);
		}

		public static LScriptHolder GetNewSnippetRunner(string filename, int line, string script) {
			script += Environment.NewLine;
			var newSnippetRunner = new LScriptHolder();

			try {
				newSnippetRunner.SetMetadataAndCompile(inputFilename: filename, inputStartLine: line, inputCode: script);
				return newSnippetRunner;
			} catch (ParserLogException ple) {
				var lstr = new LogStrBuilder();
				for (int i = 0, n = ple.GetErrorCount(); i < n; i++) {
					var pe = ple.GetError(i);
					var curline = pe.GetLine() + line;
					if (i > 0) {
						lstr.Append(Environment.NewLine);
					}
					lstr.Append(LogStr.FileLine(filename, curline)).Append(pe.GetErrorMessage());
				}
				throw new SEException(lstr.ToLogStr());
			} catch (RecursionTooDeepException rtde) {
				throw rtde; // we really do want to rethrow it, so that its useless stack is lost.
			}
		}

		public static object RunSnippet(string filename, int line, TagHolder self, string script, out LScriptHolder snippetRunner) {
			script += Environment.NewLine;

			snippetRunner = new LScriptHolder();
			return snippetRunner.RunAsSnippet(filename: filename, line: line, self: self, script: script);
		}

		public static object TryRunSnippet(string filename, int line, TagHolder self, string script, out Exception exception, out LScriptHolder snippetRunner) {
			Transaction.AssertInTransaction();

			try {
				exception = null;
				return RunSnippet(filename, line, self, script, out snippetRunner);
			} catch (FatalException) {
				throw;
			} catch (TransException) {
				throw;
			} catch (SEException sex) {
				if (sex.StartsWithFileLine) {
					Logger.WriteError(sex);
				} else {
					Logger.WriteError(filename, line, sex);
				}
				snippetRunner = null;
				exception = sex;
				return null;
			} catch (Exception e) {
				Logger.WriteError(filename, line, e);
				snippetRunner = null;
				exception = e;
				return null;
			}
		}

		internal static LScriptHolder LoadAsFunction(TriggerSection input) {
			Transaction.AssertInTransaction();

			var name = input.TriggerName;
			var sc = ScriptHolder.GetFunction(name) as LScriptHolder;
			if (sc == null) {
				sc = new LScriptHolder(input);
				sc.RegisterAsFunction();
			} else {
				if (sc.IsUnloaded) {
					sc.TrySetMetadataAndCompile(input);
				} else {
					throw new ScriptException("Function " + LogStr.Ident(name) + " already exists!");
				}
			}
			return sc;
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal static OpNode TryCompile(LScriptHolder parent, TextReader stream, int startLine) {
			try {
				return Compile(parent, stream, startLine);
			} catch (FatalException) {
				throw;
			} catch (TransException) {
				throw;
			} catch (ParserLogException ple) {
				for (int i = 0, n = ple.GetErrorCount(); i < n; i++) {
					var pe = ple.GetError(i);
					var line = pe.GetLine() + startLine;
					Logger.WriteError(parent.Filename, line, pe);
				}
			} catch (RecursionTooDeepException) {
				Logger.WriteError(parent.Filename, startLine, "Recursion too deep while parsing.");
			} catch (Exception e) {
				Logger.WriteError(parent.Filename, startLine, e);
			}
			return null;
		}

		internal static OpNode Compile(LScriptHolder parent, TextReader stream, int startLine) {
			Parser parser = new LScriptParser(stream);
			//Parser parser = new LScriptParser(stream, new DebugAnalyzer());

			Node node = null;
			node = parser.Parse();
			Analyzer analyzer = new LScriptAnalyzer();
			node = analyzer.Analyze(node);
			//DisplayTree(node);
			var finishedNode = CompileNode(parent, node, new LScriptCompilationContext());
			return finishedNode;
		}

		[SkipInstrumentation]
		internal static OpNode CompileNode(IOpNodeHolder parent, Node code, bool mustEval, LScriptCompilationContext context) {
			switch ((StrictConstants) code.GetId()) {
				case StrictConstants.STRONG_EVAL_EXPRESSION:
				case StrictConstants.EVAL_EXPRESSION:
				case StrictConstants.ARGUMENT:
				case StrictConstants.VAR_EXPRESSION:
				case StrictConstants.IF_BLOCK:
				case StrictConstants.WHILE_BLOCK:
				case StrictConstants.SCRIPT:
				case StrictConstants.CODE:
				case StrictConstants.CODE_BODY:
				case StrictConstants.SIMPLE_CODE:
				case StrictConstants.SIMPLE_CODE_BODY:
				case StrictConstants.ARGCHK:
				case StrictConstants.ARGTXT:
				case StrictConstants.ARGNUM:
				case StrictConstants.ARGO:
				case StrictConstants.ARGN:
				case StrictConstants.ARGNN:
				case StrictConstants.ARGVN:
				case StrictConstants.ARGON:
				case StrictConstants.ADD_TIMER_EXPRESSION:
				case StrictConstants.WHITESPACE:
				case StrictConstants.COMEOL:
				case StrictConstants.EVAL_WORD_EXPRESSION:
				case StrictConstants.TIMER_KEY:
				case StrictConstants.TRIGGER_KEY:
				case StrictConstants.FOREACH_BLOCK:
				case StrictConstants.FOR_BLOCK:
				case StrictConstants.SWITCH_BLOCK:
				case StrictConstants.RANDOM_EXPRESSION:
				case StrictConstants.TYPE_OF_EXPRESSION:
					return CompileNode(parent, code, context);
			}

			if (mustEval) {
				switch ((StrictConstants) code.GetId()) {
					case StrictConstants.STRING:
					case StrictConstants.SIMPLE_EXPRESSION:
						return OpNode_Lazy_Expression.Construct(parent, code, mustEval: true, context: context);

					case StrictConstants.DOTTED_EXPRESSION_CHAIN:
						return OpNode_Lazy_ExpressionChain.Construct(parent, code, context);

					case StrictConstants.CODE_BODY_PARENS:
					case StrictConstants.SIMPLE_CODE_BODY_PARENS:
						return CompileNode(parent, code.GetChildAt(1), true, context);

					case StrictConstants.SCRIPT_LINE:
						if (code.GetChildCount() > 1) {
							return CompileNode(parent, code.GetChildAt(0), context);
						}
						return OpNode_Object.Construct(parent, (object) null);
				}


				throw new InterpreterException(
					"Uncompilable node. If you see this message you have probably used expression '" + LogStr.Number(GetString(code)) +
					"'(Node type " + LogStr.Ident(code.ToString()) + ") in an invalid way.",
					context.startLine + code.GetStartLine(), code.GetStartColumn(),
					GetParentScriptHolder(parent).Filename, GetParentScriptHolder(parent).GetDecoratedName());
			}
			return CompileNode(parent, code, context);
		}

		internal static OpNode CompileNode(IOpNodeHolder parent, Node code, LScriptCompilationContext context) {
			//Console.WriteLine("compiling "+GetString(code));
			switch ((StrictConstants) code.GetId()) {
				case StrictConstants.IF_BLOCK:
					return OpNode_If.Construct(parent, code, context);

				case StrictConstants.WHILE_BLOCK:
					return OpNode_While.Construct(parent, code, context);

				case StrictConstants.FOREACH_BLOCK:
					return OpNode_Foreach.Construct(parent, code, context);

				case StrictConstants.FOR_BLOCK:
					return OpNode_For.Construct(parent, code, context);

				case StrictConstants.SWITCH_BLOCK:
					return OpNode_Switch.Construct(parent, code, context);

				case StrictConstants.WHITESPACE:
#if DEBUG
					Logger.WriteWarning(GetParentScriptHolder(parent).Filename,
						code.GetStartLine() + context.startLine, "Void code.");
#endif
					return OpNode_Object.Construct(parent, (object) null);

				case StrictConstants.COMEOL:
#if DEBUG
					Logger.WriteWarning(GetParentScriptHolder(parent).Filename,
						code.GetStartLine() + context.startLine, "Void code.");
#endif
					return OpNode_Object.Construct(parent, (object) null);

				case StrictConstants.TIMER_KEY:
					return OpNode_Object.Construct(parent, TimerKey.Acquire(
						((Token) code.GetChildAt(1)).GetImage()));

				case StrictConstants.TRIGGER_KEY:
					return OpNode_Object.Construct(parent, TriggerKey.Acquire(
						((Token) code.GetChildAt(1)).GetImage()));

				case StrictConstants.PLUGIN_KEY:
					return OpNode_Object.Construct(parent, PluginKey.Acquire(
						((Token) code.GetChildAt(2)).GetImage()));

				case StrictConstants.ARGCHK:
				case StrictConstants.ARGTXT:
				case StrictConstants.ARGNUM:
				case StrictConstants.ARGO:
				case StrictConstants.ARGN:
				case StrictConstants.ARGNN:
				case StrictConstants.ARGVN:
				case StrictConstants.ARGON:
				case StrictConstants.ARGUMENT:
					return OpNode_Argument.Construct(parent, code, context);

				case StrictConstants.SCRIPT:
					return OpNode_Script.Construct(parent, code, context);

				case StrictConstants.CODE:
					return OpNode_Code.Construct(parent, code, context);

				case StrictConstants.CODE_BODY:
					return OpNode_Lazy_UnOperator.Construct(parent, code, context);

				case StrictConstants.CODE_BODY_PARENS:
					return CompileNode(parent, code.GetChildAt(1), context);

				case StrictConstants.SIMPLE_CODE:
					return OpNode_Code.Construct(parent, code, context);

				case StrictConstants.SIMPLE_CODE_BODY:
					return OpNode_Lazy_UnOperator.Construct(parent, code, context);

				case StrictConstants.SIMPLE_CODE_BODY_PARENS:
					return CompileNode(parent, code.GetChildAt(1), context);

				case StrictConstants.EVAL_WORD_EXPRESSION:
					return CompileNode(parent, code.GetChildAt(1), true, context);

				case StrictConstants.QUOTED_STRING:
					return OpNode_Lazy_QuotedString.Construct(parent, code, context);

				case StrictConstants.SCRIPT_LINE:
					if (code.GetChildCount() > 1) {
						return CompileNode(parent, code.GetChildAt(0), context);
					}
					return OpNode_Object.Construct(parent, (object) null);

				case StrictConstants.STRONG_EVAL_EXPRESSION:
				case StrictConstants.EVAL_EXPRESSION:
					return OpNode_Lazy_EvalExpression.Construct(parent, code, context);

				case StrictConstants.DOTTED_EXPRESSION_CHAIN:
					return OpNode_Lazy_ExpressionChain.Construct(parent, code, context);

				case StrictConstants.STRING:
					return OpNode_Lazy_Expression.Construct(parent, code, mustEval: false, context: context);

				case StrictConstants.SIMPLE_EXPRESSION:
					// when the expression is just a word followed by space, it can be left un-evaled, otherwise its a method call/assignment
					var isWhiteSpaceAssigner = StrictConstants.WHITE_SPACE_ASSIGNER == (StrictConstants) code.GetChildAt(1).GetId();
					return OpNode_Lazy_Expression.Construct(parent, code, mustEval: !isWhiteSpaceAssigner, context: context);

				case StrictConstants.RANDOM_EXPRESSION:
					return OpNode_Lazy_RandomExpression.Construct(parent, code, context);

				case StrictConstants.VAR_EXPRESSION:
					return OpNode_Lazy_VarExpression.Construct(parent, code, context);

				case StrictConstants.ADD_TIMER_EXPRESSION:
					return OpNode_Lazy_AddTimer.Construct(parent, code, context);

				case StrictConstants.TYPE_OF_EXPRESSION:
					return OpNode_Typeof.Construct(parent, code, context);

				case StrictConstants.INTEGER:
					long i;
					try {
						i = ConvertTools.ParseInt64(((Token) code).GetImage());
					} catch (Exception e) {
						throw new InterpreterException("Exception while parsing integer",
							context.startLine + code.GetStartLine(), code.GetStartColumn(),
							GetParentScriptHolder(parent).Filename, GetParentScriptHolder(parent).GetDecoratedName(), e);
					}
					if ((i <= Int32.MaxValue) && (i >= Int32.MinValue)) {
						return OpNode_Object.Construct(parent, (int) i);
					}
					return OpNode_Object.Construct(parent, i);

				case StrictConstants.HEXNUMBER:
					ulong h;
					try {
						h = ConvertTools.ParseUInt64(((Token) code).GetImage().Trim());
					} catch (Exception e) {
						throw new InterpreterException("Exception while parsing hexadecimal integer",
							context.startLine + code.GetStartLine(), code.GetStartColumn(),
							GetParentScriptHolder(parent).Filename, GetParentScriptHolder(parent).GetDecoratedName(), e);
					}
					if ((h <= UInt32.MaxValue) && (h >= UInt32.MinValue)) {
						return OpNode_Object.Construct(parent, (uint) h);
					}
					return OpNode_Object.Construct(parent, h);

				case StrictConstants.FLOAT:
					double d;
					try {
						d = ConvertTools.ParseDouble(((Token) code).GetImage());
					} catch (Exception e) {
						throw new InterpreterException("Exception while parsing decimal number",
							context.startLine + code.GetStartLine(), code.GetStartColumn(),
							GetParentScriptHolder(parent).Filename, GetParentScriptHolder(parent).GetDecoratedName(), e);
					}
					return OpNode_Object.Construct(parent, d);
			}

			throw new InterpreterException(
				"Uncompilable node. If you see this message you have probably used expression '" + LogStr.Number(GetString(code)) +
				"'(Node type " + LogStr.Ident(code.ToString()) + ")  in an invalid way.",
				context.startLine + code.GetStartLine(), code.GetStartColumn(),
				GetParentScriptHolder(parent).Filename, GetParentScriptHolder(parent).GetDecoratedName());
		}

		internal static LScriptHolder GetParentScriptHolder(IOpNodeHolder holder) {
			var parentNode = holder as OpNode;
			if (parentNode != null) {
				return parentNode.ParentScriptHolder;
			}
			return (LScriptHolder) holder;
		}

		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		internal static void DisplayTree(Node node, LScriptCompilationContext context) {
			Console.WriteLine(context.indent + node + " : " + GetString(node));
			//Console.WriteLine(indent+node);
			for (int i = 0, n = node.GetChildCount(); i < n; i++) {
				var child = node.GetChildAt(i);
				context.indent += "    ";
				DisplayTree(child, context);
				context.indent = context.indent.Substring(0, context.indent.Length - 4);
			}
		}

		internal static string GetFirstTokenString(Node node) {
			if (node.GetChildCount() > 0) {
				return GetFirstTokenString(node.GetChildAt(0));
			}
			return GetString(node);
		}

		internal static string GetString(Node node) {
			var builder = new StringBuilder();
			BuildToString(node, builder);
			return builder.ToString();
		}

		private static void BuildToString(Node node, StringBuilder builder) {
			var token = node as Token;
			if (token != null) {
				if (node.GetId() == (int) StrictConstants.ESCAPEDCHAR) {
					builder.Append(token.GetImage()[1]);
				} else {
					builder.Append(token.GetImage());
				}
			}
			if (node != null) {
				for (int i = 0, n = node.GetChildCount(); i < n; i++) {
					var child = node.GetChildAt(i);
					BuildToString(child, builder);
				}
			}
		}
	}
}
