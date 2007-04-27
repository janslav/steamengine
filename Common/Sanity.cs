/*
	Sanity.cs
    Copyright (C) 2004 Richard Dillingham and contributors (if any)

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

namespace SteamEngine.Common {
	using System;
	using System.Diagnostics;
	using System.Reflection;
	
	public delegate void WriteErrorDelegate (object o);
	
	public class Sanity {
		//These three things are needed because Sanity can't see stuff in the SteamEngine namespace,
		//which is where that stuff is. I would have used the MethodInfo, ConstructorInfo, etc, instead of
		//Reflection, but they're in the SteamEngine namespace too. :P
		private static WriteErrorDelegate WriteError = null;
		
		[Conditional("TRACE")]
		public static void Init(WriteErrorDelegate del) {
			WriteError=del;
		}
		
		//Sanity.IfTrueThrow sounds better than Debug.Assert, methinks. This throws a SanityCheckException, which should
		//be caught by what runs scripts, and marked as a probable script error, but elsewhere it should be marked
		//as a probable SE bug.
		[Conditional("TRACE")]
		public static void IfTrueThrow(bool b, string s) {
			if (b) {
				throw new SanityCheckException(s);
			}
		}
		
		[Conditional("TRACE")]
		public static void IfTrueThrow(bool b, LogStr s) {
			if (b) {
				throw new SanityCheckException(s);
			}
		}
		
		[Conditional("TRACE")]
		public static void IfTrueSay(bool b, string s) {
			if (b) {
				Logger.WriteError(s);
				StackTrace();
			}
		}
		
		[Conditional("TRACE")]
		public static void IfTrueSay(bool b, LogStr s) {
			if (b) {
				Logger.WriteError(s);
				StackTrace();
			}
		}
		
		[Conditional("TRACE")]
		public static void StackTrace() {
			Console.WriteLine("Stack Trace:");
			Console.WriteLine(Environment.StackTrace);
			/*StackTrace st = new StackTrace(true);
			for(int frameNum=0; frameNum<st.FrameCount; frameNum++) {
				StackFrame frame=st.GetFrame(frameNum);
				Console.WriteLine(" at "+frame.GetMethod()+" in "+frame.GetFileName()+": line "+frame.GetFileLineNumber());
            }*/
		}
		
		[Conditional("TRACE")]
		public static void StackTraceIf(bool condition) {
			if (condition) {
				StackTrace();
			}
		}
	}
}