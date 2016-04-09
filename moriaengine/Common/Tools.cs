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
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;


namespace SteamEngine.Common {
	public static class Tools {
#if !MONO
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
		public const string commonPipeName = @"steamAuxPipe";
#else
		public const int commonPort = 2590;
#endif

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly")]
		public static readonly char[] whitespaceChars = new char[] { 
			(char) 9, (char) 10, (char) 11, (char) 12, (char) 13, (char) 32, (char) 133, (char) 160, (char) 5760, 
			(char) 8192, (char) 8193, (char) 8194, (char) 8195, (char) 8196, (char) 8197, (char) 8198, (char) 8199, 
			(char) 8200, (char) 8201, (char) 8202, (char) 8203, (char) 8232, (char) 8233, (char) 12288, 
			(char) 65279
		};


		public static void ExitBinDirectory() {
			//string cmdLine = Environment.CommandLine.Replace('"', ' ').Trim();
			//string path=Path.GetDirectoryName(Path.Combine(Environment.CurrentDirectory, cmdLine));//Directory.GetCurrentDirectory();
			string path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			string pathToLower = path.ToLowerInvariant();
			if ((pathToLower.EndsWith("bin")) ||
				(pathToLower.EndsWith("bin-profiled"))) {
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
			for (int i = 1, n = paths.Length; i < n; i++) {
				retVal = Path.Combine(retVal, paths[i]);
			}
			return retVal;
		}

		private static char[] separators = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
		public static string[] SplitPath(string path) {
			if (path == null) {
				return new string[0];
			}
			string[] dirs = path.Split(separators);
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
				string[] split = SplitPath(path);
				if (split.Length > 0) {
					string curDir = "";
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
			StringBuilder retVal = new StringBuilder();
			if (obj == null) {
				retVal.Append("null");
			} else {
				IDictionary asIDictionary = obj as IDictionary;

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
					Array asArray = obj as Array;
					if (asArray != null) {

						retVal.Append("(");
						for (int i = 0, n = asArray.Length; i < n; i++) {
							retVal.Append(ObjToString(asArray.GetValue(i)));
							retVal.Append(", ");
						}
						retVal.Append(")");
					} else {
						IList asIList = obj as IList;
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
								foreach (object o in asEnumerable) {
									retVal.Append(ObjToString(o));
									retVal.Append(", ");
								}
								retVal.Append("]");
							} else if (obj is Enum) {
								retVal.Append("'");
								retVal.Append(obj.ToString());
								retVal.Append("'");
							} else {
								string asString = obj as string;
								if (asString != null) {
									retVal.Append("\"");
									retVal.Append(asString);
									retVal.Append("\"");
								} else if (ConvertTools.IsNumber(obj)) {
									retVal.Append(obj.ToString());
								} else {
									Type asType = obj as Type;
									if (asType != null) {
										retVal.Append("'");
										retVal.Append(TypeToString(asType));
										retVal.Append("'");
									} else {
										retVal.Append("'");
										retVal.Append(obj.ToString());
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
			Type t = obj.GetType();
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

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public static string TypeToString(Type type) {
			string s = type.Name;
			if (type.IsGenericType) {
				int apostrofIndex = s.LastIndexOf("`");
				if (apostrofIndex >= 0) {
					s = s.Substring(0, apostrofIndex);
				}
				Type[] genArgs = type.GetGenericArguments();
				if (type.IsGenericTypeDefinition) {
					s += string.Concat("<", ">".PadLeft(genArgs.Length, ',')); //<,,,>
				} else {
					StringBuilder sb = new StringBuilder();
					foreach (Type t in genArgs) {
						sb.Append(TypeToString(t)).Append(", "); //recursive call :)
					}
					sb.Length -= 2;
					s += string.Concat("<", sb.ToString(), ">");
				}
			}
			return s;
		}

		private static HashAlgorithm hashAlgorithm = new SHA512Managed();

		public static Byte[] HashPassword(string password) {
			//use SHA512 to hash the password.
			Byte[] passBytes = Encoding.BigEndianUnicode.GetBytes(password);
			Byte[] hash = hashAlgorithm.ComputeHash(passBytes);
			return hash;
		}

		private static Dictionary<Type, int> checkedEnums = new Dictionary<Type, int>();


		//the enum shall start at 0 and have no holes in it, then we can get the number of numbers with a name in it.
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate"),
		System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
		public static int GetEnumLength<T>() where T : struct {
			Type enumType = typeof(T);
			int retVal;
			if (checkedEnums.TryGetValue(enumType, out retVal)) {
				return retVal;
			} else {
				if (!enumType.IsEnum) {
					throw new SEException(enumType + " is no enum.");
				}

				HashSet<int> values = new HashSet<int>();
				foreach (object o in Enum.GetValues(enumType)) {
					values.Add(Convert.ToInt32(o, System.Globalization.CultureInfo.InvariantCulture));
				}

				int n = values.Count;
				int[] arr = new int[n];
				values.CopyTo(arr, 0);
				Array.Sort<int>(arr);
				for (int i = 0; i < n; i++) {
					if (i != arr[i]) {
						throw new SEException("The enum " + enumType + " is not coherent");
					}
				}
				checkedEnums.Add(enumType, n);
				return n;
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public static string EscapeBackslashes(string value) {
			//return value;

			StringBuilder sb = new StringBuilder();
			foreach (char ch in value) {
				if (ch == '\\') {
					sb.Append('\\');
				}
				sb.Append(ch);
			}
			return sb.ToString();
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public static string UnescapeBackslashes(string value) {
			//return value;

			StringBuilder sb = new StringBuilder();
			bool lastCharWasBackslash = false;
			foreach (char ch in value) {
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


		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public static string EscapeNewlines(string value) {
			//return value;

			StringBuilder sb = new StringBuilder();
			foreach (char ch in value) {
				char escapedEquivalent;
				if (CharNeedsEscaping(ch, out escapedEquivalent)) {
					sb.Append("\\").Append(escapedEquivalent);
				} else {
					sb.Append(ch);
				}
			}
			return sb.ToString();
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public static string UnescapeNewlines(string value) {
			//return value;

			StringBuilder sb = new StringBuilder();
			bool lastCharWasBackslash = false;
			foreach (char ch in value) {
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
		LastSphereServer = int.MaxValue,
		//...
	}
}
