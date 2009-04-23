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
using System.Reflection;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Text;
using System.Globalization;
using System.Threading;
using SteamEngine.Common;
using SteamEngine.Timers;
using SteamEngine.CompiledScripts;
using SteamEngine.CompiledScripts.ClassTemplates;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.AuxServerPipe;

namespace SteamEngine {

	[Flags]
	public enum RunLevel {
		None = 0x00, 
		Startup = 0x01,
		Running = 0x02,
		Paused = 0x04,
		AwaitingRetry = 0x08,
		Shutdown = 0x10,
		Dead = 0x20,
		Recompiling = 0x40
	}

	//mutually exclusive states:
	//Running, Dead, Shutdown, Startup, AwaitingRetry
	//Running, Dead, Recompiling
	//Running, Paused

	public static class RunLevelManager {
		private static RunLevel currentLevel = RunLevel.None;

		public static RunLevel RunLevel {
			get {
				return currentLevel;
			}
		}

		internal static void SetStartup() {
#if DEBUG
			using (new MagicObject())
#endif
 {
				currentLevel = RunLevel.Startup | (currentLevel & ~(RunLevel.AwaitingRetry | RunLevel.Running | RunLevel.Shutdown | RunLevel.Dead));
			}
		}

		internal static void SetRunning() {
#if DEBUG
			using (new MagicObject())
#endif
 {
				currentLevel = RunLevel.Running;
			}
		}

		public static bool IsRunning {
			get {
				return currentLevel == RunLevel.Running;
			}
		}

		internal static void SetPaused() {
#if DEBUG
			using (new MagicObject())
#endif
 {
				currentLevel |= RunLevel.Paused;
			}
		}

		internal static void UnsetPaused() {
#if DEBUG
			using (new MagicObject())
#endif
 {
				currentLevel &= ~RunLevel.Paused;
			}
		}

		internal static void SetAwaitingRetry() {
#if DEBUG
			using (new MagicObject())
#endif
 {
				currentLevel = RunLevel.AwaitingRetry | (currentLevel & ~(RunLevel.Startup | RunLevel.Running | RunLevel.Shutdown | RunLevel.Dead));
			}
		}

		public static bool IsAwaitingRetry {
			get {
				return (currentLevel & RunLevel.AwaitingRetry) == RunLevel.AwaitingRetry;
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		internal static void UnsetAwaitingRetry() {
#if DEBUG
			using (new MagicObject())
#endif
 {
				currentLevel &= ~RunLevel.AwaitingRetry;
			}
		}

		internal static void SetShutdown() {
#if DEBUG
			using (new MagicObject())
#endif
 {
				currentLevel = RunLevel.Shutdown | (currentLevel & ~(RunLevel.Startup | RunLevel.Running | RunLevel.AwaitingRetry | RunLevel.Dead));
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		internal static void UnsetShutdown() {
#if DEBUG
			using (new MagicObject())
#endif
 {
				currentLevel &= ~RunLevel.Shutdown;
			}
		}

		internal static void SetDead() {
#if DEBUG
			using (new MagicObject())
#endif
 {
				currentLevel = RunLevel.Dead | (currentLevel & ~(RunLevel.Startup | RunLevel.Running | RunLevel.AwaitingRetry | RunLevel.Shutdown | RunLevel.Recompiling));
			}
		}

		//not needed probably. When you're dead, you're dead :)
		//internal static void UnsetDead() {
		//}

		internal static void SetRecompiling() {
#if DEBUG
			using (new MagicObject())
#endif
 {
				currentLevel = RunLevel.Recompiling | (currentLevel & ~(RunLevel.Running | RunLevel.Dead));
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		internal static void UnsetRecompiling() {
#if DEBUG
			using (new MagicObject())
#endif
 {
				currentLevel &= ~RunLevel.Recompiling;
			}
		}

		private class MagicObject : IDisposable {
			private RunLevel runLevel = currentLevel;

			public void Dispose() {
				Logger.WriteDebug("Changing state: '" + runLevel + "' -> '" + currentLevel + "'");
			}
		}
	}
}