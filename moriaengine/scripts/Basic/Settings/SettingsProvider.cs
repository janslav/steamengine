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
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts.Dialogs {
	[Summary("Class for accepting and setting all values edited in the info or settings dialogs")]
	public static class SettingsProvider {
		//sotre the used saveable prefixes for particular Types
		private static Dictionary<Type, string> prefixTypes = new Dictionary<Type, string>();

		[Summary("Try to store all edited (changed) fields from the dialog. Store the results in the special list that will be returned for " +
				"displaying")]
		public static List<SettingResult> AssertSettings(Dictionary<int, IDataFieldView> editFields, GumpResponse resp, object target) {
			List<SettingResult> resList = new List<SettingResult>();
			SettingResult oneRes = null;
			foreach (int key in editFields.Keys) {
				IDataFieldView field = (IDataFieldView) editFields[key];
				oneRes = new SettingResult(field, target); //prepare the result
				string newStringValue = resp.GetTextResponse(key); //get the value from the edit field
				if (!typeof(Enum).IsAssignableFrom(field.FieldType) && !newStringValue.Equals(field.GetStringValue(target))) {
					//hnadled type is not Enum and it has changed somehow...
					oneRes.Outcome = SettingsEnums.ChangedOK; //assume it will be OK
					//something has changed - try to make the setting
					try {
						field.SetStringValue(target, newStringValue);
					} catch {
						//setting failed for some reason
						oneRes.Outcome = SettingsEnums.ChangedError; //it wasnt OK... :-(
						//store the attempted string
						oneRes.ErroneousValue = newStringValue;
					}
					resList.Add(oneRes);//add to the list (only if changed somehow)
				} else if (typeof(Enum).IsAssignableFrom(field.FieldType) && SettingsProvider.IsEnumValueChanged(field, target, newStringValue)) {
					//if the field handles the Enum type, we have to check it a slightly different way...
					oneRes.Outcome = SettingsEnums.ChangedOK;
					try {
						//try to set the enumeration value
						field.SetValue(target, Enum.Parse(field.FieldType, newStringValue, true));
					} catch {
						oneRes.Outcome = SettingsEnums.ChangedError;
						oneRes.ErroneousValue = newStringValue;
					}
					resList.Add(oneRes);//add to the list (only if changed somehow)
				} else {
					//nothing changed
					oneRes.Outcome = SettingsEnums.NotChanged;
				}
			}
			return resList;
		}

		[Summary("Look on the outcome value and return the correct color for the dialog")]
		public static Hues ResultColor(SettingResult sres) {
			switch (sres.Outcome) {
				case SettingsEnums.ChangedError:
					return Hues.SettingsFailedColor;
				case SettingsEnums.ChangedOK:
					return Hues.SettingsCorrectColor;
				default:
					return Hues.SettingsNormalColor; //but thos won't happen :)
			}
		}

		[Summary("Count all successfully edited fields")]
		public static int CountSuccessfulSettings(List<SettingResult> results) {
			int resCntr = 0;
			foreach (SettingResult sres in results) {
				if (sres.Outcome == SettingsEnums.ChangedOK) {
					resCntr++;
				}
			}
			return resCntr;
		}

		[Summary("Count all failed fields")]
		public static int CountUnSuccessfulSettings(List<SettingResult> results) {
			int resCntr = 0;
			foreach (SettingResult sres in results) {
				if (sres.Outcome == SettingsEnums.ChangedError) {
					resCntr++;
				}
			}
			return resCntr;
		}

		[Summary("Compare case insensitively the names of enumeration values if they have changed")]
		private static bool IsEnumValueChanged(IDataFieldView field, object target, string newEnumValueName) {
			string oldEnumValuName = Enum.GetName(field.FieldType, field.GetValue(target));
			return !StringComparer.OrdinalIgnoreCase.Equals(oldEnumValuName, newEnumValueName);
		}

		[Summary("Examine the setings value's member type and get its prefix as used in ObjectSaver." +
				"We will use it in the info/settings dialog for displaying and identification")]
		public static string GetValuePrefix(IDataFieldView field, object target) {
			object value = field.GetValue(target);
			string valuePrefix = "";

			Type t = value.GetType();
			//we will store it in the special dictionary
			if (!prefixTypes.TryGetValue(t, out valuePrefix)) {
				//types like Enum, Numbers, String, Regions  or Globals doesn't have any prefixes, they will be displayed as is
				if (t.IsEnum || TagMath.IsNumberType(t) || t.Equals(typeof(String))
					|| typeof(Region).IsAssignableFrom(t) || value == Globals.Instance) {
				} else if (typeof(Thing).IsAssignableFrom(t)) {
					valuePrefix = "#";
				} else if (typeof(AbstractAccount).IsAssignableFrom(t)) {
					valuePrefix = "$";
				} else if (typeof(AbstractScript).IsAssignableFrom(t)) {
					valuePrefix = "#";
				} else {
					//try the simpleimplementors
					ISimpleSaveImplementor iss = ObjectSaver.GetSimpleSaveImplementorByType(t);
					if (iss != null) {
						valuePrefix = iss.Prefix;
					} else {
						valuePrefix = t.Name; //this is the final possibility
					}
				}

				//store the prefix in the dictionary
				prefixTypes[t] = valuePrefix;
			}
			return valuePrefix;
		}

		[Summary("Get the settings values prefix and surround it by brackets." +
				"If the value has no prefix (it is of some special type, find out what" +
				"type it is and return some description of it")]
		public static string GetTypePrefix(Type t) {
			if (t.Equals(typeof(object))) {
				//object is also possible ! - the member can be set to any value
				return "(Obj)";
			} else if (t.IsEnum) {
				return "(Enum)";
			} else if (TagMath.IsNumberType(t)) {
				return "(Num)";
			} else if (t.Equals(typeof(String))) {
				return "(Str)";
			} else if (typeof(Region).IsAssignableFrom(t)) {
				return "(Reg)";
			} else if (typeof(Thing).IsAssignableFrom(t)) {
				return "(Thg)";
			} else if (typeof(AbstractAccount).IsAssignableFrom(t)) {
				return "(Acc)";
			} else if (typeof(AbstractScript).IsAssignableFrom(t)) {
				return "(Scp)";
			} else if (t.Equals(typeof(Globals))) {
				return "(Glob)";
			} else {
				//nothing special, try the simpleimplementors
				ISimpleSaveImplementor iss = ObjectSaver.GetSimpleSaveImplementorByType(t);
				if (iss != null) {
					string pref = iss.Prefix;
					if (pref.Contains("(")) {
						return pref; //it already contains the brackets
					} else { //add the surrounding brackets
						return "(" + pref + ")";
					}
				} else {
					return "(" + t.Name + ")"; //this is the final desperate possibility
				}
			}
		}
	}

	[Summary("Class carrying information about one edit field - its former value, its value after setting and " +
			"the setting result value (success/fail)")]
	public class SettingResult {
		//former value of the field - before settings
		private object formerValue;
		//the EditableField itself (there will also be a newly set value, if successfull)
		private IDataFieldView field;
		//the target object (the one which field we are dealing with)
		private object target;

		//was the setting succesfull/erroneous or even not provided at all ? 
		private SettingsEnums outcome;
		//here will be the value we tried to set and it resulted in some error
		private string erroneousValue;

		public SettingResult(IDataFieldView field, object target) {
			this.field = field;
			this.target = target;
			formerValue = field.GetValue(target);
		}

		[Summary("What is the result of the setting?")]
		public SettingsEnums Outcome {
			get {
				return outcome;
			}
			set {
				outcome = value;
			}
		}

		[Summary("Get the name from the IDataFieldView")]
		public string Name {
			get {
				return field.GetName(target);
			}
		}

		[Summary("The value attempted to store which resluted in error - filled only in case of error :)")]
		public string ErroneousValue {
			get {
				//if the setting is OK, then return an empty string
				return erroneousValue == null ? "" : erroneousValue;
			}
			set {
				erroneousValue = value;
			}
		}

		[Summary("Get the former value of the edited field - for comparation with the result")]
		public string FormerValue {
			get {
				//Handle the Enums differently (show the name, not the value...)
				if (typeof(Enum).IsAssignableFrom(field.FieldType)) {
					return Enum.GetName(field.FieldType, formerValue);
				} else {
					return ObjectSaver.Save(formerValue);
				}
			}
			set {
				formerValue = value;
			}
		}

		[Summary("The actual value of the field - after the setting is made")]
		public string CurrentValue {
			get {
				//Handle the Enums differently (show the name, not the value...)				
				if (typeof(Enum).IsAssignableFrom(field.FieldType)) {
					return Enum.GetName(field.FieldType, field.GetValue(target));
				} else {
					return field.GetStringValue(target);
				}
			}
		}
	}
}