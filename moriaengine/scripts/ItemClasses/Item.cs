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
using SteamEngine.Networking;
using SteamEngine.Communication.TCP;

namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	public partial class ItemDef {
		[Summary("Check the resources and skillmake if the given character can craft this item")]
		public bool CanBeMade(Character chr) {
			if (chr.IsGM) {//GM can everything
				return true;
			}
			//skillmake (skills, tools etc.)
			ResourcesList requir = SkillMake;
			if (requir != null) {
				IResourceListItem missingItem;
				if (!requir.HasResourcesPresent(chr, ResourcesLocality.BackpackAndLayers, out missingItem)) {
					return false;
				}
			}

			//resources (necessary items)
			ResourcesList reslist = Resources;
			if (reslist != null) {
				IResourceListItem missingItem;
				if (!reslist.HasResourcesPresent(chr, ResourcesLocality.BackpackAndLayers, out missingItem)) {
					return false;
				}
			}
			return true;
		}
	}

	[Dialogs.ViewableClass]
	public partial class Item : AbstractItem {
		[Summary("Consume desired amount of this item, amount cannot go below zero. If resulting amount is 0 " +
				" then the item will be deleted. Method returns the actually consumed amount.")]
		public uint Consume(long howMany) {
			long prevAmount = this.Amount;
			long resultAmount = prevAmount - howMany;

			if (resultAmount < 1) {
				this.Delete();
				return (uint) prevAmount;//consumed all of the item (not necesarilly the whole "howMuch")
			} else {
				this.Amount = (uint) resultAmount;
				return (uint) howMany; //consumed the desired amount
			}
		}

		public override byte FlagsToSend {
			get {	//It looks like only 080 (invis) and 020 (static) are actually used
				int ret = 0;
				if (this.IsNotVisible) {
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
				return ((flags & 0x0080) == 0x0080);
			}
			set {
				byte newFlags = (byte) (value ? (flags | 0x80) : (flags & ~0x80));
				if (newFlags != this.flags) {
					if (value) {
						OpenedContainers.SetContainerClosed(this);
						this.RemoveFromView();
					}
					ItemSyncQueue.AboutToChange(this);
					flags = newFlags;
				}
			}
		}

		public override sealed bool IsNotVisible {
			get {
				return (this.Flag_Invisible || !this.IsInVisibleLayer || this.Flag_Disconnected);
			}
		}

		public virtual bool IsMusicalInstrument {
			get {
				return false;
			}
		}

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
			id = 1042971;//~1_NOTHING~
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
				argument = string.Concat(amount.ToString(), "\t", name);
			}
		}

		public override void On_Click(AbstractCharacter clicker, GameState clickerState, TCPConnection<GameState> clickerConn) {
			uint amount = this.Amount;
			if (this.Amount <= 1) {
				PacketSequences.SendNameFrom(clickerConn, this, this.Name, 0);
			} else {
				PacketSequences.SendNameFrom(clickerConn, this, string.Concat(amount.ToString(), " ", this.Name), 0);
			}
		}

		public virtual bool CanFallToCorpse {
			get {
				//TODO newbie flag etc.
				return true;
			}
		}

		private static TagKey linkTK = TagKey.Get("_link_");
		public virtual Thing Link {
			get {
				return this.GetTag(linkTK) as Thing;
			}
			set {
				if (value == null) {
					this.RemoveTag(linkTK);
				} else {
					this.SetTag(linkTK, value);
				}
			}
		}

		public override AbstractItem NewItem(IThingFactory factory, uint amount) {
			Thing t = factory.Create(this);
			AbstractItem i = t as AbstractItem;
			if (i != null) {
				if (i.IsStackable) {
					i.Amount = amount;
				}
				return i;
			}
			if (t != null) {
				t.Delete();//we created a character, wtf? :)
			}
			throw new SEException(factory + " did not create an item.");
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

		public virtual bool On_SpellEffect(SpellEffectArgs spellEffectArgs) {
			return false;
		}
	}
}