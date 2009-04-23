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
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using SteamEngine.Timers;
using SteamEngine.Common;
//using ICSharpCode.SharpZipLib.Zip;
//using OrganicBit.Zip;

namespace SteamEngine.CompiledScripts {
	public class E_BackupsManager_Global : CompiledTriggerGroup {

		[Summary("The maximal size of the save folder on the disk (including the backups) in Megabytes. ")]
		[Remark("A positive number means absolute size, negative number means how much space should stay "
		+ "on the given disk. After each save, when the size oversteppes the number given here, "
		+ "backups are deleted, according to an algorhitm that values more recent backups "
		+ "as more important, but still keeps older backups too...")]
		public const double SavesSize = 0;


		[Summary("The maximal number of complete backups.")]
		[Remark("A zero value means any number of nackups."
		+ "After each save, when the count oversteppes the number given here, "
		+ "backups are deleted, according to an algorhitm that values more recent backups "
		+ "as more important, but still keeps older backups too...")]
		public const int MaxBackups = 50;

		private ISaveFileManager fileManager;
		private ISavePathManager pathManager;
		private int loadAttempts = 0;
		private bool isLastLoadPossibility = true;


		public E_BackupsManager_Global() {
			//zip compressed saves. the 9 stands for compression level

			//either one of these zipping "managers" should work. it's yet needed to try them out in a real 
			//shard environment to find out if they're worth it ;)
			//fileManager = new SharpZipFileManager(9);
			//fileManager = new OrganicBitZipFileManager();

			fileManager = new NormalFileManager();
			pathManager = new SavePathManager(SavesSize, MaxBackups);
		}

		public void On_BeforeSave(Globals ignored, ScriptArgs sa) {
			string path = string.Concat(sa.Argv[0]);
			if (pathManager != null) {
				path = pathManager.GetSavingPath(path);
			}
			if (fileManager != null) {
				fileManager.StartSaving(path);
			}
			sa.Argv[0] = path;
		}


		public void On_OpenSaveStream(Globals ignored, ScriptArgs sa) {
			if (fileManager != null) {
				sa.Argv[1] = fileManager.GetSaveStream(string.Concat(sa.Argv[1]));
			}
		}

		public void On_AfterSave(Globals ignored, ScriptArgs sa) {
			bool success = Convert.ToBoolean(sa.Argv[1]);
			if (success) {
				if (fileManager != null) {
					fileManager.FinishSaving();
				}
				if (pathManager != null) {
					pathManager.SavingFinished(string.Concat(sa.Argv[0]));
				}
			}
		}

		//can be called more than once, when the particular load doesn't work
		public void On_BeforeLoad(Globals ignored, ScriptArgs sa) {
			string path = string.Concat(sa.Argv[0]);
			if (pathManager != null) {
				path = pathManager.GetLoadingPath(path, loadAttempts, out isLastLoadPossibility);
			}
			if (fileManager != null) {
				fileManager.StartLoading(path);
			}
			sa.Argv[0] = path;
			loadAttempts++;
		}

		public void On_OpenLoadStream(Globals ignored, ScriptArgs sa) {
			if (fileManager != null) {
				try {
					sa.Argv[1] = fileManager.GetLoadStream(string.Concat(sa.Argv[1]));
				} catch (FileNotFoundException e) {
					if (isLastLoadPossibility) {
						throw;//this finishes the loading
					} else {
						throw new SEException("Proper file missing in given folder. Let's try an older one...", e);//keeps on loading
					}
				}
			}
		}

		public void On_AfterLoad(Globals ignored, ScriptArgs sa) {
			bool success = Convert.ToBoolean(sa.Argv[1]);
			if (success) {
				if (fileManager != null) {
					fileManager.FinishLoading();
				}
				if (pathManager != null) {
					pathManager.LoadingFinished(string.Concat(sa.Argv[0]));
				}
			}
		}

		///////////////////////////embedded classes////////////////////////////////////


		//does the work at file level
		private interface ISavePathManager {
			string GetSavingPath(string defaultPath);
			void SavingFinished(string path);
			string GetLoadingPath(string defaultPath, int attempt, out bool isLast);
			void LoadingFinished(string path);
		}

		//manages the directories
		private interface ISaveFileManager {
			void StartSaving(string path);
			TextWriter GetSaveStream(string name);
			void FinishSaving();

			void StartLoading(string path);
			TextReader GetLoadStream(string name);
			void FinishLoading();
		}

		/////////////////////the actual implementing classes///////////////////////////

		//yeah what an original name
		private class SavePathManager : ISavePathManager {
			long maxSavesSize;
			int maxBackups;
			DateTime firstTime;
			DateTime lastTime;

			bool sizeValid = false;
			long size;

			private ArrayList sortedByTime;
			private ArrayList sortedBySpan;

			internal static DateTimeFormatInfo dtfi;
			internal const string TimeFormat = "HH_mm_ss"; //_fffffff";
			internal const string DateFormat = "yyyy-MM-dd";

			static SavePathManager() {
				dtfi = new DateTimeFormatInfo();
				dtfi.ShortDatePattern = DateFormat;
				dtfi.LongDatePattern = DateFormat;
				dtfi.ShortTimePattern = TimeFormat;
				dtfi.LongTimePattern = TimeFormat;

				dtfi.TimeSeparator = "_";
				dtfi.DateSeparator = "-";
			}

			internal SavePathManager(double maxSavesSize, int maxBackups) {
				this.maxBackups = maxBackups;
				this.maxSavesSize = (long) (maxSavesSize * 1024 * 1024);
			}

			public string GetSavingPath(string defaultPath) {
				DateTime now = DateTime.Now;
				string path = Tools.CombineMultiplePaths(
					defaultPath, now.ToString(DateFormat), now.ToString(TimeFormat));
				Tools.EnsureDirectory(path);

				return path;
			}

			public void SavingFinished(string path) {
				Backup nowList = new Backup(path, DateTime.Now);//a bit unexact, because the folder was founded before the save etc, but who cares now...
				if (sortedByTime.Count > 1) {
					int index = sortedByTime.Count - 1;
					Backup wasLast = (Backup) sortedByTime[index];
					wasLast.SetSpan((Backup) sortedByTime[index - 1], nowList);
					sortedBySpan.Add(wasLast);
				}

				lastTime = nowList.time;
				sortedByTime.Add(nowList);
				sortedBySpan.Sort(Backup.SpanComparerInstance);
				sortedByTime.Sort(Backup.TimeComparerInstance);

				while (CountIsOverLimit() || SizeIsOverLimit(path)) {
					if (sortedByTime.Count < 2) {
						break;
					}
					ReduceByOne();
				}
			}

			private void ReduceByOne() {
				if (sortedByTime.Count == 2) {
					RemoveBackup((Backup) sortedByTime[0]);
					return;
				}

				double lowestPriority = double.MaxValue;
				Backup lowestPriorityBackup = null;

				TimeSpan firstToLast = lastTime - firstTime;
				for (int i = 0, n = sortedBySpan.Count; i < n; i++) {
					Backup b = (Backup) sortedBySpan[i];
					TimeSpan fromFirst = b.time - firstTime;
					double priority;
					checked {
						priority = ((double) fromFirst.Ticks / firstToLast.Ticks) * b.span.Ticks;
					}
					if (priority < lowestPriority) {
						lowestPriorityBackup = b;
						lowestPriority = priority;
					}
				}
				RemoveBackup(lowestPriorityBackup);
			}

			private void RemoveBackup(Backup toRemove) {
				int index = sortedByTime.IndexOf(toRemove);
				sortedBySpan.Remove(toRemove);
				if ((index > 1) && (index < sortedBySpan.Count - 2)) {
					ResetSpanAtIndex(index - 1);
					ResetSpanAtIndex(index + 1);
				}

				sortedBySpan.Sort(Backup.SpanComparerInstance);
				sortedByTime.Remove(toRemove);

				firstTime = ((Backup) sortedByTime[0]).time;
				lastTime = ((Backup) sortedByTime[sortedByTime.Count - 1]).time;
				toRemove.Delete();//remove from disk
				sizeValid = false;
			}

			private void ResetSpanAtIndex(int i) {
				Backup prev = (Backup) sortedByTime[i - 1];
				Backup next = (Backup) sortedByTime[i + 1];
				Backup cur = (Backup) sortedByTime[i];
				cur.SetSpan(prev, next);
			}

			public string GetLoadingPath(string defaultPath, int attempt, out bool isLast) {
				Tools.EnsureDirectory(defaultPath, true);
				InitBackupsLists(defaultPath);
				if (sortedByTime.Count == 0) {
					throw new SEException("No previous backup folder found.");
				}
				int index = sortedByTime.Count - (1 + attempt);
				isLast = false;
				if (index == 0) {
					isLast = true;
				} else if (index < 0) {
					throw new FatalException("No older backup folders to look for.");
				}
				return ((Backup) sortedByTime[index]).FullDirPath;
			}

			public void LoadingFinished(string path) {

			}

			private void RefreshSize() {
				if (!sizeValid) {
					size = 0;
					for (int i = 0, n = sortedByTime.Count; i < n; i++) {
						Backup b = (Backup) sortedByTime[i];
						size += b.Size;
					}
					sizeValid = true;
				}
			}

			private bool SizeIsOverLimit(string path) {
				if (maxSavesSize < 0) {//disk free space limit
#if MSWIN
					string deviceName = Path.GetPathRoot(Path.GetFullPath(path));
					deviceName = deviceName.Substring(0, deviceName.Length - 1);
					//System.Management.
					System.Management.ManagementObject disk = new System.Management.ManagementObject(
						"win32_logicaldisk.deviceid=\"" + deviceName + "\"");
					disk.Get();
					long bytesFree = Convert.ToInt64(disk["FreeSpace"]);
					return (bytesFree < (-maxSavesSize));
#else

#error In MONO version the device free space detection is not implemented
					//I dont say MONO can't do this, it's me who doesn't and I do not care that much :P
#endif
				} else if (maxSavesSize > 0) {
					RefreshSize();
					return (size > maxSavesSize);
				} else {
					return false;
				}
			}

			private bool CountIsOverLimit() {
				if (maxBackups == 0) {
					return false;
				}
				return (sortedByTime.Count > maxBackups);
			}

			private void InitBackupsLists(string rootPath) {
				if (sortedByTime == null) {
					sortedByTime = new ArrayList();
					sortedBySpan = new ArrayList();
					DirectoryInfo rootDir = new DirectoryInfo(rootPath);
					foreach (DirectoryInfo subdir in rootDir.GetDirectories()) {
						try {
							DateTime date = DateTime.Parse(subdir.Name, dtfi);
							foreach (DirectoryInfo savedir in subdir.GetDirectories()) {
								try {
									TimeSpan time = DateTime.Parse(savedir.Name, dtfi) - DateTime.Today;
									DateTime dateTime = date + time;
									Backup b = new Backup(savedir, dateTime);
									sortedByTime.Add(b);
								} catch (Exception) { }//unkown dir
							}
						} catch (Exception) { }//unkown dir
					}

					sortedByTime.Sort(Backup.TimeComparerInstance);
					for (int i = 1, n = sortedByTime.Count - 1; i < n; i++) {//without the first and last one
						Backup prev = (Backup) sortedByTime[i - 1];
						Backup next = (Backup) sortedByTime[i + 1];
						Backup cur = (Backup) sortedByTime[i];
						cur.SetSpan(prev, next);
						sortedBySpan.Add(cur);
					}
					if (sortedByTime.Count > 0) {
						firstTime = ((Backup) sortedByTime[0]).time;
						lastTime = ((Backup) sortedByTime[sortedByTime.Count - 1]).time;
					}

					sortedBySpan.Sort(Backup.SpanComparerInstance);
				}
			}

			private class Backup {
				internal DirectoryInfo pathInfo;
				private long size = -1;
				internal bool valid = true;//loading from us has failed in the past.
				internal DateTime time;
				internal TimeSpan span;

				internal static IComparer SpanComparerInstance = new SpanComparer();
				internal static IComparer TimeComparerInstance = new TimeComparer();

				internal Backup(string path, DateTime time) {
					this.pathInfo = new DirectoryInfo(path);
					this.time = time;
				}

				internal Backup(DirectoryInfo pathInfo, DateTime time) {
					this.pathInfo = pathInfo;
					this.time = time;
				}

				internal long Size {
					get {
						if (size == -1) {
							size = GetSizeOfDir(pathInfo);
						}
						return size;
					}
				}

				internal void SetSpan(Backup prev, Backup next) {
					this.span = next.time - prev.time;
				}

				private static long GetSizeOfDir(DirectoryInfo dir) {
					long sizeSoFar = 0;
					foreach (FileSystemInfo entry in dir.GetFileSystemInfos()) {
						if ((entry.Attributes & FileAttributes.Directory) == FileAttributes.Directory) {
							sizeSoFar += GetSizeOfDir((DirectoryInfo) entry);
						} else {
							sizeSoFar += ((FileInfo) entry).Length;
						}
					}
					return sizeSoFar;
				}

				private class SpanComparer : IComparer {
					public int Compare(object x, object y) {
						Backup a = x as Backup;
						Backup b = y as Backup;
						if ((a != null) && (b != null)) {
							return TimeSpan.Compare(a.span, b.span);
						}
						throw new SEException("Can't compare '"+x+"' to '"+y+"'");
					}
				}

				private class TimeComparer : IComparer {
					public int Compare(object x, object y) {
						Backup a = x as Backup;
						Backup b = y as Backup;
						if ((a != null) && (b != null)) {
							return DateTime.Compare(a.time, b.time);
						}
						throw new SEException("Can't compare '" + x + "' to '" + y + "'");
					}
				}

				internal string FullDirPath {
					get {
						return pathInfo.FullName;
					}
				}

				internal void Delete() {
					string oneup = Path.Combine(pathInfo.FullName, "..");
					pathInfo.Delete(true);

					DirectoryInfo dayDir = new DirectoryInfo(oneup);
					if (dayDir.Exists) {
						if (dayDir.GetFileSystemInfos().Length == 0) {
							dayDir.Delete();
						}
					}
				}
			}
		}

		//no compression. actually does the same as the core would do, only it's done here so we can control it ;)
		private class NormalFileManager : ISaveFileManager {
			private string path;

			public void StartSaving(string path) {
				this.path = path;
			}

			public TextWriter GetSaveStream(string name) {
				string fileName = Path.Combine(path, name + ".sav");
				Console.WriteLine("Saving to " + LogStr.File(fileName));
				return new StreamWriter(File.Create(fileName));
			}

			public void FinishSaving() {
			}

			public void StartLoading(string path) {
				this.path = path;
			}

			public TextReader GetLoadStream(string name) {
				string fileName = Path.Combine(path, name + ".sav");
				Console.WriteLine("Loading " + LogStr.File(fileName));
				//throw new Exception("test exc from GetLoadStream");

				return new StreamReader(File.OpenRead(fileName));
			}

			public void FinishLoading() {

			}
		}

		private class SharpZipFileManager : ISaveFileManager {
			private int compression;
			private ICSharpCode.SharpZipLib.Zip.ZipOutputStream zipWriter;
			private ICSharpCode.SharpZipLib.Zip.ZipInputStream zipReader;
			private bool openEntry;

			public SharpZipFileManager(int compression) {
				this.compression = compression;
			}

			public void StartSaving(string path) {
				string zipFileName = Path.Combine(path, "backup.zip");
				Console.WriteLine("Saving to " + LogStr.File(zipFileName));
				zipWriter = new ICSharpCode.SharpZipLib.Zip.ZipOutputStream(File.Create(zipFileName));
				zipWriter.SetLevel(compression);
				openEntry = false;
			}

			public TextWriter GetSaveStream(string name) {
				if (openEntry) {
					zipWriter.CloseEntry();
				}

				ICSharpCode.SharpZipLib.Zip.ZipEntry entry = new ICSharpCode.SharpZipLib.Zip.ZipEntry(name + ".sav");
				entry.DateTime = DateTime.Now;
				zipWriter.PutNextEntry(entry);
				openEntry = true;

				StreamWriter sw = new StreamWriter(zipWriter);
				sw.AutoFlush = true;
				return sw;
			}

			public void FinishSaving() {

				zipWriter.CloseEntry();
				zipWriter.Finish();
				zipWriter.Close();
				zipWriter = null;
			}

			public void StartLoading(string path) {
				string zipFileName = Path.Combine(path, "backup.zip");
				Console.WriteLine("Loading " + LogStr.File(zipFileName));
				zipReader = new ICSharpCode.SharpZipLib.Zip.ZipInputStream(File.OpenRead(zipFileName));
			}

			public TextReader GetLoadStream(string name) {
				zipReader.GetNextEntry();
				return new StreamReader(zipReader);
			}

			public void FinishLoading() {
				try {
					zipReader.Close();
				} catch (Exception) { }
				zipReader = null;
			}
		}

		private class OrganicBitZipFileManager : ISaveFileManager {
			private OrganicBit.Zip.ZipWriter zipWriter;
			private OrganicBit.Zip.ZipReader zipReader;

			public OrganicBitZipFileManager() {
			}

			public void StartSaving(string path) {
				string zipFileName = Path.Combine(path, "backup.zip");
				Console.WriteLine("Saving to " + LogStr.File(zipFileName));
				zipWriter = new OrganicBit.Zip.ZipWriter(zipFileName);
			}

			public TextWriter GetSaveStream(string name) {
				OrganicBit.Zip.ZipEntry entry = new OrganicBit.Zip.ZipEntry(name + ".sav");
				entry.ModifiedTime = DateTime.Now;
				zipWriter.AddEntry(entry);

				StreamWriter sw = new StreamWriter(new OBStream(zipWriter));
				sw.AutoFlush = true;
				return sw;
			}

			public void FinishSaving() {
				zipWriter.Close();
				zipWriter = null;
			}

			public void StartLoading(string path) {
				string zipFileName = Path.Combine(path, "backup.zip");
				Console.WriteLine("Loading " + LogStr.File(zipFileName));
				try {
					zipReader = new OrganicBit.Zip.ZipReader(zipFileName);
				} catch (Exception e) {
					throw new SEException(e.Message);
				}
			}

			public TextReader GetLoadStream(string name) {
				zipReader.MoveNext();
				return new StreamReader(new OBStream(zipReader));
			}

			public void FinishLoading() {
				try {
					zipReader.Close();
				} catch (Exception) { }
				zipReader = null;
			}

			private class OBStream : Stream {
				internal OrganicBit.Zip.ZipWriter zipWriter;
				internal OrganicBit.Zip.ZipReader zipReader;

				protected internal OBStream(OrganicBit.Zip.ZipWriter zipWriter) {
					this.zipWriter = zipWriter;
				}

				protected internal OBStream(OrganicBit.Zip.ZipReader zipReader) {
					this.zipReader = zipReader;
				}

				public override void Close() {
					try {
						zipWriter.Close();
					} catch (Exception) { }
					try {
						zipReader.Close();
					} catch (Exception) { }
				}

				public override void Flush() {
				}

				public override int Read([In, Out] byte[] buffer, int offset, int count) {
					return zipReader.Read(buffer, offset, count);
				}
				public override long Seek(long offset, SeekOrigin origin) {
					throw new SEException("Can't seek.");
				}

				public override void SetLength(long length) {
					throw new SEException("Can't set length");
				}
				public override void Write(byte[] buffer, int offset, int count) {
					//Console.WriteLine("Write: "+Server.utf.GetString(buffer));
					zipWriter.Write(buffer, offset, count);
				}

				public override bool CanRead {
					get {
						return (zipReader != null);
					}
				}

				public override bool CanSeek {
					get {
						return false;
					}
				}

				public override bool CanWrite {
					get {
						return (zipWriter != null);
					}
				}

				public override long Length {
					get {
						throw new SEException("Can't get length");
					}
				}

				public override long Position {
					get {
						throw new SEException("Can't get position");
					}
					set {
						throw new SEException("Can't set position");
					}
				}
			}

		}
	}
}

//			private class NoCloseStream : Stream {
//				Stream underlying;
//
//				protected internal NoCloseStream(Stream underlying) {
//					this.underlying = underlying;
//				}
//				
//				public override void Close() {
//					//yeah, nothing! the underlyiping zip file needs not to be closed
//				}
//				
//				public override void Flush() { 
//					underlying.Flush();
//				}
//				public override int Read([In, Out] byte[] buffer, int offset, int count) {
//					return underlying.Read(buffer, offset, count);
//				}
//				public override long Seek(long offset, SeekOrigin origin) {
//					return underlying.Seek(offset, origin);
//				}
//				
//				public override void SetLength(long length) {
//					underlying.SetLength(length);
//				}
//				public override void Write(byte[] buffer, int offset, int count) {
//					//Console.WriteLine("Write: "+Server.utf.GetString(buffer));
//					underlying.Write(buffer, offset, count);
//				}
//				
//				public override bool CanRead { get {
//					return underlying.CanRead;
//				} }
//				
//				public override bool CanSeek { get {
//					return underlying.CanSeek;
//				} }
//				
//				public override bool CanWrite { get {
//					return underlying.CanWrite;
//				} }
//				
//				public override long Length { get {
//					return underlying.Length;
//				} }
//				
//				public override long Position { 
//					get {
//						return underlying.Position;
//					}
//					set {
//						underlying.Position = value;
//					}
//				}
//			}
