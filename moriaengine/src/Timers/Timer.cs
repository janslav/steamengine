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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Text;
using System.Reflection;
using System.Threading;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine.Timers {

	public abstract class Timer : IDeletable {
		private static SimpleQueue<Timer> toBeEnqueued = new SimpleQueue<Timer>();

		private static TimerPriorityQueue priorityQueue = new TimerPriorityQueue();

		private bool isDeleted = false;

		internal int index = -1; //index in the Priorityqueue. do not touch!
		internal long fireAt = -1;//internal (instead of private) because of the priorityqueue. do not touch!
		private long period = -1;//if this is > -1, it will be repeated.

		private bool isToBeEnqueued = false;

		public Timer() {
		}

		private static void ProcessToBeEnqueued() {
			while (toBeEnqueued.Count > 0) {
				Timer timer = toBeEnqueued.Dequeue();
				timer.isToBeEnqueued = false;
				if (!timer.isDeleted && timer.fireAt > -1) {
					priorityQueue.Enqueue(timer);
				}
			}
		}

		private static Timer currentTimer;
		public static Timer CurrentTimer {
			get { return currentTimer; }
		}

		private static void ProcessTimingOut() {
			long now = Globals.TimeInTicks;
			while (priorityQueue.Count > 0) {
				Timer timer = priorityQueue.Peek();
				//Console.WriteLine("TimingOut timer "+timer);
				if (timer.fireAt <= now) {
					priorityQueue.Dequeue();//we have already peeked at it
					if (timer.period > -1) {
						timer.fireAt += timer.period;
						toBeEnqueued.Enqueue(timer);
						timer.isToBeEnqueued = true;
					}
					currentTimer = timer;
					timer.OnTimeout();
					currentTimer = null;
				} else {
					return;
				}
			}
		}

		protected abstract void OnTimeout();

		public static void Cycle() {
			ProcessToBeEnqueued();
			ProcessTimingOut();
		}

		public static void Clear() {
			priorityQueue.Clear();
			toBeEnqueued.Clear();
		}

		public void Delete() {
			BeingDeleted();
			isDeleted = true;
		}

		protected virtual void BeingDeleted() {
			priorityQueue.Remove(this);
		}

		public bool IsDeleted {
			get {
				return isDeleted;
			}
		}
		
		[Summary("The time interval between invocations, using TimeSpan values to measure time intervals.")]
		[Remark("Specify negative one (-1) second (or any other negative number) to disable periodic signaling.")]
		public TimeSpan PeriodSpan {
			get {
				if (period < 0) {
					return negativeOneSecond;
				} else {
					return HighPerformanceTimer.TicksToTimeSpan(period);
				}
			}
			set {
				if (value < TimeSpan.Zero) {
					period = -1;
				} else {
					period = HighPerformanceTimer.TimeSpanToTicks(value);
				}
			}
		}

		[Summary("The time interval between invocations, in seconds. ")]
		[Remark("Specify negative one (-1) second (or any other negative TimeSpan) to disable periodic signaling.")]
		public double PeriodInSeconds {
			get {
				if (period < 0) {
					return -1;
				} else {
					return HighPerformanceTimer.TicksToSeconds(period);
				}
			}
			set {
				if (value < 0) {
					period = -1;
				} else {
					period = HighPerformanceTimer.SecondsToTicks(value);
				}
			}
		}

		[Summary("The amount of time to delay before the first invoking, in seconds.")]
		[Remark("Specify negative one (-1) second (or any other negative number) to prevent the timer from starting (i.e. to pause it). Specify 0 to start the timer immediately.")]
		public double DueInSeconds {
			get {
				return HighPerformanceTimer.TicksToSeconds(fireAt - Globals.TimeInTicks);
			}
			set {
				if (value < 0) {
					fireAt = -1;
					priorityQueue.Remove(this);
				} else {
					fireAt = Globals.TimeInTicks+HighPerformanceTimer.SecondsToTicks(value);
					priorityQueue.Remove(this);
					if (!isToBeEnqueued) {
						toBeEnqueued.Enqueue(this);
						isToBeEnqueued = true;
					}
				}
			}
		}

		public static readonly TimeSpan negativeOneSecond = TimeSpan.FromSeconds(-1);

		[Summary("The amount of time to delay before the first invoking, using TimeSpan values to measure time intervals.")]
		[Remark("Specify negative one (-1) second (or any other negative TimeSpan) to prevent the timer from starting (i.e. to pause it). Specify TimeSpan.Zero to start the timer immediately.")]
		public TimeSpan DueInSpan {
			get {
				return HighPerformanceTimer.TicksToTimeSpan(fireAt - Globals.TimeInTicks);
			}
			set {
				if (value < TimeSpan.Zero) {
					fireAt = -1;
					priorityQueue.Remove(this);
				} else {
					fireAt = Globals.TimeInTicks+HighPerformanceTimer.TimeSpanToTicks(value);
					priorityQueue.Remove(this);
					if (!isToBeEnqueued) {
						toBeEnqueued.Enqueue(this);
						isToBeEnqueued = true;
					}
				}
			}
		}

		#region save/load
		internal static void StartingLoading() {
		}
		
		[Save]
		public virtual void Save(SaveStream output) {
			if (fireAt != -1) {
				output.WriteValue("fireAt", this.fireAt);
			}
			if (period != -1) {
				output.WriteValue("period", this.period);
			}
		}

		[LoadLine]
		public virtual void LoadLine(string filename, int line, string name, string value) {
			switch (name) {
				case "fireat":
					this.fireAt = ConvertTools.ParseInt64(value);
					toBeEnqueued.Enqueue(this);
					isToBeEnqueued = true;
					break;
				case "period":
					this.period = ConvertTools.ParseInt64(value);
					break;
			}
		}
		
		

		internal static void LoadingFinished() {
			Logger.WriteDebug("Loaded "+priorityQueue.Count+" timers.");

			ProcessToBeEnqueued();
			toBeEnqueued = new SimpleQueue<Timer>();//it could have been unnecessary big...
		}
		#endregion save/load
	}
}
