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
using System.Globalization;	//for CultureInfo.Invariant for String.Compare for comparing filenames.
using System.IO;
using System.Reflection;
using System.Text;

namespace SteamEngine {
	public interface IUnloadable {
		void Unload();
		bool IsUnloaded { get; }
	}

	internal class CompScriptFileCollection : ScriptFileCollection {
		internal Assembly assembly;
		internal CompScriptFileCollection(string dirPath, string extension)
			: base(dirPath, extension) {
		}
	}

	internal class ScriptFileCollection {
		private Dictionary<string, ScriptFile> scriptFiles;
		private List<string> extensions = new List<string>();
		private DirectoryInfo mainDir;
		private DateTime newestDateTime = DateTime.MinValue;
		private List<string> avoided = new List<string>();
		private long lengthSum;

		internal ScriptFileCollection(string dirPath, string extension) {
			this.mainDir = new DirectoryInfo(dirPath);
			this.extensions.Add(extension);
		}

		internal long LengthSum {
			get {
				return this.lengthSum;
			}
		}

		public void Clear() {
			this.scriptFiles.Clear();
			this.lengthSum = 0;
			this.newestDateTime = DateTime.MinValue;
		}

		internal void AddExtension(string extension) {
			this.extensions.Add(extension);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		internal void AddAvoided(string folder) {
			this.avoided.Add(folder);
		}

		internal ScriptFile AddFile(FileInfo file) {
			ScriptFile sf = new ScriptFile(file);
			if (this.scriptFiles == null) {
				this.scriptFiles = new Dictionary<string, ScriptFile>();
			}
			this.scriptFiles[file.FullName] = sf;
			CheckTime(file);
			this.lengthSum += file.Length;
			return sf;
		}

		internal bool HasFile(FileInfo file) {
			if (this.scriptFiles != null) {
				return this.scriptFiles.ContainsKey(file.FullName);
			} else {
				return false;
			}
		}

		//internal DateTime NewestDateTime {
		//    get {
		//        return newestDateTime;
		//    }
		//}

		internal ICollection<ScriptFile> GetAllFiles() {
			if (this.scriptFiles == null) {
				this.scriptFiles = new Dictionary<string, ScriptFile>();
				InitializeList(this.mainDir);
			} else {
				FindNewFiles(this.mainDir, new List<ScriptFile>());
			}
			return this.scriptFiles.Values;
		}

		internal ICollection<ScriptFile> GetChangedFiles() {
			if (this.scriptFiles == null) {
				return GetAllFiles();
			} else {
				List<ScriptFile> list = new List<ScriptFile>();
				//if (!Globals.fastStartUp) {//in fastStartUp mode we only wanna resync the files we loaded manually
				FindNewFiles(this.mainDir, list);
				//}
				FindChangedFiles(list);
				return list;
			}
		}

		internal string[] GetAllFileNames() {
			ICollection<ScriptFile> sfs = GetAllFiles();
			string[] fileNames = new string[sfs.Count];
			int i = 0;
			foreach (ScriptFile sf in sfs) {
				fileNames[i] = sf.FullName;
				i++;
			}
			return fileNames;
		}

		private void FindNewFiles(DirectoryInfo dir, List<ScriptFile> list) {
			foreach (FileSystemInfo entry in dir.GetFileSystemInfos()) {
				DirectoryInfo di = entry as DirectoryInfo;
				if (di != null) {
					if (!IsAvoidedDirectory(di)) {
						FindNewFiles(di, list);
					}
				} else {
					if (IsRightExtension(entry.Extension)) {
						FileInfo file = (FileInfo) entry;
						if (!HasFile(file)) {
							list.Add(AddFile(file));
						}
					}
				}
			}
		}

		private void FindChangedFiles(List<ScriptFile> list) {
			foreach (ScriptFile fs in this.scriptFiles.Values) {
				long prevLength = fs.Length;
				if (fs.CheckChanged()) {
					list.Add(fs);
					this.lengthSum -= prevLength;
					this.lengthSum += fs.Length;//the new length already
				}
			}
		}

		private void CheckTime(FileInfo file) {
			if (this.newestDateTime < file.LastWriteTime) {
				this.newestDateTime = file.LastWriteTime;
			}
		}

		private void InitializeList(DirectoryInfo dir) {
			foreach (FileSystemInfo entry in dir.GetFileSystemInfos()) {
				if ((entry.Attributes & FileAttributes.Directory) == FileAttributes.Directory) {
					DirectoryInfo di = (DirectoryInfo) entry;
					if (!IsAvoidedDirectory(di)) {
						InitializeList(di);
					}
				} else {
					if (IsRightExtension(entry.Extension)) {
						AddFile((FileInfo) entry);
					}
				}
			}
		}

		private bool IsAvoidedDirectory(DirectoryInfo di) {
			foreach (string avoid in this.avoided) {
				if (String.Compare(di.Name, avoid, true, CultureInfo.InvariantCulture) == 0) { //ignore case
					return true;	//skip this folder
				}
			}
			return false;
		}

		private bool IsRightExtension(string tryThis) {
			foreach (string extension in this.extensions) {
				if (StringComparer.OrdinalIgnoreCase.Equals(tryThis, extension)) {
					return true;
				}
			}
			return false;
		}
	}

	internal class ScriptFile {
		private FileInfo file;
		private FileAttributes attribs;
		private DateTime time;
		private List<IUnloadable> scripts;
		private long length;

		internal long Length {
			get {
				return length;
			}
		}

		//LastWriteTime

		//TODO: was private
		internal ScriptFile(FileInfo file) {
			this.file = file;
			this.attribs = file.Attributes;
			this.time = file.LastWriteTime;
			length = file.Length;
			scripts = new List<IUnloadable>();
		}

		internal void Add(IUnloadable script) {
			scripts.Add(script);
		}

		internal void Unload() {
			if (scripts != null) {
				foreach (IUnloadable script in scripts) {
					script.Unload();
				}
				scripts.Clear();
			}
		}

		internal bool CheckChanged() {
			file.Refresh();
			if (file.Exists) {
				if (attribs == file.Attributes) {
					if (time == file.LastWriteTime) {
						if (length == file.Length) {
							return false;
						}
					}
				}
				attribs = file.Attributes;
				time = file.LastWriteTime;
				length = file.Length;
			}
			Unload();
			return true;
		}

		internal bool Exists {
			get {
				return file.Exists;
			}
		}

		internal string FullName {
			get {
				return file.FullName;
			}
		}

		internal string Name {
			get {
				return file.Name;
			}
		}

		internal StreamReader OpenText() {
			return new StreamReader(file.FullName, Encoding.Default);
		}
	}
}
