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
using System.Text;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Globalization;

namespace SteamEngine {

	//class: MemberWrapper
	//contains static methods that supply class member wrappers
	//by a wrapper i mean something similar to Reflection.MemberInfo, but with only one feature - 
	//invoking/setting/getting methods/fields/properties/constructors.
	//Valuetype parameters are automatically being converted using the System.Convert class.

	public sealed class MemberWrapper {
		private static AssemblyBuilder assembly;
		internal static ModuleBuilder module; //this is needed by the typebuilders
		private static Hashtable methodWrappers;
		private static Hashtable constructorWrappers;
		private static Hashtable fieldWrappers;
		//private static Hashtable propertyWrappers;

		private static Type[] singleObjTypeArr = new Type[] { typeof(object) };
		private static Hashtable convertMethods;
		private static int count = 0;

		static MemberWrapper() {
			methodWrappers = new Hashtable();
			constructorWrappers = new Hashtable();
			fieldWrappers = new Hashtable();
			//propertyWrappers = new Hashtable();
			convertMethods = new Hashtable();
			AssemblyName name = new AssemblyName();
			name.Name = "MemberWrapper Assembly";
			assembly = Thread.GetDomain().DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
			module = assembly.DefineDynamicModule("MemberWrapper Module");
		}

		//method: GetWrapperFor
		//returns instance of a new class dynamically derived from <MethodWrapper>, according to given MethodInfo
		public static MethodInfo GetWrapperFor(MethodInfo method) {
			//#if !OPTIMIZED
			//			return method;
			//#else
			method = method.GetBaseDefinition();//!! keeping the "virtuality"
			MethodWrapper wrapper = methodWrappers[method] as MethodWrapper;
			if (wrapper != null) {
				return wrapper;
			}
			wrapper = MethodWrapper.SpitAndInstantiateWrapperFor(method);
			methodWrappers[method] = wrapper;
			return wrapper;
			//#endif
		}

		//method: GetWrapperFor
		//returns instance of a new class dynamically derived from <ConstructorWrapper>, according to given ConstructorInfo
		public static ConstructorInfo GetWrapperFor(ConstructorInfo constructor) {
			//#if !OPTIMIZED
			//			return constructor;
			//#else
			ConstructorWrapper wrapper = constructorWrappers[constructor] as ConstructorWrapper;
			if (wrapper != null) {
				return wrapper;
			}
			wrapper = ConstructorWrapper.SpitAndInstantiateWrapperFor(constructor);
			constructorWrappers[constructor] = wrapper;
			return wrapper;
			//#endif
		}

		//method: GetWrapperFor
		//returns instance of a new class dynamically derived from <FieldWrapper>, according to given FieldInfo
		public static FieldInfo GetWrapperFor(FieldInfo field) {
			//#if !OPTIMIZED
			//			return field;
			//#else
			FieldWrapper wrapper = fieldWrappers[field] as FieldWrapper;
			if (wrapper != null) {
				return wrapper;
			}
			wrapper = FieldWrapper.SpitAndInstantiateWrapperFor(field);
			fieldWrappers[field] = wrapper;
			return wrapper;
			//#endif
		}

		internal static MethodInfo GetConvertMethod(Type t) {
			MethodInfo mi = (MethodInfo) convertMethods[t];
			if (mi == null) {
				string methodName = "To" + t.Name;
				mi = typeof(Convert).GetMethod(methodName, singleObjTypeArr);
				if (mi != null) {
					convertMethods[t] = mi;
				}
			}
			return mi;
		}

		internal static void EmitPushParams(ILGenerator il, ParameterInfo[] parameters, int argsAt) {
			for (int i = 0, n = parameters.Length; i < n; i++) {
				Type pt = parameters[i].ParameterType;
				EmitPushArgument(il, argsAt);//Ldarg
				EmitPushInt32(il, i);//Ldc_I4
				il.Emit(OpCodes.Ldelem_Ref); //push the indexed value
				EmitConvertOrUnBox(il, pt);
			}
		}

		internal static void EmitPushParams(ILGenerator il, Type[] types, int argsAt) {
			for (int i = 0, n = types.Length; i < n; i++) {
				Type pt = types[i];
				EmitPushArgument(il, argsAt);//Ldarg
				EmitPushInt32(il, i);//Ldc_I4
				il.Emit(OpCodes.Ldelem_Ref); //push the indexed value
				EmitConvertOrUnBox(il, pt);
			}
		}

		internal static void EmitConvertOrUnBox(ILGenerator il, Type type) {
			if (type.IsValueType) {//we must first get a MethodInfo of converting method in Convert class
				if (type.IsEnum) {
					type = Enum.GetUnderlyingType(type);
				}
				MethodInfo convertMethod = MemberWrapper.GetConvertMethod(type);
				if (convertMethod != null) {
					il.EmitCall(OpCodes.Call, convertMethod, null);
				} else {//there is no converting method
					il.Emit(OpCodes.Unbox, type);//unbox
					EmitLoadIndirectly(il, type);//Ldind
				}
			} else {
				il.Emit(OpCodes.Castclass, type);
			}
		}

		internal static void EmitLoadIndirectly(ILGenerator il, Type type) {
			switch (Type.GetTypeCode(type)) {
				case TypeCode.SByte:
					il.Emit(OpCodes.Ldind_I1); break;
				case TypeCode.Int16:
					il.Emit(OpCodes.Ldind_I2); break;
				case TypeCode.Int32:
					il.Emit(OpCodes.Ldind_I4); break;
				case TypeCode.Int64:
					il.Emit(OpCodes.Ldind_I8); break;
				case TypeCode.Single:
					il.Emit(OpCodes.Ldind_R4); break;
				case TypeCode.Double:
					il.Emit(OpCodes.Ldind_R8); break;
				case TypeCode.Byte:
					il.Emit(OpCodes.Ldind_U1); break;
				case TypeCode.UInt16:
					il.Emit(OpCodes.Ldind_U2); break;
				case TypeCode.UInt32:
					il.Emit(OpCodes.Ldind_U4); break;
				default:
					if (type.IsValueType) {
						il.Emit(OpCodes.Ldobj, type);
					} else {
						il.Emit(OpCodes.Ldind_Ref, type);
					}
					break;
			}
		}

		internal static void EmitPushArgument(ILGenerator il, int iToPush) {
			switch (iToPush) {
				case 0:
					il.Emit(OpCodes.Ldarg_0); break;
				case 1:
					il.Emit(OpCodes.Ldarg_1); break;
				case 2:
					il.Emit(OpCodes.Ldarg_2); break;
				case 3:
					il.Emit(OpCodes.Ldarg_3); break;
				default:
					il.Emit(OpCodes.Ldarg_S, iToPush); break;
			}
		}

		internal static void EmitPushInt32(ILGenerator il, int iToPush) {
			switch (iToPush) {
				case -1:
					il.Emit(OpCodes.Ldc_I4_M1); break;
				case 0:
					il.Emit(OpCodes.Ldc_I4_0); break;
				case 1:
					il.Emit(OpCodes.Ldc_I4_1); break;
				case 2:
					il.Emit(OpCodes.Ldc_I4_2); break;
				case 3:
					il.Emit(OpCodes.Ldc_I4_3); break;
				case 4:
					il.Emit(OpCodes.Ldc_I4_4); break;
				case 5:
					il.Emit(OpCodes.Ldc_I4_5); break;
				case 6:
					il.Emit(OpCodes.Ldc_I4_6); break;
				case 7:
					il.Emit(OpCodes.Ldc_I4_7); break;
				case 8:
					il.Emit(OpCodes.Ldc_I4_8); break;
				default:
					il.Emit(OpCodes.Ldc_I4, iToPush); break;
			}
		}

		internal static void EmitCall(ILGenerator il, MethodInfo method) {
			if (method.IsVirtual && !method.IsFinal && !method.DeclaringType.IsSealed) {
				il.Emit(OpCodes.Callvirt, method);
			} else {
				il.Emit(OpCodes.Call, method);
			}
		}

		internal static void EmitReturn(ILGenerator il, Type type) {
			if (type == typeof(void)) {
				il.Emit(OpCodes.Ldnull);//push null
			} else if (type.IsValueType) {
				il.Emit(OpCodes.Box, type);
			}
			il.Emit(OpCodes.Ret);
		}

		internal static string EscapeTypeName(string str) {
			str = str.Replace("\\", "\\\\");
			str = str.Replace(",", "\\,");
			str = str.Replace("[", "\\[");
			str = str.Replace("]", "\\]");
			return str;
		}



		//	
		//	//class: ConstructorWrapper
		//	//can instantiate new objects
		public abstract class ConstructorWrapper : ConstructorInfo {
			private ConstructorInfo constructorInfo;

			public override RuntimeMethodHandle MethodHandle {
				get {
					return constructorInfo.MethodHandle;
				}
			}
			public override MethodAttributes Attributes {
				get {
					return constructorInfo.Attributes;
				}
			}
			public override string Name {
				get {
					return constructorInfo.Name;
				}
			}
			public override Type DeclaringType {
				get {
					return constructorInfo.DeclaringType;
				}
			}
			public override Type ReflectedType {
				get {
					return constructorInfo.ReflectedType;
				}
			}
			public override ParameterInfo[] GetParameters() {
				return constructorInfo.GetParameters();
			}
			public override MethodImplAttributes GetMethodImplementationFlags() {
				return constructorInfo.GetMethodImplementationFlags();
			}
			public override object[] GetCustomAttributes(bool inherit) {
				return constructorInfo.GetCustomAttributes(inherit);
			}
			public override object[] GetCustomAttributes(Type attributeType, bool inherit) {
				return constructorInfo.GetCustomAttributes(attributeType, inherit);
			}
			public override bool IsDefined(Type attributeType, bool inherit) {
				return constructorInfo.IsDefined(attributeType, inherit);
			}

			private static string GetWrapperClassNameFor(ConstructorInfo ci) {
				StringBuilder sb = new StringBuilder(ci.DeclaringType.Name);
				sb.Append("_").Append("ctor");

				ParameterInfo[] parameters = ci.GetParameters();
				for (int i = 0, n = parameters.Length; i < n; i++) {
					Type pt = parameters[i].ParameterType;
					sb.Append("_").Append(pt.Name);
				}
				sb.Append("_").Append(count++);
				return EscapeTypeName(sb.ToString());
			}

			//Invoke(System.Reflection.BindingFlags ingored1, System.Reflection.Binder ignored2, 
			//	object[] parameters, System.Globalization.CultureInfo ignored3)'

			private static Type[] invokeParamTypes1 = new Type[] {
				typeof(BindingFlags), typeof(Binder), typeof(Object[]), typeof(CultureInfo)};

			//Invoke(object ignored1, System.Reflection.BindingFlags ignored2, System.Reflection.Binder ignored3, 
			//	object[] parameters, System.Globalization.CultureInfo ignored4)'

			private static Type[] invokeParamTypes2 = new Type[] {
				typeof(Object), typeof(BindingFlags), typeof(Binder), typeof(Object[]), typeof(CultureInfo)};

			private static void EmitInvokeMethod(ConstructorInfo constructor, TypeBuilder tb,
					Type[] invokeParamTypes, int paramsAt) {

				MethodBuilder mb = tb.DefineMethod("Invoke", MethodAttributes.Final | MethodAttributes.Public | MethodAttributes.ReuseSlot
					| MethodAttributes.Virtual | MethodAttributes.HideBySig, typeof(Object), invokeParamTypes);
				ILGenerator il = mb.GetILGenerator();

				EmitPushParams(il, constructor.GetParameters(), paramsAt);

				il.Emit(OpCodes.Newobj, constructor);

				EmitReturn(il, constructor.DeclaringType);
			}

			internal static ConstructorWrapper SpitAndInstantiateWrapperFor(ConstructorInfo constructor) {
				TypeBuilder tb = MemberWrapper.module.DefineType(GetWrapperClassNameFor(constructor),
					TypeAttributes.NotPublic, typeof(ConstructorWrapper));

				EmitInvokeMethod(constructor, tb, invokeParamTypes1, 3);
				EmitInvokeMethod(constructor, tb, invokeParamTypes2, 4);

				Type t = tb.CreateType();
				ConstructorWrapper constructed = (ConstructorWrapper) Activator.CreateInstance(t);
				constructed.constructorInfo = constructor;
				return constructed;
			}
		}

		//class: FieldWrapper
		//can get or set a field value
		public abstract class FieldWrapper : FieldInfo {
			private FieldInfo fieldInfo;

			public override RuntimeFieldHandle FieldHandle {
				get {
					return fieldInfo.FieldHandle;
				}
			}
			public override FieldAttributes Attributes {
				get {
					return fieldInfo.Attributes;
				}
			}
			public override string Name {
				get {
					return fieldInfo.Name;
				}
			}
			public override Type DeclaringType {
				get {
					return fieldInfo.DeclaringType;
				}
			}
			public override Type ReflectedType {
				get {
					return fieldInfo.ReflectedType;
				}
			}
			public override Type FieldType {
				get {
					return fieldInfo.FieldType;
				}
			}
			public override object[] GetCustomAttributes(bool inherit) {
				return fieldInfo.GetCustomAttributes(inherit);
			}
			public override object[] GetCustomAttributes(Type attributeType, bool inherit) {
				return fieldInfo.GetCustomAttributes(attributeType, inherit);
			}
			public override bool IsDefined(Type attributeType, bool inherit) {
				return fieldInfo.IsDefined(attributeType, inherit);
			}


			//method: SetValue
			//sets value of a field of an instance. 
			//For static fields, the instance parameter is ignored (should be null). 
			public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture) {
				throw new SEException("You can not set a readonly field.");
			}

			private static Type[] setParamTypes = new Type[] {
				typeof(Object), typeof(Object), typeof(BindingFlags), typeof(Binder), typeof(CultureInfo)};

			//method: GetValue
			//returns value of a field of an instance. 
			//For static fields, the instance parameter is ignored (should be null). 
			//public abstract object GetValue(object obj);
			private static Type[] getParamTypes = new Type[] { typeof(Object) };

			private static string GetWrapperClassNameFor(FieldInfo fi) {
				return EscapeTypeName(String.Concat(
					fi.DeclaringType.Name, "_", fi.Name, "_", (count++).ToString()));
			}

			internal static FieldWrapper SpitAndInstantiateWrapperFor(FieldInfo field) {
				TypeBuilder tb = MemberWrapper.module.DefineType(GetWrapperClassNameFor(field), TypeAttributes.NotPublic, typeof(FieldWrapper));

				//first build the SetValue method
				MethodBuilder mb;
				ILGenerator il;
				Type declaringType = field.DeclaringType;
				Type fieldType = field.FieldType;

				if (!field.IsInitOnly) {//readonly field, no set method
					mb = tb.DefineMethod("SetValue", MethodAttributes.Public | MethodAttributes.ReuseSlot | MethodAttributes.Virtual,
						typeof(void), setParamTypes);
					il = mb.GetILGenerator();

					if (!field.IsStatic) {
						il.Emit(OpCodes.Ldarg_1);//push the "this" object
						EmitConvertOrUnBox(il, declaringType);
						if (declaringType.IsValueType) {
							il.DeclareLocal(declaringType);
							il.Emit(OpCodes.Stloc_0);//set as local 0
							il.Emit(OpCodes.Ldloca_S, 0);//load it's address
						}
					}

					il.Emit(OpCodes.Ldarg_2);//push the argument
					EmitConvertOrUnBox(il, fieldType);

					if (!field.IsStatic) {
						il.Emit(OpCodes.Stfld, field);//set field
					} else {
						il.Emit(OpCodes.Stsfld, field);//set static field
					}

					il.Emit(OpCodes.Ret);//the setter method finished
				}

				mb = tb.DefineMethod("GetValue", MethodAttributes.Public | MethodAttributes.ReuseSlot | MethodAttributes.Virtual,
					typeof(object), getParamTypes);
				il = mb.GetILGenerator();

				if (!field.IsStatic) {
					il.Emit(OpCodes.Ldarg_1);//push the "this" object
					EmitConvertOrUnBox(il, declaringType);
					if (declaringType.IsValueType) {
						il.DeclareLocal(declaringType);
						il.Emit(OpCodes.Stloc_0);//set as local 0
						il.Emit(OpCodes.Ldloca_S, 0);//load it's address
					}
				}

				if (!field.IsStatic) {
					il.Emit(OpCodes.Ldfld, field);//get field
				} else {
					il.Emit(OpCodes.Ldsfld, field);//get static field
				}

				if (fieldType.IsValueType) {
					il.Emit(OpCodes.Box, fieldType);
				}
				il.Emit(OpCodes.Ret);//the getter method finished

				Type t = tb.CreateType();

				FieldWrapper constructed = (FieldWrapper) Activator.CreateInstance(t);
				constructed.fieldInfo = field;

				return constructed;
			}
		}

		//class: MethodWrapper 
		//can invoke a method and return it`s return value
		public abstract class MethodWrapper : MethodInfo {
			private MethodInfo methodInfo;

			public override Type ReturnType {
				get {
					return methodInfo.ReturnType;
				}
			}
			public override ICustomAttributeProvider ReturnTypeCustomAttributes {
				get {
					return methodInfo.ReturnTypeCustomAttributes;
				}
			}
			public override RuntimeMethodHandle MethodHandle {
				get {
					return methodInfo.MethodHandle;
				}
			}
			public override MethodAttributes Attributes {
				get {
					return methodInfo.Attributes;
				}
			}
			public override string Name {
				get {
					return methodInfo.Name;
				}
			}
			public override Type DeclaringType {
				get {
					return methodInfo.DeclaringType;
				}
			}
			public override Type ReflectedType {
				get {
					return methodInfo.ReflectedType;
				}
			}
			public override MethodInfo GetBaseDefinition() {
				return methodInfo.GetBaseDefinition();
			}
			public override ParameterInfo[] GetParameters() {
				return methodInfo.GetParameters();
			}
			public override MethodImplAttributes GetMethodImplementationFlags() {
				return methodInfo.GetMethodImplementationFlags();
			}
			public override object[] GetCustomAttributes(bool inherit) {
				return methodInfo.GetCustomAttributes(inherit);
			}
			public override object[] GetCustomAttributes(Type attributeType, bool inherit) {
				return methodInfo.GetCustomAttributes(attributeType, inherit);
			}
			public override bool IsDefined(Type attributeType, bool inherit) {
				return methodInfo.IsDefined(attributeType, inherit);
			}

			private static string GetWrapperClassNameFor(MethodInfo mi) {
				StringBuilder sb = new StringBuilder(mi.DeclaringType.Name);
				sb.Append("_").Append(mi.Name);

				ParameterInfo[] parameters = mi.GetParameters();
				for (int i = 0, n = parameters.Length; i < n; i++) {
					Type pt = parameters[i].ParameterType;
					sb.Append("_").Append(pt.Name);
				}
				sb.Append("_").Append(count++);
				return EscapeTypeName(sb.ToString());
			}

			//object abstract Invoke(object self, System.Reflection.BindingFlags ignored1, System.Reflection.Binder ignored2, 
			//		object[] pars, System.Globalization.CultureInfo ignored3);

			private static Type[] invokeParamTypes = new Type[] {
				typeof(Object), typeof(BindingFlags), typeof(Binder), typeof(Object[]), typeof(CultureInfo)};

			internal static MethodWrapper SpitAndInstantiateWrapperFor(MethodInfo method) {
				TypeBuilder tb = MemberWrapper.module.DefineType(GetWrapperClassNameFor(method),
					TypeAttributes.NotPublic, typeof(MethodWrapper));
				MethodBuilder mb = tb.DefineMethod("Invoke", MethodAttributes.Final | MethodAttributes.Public | MethodAttributes.ReuseSlot
					| MethodAttributes.Virtual | MethodAttributes.HideBySig, typeof(Object), invokeParamTypes);
				ILGenerator il = mb.GetILGenerator();

				Type declaringType = method.DeclaringType;
				if (!method.IsStatic) {
					il.Emit(OpCodes.Ldarg_1);//push the "this" object
					EmitConvertOrUnBox(il, declaringType);
					if (declaringType.IsValueType) {
						il.DeclareLocal(declaringType);
						il.Emit(OpCodes.Stloc_0);
						il.Emit(OpCodes.Ldloca_S, 0);
					}
				}

				EmitPushParams(il, method.GetParameters(), 4);
				EmitCall(il, method);
				EmitReturn(il, method.ReturnType);

				Type t = tb.CreateType();
				MethodWrapper constructed = (MethodWrapper) Activator.CreateInstance(t);
				constructed.methodInfo = method;
				return constructed;
			}
		}

		//those are disassembled methods so that I know what to do ;)

		//object Invoke(object self, System.Reflection.BindingFlags ignored1, System.Reflection.Binder ignored2, 
		//		object[] pars, System.Globalization.CultureInfo ignored3) {
		//	
		//	((TestClass) self).TestInstanceMethod(Convert.ToInt32(pars[0]), (string) pars[1]);
		//	return null;
		//}
		//L_0000: ldarg.1 				//push self
		//L_0001: castclass SteamEngine.TestClass
		//L_0006: ldarg.s pars  		//push pars
		//L_0008: ldc.i4.0				//push 0
		//L_0009: ldelem.ref			//push array at index
		//L_000a: call int32 [mscorlib]System.Convert::ToInt32(object)
		//L_000f: ldarg.s pars			//push pars
		//L_0011: ldc.i4.1				//push 1
		//L_0012: ldelem.ref			//push array index
		//L_0013: castclass string
		//L_0018: callvirt instance void SteamEngine.TestClass::TestInstanceMethod(int32, string)
		//L_001d: ldnull				//push null
		//L_001e: ret					//return


		//object Invoke(object self, System.Reflection.BindingFlags ignored1, System.Reflection.Binder ignored2, 
		//		object[] pars, System.Globalization.CultureInfo ignored3) {
		//	
		//	//TestInstanceMethod returns type string
		//	return ((TestClass) self).TestInstanceMethod(pars[0]);
		//}
		//L_0000: ldarg.1 
		//L_0001: castclass SteamEngine.TestClass
		//L_0006: ldarg.s pars
		//L_0008: ldc.i4.0 
		//L_0009: ldelem.ref 
		//L_000a: callvirt instance string SteamEngine.TestClass::TestInstanceMethod(object)
		//L_000f: ret 


		//object Invoke(object self, System.Reflection.BindingFlags ignored1, System.Reflection.Binder ignored2, 
		//		object[] pars, System.Globalization.CultureInfo ignored3) {
		//	
		//	//TestInstanceMethod returns type int
		//	return ((TestClass) self).TestInstanceMethod(pars[0]);
		//}
		//L_0000: ldarg.1 
		//L_0001: castclass SteamEngine.TestClass
		//L_0006: ldarg.s pars
		//L_0008: ldc.i4.0 
		//L_0009: ldelem.ref 
		//L_000a: callvirt instance int32 SteamEngine.TestClass::TestInstanceMethod(object)
		//L_000f: box int32				//box the valuetype
		//L_0014: ret 

		//object Invoke(object self, System.Reflection.BindingFlags ignored1, System.Reflection.Binder ignored2, 
		//		object[] pars, System.Globalization.CultureInfo ignored3) {
		//	
		//	//TestStaticMethod returns nothing and accepts an int
		//	TestStaticMethod((int) pars[0]); //for those valuetypes that have no Convert.To... method
		//	return null;
		//}
		//L_0000: ldarg.s pars
		//L_0002: ldc.i4.0 
		//L_0003: ldelem.ref			//push array index
		//L_0004: unbox int32			//unbox
		//L_0009: ldind.i4				//load int on the stack
		//L_000a: call void SteamEngine.TestClass::TestStaticMethod(int32)
		//L_000f: ldnull 
		//L_0010: ret 

		//object Invoke(object self, System.Reflection.BindingFlags ignored1, System.Reflection.Binder ignored2, 
		//		object[] pars, System.Globalization.CultureInfo ignored3) {
		//	
		//	//TestStaticMethod returns nothing and accepts an DateTime
		//	TestStaticMethod((DateTime) pars[0]); //for those valuetypes that have no Convert.To... method
		//	return null;
		//}
		//L_0000: ldarg.s pars
		//L_0002: ldc.i4.0 
		//L_0003: ldelem.ref 
		//L_0004: unbox [mscorlib]System.DateTime
		//L_0009: ldobj [mscorlib]System.DateTime
		//L_000e: call void SteamEngine.TestClass::TestStaticMethod([mscorlib]System.DateTime)
		//L_0013: ldnull 
		//L_0014: ret 

		//object Invoke(object self, System.Reflection.BindingFlags ignored1, System.Reflection.Binder ignored2, 
		//		object[] pars, System.Globalization.CultureInfo ignored3) {
		//	
		//	//TestStaticMethod returns nothing and accepts an decimal
		//	TestStaticMethod((byte) pars[0]); //for those valuetypes that have no Convert.To... method
		//	return null;
		//}
		//L_0000: ldarg.s pars
		//L_0002: ldc.i4.0 
		//L_0003: ldelem.ref 
		//L_0004: unbox unsigned int8
		//L_0009: ldind.u1 
		//L_000a: call void SteamEngine.TestClass::TestStaticMethod(unsigned int8)
		//L_000f: ldnull 
		//L_0010: ret 

		//object Invoke(object self, System.Reflection.BindingFlags ignored1, System.Reflection.Binder ignored2, 
		//		object[] pars, System.Globalization.CultureInfo ignored3) {
		//
		//	//calling method on valuetypes
		//	return ((int)self).ToString();
		//}
		//.locals init (
		//      int32 num1)
		//L_0000: ldarg.1 
		//L_0001: unbox int32
		//L_0006: ldind.i4 
		//L_0007: stloc.0			//set as local 0
		//L_0008: ldloca.s num1		//push addrss of the local
		//L_000a: call instance string int32::ToString()
		//L_000f: ret 

		//object Invoke(object instance) {
		//	//getting field (testclass is reference type)
		//	return ((TestClass) instance).testField;
		//}
		//L_0000: ldarg.1										//push argument 1
		//L_0001: unbox SteamEngine.MemberWrapper/TestClass		//unbox
		//L_0006: ldobj SteamEngine.MemberWrapper/TestClass		//Copies the value type object pointed to by an address to the top of the evaluation stack.
		//L_000b: stloc.0										//sets local 0
		//L_000c: ldloca.s class1								//Loads the address of the local variable at a specific index onto the evaluation stack.
		//L_000e: ldfld unsigned int8 SteamEngine.MemberWrapper/TestClass::testField	//gets field
		//L_0013: box unsigned int8
		//L_0018: ret 

		//object Invoke(object instance) {
		//	//getting field (testclass is value type)
		//	return ((TestClass) instance).testField;
		//}
		//L_0000: ldarg.1 
		//L_0001: castclass SteamEngine.MemberWrapper/TestClass
		//L_0006: ldfld unsigned int8 SteamEngine.MemberWrapper/TestClass::testField
		//L_000b: box unsigned int8
		//L_0010: ret 


		//object Invoke(object instance, object arg) {
		//	//getting field (testclass is reference type)
		//	((TestClass) instance).testField = (byte) arg;
		//	return null;
		//}
		//L_0000: ldarg.1 
		//L_0001: castclass SteamEngine.MemberWrapper/TestClass
		//L_0006: ldarg.2 
		//L_0007: unbox unsigned int8
		//L_000c: ldind.u1 
		//L_000d: stfld unsigned int8 SteamEngine.MemberWrapper/TestClass::testField
		//L_0012: ldnull 
		//L_0013: ret 

		//object Invoke(object instance, object arg) {
		//	//getting field (testclass is value type)
		//	TestClass cast = (TestClass) instance;
		//	cast.testField = (byte) arg;
		//	return null;
		//}
		//L_0000: ldarg.1 
		//L_0001: unbox SteamEngine.MemberWrapper/TestClass
		//L_0006: ldobj SteamEngine.MemberWrapper/TestClass
		//L_000b: stloc.0 
		//L_000c: ldloca.s class1
		//L_000e: ldarg.2 
		//L_000f: unbox unsigned int8
		//L_0014: ldind.u1 
		//L_0015: stfld unsigned int8 SteamEngine.MemberWrapper/TestClass::testField
		//L_001a: ldnull 
		//L_001b: ret 

		//void Test() {
		//	//setting field (testclass is value type)
		//	object instance = new TestClass();
		//	((TestClass) instance).testField = 5;//(byte) argument;
		//}
		//is uncompilable. WTF?? I don't get it. Bug in C# compiler??
	}
}