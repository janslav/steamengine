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

//This basically uses Windows' high performance timer via P/Invoke, and provides methods for 
//converting to and from milliseconds, seconds, and ticks.

namespace SteamEngine.Common {
	using System;
	using System.Runtime.InteropServices;
	using System.Diagnostics;
	
	public class HighPerformanceTimer {
		[DllImport("kernel32.dll")]
		public static extern bool QueryPerformanceCounter(out long counter);
		[DllImport("kernel32.dll")]
		public static extern bool QueryPerformanceFrequency(out long frequency);
		
		private static long frequency=0;
		private static double dFrequency=0;
		private static double dmFrequency=0;

		private static double timeSpanTicksFrequency=0;
		//private static bool fallback=false;
		
		static HighPerformanceTimer() {
			bool success=QueryPerformanceFrequency(out frequency);
			if (success) {
				dFrequency=(double)frequency;
				dmFrequency=dFrequency/1000.0;
				timeSpanTicksFrequency = frequency / 10000000.0;
			} else {
				throw new FatalException("Unable to access high performance timer.");
				//fallback=true;
				//Debug.WriteLine("Unable to access high performance timer. Using a less accurate timing mechanism instead.");
				//Sanity.IfTrueSay(true, "Unable to access high performance timer.");
			}
		}

		public static long TickCount {
			get {
				//if (fallback) {
				//    return Environment.TickCount;
				//} else {
				long count;
				bool success=QueryPerformanceCounter(out count);
				if (success) {
					return count;
				} else {
					throw new FatalException("Unable to access high performance timer.");
					//Debug.WriteLine("Attempt to read the high performance timer failed.");
					//Sanity.IfTrueSay(true, "Attempt to read the high performance timer failed.");
					//return Environment.TickCount;
				}
				//}
			}
		}
		
		public static double TicksToSeconds(long count) {
			//if (fallback) {
			//    return count/1000.0;
			//} else {
				return count/dFrequency;
			//}
		}
		public static long TicksToMilliseconds(long count) {
			//if (fallback) {
			//    return count;
			//} else {
				return (long) (count/dmFrequency);
			//}
		}
		public static double TicksToDMilliseconds(long count) {
			//if (fallback) {
			//    return count;
			//} else {
				return (count/dmFrequency);
			//}
		}

		//TimeSpan ticks have 100 nanoseconds, that means frequency 10 000 000 ticks per second
		//our ticks probably have the same
		public static TimeSpan TicksToTimeSpan(long count) {
			//if (fallback) {
			//    return new TimeSpan(count);
			//} else {
			return new TimeSpan((long) (count/timeSpanTicksFrequency));
			//}
		}
		public static long TimeSpanToTicks(TimeSpan span) {
			//if (fallback) {
			//    return span.Ticks;
			//} else {
			return (long) (span.Ticks * timeSpanTicksFrequency);
			//}
		}


		public static long SecondsToTicks(double count) {
			//if (fallback) {
			//    return count*1000;
			//} else {
			return (long) (count*dFrequency);
			//}
		}
		public static long MillisecondsToTicks(long count) {
			//if (fallback) {
			//    return count;
			//} else {
			return (long) (count*dmFrequency);	//(count/1000.0)*dFrequency);
			//}
		}
		
	}
}