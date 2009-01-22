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
using System.Reflection;
using System.Collections;
using SteamEngine.Common;

namespace SteamEngine {
	public delegate void DelayedMethod(object[] args);

	public class DelayedResolver {
		private static ArrayList delayedDelegates = new ArrayList();
		private static ArrayList delayedArgs = new ArrayList();
		private static ArrayList delayedNames = new ArrayList();
		private static ArrayList nextStageDelayedDelegates = new ArrayList();
		private static ArrayList nextStageDelayedArgs = new ArrayList();
		private static ArrayList nextStageDelayedNames = new ArrayList();
		private static bool resolving = false;
		private static int curDelayIndex = -1;

		public static void ClearAll() {
			delayedDelegates.Clear();
			delayedArgs.Clear();
			delayedNames.Clear();
			nextStageDelayedDelegates.Clear();
			nextStageDelayedArgs.Clear();
			nextStageDelayedNames.Clear();
			resolving = false;
			curDelayIndex = -1;
		}

		public static void DelayResolve(DelayedMethod dr, string name, params object[] args) {
			if (resolving) {
				nextStageDelayedDelegates.Add(dr);
				nextStageDelayedArgs.Add(args);
				nextStageDelayedNames.Add(name);
			} else {
				delayedDelegates.Add(dr);
				delayedArgs.Add(args);
				delayedNames.Add(name);
			}
		}

		public static void DelayResolve(DelayedMethod dr, params object[] args) {
			if (resolving) {
				nextStageDelayedDelegates.Add(dr);
				nextStageDelayedArgs.Add(args);
				nextStageDelayedNames.Add("unknown");
			} else {
				delayedDelegates.Add(dr);
				delayedArgs.Add(args);
				delayedNames.Add("unknown");
			}
		}

		public static void Postpone() {
			if (resolving) {
				nextStageDelayedDelegates.Add(delayedDelegates[curDelayIndex]);
				nextStageDelayedArgs.Add(delayedArgs[curDelayIndex]);
				nextStageDelayedNames.Add(delayedNames[curDelayIndex]);
			}
		}

		public static void ResolveArrayListElement(ArrayList list, object[] indices, object what) {
			//indices: [0] was the tagkey, 1..length are the indices in arraylists (in arraylists...)
			for (int a = 1; a < indices.Length && list != null; a++) {
				int argnum = (int) indices[a];
				if (a == indices.Length - 1) {
					list[argnum] = what;
				} else {
					list = list[argnum] as ArrayList;
				}
			}
		}

		public static void ResolveAll() {
			resolving = true;
			while (resolving) {
				int amt = delayedDelegates.Count;
				Logger.WriteDebug("Resolving " + amt + " delayed jobs");
				DateTime before = DateTime.Now;
				for (int a = 0; a < amt; a++) {
					if ((a % 50) == 0) {
						Logger.SetTitle("Resolving Delayed Jobs: " + ((a * 100) / amt) + " %");
					}
					curDelayIndex = a;
					DelayedMethod dm = (DelayedMethod) delayedDelegates[a];
					try {
						dm((object[]) delayedArgs[a]);
					} catch (FatalException) {
						throw;
					} catch (Exception e) {
						Logger.WriteError(e);
					}
				}
				DateTime after = DateTime.Now;
				Logger.WriteDebug("...took " + (after - before));
				Logger.SetTitle("");
				delayedDelegates.Clear();
				delayedArgs.Clear();
				delayedNames.Clear();
				if (nextStageDelayedDelegates.Count > 0) {
					if (nextStageDelayedDelegates.Count == amt) {
						string s = "Resolving stalled with " + amt + " requests left. Unable to resolve circular references: ";
						for (int a = 0; a < nextStageDelayedNames.Count; a++) {
							s += (string) nextStageDelayedNames[a] + ", ";
						}
						s = s.Substring(0, s.Length - 2) + ".";
						throw new ShowMessageAndExitException(s, "Circular References!");
					} else {
						delayedDelegates = nextStageDelayedDelegates;
						delayedArgs = nextStageDelayedArgs;
						delayedNames = nextStageDelayedNames;
						nextStageDelayedDelegates = new ArrayList();
						nextStageDelayedArgs = new ArrayList();
						nextStageDelayedNames = new ArrayList();
					}
				} else {
					resolving = false;
				}
			}
		}
	}
}