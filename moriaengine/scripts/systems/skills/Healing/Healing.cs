using System;
using SteamEngine;
using SteamEngine.Timers;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

    public class t_bandage_blood : CompiledTriggerGroup {

    }

    public class t_bandage : CompiledTriggerGroup {
        public void On_DClick(Item self, Character clicker) {
			//TODO? use resource system for consuming bandages

            if (self.TopObj() == clicker) {
                StartHealing(clicker, self);
            } else {
                Item otherBandage = null;
                foreach (Item i in clicker.BackpackAsContainer) {
                    if (i.Type == this) {
                        otherBandage = i;
                        break;
                    }
                }

                if (otherBandage != null) {
                    StartHealing(clicker, otherBandage);
                }
            }
        }

        [SteamFunction]
        public static void StartHealing(Character self, Item bandage) {
            ((Player)self).Target(SingletonScript<Targ_Healing>.Instance);
        }
    }


    public class Targ_Healing : CompiledTargetDef {

		protected override void On_Start(Player self, object parameter) {
            self.SysMessage("Koho se chce� pokusit l��it?");
            base.On_Start(self, parameter);
        }

		protected override bool On_TargonChar(Player self, Character targetted, object parameter) {
			//if (self.currentSkill != null) {
			//    self.ClilocSysMessage(500118);//You must wait a few moments to use another skill.
			//    return false;
			//}

            //TODO: kontrola jestli neni zmrzlej nebo tak neco

            //nevidi na cil
            if (targetted != self) {
                if (self.GetMap() != targetted.GetMap() || !self.GetMap().CanSeeLOSFromTo(self, targetted) || Point2D.GetSimpleDistance(self, targetted) > 6) {
                    self.SysMessage(targetted.Name + " je od tebe p��li� daleko!");
                    return false;
                }
            }
            //pokud ma cil stejne nebo vic zivotu
            if (targetted.Hits >= targetted.MaxHits) {
                if (targetted == self)
                    self.SysMessage("Jsi zcela zdr�v!");
                else
                    self.SysMessage(targetted.Name + " je zcela zdr�v!");
                return false;
            }

            //jestlize je to GM vylecime ihned bez otazek
            //  if (self.IsGM()) {
            //     targetted.Hits = targetted.MaxHits;
            //     return false;
            //  }


            HealingTimer timer = null;//new HealingTimer(targetted);

            //pokud jiz nekoho leci
            if (self.HasTimer(healingTimerKey)) {

                HealingTimer oldHealing = (HealingTimer)self.GetTimer(healingTimerKey);

                if (oldHealing == null) //nikdy by nemel byt null
                    return false;

                Character oldTarget = (Character)oldHealing.targetted;

                if (targetted == self) {
                    self.SysMessage("P�eru�il si l��en�!");
                } else {
                    self.SysMessage("P�eru�il si l��en� " + oldTarget.Name + "!");
                    oldTarget.SysMessage(self.Name + " p�eru�il tvoji l��bu!");
                }

                timer = oldHealing;
            }

            if (timer == null)
                timer = new HealingTimer(targetted);

            timer.DueInSeconds = SingletonScript<HealingSkillDef>.Instance.GetDelayForChar(self);

            //pokud se leci sam nebo jinyho
            if (self == targetted)
                self.SysMessage("Pokousis se lecit.");
            else {
                self.SysMessage("Pokousis se lecit " + targetted.Name + ".");
                targetted.SysMessage(self.Name + " se te pokousi lecit.");
            }

            //vyhodime mu z ruky zbran
            self.DisArm();


            self.AddTimer(healingTimerKey, timer);
            return false;
        }

		protected override bool On_TargonItem(Player self, Item targetted, object parameter) {
            self.SysMessage("P�edm�ty nelze l��it.");
            return false;
        }

        private TimerKey healingTimerKey = TimerKey.Get("_healingTimer_");
    }

    [DeepCopyableClass]
    [SaveableClass]
    public class HealingTimer : BoundTimer {

        [SaveableData]
        [CopyableData]
        public Character targetted;

        [LoadingInitializer]
        [DeepCopyImplementation]
        public HealingTimer() {
        }

        public HealingTimer(Character target) {
            this.targetted = target;
        }


        protected override void OnTimeout(TagHolder cont) {

            if (targetted == null || targetted.IsDeleted)
                return;

            Character self = (Character)cont;

            //nevidi na cil
            if (targetted != self) {
                if (self.GetMap() != targetted.GetMap() || !self.GetMap().CanSeeLOSFromTo(self, targetted) || Point2D.GetSimpleDistance(self, targetted) > 6) {
                    self.SysMessage(targetted.Name + " je od tebe p��li� daleko!");
                    return;
                }
            }

            //pokud ma cil stejne nebo vic zivotu
            if (targetted.Hits >= targetted.MaxHits) {

                if (targetted == self)
                    self.SysMessage("Jsi zcela zdr�v!");
                else
                    self.SysMessage(targetted.Name + " je zcela zdr�v!");

                return;
            }

            ushort selfHealing = self.GetSkill((int)SkillName.Healing);

            //pokud se leceni zdarilo
			if (SingletonScript<HealingSkillDef>.Instance.CheckSuccess(self, Globals.dice.Next(200))) {

				targetted.Hits += (short) ScriptUtil.EvalRangePermille(self.GetSkill((int) SkillName.Healing) + self.GetSkill((int) SkillName.Anatomy), SingletonScript<HealingSkillDef>.Instance.Effect);

                if (targetted.Hits > targetted.MaxHits)
                    targetted.Hits = targetted.MaxHits;



                self.SysMessage("L��en� se ti povedlo!");

                //jestlize je cil jinej nez healer rekneme hlasku
                if (targetted != self)
                    targetted.SysMessage(self.Name + " t� o�et�il!");

            } else {
                self.SysMessage("L��en� se ti nezda�ilo!");

                if (targetted != self)
                    targetted.SysMessage(self.Name + " se nepoda�ilo t� o�et�it!");

            }
        }
    }

    // jen kvuli staticky promeny, tartar urcite vymysli jak to udelat lip :o)
    
	//ano. pomoci singletonscriptu. Ale bylo potreba zmenit typ ty sekce v healing.scp -tar
    public class HealingSkillDef : SkillDef {

		public HealingSkillDef(string defname, string filename, int line)
			: base(defname, filename, line) {
		}

        //public static SkillDef defHealing = SkillDef.ById(SkillName.Healing);

		protected override void On_Start(Character ch) {
			throw new Exception("The method or operation is not implemented.");
		}

		protected override void On_Fail(Character ch) {
			throw new Exception("The method or operation is not implemented.");
		}

		protected override void On_Abort(Character ch) {
			throw new Exception("The method or operation is not implemented.");
		}

		protected override void On_Stroke(Character ch) {
			throw new Exception("The method or operation is not implemented.");
		}

		protected override void On_Success(Character ch) {
			throw new Exception("The method or operation is not implemented.");
		}

		protected override void On_Select(Character ch) {
			throw new Exception("The method or operation is not implemented.");
		}
	}
}