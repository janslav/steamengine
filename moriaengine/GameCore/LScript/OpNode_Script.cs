//This software is released under GNU public license. See details in the URL: 
//http://www.gnu.org/copyleft/gpl.html 

namespace SteamEngine.LScript {
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

	public class OpNode_Script : OpNode, IOpNodeHolder {
		private OpNode[] blocks;

		internal static OpNode Construct(IOpNodeHolder parent, Node code) {
			int line = code.GetStartLine() + LScript.startLine;
			int column = code.GetStartColumn();
			string filename = "<unknown>";//the name should in fact be irrelevant cos this node should never tthrow any exception by its nature
			if (parent != null) {
				filename = LScript.GetParentScriptHolder(parent).filename;
			}

			OpNode_Script constructed = new OpNode_Script(
				parent, filename, line, column, code);

			ArrayList blocksList = new ArrayList();
			for (int i = 0, n = code.GetChildCount(); i < n; i++) {
				Node block = code.GetChildAt(i);
				if (block.GetId() != (int) StrictConstants.COMEOL) {
					blocksList.Add(LScript.CompileNode(constructed, block, true));
				}
			}

			OpNode[] b = new OpNode[blocksList.Count];
			blocksList.CopyTo(b);
			constructed.blocks = b;
			return constructed;
		}

		protected OpNode_Script(IOpNodeHolder parent, string filename, int line, int column, Node origNode)
			: base(parent, filename, line, column, origNode) {

		}

		public void Replace(OpNode oldNode, OpNode newNode) {
			int index = Array.IndexOf(blocks, oldNode);
			if (index < 0) {
				throw new Exception("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			} else {
				blocks[index] = newNode;
			}
		}

		internal override object Run(ScriptVars vars) {
			object retVal = null;
			for (int i = 0, n = blocks.Length; i < n; i++) {
				retVal = blocks[i].Run(vars);
				if (vars.returned) {
					return retVal;
				}
			}
			return null;
		}

		public override string ToString() {
			StringBuilder str = new StringBuilder();
			for (int i = 0, n = blocks.Length; i < n; i++) {
				str.Append(blocks[i] + Environment.NewLine);
			}
			return str.ToString();
		}
	}

	public class OpNode_Object : OpNode, IKnownRetType {
		internal object obj;

		internal static OpNode Construct(IOpNodeHolder parent, Node code) {
			int line = code.GetStartLine() + LScript.startLine;
			int column = code.GetStartColumn();
			OpNode_Object constructed = new OpNode_Object(
				parent, LScript.GetParentScriptHolder(parent).filename, line, column, code);
			//Console.WriteLine("OpNode_Object: getting string "+LScript.GetString(code));
			constructed.obj = LScript.GetString(code);
			return constructed;
		}

		internal static OpNode Construct(IOpNodeHolder parent, object obj) {
			string filename = "<unknown>";//the name should in fact be irrelevant cos this node should never tthrow any exception by its nature
			if (parent != null) {
				filename = LScript.GetParentScriptHolder(parent).filename;
			}
			OpNode_Object constructed = new OpNode_Object(
				parent, filename, -1, -1, null);
			constructed.obj = obj;
			return constructed;
		}

		protected OpNode_Object(IOpNodeHolder parent, string filename, int line, int column, Node origNode)
			: base(parent, filename, line, column, origNode) {

		}
		internal override object Run(ScriptVars vars) {
			return obj;
		}

		public override string ToString() {
			if (obj != null) {
				return "'" + obj + "'(" + obj.GetType() + ")";
			} else {
				return "null";
			}
		}

		public Type ReturnType {
			get {
				if (obj == null) {
					return typeof(void);
				} else {
					return obj.GetType();
				}
			}
		}
	}

	public class OpNode_ToString : OpNode, IOpNodeHolder, IKnownRetType {
		private OpNode node;

		internal static OpNode_ToString Construct(IOpNodeHolder parent, Node code) {
			int line = code.GetStartLine() + LScript.startLine;
			int column = code.GetStartColumn();
			OpNode_ToString constructed = new OpNode_ToString(
				parent, LScript.GetParentScriptHolder(parent).filename, line, column, code);

			constructed.node = LScript.CompileNode(constructed, code);
			return constructed;
		}

		internal OpNode_ToString(IOpNodeHolder parent, string filename, int line, int column, Node origNode, OpNode node)
			: base(parent, filename, line, column, origNode) {
			this.node = node;
		}

		protected OpNode_ToString(IOpNodeHolder parent, string filename, int line, int column, Node origNode)
			: base(parent, filename, line, column, origNode) {

		}

		public void Replace(OpNode oldNode, OpNode newNode) {
			if (node == oldNode) {
				node = newNode;
			} else {
				throw new Exception("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			}
		}

		internal override object Run(ScriptVars vars) {
			return string.Concat(node.Run(vars));
		}

		public override string ToString() {
			return node.ToString() + ".TOSTRING()";
		}

		public Type ReturnType {
			get {
				return typeof(string);
			}
		}
	}

	public class OpNode_This : OpNode {
		internal OpNode_This(IOpNodeHolder parent, string filename, int line, int column, Node origNode)
			: base(parent, filename, line, column, origNode) {

		}

		internal override object Run(ScriptVars vars) {
			return vars.self;
		}

		public override string ToString() {
			return "THIS";
		}
	}

	public class OpNode_Constant : OpNode {
		private Constant con;
		internal OpNode_Constant(IOpNodeHolder parent, string filename, int line, int column, Node origNode, Constant con)
			: base(parent, filename, line, column, origNode) {
			this.con = con;
		}

		internal override object Run(ScriptVars vars) {
			try {
				return con.Value;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while getting value of Constant '" + con.Name + "'",
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}


		public override string ToString() {
			return string.Concat("Constant ", con.Name);
		}
	}
}