using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	public static class AnimCalculator {

		//For the AnimDuration method, which returns the approximate number of seconds an anim will take.
		private static float[,] animDuration;
		private static uint[] animsAvailableTranslate;

		static AnimCalculator() {
			animDuration = new float[256,256];
			for (int numFrames=0; numFrames<256; numFrames++) {
				for (int frameDelay=0; frameDelay<256; frameDelay++) {
					animDuration[numFrames, frameDelay] = SlowAnimDuration((ushort)numFrames, (byte)frameDelay);
				}
			}
			animsAvailableTranslate=new uint[32];
			
			//Fill animsAvailableTranslate with the power-of-two for each bit of animsAvailable.
			uint code=1;
			for (int anim=0; anim<32; anim++) {
				animsAvailableTranslate[anim]=code;
				code+=code;
			}
		}
		
		/**
			Returns an estimate of how long UO should take to finish the anim.
			The formula used is (.25*numFrames)+(.1*numFrames*frameDelay), which I got by timing how long it took to do
			anim 14 (which has 7 frames) with a variety of frameDelay values. (I figured out what the base delay
			per frame was first, and then figured out how much frameDelay affected it - I rounded, by the way, to nice
			even values, because if I were programming the UO client I would have used nice even values.
			
			But the people who programmed the UO client didn't always do what I would have done, hehe. In fact,
			they often seem to have done things that seem totally bizarre to me - like having numBackwardsFrames
			and numAnims (my names, since of course I don't have a clue what they called them!) work the way they do....
			)
			
			Example for anim 14:
				Anim(14);
				conn.WriteLine("That should take "+AnimDuration(7, 0)+" seconds to draw.");
			If someone makes a script using @timer to print the message when time is up, we can see if my estimates
			are accurate. I'll probably wind up doing that later, since the whole purpose of my testing the anim packet
			and making the AnimDuration method was that I would need to be able to predict when an anim would end,
			for future scripts - the use I was pondering was making the swing speed of weapons actually be synced with
			the anim speed of the attacking anims when using that anim, and it looks like I should be able to do that :).
			
			(
				This method, by the way, uses a two-dimensional lookup-table, which is why numFrames is a byte -
				we need 256 KB of RAM for the 256x256x4b table (4bytes for each element is a float), which isn't much,
				but if numFrames were a ushort and all possible values were legal, then it would need 65536*256*4b =
				64 MB of RAM, and that would not be good. :P
				
				I figure 256 KB is an acceptable tradeoff for the speed gain from not needing the multiplications
				(unless .NET does evil things with two-dimensional tables - I know they're slower than one-dimensional
				ones, but even if it's 4x slower than a one-dimensional table, it's still 15x or so (estimating!)
				faster than doing the three multiplications instead, and also still faster than it would be to have two
				one-dimensional tables (animBaseTime[numFrames] and animAdditionalTimePerFrame[frameDelay]),
				and to multiply the additional-time-per-frame by numFrames, (then adding the two together, which takes
				very little time compared to multiplication).
			)
				
			@numFrames This is a byte, although it is possible to have more than 255 frames. If you do, you could
				call SlowAnimDuration(ushort numFrames, byte frameDelay) instead, which is slower than this method.
				Or, if your number of frames is fixed, or if frameDelay is, then you could precalculate the results
				or write a faster calculation than what SlowAnimDuration does (based on
				(.25*numFrames)+(.1*numFrames*frameDelay), just precalculate whatever you can, like .25*numFrames if you
				know numFrames, or .1*frameDelay if you know frameDelay, etc). 
				
				This is the number of frames UO would be drawing. So if you're drawing it both forward and backward
				(using 'undo'), double the # of frames. If you're drawing it backwards and limiting the # of frames,
				well, it's up to you to feed this method the actual # of frames that will be drawn.
			@frameDelay The 'frameDelay' value you passed to the Anim method. If the anim method you used didn't take
				a frameDelay parameter, it defaulted to 0.
		*/
		public static float AnimDuration(byte numFrames, byte frameDelay) {
			return animDuration[numFrames,frameDelay];
		}
		
		private static float SlowAnimDuration(ushort numFrames, byte frameDelay) {
			return (.25f*numFrames)+(.1f*numFrames*frameDelay);
		}

		public static bool CanPerformAnim(Character self, AnimalAnim anim) {
			return CanPerformAnim(anim, self.AnimsAvailable);
		}
		public static bool CanPerformAnim(AnimalAnim anim, uint animsAvailable) {
			return (animsAvailable&(animsAvailableTranslate[(int) anim]))>0;
		}
		public static bool CanPerformAnim(Character self, MonsterAnim anim) {
			return CanPerformAnim(anim, self.AnimsAvailable);
		}
		public static bool CanPerformAnim(MonsterAnim anim, uint animsAvailable) {
			return (animsAvailable&(animsAvailableTranslate[(int) anim]))>0;
		}

		public static AnimalAnim GetAnimalAnim(GenericAnim anim, uint animsAvailable) {
			AnimalAnim realAnim = AnimalAnim.Walk;
			switch (anim) {
				case GenericAnim.Walk: {
						realAnim=AnimalAnim.Walk;
						break;
					}
				case GenericAnim.Run: {
						if (CanPerformAnim(AnimalAnim.Run, animsAvailable)) {
							realAnim=AnimalAnim.Run;
						} else {
							realAnim=AnimalAnim.Walk;
						}
						break;
					}
				case GenericAnim.StandStill: {
						if (CanPerformAnim(AnimalAnim.StandStill, animsAvailable)) {
							realAnim=AnimalAnim.StandStill;
						} else {
							realAnim=AnimalAnim.Walk;
						}
						break;
					}
				case GenericAnim.RandomIdleAction: {
						double dbl = Globals.dice.NextDouble();
						if (dbl<.5 && CanPerformAnim(AnimalAnim.IdleAction, animsAvailable)) {
							realAnim=AnimalAnim.IdleAction;
						} else if (dbl<1 && CanPerformAnim(AnimalAnim.IdleAction2, animsAvailable)) {
							realAnim=AnimalAnim.IdleAction2;
						} else {
							realAnim=AnimalAnim.StandStill;
						}
						break;
					}
				case GenericAnim.IdleAction: {
						if (CanPerformAnim(AnimalAnim.IdleAction, animsAvailable)) {
							realAnim=AnimalAnim.IdleAction;
						} else if (CanPerformAnim(AnimalAnim.IdleAction2, animsAvailable)) {
							realAnim=AnimalAnim.IdleAction2;
						} else {
							realAnim=AnimalAnim.StandStill;
						}
						break;
					}
				case GenericAnim.LookAround: {
						if (CanPerformAnim(AnimalAnim.IdleAction2, animsAvailable)) {
							realAnim=AnimalAnim.IdleAction2;
						} else if (CanPerformAnim(AnimalAnim.IdleAction, animsAvailable)) {
							realAnim=AnimalAnim.IdleAction;
						} else {
							realAnim=AnimalAnim.StandStill;
						}
						break;
					}
				case GenericAnim.AttackSwing: {
						if (CanPerformAnim(AnimalAnim.Attack, animsAvailable)) {
							realAnim=AnimalAnim.Attack;
						} else if (CanPerformAnim(AnimalAnim.Attack2, animsAvailable)) {
							realAnim=AnimalAnim.Attack2;
						} else {
							realAnim=AnimalAnim.StandStill;
						}
						break;
					}
				case GenericAnim.AttackStab: {
						if (CanPerformAnim(AnimalAnim.Attack2, animsAvailable)) {
							realAnim=AnimalAnim.Attack2;
						} else if (CanPerformAnim(AnimalAnim.Attack, animsAvailable)) {
							realAnim=AnimalAnim.Attack;
						}
						break;
					}
				case GenericAnim.AttackOverhead: {
						if (CanPerformAnim(AnimalAnim.Attack, animsAvailable)) {
							realAnim=AnimalAnim.Attack;
						} else if (CanPerformAnim(AnimalAnim.Attack2, animsAvailable)) {
							realAnim=AnimalAnim.Attack2;
						}
						break;
					}
				case GenericAnim.AttackShoot: {
						if (CanPerformAnim(AnimalAnim.Attack, animsAvailable)) {
							realAnim=AnimalAnim.Attack;
						} else if (CanPerformAnim(AnimalAnim.Attack2, animsAvailable)) {
							realAnim=AnimalAnim.Attack2;
						}
						break;
					}
				case GenericAnim.GetHit: {
						if (CanPerformAnim(AnimalAnim.GetHit, animsAvailable)) {
							realAnim=AnimalAnim.GetHit;
						} else {
							realAnim=AnimalAnim.StandStill;
						}
						break;
					}
				case GenericAnim.FallBackwards: {
						if (CanPerformAnim(AnimalAnim.Die, animsAvailable)) {
							realAnim=AnimalAnim.Die;
						} else if (CanPerformAnim(AnimalAnim.Die2, animsAvailable)) {
							realAnim=AnimalAnim.Die2;
						} else if (CanPerformAnim(AnimalAnim.LieDown, animsAvailable)) {
							realAnim=AnimalAnim.LieDown;
						} else {
							realAnim=AnimalAnim.StandStill;
						}
						break;
					}
				case GenericAnim.FallForwards: {
						if (CanPerformAnim(AnimalAnim.Die2, animsAvailable)) {
							realAnim=AnimalAnim.Die2;
						} else if (CanPerformAnim(AnimalAnim.Die, animsAvailable)) {
							realAnim=AnimalAnim.Die;
						} else if (CanPerformAnim(AnimalAnim.LieDown, animsAvailable)) {
							realAnim=AnimalAnim.LieDown;
						} else {
							realAnim=AnimalAnim.StandStill;
						}
						break;
					}
				case GenericAnim.Block: {		//also represents Dodge
						double dbl = Globals.dice.NextDouble();
						if (dbl < 0.5 && CanPerformAnim(AnimalAnim.Unknown, animsAvailable)) {
							realAnim=AnimalAnim.Unknown;
						} else {
							realAnim=AnimalAnim.StandStill;
						}
						break;
					}
				case GenericAnim.AttackBareHands: {
						if (CanPerformAnim(AnimalAnim.Attack, animsAvailable)) {
							realAnim=AnimalAnim.Attack;
						} else if (CanPerformAnim(AnimalAnim.Attack2, animsAvailable)) {
							realAnim=AnimalAnim.Attack2;
						}
						break;
					}
				case GenericAnim.Bow: {
						realAnim=AnimalAnim.Eat;
						break;
					}
				case GenericAnim.Salute: {
						realAnim=AnimalAnim.Eat;
						break;
					}
				case GenericAnim.Drink: {	//also represents Eat
						realAnim=AnimalAnim.Eat;
						break;
					}
				default: {
						throw new SanityCheckException("Unknown generic anim "+anim);
					}
			}
			return realAnim;
		}

		public static MonsterAnim GetMonsterAnim(GenericAnim anim, uint animsAvailable) {
			MonsterAnim realAnim = MonsterAnim.Walk;
			switch (anim) {
				case GenericAnim.Walk: {
						realAnim=MonsterAnim.Walk;
						break;
					}
				case GenericAnim.Run: {
						if (CanPerformAnim(MonsterAnim.Fly, animsAvailable)) {
							realAnim=MonsterAnim.Fly;
						} else {
							realAnim=MonsterAnim.Walk;
						}
						break;
					}
				case GenericAnim.StandStill: {
						if (CanPerformAnim(MonsterAnim.StandStill, animsAvailable)) {
							realAnim=MonsterAnim.StandStill;
						} else {
							realAnim=MonsterAnim.Walk;
						}
						break;
					}
				case GenericAnim.RandomIdleAction: {
						double dbl = Globals.dice.NextDouble();
						if (dbl<.5 && CanPerformAnim(MonsterAnim.IdleAction, animsAvailable)) {
							realAnim=MonsterAnim.IdleAction;
						} else if (dbl<1 && CanPerformAnim(MonsterAnim.LookAround, animsAvailable)) {
							realAnim=MonsterAnim.LookAround;
						} else {
							realAnim=MonsterAnim.StandStill;
						}
						break;
					}
				case GenericAnim.IdleAction: {
						if (CanPerformAnim(MonsterAnim.IdleAction, animsAvailable)) {
							realAnim=MonsterAnim.IdleAction;
						} else if (CanPerformAnim(MonsterAnim.LookAround, animsAvailable)) {
							realAnim=MonsterAnim.LookAround;
						} else {
							realAnim=MonsterAnim.StandStill;
						}
						break;
					}
				case GenericAnim.LookAround: {
						if (CanPerformAnim(MonsterAnim.LookAround, animsAvailable)) {
							realAnim=MonsterAnim.LookAround;
						} else if (CanPerformAnim(MonsterAnim.IdleAction, animsAvailable)) {
							realAnim=MonsterAnim.IdleAction;
						} else {
							realAnim=MonsterAnim.StandStill;
						}
						break;
					}
				case GenericAnim.AttackSwing: {
						if (CanPerformAnim(MonsterAnim.Attack, animsAvailable)) {
							realAnim=MonsterAnim.Attack;
						} else if (CanPerformAnim(MonsterAnim.Attack2, animsAvailable)) {
							realAnim=MonsterAnim.Attack2;
						} else if (CanPerformAnim(MonsterAnim.Attack3, animsAvailable)) {
							realAnim=MonsterAnim.Attack3;
						} else if (CanPerformAnim(MonsterAnim.Attack4, animsAvailable)) {
							realAnim=MonsterAnim.Attack4;
						} else if (CanPerformAnim(MonsterAnim.Attack5, animsAvailable)) {
							realAnim=MonsterAnim.Attack5;
						} else if (CanPerformAnim(MonsterAnim.Attack6, animsAvailable)) {
							realAnim=MonsterAnim.Attack6;
						} else {
							realAnim=MonsterAnim.StandStill;
						}
						break;
					}
				case GenericAnim.AttackStab: {
						if (CanPerformAnim(MonsterAnim.Attack2, animsAvailable)) {
							realAnim=MonsterAnim.Attack2;
						} else if (CanPerformAnim(MonsterAnim.Attack, animsAvailable)) {
							realAnim=MonsterAnim.Attack;
						} else if (CanPerformAnim(MonsterAnim.Attack3, animsAvailable)) {
							realAnim=MonsterAnim.Attack3;
						} else if (CanPerformAnim(MonsterAnim.Attack4, animsAvailable)) {
							realAnim=MonsterAnim.Attack4;
						} else if (CanPerformAnim(MonsterAnim.Attack5, animsAvailable)) {
							realAnim=MonsterAnim.Attack5;
						} else if (CanPerformAnim(MonsterAnim.Attack6, animsAvailable)) {
							realAnim=MonsterAnim.Attack6;
						} else {
							realAnim=MonsterAnim.StandStill;
						}
						break;
					}
				case GenericAnim.AttackOverhead: {
						if (CanPerformAnim(MonsterAnim.Attack3, animsAvailable)) {
							realAnim=MonsterAnim.Attack3;
						} else if (CanPerformAnim(MonsterAnim.Attack, animsAvailable)) {
							realAnim=MonsterAnim.Attack;
						} else if (CanPerformAnim(MonsterAnim.Attack2, animsAvailable)) {
							realAnim=MonsterAnim.Attack2;
						} else if (CanPerformAnim(MonsterAnim.Attack4, animsAvailable)) {
							realAnim=MonsterAnim.Attack4;
						} else if (CanPerformAnim(MonsterAnim.Attack5, animsAvailable)) {
							realAnim=MonsterAnim.Attack5;
						} else if (CanPerformAnim(MonsterAnim.Attack6, animsAvailable)) {
							realAnim=MonsterAnim.Attack6;
						} else {
							realAnim=MonsterAnim.StandStill;
						}
						break;
					}
				case GenericAnim.AttackShoot: {
						if (CanPerformAnim(MonsterAnim.Attack4, animsAvailable)) {
							realAnim=MonsterAnim.Attack4;
						} else if (CanPerformAnim(MonsterAnim.Attack5, animsAvailable)) {
							realAnim=MonsterAnim.Attack5;
						} else if (CanPerformAnim(MonsterAnim.Attack, animsAvailable)) {
							realAnim=MonsterAnim.Attack;
						} else if (CanPerformAnim(MonsterAnim.Attack2, animsAvailable)) {
							realAnim=MonsterAnim.Attack2;
						} else if (CanPerformAnim(MonsterAnim.Attack3, animsAvailable)) {
							realAnim=MonsterAnim.Attack3;
						} else if (CanPerformAnim(MonsterAnim.Attack6, animsAvailable)) {
							realAnim=MonsterAnim.Attack6;
						} else {
							realAnim=MonsterAnim.StandStill;
						}
						break;
					}
				case GenericAnim.GetHit: {
						if (CanPerformAnim(MonsterAnim.GetHit, animsAvailable)) {
							realAnim=MonsterAnim.GetHit;
						} else if (CanPerformAnim(MonsterAnim.GetHitWhileFlying, animsAvailable)) {
							realAnim=MonsterAnim.GetHitWhileFlying;
						} else {
							realAnim=MonsterAnim.StandStill;
						}
						break;
					}
				case GenericAnim.FallBackwards: {
						if (CanPerformAnim(MonsterAnim.FallBackwards, animsAvailable)) {
							realAnim=MonsterAnim.FallBackwards;
						} else if (CanPerformAnim(MonsterAnim.FallForwards, animsAvailable)) {
							realAnim=MonsterAnim.FallForwards;
						} else {
							realAnim=MonsterAnim.StandStill;
						}
						break;
					}
				case GenericAnim.FallForwards: {
						if (CanPerformAnim(MonsterAnim.FallForwards, animsAvailable)) {
							realAnim=MonsterAnim.FallForwards;
						} else if (CanPerformAnim(MonsterAnim.FallBackwards, animsAvailable)) {
							realAnim=MonsterAnim.FallBackwards;
						} else {
							realAnim=MonsterAnim.StandStill;
						}
						break;
					}
				case GenericAnim.Block: {		//also represents Dodge
						double dbl = Globals.dice.NextDouble();
						if (dbl < 0.5 && CanPerformAnim(MonsterAnim.BlockLeft, animsAvailable)) {
							realAnim=MonsterAnim.BlockLeft;
						} else if (CanPerformAnim(MonsterAnim.BlockRight, animsAvailable)) {
							realAnim=MonsterAnim.BlockRight;
						} else {
							realAnim=MonsterAnim.StandStill;
						}
						break;
					}
				case GenericAnim.AttackBareHands: {
						if (CanPerformAnim(MonsterAnim.Attack6, animsAvailable)) {
							realAnim=MonsterAnim.Attack6;
						} else if (CanPerformAnim(MonsterAnim.Attack, animsAvailable)) {
							realAnim=MonsterAnim.Attack;
						} else if (CanPerformAnim(MonsterAnim.Attack2, animsAvailable)) {
							realAnim=MonsterAnim.Attack2;
						} else if (CanPerformAnim(MonsterAnim.Attack3, animsAvailable)) {
							realAnim=MonsterAnim.Attack3;
						} else if (CanPerformAnim(MonsterAnim.Attack4, animsAvailable)) {
							realAnim=MonsterAnim.Attack4;
						} else if (CanPerformAnim(MonsterAnim.Attack5, animsAvailable)) {
							realAnim=MonsterAnim.Attack5;
						} else {
							realAnim=MonsterAnim.StandStill;
						}
						break;
					}
				case GenericAnim.Bow: {
						realAnim=MonsterAnim.StandStill;
						break;
					}
				case GenericAnim.Salute: {
						realAnim=MonsterAnim.StandStill;
						break;
					}
				case GenericAnim.Drink: {	//also represents Eat
						realAnim=MonsterAnim.StandStill;
						break;
					}
				default: {
						throw new SanityCheckException("Unknown generic anim "+anim);
					}
			}
			return realAnim;
		}

		public static HumanAnim GetHumanAnim(Character self, GenericAnim anim) {
			HumanAnim realAnim = HumanAnim.WalkUnarmed;
			switch (anim) {
				case GenericAnim.Walk: {
						if (self.Flag_Riding) {
							realAnim=HumanAnim.MountedWalk;
						} else if (self.Flag_WarMode) {
							realAnim=HumanAnim.WalkWarMode;
						} else if (self.WeaponAnimType != WeaponAnimType.BareHands) {
							realAnim=HumanAnim.WalkArmed;	//TODO: Check this, see if it looks right with two-handed weapons, only a shield, etc.
						} else {
							realAnim=HumanAnim.WalkUnarmed;
						}
						break;
					}
				case GenericAnim.Run: {
						if (self.Flag_Riding) {
							realAnim=HumanAnim.MountedRun;
						} else if (self.WeaponAnimType != WeaponAnimType.BareHands) {
							realAnim=HumanAnim.RunArmed;	//TODO: Check this, see if it looks right with two-handed weapons, only a shield, etc.
						} else {
							realAnim=HumanAnim.RunUnarmed;
						}
						break;
					}
				case GenericAnim.StandStill: {
						if (self.Flag_Riding) {
							realAnim=HumanAnim.MountedStandStill;
						} else if (self.Flag_WarMode) {
							realAnim=HumanAnim.WarMode;
						} else {
							realAnim=HumanAnim.StandStill;
						}
						break;
					}
				case GenericAnim.RandomIdleAction: {
						if (self.Flag_Riding) {
							double dbl = Globals.dice.NextDouble();
							if (dbl<.25) {
								realAnim=HumanAnim.MountedSalute;
							} else if (dbl<.5) {
								realAnim=HumanAnim.MountedBlock;
							} else if (dbl<.75) {
								realAnim=HumanAnim.MountedGetHit;
							} else {
								if (self.WeaponAnimType != WeaponAnimType.HeldInLeftHand) {
									//This looks like slapping the horse, and that's what wolfpack calls it,
									//but if you ask me, you shouldn't use it for an idle action unless you don't
									//have a two-handed weapon equipped (or it will look like you're attacking).
									realAnim=HumanAnim.MountedLeftHandAttack;
								} else {
									//Let's see how this looks. IIRC, if you're mounted you hold two-handed weapons
									//with only one hand, so this should look like slapping the horse too.
									realAnim=HumanAnim.MountedRightHandAttack;
								}
							}
						} else if (self.Flag_WarMode) {
							realAnim=HumanAnim.WarMode;	//no idle actions when in war mode.
						} else {
							double dbl = Globals.dice.NextDouble();
							if (dbl<.5) {
								realAnim=HumanAnim.LookAround;
							} else {
								realAnim=HumanAnim.LookDown;
							}
						}
						break;
					}
				case GenericAnim.IdleAction: {
						if (self.Flag_Riding) {
							double dbl = Globals.dice.NextDouble();
							if (dbl<.5) {
								realAnim=HumanAnim.MountedSalute;
							} else {
								if (self.WeaponAnimType != WeaponAnimType.HeldInLeftHand) {
									//This looks like slapping the horse, and that's what wolfpack calls it,
									//but if you ask me, you shouldn't use it for an idle action unless you don't
									//have a two-handed weapon equipped (or it will look like you're attacking).
									//Heh.
									realAnim=HumanAnim.MountedLeftHandAttack;
								} else {
									//Let's see how this looks. IIRC, if you're mounted you hold two-handed weapons
									//with only one hand, so this should look like slapping the horse too.
									realAnim=HumanAnim.MountedRightHandAttack;
								}
							}
						} else if (self.Flag_WarMode) {
							realAnim=HumanAnim.WarMode;	//no idle actions when in war mode.
						} else {
							realAnim=HumanAnim.LookDown;
						}
						break;
					}
				case GenericAnim.LookAround: {
						if (self.Flag_Riding) {
							realAnim=HumanAnim.MountedSalute;
						} else if (self.Flag_WarMode) {
							realAnim=HumanAnim.WarMode;	//no idle actions when in war mode.
						} else {
							realAnim=HumanAnim.LookAround;
						}
						break;
					}
				case GenericAnim.AttackSwing: {
						if (self.Flag_Riding) {
							if (self.WeaponAnimType == WeaponAnimType.HeldInLeftHand) {
								realAnim=HumanAnim.MountedLeftHandAttack;
							} else {
								realAnim=HumanAnim.MountedRightHandAttack;
							}
						} else if (self.WeaponAnimType == WeaponAnimType.HeldInLeftHand) {
							realAnim=HumanAnim.LeftHandSwing;
						} else {
							realAnim=HumanAnim.RightHandSwing;
						}
						break;
					}
				case GenericAnim.AttackStab: {
						if (self.Flag_Riding) {
							if (self.WeaponAnimType == WeaponAnimType.HeldInLeftHand) {
								realAnim=HumanAnim.MountedLeftHandAttack;
							} else {
								realAnim=HumanAnim.MountedRightHandAttack;
							}
						} else if (self.WeaponAnimType == WeaponAnimType.HeldInLeftHand) {
							realAnim=HumanAnim.LeftHandStab;
						} else {
							realAnim=HumanAnim.RightHandStab;
						}
						break;
					}
				case GenericAnim.AttackOverhead: {
						if (self.Flag_Riding) {
							if (self.WeaponAnimType == WeaponAnimType.HeldInLeftHand) {
								realAnim=HumanAnim.MountedLeftHandAttack;
							} else {
								realAnim=HumanAnim.MountedRightHandAttack;
							}
						} else if (self.WeaponAnimType == WeaponAnimType.HeldInLeftHand) {
							realAnim=HumanAnim.LeftHandOverhead;
						} else {
							realAnim=HumanAnim.RightHandOverhead;
						}
						break;
					}
				case GenericAnim.AttackShoot: {
						if (self.WeaponAnimType == WeaponAnimType.XBow) {
							if (self.Flag_Riding) {
								realAnim=HumanAnim.MountedFireCrossbow;
							} else {
								realAnim=HumanAnim.FireCrossbow;
							}
						} else {//what isnt xbow, is bow.
							if (self.Flag_Riding) {
								realAnim=HumanAnim.MountedFireBow;
							} else {
								realAnim=HumanAnim.FireBow;
							}
						}
						break;
					}
				case GenericAnim.GetHit: {
						if (self.Flag_Riding) {
							realAnim=HumanAnim.MountedGetHit;
						} else {
							realAnim=HumanAnim.GetHit;
						}
						break;
					}
				case GenericAnim.FallBackwards: {
						realAnim=HumanAnim.FallBackwards;
						break;
					}
				case GenericAnim.FallForwards: {
						realAnim=HumanAnim.FallForwards;
						break;
					}
				case GenericAnim.Block: {		//also represents Dodge
						if (self.Flag_Riding) {
							realAnim=HumanAnim.MountedBlock;
						} else {
							realAnim=HumanAnim.Block;
						}
						break;
					}
				case GenericAnim.AttackBareHands: {
						if (self.Flag_Riding) {
							realAnim=HumanAnim.MountedRightHandAttack;
						} else {
							realAnim=HumanAnim.AttackBareHands;
						}
						break;
					}
				case GenericAnim.Bow: {
						if (self.Flag_Riding) {
							realAnim=HumanAnim.MountedBlock;
						} else {
							realAnim=HumanAnim.Bow;
						}
						break;
					}
				case GenericAnim.Salute: {
						if (self.Flag_Riding) {
							realAnim=HumanAnim.MountedSalute;
						} else {
							realAnim=HumanAnim.Salute;
						}
						break;
					}
				case GenericAnim.Drink: {	//also represents Eat
						if (self.Flag_Riding) {
							realAnim=HumanAnim.MountedSalute;
						} else {
							realAnim=HumanAnim.Drink;
						}
						break;
					}
				default: {
						throw new SanityCheckException("Unknown generic anim "+anim);
					}
			}
			return realAnim;
		}

		public static byte TranslateAnim(Character self, GenericAnim anim) {
			byte realAnim = 0;
			if ((byte) anim>=(byte) HumanAnim.NumAnims) {
				Sanity.IfTrueThrow((byte) anim>0xff, "Cannot perform anim '"+anim+"', that number is too high.");
				realAnim = (byte) anim;
			} else {
				uint animsAvailable = self.AnimsAvailable;
				if (IsModelHuman(animsAvailable)) {
					realAnim=(byte) GetHumanAnim(self, anim);
				} else if (IsModelMonster(animsAvailable)) {
					realAnim=(byte) GetMonsterAnim(anim, animsAvailable);
				} else if (IsModelAnimal(animsAvailable)) {
					realAnim=(byte) GetAnimalAnim(anim, animsAvailable);
				}
			}
			Logger.WriteDebug("Translated "+anim+" to "+realAnim);
			return realAnim;
		}

		public static void PerformAttackAnim(Character self) {
			GenericAnim anim = GenericAnim.AttackBareHands;
			switch (self.WeaponType) {
				case WeaponType.OneHandSpike:
				case WeaponType.TwoHandSpike:
					anim = GenericAnim.AttackStab;
					break;
				case WeaponType.OneHandSword:
				case WeaponType.TwoHandSword:
					switch (Globals.dice.Next(3)) {
						case 0:
							anim = GenericAnim.AttackStab;
							break;
						case 1:
							anim = GenericAnim.AttackSwing;
							break;
						case 2:
							anim = GenericAnim.AttackOverhead;
							break;
					}
					break;

				case WeaponType.OneHandBlunt:
				case WeaponType.TwoHandBlunt://or should blunt weapons only do overhead?
				case WeaponType.OneHandAxe:
				case WeaponType.TwoHandAxe:
					switch (Globals.dice.Next(2)) {
						case 0:
							anim = GenericAnim.AttackOverhead;
							break;
						case 1:
							anim = GenericAnim.AttackSwing;
							break;
					}
					break;
				case WeaponType.XBowRunning:
				case WeaponType.XBowStand:
				case WeaponType.BowRunning:
				case WeaponType.BowStand:
					anim = GenericAnim.AttackShoot;
					break;

				//case WeaponAnimType.BareHands:
				//default:
				//    anim = GenericAnim.AttackPunch;
				//    break;
			}

			double seconds = self.WeaponDelay;
			byte frameDelay = 0;
			seconds = seconds - (0.25*attackAnimFrames);
			if (seconds > 0) {
				frameDelay = (byte)(seconds / (.1*attackAnimFrames));
			}
			
			self.Anim(TranslateAnim(self, anim), frameDelay);
		}

		const double attackAnimFrames = 3; //in fact it's 7 but we only use it for attack anim. The swing lands sooner than on the last frame...

		public static void PerformAnim(Character self, GenericAnim anim) {
			self.Anim(TranslateAnim(self, anim));
		}

		public static void PerformAnim(Character self, GenericAnim anim, bool backwards) {
			self.Anim(TranslateAnim(self, anim), backwards);
		}

		public static void PerformAnim(Character self, GenericAnim anim, bool backwards, bool undo) {
			self.Anim(TranslateAnim(self, anim), 1, backwards, undo, 0x01);
		}

		public static void PerformAnim(Character self, GenericAnim anim, bool backwards, byte frameDelay) {
			self.Anim(TranslateAnim(self, anim), 1, backwards, false, frameDelay);
		}

		public static void PerformAnim(Character self, GenericAnim anim, bool backwards, bool undo, byte frameDelay) {
			self.Anim(TranslateAnim(self, anim), 1, backwards, undo, frameDelay);
		}

		/*
			AnimsAvailable:
				0x80000000:	Human
				0x40000000: Animal
				0x20000000: Monster
				
				Animals and monsters use additional flags to specify which anims they have art for.
		*/
		public static bool IsModelHuman(Character self) {
			return IsModelHuman(self.AnimsAvailable);
		}

		private static bool IsModelHuman(uint animsAvailable) {
			return ((animsAvailable&0x80000000)>0);
		}

		public static bool IsModelAnimal(Character self) {
			return IsModelAnimal(self.AnimsAvailable);
		}

		private static bool IsModelAnimal(uint animsAvailable) {
			return ((animsAvailable&0x40000000)>0);
		}

		public static bool IsModelMonster(Character self) {
			return IsModelMonster(self.AnimsAvailable);
		}

		private static bool IsModelMonster(uint animsAvailable) {
			return ((animsAvailable&0x20000000)>0);
		}
	}
}