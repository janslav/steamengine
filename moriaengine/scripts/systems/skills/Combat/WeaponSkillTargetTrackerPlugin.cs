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

	partial class WeaponSkillTargetTrackerPlugin {
		HashSet<Character> attackers = new HashSet<Character>();

		private static PluginKey weaponSkillTargetPK = PluginKey.Get("_weaponSkillTarget_");

		public static void InstallTargetTracker(Character self, Character attacker) {
			WeaponSkillTargetTrackerPlugin p = self.GetPlugin(weaponSkillTargetPK) as WeaponSkillTargetTrackerPlugin;
			if (p == null) {
				p = (WeaponSkillTargetTrackerPlugin) self.AddNewPlugin(weaponSkillTargetPK, WeaponSkillTargetTrackerPluginDef.instance);
			}
			p.attackers.Add(attacker);
		}

		public static void UnInstallTargetTracker(Character self, Character attacker) {
			WeaponSkillTargetTrackerPlugin p = self.GetPlugin(weaponSkillTargetPK) as WeaponSkillTargetTrackerPlugin;
			if (p != null) {
				p.attackers.Remove(attacker);
				if (p.attackers.Count == 0) {
					p.Delete();
				}
			}
		}

		private void CheckAttackers() {
			List<Character> toBeDeleted = null;
			foreach (Character attacker in this.attackers) {
				if (!(attacker.currentSkill is WeaponSkillDef) ||
						attacker.currentSkillTarget1 != this.Cont) {//the attacker is not attacking us, let's stop tracking.
					if (toBeDeleted == null) {
						toBeDeleted = new List<Character>();
					}
					toBeDeleted.Add(attacker);
				}
			}
			if (toBeDeleted != null) {
				foreach (Character delete in toBeDeleted) {
					attackers.Remove(delete);
				}
			}
			if (this.attackers.Count == 0) {
				this.Delete();
			}

			foreach (Character attacker in this.attackers) {
				attacker.currentSkill.Stroke(attacker);
			}
		}

		public void On_Step() {
			CheckAttackers();
		}

		public void On_VisibilityChange() {
			CheckAttackers();
		}

		public void On_NewPosition() {
			CheckAttackers();
		}

		public void On_Death() {
			this.Delete();
		}

	}

	partial class WeaponSkillTargetTrackerPluginDef {
		public static WeaponSkillTargetTrackerPluginDef instance = new WeaponSkillTargetTrackerPluginDef("p_weaponSkillTargetTracker", "C# scripts", -1);
	}
}
