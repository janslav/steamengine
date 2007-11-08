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
using System.Collections;
using System.Collections.Generic;
using SteamEngine.Packets;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	public partial class ItemDef {
	}

	[Dialogs.ViewableClass]
    public partial class Item : AbstractItem {
		public override byte FlagsToSend {
			get {	//It looks like only 080 (invis) and 020 (static) are actually used
				int ret = 0;
				if (IsNotVisible) {
					ret |= 0x80;
				}
				return (byte) ret;
			}
		}

		public byte Flags {
			get {
				return flags;
			}
		}

		public bool Flag_Invisible {
			get {
				return ((flags&0x0080)==0x0080);
			}
			set {
				byte newFlags = (byte) (value?(flags|0x80):(flags&~0x80));
				if (newFlags != flags) {
					NetState.ItemAboutToChange(this);
					flags = newFlags;
				}
			}
		}

		public override sealed bool IsNotVisible {
			get {
				return (Flag_Invisible || !IsInVisibleLayer || Flag_Disconnected);
			}
		}

		public virtual bool IsMusicalInstrument { get {
			return false;
		} }

		[Summary("Enumerates every item in this item and items in all subcontainers, recurses into infinite deep.")]
		public IEnumerable<Item> EnumDeep() {
			ThrowIfDeleted();
			IEnumerator e = this.GetEnumerator();
			while (e.MoveNext()) {
				Item i = (Item) e.Current;
				yield return i;
			}
			e.Reset();
			while (e.MoveNext()) {
				Item i = (Item) e.Current;
				foreach (Item deep in i.EnumDeep()) {
					yield return deep;
				}
			}
		}

		[Summary("Enumerates every item in this item and items in all subcontainers, does not recurse.")]
		public IEnumerable<Item> EnumShallow() {
			ThrowIfDeleted();
			IEnumerator e = this.GetEnumerator();
			while (e.MoveNext()) {
				Item i = (Item) e.Current;
				yield return i;
			}
			e.Reset();
			while (e.MoveNext()) {
				Item i = (Item) e.Current;
				foreach (Item shallow in i) {
					yield return shallow;
				}
			}
		}

		public override float Weight {
			get {
				return Def.Weight;
			}
		}

		public override void FixWeight() {
		}

		public override void AdjustWeight(float adjust) {
			throw new InvalidOperationException("You can not set the weight of an ordinary (a.e. non-container) item");
		}

		public Item FindType(TriggerGroup type) {
			ThrowIfDeleted();
			foreach (Item i in this.EnumDeep()) {
				if (i.Type == type) {
					return i;
				}
			}
			return null;
		}

		public Item FindByClass(Type type) {
			foreach (Item i in this.EnumDeep()) {
				if (i.GetType() == type) {
					return i;
				}
			}
			return null;
		}

		public override void GetNameCliloc(out uint id, out string argument) {
			string name = this.Name;
			uint amount = this.Amount;
			id = 1042971;
			argument = null;
			if (this.Amount <= 1) {
				ItemDispidInfo idi = this.TypeDef.DispidInfo;
				if (idi != null) {
					if (string.Compare(name, idi.name, true) == 0) {
						id = (uint) (1020000 + (this.Model & 16383)); //hmmm...
						return;
					}
				}
				argument = name;
			} else {
				id = 1050039;//~1_NUMBER~ ~2_ITEMNAME~
				argument = string.Concat(amount.ToString(),"\t", name);
			}
		}

		public override void On_Click(AbstractCharacter clicker) {
			uint amount = this.Amount;
			if (this.Amount <= 1) {
				Server.SendNameFrom(clicker.Conn, this, this.Name, 0);
			} else {
				Server.SendNameFrom(clicker.Conn, this, string.Concat(amount.ToString(), " ", this.Name), 0);
			}
		}

		public virtual bool CanFallToCorpse {
			get {
				//TODO newbie flag etc.
				return true;
			}
		}

		private static TagKey linkTK = TagKey.Get("link");
		public virtual Thing Link {
			get {
				return this.GetTag(linkTK) as Thing;
			}
			set {
				this.SetTag(linkTK, value);
			}
		}

		public override AbstractItem NewItem(IThingFactory factory, uint amount) {
			Thing t = factory.Create(this);
			AbstractItem i = t as AbstractItem;
			if (i != null) {
				if (i.IsStackable) {
					i.Amount=amount;
				}
				return i;
			}
			if (t != null) {
				t.Delete();//we created a character, wtf? :)
			}
			throw new SEException(factory+" did not create an item.");
		}

		//public void PlayDropSound(AbstractCharacter droppingChar) {
		//    ScriptArgs sa = new ScriptArgs(droppingChar);
		//    if (!TryCancellableTrigger(TriggerKey.playDropSound, sa)) {
		//        On_DropSound(droppingChar);
		//    }
		//}

		//public virtual void On_DropSound(AbstractCharacter droppingChar) {
		//    this.SoundTo(this.TypeDef.DropSound, droppingChar);
		//}
	}
}