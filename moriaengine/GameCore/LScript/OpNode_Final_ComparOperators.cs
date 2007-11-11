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
	public class OpNode_EqualsOperator : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_EqualsOperator(IOpNodeHolder parent, Node code):base(parent, code) {
		}
		
		internal override object Run(ScriptVars vars) {
			return object.Equals(left.Run(vars), right.Run(vars));
		}
		
		public object TryRun(ScriptVars vars, object[] results) {
			return object.Equals(results[0], results[1]);
		}
	}
	
	public class OpNode_EqualsNotOperator : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_EqualsNotOperator(IOpNodeHolder parent, Node code):base(parent, code) {
		}
		
		internal override object Run(ScriptVars vars) {
			return !object.Equals(left.Run(vars), right.Run(vars));
		}
		
		public object TryRun(ScriptVars vars, object[] results) {
			return !object.Equals(results[0], results[1]);
		}
	}
	
	public class OpNode_EqualityOperator_Double : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_EqualityOperator_Double(IOpNodeHolder parent, Node code):base(parent, code) {
		}
		
		internal override object Run(ScriptVars vars) {
			object leftVar = left.Run(vars);
			object rightVar = right.Run(vars);
			try {
				return (Convert.ToDouble(leftVar) == Convert.ToDouble(rightVar));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating == operator",
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
		
		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToDouble(results[0]) == Convert.ToDouble(results[1]));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating == operator",
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}
	
	public class OpNode_EqualityOperator_Int : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_EqualityOperator_Int(IOpNodeHolder parent, Node code):base(parent, code) {
		}
		
		internal override object Run(ScriptVars vars) {
			object leftVar = left.Run(vars);
			object rightVar = right.Run(vars);
			try {
				return (Convert.ToInt64(leftVar) == Convert.ToInt64(rightVar));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating == operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
		
		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToInt64(results[0]) == Convert.ToInt64(results[1]));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating == operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}
	
	public class OpNode_InEqualityOperator_Double : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_InEqualityOperator_Double(IOpNodeHolder parent, Node code):base(parent, code) {
		}
		
		internal override object Run(ScriptVars vars) {
			object leftVar = left.Run(vars);
			object rightVar = right.Run(vars);
			try {
				return (Convert.ToDouble(leftVar) != Convert.ToDouble(rightVar));
			} catch (InterpreterException) {
				throw;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating != operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
		
		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToDouble(results[0]) != Convert.ToDouble(results[1]));
			} catch (InterpreterException) {
				throw;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating != operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}
	
	public class OpNode_InEqualityOperator_Int : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_InEqualityOperator_Int(IOpNodeHolder parent, Node code):base(parent, code) {
		}
		
		internal override object Run(ScriptVars vars) {
			object leftVar = left.Run(vars);
			object rightVar = right.Run(vars);
			try {
				return (Convert.ToInt64(leftVar) != Convert.ToInt64(rightVar));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating != operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
		
		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToInt64(results[0]) != Convert.ToInt64(results[1]));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating != operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}

	public class OpNode_LessThanOperator_Double : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_LessThanOperator_Double(IOpNodeHolder parent, Node code):base(parent, code) {
		}
		
		internal override object Run(ScriptVars vars) {
			object leftVar = left.Run(vars);
			object rightVar = right.Run(vars);
			try {
				return (Convert.ToDouble(leftVar) < Convert.ToDouble(rightVar));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating < operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
		
		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToDouble(results[0]) < Convert.ToDouble(results[1]));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating < operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}
	
	public class OpNode_GreaterThanOperator_Double : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_GreaterThanOperator_Double(IOpNodeHolder parent, Node code):base(parent, code) {
		}
		
		internal override object Run(ScriptVars vars) {
			object leftVar = left.Run(vars);
			object rightVar = right.Run(vars);
			try {
				return (Convert.ToDouble(leftVar) > Convert.ToDouble(rightVar));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating > operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
		
		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToDouble(results[0]) > Convert.ToDouble(results[1]));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating > operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}
	
	public class OpNode_LessThanOrEqualOperator_Double : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_LessThanOrEqualOperator_Double(IOpNodeHolder parent, Node code):base(parent, code) {
		}
		
		internal override object Run(ScriptVars vars) {
			object leftVar = left.Run(vars);
			object rightVar = right.Run(vars);
			try {
				return (Convert.ToDouble(leftVar) <= Convert.ToDouble(rightVar));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating <= operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
		
		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToDouble(results[0]) <= Convert.ToDouble(results[1]));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating <= operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}
	
	public class OpNode_GreaterThanOrEqualOperator_Double : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_GreaterThanOrEqualOperator_Double(IOpNodeHolder parent, Node code):base(parent, code) {
		}
		
		internal override object Run(ScriptVars vars) {
			object leftVar = left.Run(vars);
			object rightVar = right.Run(vars);
			try {
				return (Convert.ToDouble(leftVar) >= Convert.ToDouble(rightVar));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating >= operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
		
		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToDouble(results[0]) >= Convert.ToDouble(results[1]));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating >= operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}
	
	public class OpNode_LessThanOperator_Int : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_LessThanOperator_Int(IOpNodeHolder parent, Node code):base(parent, code) {
		}
		
		internal override object Run(ScriptVars vars) {
			object leftVar = left.Run(vars);
			object rightVar = right.Run(vars);
			try {
				return (Convert.ToInt64(leftVar) < Convert.ToInt64(rightVar));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating < operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
		
		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToInt64(results[0]) < Convert.ToInt64(results[1]));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating < operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}
	
	public class OpNode_GreaterThanOperator_Int : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_GreaterThanOperator_Int(IOpNodeHolder parent, Node code):base(parent, code) {
		}
		
		internal override object Run(ScriptVars vars) {
			object leftVar = left.Run(vars);
			object rightVar = right.Run(vars);
			try {
				return (Convert.ToInt64(leftVar) > Convert.ToInt64(rightVar));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating > operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
		
		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToInt64(results[0]) > Convert.ToInt64(results[1]));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating > operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}
	
	public class OpNode_LessThanOrEqualOperator_Int : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_LessThanOrEqualOperator_Int(IOpNodeHolder parent, Node code):base(parent, code) {
		}
		
		internal override object Run(ScriptVars vars) {
			object leftVar = left.Run(vars);
			object rightVar = right.Run(vars);
			try {
				return (Convert.ToInt64(leftVar) <= Convert.ToInt64(rightVar));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating <= operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
		
		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToInt64(results[0]) <= Convert.ToInt64(results[1]));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating <= operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}
	
	public class OpNode_GreaterThanOrEqualOperator_Int : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_GreaterThanOrEqualOperator_Int(IOpNodeHolder parent, Node code):base(parent, code) {
		}
		
		internal override object Run(ScriptVars vars) {
			object leftVar = left.Run(vars);
			object rightVar = right.Run(vars);
			try {
				return (Convert.ToInt64(leftVar) >= Convert.ToInt64(rightVar));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating >= operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
		
		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToInt64(results[0]) >= Convert.ToInt64(results[1]));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating >= operator", 
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}
}	


