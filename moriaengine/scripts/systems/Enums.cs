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
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	[Remark("Numbers of important hues")]
	public enum Hues : int {
		Red=33,
		Blue=99,
		Green=68,
		Info=642, //shit color :) (dark yellow-green-brown undefinable) :-/ its hard to choose
		//text colors
		PageRepliedColor=1740, //some other orange shit (used for labeling replied pages)
		WriteColor=2300,
		PlayerColor=2301,//color for players name in Admin dialog (until the coloring players is solved)
		WriteColor2=0481,//tmave tyrkysova
		ReadColor=2303,
		NAColor=2305,

		OnlineColor=2301,//color for highlighting online accounts/players
		OfflineColor = 0481,//color for highlighting offline accounts/players

		LabelColor = 1675, //nadpisy (sloupecku atd) - svetlounce bezova
		HeadlineColor = 2000,//1152, //titulek (obvykle celeho dialogu atd)

		SettingsTitleColor=2413, //the settings category color
		SettingsNormalColor=2300, //normal settings items
		SettingsCorrectColor = 95,//setting items that were correctly set
		SettingsFailedColor = 35 //setting items that weren't possible to be set
	}

	[Remark("Various sorting criteria used in different dialogs")]
	public enum SortingCriteria : int {
		NameAsc,
		NameDesc,
		AccountAsc,
		AccountDesc,
		LocationAsc,
		LocationDesc,
		TimeAsc,
		TimeDesc,
		IPAsc,
		IPDesc,
		UnreadAsc,
		UnreadDesc
	}

	[Remark("Various types of GUTA Leaf Components")]
	public enum LeafComponentTypes : int {
		//Buttons
		[Remark("Button with the big X inside")]
		ButtonCross,
		[Remark("Button with the OK inside")]
		ButtonOK,
		[Remark("Button with the tick inside")]
		ButtonTick,
		[Remark("Button with the sheet of paper inside")]
		ButtonPaper,
		[Remark("Button with flying paper")]
		ButtonSend,
		[Remark("Button for sorting (small up arrow)")]
		ButtonSortUp,
		[Remark("Button for sorting (small down arrow)")]
		ButtonSortDown,
		[Remark("Medium UP arrow")]
		ButtonPrev,
		[Remark("Medium DOWN arrow")]
		ButtonNext,
		[Remark("Button with people")]
		ButtonPeople,
		CheckBox,
		RadioButton,
		//Inputs
		InputText,
		InputNumber
	}

	public enum SettingsDisplay {
		[Remark("Zobrazit vsechny kategorie v jednom dialogu")]
		All,
		[Remark("Zobrazit jen jednu vybranou kategorii")]
		Single
	}

	[Remark("Result of settign a single field in the info or settings dialogs")]
	public enum SettingsOutcome {
		NotChanged,//field has not changed at all
		ChangedOK, //field has changed successfully
		ChangedError //field has not changed due to some fault
	}

	[Flags]
	[Summary("Urcuje, jakej rezist se ma aplikovat na dotycny damage.")]
	public enum DamageType : int {
		[Summary("Damage neredukovano")]
		Irresistable=0x000000,
		[Summary("Damage redukovano magickym rezistem (neplest s obranou mysli), i kdyz mozna nic takovyho neexistuje ;)")]
		Magic=0x000001,
		MagicFire=Magic|0x000004,
		Electric=Magic|0x000008,
		Acid=Magic|0x000010,
		Cold=Magic|0x000020,
		MagicPoison=Magic|0x000040,
		[Summary("Mystikuv utok")]
		Mystical=Magic|0x000080,
		[Summary("Damage redukovano fyzickym rezistem (neplest s armorem)")]
		Physical=0x000002,
		[Summary("Secne zbrane (mece, sekery) ")]
		Slashing=Physical|0x000100,
		[Summary("Bodne zbrane (mece, dyky, vidle) (drive piercing, prejmenovano aby se to nepletlo s prubojnosti)")]
		Stabbing=Physical|0x000200,
		[Summary("Secne bodne zbrane (mece)")]
		Sharp=Physical|Slashing|Stabbing,
		[Summary("Tupe zbrane (hole, palcaty)")]
		Blunt=Physical|0x000400,
		[Summary("Palne zbrane (luky, kuse)")]
		Archery=Physical|0x000800,
		Bleed=Physical|0x001000,

		Summon=0x002000,
		Dragon=0x004000,
		NonMagicFire=0x000004,
		NonMagicPoison=0x000040
	}


	//Jednorucni (Broad Sword, Cutlass, Katana, Kryss, Long Sword, Scimitar, Short Spear, Spear, Viking Sword, War Fork) - Fencing
	//Obourucni (Dagger, Axe, Bardiche, Battle Axe, Battle Large Axe, Executioner"s Axe, Double Axe, Halberd, Spear, Two Handed Axe, War Axe)- Swordsmanship
	//Tupe (Mace, War Mace, Hammer Pick, War Hammer, Maul)- Macefighting
	//Strelne (Bow, Crossbow, Heavy crossbow)- Archery
	//Hole (Gnarled staff, Quarter staff, Black staff) - Magery
	public enum WeaponType : byte {
		BareHands,//prazdne ruce
		OneHandBlunt,//jednorucni tupe - mace
		TwoHandBlunt,//dvourucni tupe - kladiva, hole
		OneHandSpike,//jednorucni bodne - noze
		TwoHandSpike,//dvourucni bodne - vidle, ostepy
		OneHandSword,//jednorucni bodne/secne - mece
		TwoHandSword,//dvourucni bodne/secne - obourucni mece
		OneHandAxe,//jednorucni secne - sekery
		TwoHandAxe,//dvourucni secne - sekery
		BowStand,
		BowRunning,
		XBowStand,
		XBowRunning,
		Undefined//NPC s undefined weapontypou na defu budou brat weapontype ze sve skutecne zbrane
	}

	public enum WeaponAnimType : byte {
		BareHands,//prazdne ruce
		HeldInLeftHand,
		HeldInRightHand,
		Bow,
		XBow,
		Undefined//NPC s undefined weapontypou na defu budou brat weapontype ze sve skutecne zbrane
	}

	public enum GenericAnim : byte {
		Walk=0,
		Run=1,
		StandStill=2,
		RandomIdleAction=3,
		IdleAction=4,
		LookAround=5,
		AttackSwing=6,
		AttackStab=7,
		AttackOverhead=8,
		AttackShoot=9,
		GetHit=10,
		FallBackwards=11,
		FallForwards=12,
		Block=13, Dodge=13,
		AttackBareHands=14,
		Bow=15,
		Salute=16,
		Drink=17, Eat=17
	}

	public enum HumanAnim : byte {
		WalkUnarmed=0,
		WalkArmed=1,
		RunUnarmed=2,
		RunArmed=3,
		StandStill=4,
		LookAround=5,
		LookDown=6,
		WarMode=7,
		WarModeWithTwoHandedWeapon=8,
		RightHandSwing=9, OneHandSwing = 9,
		RightHandStab=10, OneHandStab=10,
		RightHandOverhead=11, OneHandOverhead=11,
		LeftHandOverhead=12, TwoHandOverhead=12,
		LeftHandSwing=13, TwoHandSwing=13,
		LeftHandStab=14, TwoHandStab=14,
		WalkWarMode=15,
		CastForward=16,
		Cast=17,
		FireBow=18,
		FireCrossbow=19,
		GetHit=20,
		FallBackwards=21, Die1=21,
		FallForwards=22, Die2=22,
		MountedWalk=23,
		MountedRun=24,
		MountedStandStill=25,
		MountedRightHandAttack=26,
		MountedFireBow=27,
		MountedFireCrossbow=28,
		MountedLeftHandAttack=29,
		Block=30, Dodge=30,
		AttackBareHands=31,
		Bow=32,
		Salute=33,
		Drink=34, Eat=34,
		MountedSalute=47,
		MountedBlock=48,
		MountedGetHit=49,	//?
		NumAnims=50
	}
	public enum MonsterAnim : byte {
		Walk=0,
		StandStill=1,
		FallBackwards=2, Die1=2,
		FallForwards=3, Die2=3,
		Attack=4, Attack1=4,
		Attack2=5,
		Attack3=6,
		Attack4=7,	//bow?
		Attack5=8,	//xbow?
		Attack6=9,	//throw?
		GetHit=10,
		Cast=11,
		Summoned=12, Cast2=12, UseBreathWeapon=13, //For dragons, breath fire
		Cast3=13, CastForward=13,
		Cast4=14,
		BlockRight=15,
		BlockLeft=16,
		IdleAction=17,
		LookAround=18,
		Fly=19,
		TakeOff=20,
		GetHitWhileFlying=21,
		NumAnims=21
	}

	public enum AnimalAnim : byte {
		Walk=0,
		Run=1,
		StandStill=2,
		Eat=3,
		Unknown=4,
		Attack=5, Attack1=5,
		Attack2=6,
		GetHit=7,
		Die=8,
		IdleAction=9, IdleAction1=9,
		IdleAction2=10,
		LieDown=11, Sleep=11,
		Die2=12,	//This looks identical to 5 (attack) on all the animals I've tested so far...
		NumAnims=13
	}

	public enum MaterialType : byte {
		None=0,
		Metal=1,
		Ore=2,
		Wood=3
	}

	public enum Material : byte {
		None=0,
		Copper=1, Spruce=1,
		Iron=2, Chestnut=2,
		Silver=3,
		Gold=4,
		Verite=5, Oak=5,
		Valorite=6, Teak=6,
		Obsidian=7, Mahagon=7,
		Adamantinum=8, Eben=8,
		Mithril=9, Elven=9,
		Sand = 10

	}
}