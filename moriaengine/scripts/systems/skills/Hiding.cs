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
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	public class HidingSkillDef : SkillDef {
		
		public HidingSkillDef(string defname, string filename, int headerLine) : base( defname, filename, headerLine ) {
		}
		
		public override void Select(AbstractCharacter ch) {
			//todo: various state checks...
			Character self = (Character) ch;
			
			if (!this.Trigger_Select(self)) {
				self.StartSkill((int) SkillName.Hiding);
			}
		}

		internal override void Start(Character self) {
			if (!this.Trigger_Start(self)) {
				self.CurrentSkill = this;
				DelaySkillStroke(self);
			}
		}
		
		public override void Stroke(Character self) {
			//todo: various state checks...
			if (!this.Trigger_Stroke(self)) {
				if (CheckSuccess(self, Globals.dice.Next(700))) {
					Success(self);
				} else {
					Fail(self);
				}
			}
			self.CurrentSkill = null;
		}
		
		public override void Success(Character self) {
			if (!this.Trigger_Success(self)) {
				self.Flag_Hidden = true;
				self.AddTriggerGroup(E_skill_hiding.Instance);
				self.ClilocSysMessage(501240);//You have hidden yourself well.
				//todo: gain
			}
		}
		
		public override void Fail(Character self) {
			if (!this.Trigger_Fail(self)) {
				self.ClilocSysMessage(501241);//You can't seem to hide here.
			}
		}
		
		protected internal override void Abort(Character self) {
			if (!this.Trigger_Abort(self)) {
				self.SysMessage("Hiding aborted.");
			}
			self.CurrentSkill = null;
		}
	}
	
	public class E_skill_hiding : CompiledTriggerGroup {
		private static TriggerGroup instance;
		public static TriggerGroup Instance { get {
			return instance;
		} }

		protected E_skill_hiding() {
			instance = this;
		}
		
		public void On_SkillStart(Character self, Character selfToo, ushort skillId) {
			//according to uo stratics, these skills do not unhide...
			switch ((SkillName) skillId) {
				case SkillName.DetectHidden:
				case SkillName.ItemID:
				case SkillName.Anatomy:
				case SkillName.ArmsLore:
				case SkillName.AnimalLore:
				case SkillName.EvalInt:
				case SkillName.Forensics:
				case SkillName.Poisoning:
				case SkillName.Stealth:
					return;
			}
			UnHide(self);
		}
		
		public void On_Step(Character self, byte direction, bool running) {
			if (self.StealthStepsLeft < 1) {
				self.SelectSkill((short) SkillName.Stealth);
			}
			if (self.StealthStepsLeft < 1) {//stealth was not succesfull
				UnHide(self);
				return;
			}
			if (running) {
				self.StealthStepsLeft -=2;
			} else {
				self.StealthStepsLeft --;
			}
		}
		
		private void UnHide(Character self) {
			if (self.Flag_Hidden) {
				self.ClilocSysMessage(501242); //You are no longer hidden.
				self.Flag_Hidden = false;
			}
			self.RemoveTriggerGroup(this);
		}
		
		//todo: looting others should also unhide
	}
}




//

//	ushort range = (ushort)(18 - (self.Skills[(int)SkillName.Hiding].RealValue / (ushort)10 ));
//	
//	Map map = self.GetMap();
//	EnumeratorOfPlayers enumer = map.GetPlayersInRange(self.X, self.Y, range);
//	
//	bool badcombat = ( enumer.MoveNext() );
//	bool ok = ( !badcombat );
//	
//	if ( ok )
//	{
//		foreach ( Character chars in map.GetPlayersInRange(self.X, self.Y, range) ) {
//			if ( chars.CanSee( self ) ) {
//				badcombat = true;
//				ok = false;
//				break;
//			}
//		}
//		
//		ok = ( !badcombat );
//	}
//	
//	if ( badcombat ) {
//		self.SysMessage( 501237, 0x22, "" ); //You can't seem to hide right now.
//		return;
//	}
//	else {
//		if ( ok )
//		{
//			self.OverheadMessage( 501240, 0x1F4, "" ); //You have hidden yourself well.
//			self.Flag_Hidden = true;
//			return;
//		}
//		else {
//			self.OverheadMessage( 501241, 0x22, "" ); //You can't seem to hide here.
//			return;
//		}
//	}
//}