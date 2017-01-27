//This software is released under GNU public license. See details in the URL: 
//http://www.gnu.org/copyleft/gpl.html 

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using PerCederberg.Grammatica.Parser;
using Shielded;
using SteamEngine.Scripting.Objects;

namespace SteamEngine.Scripting.Interpretation {

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_Script : OpNode, IOpNodeHolder {
		private OpNode[] blocks;

		internal static OpNode Construct(IOpNodeHolder parent, Node code, LScriptCompilationContext context) {
			var line = code.GetStartLine() + context.startLine;
			var column = code.GetStartColumn();
			var filename = "<unknown>";//the name should in fact be irrelevant cos this node should never tthrow any exception by its nature
			if (parent != null) {
				filename = LScriptMain.GetParentScriptHolder(parent).Filename;
			}

			var constructed = new OpNode_Script(
				parent, filename, line, column, code);

			var blocksList = new List<OpNode>();
			for (int i = 0, n = code.GetChildCount(); i < n; i++) {
				var block = code.GetChildAt(i);
				if (block.GetId() != (int) StrictConstants.COMEOL) {
					blocksList.Add(LScriptMain.CompileNode(constructed, block, true, context));
				}
			}

			constructed.blocks = blocksList.ToArray();
			return constructed;
		}

		protected OpNode_Script(IOpNodeHolder parent, string filename, int line, int column, Node origNode)
			: base(parent, filename, line, column, origNode) {

		}

		public void Replace(OpNode oldNode, OpNode newNode) {
			var index = Array.IndexOf(this.blocks, oldNode);
			if (index < 0) {
				throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			}
			this.blocks[index] = newNode;
		}

		internal override object Run(ScriptVars vars) {
			object retVal = null;
			for (int i = 0, n = this.blocks.Length; i < n; i++) {
				retVal = this.blocks[i].Run(vars);
				if (vars.returned) {
					return retVal;
				}
			}
			return null;
		}

		public override string ToString() {
			var str = new StringBuilder();
			for (int i = 0, n = this.blocks.Length; i < n; i++) {
				str.Append(this.blocks[i] + Environment.NewLine);
			}
			return str.ToString();
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_Object : OpNode, IKnownRetType {
		internal object obj;

		internal static OpNode Construct(IOpNodeHolder parent, Node code, LScriptCompilationContext context) {
			var line = code.GetStartLine() + context.startLine;
			var column = code.GetStartColumn();
			var constructed = new OpNode_Object(
				parent, LScriptMain.GetParentScriptHolder(parent).Filename, line, column, code);
			//Console.WriteLine("OpNode_Object: getting string "+LScript.GetString(code));
			constructed.obj = LScriptMain.GetString(code);
			return constructed;
		}

		internal static OpNode Construct(IOpNodeHolder parent, object obj) {
			var filename = "<unknown>";//the name should in fact be irrelevant cos this node should never tthrow any exception by its nature
			if (parent != null) {
				filename = LScriptMain.GetParentScriptHolder(parent).Filename;
			}
			var constructed = new OpNode_Object(
				parent, filename, -1, -1, null);
			constructed.obj = obj;
			return constructed;
		}

		protected OpNode_Object(IOpNodeHolder parent, string filename, int line, int column, Node origNode)
			: base(parent, filename, line, column, origNode) {

		}
		internal override object Run(ScriptVars vars) {
			return this.obj;
		}

		public override string ToString() {
			if (this.obj != null) {
				return "'" + this.obj + "'(" + this.obj.GetType() + ")";
			}
			return "null";
		}

		public Type ReturnType {
			get {
				if (this.obj == null) {
					return typeof(void);
				}
				return this.obj.GetType();
			}
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_ToString : OpNode, IOpNodeHolder, IKnownRetType {
		private OpNode node;

		internal static OpNode_ToString Construct(IOpNodeHolder parent, Node code, LScriptCompilationContext context) {
			var line = code.GetStartLine() + context.startLine;
			var column = code.GetStartColumn();
			var constructed = new OpNode_ToString(
				parent, LScriptMain.GetParentScriptHolder(parent).Filename, line, column, code);

			constructed.node = LScriptMain.CompileNode(constructed, code, context);
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
			if (this.node == oldNode) {
				this.node = newNode;
			} else {
				throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			}
		}

		internal override object Run(ScriptVars vars) {
			return string.Concat(this.node.Run(vars));
		}

		public override string ToString() {
			return this.node + ".TOSTRING()";
		}

		public Type ReturnType {
			get {
				return typeof(string);
			}
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_This : OpNode {
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

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_Constant : OpNode {
		private Constant con;
		internal OpNode_Constant(IOpNodeHolder parent, string filename, int line, int column, Node origNode, Constant con)
			: base(parent, filename, line, column, origNode) {
			this.con = con;
		}

		internal override object Run(ScriptVars vars) {
			try {
				return this.con.Value;
			} catch (FatalException) {
				throw;
			} catch (TransException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while getting value of Constant '" + this.con.Name + "'",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}


		public override string ToString() {
			return string.Concat("Constant ", this.con.Name);
		}
	}
}