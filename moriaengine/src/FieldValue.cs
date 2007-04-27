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
using System.Reflection;
using System.IO;
using System.Globalization;
using SteamEngine.Common;

namespace SteamEngine {
	enum FieldValueType : byte {
		Model, Typed, Typeless, ThingDefType
	}
	
	public sealed class FieldValue {
		string name;
		FieldValueType fvType;
		Type type;
		bool changedValue = false;
		
		FieldValueImpl currentValue;
		FieldValueImpl defaultValue;
				
		internal FieldValue(string name, FieldValueType fvType, Type type, string filename, int line, string value) {
			this.name = name;
			this.fvType = fvType;
			this.type = type;
			
			this.SetFromScripts(filename, line, value);
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
							if (ConvertTools.TryParseSphereNumber(value, out retVal)) {
								//this is a dirty shortcut to make resolving faster, without it would it last forever
							} else {
								string statement = string.Concat("return ", value);
								retVal = SteamEngine.LScript.LScript.RunSnippet(
									tempVI.filename, tempVI.line, Globals.Instance, statement);
								if (SteamEngine.LScript.LScript.snippetRunner.ContainsRandomExpression) {
									Logger.WriteWarning(tempVI.filename, tempVI.line, "Only constant values are to be set here, so random expression makes not too much sense...");
								}
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
		
		public void SetFromScripts(string filename, int line, string value) {
			if (changedValue) {
				defaultValue = new TemporaryValueImpl(filename, line, this, value);
			} else {
				currentValue = new TemporaryValueImpl(filename, line, this, value);
				defaultValue = new TemporaryValueImpl(filename, line, this, value);
			}
		}
		
		internal bool ShouldBeSaved() {
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
				return currentValue.Value;
			}
			set {
				currentValue.Value = value;
				changedValue = true;
			}
		}
		
		public object DefaultValue {
			get {
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
		
		private class TemporaryValueImpl : FieldValueImpl{
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

///*
//	This program is free software; you can redistribute it and/or modify
//	it under the terms of the GNU General Public License as published by
//	the Free Software Foundation; either version 2 of the License, or
//	(at your option) any later version.
//
//	This program is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU General Public License for more details.
//
//	You should have received a copy of the GNU General Public License
//	along with this program; if not, write to the Free Software
//	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//	Or visit http://www.gnu.org/copyleft/gpl.html
//*/
//
//using System;
//using System.Collections;
//using System.Reflection;
//using System.IO;
//using System.Globalization;
//using SteamEngine.Common;
//
//namespace SteamEngine {
//	public abstract class FieldValue {
//		public readonly string name;
//		public readonly Type type;
//		protected object[] value;
//		protected bool[] unresolved;
//		const int Current = 0;
//		const int Default = 1;
//		
//		internal FieldValue(FieldValue copyFrom) {
//			this.name = copyFrom.name;
//			this.type = copyFrom.type;
//			this.value = new object[2];
//			this.unresolved = new bool[2];
//			this.value[Current] = copyFrom.value[Current];
//			this.value[Default] = copyFrom.value[Default];
//			this.unresolved[Current] = copyFrom.unresolved[Current];
//			this.unresolved[Default] = copyFrom.unresolved[Default];
//		}
//		
//		internal FieldValue(string name, object value, Type type) {
//			this.name = name;
//			this.type = type;
//			this.value = new object[2];
//			this.unresolved = new bool[2];
//			this.unresolved[Current] = false;
//			this.unresolved[Default] = false;
//			CurrentValue = value;
//			this.value[Default] = this.value[Current];
//		}
//		
//		public override string ToString() {
//			return "FieldValue(Name="+name+" Type="+type+" CurrentValue="+value[0]+" DefaultValue="+value[1]+")";
//		}
//		
//		protected object ResolveValue(int which) {
//			Sanity.IfTrueThrow(!(this.value[which] is string), "ResolveValue was called, but the value to resolve wasn't a string, it was a "+this.value[which].GetType().ToString()+"!");
//			
//			string val = (string) this.value[which];
//			
//			string[] arr = Utility.SplitSphereString(val);
//			if (arr.Length > 1) {
//				return arr;
//			}
//		
//			return Constant.GetWhateverThisIs(val);
//		}
//		
//		protected virtual object ConvertResolvedValue(object value) {
//			if (value is Constant) return value;
//			Type objType = value.GetType();
//			if (type.IsAssignableFrom(objType)) {
//				return value;
//			} else if (type.IsSubclassOf(typeof(Array))) {
//				Type wantedElType = type.GetElementType();
//			
//				if (objType.IsSubclassOf(typeof(Array))) {
//					Type currentElType = objType.GetElementType();
//					Array srcArr = (Array) value;
//					int n = srcArr.Length;
//					Array retVal = Array.CreateInstance(wantedElType, n);
//					for (int i = 0; i<n; i++) {
//						retVal.SetValue(
//							TagMath.ConvertTo(wantedElType, srcArr.GetValue(i)), i);
//					}
//					return retVal;
//				} else {//we just create a single-element array
//					object el = TagMath.ConvertTo(wantedElType, value);
//					Array retVal = Array.CreateInstance(wantedElType, 1);
//					retVal.SetValue(el, 0);
//					return retVal;
//				}
//			}
//			return TagMath.ConvertTo(type, value);
//		}
//		
//		protected virtual object GetValue(int which) {
//			if (this.unresolved[which]) {
//				object obj = ResolveValue(which);
//				if (type.IsInstanceOfType(obj)) {
//					this.value[which] = obj;
//				} else {
//					this.value[which] = ConvertResolvedValue(obj);
//				}
//				this.unresolved[which]=false;
//			}
//			return TranslateValue(this.value[which]);
//		}
//		protected virtual object TranslateValue(object value) {
//			if (value is Constant) {
//				value=((Constant)value).Value;
//			}
//			if (type.IsInstanceOfType(value)) {
//				return value;
//			} else {
//				return TagMath.ConvertTo(type, value);
//			}
//		}
//		protected virtual void SetValue(int which, object value) {
//			if (value==null || value.GetType()==type || value is Constant) {
//				this.value[which]=value;
//				this.unresolved[which] = false;
//			} else if (value is string) {
//				string vs = value as string;
//				if (vs.IndexOf('{')>-1) {
//					this.value[which]=null;
//					this.unresolved[which]=false;
//					throw new ScriptException("Invalid "+LogStr.Ident(name)+": "+LogStr.WarningData(value)+" (Non-constant expressions are not allowed here)");
//					//return;
//				}
//				try {
//					this.value[which] = TagMath.ConvertTo(type, value);
//				} catch (FormatException) {	//conversion failed
//					this.value[which] = value;
//					this.unresolved[which]=true;
//				} catch (TagMathException) {	//conversion failed
//					this.value[which] = value;
//					this.unresolved[which]=true;
//				} catch (InvalidCastException) {	//conversion failed
//					this.value[which] = value;
//					this.unresolved[which]=true;
//				} catch (OverflowException oe) {
//					Console.WriteLine("OverflowException in attempt to convert '"+value+"' to '"+type+"': "+oe);
//					Sanity.StackTrace();
//				}
//				
//			
//			} else {
//				this.value[which] = TagMath.ConvertTo(type, value);
//				this.unresolved[which] = false;
//			}
//		}
//		public object CurrentValue {
//			get {
//				return GetValue(Current);
//			}
//			set {
//				SetValue(Current, value);
//			}
//		}
//		
//		public object DefaultValue {
//			get {
//				return GetValue(Default);
//			}
//			set {
//				SetValue(Default, value);
//			}
//		}
//	}
//	
//	//Exists so we can do 'if (fieldValue is NormalFieldValue)' (among other things)
//	internal class NormalFieldValue : FieldValue {
//		internal NormalFieldValue(FieldValue copyFrom) : base(copyFrom) {
//		}
//		internal NormalFieldValue(string name, object value, Type type) : base(name, value, type) {
//		}
//		
//	}
//	
//	internal class ModelFieldValue : FieldValue {
//		internal ModelFieldValue(ModelFieldValue copyFrom) : base(copyFrom) {
//			
//		}
//		
//		internal ModelFieldValue(string name, object value) : base(name, value, typeof(ushort)) {
//		}
//		
//		internal ModelFieldValue(string name, object value, Type type) : base(name, value, type) {
//			Sanity.IfTrueThrow(type!=typeof(ushort) && !type.IsSubclassOf(typeof(ushort)), "The type passed for a DefFieldValue must derive from or be AbstractDef. "+type.ToString()+" does not (For "+name+")");
//		}
//		/**
//			Can accept:
//			1) A string which can be parsed to a ushort
//			2) A ushort
//			3) Anything else which TagMath can convert to a ushort
//			4) A string holding a constant name or defname of a ThingDef
//			5) A Constant
//			6) A ThingDef
//			
//			The first four are handled by base.SetValue, the last two by this method.
//		*/
//		protected override void SetValue(int which, object value) {
//			if (value==null) {
//				throw new ScriptException("You can't set a ModelFieldValue ("+name+")'s value(s) to null!");
//			}
//			if (value is Constant) {
//				this.value[which]=value;
//				this.unresolved[which]=false;
//			} else if (value is ThingDef) {
//				this.value[which]=value;
//				this.unresolved[which]=false;
//			} else {
//				base.SetValue(which, value);
//			}
//		}
//		protected override object TranslateValue(object value) {
//			if (value is Constant) {
//				return Constant.EvaluateToModel(((Constant)value).Value);
//			} else if (value is ThingDef) {
//				return ((ThingDef)value).Model;
//			} else {
//				return base.TranslateValue(value);
//			}
//		}
//		
//		protected override object ConvertResolvedValue(object obj) {
//			obj=Constant.EvaluateToModel(obj);
//			if (obj==null) {
//				return null;
//			} else if (obj is ushort) {
//				return obj;
//			} else {
//				return base.ConvertResolvedValue(value);
//			}
//		}
//	}
//
//	internal class DefFieldValue : FieldValue {
//		private static Type GDFSType = typeof(AbstractDef);
//		
//		internal DefFieldValue(DefFieldValue copyFrom) : base(copyFrom) {
//			
//		}
//		
//		internal DefFieldValue(string name, object value) : base(name, value, typeof(AbstractDef)) {
//		}
//		internal DefFieldValue(string name, object value, Type type) : base(name, value, type) {
//			Sanity.IfTrueThrow(type!=GDFSType && !type.IsSubclassOf(GDFSType), "The type passed for a DefFieldValue must derive from or be AbstractDef. "+type.ToString()+" does not (For "+name+")");
//		}
//		/**
//			Can accept:
//			1) A string holding a constant name or defname of a AbstractDef
//			2) A Constant
//			3) A AbstractDef
//			
//		*/
//		protected override void SetValue(int which, object value) {
//			if (value==null) {
//				base.SetValue(which, null);
//			} else if (value is Constant) {
//				this.value[which]=value;
//				this.unresolved[which]=false;
//			} else if (value is AbstractDef) {
//				this.value[which]=value;
//				this.unresolved[which]=false;
//			} else {
//				base.SetValue(which, value);
//			}
//		}
//		
//		protected override object TranslateValue(object value) {
//			if (value is Constant) {
//				return ((Constant)value).Value as AbstractDef;
//			} else if (value is AbstractDef) {
//				return ((AbstractDef)value);
//			} else {
//				return base.TranslateValue(value);
//			}
//		}
//		
//		protected override object ConvertResolvedValue(object obj) {
//			if (obj!=null && !type.IsInstanceOfType(obj)) {	//type is AbstractDef
//				if (obj is Constant) {
//					//Constant.EvaluateToDef(((Constant)obj).UnevaluatedValue);
//				} else if (obj is string) {
//					throw new SanityCheckException("This DefFieldValue ("+name+")'s ConvertResolveValue was passed a value that's a string - which means it isn't resolved.");
//				} else if (TagMath.IsNumberType(obj.GetType())) {
//					if (type==typeof(AbstractItemDef)) {
//						obj=ThingDef.FindItemDef((uint)TagMath.ConvertTo(typeof(uint), obj));
//					} else if (type==typeof(AbstractCharacterDef)) {
//						obj=ThingDef.FindCharDef((uint)TagMath.ConvertTo(typeof(uint), obj));
//					} else {
//						throw new SanityCheckException("This DefFieldValue ("+name+") resolved one of its values to a "+obj.GetType()+" ("+obj+"), it was expected to be a "+type.ToString()+".");
//					}
//				} else {
//					throw new SanityCheckException("This DefFieldValue ("+name+") resolved one of its values to a "+obj.GetType()+" ("+obj+"), it was expected to be a "+type.ToString()+".");
//				}
//			}
//			return obj;
//		}
//	}
//}
