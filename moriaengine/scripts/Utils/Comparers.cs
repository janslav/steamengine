using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamEngine.CompiledScripts.Utils {
	public static class Comparers {
		public static readonly Comparison<TagHolder> TagHolderByNameAsc = (a, b) =>
			String.CompareOrdinal(a == null ? "" : a.Name, b == null ? "" : b.Name);
		public static readonly Comparison<TagHolder> TagHolderByNameDesc = (b, a) =>
			String.CompareOrdinal(a == null ? "" : a.Name, b == null ? "" : b.Name);

		public static readonly Comparison<AbilityDef> AbilityDefByNameAsc = (a, b) =>
			String.CompareOrdinal(a == null ? "" : a.Name, b == null ? "" : b.Name);
		public static readonly Comparison<AbilityDef> AbilityDefByNameDesc = (b, a) =>
			String.CompareOrdinal(a == null ? "" : a.Name, b == null ? "" : b.Name);

		public static readonly Comparison<ScriptHolder> ScriptHolderByNameAsc = (a, b) =>
			String.CompareOrdinal(a == null ? "" : a.Name, b == null ? "" : b.Name);
		public static readonly Comparison<ScriptHolder> ScriptHolderByNameDesc = (b, a) =>
			String.CompareOrdinal(a == null ? "" : a.Name, b == null ? "" : b.Name);

		public static readonly Comparison<GMPageEntry> GMPageBySenderNameAsc = (a, b) =>
			TagHolderByNameDesc(a.sender, b.sender);
		public static readonly Comparison<GMPageEntry> GMPageBySenderNameDesc = (b, a) =>
			TagHolderByNameDesc(a.sender, b.sender);

		public static readonly Comparison<DelayedMsg> MsgBySenderNameAsc = (a, b) =>
			TagHolderByNameDesc(a.sender, b.sender);
		public static readonly Comparison<DelayedMsg> MsgBySenderNameDesc = (b, a) =>
			TagHolderByNameDesc(a.sender, b.sender);


		public static readonly Comparison<AbstractScript> ScriptByDefnameAsc = (a, b) =>
			String.CompareOrdinal(a == null ? "" : a.PrettyDefname, b == null ? "" : b.PrettyDefname);
		public static readonly Comparison<AbstractScript> ScriptByDefnameDesc = (b, a) =>
			String.CompareOrdinal(a == null ? "" : a.PrettyDefname, b == null ? "" : b.PrettyDefname);
	}
}
