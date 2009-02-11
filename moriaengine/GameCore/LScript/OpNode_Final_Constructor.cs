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
using SteamEngine.Common;
using PerCederberg.Grammatica.Parser;

namespace SteamEngine.LScript {
	public class OpNode_ConstructorWrapper : OpNode, IOpNodeHolder, ITriable, IKnownRetType {
		internal readonly ConstructorInfo ctor;
		private readonly OpNode[] args;

		internal OpNode_ConstructorWrapper(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, ConstructorInfo ctor, OpNode[] args)
			: base(parent, filename, line, column, origNode) {
			this.args = args;
			this.ctor = ctor;
		}

		public virtual void Replace(OpNode oldNode, OpNode newNode) {
			int index = Array.IndexOf(args, oldNode);
			if (index < 0) {
				throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			} else {
				args[index] = newNode;
			}
		}

		internal override object Run(ScriptVars vars) {
			object oSelf = vars.self;
			vars.self = vars.defaultObject;
			int argsCount = args.Length;
			object[] results = new object[argsCount];
			try {
				for (int i = 0; i < argsCount; i++) {
					results[i] = args[i].Run(vars);
				}
			} finally {
				vars.self = oSelf;
			}
			try {
				return ctor.Invoke(results);
			} catch (InterpreterException ie) {
				ie.AddTrace(this);
				throw ie;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while calling constructor '" + ctor.DeclaringType.Name + "'",
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return ctor.Invoke(results);
			} catch (InterpreterException ie) {
				ie.AddTrace(this);
				throw ie;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while calling constructor '" + ctor.DeclaringType.Name + "'",
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public override string ToString() {
			StringBuilder str = new StringBuilder("(");
			str.Append(ctor.DeclaringType).Append(".ctor(");
			for (int i = 0, n = args.Length; i < n; i++) {
				str.Append(args[i].ToString()).Append(", ");
			}
			return str.Append("))").ToString();
		}

		public Type ReturnType {
			get {
				return ctor.DeclaringType;
			}
		}
	}

	public class OpNode_ConstructorWrapper_Params : OpNode, IOpNodeHolder, ITriable, IKnownRetType {
		internal readonly ConstructorInfo ctor;
		private readonly OpNode[] normalArgs;
		private readonly OpNode[] paramArgs;
		private readonly Type paramsElementType;

		internal OpNode_ConstructorWrapper_Params(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, ConstructorInfo ctor, OpNode[] normalArgs, OpNode[] paramArgs, Type paramsElementType)
			: base(parent, filename, line, column, origNode) {
			this.normalArgs = normalArgs;
			this.paramArgs = paramArgs;
			this.paramsElementType = paramsElementType;
			this.ctor = ctor;
		}

		public virtual void Replace(OpNode oldNode, OpNode newNode) {
			int index = Array.IndexOf(normalArgs, oldNode);
			if (index >= 0) {
				normalArgs[index] = newNode;
				return;
			}
			index = Array.IndexOf(paramArgs, oldNode);
			if (index >= 0) {
				paramArgs[index] = newNode;
				return;
			}
			throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
		}

		internal override object Run(ScriptVars vars) {
			object oSelf = vars.self;
			vars.self = vars.defaultObject;
			int normalArgsLength = normalArgs.Length;
			object[] results = new object[normalArgsLength + 1];
			try {
				for (int i = 0; i < normalArgsLength; i++) {
					results[i] = normalArgs[i].Run(vars);
				}
				int paramArrayLength = paramArgs.Length;
				Array paramArray = Array.CreateInstance(paramsElementType, paramArrayLength);
				for (int i = 0; i < paramArrayLength; i++) {
					paramArray.SetValue(paramArgs[i].Run(vars), i);
				}
				results[normalArgsLength] = paramArray;
			} finally {
				vars.self = oSelf;
			}
			try {
				return ctor.Invoke(results);
			} catch (InterpreterException ie) {
				ie.AddTrace(this);
				throw ie;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while calling constructor '" + ctor.DeclaringType.Name + "'",
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			int normalArgsLength = normalArgs.Length;
			object[] modifiedResults = new object[normalArgsLength + 1];
			Array.Copy(results, modifiedResults, normalArgsLength);
			try {
				//Console.WriteLine("results[0].GetType(): "+results[0]);
				int paramArrayLength = paramArgs.Length;
				Array paramArray = Array.CreateInstance(paramsElementType, paramArrayLength);
				Array.Copy(results, normalArgsLength, paramArray, 0, paramArrayLength);
				modifiedResults[normalArgsLength] = paramArray;
				return ctor.Invoke(modifiedResults);
			} catch (InterpreterException ie) {
				ie.AddTrace(this);
				throw ie;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while calling constructor '" + ctor.DeclaringType.Name + "'",
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public override string ToString() {
			StringBuilder str = new StringBuilder("(");
			str.Append(ctor.DeclaringType).Append(".ctor(");
			for (int i = 0, n = normalArgs.Length; i < n; i++) {
				str.Append(normalArgs[i].ToString()).Append(", ");
			}
			str.Append(Tools.ObjToString(paramArgs));
			return str.Append("))").ToString();
		}

		public Type ReturnType {
			get {
				return ctor.DeclaringType;
			}
		}
	}

	public class OpNode_ConstructorWrapper_String : OpNode, IOpNodeHolder, ITriable, IKnownRetType {
		internal readonly ConstructorInfo ctor;
		private readonly string formatString;
		private readonly OpNode[] args;

		internal OpNode_ConstructorWrapper_String(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, ConstructorInfo ctor, OpNode[] args, string formatString)
			: base(parent, filename, line, column, origNode) {
			this.args = args;
			this.ctor = ctor;
			this.formatString = formatString;
		}

		public virtual void Replace(OpNode oldNode, OpNode newNode) {
			int index = Array.IndexOf(args, oldNode);
			if (index < 0) {
				throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			} else {
				args[index] = newNode;
			}
		}

		internal override object Run(ScriptVars vars) {
			object oSelf = vars.self;
			vars.self = vars.defaultObject;
			int argsCount = args.Length;
			object[] results = new object[argsCount];
			try {
				for (int i = 0; i < argsCount; i++) {
					results[i] = args[i].Run(vars);
				}
			} finally {
				vars.self = oSelf;
			}
			try {
				string resultString = String.Format(formatString, results);
				return ctor.Invoke(BindingFlags.Default, null, new object[] { resultString }, null);
			} catch (InterpreterException ie) {
				ie.AddTrace(this);
				throw ie;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while calling constructor '" + ctor.DeclaringType.Name + "'",
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				string resultString = String.Format(formatString, results);
				return ctor.Invoke(new object[] { resultString });
			} catch (InterpreterException ie) {
				ie.AddTrace(this);
				throw ie;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while calling constructor '" + ctor.DeclaringType.Name + "'",
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public override string ToString() {
			StringBuilder str = new StringBuilder("(");
			str.Append(ctor.DeclaringType).Append(".ctor(");
			for (int i = 0, n = args.Length; i < n; i++) {
				str.Append(args[i].ToString()).Append(", ");
			}
			return str.Append(").TOSTRING())").ToString();
		}

		public Type ReturnType {
			get {
				return ctor.DeclaringType;
			}
		}
	}
}