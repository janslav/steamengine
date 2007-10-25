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
using SteamEngine.Timers;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts.Dialogs {
	[ViewDescriptor(typeof(PluginHolder), "Plugin/Triggergroup Holder")]
	public static class PluginHolderDescriptor {

		[GetMethod("PluginsCount",typeof(int))]
		public static object GetPluginsCount(object target) {
			int counter = 0;
			foreach (object o in ((PluginHolder) target).GetAllPlugins()) {
				counter++;
			}
			return counter;
		}

		[GetMethod("TriggerGroupCount", typeof(int))]
		public static object GetTriggerGroupCount(object target) {
			int counter = 0;
			foreach (object o in ((PluginHolder) target).GetAllTriggerGroups()) {
				counter++;
			}
			return counter;
		}

		[Button("Delete plugins")]
		public static void DeletePlugins(object target) {
			((PluginHolder) target).DeletePlugins();
		}

		[Button("Clear TriggerGroups")]
		public static void ClearTriggerGroups(object target) {
			((PluginHolder) target).ClearTriggerGroups();
		}

	}
}