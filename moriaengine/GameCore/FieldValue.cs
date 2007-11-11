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
using System.Text.RegularExpressions;
using System.Collections;
using System.Reflection;
using System.IO;
using System.Globalization;
using SteamEngine.Common;

namespace SteamEngine {
	enum FieldValueType : byte {
		Model, Typed, Typeless, ThingDefType
	}
	
	public sealed class FieldValue : IUnloadable {
		string name;
		FieldValueType fvType;
		Type type;
		bool changedValue = false;
		bool unloaded = false;
		
		FieldValueImpl currentValue;
		FieldValueImpl defaultValue;
				
		internal FieldValue(string name, FieldValueType fvType, Type type, string filename, int line, string value) {
			this.name = name;
			this.fvType = fvType;
			this.type = type;

			this.SetFromScripts(filename, line, value);
		}

		internal FieldValue(string name, FieldValueType fvType, Type type, object value) {
			this.name = name;
			this.fvType = fvType;
			this.type = type;

			this.SetFromCode(value);
		}

		public void Unload() {
			if (this.changedValue) {
				unloaded = true;
			}
		}

		public bool IsUnloaded {
			get { return unloaded; }
		}

		private void ThrowIfUnloaded() {
			if (unloaded) {
				throw new UnloadedException("The "+this.GetType().Name+" '"+LogStr.Ident(name)+"' is unloaded.");
			}
		}

		public string Name { get {
			return name;
		} }
		
		internal void ResolveTemporaryState() {
			if (defaultValue is TemporaryValueImpl) {
				FieldValueImpl wasCurrent = currentValue;
				FieldValueImpl wasDefault = defaultValue;
				bool success = false;
				try {
					//first, resolve the default value using lscript
					TemporaryValueImpl tempVI = (TemporaryValueImpl) defaultValue;
					string value = tempVI.value;
					object retVal = null;
					if (value != null) {
						if (value.Length > 0) {
							//if ((type != null) && ((ConvertTools.IsNumberType(type)) || (fvType == FieldValueType.ThingDefType) || (fvType == FieldValueType.Model))
							if (!ResolveStringWithoutLScript(value, ref retVal)) {//this is a dirty shortcut to make resolving faster, without it would it last forever
								string statement = string.Concat("return ", value);
								retVal = SteamEngine.LScript.LScript.RunSnippet(
									tempVI.filename, tempVI.line, Globals.Instance, statement);
							}
						} else {
							retVal = "";
						}
					}
	
					try {
						defaultValue = ResolveTemporaryValueImpl();
						defaultValue.Value = retVal;
					} catch (SEException sex) {
						sex.TryAddFileLineInfo(tempVI.filename, tempVI.line);
						throw;
					} catch (Exception e) {
						throw new SEException(tempVI.filename, tempVI.line, e);
					}

					
					if (!changedValue) {//we were already resynced...the loaded value should not change
						currentValue = defaultValue.Clone();
					}

					success = true;
					return;
				} finally {
					if (!success) {
						currentValue = wasCurrent;
						defaultValue = wasDefault;
					}
				}
			}
		}


		public static Regex simpleStringRE= new Regex(@"^""(?<value>[^\<\>]*)""\s*$",
			RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);

		private bool ResolveStringWithoutLScript(string value, ref object retVal) {
			switch (this.fvType) {
				case FieldValueType.Typeless:
					if (TryResolveAsString(value, ref retVal)) {
						return true;
					} else if (TryResolveAsScript(value, ref retVal)) {
						return true;
					} else if (ConvertTools.TryParseAnyNumber(value, out retVal)) {
						return true;
					}
					break;
				case FieldValueType.Typed:
					TypeCode code = Type.GetTypeCode(this.type);
					switch (code) {
						case TypeCode.Empty:
						case TypeCode.DateTime:
							break;
						case TypeCode.Boolean:
							bool b;
							if (ConvertTools.TryParseBoolean(value, out b)) {
								retVal = b;
								return true;
							}
							break;
						case TypeCode.Object:
							if (typeof(AbstractScript).IsAssignableFrom(type)) {
								if (TryResolveAsScript(value, ref retVal)) {
									return true;
								}
							}
							break;
						case TypeCode.String:
							string str = value.Trim().Trim('"');
							if (!str.Contains("<") || !str.Contains(">")) {
								retVal = str;
								return true;
							}
							break;
						default: //it's a number
							if (ConvertTools.TryParseSpecificNumber(code, value, out retVal)) {
								return true;
							}
							break;
					}
					break;
				case FieldValueType.Model:
					short s;
					if (ConvertTools.TryParseInt16(value, out s)) {
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
					uint id;
					if (ConvertTools.TryParseUInt32(value, out id)) {
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

		internal static bool TryResolveAsString(string value, ref object retVal) {
			Match m = simpleStringRE.Match(value);
			if (m.Success) {
				retVal = m.Groups["value"].Value;
				return true;
			}
			return false;
		}

		internal static bool TryResolveAsScript(string value, ref object retVal) {
			value = value.Trim().TrimStart('#');
			AbstractScript script = AbstractScript.Get(value);
			if (script != null) {
				retVal = script;
				return true;
			}
			return false;
		}
		
		private FieldValueImpl ResolveTemporaryValueImpl() {
			switch (this.fvType) {
				case FieldValueType.Typeless:
					return new TypelessValueImpl();
				case FieldValueType.Typed:
					return new TypedValueImpl(type);
				case FieldValueType.ThingDefType:
					return new ThingDefValueImpl(type);
				case FieldValueType.Model:
					return new ModelValueImpl();
			}
			throw new Exception("This can never happen...I hope");
		}

		private void SetFromCode(object value) {
			Sanity.IfTrueThrow((changedValue || unloaded), "SetFromCode after change/unload? This should never happen.");

			defaultValue = ResolveTemporaryValueImpl();
			defaultValue.Value = value;
			currentValue = defaultValue.Clone();
			unloaded = false;
		}
		
		public void SetFromScripts(string filename, int line, string value) {
			if (changedValue) {
				defaultValue = new TemporaryValueImpl(filename, line, this, value);
			} else {
				currentValue = new TemporaryValueImpl(filename, line, this, value);
				defaultValue = new TemporaryValueImpl(filename, line, this, value);
			}
			unloaded = false;
		}
		
		internal bool ShouldBeSaved() {
			if (unloaded) {
				return false;
			}
			if (changedValue) {//it was loaded/changed , so it should be also saved :)
				return !object.Equals(CurrentValue, DefaultValue);
			}
			if ((currentValue is TemporaryValueImpl) && (defaultValue is TemporaryValueImpl)) {
				return false;//unresolved, no need of touching
			}
			return !object.Equals(CurrentValue, DefaultValue);
		}

		public object CurrentValue {
			get {
				ThrowIfUnloaded();
				return currentValue.Value;
			}
			set {
				currentValue.Value = value;
				unloaded = false;
				changedValue = true;
			}
		}
		
		public object DefaultValue {
			get {
				ThrowIfUnloaded();
				return defaultValue.Value;
			}
			//set {
			//	defaultValue.Value = value;
			//	changedValue = true;
			//}
		}

		private abstract class FieldValueImpl {
			internal abstract object Value { get; set; }
			internal abstract FieldValueImpl Clone();
		}
		
		private class TemporaryValueImpl : FieldValueImpl {
			internal string filename;
			internal int line;
			internal string value;
			FieldValue holder;
			
			internal TemporaryValueImpl(string filename, int line, FieldValue holder, string value) {
				this.filename = filename;
				this.line = line;
				this.holder = holder;
				this.value = value;
			}
			
			internal override FieldValueImpl Clone() {
				throw new InvalidOperationException("this is not supposed to be cloned");
			}
			
			internal override object Value { 
				get {
					holder.ResolveTemporaryState();
					if (holder.currentValue == this) {
						return holder.CurrentValue;
					} else {
						return holder.DefaultValue;
					}
				}
				set {
					if (holder.currentValue == this) {
						holder.ResolveTemporaryState();
						holder.CurrentValue = value;
						return;
					}
					throw new InvalidOperationException("Invalid TemporaryValueImpl instance, it's holder is not holding it, or something is setting defaultvalue. This should not happen.");
				}
			}
		}
		
		private class ModelValueImpl : FieldValueImpl {
			ThingDef thingDef;
			ushort model;
			
			//resolving constructor
			internal ModelValueImpl() {
			}
			
			private ModelValueImpl(ModelValueImpl copyFrom) {
				this.thingDef = copyFrom.thingDef;
				this.model = copyFrom.model;
			}
			
			internal override FieldValueImpl Clone() {
				return new ModelValueImpl(this);
			}
			
			internal override object Value { 
				get {
					if (thingDef == null) {
						return model;
					} else {
						return thingDef.Model;
					}
				}
				set {
					thingDef = value as ThingDef;
					if (thingDef == null) {
						model = TagMath.ToUInt16(value);
					} else {
						if ((thingDef.model.currentValue == this)||(thingDef.model.defaultValue == this)) {
							ThingDef d = thingDef;
							thingDef = null;
							throw new ScriptException(LogStr.Ident(d)+" specifies its own defname as its model, could lead to infinite loop...!");
						}
					}
			 	}
			}

		}
		
		private class TypedValueImpl : FieldValueImpl {
			protected Type type;
			object val;
			
			//resolving constructor
			internal TypedValueImpl(Type type) {
				this.type = type;
			}
			
			protected TypedValueImpl(TypedValueImpl copyFrom) {
				this.type = copyFrom.type;
				this.val = copyFrom.val;
			}
			
			internal override FieldValueImpl Clone() {
				return new TypedValueImpl(this);
			}
			
			private static object GetInternStringIfPossible(object obj) {
				string asString = obj as String;
				if (asString != null) {
					return String.Intern(asString);
				}
				return obj;
			}
			
			internal override object Value { 
				get {
					return val;
				}
				set {
					if (type.IsInstanceOfType(value)) {
						this.val = GetInternStringIfPossible(value);
						return;
					} else if (value == null) {
						this.val = TagMath.ConvertTo(type, value);
						return;
					} else if (typeof(AbstractScript).IsAssignableFrom(type) && value is string) {
						string str = (string) value;
						str = str.Trim();
						str = str.TrimStart('#');
						AbstractScript script = AbstractScript.Get(str);
						if (script != null) {
							this.val = script;
							return;
						}
					} else {
						Type objType = value.GetType();
						
						if (type.IsArray) {
							Array arr;
							Array retVal;
							Type elemType = type.GetElementType();
							if (objType.IsArray) {//we must change the element type
								arr = (Array) value;
							} else if (typeof(string).IsAssignableFrom(objType)) {
								arr = Utility.SplitSphereString((string) value);
							} else {
								retVal = Array.CreateInstance(elemType, 1);
								retVal.SetValue(TagMath.ConvertTo(elemType, value),0);
								this.val = TagMath.ConvertTo(type, retVal);
								return;
							}
							
							int n = arr.Length;
							retVal = Array.CreateInstance(elemType, n);
							for (int i = 0; i<n; i++) {
								retVal.SetValue(
									TagMath.ConvertTo(elemType, arr.GetValue(i)), i);
							}

							this.val = TagMath.ConvertTo(type, retVal);//this should actually do nothing, just for check
							return;
						}

					}
					this.val = GetInternStringIfPossible(TagMath.ConvertTo(type, value));
				}
			}
		}
		
		private class TypelessValueImpl : FieldValueImpl {
			object obj;
			
			//resolving constructor
			internal TypelessValueImpl() {
			}
			
			private TypelessValueImpl(TypelessValueImpl copyFrom) {
				this.obj = copyFrom.obj;
			}
			
			internal override FieldValueImpl Clone() {
				return new TypelessValueImpl(this);
			}
			
			internal override object Value { 
				get {
					return obj;
				}
				set {
					string asString = value as String;
					if (asString != null) {
						this.obj = String.Intern(asString);
					} else {
						this.obj = value;
					}
				}
			}
		}
		
		private class ThingDefValueImpl : TypedValueImpl {
			//resolving constructor
			internal ThingDefValueImpl(Type type) : base(type) {
			}
			
			protected ThingDefValueImpl(ThingDefValueImpl copyFrom) : base(copyFrom) {
			}
			
			internal override FieldValueImpl Clone() {
				return new ThingDefValueImpl(this);
			}
			
			internal override object Value { 
				get {
					return base.Value;
				}
				set {
					if (value != null) {
						ThingDef td = value as ThingDef;
						if (td == null) {
							if (TagMath.IsNumberType(value.GetType())) {
								uint id = ConvertTools.ToUInt32(value);
								if (typeof(AbstractItemDef).IsAssignableFrom(type)) {
									td = ThingDef.FindItemDef(id);
								} else {
									td = ThingDef.FindCharDef(id);
								}
								if (td == null) {
									throw new SEException("There is no Char/ItemDef with model "+id);
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