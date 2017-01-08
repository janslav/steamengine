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
namespace SteamEngine.Common {

	//I renamed the methods and stuff, and reimplemented only those which are needed now, cos I'm too lazy :)
	//if anyone should need any methods that are not implemented, or are commented out, implement them after the ones that are already done.
	//the commented-out ones are commented out for a reason, do not let them the way they are!
	public class ConvertTools {
		private static ConvertTools instance;

		private static readonly CultureInfo invariantCulture = CultureInfo.InvariantCulture;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
		public static readonly Regex stringRE = new Regex(@"^""(?<value>.*)""\s*$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
		public static readonly Regex floatRE = new Regex(@"^(?<value>-?\d*\.\d*)\s*$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
		public static readonly Regex intRE = new Regex(@"^(?<value>-?\d+)\s*$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
		public static readonly Regex hexRE = new Regex(@"^0[x]?(?<value>[0-9a-f]+)\s*$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		//public static Regex timeSpanRE = new Regex(@"^\:(?<value>\d+)\s*$",
		//changed to match timespan in format like [-]d.hh:mm:ss.ff
		//public static Regex timeSpanRE = new Regex(@"^\:(?<value>-?(\d*.)?([012]?\d?:[0-5]\d(:[0-5]\d(\.\d{1,7})?)?)?)\s*$",                     
		//	RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);

		//public static Regex dateTimeRE= new Regex(@"^\::(?<value>\d+)\s*$",                     
		//changed to match date/time in format like dd.MM.yyyy HH.mm.ss.fffffff
		//the whole time part is voluntary 
		//the seconds part is voluntary
		//and the decimal part too
		//I know it is not perfect, but it should be enough for our purposes.
		//public static Regex dateTimeRE = new Regex(@"^\::(?<value>([0-3]?\d?\.[01]?\d?\.\d\d\d\d)\s*([012]?\d?:[0-5]\d(:[0-5]\d(\.\d{1,7})?)?)?)\s*$",
		//	RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);

		protected ConvertTools() {
			instance = this;
		}

		//remove quotes or just trim whitespace
		public static string LoadSimpleQuotedString(string input) {
			Match ma = stringRE.Match(input);
			if (ma.Success) {
				return Tools.UnescapeNewlines(ma.Groups["value"].Value);
			} else {
				return input.Trim();
			}
		}

		/// <summary>
		/// Removes the illegal chars.
		/// If the string contains a \n, \r, or \0, then it is truncated just before that character.
		/// If it contains any tabs, they are changed to spaces.
		/// </summary>
		/// <param name="s">The s.</param>
		/// <returns></returns>
		public static string RemoveIllegalChars(string s) {
			if (s == null) return s;
			for (int a = 0; a < s.Length; a++) {
				char c = s[a];
				if (c == '\n' || c == '\r' || c == '\0') {
					s = s.Substring(0, a);
					break;
				} else if (c == '\t') {
					if (a < s.Length - 1) {
						s = s.Substring(0, a) + " " + s.Substring(a + 1);
					} else {
						s = s.Substring(0, a) + " ";
					}
				}
			}
			return s;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
		public static T ConvertTo<T>(object obj) {
			//TODO some optimisation? at least for valuetypes maybe?
			return (T) ConvertTo(typeof(T), obj);
		}

		/// <summary>
		/// The most generic method to convert types. Throws when convert impossible."
		/// </summary>
		/// <param name="type">The type.</param>
		/// <param name="obj">The obj.</param>
		/// <returns></returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public static object ConvertTo(Type type, object obj) {
			//Console.WriteLine("Converting from {0} {1} to {2}", obj.GetType(), obj, type);
			if (obj == null) return obj;
			Type objectType = obj.GetType();
			string asString = obj as string;
			if (asString == "null") {
				return null;
			}
			if ((objectType == type) || (type.IsAssignableFrom(objectType))) {
				return obj;
			} else if (type.Equals(typeof(String))) {
				return ToString(obj);
			} else if (type.Equals(typeof(Boolean))) {
				return ToBoolean(obj);
			} else if (type.IsEnum) {
				if (obj != null) {
					//The first thing to try is converting it to whatever the underlying type is,
					//since Enum.Parse fails on things like "0x4c" or "04c", etc.
					try {
						return Enum.ToObject(type, ToInt64(obj));
					} catch (InvalidCastException) {
						if (asString != null) {
							return Enum.Parse(type, asString.Replace('|', ','), true);
						}
					}
				}
			} else if (IsNumberType(type)) {
				if (asString != null) {
					return Convert.ChangeType(ParseAnyNumber(asString), type, invariantCulture);
				}
			}
			return Convert.ChangeType(obj, type, invariantCulture);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		/// <summary>
		/// The most generic method to convert types. Returns false when convert impossible.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <param name="obj">The obj.</param>
		/// <param name="retVal">The ret val.</param>
		/// <returns></returns>
		public static bool TryConvertTo(Type type, object obj, out object retVal) {
			try {
				retVal = ConvertTo(type, obj);
				return true;
			} catch (FatalException) {
				throw;
			} catch (Exception) {
				retVal = null;
				return false;
			}
		}

		/// <summary>Try converting the given object to string</summary>
		public static bool TryConvertToString(object obj, out string retVal) {
			IConvertible asConvertible = obj as IConvertible;
			if (asConvertible != null) {
				retVal = asConvertible.ToString(invariantCulture);
				return true;
			}
			IFormattable asFormattable = obj as IFormattable;
			if (asFormattable != null) {
				retVal = asFormattable.ToString(null, invariantCulture);
				return true;
			}

			retVal = null;
			return false;
		}

		/// <summary>Try converting the given object to string</summary>
		public static string ToString(object obj) {
			IConvertible asConvertible = obj as IConvertible;
			if (asConvertible != null) {
				return asConvertible.ToString(invariantCulture);
			}
			IFormattable asFormattable = (IFormattable) obj; //throws exception if impossible
			if (asFormattable != null) {
				return asFormattable.ToString(null, invariantCulture);
			} else {
				return "";
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

		public static bool IsUnsignedIntegerType(Type t) {
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
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1807:AvoidUnnecessaryStringCreation", MessageId = "s")]
		public static bool ParseBoolean(string s) {
			if (s == null)
				return false;
			switch (s.ToLowerInvariant()) {
				case "true":
				case "1":
				case "on":
					return true;
				case "false":
				case "0":
				case "off":
					return false;
			}
			throw new SEException("'" + s + "' is not a valid boolean string (true/1/on/false/0/off).");
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1807:AvoidUnnecessaryStringCreation", MessageId = "s")]
		public static bool TryParseBoolean(string s, out bool retVal) {
			if (s == null) {
				retVal = false;
				return true;
			}
			switch (s.ToLowerInvariant()) {
				case "true":
				case "1":
				case "on":
					retVal = true;
					return true;
				case "false":
				case "0":
				case "off":
					retVal = false;
					return true;
			}
			retVal = false;
			return false;
		}

		public static bool ToBoolean(object arg) {
			if (instance == null) {
				instance = new ConvertTools();	//until TagMath replaces it.
			}
			return instance.ToBoolImpl(arg);
		}

		protected virtual bool ToBoolImpl(object arg) {
			if (arg is bool) {
				return (bool) arg;
			} else if (IsNumber(arg)) {
				return (Convert.ToInt32(arg, invariantCulture) != 0);
			} else {
				string asString = arg as string;
				if (asString != null) {
					return ParseBoolean(asString);
				} else {
					return (arg != null);
				}
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
		public static object ParseAnyNumber(string input) {
			object retVal;
			if (TryParseAnyNumber(input, out retVal)) {
				return retVal;
			}
			throw new SEException("'" + input + "' does not appear to be any kind of number.");
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public static bool TryParseAnyNumber(string input, out object retVal) {
			if (string.IsNullOrEmpty(input)) {
				retVal = null;
				return false;
			}

			try {
				Match m;
				if (TryParseSphereHex(input, out retVal)) {
					return true;
				}

				m = intRE.Match(input);
				if (m.Success) {
					string toConvert = m.Groups["value"].Value;
					try {
						retVal = int.Parse(toConvert, NumberStyles.Integer, invariantCulture);
						return true;
					} catch (OverflowException) {//try a bigger type. If this fails, we give up.
						retVal = long.Parse(toConvert, NumberStyles.Integer, invariantCulture);
						return true;
					}
				}

				m = floatRE.Match(input);
				if (m.Success) {
					retVal = double.Parse(m.Groups["value"].Value, NumberStyles.Float | NumberStyles.AllowThousands, invariantCulture);
					return true;
				}

			} catch { }

			retVal = null;
			return false;
		}


		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public static object ParseSpecificNumber(TypeCode typeCode, string input) {
			if (input.StartsWith("0")) {
				Match m = hexRE.Match(input);
				if (m.Success) {
					string toConvert = m.Groups["value"].Value;
					switch (typeCode) {
						case TypeCode.Byte:
							return Byte.Parse(toConvert, NumberStyles.HexNumber, invariantCulture);
						case TypeCode.Int16:
							return Int16.Parse(toConvert, NumberStyles.HexNumber, invariantCulture);
						case TypeCode.Int32:
							return Int32.Parse(toConvert, NumberStyles.HexNumber, invariantCulture);
						case TypeCode.Int64:
							return Int64.Parse(toConvert, NumberStyles.HexNumber, invariantCulture);
						case TypeCode.SByte:
							return SByte.Parse(toConvert, NumberStyles.HexNumber, invariantCulture);
						case TypeCode.UInt16:
							return UInt16.Parse(toConvert, NumberStyles.HexNumber, invariantCulture);
						case TypeCode.UInt32:
							return UInt32.Parse(toConvert, NumberStyles.HexNumber, invariantCulture);
						case TypeCode.UInt64:
							return UInt64.Parse(toConvert, NumberStyles.HexNumber, invariantCulture);
						case TypeCode.Decimal:
							return (Decimal) ulong.Parse(toConvert, NumberStyles.HexNumber, invariantCulture);
						case TypeCode.Double:
							return (Double) ulong.Parse(toConvert, NumberStyles.HexNumber, invariantCulture);
						case TypeCode.Single:
							return (Single) ulong.Parse(toConvert, NumberStyles.HexNumber, invariantCulture);
					}
				}
			}

			switch (typeCode) {
				case TypeCode.Byte:
					return Byte.Parse(input, NumberStyles.Integer, invariantCulture);
				case TypeCode.Decimal:
					return Decimal.Parse(input, NumberStyles.Float | NumberStyles.AllowThousands, invariantCulture);
				case TypeCode.Double:
					return Double.Parse(input, NumberStyles.Float | NumberStyles.AllowThousands, invariantCulture);
				case TypeCode.Int16:
					return Int16.Parse(input, NumberStyles.Integer, invariantCulture);
				case TypeCode.Int32:
					return Int32.Parse(input, NumberStyles.Integer, invariantCulture);
				case TypeCode.Int64:
					return Int64.Parse(input, NumberStyles.Integer, invariantCulture);
				case TypeCode.Single:
					return Single.Parse(input, NumberStyles.Float | NumberStyles.AllowThousands, invariantCulture);
				case TypeCode.SByte:
					return SByte.Parse(input, NumberStyles.Integer, invariantCulture);
				case TypeCode.UInt16:
					return UInt16.Parse(input, NumberStyles.Integer, invariantCulture);
				case TypeCode.UInt32:
					return UInt32.Parse(input, NumberStyles.Integer, invariantCulture);
				case TypeCode.UInt64:
					return UInt64.Parse(input, NumberStyles.Integer, invariantCulture);
			}
			throw new SEException("typeCode out of range");
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public static bool TryParseSpecificNumber(TypeCode typeCode, string input, out object retVal) {
			try {
				retVal = ParseSpecificNumber(typeCode, input);
				return true;
			} catch (ArgumentOutOfRangeException aoore) {
				if ("typeCode".Equals(aoore.ParamName)) {
					throw;
				}
			} catch { }
			retVal = null;
			return false;
		}

		private static bool TryParseSphereHex(object input, out object retVal) {
			string str = input as string;
			if (str != null) {
				return TryParseSphereHex(str, out retVal);
			}
			retVal = null;
			return false;
		}

		private static bool TryParseSphereHex(string input, out object retVal) {
			if (input.StartsWith("0")) {
				Match m = hexRE.Match(input);
				if (m.Success) {
					string toConvert = m.Groups["value"].Value;
					try {
						retVal = uint.Parse(toConvert, NumberStyles.HexNumber, invariantCulture);
						return true;
					} catch (OverflowException) {//try a bigger type. If this fails, we give up.
						retVal = ulong.Parse(toConvert, NumberStyles.HexNumber, invariantCulture);
						return true;
					}
				}
			}
			retVal = null;
			return false;
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
			object o;
			if (TryParseSphereHex(input, out o)) {
				return Convert.ToDouble(o, invariantCulture);
			}
			return Double.Parse(input, NumberStyles.Float | NumberStyles.AllowThousands, invariantCulture);
		}

		public static bool TryParseDouble(string input, out Double retVal) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				retVal = Convert.ToDouble(o, invariantCulture);
				return true;
			}
			return Double.TryParse(input, NumberStyles.Float | NumberStyles.AllowThousands, invariantCulture, out retVal);
		}

		public static Double ToDouble(object input) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				return Convert.ToDouble(o, invariantCulture);
			}
			return Convert.ToDouble(input, invariantCulture);
		}
		#endregion Double

		#region Single (float)
		public static Single ParseSingle(string input) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				return Convert.ToSingle(o, invariantCulture);
			}
			return Single.Parse(input, NumberStyles.Float | NumberStyles.AllowThousands, invariantCulture);
		}

		public static bool TryParseSingle(string input, out Single retVal) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				retVal = Convert.ToSingle(o, invariantCulture);
				return true;
			}
			return Single.TryParse(input, NumberStyles.Float | NumberStyles.AllowThousands, invariantCulture, out retVal);
		}

		public static Single ToSingle(object input) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				return Convert.ToSingle(o, invariantCulture);
			}
			return Convert.ToSingle(input, invariantCulture);
		}
		#endregion Single

		#region Decimal (decimal)
		public static Decimal ParseDecimal(string input) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				return Convert.ToDecimal(o, invariantCulture);
			}
			return Decimal.Parse(input, NumberStyles.Float | NumberStyles.AllowThousands, invariantCulture);
		}

		public static bool TryParseDecimal(string input, out Decimal retVal) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				retVal = Convert.ToDecimal(o, invariantCulture);
				return true;
			}
			return Decimal.TryParse(input, NumberStyles.Float | NumberStyles.AllowThousands, invariantCulture, out retVal);
		}

		public static Decimal ToDecimal(object input) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				return Convert.ToDecimal(o, invariantCulture);
			}
			return Convert.ToDecimal(input, invariantCulture);
		}
		#endregion Decimal

		#region Byte (byte)
		public static byte ParseByte(string input) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				return Convert.ToByte(o, invariantCulture);
			}
			return Byte.Parse(input, NumberStyles.Integer, invariantCulture);
		}

		public static bool TryParseByte(string input, out Byte retVal) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				retVal = Convert.ToByte(o, invariantCulture);
				return true;
			}
			return Byte.TryParse(input, NumberStyles.Integer, invariantCulture, out retVal);
		}

		public static Byte ToByte(object input) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				return Convert.ToByte(o, invariantCulture);
			}
			return Convert.ToByte(input, invariantCulture);
		}
		#endregion Byte

		#region SByte (sbyte)
		[CLSCompliant(false)]
		public static sbyte ParseSByte(string input) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				return Convert.ToSByte(o, invariantCulture);
			}
			return SByte.Parse(input, NumberStyles.Integer, invariantCulture);
		}

		[CLSCompliant(false)]
		public static bool TryParseSByte(string input, out SByte retVal) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				retVal = Convert.ToSByte(o, invariantCulture);
				return true;
			}
			return SByte.TryParse(input, NumberStyles.Integer, invariantCulture, out retVal);
		}

		[CLSCompliant(false)]
		public static SByte ToSByte(object input) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				return Convert.ToSByte(o, invariantCulture);
			}
			return Convert.ToSByte(input, invariantCulture);
		}
		#endregion SByte

		#region Int16 (short)
		public static Int16 ParseInt16(string input) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				return Convert.ToInt16(o, invariantCulture);
			}
			return Int16.Parse(input, NumberStyles.Integer, invariantCulture);
		}

		public static bool TryParseInt16(string input, out Int16 retVal) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				retVal = Convert.ToInt16(o, invariantCulture);
				return true;
			}
			return Int16.TryParse(input, NumberStyles.Integer, invariantCulture, out retVal);
		}

		public static Int16 ToInt16(object input) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				return Convert.ToInt16(o, invariantCulture);
			}
			return Convert.ToInt16(input, invariantCulture);
		}
		#endregion Int16

		#region UInt16 (ushort)
		[CLSCompliant(false)]
		public static UInt16 ParseUInt16(string input) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				return Convert.ToUInt16(o, invariantCulture);
			}
			return UInt16.Parse(input, NumberStyles.Integer, invariantCulture);
		}

		[CLSCompliant(false)]
		public static bool TryParseUInt16(string input, out UInt16 retVal) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				retVal = Convert.ToUInt16(o, invariantCulture);
				return true;
			}
			return UInt16.TryParse(input, NumberStyles.Integer, invariantCulture, out retVal);
		}

		[CLSCompliant(false)]
		public static UInt16 ToUInt16(object input) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				return Convert.ToUInt16(o, invariantCulture);
			}
			return Convert.ToUInt16(input, invariantCulture);
		}
		#endregion UInt16

		#region Int32 (int)
		public static Int32 ParseInt32(string input) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				return Convert.ToInt32(o, invariantCulture);
			}
			return Int32.Parse(input, NumberStyles.Integer, invariantCulture);
		}

		public static bool TryParseInt32(string input, out Int32 retVal) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				retVal = Convert.ToInt32(o, invariantCulture);
				return true;
			}
			return Int32.TryParse(input, NumberStyles.Integer, invariantCulture, out retVal);
		}

		public static Int32 ToInt32(object input) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				return Convert.ToInt32(o, invariantCulture);
			}
			return Convert.ToInt32(input, invariantCulture);
		}


		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public static bool TryConvertToInt32(object input, out int retVal) {
			try {
				retVal = ToInt32(input);
				return true;
			} catch (FatalException) {
				throw;
			} catch (Exception) {
				retVal = 0;
				return false;
			}
		}

		#endregion Int32

		#region UInt32 (uint)
		[CLSCompliant(false)]
		public static UInt32 ParseUInt32(string input) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				return Convert.ToUInt32(o, invariantCulture);
			}
			return UInt32.Parse(input, NumberStyles.Integer, invariantCulture);
		}

		[CLSCompliant(false)]
		public static bool TryParseUInt32(string input, out UInt32 retVal) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				retVal = Convert.ToUInt32(o, invariantCulture);
				return true;
			}
			return UInt32.TryParse(input, NumberStyles.Integer, invariantCulture, out retVal);
		}

		[CLSCompliant(false)]
		public static UInt32 ToUInt32(object input) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				return Convert.ToUInt32(o, invariantCulture);
			}
			return Convert.ToUInt32(input, invariantCulture);
		}
		#endregion UInt32

		#region Int64 (long)
		public static Int64 ParseInt64(string input) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				return Convert.ToInt64(o, invariantCulture);
			}
			return Int64.Parse(input, NumberStyles.Integer, invariantCulture);
		}

		public static bool TryParseInt64(string input, out Int64 retVal) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				retVal = Convert.ToInt64(o, invariantCulture);
				return true;
			}
			return Int64.TryParse(input, NumberStyles.Integer, invariantCulture, out retVal);
		}

		public static Int64 ToInt64(object input) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				return Convert.ToInt64(o, invariantCulture);
			}
			return Convert.ToInt64(input, invariantCulture);
		}
		#endregion Int64

		#region UInt64 (ulong)
		[CLSCompliant(false)]
		public static UInt64 ParseUInt64(string input) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				return Convert.ToUInt64(o, invariantCulture);
			}
			return UInt64.Parse(input, NumberStyles.Integer, invariantCulture);
		}

		[CLSCompliant(false)]
		public static bool TryParseUInt64(string input, out UInt64 retVal) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				retVal = Convert.ToUInt64(o, invariantCulture);
				return true;
			}
			return UInt64.TryParse(input, NumberStyles.Integer, invariantCulture, out retVal);
		}

		[CLSCompliant(false)]
		public static UInt64 ToUInt64(object input) {
			object o;
			if (TryParseSphereHex(input, out o)) {
				return Convert.ToUInt64(o, invariantCulture);
			}
			return Convert.ToUInt64(input, invariantCulture);
		}
		#endregion UInt64
	}
}