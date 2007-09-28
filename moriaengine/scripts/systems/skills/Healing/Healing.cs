using System;
using SteamEngine;

namespace SteamEngine.CompiledScripts {

    public class t_bandage_blood : CompiledTriggerGroup {

    }

    public class t_bandage : CompiledTriggerGroup {
        public void On_Dclick(Item self, Character clicker) {
            if (self.TopObj() == clicker) {
                StartHealing(clicker, self);
            } else {
                Item otherBandage = null;
                foreach (Item i in clicker.Backpack) {
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
            self.SysMessage("prdel");
            //((Player) self).Target(
        }
    }
}