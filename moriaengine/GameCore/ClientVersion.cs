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
using System.Text;
using System.Text.RegularExpressions;
using SteamEngine.Common;


namespace SteamEngine {
	public class ClientVersion {
		private static Hashtable byVersionString = new Hashtable();

		public static ClientVersion Get(string versionString) {
			ClientVersion cliver = (ClientVersion) byVersionString[versionString];
			if (cliver == null) {
				cliver = new ClientVersion(versionString);
				byVersionString[versionString] = cliver;
			}

			return cliver;
		}

		public static ClientVersion nullValue = new ClientVersion();


		public readonly ClientType type;
		public readonly string versionString;//what we got from the client
		private OSI2DVersionNumber osi2dVerNum = OSI2DVersionNumber.nullValue;
		//int palanthirVerNum = 0;

		//flags:
		public readonly bool displaySkillCaps = false;
		public readonly bool aosToolTips = false;
		public readonly bool oldAosToolTips = false;
		public readonly bool needsNewSpellbook = false;

		private ClientVersion() {
			type = ClientType.Unknown;
		}

		public static Regex osi2dCliVerRE = new Regex(@"^(?<major>[0-9]+)\.(?<minor>[0-9]+)\.(?<revision>[0-9]+)(?<letter>[a-z])$",
			RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);

		private ClientVersion(string versionString) {
			this.versionString = versionString;
			try {
				Match m = osi2dCliVerRE.Match(versionString);
				if (m.Success) {
					byte major = byte.Parse(m.Groups["major"].Value);
					byte minor = byte.Parse(m.Groups["minor"].Value);
					byte revision = byte.Parse(m.Groups["revision"].Value);
					char letter = m.Groups["letter"].Value[0];
					this.type = ClientType.OSI2D;
					this.osi2dVerNum = new OSI2DVersionNumber(major, minor, revision, letter);

					int number = osi2dVerNum.comparableNumber;
					if (number >= 3000803) {//client 3.0.8d
						this.displaySkillCaps = true;
					}
					if (number > 3000816) {//client 3.0.8o
						this.aosToolTips = true;
						if (number < 4000500) {//client 4.0.5? not sure here.
							this.oldAosToolTips = true;
						}
					}
					this.needsNewSpellbook = number >= 4000000;
				}
			} catch (Exception e) {
				Logger.WriteWarning("While evaluating '"+LogStr.Ident(versionString)+"' as client version string", e);
			}

		}

		public OSI2DVersionNumber OSI2DVerNum {
			get {
				return osi2dVerNum;
			}
		}

		//public int PalanthirVerNum { get {
		//	return palanthirVerNum;
		//} }

		public override string ToString() {
			switch (type) {

				case ClientType.OSI2D:
					return "OSI2D Client "+osi2dVerNum;

				//case ClientType.Iris:
				//case ClientType.OSI3D:
				//case ClientType.OSIGod:
				//case ClientType.PlayUO:
				//case ClientType.Palanthir:
				//case ClientType.Unknown:
			}
			return "not implemented client version: "+versionString;
		}
	}

	public class OSI2DVersionNumber {
		public readonly byte major;
		public readonly byte minor;
		public readonly byte revision;
		public readonly char letter;
		public readonly int comparableNumber;

		public static OSI2DVersionNumber nullValue = new OSI2DVersionNumber(0, 0, 0, 'a');

		public OSI2DVersionNumber(byte major, byte minor, byte revision, char letter) {
			this.major = major;
			this.minor = minor;
			this.revision = revision;
			this.letter = char.ToLower(letter);

			comparableNumber = major * 1000000;
			comparableNumber += minor * 10000;
			comparableNumber += revision * 100;
			comparableNumber += ((byte) this.letter) - valueOfA;
		}

		const byte valueOfA = (byte) 'a';

		public override string ToString() {
			return string.Concat(major, ".", minor, ".", revision, letter);
		}
	}
}