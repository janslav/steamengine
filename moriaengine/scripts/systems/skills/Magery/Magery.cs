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
			//we have the spell target

			throw new Exception("The method or operation is not implemented.");
		}

		protected override bool On_Fail(SkillSequenceArgs skillSeqArgs) {
			throw new Exception("The method or operation is not implemented.");
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			throw new Exception("The method or operation is not implemented.");
		}

		protected override bool On_Stroke(SkillSequenceArgs skillSeqArgs) {
			throw new Exception("The method or operation is not implemented.");
		}

		protected override bool On_Success(SkillSequenceArgs skillSeqArgs) {
			throw new Exception("The method or operation is not implemented.");
		}

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
				ch.ClilocMessage(500015); // You do not have that spell!
			}
		}
	}
}