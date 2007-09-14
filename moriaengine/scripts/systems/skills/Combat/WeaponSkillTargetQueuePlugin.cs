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
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	partial class WeaponSkillTargetQueuePlugin {
		LinkedList<Character> targetQueue = new LinkedList<Character>();
		Dictionary<Character, LinkedListNode<Character>> targetDict = new Dictionary<Character, LinkedListNode<Character>>();

		private new Character Cont {
			get {
				return (Character) base.Cont;
			}
		}

		[SteamFunction]
		public static void AddTarget(Character self, Character target) {
			CombatPluginCreate(self).AddTarget(target);
		}

		[SteamFunction]
		public static void RemoveTarget(Character self, Character target) {
			WeaponSkillTargetQueuePlugin p = self.GetPlugin(combatPluginPK) as WeaponSkillTargetQueuePlugin;
			if (p != null) {
				p.RemoveTarget(target);
			}
		}

		[SteamFunction]
		public static void FightCurrentTarget(Character self) {
			WeaponSkillTargetQueuePlugin p = self.GetPlugin(combatPluginPK) as WeaponSkillTargetQueuePlugin;
			if (p != null) {
				p.FightCurrentTarget();
			}
		}

		public void AddTarget(Character target) {
			bool isNew = false;
			LinkedListNode<Character> node;
			if (targetDict.TryGetValue(target, out node)) {
				if (target != targetQueue.First.Value) {
					isNew = true;
					targetQueue.Remove(node);
					targetQueue.AddFirst(target);
				}
			} else {
				isNew = true;
				targetQueue.AddFirst(target);
			}

			if (isNew) {
				FightCurrentTarget();
			}
		}

		public void RemoveTarget(Character target) {
			LinkedListNode<Character> node;
			bool removedFirst = false;
			if (targetDict.TryGetValue(target, out node)) {
				if (target == targetQueue.First.Value) {
					removedFirst = true;
				}
				targetQueue.Remove(node);
				targetQueue.AddFirst(target);
			}
			if (removedFirst) {
				if (targetQueue.Count == 0) {
					this.Delete();
				}
			}
		}

		public void FightCurrentTarget() {
			if (targetQueue.Count > 0) {
				this.Timer = CombatSettings.instance.secondsToRememberTargets;
				Character target = targetQueue.First.Value;
				Cont.AbortSkill();
				Cont.currentSkillTarget1 = target;
				Cont.SelectSkill(GetSkillNameByWeaponType(Cont.WeaponType));
			} else {
				this.Delete();//also aborts the skill
			}
		}

		public static SkillName GetSkillNameByWeaponType(WeaponType weapType) {
			switch (weapType) {
				case WeaponType.BareHands:
					return SkillName.Wrestling;
				case WeaponType.ArcheryStand:
				case WeaponType.ArcheryRunning:
					return SkillName.Archery;
				case WeaponType.TwoHandAxe:
				case WeaponType.TwoHandBlade:
					return SkillName.Swords;
				case WeaponType.OneHandBlade:
				case WeaponType.OneHandAxe:
				case WeaponType.OneHandSpike:
				case WeaponType.TwoHandSpike:
					return SkillName.Fencing;
				case WeaponType.OneHandBlunt:
				case WeaponType.TwoHandBlunt:
					return SkillName.Macing;
			}
			throw new ArgumentOutOfRangeException("weapType");
		}

		public void On_WarModeChange() {
			if (!Cont.Flag_WarMode) {//
				this.Delete();
			}
		}

		public void On_UnAssign(Character cont) {
			if (cont.currentSkill is WeaponSkillDef) {
				cont.AbortSkill();
			}
		}

		public void On_Assign() {
			this.Timer = CombatSettings.instance.secondsToRememberTargets;
		}

		public void On_Timer() {
			this.Delete();
		}

		public void On_Death() {
			this.Delete();
		}

		public void On_Step() {
			TryStrokeWeapSkill();
		}

		public void On_NewPosition() {
			TryStrokeWeapSkill();
		}

		private void TryStrokeWeapSkill() {
			WeaponSkillDef skill = Cont.currentSkill as WeaponSkillDef;
			if (skill != null) {
				skill.Stroke(Cont);
			}
		}

		public void On_ItemEquip(AbstractCharacter droppingChar, AbstractItem i) {
			if (i is Weapon) {
				Cont.InvalidateCombatValues();
				if (Cont.currentSkill is WeaponSkillDef) {
					Cont.AbortSkill();
					this.FightCurrentTarget();
				}
			}
		}

		public void On_ItemUnEquip(AbstractCharacter pickingChar, AbstractItem i) {
			if (i is Weapon) {
				Cont.InvalidateCombatValues();
				if (Cont.currentSkill is WeaponSkillDef) {
					Cont.AbortSkill();
					this.FightCurrentTarget();
				}
			}
		}

		private static PluginKey combatPluginPK = PluginKey.Get("_combatPlugin_");

		[SteamFunction]
		public static WeaponSkillTargetQueuePlugin CombatPluginCreate(Character self) {
			WeaponSkillTargetQueuePlugin p = self.GetPlugin(combatPluginPK) as WeaponSkillTargetQueuePlugin;
			if (p == null) {
				p = (WeaponSkillTargetQueuePlugin) self.AddNewPlugin(combatPluginPK, WeaponSkillTargetQueuePluginDef.instance);
			}
			return p;
		}

		[SteamFunction]
		public static WeaponSkillTargetQueuePlugin CombatPluginNoCreate(Character self) {
			return self.GetPlugin(combatPluginPK) as WeaponSkillTargetQueuePlugin;
		}
	}

	partial class WeaponSkillTargetQueuePluginDef {
		public static WeaponSkillTargetQueuePluginDef instance = new WeaponSkillTargetQueuePluginDef("p_weaponSkillTargetQueue", "C# scripts", -1);
	}

}