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
using PerCederberg.Grammatica.Parser;

namespace SteamEngine.Scripting.Interpretation {
	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	public static class OpNode_Lazy_VarExpression {
		private const int TAG = 0;
		private const int ARG = 1;
		private const int VAR = 2;

		internal static OpNode Construct(IOpNodeHolder parent, Node origNode, LScriptCompilationContext context) {
			var line = origNode.GetStartLine() + context.startLine;
			var column = origNode.GetStartColumn();
			var filename = LScriptMain.GetParentScriptHolder(parent).Filename;

			//LScript.DisplayTree(origNode);
			var type = ResolveTokenType(origNode.GetChildAt(0));
			var current = 3;
			var indicesList = new List<OpNode>();
			OpNode_Lazy_Indexer lastIndex = null;
			while (OpNode.IsType(origNode.GetChildAt(current), StrictConstants.INDEXER)) {
				var indexprod = (Production) origNode.GetChildAt(current);
				var index = LScriptMain.CompileNode(parent, indexprod.GetChildAt(1), context);
				lastIndex = OpNode_Lazy_Indexer.Construct(parent, indexprod.GetChildAt(1), index, null, context);
				indicesList.Add(lastIndex);
				current++;
			}
			current++;
			Node argNode = null;
			if (lastIndex == null) {
				argNode = origNode.GetChildAt(current);
			} else if (origNode.GetChildAt(current) != null) {
				lastIndex.arg = LScriptMain.CompileNode(lastIndex, origNode.GetChildAt(current), context);
			}

			var name = LScriptMain.GetString(origNode.GetChildAt(2));
			if (StringComparer.OrdinalIgnoreCase.Equals(name, "remove")) {
				if (indicesList.Count != 0) {
					throw new InterpreterException("Remove in this context means removing of a single value, thus indexing is invalid.",
						line, column, filename, LScriptMain.GetParentScriptHolder(parent).GetDecoratedName());
				}
				if (OpNode.IsType(argNode, StrictConstants.STRING)) {
					name = LScriptMain.GetString(argNode);
					switch (type) {
						case TAG:
							return new OpNode_RemoveTag(parent, filename, line, column, origNode, name);
						case VAR:
							return new OpNode_RemoveVar(parent, filename, line, column, origNode, name);
						case ARG:
							return new OpNode_RemoveArg(parent, filename, line, column, origNode, name);
					}
					throw new FatalException("this will never happen");
				}
				throw new InterpreterException("Invalid/missing token specifying the name of the value to remove",
					line, column, filename, LScriptMain.GetParentScriptHolder(parent).GetDecoratedName());
			}
			if (StringComparer.OrdinalIgnoreCase.Equals(name, "exists")) {
				if (indicesList.Count != 0) {
					throw new InterpreterException("Exists in this context means finding out if given value exists, thus indexing is invalid.",
						line, column, filename, LScriptMain.GetParentScriptHolder(parent).GetDecoratedName());
				}
				if (OpNode.IsType(argNode, StrictConstants.STRING)) {
					name = LScriptMain.GetString(argNode);
					switch (type) {
						case TAG:
							return new OpNode_TagExists(parent, filename, line, column, origNode, name);
						case VAR:
							return new OpNode_VarExists(parent, filename, line, column, origNode, name);
						case ARG:
							return new OpNode_ArgExists(parent, filename, line, column, origNode, name);
					}
					throw new FatalException("this will never happen");
				}
				throw new InterpreterException("Invalid/missing token specifying the name of the value to examine",
					line, column, filename, LScriptMain.GetParentScriptHolder(parent).GetDecoratedName());
			}
			if (indicesList.Count == 0) {
				if (argNode == null) {
					return ConstructGetNode(type, parent, line, column, origNode, name);
				}
				var arg = LScriptMain.CompileNode(parent, argNode, context);
				switch (type) {
					case TAG:
						var setTagNode = new OpNode_SetTag(parent, filename, line, column, origNode, name, arg);
						arg.parent = setTagNode;
						return setTagNode;
					case VAR:
						var setVarNode = new OpNode_SetVar(parent, filename, line, column, origNode, name, arg);
						arg.parent = setVarNode;
						return setVarNode;
					case ARG:
						var setArgNode = new OpNode_SetArg(parent, filename, line, column, origNode, name, arg);
						arg.parent = setArgNode;
						return setArgNode;
				}
				throw new FatalException("this will never happen");
			}
			indicesList.Insert(0, ConstructGetNode(type, parent, line, column, origNode, name));
			var chain = indicesList.ToArray();
			return OpNode_Lazy_ExpressionChain.ConstructFromArray(parent, origNode, chain, context);
		}

		private static int ResolveTokenType(Node token) {
			if (OpNode.IsType(token, StrictConstants.TAG)) {
				return TAG;
			}
			if (OpNode.IsType(token, StrictConstants.VAR)) {
				return VAR;
			} //LOCAL or ARG token
			return ARG;
		}

		private static OpNode ConstructGetNode(int type, IOpNodeHolder parent, int line, int column, Node origNode, string name) {
			var filename = LScriptMain.GetParentScriptHolder(parent).Filename;
			switch (type) {
				case TAG:
					return new OpNode_GetTag(parent, filename, line, column, origNode, name);
				case VAR:
					return new OpNode_GetVar(parent, filename, line, column, origNode, name);
				case ARG:
					return new OpNode_GetArg(parent, filename, line, column, origNode, name);
			}
			throw new FatalException("this will never happen");
		}
	}
}