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
using System.Globalization;
using System.Text.RegularExpressions;
using System.Collections;
using System.Reflection;
namespace SteamEngine.Common {

	//I renamed the methods and stuff, and reimplemented only those which are needed now, cos I'm too lazy :)
	//if anyone should need any methods that are not implemented, or are commented out, implement them after the ones that are already done.
	//the commented-out ones are commented out for a reason, do not let them the way they are!
	public class ConvertTools {
		protected static ConvertTools instance;
		
		public static Regex stringRE= new Regex(@"^""(?<value>.*)""\s*$",                   
			RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);
		public static Regex floatRE= new Regex(@"^(?<value>-?\d*\.\d*)\s*$",                
			RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);
		public static Regex intRE= new Regex(@"^(?<value>-?\d+)\s*$",                    
			RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);
		public static Regex hexRE = new Regex(@"^0[x]?(?<value>[0-9a-f]+)\s*$",
			RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);
		public static Regex timeSpanRE= new Regex(@"^\:(?<value>\d+)\s*$",                     
			RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);
		public static Regex dateTimeRE= new Regex(@"^\::(?<value>\d+)\s*$",                     
			RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);

		protected ConvertTools() {
			instance=this;
		}

		[Summary("If the string contains a \n, \r, or \0, then it is truncated just before that character."
			 + "If it contains any tabs, they are changed to spaces.")]
		public static string RemoveIllegalChars(string s) {
			if (s==null) return s;
			for (int a=0; a<s.Length; a++) {
				char c = s[a];
				if (c=='\n' || c=='\r' || c=='\0') {
					s=s.Substring(0,a);
					break;
				} else if (c=='\t') {
					if (a<s.Length-1) {
						s=s.Substring(0,a)+" "+s.Substring(a+1);
					} else {
						s=s.Substring(0,a)+" ";
					}
				}
			}
			return s;
		}
		
		[Summary("The most generic method to convert types. Throws when convert impossible.")]
		public static object ConvertTo(Type type, object obj) {
			//Console.WriteLine("Converting from {0} {1} to {2}", obj.GetType(), obj, type);
			if (obj==null) return obj;
			Type objectType = obj.GetType();
			string sobj = obj as string;
			if (sobj=="null") return null;
			if ((objectType == type) || (type.IsAssignableFrom(objectType))) {
				return obj;
			} else if (type.Equals(typeof(String))) {
				return obj.ToString();
			} else if (type.Equals(typeof(Boolean))) {
				return ToBoolean(obj);
			} else if (type.IsEnum) {
				if (obj!=null) {
					//The first thing to try is converting it to whatever the underlying type is,
					//since Enum.Parse fails on things like "0x4c" or "04c", etc.
					try {
						return Enum.ToObject(type, ConvertTo(Enum.GetUnderlyingType(type), obj));
					} catch (InvalidCastException) {
						string asString = obj as String;
						if (asString != null) {
							return Enum.Parse(type, asString.Replace('|', ','), true);
						}
					}
				}
			} else if (IsNumberType(type)) {
				string asString = obj as String;
				if (asString != null) {
					return Convert.ChangeType(ParseSphereNumber(asString), type);
				}
			}
			return Convert.ChangeType(obj, type);
		}

		[Summary("The most generic method to convert types. Returns false when convert impossible.")]
		public static bool TryConvertTo(Type type, object obj, out object retVal) {
			try {
				retVal = ConvertTools.ConvertTo(type, obj);
				return true;
			} catch (Exception) {
				retVal = null;
				return false;
			}
		}

		public static bool IsNumber(object o) {
			if (o == null) {
				return false;
			} else {
				return IsNumberType(o.GetType());
			}
		}
		
		public static bool IsNumberType(Type t) {
			switch (Type.GetTypeCode(t)) {
				case TypeCode.Byte:
				case TypeCode.Decimal:
				case TypeCode.Double:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.Single:
				case TypeCode.SByte:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				case TypeCode.Boolean:
					return true;
			}
			return false;
		}
		
		public static bool IsIntegerType(Type t) {
			switch (Type.GetTypeCode(t)) {
				case TypeCode.Byte:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.SByte:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				case TypeCode.Boolean:
					return true;
			}
			return false;
		}

		public static bool IsUnSignedIntegerType(Type t) {
			switch (Type.GetTypeCode(t)) {
				case TypeCode.Byte:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
					return true;
			}
			return false;
		}

		public static bool IsSignedIntegerType(Type t) {
			switch (Type.GetTypeCode(t)) {
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.SByte:
					return true;
			}
			return false;
		}

		public static bool IsFloatType(Type t) {
			switch (Type.GetTypeCode(t)) {
				case TypeCode.Decimal:
				case TypeCode.Double:
				case TypeCode.Single:
					return true;
			}
			return false;
		}

		/**
			This will convert the following kinds of values:
				If the string is null, false is returned.
				If s is "true" "1" or "on" (case does not matter), true is returned.
				If s is "false" "0" or "off" (case does not matter), false is returned.
				If s is any other value, a FormatException is thrown.
		*/
		public static bool ParseBoolean(string s) {
			if (s==null) return false;
			s=s.ToLower();
			if (s=="true" || s=="1" || s=="on") {
				return true;
			} else if (s=="false" || s=="0" || s=="off") {
				return false;
			}
			throw new FormatException("'"+s+"' is not a valid boolean string (true/1/on/false/0/off).");
		}

		public static bool ToBoolean(object arg) {
			if (instance==null) {
				instance = new ConvertTools();	//until TagMath replaces it.
			}
			return instance.ToBoolImpl(arg);
		}
		
		protected virtual bool ToBoolImpl(object arg) {
			if (arg is bool) {
				return (bool)arg;
			} else if (IsNumber(arg)) {
				return (Convert.ToInt32(arg)!=0);
			} else if (arg is string) {
				return ParseBoolean((string) arg);
			} else {
				return (arg!=null);
			}
		}

		/**
			This will convert the following kinds of values:
				If it is an empty string : We throw FormatException without bothering to try to convert it.
				If it is "0" : We return 0.
				If it starts with '0x', we convert it to a uint (and parse it as hex).
				If it has a '.' in it, we convert it to a double.
				If it starts with '0' (but doesn't have a '.' in it), we convert it to a uint (and parse it as hex).
				If it starts with 1-9 or '.' or '-':
					If it has a '.' in it, we convert it to a double.
					Otherwise, we convert it to an int.
				
				Basically, anything in hex (or sphere hex format) is assumed to be a uint, and anything else
				is either a double or an int. This mirrors how sphere works except that sphere doesn't support 0x
				as a hex prefix.
				
				If the conversion to double, int, or uint fails because the number doesn't fit, then we retry it as
				a decimal, long, or ulong, respectively. If it STILL fails, we give up and you get an OverflowException.
				
			Throws FormatException if it isn't a number at all (like if it starts with anything other than 0-9, '.' or '-', is something like '42b5z' or 'moo', etc).
			
			If Parse throws an exception, this does not stop it. Parse may throw:
			ArgumentNullException - If input is null.
			FormatException - If input is "", or is not a number.
			OverflowException - If the format is correct, but it won't fit in the data type we're trying to convert it to.
		*/
		public static object ParseSphereNumber(string input) {
			if (input == null) {
				throw new ArgumentNullException("input");
			}

			if (input.Length==0) {
				throw new FormatException("Cannot convert an empty string to any kind of number!");
			} 

			Match m = hexRE.Match(input);
			if (m.Success) {
				string toConvert = m.Groups["value"].Value;
				try {
					return uint.Parse(toConvert, NumberStyles.HexNumber);
				} catch (OverflowException) {//try a bigger type. If this fails, we give up.
					return ulong.Parse(toConvert, NumberStyles.HexNumber);
				} 
			}
			
			m = intRE.Match(input);
			if (m.Success) {
				string toConvert = m.Groups["value"].Value;
				try {
					return int.Parse(toConvert);
				} catch (OverflowException) {//try a bigger type. If this fails, we give up.
					return long.Parse(toConvert);
				} 
			}
		
			m = floatRE.Match(input);
			if (m.Success) {
				return double.Parse(m.Groups["value"].Value);
			}
			
			throw new FormatException("'"+input+"' does not appear to be any kind of number.");
		}

		public static bool TryParseSphereNumber(string input, out object retVal) {
			retVal = null;
			try {
				retVal = ParseSphereNumber(input);
			} catch (Exception) {
				return false;
			}
			return true;
		}


		/**
			The 'Parse*' methods differ from the 'To*' methods - Parse* will only accept a string as input, not some other
			object, and Parse* supports sphere-style hex numbers (By using ConvertSphereNumber).
			'TryParse-' methods do the same like the 'Parse-' ones, except they don't throw exceptions, only return false on non-success.
		
			May throw:
				ArgumentNullException - If input is null.
				NotANumberException - If input is "", or is not a number.
				OverflowException - If the format is correct, but it won't fit in the data type we're trying to convert it to.
		*/

		#region Double (double)
		public static double ParseDouble(string input) {
			return Convert.ToDouble(ParseSphereNumber(input));
		}

		public static bool TryParseDouble(string input, out Double retVal) {
			try {
				retVal = Convert.ToDouble(ParseSphereNumber(input));
				return true;
			} catch (Exception) {
				retVal = Double.MinValue;
				return false;
			}
		}

		public static Double ToDouble(object input) {
			string asString = input as String;
			if (asString != null) {
				return ParseDouble(asString);
			} else {
				return Convert.ToDouble(input);
			}
		}
		#endregion Double

		#region Single
		public static Single ParseSingle(string input) {
			return Convert.ToSingle(ParseSphereNumber(input));
		}

		public static bool TryParseSingle(string input, out Single retVal) {
			try {
				retVal = Convert.ToSingle(ParseSphereNumber(input));
				return true;
			} catch (Exception) {
				retVal = Single.MinValue;
				return false;
			}
		}

		public static Single ToSingle(object input) {
			string asString = input as String;
			if (asString != null) {
				return ParseSingle(asString);
			} else {
				return Convert.ToSingle(input);
			}
		}
		#endregion Single

		#region Int64 (long)
		public static long ParseInt64(string input) {
			return Convert.ToInt64(ParseSphereNumber(input));
		}

		public static bool TryParseInt64(string input, out Int64 retVal) {
			try {
				retVal = Convert.ToInt64(ParseSphereNumber(input));
				return true;
			} catch (Exception) {
				retVal = Int64.MinValue;
				return false;
			}
		}

		public static Int64 ToInt64(object input) {
			string asString = input as String;
			if (asString != null) {
				return ParseInt64(asString);
			} else {
				return Convert.ToInt64(input);
			}
		}
		#endregion Int66 (long)

		#region UInt64 (ulong)
		public static UInt64 ParseUInt64(string input) {
			return Convert.ToUInt64(ParseSphereNumber(input));
		}

		public static bool TryParseUInt64(string input, out UInt64 retVal) {
			try {
				retVal = Convert.ToUInt64(ParseSphereNumber(input));
				return true;
			} catch (Exception) {
				retVal = UInt64.MinValue;
				return false;
			}
		}

		public static UInt64 ToUInt64(object input) {
			string asString = input as String;
			if (asString != null) {
				return ParseUInt64(asString);
			} else {
				return Convert.ToUInt64(input);
			}
		}
		#endregion UInt64 (ulong)

		#region Int32 (int)
		public static Int32 ParseInt32(string input) {
			return Convert.ToInt32(ParseSphereNumber(input));
		}

		public static bool TryParseInt32(string input, out Int32 retVal) {
			try {
				retVal = Convert.ToInt32(ParseSphereNumber(input));
				return true;
			} catch (Exception) {
				retVal = Int32.MinValue;
				return false;
			}
		}

		public static Int32 ToInt32(object input) {
			string asString = input as String;
			if (asString != null) {
				return ParseInt32(asString);
			} else {
				return Convert.ToInt32(input);
			}
		}
		#endregion Int32 (int)

		#region UInt32 (uint)
		public static UInt32 ParseUInt32(string input) {
			return Convert.ToUInt32(ParseSphereNumber(input));
		}

		public static bool TryParseUInt32(string input, out UInt32 retVal) {
			try {
				retVal = Convert.ToUInt32(ParseSphereNumber(input));
				return true;
			} catch (Exception) {
				retVal = UInt32.MinValue;
				return false;
			}
		}

		public static UInt32 ToUInt32(object input) {
			string asString = input as String;
			if (asString != null) {
				return ParseUInt32(asString);
			} else {
				return Convert.ToUInt32(input);
			}
		}
		#endregion UInt32 (uint)

		#region Int16 (short)
		public static Int16 ParseInt16(string input) {
			return Convert.ToInt16(ParseSphereNumber(input));
		}

		public static bool TryParseInt16(string input, out Int16 retVal) {
			try {
				retVal = Convert.ToInt16(ParseSphereNumber(input));
				return true;
			} catch (Exception) {
				retVal = Int16.MinValue;
				return false;
			}
		}

		public static Int16 ToInt16(object input) {
			string asString = input as String;
			if (asString != null) {
				return ParseInt16(asString);
			} else {
				return Convert.ToInt16(input);
			}
		}
		#endregion Int16 (short)

		#region UInt16 (ushort)
		public static UInt16 ParseUInt16(string input) {
			return Convert.ToUInt16(ParseSphereNumber(input));
		}

		public static bool TryParseUInt16(string input, out UInt16 retVal) {
			try {
				retVal = Convert.ToUInt16(ParseSphereNumber(input));
				return true;
			} catch (Exception) {
				retVal = UInt16.MinValue;
				return false;
			}
		}

		public static UInt16 ToUInt16(object input) {
			string asString = input as String;
			if (asString != null) {
				return ParseUInt16(asString);
			} else {
				return Convert.ToUInt16(input);
			}
		}
		#endregion UInt16 (ushort)

		#region SByte (sbyte)
		public static sbyte ParseSByte(string input) {
			return Convert.ToSByte(ParseSphereNumber(input));
		}

		public static bool TryParseSByte(string input, out SByte retVal) {
			try {
				retVal = Convert.ToSByte(ParseSphereNumber(input));
				return true;
			} catch (Exception) {
				retVal = SByte.MinValue;
				return false;
			}
		}

		public static SByte ToSByte(object input) {
			string asString = input as String;
			if (asString != null) {
				return ParseSByte(asString);
			} else {
				return Convert.ToSByte(input);
			}
		}
		#endregion SByte (byte)

		#region Byte (byte)
		public static Byte ParseByte(string input) {
			return Convert.ToByte(ParseSphereNumber(input));
		}

		public static bool TryParseByte(string input, out Byte retVal) {
			try {
				retVal = Convert.ToByte(ParseSphereNumber(input));
				return true;
			} catch (Exception) {
				retVal = Byte.MinValue;
				return false;
			}
		}

		public static Byte ToByte(object input) {
			string asString = input as String;
			if (asString != null) {
				return ParseByte(asString);
			} else {
				return Convert.ToByte(input);
			}
		}
		#endregion Byte (byte)
		//		
//		//public static object GetNumber(Type type, string input) {
//		//	return ConvertTo(type, ConvertSphereNumber(input));
//		//}
//		
//		
	}
}