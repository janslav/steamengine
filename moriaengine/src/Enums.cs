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

namespace SteamEngine {
	//Microsoft says to make enums names' singular, but to make bit-flag enums names' plural. So I've changed all
	//these plural ones to singular (I had added half of them myself anyways). -SL
	
	
	//This is actually used now, for Character's direction, and you pass this instead of a byte
	//to WalkRunOrFly and to the methods in OutPackets which take a direction as a parameter.
	//-SL
	public enum Direction : byte {
		North = 0,
		NorthEast = 1,
		East = 2,
		SouthEast = 3,
		South = 4,
		SouthWest = 5,
		West = 6,
		NorthWest = 7,
	}
	
	public enum StatLockType : byte {
		Up=0, Increase=0, Unlocked=0,
		Down=1, Decrease=1, LockedDown=1,
		Locked=2
	}
	
	public enum SkillLockType : byte {
		Up=0, Increase=0, Unlocked=0,
		Down=1, Decrease=1, LockedDown=1,
		Locked=2
	}
	
	//One of the possible values that can be sent to the client when it requests to delete a character.
	//AcceptedRequest and RejectWithoutSendingAMessage are special codes which aren't sent to the client,
	//the others are all reasons for rejection which are sent. The name, after "Reject_", is exactly what
	//the client prints, but with spaces. (We send a number, but the client knows what it means)
	//-SL
	public enum DeleteRequestReturnValue : byte {
		Reject_InvalidCharacterPassword=0,
		Reject_NonexistantCharacter=1,
		Reject_CharacterIsBeingPlayedRightNow=2,
		Reject_CharacterIsNotOldEnoughToDelete=3,
		Reject_CharacterIsCurrentlyQueuedForBackup=4,
		Reject_CouldntCarryOutRequest=5,
		RejectWithoutSendingAMessage=254,
		AcceptedRequest=255
	}
	
	public enum FailedLoginReason : byte {
		NoAccount=0,
		SomeoneIsAlreadyUsingThisAccount=1,
		Blocked=2,
		InvalidAccountCredentials=3,
		CommunicationsProblem=4,
		
		Count=5	//The number of elements in this enum.
	}	
	//Used by AnimRequest (search on it, it was in Temporary at the time this comment was written, but might have
	//been implemented since then) - Actually, just search on RequestableAnims.
	//-SL
	public enum RequestableAnim {
		Bow,
		Salute
	}
	
	//Used by the enumerations stuff in UberSectorEnumerator.cs and Sector.cs.
	//-SL
	internal enum SectorEnumType {
		Players,
		Things,
		Disconnects,
		Statics,
		NumSectorEnumTypes		//Used as an array-size constant.
	}
	
	//Used by the core speech code.
	//-SL
	public enum SpeechType {
		Speech=0,
		//If you send something of speechtype 1, it simply isn't displayed. Hmm. (Wild guess, GM only? Or maybe it really isn't displayed for anyone, heh.)
		Emote = 2,
		Server = 3,
		Name = 6,
		OnlyOne = 7,	//only one of these will be shown at a time. If you send another, the last one dissapears. It stays in the journal though.
		Whisper = 8,
		Yell = 9,
		Spell = 10,
		Encoded = 0xc0,
		Guild = 13,
		Alliance = 14,
		Command = 15,
	}
	
	//Used by the core status-bar sending code.
	//-SL
	public enum StatusBarType {
		Me,
		Pet,
		Other,
		NumStatusBarTypes		//Used as an array-size constant.
	}
	
	//Used by the Has/Exists methods in TagHolder. e.g. Has(rider), Has(owner), etc.
	//-SL
	public enum DoesItHaveIt {
		No=0,				//No, it is null.
		Yes=1,				//Yes, it has it, it isn't null.
		Maybe=2,			//It might have it... There's more than one member with the same name, so you need to capitalize exactly.
		No_Such_Name=3		//There are no properties matching that name!
	}
	
	[Flags]
	public enum MovementType {
		Walking=1,
		Running=2,
		RequestedStep=4,
		Teleporting=8,
		
		//Flying=3,
		//Appearing=4,
		//Disappearing=5
		//TODO?: More for dragging items, etc, so we can show the proper dragging anim?
	}

	
	public enum PriorityClass {
		Pet=0,		//A pet
		NPC=1,		//An NPC
		NPCHero=2,	//A hero NPC
		Player=3	//A player.
	}
	
	public enum CompressedPacketType {
		Single,
		Group
	}
	
	public enum GroupState {
		Open,			//- a group has been made w/ NewGroup and not yet closed.
		Ready,			//- Ready to do whatever.
		SingleBlocking	//- A single compressed packet is the latest thing, so nothing else can be done until it's discarded.
	}
	
	public enum GeneratingState {
		Generating,		//- Generating a packet. Nothing else can be done until it is done.
		Generated,		//- A packet has been generated, and is waiting to be compressed.
		Ready,			//- We are ready to generate a new packet (Or do other things).
	}
	
	public enum PickupResult : byte {
		Failed_YouCannotPickThatUp=0,
		Failed_ThatIsTooFarAway=1,
		Failed_ThatIsOutOfSight=2,
		Failed_ThatDoesNotBelongToYou=3,	//you will have to steal it
		Failed_YouAreAlreadyHoldingAnItem=4,	//unused?
		Failed_RemoveFromView=5,	//remove from view
		Failed_NoMessage=6,
		Succeeded=7,
		FailedCount=7	//The number of Failed_ elements in this enum.
	}
	
	public enum Season : byte {
		Spring = 0,
		Summer = 1,
		Fall = 2, Autumn = 2,
		Winter = 3,
		Dead = 4, Desolation = 4
	}
	
	public enum CursorType : byte {
		Normal = 0,
		Gold = 1
	}

	internal enum MoveRestriction {
		Normal = 0,
		Movable = 1
		//Immovable = 2	//Does not work
	}
	
	public enum MapTileType : byte {
		Water = 0,
		Rock = 1,
		Grass = 2,
		Lava = 3,
		Dirt = 4,
		Other = 5
	}
	
	public enum GenericAnim : byte {
		Walk = 0,
		Run = 1,
		StandStill = 2,
		RandomIdleAction = 3,
		IdleAction = 4,
		LookAround = 5,
		AttackSwing = 6,
		AttackStab = 7,
		AttackOverhead = 8,
		AttackShoot = 9,
		GetHit = 10,
		FallBackwards = 11,
		FallForwards = 12,
		Block = 13, Dodge = 13,
		AttackPunch = 14,
		Bow = 15,
		Salute = 16,
		Drink = 17, Eat = 17
	}
	
	public enum HumanAnim : byte {
		WalkUnarmed = 0,
		WalkArmed = 1,
		RunUnarmed = 2,
		RunArmed = 3,
		StandStill = 4,
		LookAround = 5,
		LookDown = 6,
		WarMode = 7,
		WarModeWithTwoHandedWeapon = 8,
		AttackSwing = 9,
		AttackStab = 10,
		AttackOverhead = 11,
		AttackTwoHandedOverhead = 12,
		AttackTwoHandedSwing = 13,
		AttackTwoHandedStab = 14,
		WalkWarMode = 15,
		CastForward = 16,
		Cast = 17,
		FireBow = 18,
		FireCrossbow = 19,
		GetHit = 20,
		FallBackwards = 21, Die1 = 21,
		FallForwards = 22, Die2 = 22,
		MountedWalk = 23,
		MountedRun = 24,
		MountedStandStill = 25,
		MountedAttackOverhead = 26,
		MountedFireBow = 27,
		MountedFireCrossbow = 28,
		MountedAttackTwohandedOverhead = 29,
		Block = 30, Dodge = 30,
		AttackPunch = 31,
		Bow = 32,
		Salute = 33,
		Drink = 34, Eat = 34,
		MountedSalute = 47,
		MountedBlock = 48,
		MountedGetHit = 49,	//?
		NumAnims = 50
	}
	public enum MonsterAnim : byte {
		Walk = 0,
		StandStill = 1,
		FallBackwards = 2, Die1 = 2,
		FallForwards = 3, Die2 = 3,
		Attack = 4, Attack1 = 4,
		Attack2 = 5,
		Attack3 = 6,
		Attack4 = 7,	//bow?
		Attack5 = 8,	//xbow?
		Attack6 = 9,	//throw?
		GetHit = 10,
		Cast = 11,
		Summoned = 12,	Cast2 = 12,	UseBreathWeapon = 13, //For dragons, breath fire
		Cast3 = 13, CastForward = 13,
		Cast4 = 14,
		BlockRight = 15,
		BlockLeft = 16,
		IdleAction = 17,
		LookAround = 18,
		Fly = 19,
		TakeOff = 20,
		GetHitWhileFlying = 21,
		NumAnims = 21
	}
	
	public enum AnimalAnim : byte {
		Walk = 0,
		Run = 1,
		StandStill = 2,
		Eat = 3,
		Unknown = 4,
		Attack = 5, Attack1 = 5,
		Attack2 = 6,
		GetHit = 7,
		Die = 8,
		IdleAction = 9, IdleAction1 = 9,
		IdleAction2 = 10,
		LieDown = 11, Sleep = 11,
		Die2 = 12,	//This looks identical to 5 (attack) on all the animals I've tested so far...
		NumAnims = 13
	}
		
	public enum HighlightColor : byte {
		NoColor = 0,		//This draws them as color 0 (the default color). You can't change this color in the client.
		
		//Name = number		//default color						The description in the client (Options->Reputation System (3rd tab down on the right))
		Innocent = 1,		//Blue by default.					("Innocent highlight color")
		Allied = 2,			//Green by default.					("Friendly guilds highlight color")
		Attackable = 3,		//Grey by default.					("Someone that can be attacked color")
		Criminal = 4,		//Also grey by default.				("Criminal highlight color")
		Enemy = 5,			//Orangeish-brown by default.		("Enemy guildmembers highlight color")
		Murderer = 6,		//Red by default.					("Murderer highlight color")
		
		Invulnerable = 7,	Yellow = 7, //You can't change this color in the client.
		Transparent = 8,	//Makes their status bar look really bizarre, and makes them turn transparent-black 
							//when highlighted.
		NumHighlightColors = 9
	}
	
	public enum ClientType {
		Iris,
		OSI2D,
		OSI3D,
		OSIGod,
		PlayUO,
		Palanthir,
		Unknown
	}
	
	public enum ContOrPoint {
		Cont, Point
	}
	
}
