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
using SteamEngine.Common;
using SteamEngine.CompiledScripts;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts.Dialogs {
	[Summary("Extend this class in order to obtain a new Category to the settings dialog")]
	public abstract class SettingsMetaCategory {
	}

	[Summary("Viewable class representing the Settings Categories")]
	[ViewableClass("Settings Categories")]
	public class SettingsCategories : SettingsMetaCategory {
		/*The class or its subclasses can contain properties/fields which will be 
		 *like the normal values for setting, but it can also contain references to other
		 *SettingsCategories which will there serve as a subcategory*/

		[NoShow]//dont show this :)
		public static SettingsCategories instance = new SettingsCategories();

		//Setting categories: - Database Settings
		[InfoField("Database Settings")]
		public DbConfig DbConfig {
			get {
				return DbManager.Config;
			}
		}

		//Setting categories: - Combat Settings
		[InfoField("Combat Settings")]
		public CombatSettings CombatSettings {
			get {
				return CombatSettings.instance;
			}
		}

		[InfoField("Magery Settings")]
		public MagerySettings MagerySettings {
			get {
				return MagerySettings.instance;
			}
		}
	}
}