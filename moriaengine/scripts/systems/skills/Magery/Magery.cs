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

	[Dialogs.ViewableClass]
	public class MagerySkillDef : SkillDef {
		//skillTarget1: spell target
		//skillTarget2: scroll or spellbook item
		//skillParam1: SpellDef instance
		//skillParam2: spell param (summoned creature def?)


		public static void TryCastSpellFromBook(Character ch, int spellid) {
			SpellDef sd = SpellDef.ById(spellid);
			if (sd != null) {
				TryCastSpellFromBook(ch, sd);
			} else {
				ch.ClilocMessage(502345); // This spell has been temporarily disabled.
			}
		}

		public static void TryCastSpellFromBook(Character ch, SpellDef spellDef) {
			Sanity.IfTrueThrow(spellDef == null, "spellDef == null");

			SpellBook book = ch.FindLayer(1) as SpellBook;
			if (book != null) {
				if (!book.HasSpell(spellDef)) {
					book = null;
				}
			}
			if (book == null) {
				foreach (Item i in ch.Backpack) {
					book = i as SpellBook;
					if (book != null) {
						if (book.HasSpell(spellDef)) {
							break;
						}
					}
				}
			}
			if (book != null) {
				SkillSequenceArgs magery = SkillSequenceArgs.Acquire(ch, SkillName.Magery, book, spellDef);
				magery.PhaseSelect();
			} else {
				ch.ClilocSysMessage(500015); // You do not have that spell!
			}
		}

		public MagerySkillDef(string defname, string filename, int line)
			: base(defname, filename, line) {
		}

		protected override bool On_Select(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			SpellBook book = skillSeqArgs.Tool as SpellBook;
			SpellDef spell = (SpellDef) skillSeqArgs.Param1;
			if (book != null) {
				if ((book.TopObj() == self) && book.HasSpell(spell)) {
					spell.Trigger_Select(skillSeqArgs);
					return true; //cancel here, the rest of Select is being done by SpellDef
				}
			}

			self.WriteLine("Casting without book not implemented yet");
			//TODO scrolls

			return true;
		}

		protected override bool On_Start(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			if (CanSeeTargetWithMessage(skillSeqArgs, self)) {
				SpellDef spell = (SpellDef) skillSeqArgs.Param1;
				int manaUse = spell.ManaUse;
				int mana = self.Mana;
				if (self.Mana >= manaUse) {
					ResourcesList req = spell.Requirements;
					ResourcesList res = spell.Resources;

					if (((req == null) || (req.HasResourcesPresent(self, ResourcesLocality.WearableLayers | ResourcesLocality.Backpack))) &&
							((res == null) || (res.ConsumeResourcesOnce(self, ResourcesLocality.Backpack)))) {
						self.Mana = (short) (mana - manaUse);
						AnimCalculator.PerformAnim(self, GenericAnim.Cast);

						self.AbortSkill();
						skillSeqArgs.DelayInSeconds = spell.CastTime;
						skillSeqArgs.DelayStroke();
						return true; //default = set delay by magery skilldef
					} else {
						self.ClilocSysMessage(502630); // More reagents are needed for this spell.
					}
				} else {
					self.ClilocSysMessage(502625); // Insufficient mana for this spell.
				}
			}
			skillSeqArgs.Dispose();
			return true;
		}

		private static bool CanSeeTargetWithMessage(SkillSequenceArgs skillSeqArgs, Character self) {
			IPoint4D target = skillSeqArgs.Target1;
			IPoint4D targetTop = target.TopPoint;
			Thing targetAsThing = target as Thing;

			int m = self.M;
			if (m == targetTop.M) {
				Regions.Map map = Regions.Map.GetMap(m);
				if (targetAsThing != null) {
					if ((!targetAsThing.IsDeleted) && (!targetAsThing.Flag_Disconnected)) {
						if (self.CanSeeForUpdate(targetAsThing) && (map.CanSeeLOSFromTo(self, targetTop))) {
							return true;
						}
					}
				} else if (target != null) {
					if ((Point2D.GetSimpleDistance(self, targetTop) <= Globals.MaxUpdateRange) && (map.CanSeeLOSFromTo(self, targetTop))) {
						return true;
					}
				}
			}

			self.ClilocSysMessage(3000269);	//That is out of sight.
			return false;
		}

		protected override bool On_Stroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			SpellBook book = skillSeqArgs.Tool as SpellBook;
			SpellDef spell = (SpellDef) skillSeqArgs.Param1;

			skillSeqArgs.Success = false;
			if (book != null) {
				if (book.IsDeleted || (book.TopObj() != self)) {
					self.ClilocSysMessage(501608);	//You don't have a spellbook.
					return false;
				} else if (!book.HasSpell(spell)) {
					self.ClilocSysMessage(501902);	//You don't know that spell.
					return false;
				}
			} else {
				self.WriteLine("Casting without book not implemented yet");
				return false;
			}

			if (!CanSeeTargetWithMessage(skillSeqArgs, self)) {
				return false;
			}

			skillSeqArgs.Success = this.CheckSuccess(self, spell.Difficulty);

			return false;
		}

		protected override bool On_Success(SkillSequenceArgs skillSeqArgs) {
			SpellDef spell = (SpellDef) skillSeqArgs.Param1;
			spell.Trigger_Success(skillSeqArgs);
			return false;
		}

		protected override bool On_Fail(SkillSequenceArgs skillSeqArgs) {
			Fizzle(skillSeqArgs.Self);
			return false;
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			Fizzle(skillSeqArgs.Self);
		}

		private static void Fizzle(Character self) {
			EffectFactory.StationaryEffect(self, 0x3735, 6, 30);
			self.Sound(0x5C);
			self.ClilocMessage(502632); // The spell fizzles.
		}
	}
}