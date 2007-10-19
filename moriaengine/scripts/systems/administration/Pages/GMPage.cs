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
using SteamEngine.CompiledScripts;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	[HasSavedMembers]
	public static class GMPages {
        [SavedMember]
		public static Hashtable gmPages = new Hashtable();

		[Remark("Returns a copy of the GMPages Hashtable (usable for sorting etc.)")]
		public static Hashtable GmPages {
			get {
				return new Hashtable(gmPages);
			}
		}

		[Remark("Simple 'add' method.")]
		public static void AddNewPage(GMPageEntry newPage) {
			gmPages[newPage.sender] = newPage;
		}

		[Remark("Simple 'remove' method.")]
		public static void DeletePage(GMPageEntry thePage) {
			gmPages.Remove(thePage.sender);
		}

		[Remark("Sorting method: Sorting criteria available are nameUp, accountUp, timeUp")]
		public static ArrayList GetSortedBy(SortingCriteria criterion) {
			ArrayList pages = new ArrayList(gmPages.Values);
			switch (criterion) {
				case SortingCriteria.NameAsc:
					pages.Sort(GMPageNameComparator.instance);
					break;
                case SortingCriteria.NameDesc:
					pages.Sort(GMPageNameComparator.instance);
                    pages.Reverse();
                    break;
                case SortingCriteria.AccountAsc:
					pages.Sort(GMPageAccountComparator.instance);
					break;
                case SortingCriteria.AccountDesc:
                    pages.Sort(GMPageAccountComparator.instance);
                    pages.Reverse();
                    break;                
                case SortingCriteria.TimeAsc:
					pages.Sort(GMPageTimeComparator.instance);
					break;
                case SortingCriteria.TimeDesc:
					pages.Sort(GMPageTimeComparator.instance);
                    pages.Reverse();
                    break;				
				default:
					pages.Sort(GMPageTimeComparator.instance);
					break;
			}
			return pages;
		}

		[Remark("Find all available GMs that are online and notify them of the new page.")]
		private static void NotifyOnlineGMs(AbstractCharacter sender) {
			bool isGMOnline = false;
			foreach (Character plr in Server.AllPlayers) {
				if (plr.IsGM()) {
					plr.SysMessage("Prisla nova page. Pocet nevyrizenych pagi: " + CountUnresolved());
					isGMOnline = true;
				}
			}
			if (isGMOnline) { //inform the sender of online GMs
				sender.SysMessage("Pritomni GM byli informovani o tve page.");
			} else {
				sender.SysMessage("V tuto chvili neni pritomen zadny GM.");
			}
		}

		[Remark("Return the number of unresolved pages  - that means count all pages that have not been replied (or deleted :) )")]
		public static int CountUnresolved() {
			int counter = 0;
			foreach (GMPageEntry page in gmPages.Values) {
				if (!page.replied) {
					counter++;
				}
			}
			return counter;
		}

		[SteamFunction]
		[Remark("Create a new gmpage, store it in the gm pages table and notify the sender about the state")]
		public static void Page(AbstractCharacter sender, ScriptArgs text) {
			if (text == null || text.Args.Equals("")) {
				sender.SysMessage("Odmitnuta prazdna page");
				return;
			}
			GMPageEntry newPage = new GMPageEntry(sender, text.Args);
			AddNewPage(newPage);
			sender.SysMessage("GMPage byla uspesne prijata a zarazena na " + CountUnresolved() + ". misto v seznamu");
			NotifyOnlineGMs(sender);
		}
	}

	[SaveableClass]
	public class GMPageEntry {
		[LoadingInitializer]
		public GMPageEntry() {
		}

		[SaveableData]
		public AbstractCharacter sender; //player who has sent the page
		[SaveableData]
		public AbstractCharacter handler; //GM who has solved the page (if any)
		[SaveableData]
		public string reason; //the players description of the problem
		[SaveableData]
		public string reply; //the GM's reply to the problem
		[SaveableData]
		public bool replied; //has any GM replied to this page?
		[SaveableData]
		public Point4D p; //location where the page was posted
		[SaveableData]
		public DateTime time; //time when the page was posted

		public GMPageEntry(AbstractCharacter sender, string reason) {
			this.replied = false;
			this.handler = null;
			this.sender = sender;
			this.reason = reason;
			this.reply = "";
			this.time = DateTime.Now;
			if (sender != null) {
				this.p = sender.P();
			}
		}

		public GMPageEntry(string reason)
			: this(Globals.SrcCharacter, reason) {
		}
	}

	[Remark("Comparator serving for sorting the list of pages by name of their author")]
	class GMPageNameComparator : IComparer {
		public static GMPageNameComparator instance = new GMPageNameComparator();

		private GMPageNameComparator() {
		}

		public int Compare(object a, object b) {
			string name1 = ((GMPageEntry) a).sender.Name;
			string name2 = ((GMPageEntry) b).sender.Name;

			return name1.CompareTo(name2);
		}
	}

	[Remark("Comparator serving for sorting the list of pages by name their creation time")]
	class GMPageTimeComparator : IComparer {
		public static GMPageTimeComparator instance = new GMPageTimeComparator();

		private GMPageTimeComparator() {
		}

		public int Compare(object a, object b) {
			DateTime time1 = ((GMPageEntry) a).time;
			DateTime time2 = ((GMPageEntry) b).time;

			return time1.CompareTo(time2);
		}
	}

	[Remark("Comparator serving for sorting the list of pages by teir creators account name")]
	class GMPageAccountComparator : IComparer {
		public static GMPageAccountComparator instance = new GMPageAccountComparator();

		private GMPageAccountComparator() {
		}

		public int Compare(object a, object b) {
			string acc1 = ((GMPageEntry) a).sender.Account.Name;
			string acc2 = ((GMPageEntry) b).sender.Account.Name;

			return acc1.CompareTo(acc2);
		}
	}
}