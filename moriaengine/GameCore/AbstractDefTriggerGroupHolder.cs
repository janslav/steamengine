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
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using System.Text.RegularExpressions;
using SteamEngine.Common;
//using SteamEngine.PScript;

namespace SteamEngine {
	public abstract class AbstractDefTriggerGroupHolder : AbstractDef, ITriggerGroupHolder {

		//attention! this class does not (yet?) use the prevNode field on TGListNode, cos we don't need it here.
		internal PluginHolder.TGListNode firstTGListNode; //linked list of triggergroup references

		private static List<TGResolver> tgResolvers = new List<TGResolver>();

		protected AbstractDefTriggerGroupHolder(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {

		}

		protected override void LoadScriptLine(string filename, int line, string param, string args) {
			switch (param) {
				case "event":
				case "events":
				//case "type":
				case "triggergroup":
				//case "resources"://in sphere, resources are the same like events... is it gonna be that way too in SE? NO!
					tgResolvers.Add(new TGResolver(this, args, filename, line));
					break;
				default:
					base.LoadScriptLine(filename, line, param, args);//the AbstractDef Loadline
					break;
			}
		}

		//This is necessary, since when our ThingDef are being made, not all scripts may have been loaded yet. -SL
		private class TGResolver {
			AbstractDefTriggerGroupHolder cont;
			string filename, tgName;
			int line;

			internal TGResolver(AbstractDefTriggerGroupHolder cont, string tgName, string filename, int line) {
				this.cont = cont;
				this.tgName = tgName;
				this.filename = filename;
				this.line = line;
			}

			internal void Resolve() {
				if (this.tgName != "0") {	//"0" means nothing
					TriggerGroup tg = TriggerGroup.GetByDefname(this.tgName);
					if (tg == null) {
						Logger.WriteWarning(LogStr.FileLine(this.filename, this.line) + "'" + LogStr.Ident(this.tgName) + "' is not a valid TriggerGroup (Event/Type).");
					} else {
						this.cont.AddTriggerGroup(tg);
					}
				}
			}
		}

		internal static void LoadingFinished() {
			foreach (TGResolver resolver in tgResolvers) {
				resolver.Resolve();
			}
			tgResolvers = new List<TGResolver>();
		}

		/*
			Method: AddTriggerGroup
			Returns false if we already have the triggerGroup, true if not.
			
			Parameters:
				tg - The TtriggerGroup to add. 
		*/
		public void AddTriggerGroup(TriggerGroup tg) {
			if (tg == null) return;
			if (firstTGListNode == null) {
				firstTGListNode = new PluginHolder.TGListNode(tg);
			} else {
				PluginHolder.TGListNode curNode = firstTGListNode;
				while (true) {
					if (curNode.storedTG == tg) {
						return;// false;//we already have it
					} else if (curNode.nextNode == null) {
						curNode.nextNode = new PluginHolder.TGListNode(tg);
						return;
					}
					curNode = curNode.nextNode;
				}
				//return true;//we had to add it
			}
		}

		public IEnumerable<TriggerGroup> GetAllTriggerGroups() {
			if (firstTGListNode != null) {
				PluginHolder.TGListNode curNode = firstTGListNode;
				do {
					yield return curNode.storedTG;
					curNode = curNode.nextNode;
				} while (curNode != null);
			}
		}

		/*
			Method: RemoveTriggerGroup
			Removes the triggerGroup if we have it.
			
			Parameters:
				tg - The triggerGroup to remove.
		*/
		public void RemoveTriggerGroup(TriggerGroup tg) {
			if (tg == null) return;
			if (firstTGListNode != null) {
				if (firstTGListNode.storedTG == tg) {
					firstTGListNode = firstTGListNode.nextNode;
					return;
				}
				PluginHolder.TGListNode lastNode = firstTGListNode;
				PluginHolder.TGListNode curNode = lastNode.nextNode;
				while (curNode != null) {
					if (curNode.storedTG == tg) {
						lastNode.nextNode = curNode.nextNode;
						return;
					}
					lastNode = curNode;
					curNode = curNode.nextNode;
				}
				//return false;//we didnt have it, so we didnt do anything
			}
		}

		/*
			Method: HasTriggerGroup
			Determines if we have this TriggerGroup on us.
			
			Parameters:
				tg - The triggergroup in question.
			
			Returns:
				True if we have it, false if we do not.
		*/

		public bool HasTriggerGroup(TriggerGroup tg) {
			if (tg == null) return false;
			PluginHolder.TGListNode curNode = firstTGListNode;
			do {
				if (curNode.storedTG == tg) {
					return true;
				}
				curNode = curNode.nextNode;
			} while (curNode != null);
			return false;
		}

		public void ClearTriggerGroups() {
			firstTGListNode = null;
		}


		public override void Unload() {
			if (firstTGListNode != null) {
				PluginHolder.TGListNode curNode = firstTGListNode;
				do {
					curNode.storedTG.Unload();
					curNode = curNode.nextNode;
				} while (curNode != null);
			}
			firstTGListNode = null;
			base.Unload();
		}

		/*
			Method: Trigger
			Triggers a trigger on this object, using the specified ScriptArgs
			
			Parameters:
				sa - The arguments (other than argv) for sphere scripts
				td - The TriggerKey for the trigger to call.
		
			Returns:
				The return value(s) from the triggers called.
		
			See also:
				<Trigger>, <CancellableTriggers>
		*/
		public virtual void Trigger(TriggerKey tk, ScriptArgs sa) {
			if (firstTGListNode != null) {
				PluginHolder.TGListNode curNode = firstTGListNode;
				do {
					curNode.storedTG.Run(this, tk, sa);
					curNode = curNode.nextNode;
				} while (curNode != null);
			}
		}

		public virtual void TryTrigger(TriggerKey tk, ScriptArgs sa) {
			if (firstTGListNode != null) {
				PluginHolder.TGListNode curNode = firstTGListNode;
				do {
					curNode.storedTG.TryRun(this, tk, sa);
					curNode = curNode.nextNode;
				} while (curNode != null);
			}
		}

		/*
			Method: CancellableTrigger
			Executes the trigger, reads return values, and returns true if anything returned 1 (returning false otherwise).
			
			Parameters:
				td - The trigger to execute
				sa - Arguments for scripts (argn, args, argo, argn1, argn2, etc). Can be null.
			
			Returns:
				True if any called trigger scripts returned 1, false otherwise.
			
			See also:
				<Trigger>, <CancellableTriggers>
		*/

		public virtual bool CancellableTrigger(TriggerKey tk, ScriptArgs sa) {
			if (firstTGListNode != null) {
				PluginHolder.TGListNode curNode = firstTGListNode;
				do {
					if (TagMath.Is1(curNode.storedTG.Run(this, tk, sa))) {
						return true;
					}
					curNode = curNode.nextNode;
				} while (curNode != null);
			}
			return false;
		}

		public virtual bool TryCancellableTrigger(TriggerKey tk, ScriptArgs sa) {
			if (firstTGListNode != null) {
				PluginHolder.TGListNode curNode = firstTGListNode;
				do {
					if (TagMath.Is1(curNode.storedTG.TryRun(this, tk, sa))) {
						return true;
					}
					curNode = curNode.nextNode;
				} while (curNode != null);
			}
			return false;
		}

		public void Trigger(TriggerKey tk, params object[] scriptArguments) {
			if ((scriptArguments != null) && (scriptArguments.Length > 0)) {
				Trigger(tk, new ScriptArgs(scriptArguments));
			} else {
				Trigger(tk, (ScriptArgs) null);
			}
		}

		public bool CancellableTrigger(TriggerKey tk, params object[] scriptArguments) {
			if ((scriptArguments != null) && (scriptArguments.Length > 0)) {
				return CancellableTrigger(tk, new ScriptArgs(scriptArguments));
			} else {
				return CancellableTrigger(tk, (ScriptArgs) null);
			}
		}
	}
}
