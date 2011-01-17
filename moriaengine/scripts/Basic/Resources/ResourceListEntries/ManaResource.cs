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
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	[Summary("Resource as characters Mana")]
	public class StatManaResource : AbstractResourceListEntry, IResourceListEntry_Simple {

		public static void Bootstrap() {
			ResourcesListParser.RegisterResourceParser(TryParse);
		}

		[Summary("Try parsing given string as ItemResource")]
		public static bool TryParse(string definition, double number, bool asPercentage, out IResourceListEntry resource) {
			if ("Mana".Equals(definition, StringComparison.OrdinalIgnoreCase)) {
				resource = new StatManaResource(number, asPercentage);
				return true;
			}
			resource = null;
			return false;
		}

		private StatManaResource(double number, bool asPercentage)
			: base(number, asPercentage) {

			if (asPercentage) {
				throw new SEException("Cannot use skill as percentage resource");
			}
		}

		[Summary("String from which the resource could be parsed again, used for saving")]
		public override string ParsableString {
			get {
				return "Mana";
			}
		}

		public override bool IsSameResource(IResourceListEntry newOne) {
			return (newOne is StatManaResource);
		}

		public override string GetResourceMissingMessage(Language language) {
			return Loc<ResListLoc>.Get(language).ManaTooLow;
		}

		#region IResourceListEntry_Simple Members
		public bool IsResourcePresent(Character chr) {
			if (this.AsPercentage) {
				return (chr.Mana * 100.0 / chr.MaxMana) >= this.DesiredCount;
			} else {
				return chr.Mana >= this.DesiredCount;
			}
		}

		[Summary("Indicates whether this is a consumable resource")]
		public bool IsConsumable { get { return true; } }

		[Summary("Consumes this resource. Throws if this is not a consumable resource.")]
		public void Consume(Character ch) {
			if (this.AsPercentage) {
				ch.AddMana(-(int) Math.Round(ch.Mana * this.DesiredCount / ch.MaxMana));
			} else {
				ch.AddMana(-(int) Math.Round(this.DesiredCount));
			}
		}
		#endregion
	}
}