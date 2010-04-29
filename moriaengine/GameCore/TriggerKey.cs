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

		public static TriggerKey Acquire(string name) {
			TriggerKey key;
			if (byName.TryGetValue(name, out key)) {
				return key;
			}
			key = new TriggerKey(name, AbstractKey.GetNewUid());
			byName[name] = key;
			return key;
		}

		//Triggers defined as fields for faster access (Won't have to look up the string every time)

		internal static readonly TriggerKey command = Acquire("command");

		//internal static readonly TriggerKey fastWalk = Get("fastWalk");

		internal static readonly TriggerKey startup = Acquire("startup");
		internal static readonly TriggerKey shutdown = Acquire("shutdown");

		internal static readonly TriggerKey beforeSave = Acquire("beforeSave");
		internal static readonly TriggerKey openSaveStream = Acquire("openSaveStream");
		internal static readonly TriggerKey afterSave = Acquire("afterSave");
		internal static readonly TriggerKey beforeLoad = Acquire("beforeLoad");
		internal static readonly TriggerKey openLoadStream = Acquire("openLoadStream");
		internal static readonly TriggerKey afterLoad = Acquire("afterLoad");

		internal static readonly TriggerKey step = Acquire("step");
		internal static readonly TriggerKey itemStep = Acquire("itemStep");

		internal static readonly TriggerKey login = Acquire("login");
		internal static readonly TriggerKey logout = Acquire("logout");

		internal static readonly TriggerKey assign = Acquire("assign");
		internal static readonly TriggerKey unAssign = Acquire("unAssign");

		//internal static readonly TriggerKey newPC = Get("newPC");

		internal static readonly TriggerKey newPosition = Acquire("newPosition");

		internal static readonly TriggerKey itemLeave = Acquire("itemLeave");
		internal static readonly TriggerKey leaveItem = Acquire("leaveItem");
		internal static readonly TriggerKey leaveChar = Acquire("leaveChar");
		internal static readonly TriggerKey leaveRegion = Acquire("leaveRegion");

		internal static readonly TriggerKey itemEnter = Acquire("itemEnter");
		internal static readonly TriggerKey enterItem = Acquire("enterItem");
		internal static readonly TriggerKey enterChar = Acquire("enterChar");
		internal static readonly TriggerKey enterRegion = Acquire("enterRegion");

		internal static readonly TriggerKey stackOnItem = Acquire("stackOnItem");
		internal static readonly TriggerKey itemStackOn = Acquire("stackon_Item");

		internal static readonly TriggerKey denyPickup = Acquire("denyPickup");
		internal static readonly TriggerKey denyPickupItem = Acquire("denyPickupItem");
		internal static readonly TriggerKey denyPickupItemFrom = Acquire("denyPickupItemFrom");

		internal static readonly TriggerKey denyPutOnGround = Acquire("denyPutOnGround");
		internal static readonly TriggerKey denyPutItemOnGround = Acquire("denyPutItemOnGround");
		internal static readonly TriggerKey denyPutItemOn = Acquire("denyPutItemOn");

		internal static readonly TriggerKey denyPutInItem = Acquire("denyPutInItem");
		internal static readonly TriggerKey denyPutItemInItem = Acquire("denyPutItemInItem");
		internal static readonly TriggerKey denyPutItemIn = Acquire("denyPutItemIn");

		internal static readonly TriggerKey putItemOn = Acquire("putItemOn");
		internal static readonly TriggerKey putOnItem = Acquire("putOnItem");

		internal static readonly TriggerKey putOnChar = Acquire("putOnChar");
		internal static readonly TriggerKey putItemOnChar = Acquire("putItemOnChar");

		internal static readonly TriggerKey denyEquipOnChar = Acquire("denyEquipOnChar");
		internal static readonly TriggerKey denyEquip = Acquire("denyEquip");

		//internal static readonly TriggerKey playDropSound = Get("playDropSound");

		internal static readonly TriggerKey destroy = Acquire("Destroy");

		internal static readonly TriggerKey itemEquip = Acquire("itemEquip");
		internal static readonly TriggerKey equip = Acquire("Equip");
		internal static readonly TriggerKey itemUnEquip = Acquire("itemUnEquip");
		internal static readonly TriggerKey unEquip = Acquire("UnEquip");
		internal static readonly TriggerKey hear = Acquire("hear");
		internal static readonly TriggerKey say = Acquire("say");

		internal static readonly TriggerKey create = Acquire("create");
		internal static readonly TriggerKey dupe = Acquire("dupe");

		internal static readonly TriggerKey charDClick = Acquire("charDClick");
		internal static readonly TriggerKey itemDClick = Acquire("itemDClick");
		internal static readonly TriggerKey dClick = Acquire("DClick");
		//internal static readonly TriggerKey denyCharDClick = Get("denyCharDClick");
		internal static readonly TriggerKey denyItemDClick = Acquire("denyItemDClick");
		internal static readonly TriggerKey denyDClick = Acquire("denyDClick");

		internal static readonly TriggerKey charClick = Acquire("charClick");
		internal static readonly TriggerKey itemClick = Acquire("itemClick");
		internal static readonly TriggerKey click = Acquire("Click");

		internal static readonly TriggerKey aosClick = Acquire("aosClick");

		internal static readonly TriggerKey enter = Acquire("enter");//region trigger
		internal static readonly TriggerKey exit = Acquire("exit");//character exiting/entering a region. can be cancellable...
		internal static readonly TriggerKey clientAttach = Acquire("ClientAttach");

		internal static readonly TriggerKey containerOpen = Acquire("containerOpen");

		internal static readonly TriggerKey buildAosToolTips = Acquire("buildAosToolTips");
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
			return TriggerKey.Acquire(match.Groups["value"].Value);
		}

		public string Prefix {
			get {
				return "@";
			}
		}
	}
}