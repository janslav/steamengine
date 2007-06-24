using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using SteamEngine;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	
	[AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct)]
	public class HasSavedMembersAttribute : Attribute {
		//no params
	}
	
	[AttributeUsage(AttributeTargets.Field|AttributeTargets.Property)]
	public class SavedMemberAttribute : Attribute {
		[Remark("The description will be used in settings dialog")]
		private string description;
		private string category;

		public string Description {
			get {
				return description;
			}
		}

		public string Category {
			get {
				return category;
			}
		}
		//no params constructor as default
		public SavedMemberAttribute() {
		}

		[Remark("Allows us to _shortly_ describe the purpose of the member..."+
				"this info will be used for displaying it in 'settings' dialog."+
				"We can also specify the settings category in which this member will be placed.")]
		public SavedMemberAttribute(string desc, string cat) {
			this.description = desc;
			this.category = cat;
		}
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

		[Remark("This generic list will hold all members with attribute SavedMember that are " +
				"used with parametrized string constructor - these members will appear in the " +
				"'settings' dialog so we can cache them for better performance. "+
				"We store the members in the generic dictionary identified by the name - the "+
				"category of their SavedMember attribute. This name identifies a generic List which "+
				"contains the desired members (there can be more than one member for the same name) - "+
				"that logically correspond with each other")]
		private static SettingsCategory[] settingsCategories;
		//private static SortedDictionary<String, List<MemberInfo>> settingsCategories = new SortedDictionary<String, List<MemberInfo>>();


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

		public static void SetValueToMember(MemberInfo mi, object value) {
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

		[Remark("This method will get the generic list of all members that have attribute 'SavedMember' "+
				"used with string-parametrized constructor. We will use them in the "+
				"'settings' dialog.")]
		public static SettingsCategory[] GetMembersForSetting() {
			//if the caching list is empty, try first to get the desired classes
			if (settingsCategories == null || settingsCategories.Length == 0) {
				//temp dictionary for creating the sorted list of settings members
				Dictionary<string, List<SettingsValue>> tempDict = new Dictionary<string, List<SettingsValue>>();
				foreach (MemberInfo mi in registeredMembers) {
					//get instances of the SavedMemberAttribute classes
					object[] attrs = mi.GetCustomAttributes(typeof(SavedMemberAttribute),false);
					if(attrs.Length > 0) {
						SavedMemberAttribute smAttr = (SavedMemberAttribute)attrs[0];
						string desc = smAttr.Description;
						if(desc != null) {
							string category = smAttr.Category;							
							List<SettingsValue> catList = null;
							SettingsValue val = new SettingsValue(desc, mi); //create the one dialog field
							try {
								//try to get the list from the list (if it exists)
								catList = tempDict[category];
							} catch (KeyNotFoundException knfe) {
								//create the list now
								catList = new List<SettingsValue>();
								//add the new list to the temporary dictionary
								tempDict[category] = catList;
							}
							//add the settings value to the list
							catList.Add(val);
						}
					}
				}
				//now the dictionary is filled, time to create the real structure:
				settingsCategories = new SettingsCategory[tempDict.Keys.Count];
				int cntr = 0;
				foreach (string key in tempDict.Keys) {
					List<SettingsValue> lst = tempDict[key];
					//pro kazdy klic vytvorime jednu (virtualni, root) kategorii s polem SettingsValues uvnitr					
					SettingsCategory newCat = new SettingsCategory(key, new AbstractSetting[lst.Count]);
					//kategore je pouze virtualni, vsichni SettingsValuesove uvnitr museji byt staticti jinak to nejde :)
					//jejich hodnoty budou taky dotazeny staticky...
					newCat.Value = null;
					//temporarni seznam nyni nalijeme od praveho pole ktere jsme vytvorili
					AddMembersToCategory(newCat, lst);
					//projdeme si pole memberù v této kategorii a zjistime, zda náhodou není nìkterý složený (to by byla totiž vnoøená kategorie)
					FindInnerCategories(newCat);

					//a pridame novou kategorii do pole
					settingsCategories[cntr++] = newCat;
				}
				//na zaver pole setridime podle nazvu kategorii
				Array.Sort(settingsCategories, delegate(SettingsCategory a, SettingsCategory b) {
					return String.Compare(a.Name, b.Name);
				}
						  );
			}
			return settingsCategories;
		}

		[Remark("Projdeme seznam memberu, nastavime level podle kategorie a konecne ten seznam "+
				"do te kategorie pridame")]
		private static void AddMembersToCategory(SettingsCategory newCat, List<SettingsValue> lst) {
			int i = 0;
			foreach(SettingsValue val in lst) {
				val.Level = newCat.Level + 1; //nastavit level
				val.Parent = newCat; //linknout na rodicovskou kategorii
				newCat.Members[i++] = val; //vlozit do pole
			}						
		}

		[Remark("Search the newly created category's members for those that are composed (contain themselves"+
				"any SavedMembers or SaveableData). If any of these is found, convert them to inner categories."+
				"This check is done recursively on the saved members of the new inner categories."+
				"Second parameter 'parentValue' serves for loading the members value - static fields do not need the "+
				"parent value at all but non-static members in the inner categories (e.g. SaveableDatas) do need their parent SavedMember class's"+
				"(statically loaded) value.")]
		private static void FindInnerCategories(SettingsCategory cat) {
			for(int i = 0; i < cat.Members.Length; i++) {
				SettingsValue setVal = (SettingsValue)cat.Members[i]; //zatim je mozno takto pretypovat, vse je SettingsValue
				object membersValue;
				Type membersType = SettingsUtilities.GetMemberType(setVal.Member, cat.Value, out membersValue);
				setVal.Value = membersValue;//takto dostaneme hodnotu instance tohoto membera
				if(!ObjectSaver.IsSimpleSaveableType(membersType)) {//mame tu slozeny typ?
					//tento slozeny member musi obsahovat vnorene SaveableData ktere budou zobrazeny jako vnitrni kategorie
					//pokud by takove neobsahoval, pak je to chyba a nema co delat v nastaveni !
					List<SettingsValue> innerSetVals = new List<SettingsValue>();
					foreach(MemberInfo innerMi in StaticMemberSaver.GetSaveableDataFromClass(membersType)) {
						innerSetVals.Add(new SettingsValue(StaticMemberSaver.GetSettingDescFor(innerMi), innerMi));
					}
					//nova vnitrni kategorie, jmeno vezmeme stejne jako byl description tohoto SavedMembera co se ukazal byt slozenym
					SettingsCategory innerCat = new SettingsCategory(StaticMemberSaver.GetSettingDescFor(setVal.Member), new AbstractSetting[innerSetVals.Count]);
					innerCat.Value = setVal.Value; //value nove kategorie bude value membera z nejz tuto kategorii delame... 
					innerCat.Level = cat.Level + 1; //bude o jeden level vnorena
					AddMembersToCategory(innerCat, innerSetVals);

					//a touto vnitrni kategorii nahradime membera ktereho jsme v ni pretvorili
					cat.Members[i] = innerCat;

					//a zaroven tuto vnitrni kategorii zkontrolujeme uplne stejnym zpusobem!
					FindInnerCategories(innerCat);
				}
			}
		}

		[Remark("Search through the members of the given class and find those marked with "+
				"'SaveableData' attribute - we will use them as a settings dialog subsection")]
		public static List<MemberInfo> GetSaveableDataFromClass(Type type) {
			List<MemberInfo> retList = new List<MemberInfo>();
			foreach(MemberInfo mi in type.GetMembers()) {
				if(mi.IsDefined(typeof(SaveableDataAttribute), false)) {
					retList.Add(mi);
				}
			}
			return retList;
		}

		[Remark("Get the SavedMember resp. SaveableData attribute and fetch the value of its 'description'"+
				"field. This will be displayed in the 'settings' dialog.")]
		public static string GetSettingDescFor(MemberInfo mi) {
			string retDesc = "";
			//first try to find the SavedMember attribute
			object[] attrs = mi.GetCustomAttributes(typeof(SavedMemberAttribute),false);
			if(attrs.Length > 0) {
				//we know that there will be exactly one SavedMember attribute
				retDesc = ((SavedMemberAttribute)attrs[0]).Description;
			}
			if(retDesc.Equals("")) {
				//still nothing found, try to find the SaveableData attribute...
				attrs = mi.GetCustomAttributes(typeof(SaveableDataAttribute), false);
				if(attrs.Length > 0) {
					//we know that there will be exactly one SavedMember attribute
					retDesc = ((SaveableDataAttribute)attrs[0]).Description;
				}
			}

			//there will always be something returned because we are calling this method only for those
			//MemberInfos that have been chosen to the 'settings dialog'
			return retDesc;
		}
	}
}