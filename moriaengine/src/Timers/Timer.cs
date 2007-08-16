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

	public abstract class Timer {
		private static ArrayList loadedTimers = new ArrayList();

		private static SimpleQueue<Timer> newTimers = new SimpleQueue<Timer>();
		private static SimpleQueue<Timer> toUpdate = new SimpleQueue<Timer>();
		
		private static TimerPriorityQueue priorityQueue = new TimerPriorityQueue();
		
		private static Dictionary<string, ConstructorInfo> constructors = new Dictionary<string, ConstructorInfo>(StringComparer.OrdinalIgnoreCase);
		
		
		internal int index = -1; //index in the Priorityqueue. do not touch!
		public readonly TimerKey name;
		private TagHolder cont;
		public TagHolder Cont { get {
			return cont;
		} }
		internal long fireAt = -1;//internal (instead of private) because of the priorityqueue. do not touch!
		//public bool freezable = true; //will be frozen when sector is frozen?
		protected object[] args;//used by subclasses
		
		private bool isBeingUpdated = false;//is in the Updated queue - do not re-enqueue
		private bool isEnqueued = false;//is the newTimers or priorityQueue
		private bool isAssigned = false;//is registered with it`s tagholder?
	
		internal static void Clear() {
			loadedTimers.Clear();
			newTimers.Clear();
			toUpdate.Clear();
			priorityQueue.Clear();
		}
		
		public virtual void Enqueue() {
			if (cont != null) {
				if (!isAssigned) {
					cont.AddTimer(this);
					isAssigned = true;
				}
			} else {
				isAssigned = false;
			}
			if (!isEnqueued) {
				isEnqueued = true;
				newTimers.Enqueue(this);
			}
		}
		
		private static void ProcessNew() {
			while (newTimers.Count > 0) {
				//Console.WriteLine("processing timer "+newTimers.Peek());
				priorityQueue.Enqueue(newTimers.Dequeue());
			}
		}
		
		private static void ProcessUpdate() {
			while (toUpdate.Count > 0) {
				Timer timer = toUpdate.Dequeue();
				timer.isBeingUpdated = false;
				if (timer.isEnqueued) {
					priorityQueue.Update(timer);
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
					if (timer.isAssigned) {//fire only when assigned, otherwise it means it's already removed!
						timer.Remove();
						currentTimer = timer;
						timer.OnTimeout();
						currentTimer = null;
					} else {
						timer.isEnqueued = false;
					}
				} else {
					return;
				}
			}
		}
		
		public static void Cycle() { 
			ProcessUpdate();
			ProcessNew();
			ProcessTimingOut();
		}
		
		//for loading
		protected Timer(TimerKey name) {
			this.name = name;
			loadedTimers.Add(this);
		}
		
		public Timer(TagHolder obj, TimerKey name, TimeSpan time, params object[] args) {
			this.name = name;
			this.cont=obj;
			this.fireAt=Globals.TimeInTicks+HighPerformanceTimer.TimeSpanToTicks(time);
			this.args=args;
		}

		protected Timer(Timer copyFrom) {
			//copying constructor (for copying of tagholders)
			name=copyFrom.name;
			fireAt=copyFrom.fireAt;
			DeepCopyFactory.GetCopyDelayed(copyFrom.args, DelayedGetCopy_Args);
			DeepCopyFactory.GetCopyDelayed(copyFrom.cont, DelayedGetCopy_Cont);
		}

		public void DelayedGetCopy_Cont(object copy) {
			cont = (TagHolder) copy;
			Enqueue();
		}

		public void DelayedGetCopy_Args(object copy) {
			args = (object[]) copy;
		}

		public bool IsDeleted {
			get {
				return ((!isEnqueued) && (!isAssigned));
			}
		}

		public TimeSpan Interval {
			get {
				return HighPerformanceTimer.TicksToTimeSpan(fireAt - Globals.TimeInTicks);
			}
			set {
				fireAt = Globals.TimeInTicks+HighPerformanceTimer.TimeSpanToTicks(value);
				if (isEnqueued && (!isBeingUpdated)) {
					toUpdate.Enqueue(this);
					isBeingUpdated = true;
				}
			}
		}

		public double InSeconds {
			get {
				return Interval.TotalSeconds;
			}
			set {
				Interval = TimeSpan.FromSeconds(value);
			}
		}

		public void Remove() {
			isEnqueued = false;
			if (isAssigned) {
				cont.RemoveTimer(this);
				isAssigned = false;
			}
		}
		
		protected abstract void OnTimeout();

		private static Type[] timerConstructorParamTypes = new Type[] { typeof(TimerKey) };

		//called by ClassManager
		internal static void RegisterSubClass(Type type) {
			ConstructorInfo match = type.GetConstructor(timerConstructorParamTypes);

			string name = type.Name;
			if (match!=null) {
				constructors[name]=MemberWrapper.GetWrapperFor(match);
			} else {
				throw new Exception("The Timer subclass "+name+" does not have proper loading constructor");
			}
		}
		
		internal static void SaveThis(SaveStream output, Timer timer) {
			if (timer.IsDeleted) {
				Logger.WriteError("Timer "+LogStr.Ident(timer)+" is already deleted.");
				return;
			}

			output.WriteSection(timer.GetType().Name, timer.name.name);
			timer.Save(output);
		}

		internal virtual void Save(SaveStream output) {
			output.WriteValue("object",cont);
			output.WriteValue("fireat",fireAt);
			//if (!freezable) {
				//output.WriteValue("freezable", freezable);
			//}
			
			if (args!=null) {
				for (int a=0; a<args.Length; a++) {
					object o = args[a];
					if (o!=null) {
						output.WriteValue("args["+a+"]",o);
					}
				}
			}
		}

		public override string ToString() {
			StringBuilder toreturn = new StringBuilder(GetType().Name+" ").Append(name).Append(" on ");
			toreturn.Append(string.Concat("'", cont, "'"));
			//if (args!=null) {
			//	if (args.Length>0) {
			//		toreturn.Append(", args:");
			//		for (int i=0, n = args.Length; i<n; i++) {
			//			toreturn.Append(Tools.ObjToString(args[i])+", ");
			//		}
			//	}
			//}
			return toreturn.ToString();
		}

		//regular expressions for textual loading
		//args[465]
		static Regex argsRE= new Regex(@"args\s*\[\s*(?<index>\d+)\s*\]\s*$",
			RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);
		
		
		public static void StartingLoading() {
		}
		
		private static ArrayList argsList = new ArrayList();
		internal static void Load(PropsSection input) {
			ConstructorInfo constructor;
			if (!constructors.TryGetValue(input.headerType, out constructor)) {
				Logger.WriteError(input.filename,input.headerLine,"There is no proper timer constructor for this section...");
				return;
			}
			TimerKey name = TimerKey.Get(input.headerName);
			Timer timer = (Timer)constructor.Invoke(BindingFlags.Default, null, new object[] {name}, null);
			argsList.Clear();
			
			foreach (PropsLine p in input.props.Values) {
				try {
					timer.LoadLine(input.filename, p.line, p.name.ToLower(), p.value);
				} catch (FatalException) {
					throw;
				} catch (Exception ex) {
					Logger.WriteWarning(input.filename,p.line,ex);
				}
			}
			timer.args = argsList.ToArray();
		}
		
		internal virtual void LoadLine(string filename, int line, string name, string value) {
			Match m=argsRE.Match(name);
			if (m.Success) {
				int index=int.Parse(m.Groups["index"].Value, NumberStyles.Integer);
				while (argsList.Count<=index) {
					argsList.Add(null);
				}
				ObjectSaver.Load(value, new LoadObjectParam(DelayedLoad_Args), filename, line, index);
				return;
			}
			switch (name) {
				case "object": 
					ObjectSaver.Load(value, new LoadObject(DelayedLoad_Cont), filename, line);
					return;
				case "fireat":
					fireAt = ConvertTools.ParseInt64(value);
					return;
				//case "freezable":
					//freezable = (bool) ObjectSaver.Load(value);
					//return;
			}
			throw new ScriptException("Invalid data '"+LogStr.Ident(name)+" = "+LogStr.Ident(value)+"'.");
		}

		//this method checks if timers are loaded correctly (if they have obj and delay) 
		//and sends them to the priorityqueue
		internal static void LoadingFinished() {

			Logger.WriteDebug("Resolving timers");
			for (int i = 0, n = loadedTimers.Count; i<n; i++) {
				Timer timer = loadedTimers[i] as Timer;
				if (timer != null) {
					try {
						if (timer.cont != null) {
							if (timer.fireAt != -1) {
								timer.Enqueue();
							} else {
								Logger.WriteError(LogStr.Ident(timer)+" does not have the delay property loaded.");
							}
						} else {
							Logger.WriteError(LogStr.Ident(timer)+" does not have the object property loaded.");	
						}
					} catch (FatalException) {
						throw;
					} catch (Exception ex) {
						Logger.WriteError(timer.GetType().Name, timer.name, ex);
					}
				}
			}
			loadedTimers = new ArrayList();

			Logger.WriteDebug("Loaded "+loadedTimers.Count+" timers.");
		}
		
		internal static bool IsTimerName(string name) {
			return constructors.ContainsKey(name);
		}
		
		public virtual void DelayedLoad_Cont(object resolvedObject, string filename, int line) {
			cont = (TagHolder) resolvedObject;
		}
		
		public void DelayedLoad_Args(object resolvedObject, string filename, int line, object index) {
			argsList[(int) index] = resolvedObject;
		}
	}
}
