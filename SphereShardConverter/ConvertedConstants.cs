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
using System.Reflection;
using System.IO;
using System.Globalization;
using SteamEngine.Common;
using System.Configuration;
using System.Text.RegularExpressions;

namespace SteamEngine.Converter {
	public class ConvertedConstants : ConvertedDef {
		public ConvertedConstants(PropsSection input) : base(input) {
			headerType = "Constants";
		}

		public override void FirstStage() {
			base.FirstStage ();
			string line;
			StringReader reader = new StringReader(origData.GetTrigger(0).code.ToString());
			while ((line = reader.ReadLine()) != null) {
				writtenData.Add(ConvertedTemplateDef.FixRandomExpression(line));
			}
		}
	}
}