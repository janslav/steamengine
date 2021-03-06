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
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>Resource as characters Strength</summary>
	public class StatStrResource : AbstractResourceListEntry, IResourceListEntry_Simple {

		public static void Bootstrap() {
			ResourcesListParser.RegisterResourceParser(TryParse);
		}

		/// <summary>Try parsing given string as ItemResource</summary>
		public static bool TryParse(string definition, double number, bool asPercentage, out IResourceListEntry resource) {
			if ("Str".Equals(definition, StringComparison.OrdinalIgnoreCase) ||
					"Strength".Equals(definition, StringComparison.OrdinalIgnoreCase)) {
				resource = new StatStrResource(number, asPercentage);
				return true;
			}
			resource = null;
			return false;
		}

		private StatStrResource(double number, bool asPercentage)
			: base(number, asPercentage) {

			if (asPercentage) {
				throw new SEException("Cannot use strength as percentage resource");
			}
		}

		/// <summary>String from which the resource could be parsed again, used for saving</summary>
		public override string ParsableString {
			get {
				return "str";
			}
		}

		public override bool IsSameResource(IResourceListEntry newOne) {
			return (newOne is StatStrResource);
		}

		public override string GetResourceMissingMessage(Language language) {
			return Loc<ResListLoc>.Get(language).StrTooLow;
		}

		#region IResourceListEntry_Simple Members
		public bool IsResourcePresent(Character chr) {
			return chr.Str >= this.DesiredCount;
		}

		/// <summary>Indicates whether this is a consumable resource</summary>
		public bool IsConsumable { get { return false; } }

		/// <summary>Consumes this resource. Throws if this is not a consumable resource.</summary>
		public void Consume(Character ch) {
			throw new InvalidOperationException("Strength can not be consumed as a resource.");
		}
		#endregion
	}
}