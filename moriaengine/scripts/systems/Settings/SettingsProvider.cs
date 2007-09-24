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
using System.Collections;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts.Dialogs {
	[Remark("Class for accepting and setting all values edited in the info or settings dialogs")]
	public static class SettingsProvider {
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
				} else {
					//nothing changed
					oneRes.Outcome = SettingsOutcome.NotChanged;
				}
				resList.Add(oneRes);
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
				return field.Name;
			}
		}

		[Remark("The value attempted to store which resluted in error - filled only in case of error :)")]
		public string ErroneousValue {
			get {
				return erroneousValue;
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