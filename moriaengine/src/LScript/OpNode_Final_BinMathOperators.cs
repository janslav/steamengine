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

namespace SteamEngine.LScript {
	public class OpNode_AddOperator : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_AddOperator(IOpNodeHolder parent, Node code):base(parent, code) {
		}
		
		internal override object Run(ScriptVars vars) {
			object leftVal = left.Run(vars);
			object rightVal = right.Run(vars);
			try {
				return (Convert.ToDouble(leftVal) + Convert.ToDouble(rightVal));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating + operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
		
		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToDouble(results[0]) + Convert.ToDouble(results[1]));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating + operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}
	
	public class OpNode_SubOperator : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_SubOperator(IOpNodeHolder parent, Node code):base(parent, code) {
		}
		
		internal override object Run(ScriptVars vars) {
			object leftVal = left.Run(vars);
			object rightVal = right.Run(vars);
			try {
				return (Convert.ToDouble(leftVal) - Convert.ToDouble(rightVal));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating - operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
		
		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToDouble(results[0]) - Convert.ToDouble(results[1]));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating - operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}
	
	public class OpNode_MulOperator : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_MulOperator(IOpNodeHolder parent, Node code):base(parent, code) {
		}
		
		internal override object Run(ScriptVars vars) {
			object leftVal = left.Run(vars);
			object rightVal = right.Run(vars);
			try {
				return (Convert.ToDouble(leftVal) * Convert.ToDouble(rightVal));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating * operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
		
		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToDouble(results[0]) * Convert.ToDouble(results[1]));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating * operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}
	
	public class OpNode_DivOperator_Double : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_DivOperator_Double(IOpNodeHolder parent, Node code):base(parent, code) {
		}
		
		internal override object Run(ScriptVars vars) {
			object leftVal = left.Run(vars);
			object rightVal = right.Run(vars);
			try {
				return (Convert.ToDouble(leftVal) / Convert.ToDouble(rightVal));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating / operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
		
		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToDouble(results[0]) / Convert.ToDouble(results[1]));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating / operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}
	
	public class OpNode_DivOperator_Int : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_DivOperator_Int(IOpNodeHolder parent, Node code):base(parent, code) {
		}
		
		internal override object Run(ScriptVars vars) {
			object leftVal = left.Run(vars);
			object rightVal = right.Run(vars);
			try {
				return (Convert.ToInt64(leftVal) / Convert.ToInt64(rightVal));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating / operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
		
		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToInt64(results[0]) / Convert.ToInt64(results[1]));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating / operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}
	
	public class OpNode_ModOperator : OpNode_Lazy_BinOperator, ITriable {// %
		internal OpNode_ModOperator(IOpNodeHolder parent, Node code):base(parent, code) {
		}
		
		internal override object Run(ScriptVars vars) {
			object leftVal = left.Run(vars);
			object rightVal = right.Run(vars);
			try {
				return (Convert.ToDouble(leftVal) % Convert.ToDouble(rightVal));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating % operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
		
		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToDouble(results[0]) % Convert.ToDouble(results[1]));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating % operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}
	
	public class OpNode_BinaryAndOperator : OpNode_Lazy_BinOperator, ITriable {// & as binary operator
		internal OpNode_BinaryAndOperator(IOpNodeHolder parent, Node code):base(parent, code) {
		}
		
		internal override object Run(ScriptVars vars) {
			object leftVal = left.Run(vars);
			object rightVal = right.Run(vars);
			try {
				return (Convert.ToInt64(leftVal) & Convert.ToInt64(rightVal));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating & operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
		
		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToInt64(results[0]) & Convert.ToInt64(results[1]));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating & operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}
	
	public class OpNode_BinaryOrOperator : OpNode_Lazy_BinOperator, ITriable {// & as binary operator
		internal OpNode_BinaryOrOperator(IOpNodeHolder parent, Node code):base(parent, code) {
		}
		
		internal override object Run(ScriptVars vars) {
			object leftVal = left.Run(vars);
			object rightVal = right.Run(vars);
			try {
				return (Convert.ToInt64(leftVal) | Convert.ToInt64(rightVal));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating | operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
		
		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToInt64(results[0]) | Convert.ToInt64(results[1]));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating | operator", 
				this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}
}	