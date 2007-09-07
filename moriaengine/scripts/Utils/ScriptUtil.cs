using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	public static class ScriptUtil {
		public static ArrayList ArrayListFromEnumerable(IEnumerable enumerable) {
			return ArrayListFromEnumerator(enumerable.GetEnumerator());
		}

		public static ArrayList ArrayListFromEnumerator(IEnumerator enumerator) {
			ArrayList list = new ArrayList();
			while (enumerator.MoveNext()) {
				list.Add(enumerator.Current);
			}
			return list;
		}

		public static string GetLogString(GameConn conn, string message) {
			AbstractAccount acc = conn.Account;
			AbstractCharacter ch = conn.CurCharacter;
			if (acc != null) {
				if (ch != null) {
					return string.Concat(
						"Acc '", acc.Name, "', char '", ch.Name, "' (#", ch.Uid.ToString("x"), "): "+message);
				} else {
					return string.Concat(
						"Acc '", acc.Name, "': "+message);
				}
			} else {
				return string.Concat(
					"Client ", conn.uid, ": "+message);
			}
		}

		public static string GetLogString(Conn conn, string message) {
			AbstractAccount acc = conn.Account;
			if (acc != null) {
				return string.Concat(
					"Acc '", acc.Name, "': "+message);
			} else {
				return string.Concat(
					"Client ", conn.uid, ": "+message);
			}
		}
	}
}