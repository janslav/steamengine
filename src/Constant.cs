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
using System.Collections;
using SteamEngine.LScript;
using SteamEngine.Common;
using System.Globalization;
using System.Configuration;

namespace SteamEngine {
	
	/**
		just a note: this is not that much true anymore, cos I've reimplemented it 
			but I was too lazy to rewrite this text ;)
			-tar
	
	
		Represents a constant defined in scripts, or a {} (random selection) expression.
		
		This is also used when model={} and the like are written in scripts, though that will not
		register a constant in cases like that.
		
		Most of the time you would want to use Constant.Evaluate(object), or EvaluateToDef, or
		EvaluateToModel. Each can take a Constant, or a string holding the
		name of a constant or def, and may also accept additional types:
		
		Name			Additional types accepted		Returns					When it finds a AbstractDef, returns
		Evaluate		Any types you want				Whatever the type is	Its defname.
		EvaluateToModel	Any numerical types, ThingDef	ushort					Its Model, if it's a ThingDef. If not, it's an error (Currently, other def types don't have models).
		EvaluateToDef	AbstractDef			AbstractDef	The def itself.
		
		EvaluateToDef and EvaluateToModel throw ScriptException if they cannot convert their result
		to a AbstractDef or ushort, respectively.
		
		Additionally, if you have a Constant object, you can simply retrieve their Value, Model, or Def properties,
		which use the above methods. Mind you, you never really need these properties, but they're convenient
		to have if you already have a Constant object. They act the same as the methods above (since they use them).
		
		There are other methods here, but you should not usually need to use them. The most useful of them,
		however, are:
		
		Constant.TranslateRandomStatement:	Translates {} statements (held in a string) and returns
		a new Constant, which won't be registered as an actual Constant, but which can be used
		to get a random value from the statement. The information in the {} is stored in an
		efficient format from which it can rapidly be evaluated and a random value returned, as many
		times as you want to.
		
		Note that Constants which handle random stuff will all internally have references to defs,
		NOT their defnames -- you can still set a Constant to a defname if you want, but the random
		stuff uses defs to be faster.
		
		If you use Constant.Get(string name) and it finds that constant, it'll return the Constant
		object which represents that constant. That's all that does.
		
		Constant.GetValue(string name), simply gets the constant and returns its Value, if it exists.
	*/
	
	public class Constant : IUnloadable {
		private string name;
		private string filename = "<filename>";
		private int line = -1;
		private ConstantValue held;
		private bool unloaded = false;

		public static bool ConstantTracingOn = TagMath.ParseBoolean(ConfigurationManager.AppSettings["Constant Trace Messages"]);
		private static Hashtable byName = new Hashtable(StringComparer.OrdinalIgnoreCase);
		
		//		internal Constant(string filename, int line, string name, object value) : this(name, value) {
		//			this.filename = filename;
		//			this.line = line;
		//		}

		internal Constant(string name, object value) {
			this.held = new NormalConstant(value);
			this.name = name;
		}

		public void Set(object value) {
			this.held = new NormalConstant(value);
			unloaded = false;
		}

		public string Name { get {
			return name;
		} }
		
		public string Filename { get {
			return filename;
		} }
		
		public int Line { get {
			return line;
		} }

		public object Value { get {
			if (unloaded) {
				throw new UnloadedException("The constant '"+name+"' is unloaded.");
			}
			return held.Value;
		} }

		public static object GetValue(string name) {
			Constant def = byName[name] as Constant;
			if (def != null) {
				return def.Value;
			} else {
				throw new SEException("There is no constant called "+name+".");
			}
		}
		
		public static Constant Set(string name, object newValue) {
			Constant def = byName[name] as Constant;
			if (def == null) {
				def = new Constant(name, newValue);
				byName[name] = def;
			} else {
				def.Set(newValue);
			}
			return def;
		}

		public static Constant Get(string name) {
			return (Constant) (byName[name]);
		}
		
		internal static void StartingLoading() {
			
		}
		
		internal static Constant[] Load(PropsSection input)  {
			ArrayList list = new ArrayList();
			string line;
			int linenum = input.headerLine;
			StringReader reader = new StringReader(input.GetTrigger(0).code.ToString());
			while ((line = reader.ReadLine()) != null) {
				linenum++;
				line = line.Trim();
				if ((line.Length==0)||(line.StartsWith("//"))) {
					continue;
				}
				string name, value;
				int spaceAt = line.IndexOf(" ");
				int tabAt = line.IndexOf("\t");
				int equalityAt = line.IndexOf("=");
				int delimiterAt = MinPositive(spaceAt, tabAt, equalityAt);
				if ((delimiterAt == -1) || (delimiterAt >= line.Length))  {
					name = Utility.UnComment(line);
					value = "";
#if DEBUG
					Logger.WriteWarning(input.filename, linenum, "No value of this Constant...?");
#endif
				} else {
					name = line.Substring(0, delimiterAt).Trim();
					value = Utility.UnComment(line.Substring(delimiterAt+1, line.Length-(delimiterAt+1)));
				}
				Constant d = byName[name] as Constant;
				if (d == null) {
					d = new Constant(name, null);
					byName[name] = d;
				} else {
					if (d.unloaded) {
						d.unloaded = false;
					} else {
						Logger.WriteError(input.filename, linenum, "Constant "+LogStr.Ident(name)+" defined multiple times. Ignoring");
						continue;
					}
				}
				d.held = new TemporaryValue(d, value);
				d.filename = input.filename;
				d.line = linenum;
				list.Add(d);
			}

			if (input.TriggerCount>1) {
				Logger.WriteWarning(input.filename, input.headerLine, "Triggers in a definition of constants are nonsensual (and ignored).");
			}
			return ((Constant[]) list.ToArray(typeof(Constant)));
		}

		private static int MinPositive(params int[] numbers) {
			int result = int.MaxValue;
			foreach (int num in numbers) {
				if ((num > 0) && (num < result)) {
					result = num;
				}
			}
			return result;
		}
		
		internal static void LoadingFinished() {
			//dump the number of constants loaded?
		}

		[Summary("This method is called on startup when the resolveEverythingAtStart in steamengine.ini is set to True")]
		public static void ResolveAll() {
			int count = byName.Count;
			Logger.WriteDebug("Resolving "+count+" constants");
			DateTime before = DateTime.Now;
			int a = 0;
			foreach (Constant c in byName.Values) {
				if ((a%20)==0) {
					Logger.SetTitle("Resolving Constants: "+((a*100)/count)+" %");
				}
				if (!c.unloaded) {//those should have already stated what's the problem :)
					TemporaryValue tv = c.held as TemporaryValue;
					if (tv != null) {
						c.ResolveValueFromScript(tv.str);
					}
				}
				a++;
			}
			DateTime after = DateTime.Now;
			Logger.WriteDebug("...took "+(after-before));
			Logger.SetTitle("");
		}

		internal void ResolveValueFromScript(string value) {
			//			//LScript converts it into many params and then fails to find a set method for it :O.
			//			//So we assume here that it has commas but not {}s,
			//			//it MUST be treated as a string (perhaps holding map coordinates, for instance).
			//			//And it'll be the job of the called method to deal with the string it gets passed,
			//			//for now. -SL
			//			//TODO: Consider making commaized numbers be turned into an array and stored. Would that
			//			//require changes to LScript, or how does it recognize an array? Or does it?
			//			//We could bypass it if needed (like the {}-handling code does) and set the value directly.
			//this is not that much valid anymore, but we still need a syntax for array... -tar

			string statement = string.Concat("return ", value);
			Logger.WriteInfo(ConstantTracingOn, "TryRunSnippet(filename("+filename+"), line("+line+"), statement("+statement+"))");
			object retVal = SteamEngine.LScript.LScript.TryRunSnippet(
				filename, line, Globals.instance, statement);
			if (!SteamEngine.LScript.LScript.LastSnippetSuccess) {
				unloaded = true;
				Logger.WriteWarning(filename, line, "No value was set on this ("+this+"): It is now unloaded!");
			} else {
				unloaded = false;
				if (SteamEngine.LScript.LScript.snippetRunner.ContainsRandomExpression) {
					held = new LScriptHolderConstant(SteamEngine.LScript.LScript.snippetRunner);
					SteamEngine.LScript.LScript.snippetRunner = new LScriptHolder();//a bit hackish, yes. sssssh
				} else {
					held = new NormalConstant(retVal);
				}
			}
		}
		
		internal static void UnloadAll() {
			foreach (Constant c in byName.Values) {
				if (c != null) {
					c.Unload();
				}
			}
			byName.Clear();
		}

		public void Unload() {
			unloaded = true;
		} 

		private abstract class ConstantValue {
			internal abstract object Value { get; }
		}
		
		public override string ToString() {
			return held+" "+name;
		}

		private sealed class NormalConstant : ConstantValue {
			private object value;

			internal NormalConstant(object value) {
				this.value = value;
				string asString = value as string;
				if (asString != null) {
					this.value = String.Intern(asString);
				}
			}

			internal override sealed object Value { get {
				return value;
			} }
			
			public override string ToString() {
				return "Constant";
			}
		}

		//for the random expressions
		private sealed class LScriptHolderConstant : ConstantValue {
			private LScriptHolder sh;

			internal LScriptHolderConstant(LScriptHolder sh) {
				this.sh = sh;
			}

			internal override sealed object Value { get {
				return sh.Run(Globals.instance, (ScriptArgs) null);
			} }
			
			public override string ToString() {
				return "RandomConstant";
			}
		}

		private sealed class TemporaryValue : ConstantValue {
			internal string str;
			private Constant holder;

			internal TemporaryValue(Constant holder, string str) {
				this.holder = holder;
				this.str = str;
			}

			internal override sealed object Value { get {
				holder.ResolveValueFromScript(str);
				return holder.Value;
			} }
			
			public override string ToString() {
				return "TemporaryConstant";
			}
		}
	}
}
