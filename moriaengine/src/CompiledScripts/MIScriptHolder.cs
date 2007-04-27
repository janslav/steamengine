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
using System.Reflection;
using SteamEngine;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts { 
	internal class MIScriptHolder {
		//a helper class only now with one(two) static method(s)
		
		internal static ScriptHolder ChooseMIScriptHolder(MethodInfo mi, object codeHolder, string trigName) {
			bool takesScriptArgs=false;			//This determines how arguments are passed to the method.
			bool takesArgs=false;				//Does it even take any args other than 'TagHolder self' at all?
			ParameterInfo[] pi=mi.GetParameters();
			if (pi.Length==1) {
				takesArgs=false;
			} else {
				takesArgs=true;
			}
			//if (pi.Length>=1) {
			//	Type pt=pi[0].ParameterType;
			//	if (pt!=typeof(TagHolder)&&(!pt.IsSubclassOf(typeof(TagHolder)))) {	//Then it's not valid for this!
			//		//added possibility for the 'self' to be tagholder`s subclass -tar
			//		throw new ScriptException("Your "+mi.Name+" method in the "+codeHolder.GetType().Name+" class has an invalid first argument (Of type "+pt.Name+" when it should be a TagHolder) and will not work.");
			//	}
			//}
			if (pi.Length==2) {					//Determine whether it takes 
				Type pt=pi[1].ParameterType;
				if (typeof(ScriptArgs).IsAssignableFrom(pt)) {
					takesScriptArgs=true;
				}
			}
			if (takesArgs) {
				if (takesScriptArgs) {
					//def_foo(object self, ScriptArgs sa) {}
					return new MIScriptHolder_ScriptArgs(mi, codeHolder, trigName);
				} else {
					//def_foo(object self, sometype arg1, sometype arg2, ...) {}
					return new MIScriptHolder_Args(mi, codeHolder, trigName);
				}
			} else {
				//def_foo(object self) {}
				return new MIScriptHolder_NoArgs(mi, codeHolder, trigName);
			}
		}
	}
	
	internal class MIScriptHolder_NoArgs : ScriptHolder {
		private MethodInfo mi;				//Info on the method, we use it to call it, etc.
		private object codeHolder;	//The instance with the actual methods.
		
		internal MIScriptHolder_NoArgs(MethodInfo mi, object codeHolder, string trigName) : base(trigName) {
			this.mi=MemberWrapper.GetWrapperFor(mi);
			this.codeHolder=codeHolder;
		}
		
		private static object[] arg = new object[1];
		//Exception handling of scripts will be done in "Trigger" and "CallFunc".
		public override object Run(object self, ScriptArgs sa) {
			//CompiledScript ignores the ScriptArgs.
			//we know args should be ignored
			arg[0]=self;
			lastRunSuccesful = true;
			try {
				return mi.Invoke(codeHolder, arg);
			} catch (Exception e) {
				lastRunSuccesful = false;
				this.lastRunException = e;
				throw;
			}
		}
	}
	
	internal class MIScriptHolder_ScriptArgs : ScriptHolder {
		private MethodInfo mi;				//Info on the method, we use it to call it, etc.
		private object codeHolder;	//The instance with the actual methods.
		
		internal MIScriptHolder_ScriptArgs(MethodInfo mi, object codeHolder, string trigName) : base(trigName) {
			this.mi=MemberWrapper.GetWrapperFor(mi);
			this.codeHolder=codeHolder;
		}

		private static object[] arg = new object[2];
		//Exception handling of scripts will be done in "Trigger" and "CallFunc".
		public override object Run(object self, ScriptArgs sa) {
			//CompiledScript ignores the ScriptArgs.
			//we know the function wants the tagholder and an array
			arg[0]=self;
			arg[1]=sa;
			lastRunSuccesful = true;
			try {
				return mi.Invoke(codeHolder, arg);
			} catch (Exception e) {
				lastRunSuccesful = false;
				this.lastRunException = e;
				throw;
			}
		}
	}
	
	internal class MIScriptHolder_Args : ScriptHolder {
		private MethodInfo mi;				//Info on the method, we use it to call it, etc.
		private object codeHolder;	//The instance with the actual methods.
		
		internal MIScriptHolder_Args(MethodInfo mi, object codeHolder, string trigName) : base(trigName) {
			this.mi=MemberWrapper.GetWrapperFor(mi);
			this.codeHolder=codeHolder;
		}
		
		//Exception handling of scripts will be done in "Trigger" and "CallFunc".
		public override object Run(object self, ScriptArgs sa) {
			//CompiledScript ignores the ScriptArgs.
			//make a new array and fit 'self' in as the first parameter
			object[] argv = sa.argv;
			object[] args = new object[argv.Length+1];
			args[0]=self;
			for (int a = 0, n = argv.Length; a<n; a++) {
				args[a+1]=argv[a];
			}
			lastRunSuccesful = true;
			try {
				return mi.Invoke(codeHolder, args);
			} catch (Exception e) {
				lastRunSuccesful = false;
				this.lastRunException = e;
				throw;
			}
		}
	}
}		