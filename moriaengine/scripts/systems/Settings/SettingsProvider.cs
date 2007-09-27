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

namespace SteamEngine.CompiledScripts.Dialogs {
	[Remark("Class for accepting and setting all values edited in the info or settings dialogs")]
	public static class SettingsProvider {
		//sotre the used saveable prefixes for particular Types
		private static Dictionary<Type, string> prefixTypes = new Dictionary<Type, string>();

		[Remark("Try to store all edited (changed) fields from the dialog. Store the results in the special list that will be returned for "+
				"displaying")]
		public static List<SettingResult> AssertSettings(Hashtable editFields, GumpResponse resp, object target) {
			List<SettingResult> resList = new List<SettingResult>();
			SettingResult oneRes = null;
			foreach(int key in editFields.Keys) {
				IDataFieldView field = (IDataFieldView)editFields[key];
				oneRes = new SettingResult(field, target); //prepare the result
				string newStringValue = resp.GetTextResponse(key); //get the value from the edit field
				if(!newStringValue.Equals(field.GetStringValue(target))) {
					oneRes.Outcome = SettingsOutcome.ChangedOK; //assume it will be OK
					//something has changed - try to make the setting
					try {
						field.SetStringValue(target, newStringValue);
					} catch {
						//setting failed for some reason
						oneRes.Outcome = SettingsOutcome.ChangedError; //it wasnt OK... :-(
						//store the attempted string
						oneRes.ErroneousValue = newStringValue;
					}
					resList.Add(oneRes);//add to the list (only if changed somehow)
				} else {
					//nothing changed
					oneRes.Outcome = SettingsOutcome.NotChanged;
				}								
			}
			return resList;
		}

		[Remark("Look on the outcome value and return the correct color for the dialog")]
		public static Hues ResultColor(SettingResult sres) {
			switch(sres.Outcome) {
				case SettingsOutcome.ChangedError:
					return Hues.SettingsFailedColor;
				case SettingsOutcome.ChangedOK:
					return Hues.SettingsCorrectColor;
				default:
					return Hues.SettingsNormalColor; //but thos won't happen :)
			}			
		}

		[Remark("Count all successfully edited fields")]
		public static int CountSuccessfulSettings(List<SettingResult> results) {
			int resCntr = 0;
			foreach(SettingResult sres in results) {
				if(sres.Outcome == SettingsOutcome.ChangedOK) {
					resCntr++;
				}
			}
			return resCntr;
		}

		[Remark("Count all failed fields")]
		public static int CountUnSuccessfulSettings(List<SettingResult> results) {
			int resCntr = 0;
			foreach(SettingResult sres in results) {
				if(sres.Outcome == SettingsOutcome.ChangedError) {
					resCntr++;
				}
			}
			return resCntr;
		}

		[Remark("Examine the setings value's member type and get its prefix as used in ObjectSaver." +
				"We will use it in the info/settings dialog for displaying and identification")]
		public static string GetValuePrefix(IDataFieldView field, object target) {
			object value = field.GetValue(target);
			string valuePrefix = "";

			//we will store it in the special dictionary
			if(!prefixTypes.ContainsKey(value.GetType())) {
				//prefix is not yet stored, find and store it now!							
				Type t = value.GetType();

				//types like Enum, Numbers, String, Regions  or Globals doesn't have any prefixes, they will be displayed as is
				if(t.IsEnum || TagMath.IsNumberType(t) || t.Equals(typeof(String))
					|| typeof(Region).IsAssignableFrom(t) || value == Globals.Instance) {
				} else if(typeof(Thing).IsAssignableFrom(t)) {
					valuePrefix = "#";
				} else if(typeof(AbstractAccount).IsAssignableFrom(t)) {
					valuePrefix = "$";
				} else if(typeof(AbstractScript).IsAssignableFrom(t)) {
					valuePrefix = "#";
				} else {
					//try the simpleimplementors
					ISimpleSaveImplementor iss = ObjectSaver.GetSimpleSaveImplementorByType(t);
					if(iss != null) {
						valuePrefix = iss.Prefix;
					} else {
						valuePrefix = t.Name; //this is the final possibility
					}
				}

				//store the prefix in the dictionary
				prefixTypes[t] = valuePrefix;
			} else {
				valuePrefix = prefixTypes[value.GetType()];
			}
			return valuePrefix;
		}

		[Remark("Get the settings values prefix and surround it by brackets." +
				"If the value has no prefix (it is of some special type, find out what" +
				"type it is and return some description of it")]
		public static string GetTypePrefix(IDataFieldView field) {
			//get type
			Type t = field.FieldType;
			if(t.Equals(typeof(object))) {
				//object is also possible ! - the member can be set to any value
				return "(Obj)";
			} else if(t.IsEnum) {
				return "(Enum)";
			} else if(TagMath.IsNumberType(t)) {
				return "(Num)";
			} else if(t.Equals(typeof(String))) {
				return "(Str)";
			} else if(typeof(Region).IsAssignableFrom(t)) {
				return "(Reg)";
			} else if(typeof(Thing).IsAssignableFrom(t)) {
				return "(Thg)";
			} else if(typeof(AbstractAccount).IsAssignableFrom(t)) {
				return "(Acc)";
			} else if(typeof(AbstractScript).IsAssignableFrom(t)) {
				return "(Abs)";
			} else if(t.Equals(typeof(Globals))) {
				return "(Glob)";
			} else {
				//nothing special, try the simpleimplementors
				ISimpleSaveImplementor iss = ObjectSaver.GetSimpleSaveImplementorByType(t);
				if(iss != null) {
					return "("+iss.Prefix+")";
				} else {
					return "("+t.Name+")"; //this is the final desperate possibility
				}				
			}
		}
	}

	[Remark("Class carrying information about one edit field - its former value, its value after setting and "+
			"the setting result value (success/fail)")]
	public class SettingResult {
		//former value of the field - before settings
		private object formerValue;
		//the EditableField itself (there will also be a newly set value, if successfull)
		private IDataFieldView field;
		//the target object (the one which field we are dealing with)
		private object target;

		//was the setting succesfull/erroneous or even not provided at all ? 
		private SettingsOutcome outcome;
		//here will be the value we tried to set and it resulted in some error
		private string erroneousValue;

		public SettingResult(IDataFieldView field, object target) {
			this.field = field;
			this.target = target;
			formerValue = field.GetValue(target);
		}

		[Remark("What is the result of the setting?")]
		public SettingsOutcome Outcome {
			get {
				return outcome;
			}
			set {
				outcome = value;
			}
		}

		[Remark("Get the name from the IDataFieldView")]
		public string Name {
			get {
				return field.GetName(target);
			}
		}

		[Remark("The value attempted to store which resluted in error - filled only in case of error :)")]
		public string ErroneousValue {
			get {
				//if the setting is OK, then return an empty string
				return erroneousValue == null ? "" : erroneousValue;
			}
			set {
				erroneousValue = value;
			}
		}

		[Remark("Get the former value of the edited field - for comparation with the result")]
		public string FormerValue {
			get {
				return ObjectSaver.Save(formerValue);
			}
		}

		[Remark("The actual value of the field - after the setting is made")]
		public string CurrentValue {
			get {
				return field.GetStringValue(target);
			}
		}
	}
}