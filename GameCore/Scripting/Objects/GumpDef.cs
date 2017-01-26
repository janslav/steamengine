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

namespace SteamEngine.Scripting.Objects {
	public abstract class GumpDef : AbstractScript {
		internal GumpDef()
		{
		}

		internal GumpDef(string name)
			: base(name) {
		}

		public new static GumpDef GetByDefname(string name) {
			return AbstractScript.GetByDefname(name) as GumpDef;
		}

		internal abstract Gump InternalConstruct(Thing focused, AbstractCharacter sendTo, DialogArgs args);
	}

	/// <summary>
	/// Dialog arguments holder. It can contain arguments as tags as well as an array of (e.g. hardcoded arguments)
	/// the array's length is unmodifiable so the only way to put args into it is to put them during constructor call.
	/// Arguments added in this way should be only the compulsory dialog arguments necessary in every case (for example 
	/// label and text in the Info/Error dialog-messages). Other args should be added as tags!
	/// </summary>
	public class DialogArgs : TagHolder {
		private object[] fldArgs;

		//public DialogArgs() {
		//    this.fldArgs = new object[0]; //aspon prazdny pole, ale ne null
		//}

		public DialogArgs(params object[] args) {
			this.fldArgs = args;
		}

		public object this[int i] {
			get {
				return this.fldArgs[i];
			}
			set {
				this.fldArgs[i] = value;
			}
		}

		public object[] GetArgsArray() {
			return this.fldArgs;
		}
	}

	public class ResponseText {
		private readonly int id;
		private readonly string text;

		public ResponseText(int id, string text) {
			this.id = id;
			this.text = text;
		}

		public int Id {
			get {
				return this.id;
			}
		}

		public string Text {
			get {
				return this.text;
			}
		}
	}

	public class ResponseNumber {
		private readonly int id;
		private readonly decimal number;

		public ResponseNumber(int id, decimal number) {
			this.id = id;
			this.number = number;
		}

		public int Id {
			get {
				return this.id;
			}
		}

		public decimal Number {
			get {
				return this.number;
			}
		}
	}
}

//The 0xBF packet starts off with a cmd byte, followed by two bytes for the length.  After that is a two byte value which is a subcmd, and the message varies after that.
//General Info (5 bytes, plus specific message)
//· BYTE cmd
//· BYTE[2] len
//· BYTE[2] subcmd
//· BYTE[len-5] submessage
//
//Subcommand 4: "Close Generic GumpDef"
//
//    * BYTE[4] dialogID // which gump to destroy (second ID in 0xB0 packet)
//    * BYTE[4] buttonId // response buttonID for packet 0xB1