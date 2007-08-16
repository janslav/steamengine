using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	[Summary("Methods for simulating of sphereserver API in some cases")]
	public static class LegacyUtils {
		[SteamFunction]
		public static void AddEvent(ITriggerGroupHolder self, TriggerGroup tg) {
			self.AddTriggerGroup(tg);
		}

		[SteamFunction]
		public static void RemoveEvent(ITriggerGroupHolder self, TriggerGroup tg) {
			self.RemoveTriggerGroup(tg);
		}

		[SteamFunction]
		public static bool HasEvent(ITriggerGroupHolder self, TriggerGroup tg) {
			return self.HasTriggerGroup(tg);
		}

		[SteamFunction]
		public static void Events(ITriggerGroupHolder self, ScriptArgs sa) {
			if (sa != null) {
				object[] argv = sa.Argv;
				if (argv.Length > 0) {
					object firstArg = argv[0];
					TriggerGroup tg = firstArg as TriggerGroup;
					if (tg != null) {
						Events(self, tg);
						return;
					} else {
						TGRemover tgr = firstArg as TGRemover;
						if (tgr != null) {
							Events(self, tgr);
							return;
						} else {
							int i = Convert.ToInt32(firstArg);
							Events(self, i);
							return;
						}
					}
				}
			}
			Events(self);
		}

		public static void Events(ITriggerGroupHolder self, TriggerGroup tg) {//applies to spherescript-like "events(+e_blah)"
			self.AddTriggerGroup(tg);
		}


		public static void Events(ITriggerGroupHolder self, TGRemover remover) {
			self.RemoveTriggerGroup(remover.tg);
		}

		public static void Events(ITriggerGroupHolder self, int i) {
			if (i == 0) {
				self.ClearTriggerGroups();
			}
		}

		public static string Events(ITriggerGroupHolder self) {
			StringBuilder toreturn= new StringBuilder();
			foreach (TriggerGroup tg in self.AllTriggerGroups) {
				toreturn.Append(tg.ToString()).Append(", ");

			}
			if (toreturn.Length > 2) {
				toreturn.Length -= 2;
			}
			return toreturn.ToString();
		}
	}
}