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
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using EQATEC.Profiler;
using Shielded;
using SteamEngine.Common;
using SteamEngine.Scripting;
using SteamEngine.Scripting.Compilation;
using SteamEngine.Scripting.Interpretation;
using SteamEngine.Scripting.Objects;
using SteamEngine.Transactionality;

namespace SteamEngine {
	public enum FieldValueType : byte {
		Model, Typed, Typeless, ThingDefType
	}

	public interface IFieldValueParser {
		Type HandledType { get; }
		[SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate")]
		bool TryParse(string input, out object retVal);
	}

	public sealed class FieldValue : IUnloadable {
		private static readonly ShieldedDictNc<Type, IFieldValueParser> parsers = new ShieldedDictNc<Type, IFieldValueParser>();

		private readonly string name;
		private readonly FieldValueType fvType;
		private readonly Type type;

		private readonly Shielded<State> shieldedState = new Shielded<State>();

		private struct State {
			internal bool isChangedManually;
			internal bool isSetFromScripts;
			internal bool unloaded;

			internal FieldValueImpl currentImpl;
			internal FieldValueImpl defaultImpl;
		}

		public FieldValue(string name, FieldValueType fvType, Type type, string filename, int line, string value) {
			this.name = name;
			this.fvType = fvType;
			this.type = type;

			this.SetFromScripts(filename, line, value);
		}

		public FieldValue(string name, FieldValueType fvType, Type type, object value) {
			this.name = name;
			this.fvType = fvType;
			this.type = type;

			this.SetFromCode(value);
		}

		public static void Bootstrap() {
			ClassManager.RegisterSupplySubclassInstances<IFieldValueParser>(RegisterParser, false, false);
		}

		public static void RegisterParser(IFieldValueParser parser) {
			Transaction.AssertInTransaction();
			var t = parser.HandledType;
			foreach (var knownType in parsers.Keys) {
				if (t.IsAssignableFrom(knownType) || knownType.IsAssignableFrom(t)) {
					throw new SEException(parser + " is incompatible with " + parsers[knownType] + " as FieldValue parser");
				}
			}
			parsers[t] = parser;
		}

		public static IFieldValueParser GetParserFor(Type type) {
			IFieldValueParser outVal;
			if (parsers.TryGetValue(type, out outVal)) {
				return outVal;
			}
			return null;
		}

		internal static void ForgetScripts() {
			Transaction.AssertInTransaction();
			parsers.Clear();
		}

		public void Unload() {
			Transaction.AssertInTransaction();
			if (this.shieldedState.Value.isChangedManually) {
				this.shieldedState.Modify((ref State s) => s.unloaded = true);
			}
		}

		public bool IsUnloaded => this.shieldedState.Value.unloaded;

		private void ThrowIfUnloaded() {
			if (this.shieldedState.Value.unloaded) {
				throw new UnloadedException("The " + Tools.TypeToString(this.GetType()) + " '" + LogStr.Ident(this.name) + "' is unloaded.");
			}
		}

		public string Name => this.name;

		internal void ResolveTemporaryState() {
			Transaction.AssertInTransaction();
			var tempVi = this.shieldedState.Value.defaultImpl as TemporaryValueImpl;
			if (tempVi == null)
				return;
			try {
				var value = tempVi.valueString;
				object retVal = null;
				if (value != null) {
					if (value.Length > 0) {
						if ((this.fvType == FieldValueType.Typed) && (this.type.IsArray)) {

							if (this.type.GetArrayRank() > 1) {
								throw new SEException("Can't use a multirank array in a FieldValue");
							}
							var sourceArray = Utility.SplitSphereString(value, false); //
							var elemType = this.type.GetElementType();
							var n = sourceArray.Length;
							var resultArray = Array.CreateInstance(elemType, n);

							for (var i = 0; i < n; i++) {
								resultArray.SetValue(ConvertTools.ConvertTo(elemType, this.ResolveSingleValue(tempVi, sourceArray[i], null)), i);
							}

							retVal = resultArray;
						} else {
							retVal = this.ResolveSingleValue(tempVi, value, retVal);
						}
					} else {
						retVal = "";
					}
				}

				var valueDefaultValue = this.GetFittingValueImpl();
				this.shieldedState.Modify((ref State s) => s.defaultImpl = valueDefaultValue);
				valueDefaultValue.Value = retVal;
			} catch (FatalException) {
				throw;
			} catch (TransException) {
				throw;
			} catch (SEException sex) {
				sex.TryAddFileLineInfo(tempVi.filename, tempVi.line);
				throw;
			} catch (Exception e) {
				throw new SEException(tempVi.filename, tempVi.line, e);
			}

			if (!this.shieldedState.Value.isChangedManually) {
				//we were already resynced...the loaded value should not change
				this.shieldedState.Modify((ref State s) => s.currentImpl = s.defaultImpl.Clone());
			}
		}

		private object ResolveSingleValue(TemporaryValueImpl tempVI, string value, object retVal) {
			if (!this.ResolveStringWithoutLScript(value, ref retVal)) {//this is a dirty shortcut to make resolving faster, without it would it last forever
				var statement = string.Concat("return(", value, ")");
				LScriptHolder snippetRunner;
				retVal = LScriptMain.RunSnippet(tempVI.filename, tempVI.line, Globals.Instance, statement, out snippetRunner);
			}
			return retVal;
		}

		private static readonly Regex simpleStringRE = new Regex(@"^""(?<value>[^\<\>]*)""\s*$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes"), SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		private bool ResolveStringWithoutLScript(string value, ref object retVal) {
			switch (this.fvType) {
				case FieldValueType.Typeless:
					if (TryResolveAsString(value, ref retVal)) {
						return true;
					}
					if (TryResolveAsScript(value, ref retVal)) {
						return true;
					}
					if (ConvertTools.TryParseAnyNumber(value, out retVal)) {
						return true;
					}
					if (TryResolveWithExternalParser(null, value, ref retVal)) {
						return true;
					}
					break;
				case FieldValueType.Typed:
					var code = Type.GetTypeCode(this.type);
					switch (code) {
						case TypeCode.Boolean:
							bool b;
							if (ConvertTools.TryParseBoolean(value, out b)) {
								retVal = b;
								return true;
							}
							break;
						case TypeCode.Empty:
						case TypeCode.DateTime:
						case TypeCode.Object:
							if (typeof(AbstractScript).IsAssignableFrom(this.type)) {
								if (TryResolveAsScript(value, ref retVal)) {
									return true;
								}
							}
							if (TryResolveWithExternalParser(this.type, value, ref retVal)) {
								return true;
							}

							break;
						case TypeCode.String:
							var str = value.Trim().Trim('"');
							if (!str.Contains("<") && !str.Contains(">")) {
								retVal = str;
								return true;
							}
							break;
						default: //it's a number
							if (this.type.IsEnum) {
								try {
									var enumStr = value.Replace(this.type.Name + ".", "") // "FieldValueType.Typed | FieldValueType.Typeless" -> "Typed , Typeles"
										.Replace("|", ",").Replace("+", ","); //hopefully the actual value won't change by this optimisation ;)
									retVal = Enum.Parse(this.type, enumStr, true);
									return true;
								} catch { }
							}
							if (ConvertTools.TryParseSpecificNumber(code, value, out retVal)) {
								return true;
							}
							break;
					}
					break;
				case FieldValueType.Model:
					int s;
					if (ConvertTools.TryParseInt32(value, out s)) {
						retVal = s;
						return true;
					}
					if (TryResolveAsScript(value, ref retVal)) {
						if (retVal is ThingDef) {
							return true;
						}
					}
					break;
				case FieldValueType.ThingDefType:
					int id;
					if (ConvertTools.TryParseInt32(value, out id)) {
						ThingDef def;
						if (typeof(AbstractItemDef).IsAssignableFrom(this.type)) {
							def = ThingDef.FindItemDef(id);
						} else {
							def = ThingDef.FindCharDef(id);
						}
						if (def != null) {
							retVal = def;
							return true;
						}
					}
					if (TryResolveAsScript(value, ref retVal)) {
						if (retVal is ThingDef) {
							return true;
						}
					}
					break;
			}

			retVal = null;
			return false;
		}

		private static bool TryResolveWithExternalParser(Type returnType, string value, ref object retVal) {
			if (returnType != null) {
				IFieldValueParser parser;
				if (parsers.TryGetValue(returnType, out parser)) {
					return parser.TryParse(value, out retVal);
				}
			}
			foreach (var pair in parsers) {
				if ((returnType == null) || (returnType.IsAssignableFrom(pair.Key))) {
					if (pair.Value.TryParse(value, out retVal)) {
						return true;
					}
				}
			}
			return false;
		}

		internal static bool TryResolveAsString(string value, ref object retVal) {
			var m = simpleStringRE.Match(value);
			if (m.Success) {
				retVal = m.Groups["value"].Value;
				return true;
			}
			return false;
		}

		internal static bool TryResolveAsScript(string value, ref object retVal) {
			value = value.Trim().TrimStart('#');
			var script = AbstractScript.GetByDefname(value);
			if (script != null) {
				retVal = script;
				return true;
			}
			return false;
		}

		[SkipInstrumentation]
		private FieldValueImpl GetFittingValueImpl() {
			switch (this.fvType) {
				case FieldValueType.Typeless:
					return new TypelessValueImpl();
				case FieldValueType.Typed:
					return new TypedValueImpl(this.type);
				case FieldValueType.ThingDefType:
					return new ThingDefValueImpl(this.type);
				case FieldValueType.Model:
					return new ModelValueImpl();
			}
			throw new SEException("this.fvType out of range. This should not happen.");
		}

		private void SetFromCode(object value) {
			Transaction.AssertInTransaction();

			var state = this.shieldedState.Value;
			Sanity.IfTrueThrow(state.isChangedManually || state.unloaded, "SetFromCode after change/unload? This should never happen.");

			this.shieldedState.Modify((ref State s) => {
				s.defaultImpl = this.GetFittingValueImpl();
				s.defaultImpl.Value = value;
				s.currentImpl = s.defaultImpl.Clone();
				s.unloaded = false;
			});
		}

		public void SetFromScripts(string filename, int line, string value) {
			Transaction.AssertInTransaction();
			this.shieldedState.Modify((ref State s) => {
				if (s.isChangedManually) {
					s.defaultImpl = new TemporaryValueImpl(filename, line, this, value);
				} else {
					s.currentImpl = new TemporaryValueImpl(filename, line, this, value);
					s.defaultImpl = new TemporaryValueImpl(filename, line, this, value);
				}

				s.isSetFromScripts = true;
				s.unloaded = false;
			});
		}

		internal bool ShouldBeSaved() {
			Transaction.AssertInTransaction();
			var state = this.shieldedState.Value;
			if (state.unloaded) {
				return false;
			}
			if (state.isChangedManually) {
				//it was loaded/changed , so it should be also saved :)
				return !CurrentAndDefaultEquals(this.CurrentValue, this.DefaultValue);
			}
			if ((state.currentImpl is TemporaryValueImpl) && (state.defaultImpl is TemporaryValueImpl)) {
				return false; //unresolved, no need of touching
			}
			return !CurrentAndDefaultEquals(this.CurrentValue, this.DefaultValue);
		}

		private static bool CurrentAndDefaultEquals(object a, object b) {
			if ((a is Array) && (b is Array)) { //or should we equal Collection? Arrays should be the typical collection type here tho
				var arrA = (Array) a;
				var arrB = (Array) b;
				var n = arrA.Length;
				if ((n == arrB.Length) &&
					(arrA.GetType().GetElementType() == arrB.GetType().GetElementType())) {
					for (var i = 0; i < n; i++) {
						if (!CurrentAndDefaultEquals(arrA.GetValue(i), arrB.GetValue(i))) {
							return false;
						}
					}
					return true;
				}
				return false;
			}

			return Equals(a, b);
		}

		/// <summary>If true, it has not been set from scripts nor from saves nor manually</summary>
		public bool IsEmptyAndUnchanged {
			get {
				Transaction.AssertInTransaction();
				var state = this.shieldedState.Value;
				if (state.isSetFromScripts || state.isChangedManually) {
					return false;
				}
				if ((state.currentImpl is TemporaryValueImpl) && (state.defaultImpl is TemporaryValueImpl)) {
					return false; //unresolved, no need of touching
				}
				return true;
			}
		}

		public object CurrentValue {
			get {
				this.ThrowIfUnloaded();
				this.ResolveTemporaryState();
				return this.shieldedState.Value.currentImpl.Value;
			}
			set {
				this.ResolveTemporaryState();
				this.shieldedState.Modify((ref State s) => {
					s.currentImpl.Value = value;
					s.unloaded = false;
					s.isChangedManually = true;
				});
			}
		}

		public object DefaultValue {
			get {
				this.ThrowIfUnloaded();
				this.ResolveTemporaryState();
				return this.shieldedState.Value.defaultImpl.Value;
			}
		}

		[SkipInstrumentation]
		private abstract class FieldValueImpl {
			internal abstract object Value { get; set; }
			internal abstract FieldValueImpl Clone();
		}

		private class TemporaryValueImpl : FieldValueImpl {
			internal readonly string filename;
			internal readonly int line;
			internal readonly string valueString;
			private readonly FieldValue holder;

			internal TemporaryValueImpl(string filename, int line, FieldValue holder, string value) {
				this.filename = filename;
				this.line = line;
				this.holder = holder;
				this.valueString = value;
			}

			internal override FieldValueImpl Clone() {
				throw new InvalidOperationException("Should never be called.");
			}

			internal override object Value {
				get {
					throw new InvalidOperationException("Should never be called.");
				}
				set {
					throw new InvalidOperationException("Should never be called.");
				}
			}
		}

		private sealed class ModelValueImpl : FieldValueImpl {

			private readonly Shielded<State> shieldedState;

			private struct State {
				internal ThingDef thingDef;
				internal int model;
			}

			//resolving constructor
			[SkipInstrumentation]
			internal ModelValueImpl() {
				this.shieldedState = new Shielded<State>();
			}

			[SkipInstrumentation]
			private ModelValueImpl(ModelValueImpl copyFrom) {
				var copyFromState = copyFrom.shieldedState.Value;
				this.shieldedState = new Shielded<State>(new State {
					thingDef = copyFromState.thingDef,
					model = copyFrom.shieldedState.Value.model,
				});
			}

			[SkipInstrumentation]
			internal override FieldValueImpl Clone() {
				return new ModelValueImpl(this);
			}

			internal override object Value {
				get {
					Transaction.AssertInTransaction();
					var state = this.shieldedState.Value;
					if (state.thingDef == null) {
						return state.model;
					}
					return state.thingDef.Model;
				}
				set {
					Transaction.AssertInTransaction();
					this.shieldedState.Modify((ref State s) => {
						s.thingDef = value as ThingDef;
						if (s.thingDef == null) {
							s.model = ConvertTools.ToInt32(value);
						} else {
							var holderState = s.thingDef.model.shieldedState.Value;
							if ((holderState.currentImpl == this) || (holderState.defaultImpl == this)) {
								var d = s.thingDef;
								s.thingDef = null;
								throw new ScriptException(LogStr.Ident(d) + " specifies its own defname as its model, could lead to infinite loop...!");
							}
						}
					});
				}
			}
		}

		private class TypedValueImpl : FieldValueImpl {
			protected readonly Type type;
			private readonly Shielded<object> val;

			//resolving constructor
			[SkipInstrumentation]
			internal TypedValueImpl(Type type) {
				this.type = type;
				this.val = new Shielded<object>();
			}

			[SkipInstrumentation]
			protected TypedValueImpl(TypedValueImpl copyFrom) {
				this.type = copyFrom.type;
				this.val = new Shielded<object>(initial: copyFrom.val.Value);
			}

			[SkipInstrumentation]
			internal override FieldValueImpl Clone() {
				return new TypedValueImpl(this);
			}

			private static object GetInternStringIfPossible(object obj) {
				var asString = obj as string;
				if (asString != null) {
					return string.Intern(asString);
				}
				return obj;
			}

			internal override object Value {
				get {
					return this.val.Value;
				}
				set {
					Transaction.AssertInTransaction();
					if (value != null) {
						var sourceType = value.GetType();
						if ((sourceType != this.type) && (this.type.IsArray)) {
							Array sourceArray;
							var elemType = this.type.GetElementType();
							if (sourceType.IsArray) {//we must change the element type
								if (sourceType.GetArrayRank() > 1) {
									throw new SEException("Can't use a multirank array in a FieldValue");
								}
								sourceArray = (Array) value;
							} else if (value is string) {
								sourceArray = Utility.SplitSphereString((string) value, false); //
							} else {
								sourceArray = new[] { value }; //just wrap it in a 1-element array, gets converted in the next step
							}

							var n = sourceArray.Length;
							var resultArray = Array.CreateInstance(elemType, n);
							for (var i = 0; i < n; i++) {
								resultArray.SetValue(
									ConvertSingleValue(elemType, sourceArray.GetValue(i)), i);
							}

							this.val.Value = ConvertTools.ConvertTo(this.type, resultArray); //this should actually do nothing, just for check
							return;
						}
					}
					this.val.Value = ConvertSingleValue(this.type, value);
				}
			}

			private static object ConvertSingleValue(Type type, object value) {
				var valueAsString = value as string;
				if (typeof(AbstractScript).IsAssignableFrom(type) && valueAsString != null) {
					valueAsString = valueAsString.Trim();
					valueAsString = valueAsString.TrimStart('#');
					var script = AbstractScript.GetByDefname(valueAsString);
					if (script != null) {
						return script;
					}
				}
				return GetInternStringIfPossible(ConvertTools.ConvertTo(type, value)); //ConvertTo will throw exception if impossible
			}
		}

		private sealed class TypelessValueImpl : FieldValueImpl {
			private readonly Shielded<object> obj;

			//resolving constructor
			[SkipInstrumentation]
			internal TypelessValueImpl() {
				this.obj = new Shielded<object>();
			}

			[SkipInstrumentation]
			private TypelessValueImpl(TypelessValueImpl copyFrom) {
				this.obj = new Shielded<object>(initial: copyFrom.obj.Value);
			}

			[SkipInstrumentation]
			internal override FieldValueImpl Clone() {
				return new TypelessValueImpl(this);
			}

			internal override object Value {
				get {
					return this.obj.Value;
				}
				set {
					Transaction.AssertInTransaction();
					var asString = value as string;
					if (asString != null) {
						this.obj.Value = string.Intern(asString);
					} else {
						this.obj.Value = value;
					}
				}
			}
		}

		private sealed class ThingDefValueImpl : TypedValueImpl {
			//resolving constructor
			[SkipInstrumentation]
			internal ThingDefValueImpl(Type type)
				: base(type) {
			}

			[SkipInstrumentation]
			private ThingDefValueImpl(ThingDefValueImpl copyFrom)
				: base(copyFrom) {
			}

			[SkipInstrumentation]
			internal override FieldValueImpl Clone() {
				return new ThingDefValueImpl(this);
			}

			internal override object Value {
				get {
					return base.Value;
				}
				set {
					if (value != null) {
						var td = value as ThingDef;
						if (td == null) {
							if (ConvertTools.IsNumberType(value.GetType())) {
								var id = ConvertTools.ToInt32(value);
								if (typeof(AbstractItemDef).IsAssignableFrom(this.type)) {
									td = ThingDef.FindItemDef(id);
								} else {
									td = ThingDef.FindCharDef(id);
								}
								if (td == null) {
									throw new SEException("There is no Char/ItemDef with model " + id + "(0x" + id.ToString("x") + ")");
								}
								base.Value = td;
								return;
							}
						}
					}
					base.Value = value;
				}
			}
		}
	}
}