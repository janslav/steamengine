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
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SteamEngine {
	//TriggerKeys are used when calling triggers. You should call Get(name) once to get a TriggerKey, and then use
	//that from then on for calling that trigger.
	public class TriggerKey : AbstractKey {
		private static Dictionary<string, TriggerKey> byName = new Dictionary<string, TriggerKey>(StringComparer.OrdinalIgnoreCase);

		private TriggerKey(string name, int uid)
			: base(name, uid) {
		}

		public static TriggerKey Get(string name) {
			TriggerKey key;
			if (byName.TryGetValue(name, out key)) {
				return key;
			}
			key = new TriggerKey(name, AbstractKey.GetNewUid());
			byName[name] = key;
			return key;
		}

		//Triggers defined as fields for faster access (Won't have to look up the string every time)

		public static readonly TriggerKey command = Get("command");

		public static readonly TriggerKey fastWalk = Get("fastWalk");

		public static readonly TriggerKey startup = Get("startup");
		public static readonly TriggerKey shutdown = Get("shutdown");

		public static readonly TriggerKey beforeSave = Get("beforeSave");
		public static readonly TriggerKey openSaveStream = Get("openSaveStream");
		public static readonly TriggerKey afterSave = Get("afterSave");
		public static readonly TriggerKey beforeLoad = Get("beforeLoad");
		public static readonly TriggerKey openLoadStream = Get("openLoadStream");
		public static readonly TriggerKey afterLoad = Get("afterLoad");

		public static readonly TriggerKey step = Get("step");
		public static readonly TriggerKey itemStep = Get("itemStep");

		public static readonly TriggerKey login = Get("login");
		public static readonly TriggerKey logout = Get("logout");

		public static readonly TriggerKey assign = Get("assign");
		public static readonly TriggerKey unAssign = Get("unAssign");

		public static readonly TriggerKey newPC = Get("newPC");

		public static readonly TriggerKey newPosition = Get("newPosition");

		public static readonly TriggerKey itemLeave = Get("itemLeave");
		public static readonly TriggerKey leaveItem = Get("leaveItem");
		public static readonly TriggerKey leaveChar = Get("leaveChar");
		public static readonly TriggerKey leaveRegion = Get("leaveRegion");

		public static readonly TriggerKey itemEnter = Get("itemEnter");
		public static readonly TriggerKey enterItem = Get("enterItem");
		public static readonly TriggerKey enterChar = Get("enterChar");
		public static readonly TriggerKey enterRegion = Get("enterRegion");

		public static readonly TriggerKey stackOnItem = Get("stackOnItem");
		public static readonly TriggerKey itemStackOn = Get("stackon_Item");

		public static readonly TriggerKey denyPickup = Get("denyPickup");
		public static readonly TriggerKey denyPickupItem = Get("denyPickupItem");
		public static readonly TriggerKey denyPickupItemFrom = Get("denyPickupItemFrom");

		public static readonly TriggerKey denyPutOnGround = Get("denyPutOnGround");
		public static readonly TriggerKey denyPutItemOnGround = Get("denyPutItemOnGround");
		public static readonly TriggerKey denyPutItemOn = Get("denyPutItemOn");

		public static readonly TriggerKey denyPutInItem = Get("denyPutInItem");
		public static readonly TriggerKey denyPutItemInItem = Get("denyPutItemInItem");
		public static readonly TriggerKey denyPutItemIn = Get("denyPutItemIn");

		public static readonly TriggerKey putItemOn = Get("putItemOn");
		public static readonly TriggerKey putOnItem = Get("putOnItem");

		public static readonly TriggerKey putOnChar = Get("putOnChar");
		public static readonly TriggerKey putItemOnChar = Get("putItemOnChar");

		public static readonly TriggerKey denyEquipOnChar = Get("denyEquipOnChar");
		public static readonly TriggerKey denyEquip = Get("denyEquip");

		public static readonly TriggerKey playDropSound = Get("playDropSound");

		public static readonly TriggerKey destroy = Get("Destroy");

		public static readonly TriggerKey itemEquip = Get("itemEquip");
		public static readonly TriggerKey equip = Get("Equip");
		public static readonly TriggerKey itemUnEquip = Get("itemUnEquip");
		public static readonly TriggerKey unEquip = Get("UnEquip");
		public static readonly TriggerKey hear = Get("hear");
		public static readonly TriggerKey say = Get("say");

		public static readonly TriggerKey create = Get("create");
		public static readonly TriggerKey dupe = Get("dupe");

		public static readonly TriggerKey charDClick = Get("charDClick");
		public static readonly TriggerKey itemDClick = Get("itemDClick");
		public static readonly TriggerKey dClick = Get("DClick");
		public static readonly TriggerKey denyCharDClick = Get("denyCharDClick");
		public static readonly TriggerKey denyItemDClick = Get("denyItemDClick");
		public static readonly TriggerKey denyDClick = Get("denyDClick");

		public static readonly TriggerKey charClick = Get("charClick");
		public static readonly TriggerKey itemClick = Get("itemClick");
		public static readonly TriggerKey click = Get("Click");

		public static readonly TriggerKey aosClick = Get("aosClick");

		public static readonly TriggerKey enter = Get("enter");//region trigger
		public static readonly TriggerKey exit = Get("exit");//character exiting/entering a region. can be cancellable...
		public static readonly TriggerKey clientAttach = Get("ClientAttach");

		public static readonly TriggerKey containerOpen = Get("containerOpen");

	}


	public sealed class TriggerKeySaveImplementor : SteamEngine.Persistence.ISimpleSaveImplementor {
		public static Regex re = new Regex(@"^\@(?<value>.+)\s*$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		public Type HandledType {
			get {
				return typeof(TriggerKey);
			}
		}

		public Regex LineRecognizer {
			get {
				return re;
			}
		}

		public string Save(object objToSave) {
			return "@" + ((TriggerKey) objToSave).Name;
		}

		public object Load(Match match) {
			return TriggerKey.Get(match.Groups["value"].Value);
		}

		public string Prefix {
			get {
				return "@";
			}
		}
	}
}