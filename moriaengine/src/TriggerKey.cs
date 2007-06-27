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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Globalization;
using SteamEngine.Packets;
using System.Text.RegularExpressions;
	
namespace SteamEngine {
	//TriggerKeys are used when calling triggers. You should call Get(name) once to get a TriggerKey, and then use
	//that from then on for calling that trigger.
	//This and FunctionKey are very similar, and serve similar purposes.
	public class TriggerKey : AbstractKey{
		private static Hashtable byName = new Hashtable(StringComparer.OrdinalIgnoreCase);
		private static int uids = 0;
				
		private TriggerKey(string name, int uid) : base(name, uid) {
		}
		
		public static TriggerKey Get(string name) {
			TriggerKey tk = byName[name] as TriggerKey;
			if (tk!=null) {
				return tk;
			}
			int uid=uids++;
			tk = new TriggerKey(name,uid);
			byName[name]=tk;
			return tk;
		}
		
		//Triggers defined as fields for faster access (Won't have to look up the string every time)
		
		public static readonly TriggerKey command=Get("command");
		
		public static readonly TriggerKey fastWalk=Get("fastWalk");
		
		public static readonly TriggerKey startup=Get("startup");
		public static readonly TriggerKey shutdown=Get("shutdown");
		
		public static readonly TriggerKey beforeSave=Get("beforeSave");
		public static readonly TriggerKey openSaveStream=Get("openSaveStream");
		public static readonly TriggerKey afterSave=Get("afterSave");
		public static readonly TriggerKey beforeLoad=Get("beforeLoad");
		public static readonly TriggerKey openLoadStream=Get("openLoadStream");
		public static readonly TriggerKey afterLoad=Get("afterLoad");
		
		public static readonly TriggerKey step=Get("step");
		public static readonly TriggerKey itemStep=Get("itemStep");
		
		public static readonly TriggerKey login=Get("login");
		public static readonly TriggerKey logout=Get("logout");
		
		public static readonly TriggerKey newPC=Get("newPC");
		
		public static readonly TriggerKey stackon_Item=Get("stackon_Item");//item trigger
		public static readonly TriggerKey stackon_Char=Get("stackon_Char");//item trigger
		public static readonly TriggerKey itemDropon_Item=Get("itemDropon_Item");//character trigger
		public static readonly TriggerKey itemDropon_Char=Get("itemDropon_Char");//character trigger
		public static readonly TriggerKey stackOn=Get("StackOn");//thing trigger
		public static readonly TriggerKey itemStackOn_Item=Get("itemStackOn_Item");//may be redundant (practically the same as itemDropon_Item)
		public static readonly TriggerKey itemStackOn_Char=Get("itemStackOn_Char");//may be redundant (practically the same as itemDropon_Char)
		
		public static readonly TriggerKey itemDropon_Ground=Get("itemDropon_Ground");
		public static readonly TriggerKey dropon_Ground=Get("Dropon_Ground");
		public static readonly TriggerKey playDropSound=Get("playDropSound");
		
		public static readonly TriggerKey itemPickup_Ground=Get("itemPickup_Ground");
		public static readonly TriggerKey pickUp_Ground=Get("pickUp_Ground");
		
		public static readonly TriggerKey itemPickup_Pack=Get("itemPickup_Pack");
		public static readonly TriggerKey pickUp_Pack=Get("Pickup_Pack");
		
		public static readonly TriggerKey pickUpFrom=Get("pickUpFrom");
		
		public static readonly TriggerKey Destroy=Get("Destroy");
		
		public static readonly TriggerKey itemEquip=Get("itemEquip");
		public static readonly TriggerKey Equip=Get("Equip");
		public static readonly TriggerKey itemUnEquip=Get("itemUnEquip");
		public static readonly TriggerKey unEquip=Get("UnEquip");
		public static readonly TriggerKey hear=Get("hear");
		public static readonly TriggerKey say=Get("say");
		public static readonly TriggerKey see=Get("see");
		public static readonly TriggerKey lostSightOf=Get("lostSightOf");
		public static readonly TriggerKey seeMoving=Get("seeMoving");
		public static readonly TriggerKey memoryEquip=Get("memoryEquip");
		public static readonly TriggerKey memoryUnEquip=Get("memoryUnEquip");
		
		public static readonly TriggerKey create=Get("create");
		
		public static readonly TriggerKey charDClick=Get("charDClick");
		public static readonly TriggerKey itemDClick=Get("itemDClick");
		public static readonly TriggerKey DClick=Get("DClick");
		
		public static readonly TriggerKey charClick=Get("charClick");
		public static readonly TriggerKey itemClick=Get("itemClick");
		public static readonly TriggerKey click=Get("Click");

		public static readonly TriggerKey aosClick=Get("aosClick");
		
		
		public static readonly TriggerKey enter=Get("enter");//region trigger
		public static readonly TriggerKey exit=Get("exit");//character exiting/entering a region. can be cancellable...
		public static readonly TriggerKey ClientAttach=Get("ClientAttach");

	}


	public class TriggerKeySaveImplementor : SteamEngine.Persistence.ISimpleSaveImplementor {
		public static Regex re = new Regex(@"^\@(?<value>.+)\s*$",                     
			RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);
	
		public Type HandledType { get {
			return typeof(TriggerKey);
		} }
		
		public Regex LineRecognizer { get {
			return re;
		} }
		
		public string Save(object objToSave) {
			return "@"+((TriggerKey) objToSave).name;
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