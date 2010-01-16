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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.CompiledScripts;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public class AbilityDef : AbstractIndexedDef<AbilityDef, string> {
		//private static Dictionary<string, AbilityDef> byName = new Dictionary<string, AbilityDef>(StringComparer.OrdinalIgnoreCase);

		//private static Dictionary<string, ConstructorInfo> abilityDefCtorsByName = new Dictionary<string, ConstructorInfo>(StringComparer.OrdinalIgnoreCase);
		//string-ConstructorInfo pairs  ("AbilityDef" - AbilityDef.ctor)

		#region Accessors
		//private string name; //logical name of the ability

		private FieldValue maxPoints; //maximum points allowed to assign
		private FieldValue useDelay;
		private FieldValue resourcesConsumed;//resourcelist of resources to be consumed for ability using
		private FieldValue resourcesPresent;//resourcelist of resources that player must have intending to run the ability

		public static new AbilityDef GetByDefname(string defname) {
			return AbstractScript.GetByDefname(defname) as AbilityDef;
		}

		public static AbilityDef GetByName(string key) {
			return GetByDefIndex(key);
		}

		public static int AbilitiesCount {
			get {
				return IndexedCount;
			}
		}

		[Summary("Return enumerable containing all abilities (copying the values from the main dictionary)")]
		//public static Dictionary<string,AbilityDef>.ValueCollection AllAbilities {
		public static ICollection<AbilityDef> AllAbilities {
			get {
				return AllIndexedDefs;
			}
		}

		public string Name {
			get {
				return this.DefIndex;
			}
		}

		[InfoField("Max points")]
		public ushort MaxPoints {
			get {
				return (ushort) maxPoints.CurrentValue;
			}
			set {
				maxPoints.CurrentValue = value;
			}
		}

		[InfoField("Usage delay")]
		[Summary("Field for holding the number information about the pause between next activation try." +
				"You can use 0 for no delay")]
		public double UseDelay {
			get {
				return (double) useDelay.CurrentValue;
			}
			set {
				useDelay.CurrentValue = value;
			}
		}

		protected override void LoadScriptLine(string filename, int line, string param, string args) {
			base.LoadScriptLine(filename, line, param, args);
		}

		public override string ToString() {
			return Tools.TypeToString(this.GetType()) + " " + Name;
		}

		#endregion Accessors

		#region Factory methods
		[Summary("Method for instatiating Abilities. Basic implementation is easy but can be overriden " +
				"if we want to return some descendants of the Ability class - e.g. RegenAbility...")]
		public virtual Ability Create(Character chr) {
			return new Ability(this, chr);
		}

		[Summary("Overall method for running the abilites. Its basic implementation looks if the character has given ability" +
				"and in case he has, it runs the protected activation method")]
		public void Activate(Character chr) {
			Ability ab = chr.GetAbilityObject(this);
			if (ab == null || ab.Points == 0) {
				SendAbilityResultMessage(chr, DenyResultAbilities.Deny_DoesntHaveAbility);
			} else {
				DenyAbilityArgs args = new DenyAbilityArgs(chr, this, ab);
				bool cancelDeny = Trigger_DenyUse(args); //return value means only that the trigger has been cancelled
				DenyResultAbilities retVal = args.Result;//this value contains the info if we can or cannot run the ability

				if (retVal == DenyResultAbilities.Allow) {
					bool cancelActivate = Trigger_Activate(chr);
					ab.LastUsage = Globals.TimeInSeconds; //set the last usage time
					Activate(chr, ab); //call specific behaviour of the ability class (logging, Ability object state switching etc.)					
				}
				SendAbilityResultMessage(chr, retVal); //send result(message) of the "activate" call to the client
			}
		}

		protected virtual void Activate(Character chr, Ability ab) {
			//default without implementation, children can contain some specific behaviour which goes 
			//beyond the Activate(Character) method capabilities...
		}
		#endregion Factory methods

		#region Trigger methods

		internal static readonly TriggerKey tkAssign = TriggerKey.Acquire("Assign");
		internal static readonly TriggerKey tkDenyAssign = TriggerKey.Acquire("DenyAssign");
		internal static readonly TriggerKey tkUnAssign = TriggerKey.Acquire("UnAssign");
		internal static readonly TriggerKey tkValueChanged = TriggerKey.Acquire("ValueChanged");
		internal static readonly TriggerKey tkActivate = TriggerKey.Acquire("Activate");
		internal static readonly TriggerKey tkDenyUse = TriggerKey.Acquire("DenyUse");

		private TriggerGroup scriptedTriggers;

		[Summary("C# based @activate trigger method")]
		protected virtual bool On_Activate(Character chr) {
			chr.SysMessage("Abilita " + Name + " nem� implementaci trigger metody On_Activate");
			return false; //no cancelling
		}

		[Summary("LScript based @activate triggers" +
				"Gets called when every prerequisity has been fulfilled and the ability can be run now")]
		protected bool Trigger_Activate(Character chr) {
			bool cancel = false;
			cancel = TryCancellableTrigger(chr, AbilityDef.tkActivate, null);
			if (!cancel) {
				cancel = chr.On_AbilityActivate(this);
				if (!cancel) {//still not cancelled
					cancel = On_Activate(chr);
				}
			}
			return cancel;
		}

		[Summary("This method fires the @denyUse triggers. "
				+ "Their purpose is to check if all requirements for running the ability have been met")]
		protected bool Trigger_DenyUse(DenyAbilityArgs args) {
			bool cancel = false;
			cancel = this.TryCancellableTrigger(args.abiliter, AbilityDef.tkDenyUse, args);
			if (!cancel) {//not cancelled (no return 1 in LScript), lets continue
				cancel = args.abiliter.On_AbilityDenyUse(args);
				if (!cancel) {//still not cancelled
					cancel = On_DenyUse(args);
				}
			}
			return cancel;
		}

		[Summary("C# based @denyUse trigger method, implementation of common checks (timers...)")]
		protected virtual bool On_DenyUse(DenyAbilityArgs args) {
			Ability ab = args.runAbility;
			//common check - is the usage timer OK?
			if ((Globals.TimeInSeconds - ab.LastUsage) <= this.UseDelay) { //check the timing if OK
				args.Result = DenyResultAbilities.Deny_TimerNotPassed;
				return true;//same as "return 1" from LScript - cancel trigger sequence
			}
			//check resources present (if needed)
			ResourcesList resPresent = resourcesPresent.CurrentValue as ResourcesList;
			if (resPresent != null) {
				IResourceListItem missingItem;
				if (!resPresent.HasResourcesPresent(args.abiliter, ResourcesLocality.BackpackAndLayers, out missingItem)) {
					ResourcesList.SendResourceMissingMsg(args.abiliter, missingItem);
					args.Result = DenyResultAbilities.Deny_NotEnoughResourcesPresent;
					return true;
				}
			}
			//check consumable resources
			ResourcesList resConsum = resourcesConsumed.CurrentValue as ResourcesList;
			if (resConsum != null) {
				//look to the backpack and among the items that we are wearing
				IResourceListItem missingItem;
				if (!resConsum.ConsumeResourcesOnce(args.abiliter, ResourcesLocality.BackpackAndLayers, out missingItem)) {
					ResourcesList.SendResourceMissingMsg(args.abiliter, missingItem);
					args.Result = DenyResultAbilities.Deny_NotEnoughResourcesToConsume;
					return true;
				}
			}
			return false; //continue
		}

		[Summary("This method implements the assigning of the first point to the Ability")]
		protected virtual void On_Assign(Character ch) {
			//ch.SysMessage("Abilita " + Name + " nem� implementaci trigger metody On_Assign");
		}

		internal void Trigger_Assign(Character chr) {
			if (chr != null) {
				TryTrigger(chr, AbilityDef.tkAssign, null);
				chr.On_AbilityAssign(this);
				On_Assign(chr);
			}
		}

		[Summary("This method fires the @denyAssign triggers. "
				+ "Their purpose is to check if character can be assigned this ability")]
		protected virtual bool On_DenyAssign(DenyAbilityArgs args) {
			//ch.SysMessage("Abilita " + Name + " nem� implementaci trigger metody On_DenyAssign");
			return false; //continue
		}

		internal bool Trigger_DenyAssign(DenyAbilityArgs args) {
			bool cancel = false;
			cancel = this.TryCancellableTrigger(args.abiliter, AbilityDef.tkDenyAssign, args);
			if (!cancel) {//not cancelled (no return 1 in LScript), lets continue
				cancel = args.abiliter.On_AbilityDenyAssign(args);
				if (!cancel) {//still not cancelled
					cancel = On_DenyAssign(args);
				}
			}
			return cancel;
		}

		[Summary("This method implements the unassigning of the last point from the Ability")]
		protected virtual void On_UnAssign(Character ch) {
			ch.SysMessage("Abilita " + Name + " nem� implementaci trigger metody On_UnAssign");
		}

		internal void Trigger_UnAssign(Character chr) {
			if (chr != null) {
				TryTrigger(chr, AbilityDef.tkUnAssign, null);
				chr.On_AbilityUnAssign(this);
				On_UnAssign(chr);
			}
		}

		[Summary("This method implements changing ability points")]
		protected virtual void On_ValueChanged(Character ch, Ability ab, int previousValue) {
		}

		internal void Trigger_ValueChanged(Character chr, Ability ab, int previousValue) {
			if (chr != null) {
				TryTrigger(chr, AbilityDef.tkValueChanged, new ScriptArgs(ab, previousValue));
				chr.On_AbilityValueChanged(ab, previousValue);
				On_ValueChanged(chr, ab, previousValue);
			}
		}

		public bool TryCancellableTrigger(AbstractCharacter self, TriggerKey td, ScriptArgs sa) {
			//cancellable trigger just for the one triggergroup
			if (this.scriptedTriggers != null) {
				if (TagMath.Is1(this.scriptedTriggers.TryRun(self, td, sa))) {
					return true;
				}
			}
			return false;
		}

		public void TryTrigger(AbstractCharacter self, TriggerKey td, ScriptArgs sa) {
			if (this.scriptedTriggers != null) {
				this.scriptedTriggers.TryRun(self, td, sa);
			}
		}
		#endregion Trigger methods

		public AbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			useDelay = InitTypedField("useDelay", 0, typeof(double));
			maxPoints = InitTypedField("maxPoints", 0, typeof(ushort));
			resourcesConsumed = InitTypedField("resourcesConsumed", null, typeof(ResourcesList));
			resourcesPresent = InitTypelessField("resourcesPresent", null);
		}

		public override void LoadScriptLines(PropsSection ps) {
			PropsLine p = ps.PopPropsLine("name");
			this.DefIndex = ConvertTools.LoadSimpleQuotedString(p.Value);
			this.Unregister();

			base.LoadScriptLines(ps);

			if (ps.TriggerCount > 0) {
				ps.HeaderName = "t__" + this.Defname + "__";
				this.scriptedTriggers = ScriptedTriggerGroup.Load(ps);
			}
		}

		public override void Unload() {
			if (this.scriptedTriggers != null) {
				this.scriptedTriggers.Unload();
			}
			base.Unload();
		}

		#region utilities
		[Summary("Method for sending clients messages about their attempt of ability usage")]
		internal void SendAbilityResultMessage(Character toWhom, DenyResultAbilities res) {
			switch (res) {
				//case DenyResultAbilities.Allow:								
				case DenyResultAbilities.Deny_DoesntHaveAbility:
					toWhom.RedMessage("O abilit� " + Name + " nev� v�bec nic");
					break;
				case DenyResultAbilities.Deny_TimerNotPassed:
					toWhom.RedMessage("Abilitu nelze pou��t tak brzy po p�edchoz�m pou�it�");
					break;
				case DenyResultAbilities.Deny_WasSwitchedOff:
					toWhom.SysMessage("Abilita " + Name + " byla vypnuta");
					break;
				case DenyResultAbilities.Deny_NotEnoughResourcesToConsume:
					toWhom.RedMessage("Nedostatek zdroj� ke spot�eb� pro spu�t�n� ability " + Name);
					break;
				case DenyResultAbilities.Deny_NotEnoughResourcesPresent:
					toWhom.RedMessage("Nedostatek zdroj� pro spu�t�n� ability " + Name);
					break;
				case DenyResultAbilities.Deny_NotAllowedToHaveThisAbility:
					toWhom.RedMessage("Nejsi opr�vn�n m�t abilitu " + Name);
					break;
			}
		}
		#endregion utilities
	}

	public class DenyAbilityArgs : ScriptArgs {
		public readonly Character abiliter;
		public readonly AbilityDef runAbilityDef;
		public readonly Ability runAbility;

		public DenyAbilityArgs(params object[] argv)
			: base(argv) {
			Sanity.IfTrueThrow(!(argv[0] is DenyResultAbilities), "argv[0] is not DenyResultAbilities");
		}

		public DenyAbilityArgs(Character abiliter, AbilityDef runAbilityDef, Ability runAbility)
			: this(DenyResultAbilities.Allow, abiliter, runAbilityDef, runAbility) {
			this.abiliter = abiliter;
			this.runAbilityDef = runAbilityDef;
			this.runAbility = runAbility; //this can be null (if we dont have the ability)
		}

		public DenyResultAbilities Result {
			get {
				return (DenyResultAbilities) Convert.ToInt32(Argv[0]);
			}
			set {
				Argv[0] = value;
			}
		}
	}
}
