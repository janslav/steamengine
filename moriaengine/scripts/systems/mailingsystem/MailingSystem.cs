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
using SteamEngine;
using SteamEngine.Common;
using SteamEngine.CompiledScripts;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	[Summary("Static class for operating with clients delayed messages.")]
	public static class MsgsBoard {		
		[Summary("A unique tag name for holding a list of client delayed messages")]
		internal static TagKey _delayed_msgs_ = TagKey.Get("_delayed_msgs_");

		[Summary("Default senders name (if the sender was not specified)")]
		public const string NO_SENDER = "System";

		[Summary("Various comparators")]
		private static MsgsSenderComparator senderComparator = new MsgsSenderComparator();
		private static MsgsTimeComparator timeComparator = new MsgsTimeComparator();
		private static MsgsUnreadComparator unreadComparator = new MsgsUnreadComparator();


		[Summary("Returns a copy of the list of clients delayed messages (for sorting e.g.)")]
		public static ArrayList GetClientsMessages(Character whose) {
			//get the client messages (empty list if no messages present)
			ArrayList list = GetMessages(whose);
			//if the list is empty, return it now, else make a copy of the messages list
			if(list.Count == 0) {
				return list;
			} else {
				return new ArrayList(list);
			}
		}

		[Summary("Simple 'add' method.")]
		public static void AddNewMessage(Character whom, DelayedMsg newMsg) {
			GetMessages(whom).Add(newMsg);
		}

		[Summary("Simple 'remove' method.")]
		public static void DeleteMessage(Character whose, DelayedMsg newMsg) {
			GetMessages(whose).Remove(newMsg);
		}

		[Summary("Private utility method returning the characters list of messages")]
		private static ArrayList GetMessages(Character whose) {
			ArrayList retList = (ArrayList)whose.GetTag(_delayed_msgs_);
			if (retList == null) { // no messages previously posted
				retList = new ArrayList();
				whose.SetTag(_delayed_msgs_, retList);
			}
			return retList;
		}

		[Summary("Sorting method used for Character: Sorting parameters available are senders name, time and read/unread messages first")]
		public static ArrayList GetSortedBy(Character whom, SortingCriteria criterion) {
			//get a new copy of the list and sort it using another version of this method			
			return GetSortedBy(GetClientsMessages(whom), criterion);
		}

		[Summary("Sorting method when the list is obtained first: Sorting parameters available are senders name, time and read/unread messages first")]
		public static ArrayList GetSortedBy(ArrayList messages, SortingCriteria criterion) {
			//get a new copy of the list			
			switch(criterion) {
				case SortingCriteria.NameAsc: //order by sender alphabetically
					messages.Sort(senderComparator);
					break;
				case SortingCriteria.NameDesc: //order by sender alphabetically
					messages.Sort(senderComparator);
					messages.Reverse();
					break;
				case SortingCriteria.TimeAsc: //order by time
					messages.Sort(timeComparator);
					break;
				case SortingCriteria.TimeDesc: //order by time
					messages.Sort(timeComparator);
					messages.Reverse();
					break;
				case SortingCriteria.UnreadAsc: //order unread messages first
					messages.Sort(unreadComparator);
					break;
				case SortingCriteria.UnreadDesc: //order unread messages first
					messages.Sort(unreadComparator);
					messages.Reverse();
					break;
				default: //defaultly sort by time
					messages.Sort(timeComparator);
					break;
			}
			return messages;
		}

		[Summary("Return the number of unread messages (those the recipient has not opened by "+
                "'read/display detail' button")]
		public static int CountUnread(ArrayList msgs) {
			int counter = 0;
			foreach (DelayedMsg msg in msgs) {
				if (!msg.read) {
					counter++;
				}
			}
			return counter;
		}

		[Summary("Return the number of unread messages for the specified player")]
		public static int CountUnread(Character whose) {
			return CountUnread(GetMessages(whose));
		}
	}

	[Summary("The object of the clients delayed message. Holds the info about the sender, sending time and "+
            "the message body")]
	[SaveableClass]
	public class DelayedMsg {
		[LoadingInitializer]
		public DelayedMsg() {
		}

		[SaveableData]
		public AbstractCharacter sender; //player/GM who has sent the page or null if the sender was the system
		[SaveableData]
		public string text; //the text of the message
		[SaveableData]
		public DateTime time; //time when the message was posted
		[SaveableData]
		public bool read; //was the message read by recipient or not ?
		[SaveableData]
		public Hues color; //messages color


		[Summary("No sender - the message was created by server probably.")]
		public DelayedMsg(string text) {
			this.sender = null;
			this.text = text;
			this.time = DateTime.Now;
			this.color = Hues.WriteColor; //default
			this.read = false;
		}

		public DelayedMsg(AbstractCharacter sender, string text)
			: this(text) {
			this.sender = sender;
		}

		[Summary("Allows us to specify if the message should be displayed as red")]
		public DelayedMsg(string text, bool redMessage)
			: this(text) {
			this.color = Hues.Red;
		}

		public DelayedMsg(AbstractCharacter sender, string text, bool redMessage)
			: this(text, redMessage) {
			this.sender = sender;
		}

		[Summary("Allows uas to specify the messages color also...")]
		public DelayedMsg(AbstractCharacter sender, string text, Hues hue)
			: this(sender, text) {
			this.color = hue;
		}
	}

	[Summary("Comparator serving for sorting the list of messages by sender")]
	class MsgsSenderComparator : IComparer {
		public int Compare(object a, object b) {
			DelayedMsg msg1 = (DelayedMsg) a;
			DelayedMsg msg2 = (DelayedMsg) b;
			string name1 = "";
			string name2 = "";
			//if sender was not specified, consider the message as from "System"
			if (msg1.sender == null) {
				name1 = MsgsBoard.NO_SENDER;
			} else {
				name1 = msg1.sender.Name;
			}

			if (msg2.sender == null) {
				name2 = MsgsBoard.NO_SENDER;
			} else {
				name2 = msg2.sender.Name;
			}

			return name1.CompareTo(name2);
		}
	}

	[Summary("Comparator serving for sorting the list of messages by their creation time")]
	class MsgsTimeComparator : IComparer {
		public int Compare(object a, object b) {
			DateTime time1 = ((DelayedMsg) a).time;
			DateTime time2 = ((DelayedMsg) b).time;

			return time1.CompareTo(time2);
		}
	}

	[Summary("Comparator serving for sorting the list of messages by their unread/status")]
	class MsgsUnreadComparator : IComparer {
		public int Compare(object a, object b) {
			bool read1 = ((DelayedMsg) a).read;
			bool read2 = ((DelayedMsg) b).read;

			return read1.CompareTo(read2);
		}
	}
}