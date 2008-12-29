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
using SteamEngine.Regions;
using SteamEngine.CompiledScripts;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	[Flags]
	public enum SpellFlag : int {
		None = 0,
		CanTargetStatic = 0x0001,
		CanTargetGround = 0x0002,
		CanTargetItem = 0x0004,
		CanTargetChar = 0x0008,
		CanTargetAnything = CanTargetStatic | CanTargetGround | CanTargetItem | CanTargetChar,
		AlwaysTargetSelf = 0x0010,
		//TargetNeedsLOS = 0x0020,
		CanEffectStatic = 0x0040,
		CanEffectGround = 0x0080,
		CanEffectItem = 0x0100,
		CanEffectChar = 0x0200,
		CanEffectAnything = CanEffectStatic | CanEffectGround | CanEffectItem | CanEffectChar,
		EffectNeedsLOS = 0x0400,
		IsAreaSpell = 0x0800,
		TargetCanMove = 0x1000, 
		UseMindPower = 0x2000, //otherwise, magery value is used, multiplied by the spells Effect value
		IsHarmful = 0x4000,
		IsBeneficial = 0x8000
	}

	[ViewableClass]
	public class SpellDef : AbstractDef {
		private static Dictionary<string, ConstructorInfo> spellDefCtorsByName = new Dictionary<string, ConstructorInfo>(StringComparer.OrdinalIgnoreCase);

		private static Dictionary<int, SpellDef> byId = new Dictionary<int, SpellDef>();

		private TriggerGroup scriptedTriggers;

		private int id;

		public static SpellDef ByDefname(string defname) {
			AbstractScript script;
			byDefname.TryGetValue(defname, out script);
			return script as SpellDef;
		}

		public static SpellDef ById(int key) {
			SpellDef retVal;
			byId.TryGetValue(key, out retVal);
			return retVal;
		}

		private static void RegisterSpellDef(SpellDef sd) {
			byDefname[sd.Defname] = sd;
			byId[sd.id] = sd;
		}

		private static void UnRegisterSpellDef(SpellDef sd) {
			byDefname.Remove(sd.Defname);
			byId.Remove(sd.id);
		}

		public static ICollection<SpellDef> AllSpellDefs {
			get {
				return byId.Values;
			}
		}

		#region Loading from scripts

		public static new void Bootstrap() {
			ClassManager.RegisterSupplySubclasses<SpellDef>(RegisterSpellDefType);
		}

		//for loading of Spelldefs from .scp scripts
		public static new bool ExistsDefType(string name) {
			return spellDefCtorsByName.ContainsKey(name);
		}

		private static Type[] SpellDefConstructorParamTypes = new Type[] { typeof(string), typeof(string), typeof(int) };

		//called by ClassManager
		internal static bool RegisterSpellDefType(Type spellDefType) {
			ConstructorInfo ci;
			if (spellDefCtorsByName.TryGetValue(spellDefType.Name, out ci)) { //we have already a SpellDef type named like that
				throw new OverrideNotAllowedException("Trying to overwrite class " + LogStr.Ident(ci.DeclaringType) + " in the register of SpellDef classes.");
			}
			ci = spellDefType.GetConstructor(SpellDefConstructorParamTypes);
			if (ci == null) {
				throw new Exception("Proper constructor not found.");
			}
			spellDefCtorsByName[spellDefType.Name] = MemberWrapper.GetWrapperFor(ci);

			ScriptLoader.RegisterScriptType(spellDefType.Name, LoadFromScripts, false);

			return false;
		}

		internal static void StartingLoading() {

		}

		internal static IUnloadable LoadFromScripts(PropsSection input) {
			//it is something like this in the .scp file: [headerType headerName] = [WarcryDef a_warcry] etc.
			string typeName = input.headerType.ToLower();

			string spellDefName = input.PopPropsLine("defname").value;

			AbstractScript def;
			byDefname.TryGetValue(spellDefName, out def);
			SpellDef spellDef = def as SpellDef;

			ConstructorInfo constructor = spellDefCtorsByName[typeName];

			if (spellDef == null) {
				if (def != null) {//it isnt SpellDef
					throw new ScriptException("SpellDef " + LogStr.Ident(spellDefName) + " has the same name as " + LogStr.Ident(def));
				} else {
					object[] cargs = new object[] { spellDefName, input.filename, input.headerLine };
					spellDef = (SpellDef) constructor.Invoke(cargs);
				}
			} else if (spellDef.unloaded) {
				if (spellDef.GetType() != constructor.DeclaringType) {
					throw new OverrideNotAllowedException("You can not change the class of a SpellDef while resync. You have to recompile or restart to achieve that. Ignoring.");
				}
				spellDef.unloaded = false;
				//we have to load the name first, so that it may be unloaded by it...

				PropsLine p = input.PopPropsLine("name");
				spellDef.LoadScriptLine(input.filename, p.line, p.name.ToLower(), p.value);

				UnRegisterSpellDef(spellDef);//will be re-registered again
			} else {
				throw new OverrideNotAllowedException("SpellDef " + LogStr.Ident(spellDefName) + " defined multiple times.");
			}

			if (!TagMath.TryParseInt32(input.headerName, out spellDef.id)) {
				throw new ScriptException("Unrecognized format of the id number in the skilldef script header.");
			}

			//now do load the trigger code. 
			if (input.TriggerCount > 0) {
				input.headerName = "t__" + spellDefName + "__";
				spellDef.scriptedTriggers = ScriptedTriggerGroup.Load(input);
			} else {
				spellDef.scriptedTriggers = null;
			}

			spellDef.LoadScriptLines(input);

			RegisterSpellDef(spellDef);

			if (spellDef.scriptedTriggers == null) {
				return spellDef;
			} else {
				return new UnloadableGroup(spellDef, spellDef.scriptedTriggers);
			}
		}

		internal static void LoadingFinished() {

		}

		#endregion Loading from scripts

		public override string ToString() {
			return string.Concat(this.defname, " (spellId ", this.id.ToString(), ")");
		}

		public bool TryCancellableTrigger(IPoint4D self, TriggerKey td, ScriptArgs sa) {
			//cancellable trigger just for the one triggergroup
			if (this.scriptedTriggers != null) {
				object retVal = this.scriptedTriggers.TryRun(self, td, sa);
				try {
					int retInt = Convert.ToInt32(retVal);
					if (retInt == 1) {
						return true;
					}
				} catch (Exception) {
				}
			}
			return false;
		}

		#region FieldValues
		private FieldValue scrollItem;
		private FieldValue runeItem;
		private FieldValue name;
		private FieldValue castTime;
		private FieldValue flags;
		private FieldValue manaUse;
		private FieldValue requirements;
		private FieldValue resources;
		private FieldValue difficulty;
		private FieldValue effect;
		private FieldValue duration;
		private FieldValue sound;
		private FieldValue runes;
		private FieldValue effectRange;

		public SpellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {

			this.name = this.InitField_Typed("name", null, typeof(string));
			this.scrollItem = this.InitField_ThingDef("scrollItem", null, typeof(SpellScrollDef));
			this.runeItem = this.InitField_ThingDef("runeItem", null, typeof(ItemDef));
			this.castTime = this.InitField_Typed("castTime", null, typeof(double));
			this.flags = this.InitField_Typed("flags", null, typeof(SpellFlag));
			this.manaUse = this.InitField_Typed("manaUse", 10, typeof(int));
			this.requirements = this.InitField_Typed("requirements", null, typeof(ResourcesList));
			this.resources = this.InitField_Typed("resources", null, typeof(ResourcesList));
			this.difficulty = this.InitField_Typed("difficulty", null, typeof(int));
			this.effect = this.InitField_Typed("effect", null, typeof(double[]));
			this.duration = this.InitField_Typed("duration", null, typeof(double[]));
			this.sound = this.InitField_Typed("sound", null, typeof(SoundNames));
			this.runes = this.InitField_Typed("runes", null, typeof(string));
			this.effectRange = this.InitField_Typed("effectRange", 5, typeof(int));
		}

		public int Id {
			get {
				return this.id;
			}
		}

		public string Name {
			get {
				return (string) this.name.CurrentValue;
			}
			set {
				this.name.CurrentValue = value;
			}
		}

		public SpellScrollDef ScrollItem {
			get {
				return (SpellScrollDef) this.scrollItem.CurrentValue;
			}
			set {
				this.scrollItem.CurrentValue = value;
			}
		}

		public ItemDef RuneItem {
			get {
				return (ItemDef) this.runeItem.CurrentValue;
			}
			set {
				this.runeItem.CurrentValue = value;
			}
		}

		public double CastTime {
			get {
				return (double) this.castTime.CurrentValue;
			}
			set {
				this.castTime.CurrentValue = value;
			}
		}

		public SpellFlag Flags {
			get {
				return (SpellFlag) this.flags.CurrentValue;
			}
			set {
				this.flags.CurrentValue = value;
			}
		}

		public int ManaUse {
			get {
				return (int) this.manaUse.CurrentValue;
			}
			set {
				this.manaUse.CurrentValue = value;
			}
		}

		public ResourcesList Requirements {
			get {
				return (ResourcesList) this.requirements.CurrentValue;
			}
			set {
				this.requirements.CurrentValue = value;
			}
		}

		public ResourcesList Resources {
			get {
				return (ResourcesList) this.resources.CurrentValue;
			}
			set {
				this.resources.CurrentValue = value;
			}
		}

		public int Difficulty {
			get {
				return (int) this.difficulty.CurrentValue;
			}
			set {
				this.difficulty.CurrentValue = value;
			}
		}

		public double[] Effect {
			get {
				return (double[]) this.effect.CurrentValue;
			}
			set {
				this.effect.CurrentValue = value;
			}
		}

		public double GetEffectForValue(double spellpower) {
			return ScriptUtil.EvalRangePermille(spellpower, this.Effect);
		}

		public double[] Duration {
			get {
				return (double[]) this.duration.CurrentValue;
			}
			set {
				this.duration.CurrentValue = value;
			}
		}

		public double GetDurationForValue(double spellpower) {
			return ScriptUtil.EvalRangePermille(spellpower, this.Duration);
		}

		public SoundNames Sound {
			get {
				return (SoundNames) this.sound.CurrentValue;
			}
			set {
				this.sound.CurrentValue = value;
			}
		}

		public string Runes {
			get {
				return (string) this.runes.CurrentValue;
			}
			set {
				this.runes.CurrentValue = value;
			}
		}

		public int EffectRange {
			get {
				return (int) this.effectRange.CurrentValue;
			}
			set {
				this.effectRange.CurrentValue = value;
			}
		}
		#endregion FieldValues

		internal void Trigger_Select(SkillSequenceArgs mageryArgs) {
			//Checked so far: death, book on self
			//TODO: Check zones, frozen?, hypnoform?, cooldown?
			Character caster = mageryArgs.Self;
			//ResourcesList req = this.Requirements;
			//if (req != null) {
			//    if (!req.HasResourcesPresent(
			//}


			SpellFlag flags = this.Flags;
			if ((flags & SpellFlag.AlwaysTargetSelf) == SpellFlag.AlwaysTargetSelf) {
				mageryArgs.Target1 = caster;
				mageryArgs.PhaseStart();
				return;
			} else if ((flags & SpellFlag.CanTargetAnything) != SpellFlag.None) {
				if (mageryArgs.Target1 == null) {//if not null, it already has a target
					Player self = caster as Player;
					if (self != null) {
						self.Target(SingletonScript<SpellTargetDef>.Instance, mageryArgs);
						return;
					} else {
						caster.AnnounceBug();
						throw new SEException("Only Players can target spells");
					}
				} else {
					mageryArgs.PhaseStart();
					//ConsumeResources(mageryArgs, caster);
					return;
				}
			}

			throw new SEException("SpellDef.Trigger_Select - unfinished");
		}
		
		private static TriggerKey tkSuccess = TriggerKey.Get("success");
		private static TriggerKey tkSpellEffect = TriggerKey.Get("spelleffect");
		private static TriggerKey tkEffectChar = TriggerKey.Get("effectchar");
		private static TriggerKey tkEffectItem = TriggerKey.Get("effectitem");
		private static TriggerKey tkEffectGround = TriggerKey.Get("effectground");

		internal void Trigger_Success(SkillSequenceArgs mageryArgs) {
			//target visibility/distance/LOS and self being alive checked in magery stroke
			Character caster = mageryArgs.Self;

			bool cancel = this.TryCancellableTrigger(caster, tkSuccess, mageryArgs.scriptArgs);
			if (!cancel) {
				cancel = this.On_Success(mageryArgs);
			}
			if (cancel) {
				return;
			}

			SpellFlag flags = this.Flags;
			IPoint4D target = mageryArgs.Target1;
			bool isArea = (flags & SpellFlag.IsAreaSpell) == SpellFlag.IsAreaSpell;
			SpellEffectArgs sea = null;

			bool singleEffectDone = false;
			Character targetAsChar = target as Character;
			if (targetAsChar != null) {
				if ((flags & SpellFlag.CanEffectChar) == SpellFlag.CanEffectChar) {
					singleEffectDone = true;
					sea = this.GetSpellPowerAgainstChar(caster, target, targetAsChar, sea);
					if (this.CheckSpellPowerWithMessage(sea)) {
						this.Trigger_EffectChar(targetAsChar, sea);
					}
				}
			} else {
				Item targetAsItem = target as Item;
				if (targetAsItem != null) {
					if ((flags & SpellFlag.CanEffectItem) == SpellFlag.CanEffectItem) {
						singleEffectDone = true;
						sea = this.GetSpellPowerAgainstNonChar(caster, target, targetAsItem, sea);
						if (this.CheckSpellPowerWithMessage(sea)) {
							this.Trigger_EffectItem(targetAsItem, sea);							
						}
					}
				}
			}
			if (!singleEffectDone) {
				if (((flags & SpellFlag.CanEffectGround) == SpellFlag.CanEffectGround) ||
					(((flags & SpellFlag.CanEffectStatic) == SpellFlag.CanEffectStatic) && (target is Static))) {
					singleEffectDone = true;

					sea = this.GetSpellPowerAgainstNonChar(caster, target, target, sea);
					if (this.CheckSpellPowerWithMessage(sea)) {
						this.Trigger_EffectGround(target, sea);
					}
				}
			}

			if (!singleEffectDone) {
				throw new Exception(this + ": Invalid target and/or spell flag?!");
			}

			if (isArea) {
				bool canEffectItem = (flags & SpellFlag.CanEffectItem) == SpellFlag.CanEffectItem;
				bool canEffectChar = (flags & SpellFlag.CanEffectChar) == SpellFlag.CanEffectChar;
				if (canEffectItem || canEffectChar) {
					foreach (Thing t in target.GetMap().GetThingsInRange(target.X, target.Y, this.EffectRange)) {
						Character ch = t as Character;
						if (ch != null) {
							if (canEffectChar) {
								//TODO: do not effect allies if harmful? Do not effect enemy if beneficial?
								sea = this.GetSpellPowerAgainstChar(caster, target, ch, sea);
								if (this.CheckSpellPowerWithMessage(sea)) {
									this.Trigger_EffectChar(ch, sea);
								}
							}
						} else if (canEffectItem) {
							Item i = t as Item;
							if (i != null) {
								sea = this.GetSpellPowerAgainstNonChar(caster, target, i, sea);
								if (this.CheckSpellPowerWithMessage(sea)) {
									this.Trigger_EffectItem(i, sea);
								}
							}
						}
					}
					//TODO? effect ground in the whole area? Or only statics?
				}
			}

			if (sea != null) {
				sea.Dispose();
			}
		}

		private bool CheckSpellPowerWithMessage(SpellEffectArgs sea) {
			SpellFlag flags = this.Flags;
			if ((flags & SpellFlag.IsHarmful) == SpellFlag.IsHarmful) {
				if (sea.SpellPower < 1) {
					sea.Caster.SysMessage("C�l odolal kouzlu!");
					Character targetAsChar = sea.CurrentTarget as Character;
					if (targetAsChar != null) {
						targetAsChar.ClilocSysMessage(501783); // You feel yourself resisting magical energy.
					}
					return false;
				}
			}
			return true;
		}

		private SpellEffectArgs GetSpellPowerAgainstChar(Character caster, IPoint4D mainTarget, Character currentTarget, SpellEffectArgs sea) {
			int spellPower;
			SpellFlag flags = this.Flags;
			if ((flags & SpellFlag.UseMindPower) == SpellFlag.UseMindPower) {
				int mindDef;
				if (caster.IsPlayerForCombat) {
					mindDef = currentTarget.MindDefenseVsP;
				} else {
					mindDef = currentTarget.MindDefenseVsM;
				}
				if (currentTarget.IsPlayerForCombat) {
					spellPower = caster.MindPowerVsP - mindDef;
				} else {
					spellPower = caster.MindPowerVsM - mindDef;
				}
			} else {
				spellPower = caster.GetSkill(SkillName.Magery);
			}

			if (sea == null) {
				sea = SpellEffectArgs.Acquire(caster, mainTarget, currentTarget, this, spellPower);				
			} else {
				sea.CurrentTarget = currentTarget;
				sea.SpellPower = spellPower;
			}
			return sea;
		}

		private SpellEffectArgs GetSpellPowerAgainstNonChar(Character caster, IPoint4D target, IPoint4D currentTarget, SpellEffectArgs sea) {
			int spellPower = caster.GetSkill(SkillName.Magery);
			if (sea == null) {
				sea = SpellEffectArgs.Acquire(caster, target, currentTarget, this, spellPower);
			} else {
				sea.CurrentTarget = currentTarget;
				sea.SpellPower = spellPower;
			}
			return sea;
		}

		private void Trigger_EffectChar(Character target, SpellEffectArgs spellEffectArgs) {
			bool cancel = target.TryCancellableTrigger(tkSpellEffect, spellEffectArgs.scriptArgs);
			if (!cancel) {
				try {
					cancel = target.On_SpellEffect(spellEffectArgs);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (!cancel) {
					cancel = this.TryCancellableTrigger(target, tkEffectChar, spellEffectArgs.scriptArgs);
					if (!cancel) {
						try {
							this.On_EffectChar(target, spellEffectArgs);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					}
				}
			}
		}

		private void Trigger_EffectItem(Item target, SpellEffectArgs spellEffectArgs) {
			bool cancel = target.TryCancellableTrigger(tkSpellEffect, spellEffectArgs.scriptArgs);
			if (!cancel) {
				try {
					cancel = target.On_SpellEffect(spellEffectArgs);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (!cancel) {
					cancel = this.TryCancellableTrigger(target, tkEffectItem, spellEffectArgs.scriptArgs);
					if (!cancel) {
						try {
							this.On_EffectItem(target, spellEffectArgs);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					}
				}
			}
		}

		private void Trigger_EffectGround(IPoint4D target, SpellEffectArgs spellEffectArgs) {
			bool cancel = this.TryCancellableTrigger(target, tkEffectGround, spellEffectArgs.scriptArgs);
			if (!cancel) {
				try {
					this.On_EffectGround(target, spellEffectArgs);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			}
		}

		public virtual bool On_Success(SkillSequenceArgs mageryArgs) {
			return false;
		}

		public virtual void On_EffectGround(IPoint4D target, SpellEffectArgs spellEffectArgs) {
		}

		public virtual void On_EffectChar(Character target, SpellEffectArgs spellEffectArgs) {
			if ((this.Flags & SpellFlag.IsHarmful) == SpellFlag.IsHarmful) {
				target.Trigger_HostileAction(spellEffectArgs.Caster);
			}
		}

		public virtual void On_EffectItem(Item target, SpellEffectArgs spellEffectArgs) {
		}
	}

	public class SpellEffectArgs : Poolable {
		Character caster;
		IPoint4D currentTarget;
		IPoint4D mainTarget;
		SpellDef spellDef;
		int spellPower;

		public readonly ScriptArgs scriptArgs;

		public SpellEffectArgs()
			: base() {
			this.scriptArgs = new ScriptArgs(this);
		}

		public static SpellEffectArgs Acquire(Character caster, IPoint4D mainTarget, IPoint4D currentTarget, SpellDef spellDef, int spellPower) {
			SpellEffectArgs retVal = Pool<SpellEffectArgs>.Acquire();
			retVal.caster = caster;
			retVal.mainTarget = mainTarget;
			retVal.currentTarget = currentTarget;
			retVal.spellDef = spellDef;
			retVal.spellPower = spellPower;
			return retVal;
		}

		//protected override void On_Reset() {
		//    this.source = null;
		//    this.target = null;
		//    this.spellDef = null;
		//    this.spellPower = 0;
		//    base.On_Reset();
		//}

		public Character Caster {
			get {
				return this.caster;
			}
		}

		public IPoint4D MainTarget {
			get {
				return this.mainTarget;
			}
		}

		public IPoint4D CurrentTarget {
			get {
				return this.currentTarget;
			}
			set {
				this.currentTarget = value;
			}
		}

		public SpellDef SpellDef {
			get {
				return this.spellDef;
			}
		}

		public int SpellPower {
			get {
				return this.spellPower;
			}
			set {
				this.spellPower = value;
			}
		}
	}


	public sealed class SpellTargetDef : CompiledTargetDef {
		//better without message?
		//protected override void On_Start(Player self, object parameter) {
		//    self.SysMessage("Vyber c�l kouzla");
		//    base.On_Start(self, parameter);
		//}

		protected override void On_TargonCancel(Player caster, object parameter) {
			SkillSequenceArgs mageryArgs = (SkillSequenceArgs) parameter;
			mageryArgs.Dispose();
		}

		protected override bool On_TargonGround(Player caster, IPoint4D targetted, object parameter) {
			SkillSequenceArgs mageryArgs = (SkillSequenceArgs) parameter;
			SpellDef spell = (SpellDef) mageryArgs.Param1;
			SpellFlag flags = spell.Flags;

			if ((flags & SpellFlag.CanTargetGround) == SpellFlag.CanTargetGround) {
				if (((flags & SpellFlag.TargetCanMove) != SpellFlag.TargetCanMove) && targetted is Thing) {
					//we pretend to have targetted the ground, cos we don't want it to move
					mageryArgs.Target1 = new Point4D(targetted.TopPoint);
				} else {
					mageryArgs.Target1 = targetted;
				}
				mageryArgs.PhaseStart();
			} else {
				return true; //repeat targetting
			}
			return false;
		}

		protected override bool On_TargonChar(Player caster, Character targetted, object parameter) {
			return this.TargonNonGround(caster, targetted, parameter, SpellFlag.CanTargetChar);
		}

		protected override bool On_TargonItem(Player caster, Item targetted, object parameter) {
			return this.TargonNonGround(caster, targetted, parameter, SpellFlag.CanTargetItem);
		}

		protected override bool On_TargonStatic(Player caster, Static targetted, object parameter) {
			return this.TargonNonGround(caster, targetted, parameter, SpellFlag.CanTargetStatic);
		}

		private bool TargonNonGround(Player caster, IPoint4D targetted, object parameter, SpellFlag targetSF) {
			SkillSequenceArgs mageryArgs = (SkillSequenceArgs) parameter;
			SpellDef spell = (SpellDef) mageryArgs.Param1;

			if ((spell.Flags & targetSF) == targetSF) {
				mageryArgs.Target1 = targetted;
				mageryArgs.PhaseStart();
			} else {
				return this.On_TargonGround(caster, targetted, parameter);
			}
			return false;
		}
	}
}