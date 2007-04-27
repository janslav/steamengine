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
using System.IO;
using SteamEngine.Common;
using SteamEngine.Packets;	//for DeleteRequestReturnValue

namespace SteamEngine {
	//This class has methods which are called when various packets are recieved from clients.
	//These methods exist to note things that should be implemented later, but aren't yet.
	//If you want to implement something that is in here, go right ahead, but do a search on the method name
	//you're replacing to find out what calls it, and change the calls to point to your new method
	//(And then delete or comment out the method here).
	
	//(I'm basically going through and adding a bunch of packets without worrying about
	// making systems to hook them up to, etc.)
	
	//Also, at the bottom of this file, outside the class itself, there is a list of new methods in OutPackets.cs
	//which can be used to send information to the client. The listed methods aren't actually used by anything presently,
	//but that's what the list is for - so nobody accidentally reimplements a packet-sending method which already exists
	//and is only waiting for someone to come along and use it.
	
	//-SL
	
	//Most of these methods will print debug information when they are called, and all of them have extensive
	//documentation to aid implementation. I recommend keeping the documentation, and existing parameters and
	//return values, to make things as easy for you as possible.
	
	//Sanity checks exist to verify assumptions. One good guideline is not to assume something that isn't tested
	//by a sanity check. If you're going to assume something which isn't checked, add a sanity check for it.
	//Sanity checks only exist in the debug build, so they won't slow down the release builds.
	
	internal class Temporary {
		
		/*
			Method: MakeNewArea
				Called by God-Client packet handling code. There ought to be a command to do the same thing with a
				normal client.
			
				The god client has the user enter upper left and lower right coordinates, but sends width and height
				instead of lower right coordinates. If you put in, say, UL(10,10) and LR(20,30), the GC will send
				UL(10,10) and Width(10) and Height(20).
				
				The z coordinates sent by the god client range from 0-255. I don't know why that is - Elsewhere
				they normally range from -128 to 127. However, it does seem like they are real z coordinates
				(i.e. 15 really is 15, not -113 or anything like that), because if you put in a value below 0
				in a Z field in the God Client, the God Client will change it to *your current Z coordinate*.
				I have no idea why OSI would make the God Client unable to make areas below 0 z, or what an
				area above 127 is supposed to represent. 
				
				But since 0-255 is the default, it should probably be taken to mean 'all possible z values'.
				
			Parameters:
				upperLeftX - The upper-left (starting) x coordinate. (Not checked for validity)
				upperLeftY - The upper-left (starting) y coordinate. (Not checked for validity)
				width - The (X) width of the area, in tiles. (Not checked for validity)
				height - The (Y) height of the area, in tiles. (Not checked for validity)
				lowZ - The low Z point of the area, from 0-255, with 0 being the default. This is not a mistake.
				highZ - The high Z point of the area, from 0-255, with 255 being the default. This is not a mistake.
				name - The name of the area (God Client limits this to 39 characters)
				description - A description of the area  (God Client limits this to 39 characters)
				sfx - The sound FX # to play in this area.
				music - The music # to play in this area.
				nightsfx - The sound FX # to play in this area at night-time.
				dungeon - The dungeon # this area belongs to. Generally 0. The list in the God Client is apparently
					hardcoded to the list of dungeons on the OSI map. This can be ignored.
				light - The light level of the area. Presumably this should be ignored if it were 0, so this is only
					used for areas with fixed light levels, like dungeons.
		*/
		internal static void MakeNewArea(ushort upperLeftX, ushort upperLeftY, ushort width,
								ushort height, ushort lowZ, ushort highZ, string name, string description, ushort sfx,
								ushort music, ushort nightsfx, byte dungeon, ushort light) {
			Logger.WriteWarning("Unimplemented: MakeNewArea called. Arguments: UL("+upperLeftX+","+upperLeftY+") width/height("+
							width+","+height+") Z("+lowZ+","+highZ+") sfx("+sfx+") music("+music+") nightsfx("+
							nightsfx+") dungeon("+dungeon+") light("+light+") name("+name+") description("+description+
							")");
			/*Doesn't work: The client apparently wants a different packet than 0x58 to tell it about new areas...
			if (Globals.srcConn is GameConn) {
				PacketSender.PrepareNewArea(upperLeftX, upperLeftY, width, height, lowZ, highZ, name, description, sfx, music, nightsfx, dungeon, light);
				PacketSender.SendTo(Globals.srcConn as GameConn, true);
			}*/
		}
		
		/*
			Method: AttackRequest
				This is called when a client requests to attack another character.
				LOS checking is NOT done before this function is called. That can be done by
				whoever implements this method (Presumably as attacker.AttackRequest(target)).
		*/
		internal static void AttackRequest(AbstractCharacter attacker, AbstractCharacter target) {
			//precondition checks
			Sanity.IfTrueThrow(attacker==null,"Attacker is null in AttackRequest.");
			Sanity.IfTrueThrow(target==null,"Target is null in AttackRequest.");
			Sanity.IfTrueThrow(attacker.IsDeleted,"Attacker does not exist (IsDeleted is true).");
			Sanity.IfTrueThrow(target.IsDeleted,"Attacker does not exist (IsDeleted is true).");
			
			Logger.WriteWarning("Unimplemented: "+attacker+" requests to attack "+target);
			//Check if the character and the target are both alive later on. Note that scripters
			//may want to give ghosts (possibly only some ghosts, even) the ability to "attack"
			//players for some reason, so probably @attack triggers should be called, and allow/deny
			//done based on return values:
			//return 0 (allow request) - allow attack regardless of alive/dead state of characters.
			//return 1 (cancel request) - allow attack regardless of alive/dead state of characters.
			//return 2 (default value) - allow attack if both characters are alive, disallow if either is dead
			
		}
		
		/*
			Method: UseSkillNumberRequest
				This is called when a client requests to use a particular skill.
				
			Parameters:
				cre - The character making the request.
				skillNumber - The ID# of the skill, with 1 being anatomy, etc.
					You need to check for invalid skillNumber values. The code in InPackets
					doesn't know how many skills there are, and so does not validate the skill number.
					
			edit: AbstractCharacter.SelectSkill(int skillId) is now called instead of this
		*/
		//internal static void UseSkillNumberRequest(AbstractCharacter cre, int skillNumber) {
		//	Sanity.IfTrueThrow(cre==null,"Character is null in UseSkillNumberRequest.");
		//	if (cre.IsAlive) {
		//		Logger.WriteWarning("Unimplemented: UseSkillNumberRequest called on "+cre+" for skill "+skillNumber);
		//	} else {
		//		Logger.WriteDebug("Ignoring UseSkillNumberRequest from dead character "+cre+".");
		//	}
		//}
		
		/*
			Method: UseLastSkillRequest
				This is called when a client requests to use their last used skill again.
				
			Parameters:
				cre - The character making the request.   
				
			edit: AbstractCharacter.SelectSkill(int skillId) is now called instead of this
				
		*/
		//internal static void UseLastSkillRequest(AbstractCharacter cre) {
		//	Sanity.IfTrueThrow(cre==null,"Character is null in UseLastSkillRequest.");
		//	if (cre.IsAlive) {
		//		Logger.WriteWarning("Unimplemented: UseLastSkillRequest called on "+cre+".");
		//	} else {
		//		Logger.WriteDebug("Ignoring UseLastSkillRequest from dead character "+cre+".");
		//	}
		//}
		
		/*
			Method: UseSpellNumberRequest
				This is called when a client requests to cast a particular spell.
				You need to check for invalid skillNumber values. The code in InPackets
				doesn't know how many skills there are, and so does not validate the skill number.
			Parameters:
				cre - The character making the request.
				spellNumber - The ID# of the spell, with 1 being clumsy, etc.
					You need to check for invalid spellNumber values. The code in InPackets
					doesn't know how many spells there are, and so does not validate the spell number.
		*/
		internal static void UseSpellNumberRequest(AbstractCharacter cre, int spellNumber) {
			Sanity.IfTrueThrow(cre==null,"Character is null in UseSpellNumberRequest.");
			Logger.WriteWarning("Unimplemented: UseSpellNumberRequest called on "+cre+" for spell "+spellNumber);
		}
		
		/*
			Method: UseLastSpellRequest
				This is called when a client requests to use their last used spell again.
				
			Parameters:
				cre - The character making the request.
		*/
		internal static void UseLastSpellRequest(AbstractCharacter cre) {
			Sanity.IfTrueThrow(cre==null,"Character is null in UseLastSpellRequest.");
			Logger.WriteWarning("Unimplemented: UseLastSpellRequest called on "+cre+".");
		}
		
		/*
			Method: UseLastSpellRequest
				This is called when a client uses the macro command 'OpenDoor.'
				Presumably, they want to open any nearby door, though the client doesn't
				check for any. This does not check to make sure they are alive.
				
			Parameters:
				cre - The character making the request.
		*/
		internal static void OpenDoorMacroRequest(AbstractCharacter cre) {
			Sanity.IfTrueThrow(cre==null,"Character is null in UseLastSpellRequest.");
			Logger.WriteWarning("Unimplemented: OpenDoorMacroRequest called on "+cre+".");
		}
		
		/*
			Method: AnimRequest
				This is called when the client requests to bow, salute, etc. The client sends
				a whole string, but this works on the assumption that the only valid anim names are
				"bow" and "salute", so it only checks the first letter. (UO3D (if it isn't blocked!)
				sends its anims in a different packet) (The debug build looks at the string
				and treats anything other than "bow" or "salute" as a suspicious error - which is likely to
				get someone's attention if they do it and get kicked, which should get them to report
				it as a bug, hopefully, which would be what it is (assuming they hadn't hacked the client
				to send "flurble" or something))
			
			Parameters:
				cre - The character making the request.
				anim - The anim requested, that is RequestableAnim.Bow or RequestableAnim.Salute. 
		*/
		internal static void AnimRequest(AbstractCharacter cre, RequestableAnim anim) {
			Sanity.IfTrueThrow(cre==null,"Character is null in AnimRequest.");
			Logger.WriteWarning("Unimplemented: AnimRequest called on "+cre+". Requested '"+anim+"'.");
		}
				
		/*
			Method: GodClientCommandRequest
				This is called when we recieve a command from a God Client. This is called whether it
				is in god mode or not, but if it isn't, then any message above 32 characters will be
				truncated (before it is displayed).
				
				We don't label sending god-commands without god-mode a suspicious error because the GC
				does like to send one before entering god-mode - We do ignore them though.
			
			Parameters:
				cre - The character making the request.
				anim - A string containing the name of the anim, such as "bow" or "salute".
		*/
		internal static void GodClientCommandRequest(GameConn conn, string cmd) {
			Sanity.IfTrueThrow(conn==null,"Source connection is null in GodClientCommandRequest.");
			Sanity.IfTrueThrow(cmd==null,"Command is null in GodClientCommandRequest.");
			if (conn.GodMode) {
				Logger.WriteWarning("Unimplemented: GodClientCommandRequest from "+conn+". It commanded '"+cmd+"'");
			} else {
				Logger.WriteWarning("Disregarding God Client command from a client which isn't in god mode ("+conn+"). It commanded '"+cmd+"'.");
			}
		}
		
		/*
			Method: UsePrimaryAbilityRequest
				This is called when a player uses the "PrimaryAbility" macro (or presumably
				double-clicks the primary ability button, though I haven't shown one yet so
				I can't try it yet).
			
			Parameters:
				cre - The character making the request.
		*/
		internal static void UsePrimaryAbilityRequest(AbstractCharacter cre) {
			Sanity.IfTrueThrow(cre==null,"Character is null in UsePrimaryAbilityRequest.");
			Logger.WriteWarning("Unimplemented: UsePrimaryAbilityRequset for "+cre+".");
		}
		
		/*
			Method: UseSecondaryAbilityRequest
				This is called when a player uses the "SecondaryAbility" macro (or presumably
				double-clicks the secondary ability button, though I haven't shown one yet so
				I can't try it yet).
			
			Parameters:
				cre - The character making the request.
		*/
		internal static void UseSecondaryAbilityRequest(AbstractCharacter cre) {
			Sanity.IfTrueThrow(cre==null,"Character is null in UseSecondaryAbilityRequest.");
			Logger.WriteWarning("Unimplemented: UseSecondaryAbilityRequest for "+cre+".");
		}
	}
	
	
	/*
(SL)	See docs/packets.txt for information on the methods you can call in PacketSender to send
		or prepare to send various things. Comments do not yet exist for most of those methods, however.
		
		Additionally, look at methods on Thing, Character, Item, etc, such as Sound, Yell, SendUpdateStats,
		SendUpdateHitpoints, etc.
		
		
		Other methods which would be useful for something or other:
			AbstractCharacter's MakeBePlayer and MakeBeNonPlayer would be useful for the 'control' (possess) command,
				and anything else which needs that sort of effect.
	*/
	
}
