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
using SteamEngine;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	[HasSavedMembers]
	[Summary("Class containing all main crafting categories (according to the crafting skills)." +
			"Each skill has one main category for its items and possible subcategories")]
	public static class CraftmenuContents {
		[SavedMember]
		public static CraftmenuCategory categoryAlchemy = new CraftmenuCategory("Alchemy", null);
		[SavedMember]
		public static CraftmenuCategory categoryBlacksmithing = new CraftmenuCategory("Blacksmithing", null);
		[SavedMember]
		public static CraftmenuCategory categoryBowcraft = new CraftmenuCategory("Bowcraft", null);
		[SavedMember]
		public static CraftmenuCategory categoryCarpentry = new CraftmenuCategory("Carpentry", null);
		[SavedMember]
		public static CraftmenuCategory categoryCooking = new CraftmenuCategory("Cooking", null);
		[SavedMember]
		public static CraftmenuCategory categoryInscription = new CraftmenuCategory("Inscription", null);
		[SavedMember]
		public static CraftmenuCategory categoryTailoring = new CraftmenuCategory("Tailoring", null);
		[SavedMember]
		public static CraftmenuCategory categoryTinkering = new CraftmenuCategory("Tinkering", null);

		[Summary("Get the category path ('cat1->cat2->cat3') and try to find and return the category specified")]
		public static CraftmenuCategory GetCategoryByPath(string path) {
			string[] catChain = path.Split(new string[] { "->" }, StringSplitOptions.None);
			CraftmenuCategory candidateCat = null;
			switch (catChain[0]) {
				case "Alchemy":
					candidateCat = categoryAlchemy;
					break;
				case "Blacksmithing":
					candidateCat = categoryBlacksmithing;
					break;
				case "Bowcraft":
					candidateCat = categoryBowcraft;
					break;
				case "Carpentry":
					candidateCat = categoryCarpentry;
					break;
				case "Cooking":
					candidateCat = categoryCooking;
					break;
				case "Inscription":
					candidateCat = categoryInscription;
					break;
				case "Tailoring":
					candidateCat = categoryTailoring;
					break;
				case "Tinkering":
					candidateCat = categoryTinkering;
					break;
			}
			if (candidateCat != null) {
				if (catChain.Length == 1) {
					return candidateCat;
				}
				for (int i = 1; i < catChain.Length; i++) {
					foreach (ICraftmenuElement elem in candidateCat.contents) {
						if (elem.IsCategory && elem.Name.Equals(catChain[i], StringComparison.InvariantCultureIgnoreCase)) {
							candidateCat = (CraftmenuCategory) elem; //found the correct category in the chain
							break; //continue with another identifier string
						}
					}
				}
				return candidateCat; //last Category found
			}
			return null;//nothing was found which will not occur
		}
	}

	[Summary("One line in the craftmenu - can be either the category of items or the particular item(def) itself")]
	public interface ICraftmenuElement {
		string Name {
			get;
		}

		bool IsCategory {
			get;
		}

		CraftmenuCategory Parent {
			get;
		}

		void Remove();
	}

	[SaveableClass]
	[Summary("Craftmenu category class. This is the entity where all Items from the menu are stored as well " +
			"as it is a container for another subcategories")]
	public class CraftmenuCategory : ICraftmenuElement {
		[SaveableData]
		public string name; //name of the category
		[SaveableData]
		public List<ICraftmenuElement> contents = new List<ICraftmenuElement>(); //itemdefs and subcategorties contained in the category
		[SaveableData]
		public CraftmenuCategory parent = null;

		[LoadingInitializer]
		public CraftmenuCategory() {
		}

		public CraftmenuCategory(string name, CraftmenuCategory parent) {
			this.name = name;
			this.parent = parent;
		}

		[Summary("Get name compound also from the possible parent's name (if any)")]
		public string FullName {
			get {
				if (parent == null) {
					return name;
				} else {
					return parent.FullName + "->" + name;
				}
			}
		}

		#region ICraftmenuElement Members
		public string Name {
			get {
				return name;
			}
		}

		public bool IsCategory {
			get {
				return true;
			}
		}

		public CraftmenuCategory Parent {
			get {
				return parent;
			}
		}

		[Summary("Category cleaning method - clear the contents and remove from the parent")]
		public void Remove() {
			if (parent != null) {
				parent.contents.Remove(this);
			}
			contents.Clear();
		}
		#endregion
	}

	[SaveableClass]
	[Summary("Craftmenu item class. This is the entity representing a single Item in the craftmenu")]
	public class CraftmenuItem : ICraftmenuElement {
		[SaveableData]
		public ItemDef itemDef;
		[SaveableData]
		public CraftmenuCategory parent;

		[LoadingInitializer]
		public CraftmenuItem() {
		}

		public CraftmenuItem(ItemDef itemDef, CraftmenuCategory parent) {
			this.itemDef = itemDef;
			this.parent = parent;
		}

		#region ICraftmenuElement Members
		public string Name {
			get {
				return itemDef.Name;
			}
		}

		public bool IsCategory {
			get {
				return false;
			}
		}

		[Summary("CraftmenuItem must always be in some CraftmenuCategory!")]
		public CraftmenuCategory Parent {
			get {
				return parent;
			}
		}

		[Summary("Remove the item from the parent's list")]
		public void Remove() {
			if (parent != null) {
				parent.contents.Remove(this);
			}
		}
		#endregion
	}

	[Summary("Target for targetting single items or containers containing items to be added to the craftmenu (to the selected category)")]
	public class Targ_Craftmenu : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			self.SysMessage("Zamìø pøedmìt pøípadnì konejner s pøedmìty pro pøidání do craftmenu.");
			base.On_Start(self, parameter);
		}

		protected override bool On_TargonItem(Player self, Item targetted, object parameter) {
			CraftmenuCategory catToPut = (CraftmenuCategory) parameter;

			CraftmenuItem newItem = null;
			if (targetted.IsContainer) {
				int containedCount = 0;
				foreach (Item inner in targetted.EnumShallow()) {
					newItem = new CraftmenuItem((ItemDef) inner.Def, catToPut);//add the contained items
					catToPut.contents.Add(newItem);
					containedCount++;
				}
				if (containedCount == 0) {//nothing was inside, we attempted to add the empty container
					newItem = new CraftmenuItem((ItemDef) targetted.Def, catToPut);//add the container itself
					catToPut.contents.Add(newItem);
				}
			} else {//single item - add it now
				newItem = new CraftmenuItem((ItemDef) targetted.Def, catToPut);
				catToPut.contents.Add(newItem);
			}

			//reopen the dialog on the stored position
			//check the stored last displayed category
			string prevCat = TagMath.SGetTag(self, D_Craftmenu.tkCraftmenuLastpos);
			if (prevCat != null) {
				CraftmenuCategory oldCat = CraftmenuContents.GetCategoryByPath(prevCat);
				if (oldCat != null) {
					self.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs(oldCat));
				} else {
					self.Dialog(SingletonScript<D_CraftmenuCategories>.Instance);
				}
			} else {
				self.Dialog(SingletonScript<D_CraftmenuCategories>.Instance);
			}
			return false;
		}
	}
}