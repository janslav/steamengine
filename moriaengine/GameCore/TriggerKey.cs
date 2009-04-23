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
	public sealed class TriggerKey : AbstractKey {
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

		internal static readonly TriggerKey command = Get("command");

		//internal static readonly TriggerKey fastWalk = Get("fastWalk");

		internal static readonly TriggerKey startup = Get("startup");
		internal static readonly TriggerKey shutdown = Get("shutdown");

		internal static readonly TriggerKey beforeSave = Get("beforeSave");
		internal static readonly TriggerKey openSaveStream = Get("openSaveStream");
		internal static readonly TriggerKey afterSave = Get("afterSave");
		internal static readonly TriggerKey beforeLoad = Get("beforeLoad");
		internal static readonly TriggerKey openLoadStream = Get("openLoadStream");
		internal static readonly TriggerKey afterLoad = Get("afterLoad");

		internal static readonly TriggerKey step = Get("step");
		internal static readonly TriggerKey itemStep = Get("itemStep");

		internal static readonly TriggerKey login = Get("login");
		internal static readonly TriggerKey logout = Get("logout");

		internal static readonly TriggerKey assign = Get("assign");
		internal static readonly TriggerKey unAssign = Get("unAssign");

		//internal static readonly TriggerKey newPC = Get("newPC");

		internal static readonly TriggerKey newPosition = Get("newPosition");

		internal static readonly TriggerKey itemLeave = Get("itemLeave");
		internal static readonly TriggerKey leaveItem = Get("leaveItem");
		internal static readonly TriggerKey leaveChar = Get("leaveChar");
		internal static readonly TriggerKey leaveRegion = Get("leaveRegion");

		internal static readonly TriggerKey itemEnter = Get("itemEnter");
		internal static readonly TriggerKey enterItem = Get("enterItem");
		internal static readonly TriggerKey enterChar = Get("enterChar");
		internal static readonly TriggerKey enterRegion = Get("enterRegion");

		internal static readonly TriggerKey stackOnItem = Get("stackOnItem");
		internal static readonly TriggerKey itemStackOn = Get("stackon_Item");

		internal static readonly TriggerKey denyPickup = Get("denyPickup");
		internal static readonly TriggerKey denyPickupItem = Get("denyPickupItem");
		internal static readonly TriggerKey denyPickupItemFrom = Get("denyPickupItemFrom");

		internal static readonly TriggerKey denyPutOnGround = Get("denyPutOnGround");
		internal static readonly TriggerKey denyPutItemOnGround = Get("denyPutItemOnGround");
		internal static readonly TriggerKey denyPutItemOn = Get("denyPutItemOn");

		internal static readonly TriggerKey denyPutInItem = Get("denyPutInItem");
		internal static readonly TriggerKey denyPutItemInItem = Get("denyPutItemInItem");
		internal static readonly TriggerKey denyPutItemIn = Get("denyPutItemIn");

		internal static readonly TriggerKey putItemOn = Get("putItemOn");
		internal static readonly TriggerKey putOnItem = Get("putOnItem");

		internal static readonly TriggerKey putOnChar = Get("putOnChar");
		internal static readonly TriggerKey putItemOnChar = Get("putItemOnChar");

		internal static readonly TriggerKey denyEquipOnChar = Get("denyEquipOnChar");
		internal static readonly TriggerKey denyEquip = Get("denyEquip");

		//internal static readonly TriggerKey playDropSound = Get("playDropSound");

		internal static readonly TriggerKey destroy = Get("Destroy");

		internal static readonly TriggerKey itemEquip = Get("itemEquip");
		internal static readonly TriggerKey equip = Get("Equip");
		internal static readonly TriggerKey itemUnEquip = Get("itemUnEquip");
		internal static readonly TriggerKey unEquip = Get("UnEquip");
		internal static readonly TriggerKey hear = Get("hear");
		internal static readonly TriggerKey say = Get("say");

		internal static readonly TriggerKey create = Get("create");
		internal static readonly TriggerKey dupe = Get("dupe");

		internal static readonly TriggerKey charDClick = Get("charDClick");
		internal static readonly TriggerKey itemDClick = Get("itemDClick");
		internal static readonly TriggerKey dClick = Get("DClick");
		//internal static readonly TriggerKey denyCharDClick = Get("denyCharDClick");
		internal static readonly TriggerKey denyItemDClick = Get("denyItemDClick");
		internal static readonly TriggerKey denyDClick = Get("denyDClick");

		internal static readonly TriggerKey charClick = Get("charClick");
		internal static readonly TriggerKey itemClick = Get("itemClick");
		internal static readonly TriggerKey click = Get("Click");

		internal static readonly TriggerKey aosClick = Get("aosClick");

		internal static readonly TriggerKey enter = Get("enter");//region trigger
		internal static readonly TriggerKey exit = Get("exit");//character exiting/entering a region. can be cancellable...
		internal static readonly TriggerKey clientAttach = Get("ClientAttach");

		internal static readonly TriggerKey containerOpen = Get("containerOpen");
	}


	public sealed class TriggerKeySaveImplementor : SteamEngine.Persistence.ISimpleSaveImplementor {
		private static Regex re = new Regex(@"^\@(?<value>.+)\s*$",
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

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public string Save(object objToSave) {
			return "@" + ((TriggerKey) objToSave).Name;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
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