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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace SteamEngine.Common {
	public static class Tools {
#if !MONO
		[SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
		public const string commonPipeName = @"steamAuxPipe";
#else
		public const int commonPort = 2590;
#endif

		[SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
		[SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly")]
		public static readonly char[] whitespaceChars = {
			(char) 9, (char) 10, (char) 11, (char) 12, (char) 13, (char) 32, (char) 133, (char) 160, (char) 5760,
			(char) 8192, (char) 8193, (char) 8194, (char) 8195, (char) 8196, (char) 8197, (char) 8198, (char) 8199,
			(char) 8200, (char) 8201, (char) 8202, (char) 8203, (char) 8232, (char) 8233, (char) 12288,
			(char) 65279
		};


		public static void ExitBinDirectory() {
			var path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			if (path.EndsWith("build\\" + Build.Type, StringComparison.OrdinalIgnoreCase)) {// || (pathToLower.EndsWith("bin-profiled"))) {
				var newDir = Path.GetDirectoryName(Path.GetDirectoryName(path));
				Directory.SetCurrentDirectory(newDir);
				Console.WriteLine($"Changed current directory to '{newDir}'.");
			}
			else
			{
				Logger.WriteWarning($"Current directory '{path}' unexpected. Normally this should be run from the directory into which it compiles. Otherwise Strange things may happen.");
			}
		}

		public static string CombineMultiplePaths(params string[] paths) {
			if ((paths == null) || (paths.Length == 0)) {
				return "";
			}
			if (paths.Length == 1) {
				return paths[0];
			}
			var retVal = paths[0];
			for (int i = 1, n = paths.Length; i < n; i++) {
				retVal = Path.Combine(retVal, paths[i]);
			}
			return retVal;
		}

		private static char[] separators = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
		public static string[] SplitPath(string path) {
			if (path == null) {
				return new string[0];
			}
			var dirs = path.Split(separators);
			if (dirs[0].EndsWith(Path.VolumeSeparatorChar.ToString())) {
				dirs[0] = dirs[0] + Path.DirectorySeparatorChar;
			}

			//VolumeSeparatorChar
			return dirs;
		}

		public static void EnsureDirectory(string path) {
			EnsureDirectory(path, false);
		}

		public static void EnsureDirectory(string path, bool announce) {
			if (!Directory.Exists(path)) {
				var split = SplitPath(path);
				if (split.Length > 0) {
					var curDir = "";
					for (int i = 0, n = split.Length; i < n; i++) {
						curDir = Path.Combine(curDir, split[i]);
						if (!Directory.Exists(curDir)) {
							if (announce) {
								Console.WriteLine("Creating directory " + LogStr.Ident(curDir));
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
			var retVal = new StringBuilder();
			if (obj == null) {
				retVal.Append("null");
			} else {
				var asIDictionary = obj as IDictionary;

				if (asIDictionary != null) {
					if (!(obj is Hashtable) && !(typeof(Dictionary<,>).IsInstanceOfType(obj))) { //if not Hashtable or Dictionary<>, display the actual type
						retVal.Append(TypeToString(obj.GetType()));
					}
					retVal.Append("{");
					foreach (DictionaryEntry entry in asIDictionary) {
						retVal.Append(ObjToString(entry.Key));
						retVal.Append(" : ");
						retVal.Append(ObjToString(entry.Value));
						retVal.Append(", ");
					}
					retVal.Append("}");

				} else {
					var asArray = obj as Array;
					if (asArray != null) {

						retVal.Append("(");
						for (int i = 0, n = asArray.Length; i < n; i++) {
							retVal.Append(ObjToString(asArray.GetValue(i)));
							retVal.Append(", ");
						}
						retVal.Append(")");
					} else {
						var asIList = obj as IList;
						if (asIList != null) {
							if (!(obj is ArrayList) && !(typeof(List<>).IsInstanceOfType(obj))) { //if not arraylist or List<>, display the actual type
								retVal.Append(TypeToString(obj.GetType()));
							}

							retVal.Append("[");
							for (int i = 0, n = asIList.Count; i < n; i++) {
								retVal.Append(ObjToString(asIList[i]));
								retVal.Append(", ");
							}
							retVal.Append("]");
						} else {
							IEnumerable asEnumerable;
							if (AsEnumerableNotThing(obj, out asEnumerable)) {
								retVal.Append(TypeToString(obj.GetType()));

								retVal.Append("[");
								foreach (var o in asEnumerable) {
									retVal.Append(ObjToString(o));
									retVal.Append(", ");
								}
								retVal.Append("]");
							} else if (obj is Enum) {
								retVal.Append("'");
								retVal.Append(obj);
								retVal.Append("'");
							} else {
								var asString = obj as string;
								if (asString != null) {
									retVal.Append("\"");
									retVal.Append(asString);
									retVal.Append("\"");
								} else if (ConvertTools.IsNumber(obj)) {
									retVal.Append(obj);
								} else {
									var asType = obj as Type;
									if (asType != null) {
										retVal.Append("'");
										retVal.Append(TypeToString(asType));
										retVal.Append("'");
									} else {
										retVal.Append("'");
										retVal.Append(obj);
										retVal.Append("'");
									}
								}
							}
						}
					}
				}
			}
			return retVal.ToString();
		}

		private static bool AsEnumerableNotThing(object obj, out IEnumerable asEnumerable) {
			var t = obj.GetType();
			do {
				if (t.Name.Equals("Thing")) {
					asEnumerable = null;
					return false;
				}
				t = t.BaseType;
			} while (t != null);

			asEnumerable = obj as IEnumerable;
			return asEnumerable != null;
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public static string TypeToString(Type type) {
			var s = type.Name;
			if (type.IsGenericType) {
				var apostrofIndex = s.LastIndexOf("`");
				if (apostrofIndex >= 0) {
					s = s.Substring(0, apostrofIndex);
				}
				var genArgs = type.GetGenericArguments();
				if (type.IsGenericTypeDefinition) {
					s += string.Concat("<", ">".PadLeft(genArgs.Length, ',')); //<,,,>
				} else {
					var sb = new StringBuilder();
					foreach (var t in genArgs) {
						sb.Append(TypeToString(t)).Append(", "); //recursive call :)
					}
					sb.Length -= 2;
					s += string.Concat("<", sb.ToString(), ">");
				}
			}
			return s;
		}

		private static HashAlgorithm hashAlgorithm = new SHA512Managed();

		public static byte[] HashPassword(string password) {
			//use SHA512 to hash the password.
			var passBytes = Encoding.BigEndianUnicode.GetBytes(password);
			var hash = hashAlgorithm.ComputeHash(passBytes);
			return hash;
		}

		private static Dictionary<Type, int> checkedEnums = new Dictionary<Type, int>();


		//the enum shall start at 0 and have no holes in it, then we can get the number of numbers with a name in it.
		[SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate"),
		SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
		public static int GetEnumLength<T>() where T : struct {
			var enumType = typeof(T);
			int retVal;
			if (checkedEnums.TryGetValue(enumType, out retVal)) {
				return retVal;
			}
			if (!enumType.IsEnum) {
				throw new SEException(enumType + " is no enum.");
			}

			var values = new HashSet<int>();
			foreach (var o in Enum.GetValues(enumType)) {
				values.Add(Convert.ToInt32(o, CultureInfo.InvariantCulture));
			}

			var n = values.Count;
			var arr = new int[n];
			values.CopyTo(arr, 0);
			Array.Sort(arr);
			for (var i = 0; i < n; i++) {
				if (i != arr[i]) {
					throw new SEException("The enum " + enumType + " is not coherent");
				}
			}
			checkedEnums.Add(enumType, n);
			return n;
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public static string EscapeBackslashes(string value) {
			//return value;

			var sb = new StringBuilder();
			foreach (var ch in value) {
				if (ch == '\\') {
					sb.Append('\\');
				}
				sb.Append(ch);
			}
			return sb.ToString();
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public static string UnescapeBackslashes(string value) {
			//return value;

			var sb = new StringBuilder();
			var lastCharWasBackslash = false;
			foreach (var ch in value) {
				if (lastCharWasBackslash) {
					sb.Append(ch);
					lastCharWasBackslash = false;
					continue;
				}

				if (ch == '\\') {
					lastCharWasBackslash = true;
					continue;
				}
				sb.Append(ch);
			}
			return sb.ToString();
		}


		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public static string EscapeNewlines(string value) {
			//return value;

			var sb = new StringBuilder();
			foreach (var ch in value) {
				char escapedEquivalent;
				if (CharNeedsEscaping(ch, out escapedEquivalent)) {
					sb.Append("\\").Append(escapedEquivalent);
				} else {
					sb.Append(ch);
				}
			}
			return sb.ToString();
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public static string UnescapeNewlines(string value) {
			//return value;

			var sb = new StringBuilder();
			var lastCharWasBackslash = false;
			foreach (var ch in value) {
				if (lastCharWasBackslash) {
					char unEscapedEquivalent;
					if (CharNeedsUnEscaping(ch, out unEscapedEquivalent)) {
						sb.Append(unEscapedEquivalent);
					} else {
						sb.Append(ch);
					}
					lastCharWasBackslash = false;
					continue;
				}

				if (ch == '\\') {
					lastCharWasBackslash = true;
					continue;
				}
				sb.Append(ch);
			}
			return sb.ToString();
		}

		private static bool CharNeedsEscaping(char ch, out char escapedEquivalent) {
			escapedEquivalent = ch;
			switch (ch) {
				case '\n':
					escapedEquivalent = 'n';
					return true;
				case '\r':
					escapedEquivalent = 'r';
					return true;
				case '\\':
					return true;
			}
			return false;
		}

		private static bool CharNeedsUnEscaping(char ch, out char unEscapedEquivalent) {
			unEscapedEquivalent = ch;
			switch (ch) {
				case 'n':
					unEscapedEquivalent = '\n';
					return true;
				case 'r':
					unEscapedEquivalent = '\r';
					return true;
			}
			return false;
		}
	}


	//used more or less just like a renamed int. A new struct might be better but this works too :)
	//just wanted to make clear what's what
	//used by aux and rc
	public enum GameUid {
		AuxServer = 0,
		FirstSEGameServer = 1,
		LastSphereServer = int.MaxValue
		//...
	}
}
