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
using System.Timers;
using System.Collections;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Text;
using SteamEngine.Common;

//will be alternative to Timer which runs triggers...
//this runs "hardcoded" methods via TimerDelegate
namespace SteamEngine.Timers {
	public delegate void TimerDelegate(object[] parameters);

	public class DelegateTimer : Timer {
		private TimerDelegate deleg;
		
		protected sealed override void OnTimeout() {
			//Console.WriteLine("OnTimeout on timer "+this);
			Sanity.IfTrueThrow((deleg == null) || (deleg.Target != this.Obj),
				"DelegateTimer.OnTimeout() has null deleg or deleg.Target different from this.Obj");

			deleg(this.args);
		}
		
		public DelegateTimer(TimerKey name) : base(name) {
		}
		
		protected DelegateTimer(DelegateTimer copyFrom, TagHolder assignTo) : base(copyFrom, assignTo) {
			//copying constructor for copying of tagholders
			deleg = (TimerDelegate) Delegate.CreateDelegate(typeof(TimerDelegate), assignTo, copyFrom.deleg.Method);
		}

		protected sealed override Timer Dupe(TagHolder assignTo) {
			return new DelegateTimer(this, assignTo);
		}

		public DelegateTimer(TimerKey name, TimeSpan time, TimerDelegate deleg, params object[] args)
				: base((TagHolder) deleg.Target, name, time, args) {
			this.deleg = deleg;
		}

		internal sealed override void Save(SteamEngine.Persistence.SaveStream output) {
			MethodInfo method = deleg.Method;
			StringBuilder sb = new StringBuilder("deleg=");
			sb.Append(method.DeclaringType.ToString());
			sb.Append(".").Append(method.Name);
			sb.Append("(");
			ParameterInfo[] pars = method.GetParameters();
			if (pars.Length > 0) {
				foreach (ParameterInfo pi in pars) {
					sb.Append(pi.ParameterType.ToString());
					sb.Append(", ");
				}
				sb.Length -= 2;
			}
			sb.Append(")");
			output.WriteLine(sb.ToString());
			base.Save(output);
		}
		
		public static Regex methodSignRE= new Regex(@"^\s*(?<type>[a-zA-Z0-9\.]+)\.(?<method>[a-zA-Z0-9]+)\((([a-zA-Z0-9\.]+)(\,\s*)?)*\)\s*$",                     
			RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);
		
		internal sealed override void LoadLine(string filename, int line, string name, string value) {
			if (name=="deleg") {
				//Console.WriteLine("loading method with string: "+value);
				
				Match m = methodSignRE.Match(value);
				if (m.Success) {
					GroupCollection gc = m.Groups;
					Type type = Type.GetType(gc["type"].Value, true, true); //true true: throw exception, case insensitive
					string methodName = gc["method"].Value;
					CaptureCollection cc = gc[2].Captures;
					int ccCount = cc.Count;
					Type[] paramTypes = new Type[ccCount];
					for (int i = 0; i < ccCount; i++) {
						paramTypes[i] = Type.GetType(cc[i].Value, true, true);
					}
				    MethodInfo mi = type.GetMethod(methodName, paramTypes);
				    if (mi != null) {
						deleg = (TimerDelegate) Delegate.CreateDelegate(typeof(TimerDelegate), this.Obj, mi);
				    } else {
				    	throw new Exception("Unrecognized method.");
				    }
				} else {
					throw new Exception("The value has unparsable format");
				}
				return;
			}
			base.LoadLine(filename, line, name, value);
		}

		public override void LoadObject_Delayed(object resolvedObject, string filename, int line) {
			base.LoadObject_Delayed(resolvedObject, filename, line);
			if ((deleg != null) && (deleg.Target != this.Obj)) {
				deleg = (TimerDelegate) Delegate.CreateDelegate(typeof(TimerDelegate), this.Obj, deleg.Method);
			}
		}
	}
}
