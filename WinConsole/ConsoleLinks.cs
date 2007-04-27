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
using System.IO;

namespace SteamClients {
	internal enum FileLinkType {FileOnly,FileLine};

	internal class FileLink : IComparer {
		public int Start;
		public int Length;
		public FileLinkType Type;

		public int End {
			get { return Start+Length; }
		}

		public FileLink(int start,int len,FileLinkType type) {
			Start=start;
			Length=len;
			Type=type;
		}

		int IComparer.Compare(object x,object y) {
			FileLink fx=x as FileLink;
			FileLink fy=y as FileLink;
			if (fx==null || fy==null)
				return 0;
			return fx.Start-fy.Start;
		}
	}

	public class ConsoleLinks {
		private ArrayList Links;
		private ArrayList searchDirs;
		public bool recurseDirectories;

		public int Count {
			get { return Links.Count; }
		}

		public ConsoleLinks() {
			searchDirs=new ArrayList();
			Links=new ArrayList();
		}

		internal FileLink GetLinkFromPosition(int pos) {
			if (Links.Count<=0)
				return null;
			foreach (FileLink link in Links) {
				if (pos>=link.Start && pos<link.End) {
					return link;
				}
			}
			return null;
		}

		internal void RemoveAllLinks() {
			Links.Clear();
		}

		internal void AddLink(int start,int len,FileLinkType type) {
			FileLink link=new FileLink(start,len,type);
			Links.Add(link);
			Links.Sort(link);
		}

		private bool ResolveLinkHelper(string dir,ref string file) {
			string[] subdirs=Directory.GetDirectories(dir);
			foreach (string sub in subdirs) {
				string test=Path.Combine(sub,file);
				if (File.Exists(test)) {
					file=test;
					return true;
				}
				if (ResolveLinkHelper(sub,ref file)) {
					return true;
				}
			}
			return false;
		}

		internal bool ResolveLink(ref string file) {
			try {
				if (!File.Exists(file)) {
					foreach (string dir in searchDirs) {
						string test=Path.Combine(dir,file);
						if (!File.Exists(test)) {
							if (recurseDirectories) {
								if (ResolveLinkHelper(dir,ref file)) {
									return true;
								}
							}
						} else {
							file=test;
							return true;
						}
					}
					return false;
				}
				return true;
			} catch (Exception) {
				return false;
			}
		}

		internal void AddSearchDir(string dir) {
			dir=Path.GetFullPath(dir);
			if (Directory.Exists(dir)) {
				searchDirs.Add(dir);
			}

		}
	}
}
