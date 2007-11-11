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
using SteamEngine.Common;

namespace SteamEngine.LScript {
	public class IntrinsicMethods { //abstract - i.e. static
		public static ISrc Src {
			get {
				return Globals.Src;
			}
		}

		public static void Throw(Exception e) {
			throw e;
		}

		public static TagHolder LastNew {
			get {
				return Globals.lastNew;
			}
		}

		public static AbstractItem LastNewItem {
			get {
				return Globals.lastNewItem;
			}
		}

		public static AbstractCharacter LastNewChar {
			get {
				return Globals.lastNewChar;
			}
		}

		public static Globals Serv {
			get {
				return Globals.instance;
			}
		}

		public static void Print(string str) {
			//Console.WriteLine("str.GetType(): "+str.GetType());
			Console.WriteLine(str);
		}

		public static void Show(object o) {
			//Console.WriteLine("str.GetType(): "+str.GetType());
			Globals.SrcWriteLine(Tools.ObjToString(o));
		}

		public static int StrLen(string str) {
			return str.Length;
		}

		public static int StrCmp(string str1, string str2) {
			return string.Compare(str1, str2, false);
		}

		public static int StrCmpi(string str1, string str2) {
			return string.Compare(str1, str2, true);
		}

		public static ArrayList List() { //a 'shortcut' for spherescript to make an arraylist
			return new ArrayList();
		}

		public static object Qval(bool test, object obj1, object obj2) {
			return test ? obj1 : obj2;
		}

		public static Thing Finduid(int uid) {
			return Thing.UidGetThing(uid);
		}

		public static int Min(int int1, int int2) {
			return Math.Min(int1, int2);
		}

		public static int Max(int int1, int int2) {
			return Math.Max(int1, int2);
		}

		//public static int negativenumber() {
		//	return -1;
		//}



		//public static double Sqrt(double arg) {
		//	return Math.Sqrt(arg);
		//}
		//public static void comptest() {
		//	DateTime before = DateTime.Now;
		//	for (int i = 1000000; i>0; i--) {
		//		Sqrt(687231);
		//	}
		//	DateTime after = DateTime.Now;
		//	Console.WriteLine("diff: "+(after-before));
		//}
		//
		//private static double Sqrt(double arg) {
		//	double root1 = arg;
		//	double root2 = arg;
		//	double lastroot = -1;
		//	for (int i = 0; i<30; i++) {
		//		lastroot = root1;
		//		root1 = (root1/2)+((root2/root1)/2);
		//		if ((lastroot==root1) || (lastroot == root1)) {
		//			return lastroot;
		//		}
		//	}
		//	return lastroot;
		//}
	}
}