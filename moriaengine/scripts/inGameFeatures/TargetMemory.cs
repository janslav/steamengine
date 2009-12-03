using System;
using System.Collections.Generic;
using System.Text;

namespace SteamEngine.CompiledScripts.inGameFeatures {
    public static class TargetMemory {

        #region SetEquip
        [SteamFunction]
        public static void SetEquip(Player self, int n) {
            if (n <= 10) {
                    self.Target(SingletonScript<Targ_TargetMemory>.Instance, (object)n);
            } else {
                self.SysMessage("Moc velky index!");
            }
        }

        [SteamFunction]
        public static void SetEquip1(Player self) {
            SetEquip(self, 1);
        }

        [SteamFunction]
        public static void SetEquip2(Player self) {
            SetEquip(self, 2);
        }

        [SteamFunction]
        public static void SetEquip3(Player self) {
            SetEquip(self, 3);
        }

        [SteamFunction]
        public static void SetEquip4(Player self) {
            SetEquip(self, 4);
        }

        [SteamFunction]
        public static void SetEquip5(Player self) {
            SetEquip(self, 5);
        }

        [SteamFunction]
        public static void SetEquip6(Player self) {
            SetEquip(self, 6);
        }

        [SteamFunction]
        public static void SetEquip7(Player self) {
            SetEquip(self, 7);
        }

        [SteamFunction]
        public static void SetEquip8(Player self) {
            SetEquip(self, 8);
        }

        [SteamFunction]
        public static void SetEquip9(Player self) {
            SetEquip(self, 9);
        }

        [SteamFunction]
        public static void SetEquip10(Player self) {
            SetEquip(self, 10);
        }
        #endregion

        #region Heal
        [SteamFunction]
        public static void Heal(Player self, int n) {
            SkillSequenceArgs param = SkillSequenceArgs.Acquire(self, SkillName.Healing);
            param.Target1 = self.targMem[n - 1] as Character;
            if (param.Target1 != null) {
                param.PhaseSelect();
            } else {
                Globals.SrcWriteLine("Target na léèení nenalezen nebo nelze léèit.");
            }

        }

        [SteamFunction]
        public static void Heal1(Player self) {
            Heal(self, 1);
        }

        [SteamFunction]
        public static void Heal2(Player self) {
            Heal(self, 2);
        }

        [SteamFunction]
        public static void Heal3(Player self) {
            Heal(self, 3);
        }

        [SteamFunction]
        public static void Heal4(Player self) {
            Heal(self, 4);
        }

        [SteamFunction]
        public static void Heal5(Player self) {
            Heal(self, 5);
        }

        [SteamFunction]
        public static void Heal6(Player self) {
            Heal(self, 6);
        }

        [SteamFunction]
        public static void Heal7(Player self) {
            Heal(self, 7);
        }

        [SteamFunction]
        public static void Heal8(Player self) {
            Heal(self, 8);
        }

        [SteamFunction]
        public static void Heal9(Player self) {
            Heal(self, 9);
        }

        [SteamFunction]
        public static void Heal10(Player self) {
            Heal(self, 10);
        }
        #endregion

        #region UseEquip
        [SteamFunction]
        public static void UseEquip(Player self, int n) {
            if (n <= 10) {
            //TODO: Kontrola, jestli lze item pouzit.
                self.targMem[n].Use();
            } else {
                self.SysMessage("Moc velky index!");
            }
        }

        [SteamFunction]
        public static void UseEquip1(Player self) {
            UseEquip(self, 1);
        }

        [SteamFunction]
        public static void UseEquip2(Player self) {
            UseEquip(self, 2);
        }

        [SteamFunction]
        public static void UseEquip3(Player self) {
            UseEquip(self, 3);
        }

        [SteamFunction]
        public static void UseEquip4(Player self) {
            UseEquip(self, 4);
        }

        [SteamFunction]
        public static void UseEquip5(Player self) {
            UseEquip(self, 5);
        }

        [SteamFunction]
        public static void UseEquip6(Player self) {
            UseEquip(self, 6);
        }

        [SteamFunction]
        public static void UseEquip7(Player self) {
            UseEquip(self, 7);
        }

        [SteamFunction]
        public static void UseEquip8(Player self) {
            UseEquip(self, 8);
        }

        [SteamFunction]
        public static void UseEquip9(Player self) {
            UseEquip(self, 9);
        }

        [SteamFunction]
        public static void UseEquip10(Player self) {
            UseEquip(self, 10);
        }
        #endregion

        //#region SetLast
        //[SteamFunction]
        //public static void SetLast(Player self, int n) {
        //    if (n <= 10) {
                
        //    } else {
        //        self.SysMessage("Moc velky index!");
        //    }
        //}

        //[SteamFunction]
        //public static void SetLast1(Player self) {
        //    SetLast(self, 1);
        //}

        //[SteamFunction]
        //public static void SetLast2(Player self) {
        //    SetLast(self, 2);
        //}

        //[SteamFunction]
        //public static void SetLast3(Player self) {
        //    SetLast(self, 3);
        //}

        //[SteamFunction]
        //public static void SetLast4(Player self) {
        //    SetLast(self, 4);
        //}

        //[SteamFunction]
        //public static void SetLast5(Player self) {
        //    SetLast(self, 5);
        //}

        //[SteamFunction]
        //public static void SetLast6(Player self) {
        //    SetLast(self, 6);
        //}

        //[SteamFunction]
        //public static void SetLast7(Player self) {
        //    SetLast(self, 7);
        //}

        //[SteamFunction]
        //public static void SetLast8(Player self) {
        //    SetLast(self, 8);
        //}

        //[SteamFunction]
        //public static void SetLast9(Player self) {
        //    SetLast(self, 9);
        //}

        //[SteamFunction]
        //public static void SetLast10(Player self) {
        //    SetLast(self, 10);
        //}
        //#endregion
    }

    public class Targ_TargetMemory : CompiledTargetDef {
        private int n = 0;
        protected override void On_Start(Player self, object parameter) {
            Globals.SrcWriteLine("Vyber target.");
            n = (int)parameter;
            base.On_Start(self, parameter);
        }

        protected override bool On_TargonThing(Player self, Thing targetted, object parameter) {
            if (self.targMem == null) {
                self.targMem = new List<Thing>();
            }
            if (n < self.targMem.Count) {
                self.targMem[n - 1] = targetted;
            } else {
                while (n > self.targMem.Count) {
                    self.targMem.Add(null);
                }
                self.targMem[n - 1] = targetted;
            }
            Globals.SrcWriteLine("Target pridan.");
            return false;
        }
    }
}