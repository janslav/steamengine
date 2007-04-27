using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using SteamEngine;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	
	[AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct)]
	public class HasSavedMembersAttribute : Attribute {
		//no params
	}
	
	[AttributeUsage(AttributeTargets.Field|AttributeTargets.Property)]
	public class SavedMemberAttribute : Attribute {
		//no params
	}
	
	//[HasSavedMembers]
	public static class StaticMemberSaver {
		public class E_StaticMemberSaver_Global : CompiledTriggerGroup {
			public void On_BeforeSave(Globals ignored) {
				SaveMembers();
			}

			public void On_AfterSave(Globals instance) {
				instance.RemoveTag(tkSavedStaticMembersTable);
			}
		}

		//[SavedMember]
		//private static string testField = "teeeeeeeeeeeeeeststriiiiing";

		private static List<MemberInfo> registeredMembers = new List<MemberInfo>();
		private static TagKey tkSavedStaticMembersTable = TagKey.Get("SavedStaticMembersTable");


		private static void LoadMembers() {
			Hashtable table = Globals.Instance.GetTag(tkSavedStaticMembersTable) as Hashtable;
			Globals.Instance.RemoveTag(tkSavedStaticMembersTable);
			
			if (table != null) {
				foreach (DictionaryEntry entry in table) {
					try {
						string[] splitKey = ((string) entry.Key).Split(':');
						Type type = ClassManager.GetType(splitKey[0]);
						if (type != null) {
							MemberInfo[] mis = type.GetMember(splitKey[1], BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);
							if (mis.Length > 0) {
								MemberInfo mi = mis[0];
								Type miReturnType;
								if (IsPropertyOrField(mi, out miReturnType)) {
									object value;
									if (ConvertTools.TryConvertTo(miReturnType, entry.Value, out value)) {
										SetValueToMember(mi, value);
									} else {
										Logger.WriteWarning("Cannot convert loaded type '"+entry.Value.GetType()+"' to type '"+
										miReturnType+"', while loading the static member "+LogStr.Ident(entry.Key));
									}
								} else {
									Logger.WriteWarning("The member '"+LogStr.Ident(entry.Key)+"' is not a field or property, but is being loaded as such...ignoring");
								}
							} else {
								Logger.WriteWarning("Unknown class member '"+LogStr.Ident(splitKey[1])+"' while trying to load static member '"+LogStr.Ident(entry.Key)+"'");
							}

						} else {
							Logger.WriteWarning("Unknown class '"+LogStr.Ident(splitKey[0])+"' while trying to load static member '"+LogStr.Ident(entry.Key)+"'");
						}
					} catch (FatalException) {
						throw;
					} catch (Exception e) {
						Logger.WriteError("Error while loading static members",e);
					}
				}
			}
		}

		private static bool IsPropertyOrField(MemberInfo mi, out Type returnType) {
			if (mi.MemberType == MemberTypes.Property) {
				returnType = ((PropertyInfo) mi).PropertyType;
				return true;
			} else if (mi.MemberType == MemberTypes.Field) {
				returnType = ((FieldInfo) mi).FieldType;
				return true;
			}
			returnType = null;
			return false;
		}

		private static void SetValueToMember(MemberInfo mi, object value) {
			if (mi.MemberType == MemberTypes.Property) {
				((PropertyInfo) mi).SetValue(null, value, null);
			} else if (mi.MemberType == MemberTypes.Field) {
				((FieldInfo) mi).SetValue(null, value);
			}
		}

		private static void SaveMembers() {
			Hashtable table = new Hashtable();
			foreach (MemberInfo mi in registeredMembers) {
				object obj;
				if (mi.MemberType == MemberTypes.Property) {
					obj = ((PropertyInfo) mi).GetValue(null, null);
				} else {
					obj = ((FieldInfo) mi).GetValue(null);
				}
				string key = mi.ReflectedType.Name+":"+mi.Name;
				table[key] = obj;
			}
			Globals.Instance.SetTag(tkSavedStaticMembersTable, table);
		}

		//called by Classmanager at the end of starting process, i.e. when everything is loaded
		public static void Init() {
			//Console.WriteLine("StaticMemberSaver init");
			
			LoadMembers();
			
			registeredMembers.Clear();
			foreach (Type type in ClassManager.allTypesbyName.Values) {
				if (Attribute.IsDefined(type, typeof(HasSavedMembersAttribute))) {
					foreach (MemberInfo mi in type.GetMembers(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static)) {
						TryRegisterMember(mi);
					}
				}
			}
		}

		private static void TryRegisterMember(MemberInfo mi) {
			if (mi.IsDefined(typeof(SavedMemberAttribute), false)) {
				if (mi.MemberType == MemberTypes.Property) {
					PropertyInfo pi = (PropertyInfo) mi;
					if (!pi.CanRead || !pi.CanWrite) {
						Logger.WriteWarning(pi.DeclaringType.Name, pi.Name, "The property must be both writable and readable to be saved and loaded.");
						return;
					}
				}
				registeredMembers.Add(mi);
			}
		}
	}
}