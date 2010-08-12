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

		private FieldValue chance;
		private FieldValue cooldown;
		private FieldValue resourcesConsumed;//resourcelist of resources to be consumed for ability using
		private FieldValue resourcesPresent;//resourcelist of resources that player must have intending to run the ability
		private FieldValue effectPower;
		private FieldValue effectDuration;

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

		[Summary("Used in some abilities to compute the probabilty of their success. "+
			"Typically 1.0 = 100%")]
		public double Chance {
			get {
				return (double) this.chance.CurrentValue;
			}
			set {
				this.chance.CurrentValue = value;
			}
		}

		public bool CheckSuccess(Character target) {
			return CheckSuccess(target.GetAbility(this));
		}

		public bool CheckSuccess(Ability ab) {
			return CheckSuccess(ab.ModifiedPoints);
		}

		public bool CheckSuccess(int points) {
			return Globals.dice.NextDouble() <= (points * this.Chance);
		}

		[Summary("Field for holding the number of seconds between next activation try." +
				"You can use 0 for no delay")]
		public double Cooldown {
			get {
				return (double) this.cooldown.CurrentValue;
			}
			set {
				this.cooldown.CurrentValue = value;
			}
		}

		[NoShow]
		[Summary("Field for holding the number of seconds between next activation try." +
				"You can use 0 for no delay")]
		public TimeSpan CooldownAsSpan {
			get {
				return TimeSpan.FromSeconds(this.Cooldown);
			}
		}

		[Summary("Used in some abilities to compute the power of their effect")]
		public double EffectPower {
			get {
				return (double) effectPower.CurrentValue;
			}
			set {
				effectPower.CurrentValue = value;
			}
		}

		[Summary("Used in some abilities to compute the duration of their effect. Typically in seconds.")]
		public double EffectDuration {
			get {
				return (double) effectDuration.CurrentValue;
			}
			set {
				effectDuration.CurrentValue = value;
			}
		}

		
		#endregion Accessors

		#region Factory methods
		[Summary("Method for instatiating Abilities.")]
		public virtual Ability Create(Character chr) {
			return new Ability(this, chr);
		}

		[Summary("Overall method for running the abilites. Its basic implementation looks if the character has given ability" +
				"and in case he has, it runs the protected activation method")]
		public virtual void Activate(Character chr) {
					Ability ab = chr.GetAbilityObject(this);

				DenyResult retVal = this.Trigger_DenyActivate(chr, ab); //return value means only that the trigger has been cancelled

				if (retVal.Allow) {
					this.Trigger_Activate(chr, ab);
					ab.LastUsage = Globals.TimeAsSpan; //set the last usage time
				} else {
					retVal.SendDenyMessage(chr); //send result(message) of the "activate" call to the client
				}
		}
		#endregion Factory methods

		#region Trigger methods

		internal static readonly TriggerKey tkAssign = TriggerKey.Acquire("assign");
		internal static readonly TriggerKey tkUnAssign = TriggerKey.Acquire("unAssign");
		internal static readonly TriggerKey tkAbilityValueChanged = TriggerKey.Acquire("abilityValueChanged");
		internal static readonly TriggerKey tkValueChanged = TriggerKey.Acquire("valueChanged");
		internal static readonly TriggerKey tkActivateAbility = TriggerKey.Acquire("activateAbility");
		internal static readonly TriggerKey tkActivate = TriggerKey.Acquire("activate");
		internal static readonly TriggerKey tkDenyActivateAbility = TriggerKey.Acquire("denyActivateAbility");
		internal static readonly TriggerKey tkDenyActivate = TriggerKey.Acquire("denyActivate");

		private TriggerGroup scriptedTriggers;

		[Summary("LScript based @activate triggers" +
				"Gets called when every prerequisity has been fulfilled and the ability can be run now")]
		protected void Trigger_Activate(Character chr, Ability ab) {
			ScriptArgs sa = new ScriptArgs(this, ab);

			bool cancel = chr.TryCancellableTrigger(AbilityDef.tkActivateAbility, sa);
			if (!cancel) {
				try {
					cancel = chr.On_ActivateAbility(this, ab);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (!cancel) {
					cancel = this.TryCancellableTrigger(chr, AbilityDef.tkActivate, null);
					if (!cancel) {
						try {
							this.On_Activate(chr, ab);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					}
				}
			}
		}

		[Summary("C# based @activate trigger method")]
		protected virtual bool On_Activate(Character chr, Ability ab) {
			chr.SysMessage(String.Format(System.Globalization.CultureInfo.InvariantCulture,
				Loc<AbilityDefLoc>.Get(chr.Language).AbilityActivated,
				ab.Def.Name));
			return false; //no cancelling
		}

		[Summary("This method fires the @denyUse triggers. "
		        + "Their purpose is to check if all requirements for running the ability have been met")]
		protected DenyResult Trigger_DenyActivate(Character chr, Ability ab) {
			if (ab == null || ab.ModifiedPoints == 0) {
				return DenyResultMessages_Abilities.Deny_DoesntHaveAbility;
			}

			DenyAbilityArgs denyArgs = new DenyAbilityArgs(chr, this, ab);

			bool cancel = chr.TryCancellableTrigger(tkDenyActivateAbility, denyArgs);
			if (!cancel) {
				try {
					cancel = chr.On_DenyActivateAbility(denyArgs);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (!cancel) {
					cancel = this.TryCancellableTrigger(chr, AbilityDef.tkDenyActivate, denyArgs);
					if (!cancel) {
						try {
							this.On_DenyActivate(denyArgs);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					}
				}
			}
			return denyArgs.Result;
		}

		[Summary("C# based @denyUse trigger method, implementation of common checks")]
		protected virtual bool On_DenyActivate(DenyAbilityArgs args) {
		    Ability ab = args.ranAbility;
		    //check cooldown
		    if (((Globals.TimeAsSpan - ab.LastUsage) <= this.CooldownAsSpan) && !args.abiliter.IsGM) { //check the timing if OK
		        args.Result = DenyResultMessages_Abilities.Deny_NotYetCooledDown;
		        return true;//same as "return 1" from LScript - cancel trigger sequence
		    }

		    //check resources present (if needed)
		    ResourcesList resPresent = resourcesPresent.CurrentValue as ResourcesList;
		    if (resPresent != null) {
		        IResourceListItem missingItem;
		        if (!resPresent.HasResourcesPresent(args.abiliter, ResourcesLocality.BackpackAndLayers, out missingItem)) {
		            missingItem.SendMissingMessage(args.abiliter);
		            args.Result = DenyResultMessages_Abilities.Deny_NotEnoughResourcesPresent;
					return true; //cancel
		        }
		    }

		    //check consumable resources
		    ResourcesList resConsum = resourcesConsumed.CurrentValue as ResourcesList;
		    if (resConsum != null) {
		        //look to the backpack and among the items that we are wearing
		        IResourceListItem missingItem;
		        if (!resConsum.ConsumeResourcesOnce(args.abiliter, ResourcesLocality.BackpackAndLayers, out missingItem)) {
		            missingItem.SendMissingMessage(args.abiliter);
		            args.Result = DenyResultMessages_Abilities.Deny_NotEnoughResourcesToConsume;
					return true; //cancel
		        }
		    }

			DenyResult result = args.abiliter.CheckAlive();
			args.Result = result;
			if (!result.Allow) {
				return true; //cancel
			}

		    return false; //all ok, continue
		}

		//this is not a character trigger. I think for them the @valuechanged should be enough
		private void Trigger_Assign(Character chr, Ability ab, ScriptArgs sa) {
			this.TryTrigger(chr, AbilityDef.tkAssign, sa);
			try {
				this.On_Assign(chr, ab);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		[Summary("This method implements the assigning of the first point to the Ability")]
		protected virtual void On_Assign(Character ch, Ability ab) {
			//ch.SysMessage("Abilita " + Name + " nemá implementaci trigger metody On_Assign");
		}

		//this is not a character trigger. I think for them the @valuechanged should be enough
		internal void Trigger_UnAssign(Character chr, Ability ab, ScriptArgs sa) {
			this.TryTrigger(chr, AbilityDef.tkUnAssign, sa);
			try {
				this.On_UnAssign(chr, ab);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		[Summary("This method implements the assigning of the first point to the Ability")]
		protected virtual void On_UnAssign(Character ch, Ability ab) {
			//ch.SysMessage("Abilita " + Name + " nemá implementaci trigger metody On_Assign");
		}

		internal void Trigger_ValueChanged(Character chr, Ability ab, int previousValue) {
			ScriptArgs sa = new ScriptArgs(this, ab, previousValue);
			chr.TryTrigger(AbilityDef.tkAbilityValueChanged, sa);
			try {
				chr.On_AbilityValueChanged(this, ab, previousValue);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }

			this.TryTrigger(chr, AbilityDef.tkValueChanged, sa);
			try {
				this.On_ValueChanged(chr, ab, previousValue);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }

			int newValue = ab.ModifiedPoints;
			if (previousValue == 0) {
				if (newValue > 0) {
					this.Trigger_Assign(chr, ab, sa);
				} else {
					Logger.WriteWarning("previousValue == 0 && newValue == " + newValue, new System.Diagnostics.StackTrace());
				}
			} else if (newValue == 0) { //should mean that the previous value was positive
				if (previousValue > 0) {
					this.Trigger_UnAssign(chr, ab, sa);
				} else {
					Logger.WriteWarning("newValue == 0 && previousValue == " + previousValue, new System.Diagnostics.StackTrace());
				}
			}
		}

		protected virtual void On_ValueChanged(Character ch, Ability ab, int previousValue) {
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
			this.cooldown = InitTypedField("cooldown", 0, typeof(double));
			this.chance = InitTypedField("chance", 1, typeof(double));
			this.resourcesConsumed = InitTypedField("resourcesConsumed", null, typeof(ResourcesList));
			this.resourcesPresent = InitTypedField("resourcesPresent", null, typeof(ResourcesList));
			this.effectPower = InitTypedField("effectPower", 1.0, typeof(double));
			this.effectDuration = InitTypedField("effectDuration", 5.0, typeof(double));			
		}

		#region Loading from scripts
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
		#endregion Loading from scripts
	}

	public class DenyAbilityArgs : DenyTriggerArgs {
		public readonly Character abiliter;
		public readonly AbilityDef ranAbilityDef;
		public readonly Ability ranAbility;

		public DenyAbilityArgs(Character abiliter, AbilityDef ranAbilityDef, Ability ranAbility)
			: base(DenyResultMessages.Allow, abiliter, ranAbilityDef, ranAbility) {
			this.abiliter = abiliter;
			this.ranAbilityDef = ranAbilityDef;
			this.ranAbility = ranAbility; //this can be null (if we dont have the ability)
		}
	}

	//abilities running possible results
	public static class DenyResultMessages_Abilities {
		public static readonly DenyResult Deny_DoesntHaveAbility = 
			new CompiledLocDenyResult<AbilityDefLoc>("YouDontHaveThisAbility"); //we don't have the ability (no points in it)
		public static readonly DenyResult Deny_NotYetCooledDown =
			new CompiledLocDenyResult<AbilityDefLoc>("NotYetCooledDown"); //the ability usage timer has not yet passed
		public static readonly DenyResult Deny_HasBeenSwitchedOff =
			new CompiledLocDenyResult<AbilityDefLoc>("HasBeenSwitchedOff"); //the ability was currently running (for ActivableAbilities only) so we switched it off
		public static readonly DenyResult Deny_NotEnoughResourcesToConsume =
			new CompiledLocDenyResult<AbilityDefLoc>("NotEnoughResourcesToConsume"); //missing some resources from "to consume" list
		public static readonly DenyResult Deny_NotEnoughResourcesPresent =
			new CompiledLocDenyResult<AbilityDefLoc>("NotEnoughResourcesPresent"); //missing some resources from "has present" list
		public static readonly DenyResult Deny_NotAllowedToHaveThisAbility =
			new CompiledLocDenyResult<AbilityDefLoc>("NotAllowedToHaveAbility"); //other reason why not allow to have the ability (e.g. wrong profession etc.)
	}

	public class AbilityDefLoc : CompiledLocStringCollection {
		public string AbilityActivated = "Abilita {0} aktivována.";
		public string YouDontHaveThisAbility = "O této abilitì nevíš vùbec nic.";
		public string NotYetCooledDown = "Abilitu nelze použít tak brzy po pøedchozím použití.";
		public string HasBeenSwitchedOff = "Abilita deaktivována.";
		public string NotEnoughResourcesToConsume = "Nedostatek zdrojù ke spotøebì pro aktivaci ability.";
		public string NotEnoughResourcesPresent = "Nedostatek zdrojù pro aktivaci ability.";
		public string NotAllowedToHaveAbility = "Nejsi oprávnìn mít tuto abilitu.";
	}
}
