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
using SteamEngine.Common;
using System.Windows.Forms;

namespace SteamEngine {
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable"),
	System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
	public class SEException : Exception {
		private LogStr niceMessage;

		public SEException()
			: base() {
			niceMessage = LogStr.Raw(this.Message);
		}

		public SEException(string s)
			: base(s) {
			niceMessage = (LogStr) s;
		}

		public SEException(string s, Exception e)
			: base(s, e) {
			niceMessage = (LogStr) s;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public SEException(LogStr s, Exception e)
			: base(s.RawString, e) {
			niceMessage = s;
		}

		public SEException(string filename, int line, Exception e)
			: this(LogStr.FileLine(filename, line), e) {
		}

		public SEException(string filename, int line, LogStr ls)
			: this(LogStr.FileLine(filename, line) + ": " + ls) {
		}

		public SEException(string filename, int line, string str)
			: this(LogStr.FileLine(filename, line) + ": " + str) {
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public SEException(LogStr s)
			: base(s.RawString) {
			niceMessage = s;
		}

		public virtual bool StartsWithFileLine {
			get {
				return niceMessage.NiceString.StartsWith("(" + LogStr.GetStyleStartPrefix(LogStyles.FileLine));
			}
		}

		public virtual void TryAddFileLineInfo(string filename, int line) {
			if (!niceMessage.NiceString.StartsWith("(" + LogStr.GetStyleStartPrefix(LogStyles.FileLine))) {
				//if we don't already start with the file/line info, let's add it on the beginning
				niceMessage = LogStr.FileLine(filename, line) + niceMessage;
			}
		}

		public LogStr NiceMessage {
			get {
				return this.niceMessage;
			}
		}

		protected void AppendNiceMessage(string msg) {
			this.niceMessage += msg;
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
	public class ScriptException : SEException {
		public ScriptException()
			: base() {
		}
		public ScriptException(string s)
			: base(s) {
		}
		public ScriptException(LogStr s)
			: base(s) {
		}
		public ScriptException(string s, Exception e)
			: base(s, e) {
		}
		public ScriptException(LogStr s, Exception e)
			: base(s, e) {
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
	public class CallFuncException : SEException {
		public CallFuncException()
			: base() {
		}
		public CallFuncException(string s)
			: base(s) {
		}
		public CallFuncException(LogStr s)
			: base(s) {
		}
		public CallFuncException(string s, Exception e)
			: base(s, e) {
		}
		public CallFuncException(LogStr s, Exception e)
			: base(s, e) {
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
	public class TagMathException : SEException {
		public TagMathException()
			: base() {
		}
		public TagMathException(string s)
			: base(s) {
		}
		public TagMathException(LogStr s)
			: base(s) {
		}
		public TagMathException(string s, Exception e)
			: base(s, e) {
		}
		public TagMathException(LogStr s, Exception e)
			: base(s, e) {
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
	public class UnloadedException : SEException {
		public UnloadedException()
			: base() {
		}
		public UnloadedException(string s)
			: base(s) {
		}
		public UnloadedException(LogStr s)
			: base(s) {
		}
		public UnloadedException(string s, Exception e)
			: base(s, e) {
		}
		public UnloadedException(LogStr s, Exception e)
			: base(s, e) {
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
	public class DeletedException : SEException {
		public DeletedException()
			: base() {
		}
		public DeletedException(string s)
			: base(s) {
		}
		public DeletedException(LogStr s)
			: base(s) {
		}
		public DeletedException(string s, Exception e)
			: base(s, e) {
		}
		public DeletedException(LogStr s, Exception e)
			: base(s, e) {
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
	public class ServerException : SEException {
		public ServerException()
			: base() {
		}
		public ServerException(string s)
			: base(s) {
		}
		public ServerException(LogStr s)
			: base(s) {
		}
		public ServerException(string s, Exception e)
			: base(s, e) {
		}
		public ServerException(LogStr s, Exception e)
			: base(s, e) {
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
	public class UnrecognizedValueException : SEException {
		public UnrecognizedValueException()
			: base() {
		}
		public UnrecognizedValueException(string s)
			: base(s) {
		}
		public UnrecognizedValueException(LogStr s)
			: base(s) {
		}
		public UnrecognizedValueException(string s, Exception e)
			: base(s, e) {
		}
		public UnrecognizedValueException(LogStr s, Exception e)
			: base(s, e) {
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
	public class InsufficientDataException : SEException {
		public InsufficientDataException()
			: base() {
		}
		public InsufficientDataException(string s)
			: base(s) {
		}
		public InsufficientDataException(LogStr s)
			: base(s) {
		}
		public InsufficientDataException(string s, Exception e)
			: base(s, e) {
		}
		public InsufficientDataException(LogStr s, Exception e)
			: base(s, e) {
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
	public class UnsaveableTypeException : SEException {
		public UnsaveableTypeException()
			: base() {
		}
		public UnsaveableTypeException(string s)
			: base(s) {
		}
		public UnsaveableTypeException(LogStr s)
			: base(s) {
		}
		public UnsaveableTypeException(string s, Exception e)
			: base(s, e) {
		}
		public UnsaveableTypeException(LogStr s, Exception e)
			: base(s, e) {
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
	public class NonExistingObjectException : SEException {
		public NonExistingObjectException()
			: base() {
		}
		public NonExistingObjectException(string s)
			: base(s) {
		}
		public NonExistingObjectException(LogStr s)
			: base(s) {
		}
		public NonExistingObjectException(string s, Exception e)
			: base(s, e) {
		}
		public NonExistingObjectException(LogStr s, Exception e)
			: base(s, e) {
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
	public class OverrideNotAllowedException : SEException {
		public OverrideNotAllowedException()
			: base() {
		}
		public OverrideNotAllowedException(string s)
			: base(s) {
		}
		public OverrideNotAllowedException(LogStr s)
			: base(s) {
		}
		public OverrideNotAllowedException(string s, Exception e)
			: base(s, e) {
		}
		public OverrideNotAllowedException(LogStr s, Exception e)
			: base(s, e) {
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
	public class SanityCheckException : SEException {
		public SanityCheckException()
			: base() {
		}
		public SanityCheckException(string s)
			: base(s) {
		}
		public SanityCheckException(LogStr s)
			: base(s) {
		}
		public SanityCheckException(string s, Exception e)
			: base(s, e) {
		}
		public SanityCheckException(LogStr s, Exception e)
			: base(s, e) {
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
	public class NotSupportedSEException : SEException {
		public NotSupportedSEException()
			: base("Not supported") {
		}
		public NotSupportedSEException(string s)
			: base(s) {
		}
		public NotSupportedSEException(LogStr s)
			: base(s) {
		}
		public NotSupportedSEException(string s, Exception e)
			: base(s, e) {
		}
		public NotSupportedSEException(LogStr s, Exception e)
			: base(s, e) {
		}
	}

	/*
		Class:	FatalException
		Exception classes which extend this are passed up the stack and finally will be handled by SE specially.
		Note that every catch (Exception) MUST BE PRECEDED WITH catch (FatalException fe) { throw; }
		Previously it was suggested to throw fe, but that would result in the loss of the stack-trace info from before.
		We need to embed the exception into another and throw the new one, that preserves it.

		I think thats wrong :) you can just write "throw;" and it rethrows the exception correctly including the
		previons stack info. -tar
	*/
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
	public class FatalException : SEException {
		public FatalException()
			: base() {
		}
		public FatalException(string s)
			: base(s) {
		}
		public FatalException(LogStr s)
			: base(s) {
		}
		public FatalException(string s, Exception e)
			: base(s, e) {
		}
		public FatalException(LogStr s, Exception e)
			: base(s, e) {
		}
	}

	/*
		Class: SEBugException
		Thrown when a bug in SE is detected by sanity-checking code.
	*/
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
	public class SEBugException : FatalException {
		public SEBugException()
			: base() {
		}
		public SEBugException(string s)
			: base(s) {
		}
		public SEBugException(LogStr s)
			: base(s) {
		}
		public SEBugException(string s, Exception e)
			: base(s, e) {
		}
		public SEBugException(LogStr s, Exception e)
			: base(s, e) {
		}
	}

	/*
		Class: ShowMessageAndExitException
		Used to display a message in a popup message box, and then exit.
	*/
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
	public class ShowMessageAndExitException : FatalException {
		private string msg;
		private string title;

		public ShowMessageAndExitException()
			: base() {
		}
		public ShowMessageAndExitException(string s, string t)
			: base(s) {
			msg = s; title = t;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public ShowMessageAndExitException(LogStr s, string t)
			: base(s) {
			msg = s.RawString; title = t;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions")]
		public void Show() {
			IWin32Window window = Form.ActiveForm;
			if (window != null) {
				MessageBox.Show(window, msg, title);
			} else {
				MessageBox.Show(msg, title);
			}
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
	public class InvalidFilenameException : SEException {
		public InvalidFilenameException()
			: base() {
		}
		public InvalidFilenameException(string s)
			: base(s) {
		}
		public InvalidFilenameException(LogStr s)
			: base(s) {
		}
		public InvalidFilenameException(string s, Exception e)
			: base(s, e) {
		}
		public InvalidFilenameException(LogStr s, Exception e)
			: base(s, e) {
		}
	}
}
