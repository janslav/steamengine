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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Globalization;
using System.Text.RegularExpressions;
using SteamEngine;
using SteamEngine.Timers;
using SteamEngine.Common;

namespace SteamEngine {
	public class TagMath : ConvertTools {
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		private TagMath instance = new TagMath();

		protected TagMath()
			: base() {
		}

		protected override bool ToBoolImpl(object arg) {
			IDeletable deletable = arg as IDeletable;
			if (deletable != null) {
				return !deletable.IsDeleted;
			} else {
				return base.ToBoolImpl(arg);
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), Summary("Try to obtain a string tag value - not 'toString' but regular string instance")]
		public static string SGetTag(TagHolder from, TagKey which) {
			object tagValue = from.GetTag(which);
			if (tagValue == null) {
				return null; //return null
			}

			IConvertible convertibleVal = tagValue as IConvertible;
			if (convertibleVal != null) {
				return convertibleVal.ToString(CultureInfo.InvariantCulture);
			}
			IFormattable formattableVal = tagValue as IFormattable;
			if (formattableVal != null) {
				return formattableVal.ToString(null, CultureInfo.InvariantCulture);
			}
			//not available to transform to string (we dont want the ToString only!)
			throw new SEException("Unexpected conversion attempt: " + Tools.TypeToString(tagValue.GetType()) + " -> String");
		}

		[Summary("Try to obtain a string tag value - not 'toString' but regular string instance")]
		public static string SGetTagNotNull(TagHolder from, TagKey which) {
			string retVal = SGetTag(from, which);
			if (retVal == null) {
				return String.Empty; //return "" instead of null
			}
			return retVal;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), Summary("Try to obtain a int32 (int) tag value. Return 0 if no tag is found. Not using (int) cast " +
				"so we are able to accept a non 'int' numbers such as uints, shorts etc.")]
		public static int IGetTag(TagHolder from, TagKey which) {
			return ConvertTools.ToInt32(from.GetTag(which));
		}

		public static bool Is1(object o) {
			int i;
			if (ConvertTools.TryConvertToInt32(o, out i)) {
				if (i == 1) {
					return true;
				}
			}
			return false;
		}
		
		public static string TimeSpanToString(TimeSpan ts, string format) {
			DateTime dt = DateTime.MinValue.Add(ts);
			return dt.ToString(format);
		}

		public static string TimeSpanToSimpleString(TimeSpan ts) {
			return TimeSpanToString(ts, "H:mm:ss");
		}
	}
}