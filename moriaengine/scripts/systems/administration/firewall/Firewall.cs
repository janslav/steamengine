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
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using SteamEngine.Timers;
using SteamEngine.Common;
using System.Net;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using SteamEngine;
using SteamEngine.Packets;
using SteamEngine.Persistence;

//using ICSharpCode.SharpZipLib.Zip;
//using OrganicBit.Zip;

namespace SteamEngine.CompiledScripts {
    public class E_Firewall_Global : CompiledTriggerGroup {
        public void On_ClientAttach(Globals ignored, ScriptArgs sa) {
            GameConn conn = (GameConn)sa.Argv[0];
            if (Firewall.IsBlockedIP(conn.IP)) {
				Prepared.SendFailedLogin(conn, FailedLoginReason.Blocked);
                conn.Close("IP Blocked");
            }
        }
    }

    [HasSavedMembers]
    public static class Firewall {
        [SavedMember]
        private static Hashtable blockedIPEntries = new Hashtable();
        [SavedMember]
        private static ArrayList blockedIPRangeEntries = new ArrayList();

        [Remark("Various comparators")]
        private static IPComparator ipComparator = new IPComparator();
        private static AccountComparator accComparator = new AccountComparator();

        [Remark("Returns a copy of the BlockedIPEntry Hashtable (usable for sorting etc.)")]
        public static Hashtable BlockedIPEntries {
            get {
                return new Hashtable(blockedIPEntries);
            }
        }

        [Remark("Returns a copy of the BlockedIPRangeEntry Hashtable (usable for sorting etc.)")]
        public static ArrayList BlockedIPRangeEntries {
            get {
                return new ArrayList (blockedIPRangeEntries);
            }
        }

        [Remark("Sorting method: Sorting parameters available are ip, account")]
        public static ArrayList GetSortedBy(SortingCriteria criterion) {
            ArrayList ipentries = new ArrayList(blockedIPEntries.Values);
            ipentries.AddRange(blockedIPRangeEntries);

            switch (criterion) {
                case SortingCriteria.IPAsc:
                    ipentries.Sort(ipComparator);
                    break;

                case SortingCriteria.AccountAsc:
                    ipentries.Sort(accComparator);
                    break;

                case SortingCriteria.IPDesc:
                    ipentries.Sort(ipComparator);
                    ipentries.Reverse();
                    break;

                case SortingCriteria.AccountDesc:
                    ipentries.Sort(accComparator);
                    ipentries.Reverse();
                    break;

                default:
                    ipentries.Sort(ipComparator);
                    break;
            }

            return ipentries;
        }




        [Remark("Comparator serving for sorting the list of Blockedipentries by blocked by")]
        class AccountComparator : IComparer {
            public int Compare(object a, object b) {
                string acc1 = ((ISortableIpBlockEntry)a).Account.Name;
                string acc2 = ((ISortableIpBlockEntry)b).Account.Name;

                return acc1.CompareTo(acc2);
            }
        }

        [Remark("Comparator serving for sorting the list of Blockedipentries by IP")]
        class IPComparator : IComparer {
            public int Compare(object a, object b) {
                IPAddress ip1 = ((ISortableIpBlockEntry)a).Ip;
                IPAddress ip2 = ((ISortableIpBlockEntry)b).Ip;

                if ((IPAddress.NetworkToHostOrder(getLongFromAddress(ip1))) < (IPAddress.NetworkToHostOrder(getLongFromAddress(ip2)))) {
                    return -1;
                }
                if ((IPAddress.NetworkToHostOrder(getLongFromAddress(ip1))) > (IPAddress.NetworkToHostOrder(getLongFromAddress(ip2)))) {
                    return 1;
                }
                else return 0;
            }
        }

        public static void AddBlockedIP(IPAddress IP, String reason, GameAccount who) {
            if (IsBlockedIP(IP) == true) {
                Globals.SrcWriteLine("The IP " + IP + " is already blocked.");
                return;
            }
            BlockedIPEntry ipbe = new BlockedIPEntry();
            ipbe.ip = IP;
            ipbe.reason = reason;
            ipbe.blockedBy = who;
            blockedIPEntries[IP] = ipbe;
            Globals.SrcWriteLine("The IP " + IP + " was blocked.");

        }
        public static void AddBlockedIP(String IP, String reason, GameAccount who) {
            IPAddress address;
            if (IPAddress.TryParse(IP, out address)) {
                Firewall.AddBlockedIP(address, reason, who);
            }
            else {
                Globals.SrcWriteLine("voe!(Tartaros) Zadal jsi uplne blbou adresu.");
            }


            /*if (IsBlockedIP(IP) == true) {
                Globals.SrcWriteLine("The IP " + IP + " is already blocked.");
                return;
            }
            BlockedIPEntry ipbe = new BlockedIPEntry();
            ipbe.ip = IPAddress.Parse(IP);
            ipbe.reason = reason;
            ipbe.blockedBy = who;
            blockedIPEntries[IPAddress.Parse(IP)] = ipbe;
            Globals.SrcWriteLine("The IP " + IP + " was blocked."); */


        }

        public static void RemoveBlockedIP(String IP) {
            Firewall.RemoveBlockedIP(IPAddress.Parse(IP));

            /*if (IsBlockedIP(IP) == false) {
                return;
            }
            blockedIPEntries.Remove(IPAddress.Parse(IP)); */
        }

        public static void RemoveBlockedIP(IPAddress IP) {
            //Firewall.RemoveBlockedIP(IP.ToString());
            if (IsBlockedIP(IP) == false) {
                return;
            }
            blockedIPEntries.Remove(IP);
            Globals.SrcWriteLine("The IP " + IP + " was unblocked.");
        }

        public static bool IsBlockedIP(IPAddress IP) {
            BlockedIPEntry lookEntry = (BlockedIPEntry)blockedIPEntries[IP];

            foreach (BlockedIPRangeEntry bire in blockedIPRangeEntries) {
                if (IsIPInRange(IP, bire.fromIp, bire.toIp) == true) {
                    return true;
                }
            }
            if (lookEntry == null) {
                return false;
            }
            else {
                return true;
            };
        }


        public static bool IsBlockedIP(String IP) {
            return Firewall.IsBlockedIP(IPAddress.Parse(IP));
            /*BlockedIPEntry lookEntry = (BlockedIPEntry) blockedIPEntries[IPAddress.Parse(IP)];

            foreach (BlockedIPRangeEntry bire in blockedIPRangeEntries.Values) {
                if (IsIPInRange(IPAddress.Parse(IP), bire.fromIp, bire.toIp) == true) {
                    return true;
                }
            }

            if (lookEntry == null) {
                return false;
            } else return true; */
        }

        public static void ShowBlockedIP() {
            foreach (BlockedIPEntry bie in blockedIPEntries.Values) {
                Globals.SrcWriteLine("Blocked IP: " + bie.ip + " Reason: " + bie.reason + " Blocked by: " + bie.blockedBy);
            }
            foreach (BlockedIPRangeEntry bire in blockedIPRangeEntries) {
                Globals.SrcWriteLine("Blocked IPRange from: " + bire.fromIp + " to: " + bire.toIp + " Reason: " + bire.reason + " Blocked by: " + bire.blockedBy);
            }


        }
        public static void AddBlockedIPRange(String fromIP, String toIP, String reason, GameAccount who) {
            IPAddress address1;
            IPAddress address2;
            if (IPAddress.TryParse(fromIP, out address1) && IPAddress.TryParse(toIP, out address2)) {
                Firewall.AddBlockedIPRange(IPAddress.Parse(fromIP), IPAddress.Parse(toIP), reason, who);
            }
            else {
                Globals.SrcWriteLine("voe!(Tartaros) Zadal jsi uplne blbou adresu.");
            }
            /* BlockedIPRangeEntry iprbe = new BlockedIPRangeEntry();
			iprbe.fromIp = IPAddress.Parse(fromIP);
			iprbe.toIp = IPAddress.Parse(toIP);
			iprbe.reason = reason;
			iprbe.blockedBy = who;
			blockedIPRangeEntries[IPAddress.Parse(fromIP)] = iprbe;
            Globals.SrcWriteLine("Blocked IPRange from: " + iprbe.fromIp + " to: " + iprbe.toIp);
            */
        }

        public static void AddBlockedIPRange(IPAddress fromIP, IPAddress toIP, String reason, GameAccount who) {
            //Firewall.AddBlockedIPRange(fromIP.ToString(), toIP.ToString() , reason, who);
            BlockedIPRangeEntry iprbe = new BlockedIPRangeEntry();
            iprbe.fromIp = fromIP;
            iprbe.toIp = toIP;
            iprbe.reason = reason;
            iprbe.blockedBy = who;
            blockedIPRangeEntries.Add(iprbe);
        }

        public static void RemoveBlockedIPRange(IPAddress fromIP, IPAddress toIP) {
            foreach (BlockedIPRangeEntry bipe in BlockedIPRangeEntries) {
				if ((bipe.fromIp.Equals(fromIP))&&(bipe.toIp.Equals(toIP))) {
					blockedIPRangeEntries.Remove(bipe);
					Globals.SrcWriteLine("IPRange: " + bipe.fromIp + "-" + bipe.toIp + " was unblocked.");
					return;
				}
            }
            Globals.SrcWriteLine("IPRange nenalezena") ;          
        }
		public static void RemoveBlockedIPRange(String fromIP, String toIP) {
			Firewall.RemoveBlockedIPRange(IPAddress.Parse(fromIP), IPAddress.Parse(toIP));
		}
		/*public static void RemoveBlockedIPRange(String IP) {
            Firewall.RemoveBlockedIPRange(IPAddress.Parse(IP));
            /*if (IsBlockedIP(IP) == false) {
				return;
			}
			blockedIPRangeEntries.Remove(IPAddress.Parse(IP));
        } */

        public static bool IsIPInRange(IPAddress ipair, IPAddress iplo, IPAddress iphi) {
            long addr = IPAddress.NetworkToHostOrder(getLongFromAddress(ipair));
            return ((addr >= IPAddress.NetworkToHostOrder(getLongFromAddress(iplo))) && (addr <= IPAddress.NetworkToHostOrder(getLongFromAddress(iphi))));
        }

        private static long getLongFromAddress(IPAddress ipa) {
            byte[] b = ipa.GetAddressBytes();
            long l = 0;
            for (int i = 0; i < b.Length; i++) {
                l += (long)(b[i] * Math.Pow(256, i));
            }
            return l;
        }
    }

    [SaveableClass]
    public class BlockedIPEntry : ISortableIpBlockEntry {
        [LoadingInitializer]
        public BlockedIPEntry() {
        }
        [SaveableData]
        public IPAddress ip;
        [SaveableData]
        public String reason;
        [SaveableData]
        public GameAccount blockedBy;

        IPAddress ISortableIpBlockEntry.Ip {
            get { return ip; }
        }
        String ISortableIpBlockEntry.toIp {
            get { return ""; }
        }
        String ISortableIpBlockEntry.Reason {
            get { return reason; }
        }
        GameAccount ISortableIpBlockEntry.Account {
            get { return blockedBy; }
        }
    }

    [SaveableClass]
    public class BlockedIPRangeEntry : ISortableIpBlockEntry {
        [LoadingInitializer]
        public BlockedIPRangeEntry() {
        }

        [SaveableData]
        public IPAddress fromIp;
        [SaveableData]
        public IPAddress toIp;
        [SaveableData]
        public String reason;
        [SaveableData]
        public GameAccount blockedBy;

        IPAddress ISortableIpBlockEntry.Ip {
            get { return fromIp; }
        }
        String ISortableIpBlockEntry.toIp {
            get { return toIp.ToString(); }
        }
        String ISortableIpBlockEntry.Reason {
            get { return reason; }
        }
        GameAccount ISortableIpBlockEntry.Account {
            get { return blockedBy; }
        }
    }


    public interface ISortableIpBlockEntry {
        IPAddress Ip { get; }
        String toIp { get; }
        String Reason { get; }
        GameAccount Account { get; }

    }

    public class IPAddressSaveImplementor : ISimpleSaveImplementor {
        public static Regex re = new Regex(@"^\(IP\)(?<value>.+)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public Type HandledType {
            get {
                return typeof(IPAddress);
            }
        }

        public Regex LineRecognizer {
            get {
                return re;
            }
        }

        public string Save(object objToSave) {
            return "(IP)" + objToSave;
        }

        public object Load(Match match) {
            return IPAddress.Parse(match.Groups["value"].Value);
        }

    }
}