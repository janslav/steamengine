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
using SteamEngine;
using SteamEngine.Persistence;
using SteamEngine.Common;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {
	public class DiscordanceSkillDef : SkillDef {

		public DiscordanceSkillDef(string defname, string filename, int headerLine) : base(defname, filename, headerLine) {
		}

		private static TriggerGroup t_Musical;
		private static AbstractTargetDef td_Discordance;

		public TriggerGroup T_Musical {
			get {
				if (t_Musical == null) {
					t_Musical = TriggerGroup.Get("t_musical");
				}
				return t_Musical;
			}
		}

		public AbstractTargetDef TD_Discordance {
			get {
				if (td_Discordance == null) {
					td_Discordance = AbstractTargetDef.Get("Targ_Discordance");
				}
				return td_Discordance;
			}
		}

		public override void Select(AbstractCharacter ch) {
			Character self = (Character) ch;
			self.currentSkillTarget2 = ((Item)self.Backpack).FindType(T_Musical);
			((Player)self).Target(TD_Discordance);
		}

		internal override void Start(Character self) {
			if (!this.Trigger_Start(self)) {
				self.SysMessage("Pokousis se oslabit " + ((Character) self.currentSkillTarget1).Name + ".");
				DelaySkillStroke(self);
			}
		}
		
		public override void Stroke(Character self) {
			if (!this.Trigger_Stroke(self)) {
				if (GetValueForChar(((Character) self.currentSkillTarget1)) != 0) {
					self.SysMessage("Tohle nelze oslabit.");
				} else if (Convert.ToInt32(self.currentSkillParam) == 1) {
					double targExperience = ((Character) self.currentSkillTarget1).Experience;
					if ((GetValueForChar(self) * 0.3) < targExperience) {
						self.SysMessage("Oslabeni tohoto cile presahuje tve moznosti.");
					} else {
						double discordancePower = Globals.EvalRandomFaktor(GetValueForChar(self), 0, 300);
						if (discordancePower > targExperience) {
							Success(self);
							return;
						} else {
							self.SysMessage("Oslabeni se nepovedlo.");
						}
					}
				} else {
					self.SysMessage("Oslabeni se nepovedlo.");
				}
				Fail(self);
			}
		}
		
		public override void Success(Character self) {
			if (!this.Trigger_Success(self)) {
				if (((Character) self.currentSkillTarget1).FindMemory(DiscordanceEffectMemoryDef.Instance) != null) {
					self.SysMessage("Cil je jiz oslaben.");
				} else {
					self.SysMessage("Uspesne jsi oslabil cil.");
					DiscordanceEffectMemoryDef.Instance.Create(((Character)self.currentSkillTarget1));
					DiscordanceEffectMemory newMemory = (DiscordanceEffectMemory)Globals.lastNew;
					newMemory.discordEffectPower = GetValueForChar(self);
					newMemory.DiscordEffectStart();
				}
			}
			self.CurrentSkill = null;
		}
		
		public override void Fail(Character self) {
			if (!this.Trigger_Fail(self)) {
				//self.currentSkillTarget.Attack(self) - nebo neco takovyho, proste potvoru poslat na me nebo zvysit AGGRO
			}
			self.CurrentSkill = null;
		}
		
		protected internal override void Abort(Character self) {
			if (!this.Trigger_Abort(self)) {
				self.SysMessage("Oslabovani bylo predcasne preruseno.");
			}
			self.CurrentSkill = null;
		}
	}


	public class Targ_Discordance : CompiledTargetDef {

		protected override void On_Start(Character self, object parameter)
		{
			self.SysMessage("Koho chces zkusit oslabit?");
			base.On_Start(self, parameter);
		}

		protected override bool On_TargonChar(Character self, Character targetted, object parameter)
		{
			if (targetted.IsPlayer) {
				self.SysMessage("Zameruj jenom monstra!");
				return false;
			} else if (self.CurrentSkill != null) {
				self.ClilocSysMessage(500118);                    //You must wait a few moments to use another skill.
				return false;
			} else if ((targetted.FindMemory(DiscordanceEffectMemoryDef.Instance)) != null) {
				self.SysMessage("Cil je jiz oslaben.");
				self.CurrentSkill = null;
				return false;
			}
			self.SelectSkill((int)SkillName.Musicianship);
			if ((int)self.currentSkillParam == 2) {
				return false;
			}
			self.currentSkillTarget1 = targetted;
			self.StartSkill((int)SkillName.Discordance);
			return false;
		}

		protected override bool On_TargonItem(Character self, Item targetted, object parameter)
		{
			self.SysMessage("Predmety nelze oslabit.");
			return false;
		}

		protected override bool On_TargonStatic(Character self, Static targetted, object parameter)
		{
			return true;
		}

		protected override bool On_TargonGround(Character self, IPoint3D targetted, object parameter)
		{
			return true;
		}

		protected override void On_TargonCancel(Character self, object parameter)
		{
			self.SysMessage("Target zrusen");
		}
	}

	[SaveableClass]
	public class DiscordanceEffectMemory : Memory {

		[SaveableData]
		public int discordEffectPower = 0;

		[SaveableData]
		public short lowed_dex = 0;
		[SaveableData]
		public short lowed_str = 0;
		[SaveableData]
		public short lowed_int = 0;
		[SaveableData]
		public short lowed_hits = 0;
		[SaveableData]
		public short lowed_mana = 0;
		[SaveableData]
		public short lowed_stam = 0;
		[SaveableData]
		public ushort lowed_magery = 0;
		[SaveableData]
		public ushort lowed_ei = 0;
		[SaveableData]
		public ushort lowed_resist = 0;
		[SaveableData]
		public ushort lowed_wrestl = 0;
		[SaveableData]
		public ushort lowed_archer = 0;
		[SaveableData]
		public ushort lowed_mace = 0;
		[SaveableData]
		public ushort lowed_onehand = 0;
		[SaveableData]
		public ushort lowed_twohand = 0;
		[SaveableData]
		public ushort lowed_poison = 0;

		[LoadingInitializer]
		public DiscordanceEffectMemory() {
		}

		public DiscordanceEffectMemory(DiscordanceEffectMemoryDef def)
			: base(def) {
		}

		protected DiscordanceEffectMemory(DiscordanceEffectMemory copyFrom)
			: base(copyFrom) {
			this.lowed_dex = copyFrom.lowed_dex;
			this.lowed_str = copyFrom.lowed_str;
			this.lowed_int = copyFrom.lowed_int;
			this.lowed_hits = copyFrom.lowed_hits;
			this.lowed_mana = copyFrom.lowed_mana;
			this.lowed_stam = copyFrom.lowed_stam;
			this.lowed_magery = copyFrom.lowed_magery;
			this.lowed_ei = copyFrom.lowed_ei;
			this.lowed_resist = copyFrom.lowed_resist;
			this.lowed_wrestl = copyFrom.lowed_wrestl;
			this.lowed_archer = copyFrom.lowed_archer;
			this.lowed_mace = copyFrom.lowed_mace;
			this.lowed_onehand = copyFrom.lowed_onehand;
			this.lowed_twohand = copyFrom.lowed_twohand;
			this.lowed_poison = copyFrom.lowed_poison;
			this.discordEffectPower = copyFrom.discordEffectPower;
		}

		internal override Memory Dupe() {
			Sanity.IfTrueThrow((this.GetType() != typeof(DiscordanceEffectMemory)), "Dupe() needs to be overriden by subclasses");
			return new DiscordanceEffectMemory(this);
		}

		public void DiscordEffectStart() {
			int lowerConst = discordEffectPower / 4;

			lowed_dex = (short) DiscordanceValueLower(Cont.Dex, lowerConst);
			lowed_str = (short) DiscordanceValueLower(Cont.Str, lowerConst);
			lowed_int = (short) DiscordanceValueLower(Cont.Int, lowerConst);
			lowed_hits = (short) DiscordanceValueLower(Cont.MaxHits, lowerConst);
			lowed_mana = (short) DiscordanceValueLower(Cont.MaxMana, lowerConst);
			lowed_stam = (short) DiscordanceValueLower(Cont.MaxStam, lowerConst);
			lowed_ei = (ushort) DiscordanceValueLower(Cont.Skills[16].RealValue, lowerConst);
			lowed_magery = (ushort) DiscordanceValueLower(Cont.Skills[25].RealValue, lowerConst);
			lowed_resist = (ushort) DiscordanceValueLower(Cont.Skills[26].RealValue, lowerConst);
			lowed_poison = (ushort) DiscordanceValueLower(Cont.Skills[30].RealValue, lowerConst);
			lowed_archer = (ushort) DiscordanceValueLower(Cont.Skills[31].RealValue, lowerConst);
			lowed_twohand = (ushort) DiscordanceValueLower(Cont.Skills[40].RealValue, lowerConst);
			lowed_mace = (ushort) DiscordanceValueLower(Cont.Skills[41].RealValue, lowerConst);
			lowed_onehand = (ushort) DiscordanceValueLower(Cont.Skills[42].RealValue, lowerConst);
			lowed_wrestl = (ushort) DiscordanceValueLower(Cont.Skills[43].RealValue, lowerConst);

			Cont.Dex -= lowed_dex;
			Cont.Str -= lowed_str;
			Cont.Int -= lowed_int;
			Cont.MaxHits -= lowed_hits;
			Cont.MaxMana -= lowed_mana;
			Cont.MaxStam -= lowed_stam;
			Cont.Skills[16].RealValue -= lowed_ei;
			Cont.Skills[25].RealValue -= lowed_magery;
			Cont.Skills[26].RealValue -= lowed_resist;
			Cont.Skills[30].RealValue -= lowed_poison;
			Cont.Skills[31].RealValue -= lowed_archer;
			Cont.Skills[40].RealValue -= lowed_twohand;
			Cont.Skills[41].RealValue -= lowed_mace;
			Cont.Skills[42].RealValue -= lowed_onehand;
			Cont.Skills[43].RealValue -= lowed_wrestl;

			if (Cont.Hits > Cont.MaxHits) {
				Cont.Hits = Cont.MaxHits;
			}
			if (Cont.Mana > Cont.MaxMana) {
				Cont.Mana = Cont.MaxMana;
			}
			if (Cont.Stam > Cont.MaxStam) {
				Cont.Stam = Cont.MaxStam;
			}
			//new DiscordDecayTimer(this, TimeSpan.FromSeconds(Globals.EvalRangePermille(discordEffectPower, 10, 15))).Enqueue();
		}

		public override void On_Equip(Character self) {
			//Console.WriteLine("DiscordanceEffectMemory  On_Equip src:"+Globals.src+", self:"+self+", cont:"+Cont);
		}

		public override void On_UnEquip(Character self) {
			Cont.Dex += lowed_dex;
			Cont.Str += lowed_str;
			Cont.Int += lowed_int;
			Cont.MaxHits += lowed_hits;
			Cont.MaxMana += lowed_mana;
			Cont.MaxStam += lowed_stam;
			Cont.Skills[16].RealValue += lowed_ei;
			Cont.Skills[25].RealValue += lowed_magery;
			Cont.Skills[26].RealValue += lowed_resist;
			Cont.Skills[30].RealValue += lowed_poison;
			Cont.Skills[31].RealValue += lowed_archer;
			Cont.Skills[40].RealValue += lowed_twohand;
			Cont.Skills[41].RealValue += lowed_mace;
			Cont.Skills[42].RealValue += lowed_onehand;
			Cont.Skills[43].RealValue += lowed_wrestl;
		}

		public int DiscordanceValueLower(int value,int lowerConst) {
			int lowedVal = ((value * lowerConst) / 1000);
			if (value < lowedVal) {
				lowedVal = value;
			}
			return lowedVal;
		}

		//private static TimerKey discordTimerKey = TimerKey.Get("_discordDecayTimer_");

		//[ManualDeepCopyClass]
		//public class DiscordDecayTimer : Timer {
		//    public DiscordDecayTimer(TimerKey name)
		//        : base(name) {
		//    }

		//    [DeepCopyImplementation]
		//    public DiscordDecayTimer(DiscordDecayTimer copyFrom)
		//        : base(copyFrom) {
		//    }

		//    public DiscordDecayTimer(Memory obj, TimeSpan time)
		//        : base(obj, discordTimerKey, time, null) {
		//    }

		//    protected sealed override void OnTimeout() {
		//        Memory self = Cont as Memory;
		//        if (self != null) {
		//            self.Delete();
		//        }
		//    }
		//}
	}

	public class DiscordanceEffectMemoryDef : MemoryDef {

		private static DiscordanceEffectMemoryDef instance;

		public static DiscordanceEffectMemoryDef Instance { get {
			return instance;
		} }

		public DiscordanceEffectMemoryDef()
				: base("m_discordanceMemory", "C#scripts", -1) {
			instance = this;
		}

		public override Memory Create() {
			return new DiscordanceEffectMemory(this);
		}
	}
}