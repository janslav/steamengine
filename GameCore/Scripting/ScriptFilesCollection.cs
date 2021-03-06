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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using SteamEngine.Transactionality;

namespace SteamEngine.Scripting {
	public interface IUnloadable {
		void Unload();
		bool IsUnloaded { get; }
	}

	internal class CompiledScriptFileCollection : ScriptFileCollection {
		internal Assembly assembly;
		internal CompiledScriptFileCollection(string dirPath, string extension)
			: base(dirPath, extension) {
		}
	}

	internal class ScriptFileCollection {
		private Dictionary<string, ScriptFile> scriptFiles;
		private readonly List<string> extensions = new List<string>();
		private readonly DirectoryInfo mainDir;
		private DateTime newestDateTime = DateTime.MinValue;
		private readonly List<string> avoided = new List<string>();

		internal ScriptFileCollection(string dirPath, params string[] extensions) {
			this.mainDir = new DirectoryInfo(dirPath);
			this.extensions.AddRange(extensions);
		}

		internal long LengthSum { get; private set; }

		public void Clear() {
			Transaction.AssertNotInTransaction();
			this.scriptFiles.Clear();
			this.LengthSum = 0;
			this.newestDateTime = DateTime.MinValue;
		}

		internal void AddExtension(string extension) {
			Transaction.AssertNotInTransaction();
			this.extensions.Add(extension);
		}

		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		internal void AddAvoided(string folder) {
			Transaction.AssertNotInTransaction();
			this.avoided.Add(folder);
		}

		internal ScriptFile AddFile(FileInfo file) {
			Transaction.AssertNotInTransaction();
			var sf = new ScriptFile(file);
			if (this.scriptFiles == null) {
				this.scriptFiles = new Dictionary<string, ScriptFile>();
			}
			this.scriptFiles[file.FullName] = sf;
			this.CheckTime(file);
			this.LengthSum += file.Length;
			return sf;
		}

		internal bool HasFile(FileInfo file) {
			if (this.scriptFiles != null) {
				return this.scriptFiles.ContainsKey(file.FullName);
			}
			return false;
		}

		//internal DateTime NewestDateTime {
		//    get {
		//        return newestDateTime;
		//    }
		//}

		internal ICollection<ScriptFile> GetAllFiles() {
			Transaction.AssertNotInTransaction();
			if (this.scriptFiles == null) {
				this.scriptFiles = new Dictionary<string, ScriptFile>();
				this.InitializeList(this.mainDir);
			} else {
				this.FindNewFiles(this.mainDir, new List<ScriptFile>());
			}
			return this.scriptFiles.Values;
		}

		internal ICollection<ScriptFile> GetChangedFiles() {
			Transaction.AssertNotInTransaction();
			if (this.scriptFiles == null) {
				return this.GetAllFiles();
			}
			var list = new List<ScriptFile>();
			//if (!Globals.fastStartUp) {//in fastStartUp mode we only wanna resync the files we loaded manually
			this.FindNewFiles(this.mainDir, list);
			//}
			this.FindChangedFiles(list);
			return list;
		}

		internal string[] GetAllFileNames() {
			var sfs = this.GetAllFiles();
			var fileNames = new string[sfs.Count];
			var i = 0;
			foreach (var sf in sfs) {
				fileNames[i] = sf.FullName;
				i++;
			}
			return fileNames;
		}

		private void FindNewFiles(DirectoryInfo dir, ICollection<ScriptFile> list) {
			foreach (var entry in dir.GetFileSystemInfos()) {
				var di = entry as DirectoryInfo;
				if (di != null) {
					if (!this.IsAvoidedDirectory(di)) {
						this.FindNewFiles(di, list);
					}
				} else {
					if (this.IsRightExtension(entry.Extension)) {
						var file = (FileInfo) entry;
						if (!this.HasFile(file)) {
							list.Add(this.AddFile(file));
						}
					}
				}
			}
		}

		private void FindChangedFiles(ICollection<ScriptFile> list) {
			foreach (var fs in this.scriptFiles.Values) {
				var prevLength = fs.Length;
				if (fs.CheckChanged()) {
                    fs.Unload();
                    list.Add(fs);
					this.LengthSum -= prevLength;
					this.LengthSum += fs.Length;//the new length already
				}
			}
		}

		private void CheckTime(FileInfo file) {
			Transaction.AssertNotInTransaction();
			if (this.newestDateTime < file.LastWriteTime) {
				this.newestDateTime = file.LastWriteTime;
			}
		}

		private void InitializeList(DirectoryInfo dir) {
			foreach (var entry in dir.GetFileSystemInfos()) {
				if ((entry.Attributes & FileAttributes.Directory) == FileAttributes.Directory) {
					var di = (DirectoryInfo) entry;
					if (!this.IsAvoidedDirectory(di)) {
						this.InitializeList(di);
					}
				} else {
					if (this.IsRightExtension(entry.Extension)) {
						this.AddFile((FileInfo) entry);
					}
				}
			}
		}

		private bool IsAvoidedDirectory(DirectoryInfo di) {
			foreach (var avoid in this.avoided) {
				if (string.Compare(di.Name, avoid, true, CultureInfo.InvariantCulture) == 0) { //ignore case
					return true; //skip this folder
				}
			}
			return false;
		}

		private bool IsRightExtension(string tryThis) {
			foreach (var extension in this.extensions) {
				if (StringComparer.OrdinalIgnoreCase.Equals(tryThis, extension)) {
					return true;
				}
			}
			return false;
		}
	}
}
