using System;
using SteamEngine;
using SteamEngine.Timers;
using SteamEngine.Persistence;
using SteamEngine.Common;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {



    public class t_bandage_blood : CompiledTriggerGroup {

    }

    public class t_bandage : CompiledTriggerGroup {

        public void On_DClick(Item self, Character clicker) {
            //TODO? use resource system for consuming bandages
            SkillSequenceArgs skillSeq = SkillSequenceArgs.Acquire(clicker, SkillName.Healing);
            
            GameState state = clicker.GameState;

            if (self.TopObj() == self) {
                skillSeq.Tool = self;
                StartHealing(clicker, skillSeq);
            } else {
                Item otherBandage = null;
                foreach (Item i in clicker.Backpack) {
                    if (i.Type == BandageDef.Type) {
                        otherBandage = i;
                        break;
                    }
                }

                if (otherBandage != null) {
                    skillSeq.Tool = otherBandage;
                    StartHealing(clicker,skillSeq);
                } else if (otherBandage == null) {
                    if (state != null) {
                        state.WriteLine(Loc<HealingLoc>.Get(state.Language).NoBands);
                    }
                }
            }
        }

        private static ItemDef bandageDef;
        public static ItemDef BandageDef {
            get {
                if (bandageDef == null) {
                    bandageDef = (ItemDef)ThingDef.GetByDefname("i_bandage");
                }
                return bandageDef;
            }
        }

        [SteamFunction]
        public static void StartHealing(Character self, object parametr) {
            ((Player)self).Target(SingletonScript<Targ_Healing>.Instance,parametr);
        }
        
}


    public class Targ_Healing : CompiledTargetDef {
        SkillSequenceArgs skillSeq = null;
        

        protected override void On_Start(Player self, object parameter) {
            skillSeq = (SkillSequenceArgs)parameter;
            GameState stateSelf = self.GameState;
            if (stateSelf != null) {
                stateSelf.WriteLine(Loc<HealingLoc>.Get(stateSelf.Language).TargetWho);
            }
            base.On_Start(self, parameter);
        }

        protected override bool On_TargonChar(Player self, Character targetted, object parameter) {
            //TODO: kontrola jestli neni zmrzlej nebo tak neco

            if (targetted != null) {
                skillSeq.Target1 = targetted;
                skillSeq.PhaseSelect();
            }
            return false;
        }

        protected override bool On_TargonItem(Player self, Item targetted, object parameter) {
            GameState stateSelf = self.GameState;
            if (stateSelf != null) {
                stateSelf.WriteLine(Loc<HealingLoc>.Get(stateSelf.Language).TargetItem);
            }
            return false;
        }

        private TimerKey healingTimerKey = TimerKey.Acquire("_healingTimer_");
    }

    // jen kvuli staticky promeny, tartar urcite vymysli jak to udelat lip :o)
    //ano. pomoci singletonscriptu. Ale bylo potreba zmenit typ ty sekce v healing.scp -tar
    [Dialogs.ViewableClass]
    public class HealingSkillDef : SkillDef {

        public HealingSkillDef(string defname, string filename, int line)
            : base(defname, filename, line) {
        }

        //public static SkillDef defHealing = SkillDef.ById(SkillName.Healing);

        protected override bool On_Select(SkillSequenceArgs skillSeqArgs) {
            Character self = skillSeqArgs.Self;
            Character target = skillSeqArgs.Target1 as Character;
            Item tool = skillSeqArgs.Tool;
            GameState stateSelf = self.GameState;
            
            if (tool == null) {
                Item otherBandage = null;
                foreach (Item i in self.Backpack) {
                    if (i.Type == BandageDef.Type) {
                        otherBandage = i;
                        break;
                    }
                }

                if (otherBandage != null) {
                    skillSeqArgs.Tool = otherBandage;
                } else if (otherBandage == null) {
                    if (stateSelf != null) {
                        stateSelf.WriteLine(Loc<HealingLoc>.Get(stateSelf.Language).NoBands);
                    }
                    return true;
                }
            }

            //není cíl
            if (target == null) {
                return true;
            }
            GameState stateTarget = target.GameState;

            //nevidi na cil
			if (Point2D.GetSimpleDistance(self, target) > 6) {
                if (stateSelf != null) {
                    stateSelf.WriteLine(String.Format(System.Globalization.CultureInfo.InvariantCulture, 
						Loc<HealingLoc>.Get(stateSelf.Language).TargetOut, target.Name));
                }
                return true;
			}

			if (!self.CanInteractWithMessage(target)) {
				return true;
            }

            //pokud ma cil stejne nebo vic zivotu
            if (target.Hits >= target.MaxHits) {
                if (target == self) {
                    if (stateSelf != null) {
                        stateSelf.WriteLine(Loc<HealingLoc>.Get(stateSelf.Language).SelfFull);
                    }
                } else {
                    if (stateTarget != null) {
                        stateTarget.WriteLine(String.Format(System.Globalization.CultureInfo.InvariantCulture,
							Loc<HealingLoc>.Get(stateTarget.Language).TargetFull, target.Name));
                    }
                }
                return true;
            }

            //TODO ? Dodelat podminku, kdyz je hrac frozen
 
            return false;
        }
        

        protected override bool On_Start(SkillSequenceArgs skillSeqArgs) {
            Character self = skillSeqArgs.Self;
            GameState stateSelf = self.GameState;

            skillSeqArgs.DelayInSeconds = this.GetDelayForChar(self);

            if (skillSeqArgs.Target1 == self) {
                if (stateSelf != null) {
                    stateSelf.WriteLine(Loc<HealingLoc>.Get(stateSelf.Language).SelfStartHeal);
                }
            } else {
                if (stateSelf != null) {
                    stateSelf.WriteLine(String.Format(System.Globalization.CultureInfo.InvariantCulture,
						Loc<HealingLoc>.Get(stateSelf.Language).TargetStartHeal, ((Character) skillSeqArgs.Target1).Name));
                }
            }
            if (skillSeqArgs.DelaySpan < TimeSpan.Zero) {
                skillSeqArgs.PhaseStroke();
            } else {
                skillSeqArgs.Self.AddTimer(TimerKey.Acquire("_skillTimer_"), new SkillSequenceArgs.SkillStrokeTimer(skillSeqArgs)).DueInSpan = skillSeqArgs.DelaySpan;
            }
            return true;
        }

        protected override bool On_Stroke(SkillSequenceArgs skillSeqArgs) {
            Character self = skillSeqArgs.Self;
            Character target = skillSeqArgs.Target1 as Character;
            Item tool = skillSeqArgs.Tool;
            GameState stateSelf = self.GameState;

            if (skillSeqArgs.Tool.IsDeleted || skillSeqArgs.Tool.TopObj() != self) {
                if (stateSelf != null) {
                    stateSelf.WriteLine(Loc<HealingLoc>.Get(stateSelf.Language).NoBands);
                }
                return true;
            }

            skillSeqArgs.Success = this.CheckSuccess(self, Globals.dice.Next(200));

            return false;
        }

        protected override bool On_Success(SkillSequenceArgs skillSeqArgs) {

            Character targetted = skillSeqArgs.Target1 as Character;
            Character self = skillSeqArgs.Self;

            // TODO? Vytvorit vzorec pro maximum a minimum pridanych HP

            targetted.Hits += (short)ScriptUtil.EvalRangePermille(self.GetSkill(SkillName.Healing) + self.GetSkill(SkillName.Anatomy),this.Effect);

            if (targetted.Hits > targetted.MaxHits) {
                targetted.Hits = targetted.MaxHits;
            }
            GameState state = skillSeqArgs.Self.GameState;
            if (state != null) {
                state.WriteLine(Loc<HealingLoc>.Get(state.Language).Success);
            }
            skillSeqArgs.Tool.Amount -= 1;
            BloodyBandageAdder(self);
            return false;
        }

        protected override bool On_Fail(SkillSequenceArgs skillSeqArgs) {
            skillSeqArgs.Tool.Amount -= 1;
            GameState state = skillSeqArgs.Self.GameState;
            if (state != null) {
                state.WriteLine(Loc<HealingLoc>.Get(state.Language).Fail);
            }
            Character targetted = skillSeqArgs.Target1 as Character;
            if (targetted != skillSeqArgs.Self) {
                if (state != null) {
                    state.WriteLine(String.Format(System.Globalization.CultureInfo.InvariantCulture,
						Loc<HealingLoc>.Get(state.Language).TargetFailed, targetted.Name));
                }
            }
            return false;
        }


        protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
            throw new SEException("The method or operation is not implemented.");
        }


        private static ItemDef bloodyBandageDef;
        public static ItemDef BloodyBandageDef {
            get {
                if (bloodyBandageDef == null) {
                    bloodyBandageDef = (ItemDef)ThingDef.GetByDefname("i_bandage_bloody");
                }
                return bloodyBandageDef;
            }
        }

        private static ItemDef bandageDef;
        public static ItemDef BandageDef {
            get {
                if (bandageDef == null) {
                    bandageDef = (ItemDef)ThingDef.GetByDefname("i_bandage");
                }
                return bandageDef;
            }
        }

        private static void BloodyBandageAdder(Character self) {
            Item bloodybandage = null;
            foreach (Item i in self.Backpack) {
                if (i.Type == BloodyBandageDef.Type) {
                    bloodybandage = i;
                    break;
                }
            }
            if (bloodybandage == null) {
                BloodyBandageDef.Create(self.Backpack);
            } else {
                bloodybandage.Amount += 1;
            }
        }
    }

    public class HealingLoc : CompiledLocStringCollection {
        internal readonly string Success = "Léèení se ti povedlo!";
        internal readonly string Fail = "Léèení se ti nezdaøilo!";
        internal readonly string NoBands = "Nemáš u sebe bandáže!";
        internal readonly string TargetOut = "{0} je od tebe pøíliš daleko!";
        internal readonly string SelfFull = "Jsi zcela zdráv!";
		internal readonly string TargetFull = "{0} je zcela zdráv!";
		internal readonly string TargetFailed = "{0} selhal pøi pokusu tì ošetøit!";
        internal readonly string TargetWho = "Koho chceš léèit?";
        internal readonly string TargetItem = "Pøedmìty nelze léèit.";
        internal readonly string TargetStartHeal = "Zaèínáš léèit {0}.";
        internal readonly string SelfStartHeal = "Zaèínáš se léèit.";
    }
}