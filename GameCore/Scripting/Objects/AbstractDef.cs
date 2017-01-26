
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Shielded;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Scripting.Compilation;

namespace SteamEngine.Scripting.Objects {
	public abstract class AbstractDef : AbstractScript, ITagHolder {
		static int uids;
		private readonly int uid;

		//keys are both strings and tagkeys
		private readonly ShieldedDictNc<object, FieldValue> fieldValues =
			new ShieldedDictNc<object, FieldValue>(comparer: new GenericComparerDecorator(StringComparer.OrdinalIgnoreCase));

		private readonly string filename;
		private readonly int headerLine;
		private readonly Shielded<string> shieldedAltDefname = new Shielded<string>();
		private bool alreadySaved;

		#region Accessors
		public string Filename => this.filename;

		public int HeaderLine => this.headerLine;

		public override string ToString() {
			//zobrazovat budeme radsi napred defname a pak udaj o typu (v dialozich se to totiz casto nevejde)
			if (string.IsNullOrEmpty(this.Altdefname) || string.IsNullOrEmpty(this.Defname)) {
				return string.Concat("[", this.PrettyDefname, " : ", Tools.TypeToString(this.GetType()), "]");
				//return string.Concat("[", Tools.TypeToString(this.GetType()), " ", this.PrettyDefname, "]");
			}
			return string.Concat("[", this.Defname, "/", this.Altdefname, " : ", Tools.TypeToString(this.GetType()), "]");
			//return string.Concat("[", Tools.TypeToString(this.GetType()), " ", this.Defname, "/", this.shieldedAltDefname, "]");
		}

		public string Filepos {
			get {
				return "line " + this.headerLine + " of " + this.filename;
			}
		}

		public string Altdefname {
			get {
				return this.shieldedAltDefname.Value;
			}
			protected set {
				Shield.AssertInTransaction();
				this.Unregister();
				this.shieldedAltDefname.Value = value;
				this.Register();
			}
		}

		public override string PrettyDefname {
			get {
				//from the two available defnames (like c_0x190 and c_man) it returns the more understandable (c_man)
				if (this.Defname.IndexOf("_0x") > 0) {
					var alt = this.Altdefname;
					if (alt != null) {
						return alt;
					}
				}
				return this.Defname;
			}
		}

		public override bool Equals(object obj) {
			return obj == this;
		}

		public override int GetHashCode() {
			return this.uid;
		}

		public new static AbstractDef GetByDefname(string name) {
			return AbstractScript.GetByDefname(name) as AbstractDef;
		}
		#endregion Accessors

		#region Loading from saves
		internal static void LoadSectionFromSaves(PropsSection input) {
			Shield.AssertInTransaction();
			//todo: a way to load new defs (or just regions)

			string typeName = input.HeaderType;
			string defname = input.HeaderName;

			AbstractDef def = GetByDefname(defname);
			if (def == null) {
				Logger.WriteError(input.Filename, input.HeaderLine, LogStr.Ident(typeName + " " + defname)
					+ " is in the world being loaded, but it was not defined in the scripts. Skipping.");
				return;
			}
			if (!StringComparer.OrdinalIgnoreCase.Equals(def.GetType().Name, typeName)) {
				Logger.WriteWarning(input.Filename, input.HeaderLine,
					LogStr.Ident(typeName + " " + defname) + " declared wrong class. It is in fact " + LogStr.Ident(Tools.TypeToString(def.GetType())) + ".");
			}

			def.LoadFromSaves(input);
		}

		public virtual void LoadFromSaves(PropsSection input) {
			foreach (PropsLine pl in input.PropsLines) {
				ObjectSaver.Load(pl.Value, this.LoadField_Delayed, this.filename, pl.Line,
					pl.Name);
			}
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "filename"), SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		private void LoadField_Delayed(object resolvedObject, string filename, int line, object args) {
			Shield.AssertInTransaction();
			string fieldName = (string) args;
			FieldValue fv;

			if (!this.fieldValues.TryGetValue(fieldName, out fv)) {//that means it's not in scripts
				Match m = TagHolder.tagRE.Match(fieldName);
				if (m.Success) {    //If the name begins with 'tag.'
					string tagName = m.Groups["name"].Value;
					TagKey tk = TagKey.Acquire(tagName);
					tagName = "tag." + tagName;
					fv = new FieldValue(tagName, FieldValueType.Typeless, null, "", -1, "");
					this.fieldValues[tk] = fv;
					this.fieldValues[tagName] = fv;
				} else {
					Logger.WriteWarning("Unknown saved FieldValue line: " + fieldName + " = '" + resolvedObject + "'");
					return;
				}
			}

			if (fv != null) {
				try {
					fv.CurrentValue = resolvedObject;
				} catch (FatalException) {
					throw;
				} catch (TransException) {
					throw;
				} catch (Exception e) {
					Logger.WriteWarning(filename, line, e);
				}
			}
		}
		#endregion Loading from saves

		#region FieldValue methods
		protected FieldValue InitTypedField(string name, object value, Type type) {
			Shield.AssertInTransaction();
			if (typeof(ThingDef).IsAssignableFrom(type)) {
				return this.InitThingDefField(name, value, type);
			}

			FieldValue fieldValue;
			if (!this.fieldValues.TryGetValue(name, out fieldValue)) {
				fieldValue = new FieldValue(name, FieldValueType.Typed, type, value);
				this.fieldValues[name] = fieldValue;
			} else {
				throw new SanityCheckException("InitField_Typed called , when a fieldvalue called " + LogStr.Ident(name) + " already exists.");
			}
			return fieldValue;
		}

		protected FieldValue InitThingDefField(string name, object value, Type type) {
			Shield.AssertInTransaction();
			FieldValue fieldValue;
			if (!this.fieldValues.TryGetValue(name, out fieldValue)) {
				fieldValue = new FieldValue(name, FieldValueType.ThingDefType, type, value);
				this.fieldValues[name] = fieldValue;
			} else {
				throw new SanityCheckException("InitField_Typed called , when a fieldvalue called " + LogStr.Ident(name) + " already exists.");
			}
			return fieldValue;
		}

		protected FieldValue InitModelField(string name, object value) {
			Shield.AssertInTransaction();
			FieldValue fieldValue;
			if (!this.fieldValues.TryGetValue(name, out fieldValue)) {
				fieldValue = new FieldValue(name, FieldValueType.Model, null, value);
				this.fieldValues[name] = fieldValue;
			} else {
				throw new SanityCheckException("InitField_Model called , when a fieldvalue called " + LogStr.Ident(name) + " already exists.");
			}
			return fieldValue;
		}

		protected FieldValue InitTypelessField(string name, object value) {
			Shield.AssertInTransaction();
			FieldValue fieldValue;
			if (!this.fieldValues.TryGetValue(name, out fieldValue)) {
				fieldValue = new FieldValue(name, FieldValueType.Typeless, null, value);
				this.fieldValues[name] = fieldValue;
			} else {
				throw new SanityCheckException("InitField_Typeless called , when a fieldvalue called " + LogStr.Ident(name) + " already exists.");
			}
			return fieldValue;
		}

		protected bool HasFieldValue(string name) {
			return this.fieldValues.ContainsKey(name);
		}

		//public void SetDefaultFieldValue(string name, object value) {
		//    ((FieldValue) this.fieldValues[name]).DefaultValue = value;
		//}

		protected virtual void SetCurrentFieldValue(string name, object value) {
			Shield.AssertInTransaction();
			((FieldValue) this.fieldValues[name]).CurrentValue = value;
		}

		protected object GetDefaultFieldValue(string name) {
			FieldValue fv;
			if (this.fieldValues.TryGetValue(name, out fv)) {
				return fv.DefaultValue;
			}
			return null;
		}

		protected object GetCurrentFieldValue(string name) {
			FieldValue fv;
			if (this.fieldValues.TryGetValue(name, out fv)) {
				return fv.CurrentValue;
			}
			return null;
		}
		#endregion FieldValue methods

		#region Saving
		internal static void SaveAll(SaveStream output) {
			Logger.WriteDebug("Saving defs.");
			output.WriteComment("Defs");

			var all = AllScripts;

			foreach (AbstractScript script in all) {
				AbstractDef def = script as AbstractDef;
				if (def != null) {
					def.alreadySaved = false;
				}
			}
			int count = all.Count;
			int a = 0;
			int countPerCent = count / 200;
			foreach (AbstractScript script in all) {
				AbstractDef def = script as AbstractDef;
				if (def != null) {
					if ((a % countPerCent) == 0) {
						Logger.SetTitle("Saving defs: " + ((a * 100) / count) + " %");
					}
					if ((!def.IsUnloaded) && (!def.alreadySaved)) {
						def.Save(output);
						def.alreadySaved = true;
					}
					a++;
				}
			}
			Logger.SetTitle("");
			output.WriteLine();
			ObjectSaver.FlushCache(output);
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public void Save(SaveStream output) {
			Shield.AssertInTransaction();

			bool headerWritten = false;
			foreach (var entry in this.fieldValues) {
				if (entry.Key is TagKey) { //so that tags dont get written twice
					continue;
				}
				try {
					var fv = entry.Value;
					if (fv.ShouldBeSaved()) {
						if (!headerWritten) {
							output.WriteLine();
							output.WriteSection(this.GetType().Name, this.PrettyDefname);
							headerWritten = true;
						}
						output.WriteValue(fv.Name, fv.CurrentValue);
					}
				} catch (FatalException) {
					throw;
				} catch (TransException) {
					throw;
				} catch (Exception e) {
					Logger.WriteWarning(e);//this should not actually happen, I hope :)
				}
			}
			if (headerWritten) {
				ObjectSaver.FlushCache(output);
			}
		}
		#endregion Saving

		#region Loading from scripts
		private static readonly Type[] constructorParamTypes = { typeof(string), typeof(string), typeof(int) };

		private static readonly ShieldedDictNc<string, ConstructorInfo> constructorsByTypeName =
			new ShieldedDictNc<string, ConstructorInfo>(comparer: StringComparer.OrdinalIgnoreCase);

		public delegate void DefnameParser(PropsSection section, out string defname, out string altdefname);

		private static readonly ShieldedDictNc<Type, DefnameParser> registeredDefnameParsersByType = new ShieldedDictNc<Type, DefnameParser>();
		private static readonly ShieldedDictNc<Type, DefnameParser> inferredDefnameParsersByType = new ShieldedDictNc<Type, DefnameParser>();

		private readonly ShieldedSeqNc<PropsLine> postponedLines = new ShieldedSeqNc<PropsLine>();

		public static Type GetDefTypeByName(string name) {
			Shield.AssertInTransaction();
			ConstructorInfo ctor;
			if (constructorsByTypeName.TryGetValue(name, out ctor)) {
				return ctor.DeclaringType;
			}
			return null;
		}

		public static void RegisterDefnameParser<T>(DefnameParser parserMethod) where T : AbstractDef {
			Shield.AssertInTransaction();
			registeredDefnameParsersByType.Add(typeof(T), parserMethod); //throws in case of duplicity
			inferredDefnameParsersByType.Clear();
		}

		private static DefnameParser GetDefnameParser(Type defType) {
			Shield.AssertInTransaction();
			DefnameParser parserMethod;

			if (inferredDefnameParsersByType.TryGetValue(defType, out parserMethod)) {
				return parserMethod;
			}

			//lazy initialisation. We're looking for the according parser
			Type type = defType;
			while (type != typeof(AbstractDef)) {
				if (registeredDefnameParsersByType.TryGetValue(type, out parserMethod)) {
					inferredDefnameParsersByType.Add(defType, parserMethod);
					return parserMethod;
				}
				type = type.BaseType;
			}

			parserMethod = DefaultDefnameParser;
			inferredDefnameParsersByType.Add(defType, parserMethod);
			return parserMethod;
		}

		public new static void Bootstrap() {
			//all AbstractDef descendants will be registered so that they get loaded by our LoadFromScripts method
			ClassManager.RegisterSupplySubclasses<AbstractDef>(RegisterSubtype);
		}

		public static bool RegisterSubtype(Type defType) {
			Shield.AssertInTransaction();
			if (!defType.IsAbstract) {
				ConstructorInfo ci;
				if (constructorsByTypeName.TryGetValue(defType.Name, out ci)) {
					//we have already a Def type named like that
					throw new OverrideNotAllowedException("Trying to overwrite class " + LogStr.Ident(ci.DeclaringType) +
															" in the register of AbstractDef classes.");
				}
				ci = defType.GetConstructor(constructorParamTypes);
				if (ci != null) {
					constructorsByTypeName[defType.Name] = MemberWrapper.GetWrapperFor(ci);

					ScriptLoader.RegisterScriptType(defType.Name, LoadFromScripts, false);
				}
			}
			return false;
		}

		//called by WorldSaver Load
		public static bool ExistsDefType(string name) {
			return constructorsByTypeName.ContainsKey(name);
		}

		protected AbstractDef(string defname, string filename, int headerLine)
			: base(defname) {
			this.filename = filename;
			this.headerLine = headerLine;
			this.uid = uids++;
		}

		public static IUnloadable LoadFromScripts(PropsSection input) {
			Shield.AssertInTransaction();
			//it is something like this in the .scp file: [headerType headerName] = [WarcryDef a_warcry] etc.
			string typeName = input.HeaderType;
			ConstructorInfo constructor = constructorsByTypeName[typeName];
			Type type = constructor.DeclaringType;

			string defname, altdefname;
			GetDefnameParser(type)(input, out defname, out altdefname);
			defname = string.Intern(string.Concat(defname));
			altdefname = string.Intern(string.Concat(altdefname));

			AbstractScript def = AbstractScript.GetByDefname(defname);
			if (!string.IsNullOrEmpty(altdefname)) {
				AbstractScript defByAltdefname = AbstractScript.GetByDefname(altdefname);
				if (defByAltdefname != null) {
					if (def == null) {
						def = defByAltdefname;
						defByAltdefname = null;
					} else if (defByAltdefname != def) {
						throw new OverrideNotAllowedException("Header defname '" + defname + "' and alternate defname '" + altdefname +
															  "' identify two different objects: "
															  + "'" + def + "' and '" + defByAltdefname + "'. Ignoring.");
					}
				}
			}

			//check if it isn't another type or duplicity
			if (def != null) {
				if (def.GetType() != type) {
					throw new OverrideNotAllowedException(
						"You can not change the class of a Def while resync. You have to recompile or restart to achieve that. Ignoring.");
				}
				if (!def.IsUnloaded) {
					throw new OverrideNotAllowedException(def + " defined multiple times.");
				}
			}

			//construct new or resync
			AbstractDef constructed = (AbstractDef) def;
			if (def == null) {
				object[] cargs = { defname, input.Filename, input.HeaderLine };
				constructed = (AbstractDef) constructor.Invoke(cargs);
			} else {
				//?call some kind of "OnResync" virtual method?
				constructed.UnUnload();
				constructed.Unregister();
			}

			constructed.InternalSetDefname(defname);
			constructed.shieldedAltDefname.Value = altdefname;

			constructed.LoadScriptLines(input);

			constructed.Register();

			return constructed;
		}

		private static void DefaultDefnameParser(PropsSection section, out string defname, out string altdefname) {
			string typeName = section.HeaderType.ToLowerInvariant();
			defname = section.HeaderName.ToLowerInvariant();

			bool defnameIsNum;
			defname = DenumerizeDefname(typeName, defname, out defnameIsNum);

			altdefname = null;
			PropsLine defnameLine = section.TryPopPropsLine("defname");
			if (defnameLine != null) {
				altdefname = defnameLine.Value;
				bool altdefnameIsNum;
				altdefname = DenumerizeDefname(typeName, altdefname, out altdefnameIsNum);

				if (string.Equals(defname, altdefname, StringComparison.OrdinalIgnoreCase)) {
					Logger.WriteWarning("Defname redundantly specified for " + section.HeaderType + " " + LogStr.Ident(defname) + ".");
					altdefname = null;
				}

				//if header name is a number, we put it as alt
				if (defnameIsNum && !altdefnameIsNum) {
					string t = altdefname;
					altdefname = defname;
					defname = t;
				}
			}
		}

		private static string DenumerizeDefname(string typeName, string defname, out bool isNumeric) {
			int defnum;
			isNumeric = false;
			if (ConvertTools.TryParseInt32(defname, out defnum)) {
				defname = defnum.ToString("x", CultureInfo.InvariantCulture);
				defname = string.Concat("_", typeName, "_0x", defname, "_");
				isNumeric = true;
			}
			return defname;
		}


		//register with static dictionaries and lists. 
		//Can be called multiple times without harm
		public override AbstractScript Register() {
			Shield.AssertInTransaction();

			var alt = this.Altdefname;
			if (!string.IsNullOrEmpty(alt)) {
				AbstractScript previous;
				if (AllScriptsByDefname.TryGetValue(alt, out previous)) {
					if (previous != this) {
						throw new SEException("previous != this when registering AbstractScript '" + alt + "'");
					}
				} else {
					AllScriptsByDefname.Add(alt, this);
				}
			}

			return base.Register();
		}

		//unregister from static dictionaries and lists. 
		//Can be called multiple times without harm
		protected override void Unregister() {
			Shield.AssertInTransaction();

			var alt = this.Altdefname;
			if (!string.IsNullOrEmpty(alt)) {
				AbstractScript previous;
				if (AllScriptsByDefname.TryGetValue(alt, out previous)) {
					if (previous != this) {
						throw new SEException("previous != this when unregistering AbstractScript '" + alt + "'. Should not happen.");
					} else {
						AllScriptsByDefname.Remove(alt);
					}
				}
			}

			base.Unregister();
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public virtual void LoadScriptLines(PropsSection ps) {
			foreach (PropsLine p in ps.PropsLines) {
				try {
					string name = p.Name.ToLowerInvariant();

					if (name.StartsWith("tag.") || (this.fieldValues.ContainsKey(name))) {
						this.LoadScriptLine(ps.Filename, p.Line, name, p.Value);
					} else {
						this.postponedLines.Add(p);
					}
				} catch (FatalException) {
					throw;
				} catch (TransException) {
					throw;
				} catch (Exception ex) {
					Logger.WriteWarning(ps.Filename, p.Line, ex);
				}
			}
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "filename")]
		protected virtual void LoadScriptLine(string filename, int line, string param, string args) {
			Shield.AssertInTransaction();

			Match m = TagHolder.tagRE.Match(param);
			FieldValue fieldValue;
			if (m.Success) {    //If the name begins with 'tag.'
				string tagName = m.Groups["name"].Value;
				TagKey tk = TagKey.Acquire(tagName);

				if (!this.fieldValues.TryGetValue(tk, out fieldValue)) {
					tagName = "tag." + tagName;
					fieldValue = new FieldValue(tagName, FieldValueType.Typeless, null, filename, line, args);
					this.fieldValues[tk] = fieldValue;
					this.fieldValues[tagName] = fieldValue;
				} else {
					fieldValue.SetFromScripts(filename, line, args);
				}
				return;
			}

			switch (param) {
				case "category":
				case "subsection":
				case "description":
					return;
				//axis props are ignored. Or shouldnt they? :)
				default:
					if (this.fieldValues.TryGetValue(param, out fieldValue)) {
						fieldValue.SetFromScripts(filename, line, args);
						return;
					}
					throw new ScriptException("Invalid data '" + LogStr.Ident(param) + "' = '" + LogStr.Number(args) + "'.");
			}
		}

		internal static void LoadingFinished() {
			// load postponed lines. That are those which are not initialised by the start of the loading.
			// this is so scripts can load dynamically named defnames, where the dynamic names can depend on not-yet-loaded other scripts
			// this way, for example ProfessionDef definition can list skills and abilities
			foreach (AbstractScript script in AllScripts) {
				AbstractDef def = script as AbstractDef;
				if (def != null) {
					def.LoadPostponedScriptLines();
					def.Trigger_AfterLoadFromScripts();
				}
			}
		}

		private void LoadPostponedScriptLines() {
			foreach (var p in Shield.InTransaction(this.postponedLines.ToList)) {
				try {
					Shield.InTransaction(() =>
						this.LoadScriptLine(this.filename, p.Line, p.Name.ToLowerInvariant(), p.Value));
				} catch (FatalException) {
					throw;
				} catch (TransException) {
					throw;
				} catch (Exception ex) {
					Logger.WriteWarning(this.filename, p.Line, ex);
				}
			}
			Shield.InTransaction(this.postponedLines.Clear);
		}

		private void Trigger_AfterLoadFromScripts() {
			try {
				Shield.InTransaction(this.On_AfterLoadFromScripts);
			} catch (FatalException) {
				throw;
			} catch (TransException) {
				throw;
			} catch (SEException se) {
				se.TryAddFileLineInfo(this.filename, this.headerLine);
				Logger.WriteError(se);
			} catch (Exception e) {
				Logger.WriteError(this.filename, this.headerLine, e);
			}
		}

		protected virtual void On_AfterLoadFromScripts() {
		}

		/// <summary>
		/// Resolves all fieldvalues of all defs
		/// </summary>
		/// <remarks>
		/// This method is called on startup when the resolveEverythingAtStart in steamengine.ini is set to True
		/// </remarks>
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public static void ResolveAll() {
			var all = AllScripts;
			int count = all.Count;
			Logger.WriteDebug("Resolving " + count + " defs");

			DateTime before = DateTime.Now;
			var a = new Shielded<int>();
			int countPerCent = count / 100;
			foreach (var script in all) {
				Shield.InTransaction(() => {
					AbstractDef def = script as AbstractDef;
					if (def != null) {
						if ((a.Value % countPerCent) == 0) {
							Logger.SetTitle("Resolving def field values: " + ((a.Value * 100) / count) + " %");
						}
						if (!def.IsUnloaded) { //those should have already stated what's the problem :)
							foreach (FieldValue fv in def.fieldValues.Values) {
								try {
									fv.ResolveTemporaryState();
								} catch (FatalException) {
									throw;
								} catch (TransException) {
									throw;
								} catch (Exception e) {
									Logger.WriteWarning(e);
								}
							}
						}
						a.Value++;
					}
				});
			}
			DateTime after = DateTime.Now;
			Logger.WriteDebug("...took " + (after - before));
			Logger.SetTitle("");
		}

		public override void Unload() {
			Shield.AssertInTransaction();

			foreach (FieldValue fv in this.fieldValues.Values.ToList()) {
				fv.Unload();
			}

			base.Unload();

			//todo: clear the other various properties...
			//todo: not clear those tags/tgs/timers/whatever that were set dynamically (ie not in scripted defs)
		}

		//unloads instances that come from scripts.
		internal static void ForgetScripts() {
			ForgetAll(); //just to be sure - unregister everything. Should have been be called by Main anyway at this point

			Shield.InTransaction(() => {
				constructorsByTypeName.Clear(); //we assume that inside core there are no non-abstract defs

				//only leave the defnameParsers defined in core (like AbstractSkillDef and ThingDef probably? :)
				inferredDefnameParsersByType.Clear();

				foreach (var t in registeredDefnameParsersByType.Keys.ToList()) {
					if (t.Assembly != ClassManager.CoreAssembly) {
						//not in core
						registeredDefnameParsersByType.Remove(t);
					}
				}
			});
		}
		#endregion Loading from scripts

		#region ITagHolder methods
		public object GetTag(TagKey tk) {
			FieldValue fv;
			if (this.fieldValues.TryGetValue(tk, out fv)) {
				return fv.CurrentValue;
			}
			return null;
		}

		public void SetTag(TagKey tk, object value) {
			Shield.AssertInTransaction();

			FieldValue fv;
			if (!this.fieldValues.TryGetValue(tk, out fv)) {
				string tagName = "tag." + tk;
				fv = new FieldValue(tagName, FieldValueType.Typeless, null, "", -1, "");
				this.fieldValues[tk] = fv;
				this.fieldValues[tagName] = fv;
			}
			fv.CurrentValue = value;
		}

		public bool HasTag(TagKey tk) {
			return this.fieldValues.ContainsKey(tk);
		}

		public void RemoveTag(TagKey tk) {
			Shield.AssertInTransaction();

			FieldValue fv;
			if (this.fieldValues.TryGetValue(tk, out fv)) {
				fv.CurrentValue = null;
			}
		}

		public void ClearTags() {
			Shield.AssertInTransaction();

			foreach (var entry in this.fieldValues) {
				if (entry.Key is TagKey) {
					entry.Value.CurrentValue = null;
				}
			}
		}

		public class GenericComparerDecorator : IEqualityComparer<object> {
			private readonly IEqualityComparer nonGeneriComparer;

			public GenericComparerDecorator(IEqualityComparer nonGeneriComparer) {
				this.nonGeneriComparer = nonGeneriComparer;
			}

			bool IEqualityComparer<object>.Equals(object x, object y) {
				return this.nonGeneriComparer.Equals(x, y);
			}

			int IEqualityComparer<object>.GetHashCode(object obj) {
				return this.nonGeneriComparer.GetHashCode(obj);
			}
		}

		#endregion ITagHolder methods
	}
}
