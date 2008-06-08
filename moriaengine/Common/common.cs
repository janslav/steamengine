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

using System.IO;
using System;
using System.Reflection;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;


namespace SteamEngine.Common {
	public static class Tools {

		public readonly static string commonPipeName = @"\\.\pipe\steamAuxPipe";

		public static void ExitBinDirectory() {
			//string cmdLine = Environment.CommandLine.Replace('"', ' ').Trim();
			//string path=Path.GetDirectoryName(Path.Combine(Environment.CurrentDirectory, cmdLine));//Directory.GetCurrentDirectory();
			string path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			if (path.ToLower().EndsWith("bin")) {
				Directory.SetCurrentDirectory(Path.GetDirectoryName(path));
			} else {
				Directory.SetCurrentDirectory(path);
			}
		}
		
		public static string CombineMultiplePaths(params string[] paths) {
			if ((paths == null) || (paths.Length == 0)) {
				return "";
			} else if (paths.Length == 1) {
				return paths[0];
			}
			string retVal = paths[0];
			for (int i = 1, n = paths.Length; i<n; i++) {
				retVal = Path.Combine(retVal, paths[i]);
			}
			return retVal;
		}
		
		private static char[] separators = new char[] {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar};
		public static string[] SplitPath(string path) {
			if (path == null) {
				return new string[0];
			}
			string[] dirs = path.Split(separators);
			if (dirs[0].EndsWith(Path.VolumeSeparatorChar.ToString())) {
				dirs[0] = dirs[0]+Path.DirectorySeparatorChar;
			}

			//VolumeSeparatorChar
			return dirs;
		}
		
		public static void EnsureDirectory(string path) {
			EnsureDirectory(path, false);
		}
		
		public static void EnsureDirectory(string path, bool announce) {
			if (!Directory.Exists(path)) {
				string[] split = SplitPath(path);
				if (split.Length > 0) {
					string curDir = "";
					for (int i = 0, n = split.Length; i<n; i++) {
						curDir = Path.Combine(curDir, split[i]);
						if (!Directory.Exists(curDir)) {
							if (announce) {
								Console.WriteLine("Creating directory "+LogStr.Ident(curDir));
							}
							Directory.CreateDirectory(curDir);
						}
					}
				}
			}
		}
		
		public static string ObjToString(object obj) {
			//this is primarily to show entire arraylists, 
			//but it can once show more (.NET hardcoded) types
			StringBuilder toreturn= new StringBuilder();
			if (obj==null) {
				toreturn.Append("null");
			} else if (obj is IDictionary) {
				IDictionary ht = obj as IDictionary;
				toreturn.Append("{");
				foreach (DictionaryEntry entry in ht) {
					toreturn.Append(ObjToString(entry.Key));
					toreturn.Append(" : ");
					toreturn.Append(ObjToString(entry.Value));
					toreturn.Append(", ");
				}
				toreturn.Append("}");
			} else if (obj is Array) {
				Array arr= (Array) obj;
				toreturn.Append("(");
				for (int i = 0; i < arr.Length; i++) {
					toreturn.Append(ObjToString(arr.GetValue(i)));
					toreturn.Append(", ");
				}
				toreturn.Append(")");
			} else if (obj is IList) {
				IList arr=obj as IList;
				toreturn.Append("[");
				for (int i = 0; i < arr.Count; i++) {
					toreturn.Append(ObjToString(arr[i]));
					toreturn.Append(", ");
				}
				toreturn.Append("]");
			} else if (obj is Enum) {
				toreturn.Append("'");
				toreturn.Append(obj.ToString());
				toreturn.Append("'");
			} else {
				toreturn.Append("'");
				toreturn.Append(obj.ToString());
				toreturn.Append("'");
			}
			return toreturn.ToString();
		}


		public static Byte[] HashPassword(string password) {
			//use SHA512 to hash the password.
			Byte[] passBytes=Encoding.BigEndianUnicode.GetBytes(password);
			SHA512Managed sha = new SHA512Managed();
			Byte[] hash=sha.ComputeHash(passBytes);
			sha.Clear();
			return hash;
		}
	}
}