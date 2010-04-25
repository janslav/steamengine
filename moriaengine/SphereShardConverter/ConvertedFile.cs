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
using System.Reflection;
using System.IO;
using System.Globalization;
using SteamEngine.Common;

namespace SteamEngine.Converter {
	public class ConvertedFile {
		public readonly string origPath;
		public readonly string convertedPath;
		public readonly List<ConvertedDef> defs = new List<ConvertedDef>();

		public ConvertedFile(string path)
			: base() {
			this.origPath = Path.GetFullPath(path);
			this.convertedPath = GetNewFilename(path);
		}

		private static string GetNewFilename(string fileName) {
			string outFileName = Path.GetFileNameWithoutExtension(fileName);
			string pathPart = Path.GetDirectoryName(fileName);
			if (outFileName.ToLowerInvariant().IndexOf("sphere_d_") == 0) {
				outFileName = outFileName.Substring(9);
			}
			if (outFileName.ToLowerInvariant().IndexOf("sphere") == 0) {
				outFileName = outFileName.Substring(6);
			}
			if (outFileName[0] == '_') {
				outFileName = outFileName.Substring(1);
			}
			return Tools.CombineMultiplePaths(ConverterMain.convertToPath, pathPart, outFileName + ".def");
		}

		public void Flush() {
			Tools.EnsureDirectory(Path.GetDirectoryName(this.convertedPath));
			using (FileStream file = new FileStream(this.convertedPath, FileMode.Create)) {
				StreamWriter writer = new StreamWriter(file);

				writer.WriteLine("//Automatically generated by SteamEngine's converter");

				foreach (ConvertedDef def in this.defs) {
					def.Dump(writer);
				}
				writer.Close();
			}
		}

		public void AddDef(ConvertedDef def) {
			this.defs.Add(def);
		}
	}
}