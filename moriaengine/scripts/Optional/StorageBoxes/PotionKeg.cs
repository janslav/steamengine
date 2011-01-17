using SteamEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.LScript;

partial class PotionKeg {
    protected override void On_Dclick() {
        self.SysMessage("Co, chces nalit do potion kegu ?");
    }
   
}
