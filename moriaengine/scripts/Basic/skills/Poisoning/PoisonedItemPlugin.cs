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
using SteamEngine;
using SteamEngine.Common;
using SteamEngine.Regions;
using SteamEngine.Timers;
using SteamEngine.Persistence;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {


	[Dialogs.ViewableClass]
	public partial class PoisonedItemPlugin {
		public static PluginKey poisonPK = PluginKey.Acquire("_poison_");

		public const int projectilesPerPotion = 50;


		internal static PoisonedItemPlugin Acquire(PoisonPotion potion) {
			PoisonedItemPlugin plugin = (PoisonedItemPlugin) PoisonedItemPluginDef.instance.Create();
			plugin.poisonTickCount = potion.PoisonTickCount;
			plugin.poisonPower = potion.PoisonPower;
			plugin.poisonType = potion.PoisonType;
			return plugin;
		}

		public static PoisonedItemPlugin GetPoisonPlugin(Item item) {
			return item.GetPlugin(poisonPK) as PoisonedItemPlugin;
		}

		public PoisonEffectPluginDef PoisonType {
			get {
				return this.poisonType;
			}
		}

		public int PoisonPower {
			get {
				return this.poisonPower;
			}
		}

		public int PoisonTickCount {
			get {
				return this.poisonTickCount;
			}
		}

		public int PoisonDoses {
			get {
				return this.poisonDoses;
			}
		}

		public void On_BuildAosToolTips(AosToolTips opc, Language language) {
			PoisonedItemLoc loc = Loc<PoisonedItemLoc>.Get(language);
			opc.AddNameColonValue(loc.DosesLeft, this.poisonDoses.ToString(System.Globalization.CultureInfo.InvariantCulture));
			opc.AddNameColonValue(loc.Power, this.poisonPower.ToString(System.Globalization.CultureInfo.InvariantCulture));
		}

		public void On_Click(Character clicker) {
			GameState state = clicker.GameState;
			if (state != null) {
				PoisonedItemLoc loc = Loc<PoisonedItemLoc>.Get(clicker.Language);
				string msg = loc.DosesLeft + ": " + this.poisonDoses.ToString(System.Globalization.CultureInfo.InvariantCulture) +
					loc.Power + ": " + this.poisonPower.ToString(System.Globalization.CultureInfo.InvariantCulture);
				PacketSequences.SendOverheadMessageFrom(state.Conn, (Thing) this.Cont, msg, -1);
			}
		}

		public void BindToProjectile(Projectile projectile) {
			PoisonedItemPlugin previous = projectile.GetPlugin(poisonPK) as PoisonedItemPlugin;
			if (previous != null) {
				Sanity.IfTrueThrow(previous == this, "previous == this");
				Sanity.IfTrueThrow(previous.poisonType != this.poisonType, "previous.poisonType != this.poisonType");
				
				//the new potion and the old one are summed up, their power averaged.
				int prevDoses = previous.poisonDoses;
				int newDoses = prevDoses + projectilesPerPotion;
				previous.poisonPower = (previous.poisonPower * prevDoses + 
					this.poisonPower * projectilesPerPotion) / newDoses;
				previous.poisonTickCount = (previous.poisonTickCount * prevDoses +
					this.poisonTickCount * projectilesPerPotion) / newDoses;

				this.Delete(); //probably does nothing, but to be sure...
			} else {
				projectile.AddPlugin(poisonPK, this);
			}
		}

		public void BindToWeapon(Weapon weapon) {
			weapon.AddPlugin(poisonPK, this);
		}

		//there's a chance this weapon/projectile is in use already. If not, the TG will harmlessly uninstall itself soon.
		public void On_Assign() {
			Thing thing = this.Cont as Thing;
			if (thing != null) {
				Character topChar = thing.TopObj() as Character;
				if (topChar != null) {
					this.InstallCharacterTG(topChar);
				}
			}
		}

		//our weaponis being equipped to be fought with
		public void On_Equip(ItemInCharArgs args) {
			this.InstallCharacterTG((Character) args.Cont);
		}

		//our projectile is being used with a weapon
		public void On_CoupledWithWeapon(Character cont, Weapon weapon) {
			this.InstallCharacterTG(cont);
		}

		private void InstallCharacterTG(Character topChar) {
			topChar.AddTriggerGroup(SingletonScript<E_Poisoned_Weapon_User>.Instance);
		}

		internal void Apply(Thing source, Character target, EffectFlag sourceType) {
			//install the poison under one of 2 possible pluginnames - spell or potion. 
			//Or maybe there should also be slots for each different type?

			PluginKey key;
			if ((sourceType & EffectFlag.FromSpellBook) == EffectFlag.FromSpellBook) {
				key = SingletonScript<PoisonSpellDef>.Instance.EffectPluginKey_Spell;
			} else { //everything that's not a spell is a potion, right? :)
				key = SingletonScript<PoisonSpellDef>.Instance.EffectPluginKey_Potion;
			}

			PoisonEffectPlugin previous = target.GetPlugin(key) as PoisonEffectPlugin;
			if (previous != null) {
				if ((previous.Def == this.Def) && (previous.EffectPower > this.poisonPower)) {
					//previous poison is of the same type, and stronger, so we leave it alone
					return;
				}
			}

			PoisonEffectPlugin effect = (PoisonEffectPlugin) this.poisonType.Create();
			effect.Init(source, sourceType, this.poisonPower,
				TimeSpan.FromTicks(effect.TypeDef.TickInterval.Ticks * this.poisonTickCount));
			target.AddPlugin(key, effect);
		}

		internal void WipeSingleDoseFromWeapon() {
			if (this.poisonDoses > 0 && this.poisonPower > 0) {
				this.poisonPower -= (this.poisonPower / this.poisonDoses); //poison on weapon weakens by use
				this.poisonDoses--;
			} else {
				this.Delete();
			}
		}

		internal void DetractSingleDoseFromAmmo() {
			this.poisonDoses--;
			if (this.poisonDoses <= 0) {
				this.Delete();
			}
		}

		//this.Cont is being stacked onto another item (projectile) 
		public virtual bool On_StackOnItem(ItemStackArgs args) {
			//this == args.ManipulatedItem

			PoisonedItemPlugin otherPoison = args.WaitingStack.GetPlugin(poisonPK) as PoisonedItemPlugin;
			if (otherPoison == null) {
				//no poison on the waitingstack, we transfer there

				this.Cont.RemovePlugin(this);
				args.WaitingStack.AddPlugin(poisonPK, this);
			}

			return false; //do not cancel
		}

		//another item (projectile) is being stacked onto this.Cont
		public virtual bool On_ItemStackOn(ItemStackArgs args) {
			//this == args.WaitingStack

			PoisonedItemPlugin otherPoison = args.ManipulatedItem.GetPlugin(poisonPK) as PoisonedItemPlugin;
			if ((otherPoison != null) && (otherPoison.poisonType == this.poisonType)) {
				//the new potion and the old one are summed up, their power averaged.

				int otherDoses = otherPoison.poisonDoses;
				int summedDoses = otherDoses + this.poisonDoses;
				this.poisonPower = (otherPoison.poisonPower * otherDoses +
					this.poisonPower * this.poisonDoses) / summedDoses;
				this.poisonTickCount = (otherPoison.poisonTickCount * otherDoses +
					this.poisonTickCount * this.poisonDoses) / summedDoses;
			}

			otherPoison.Delete(); //probably does nothing, but to be sure...
			
			return false;
		}
	}

	public class E_Poisoned_Weapon_User : CompiledTriggerGroup {
		public void On_AfterSwing(Character self, WeaponSwingArgs swingArgs) {
			if (swingArgs.FinalDamage > 0) { //or maybe we don't really care? 
				bool poisonUsed = false;
				PoisonedItemPlugin poison = self.Weapon.GetPlugin(PoisonedItemPlugin.poisonPK) as PoisonedItemPlugin;
				if (poison != null) {
					poison.Apply(self, swingArgs.defender, EffectFlag.HarmfulEffect | EffectFlag.FromPotion);
					poison.WipeSingleDoseFromWeapon();
					poisonUsed = true;
				}

				Projectile projectile = self.WeaponProjectile;
				if (projectile != null) {
					poison = projectile.GetPlugin(PoisonedItemPlugin.poisonPK) as PoisonedItemPlugin;
					if (poison != null) {
						poison.Apply(self, swingArgs.defender, EffectFlag.HarmfulEffect | EffectFlag.FromPotion);
						poison.DetractSingleDoseFromAmmo();
						poisonUsed = true;
					}
				}

				if (!poisonUsed) {
					self.RemoveTriggerGroup(this);
				}
			}
		}
	}

	[Dialogs.ViewableClass]
	public partial class PoisonedItemPluginDef {
		public static readonly PoisonedItemPluginDef instance = (PoisonedItemPluginDef)
			new PoisonedItemPluginDef("p_poisoned_item", "C# scripts", -1).Register();
	}

	public class PoisonedItemLoc : CompiledLocStringCollection {
		public string DosesLeft = "Poison Doses";
		public string Power = "Poison Power";
	}

	//we do not use generated code because we want a customised copy constructor
	#region Originally generated
	public partial class PoisonedItemPluginDef : PluginDef {

		public PoisonedItemPluginDef(String defname, String filename, Int32 headerLine) :
			base(defname, filename, headerLine) {
		}

		protected override SteamEngine.Plugin CreateImpl() {
			return new PoisonedItemPlugin();
		}

		public new static void Bootstrap() {
			SteamEngine.PluginDef.RegisterPluginDef(typeof(PoisonedItemPluginDef), typeof(PoisonedItemPlugin));
		}
	}

	[SteamEngine.DeepCopyableClassAttribute()]
	[SteamEngine.Persistence.SaveableClassAttribute()]
	public partial class PoisonedItemPlugin : Plugin {

		private PoisonEffectPluginDef poisonType = null;

		private Int32 poisonPower = 0;

		private Int32 poisonTickCount = 0;

		private Int32 poisonDoses = 0;

		[SteamEngine.DeepCopyImplementationAttribute()]
		public PoisonedItemPlugin(PoisonedItemPlugin copyFrom) :
			base(copyFrom) {

			this.poisonType = copyFrom.poisonType;
			this.poisonPower = copyFrom.poisonPower;
			this.poisonTickCount = copyFrom.poisonTickCount;
			this.poisonDoses = copyFrom.poisonDoses / 2;
			copyFrom.poisonDoses -= this.poisonDoses;
		}

		[SteamEngine.Persistence.LoadingInitializerAttribute()]
		public PoisonedItemPlugin() {
		}

		public new PoisonedItemPluginDef TypeDef {
			get {
				return ((PoisonedItemPluginDef) (base.Def));
			}
		}

		[SteamEngine.Persistence.SaveAttribute()]
		public override void Save(SaveStream output) {
			if ((this.poisonType != null)) {
				output.WriteValue("poisonType", this.poisonType);
			}
			if ((this.poisonPower != 0)) {
				output.WriteValue("poisonPower", this.poisonPower);
			}
			if ((this.poisonTickCount != 0)) {
				output.WriteValue("poisonTickCount", this.poisonTickCount);
			}
			if ((this.poisonDoses != 0)) {
				output.WriteValue("poisonDoses", this.poisonDoses);
			}
			base.Save(output);
		}

		private void DelayedLoad_PoisonType(object resolvedObject, string filename, int line) {
			this.poisonType = ((PoisonEffectPluginDef) (resolvedObject));
		}

		[SteamEngine.Persistence.LoadLineAttribute()]
		public override void LoadLine(string filename, int line, string valueName, string valueString) {
			switch (valueName) {

				case "poisontype":
					SteamEngine.Persistence.ObjectSaver.Load(valueString, new SteamEngine.Persistence.LoadObject(this.DelayedLoad_PoisonType), filename, line);
					break;

				case "poisonpower":
					this.poisonPower = SteamEngine.Common.ConvertTools.ParseInt32(valueString);
					break;

				case "poisontickcount":
					this.poisonTickCount = SteamEngine.Common.ConvertTools.ParseInt32(valueString);
					break;

				case "poisondoses":
					this.poisonDoses = SteamEngine.Common.ConvertTools.ParseInt32(valueString);
					break;

				default:

					base.LoadLine(filename, line, valueName, valueString);
					break;
			}
		}
	}
	#endregion Originally generated
}


