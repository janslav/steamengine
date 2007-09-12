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
using SteamEngine.Common;
using System.Collections;
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts.Dialogs {
	[Remark("Data source for all paging dialogs (those, where there are displayed larger "+
			"collections of data that need to be divided to more pages")]
	public interface IPageableCollection {
		[Remark("Field for checking the OutOfBounds problem :)")]
		int LineCount { get; }
		[Remark("Returns an enumeration for a subset of data (for one single page)")]
		IEnumerable GetPage(int firstLineIndex, int maxLinesOnPage);		
	}

	public interface IPageableCollection<T> : IPageableCollection {
		new IEnumerable<T> GetPage(int firstLineIndex, int maxLinesOnPage);		
	}

	[Remark("Wrapper class for List<T> which allows us to use the paging")]
	public class PageableList<T> : IPageableCollection<T> {
		private List<T> wrappedList;

		public PageableList(List<T> wrappedList) {
			this.wrappedList = wrappedList;
		}

		#region IPageableCollection<T> Members
		public IEnumerable<T> GetPage(int firstLineIndex, int maxLinesOnPage) {
			return new Page(firstLineIndex, maxLinesOnPage, wrappedList);			
		}
		#endregion

		#region IPageableCollection Members
		public int LineCount {
			get {
				return wrappedList.Count;
			}
		}

		IEnumerable IPageableCollection.GetPage(int firstLineIndex, int maxLinesOnPage) {
			return new Page(firstLineIndex, maxLinesOnPage, wrappedList);			
		}
		#endregion

		[Remark("Inner class for getting the exact range from the wrappedList. Similar to AbstractPage.")]
		public class Page : IEnumerator<T>, IEnumerable<T> {
			private int nextIndex, upperBound;
			private List<T> wrappedList;
			private T current;
			public Page(int startIndex, int pageSize, List<T> wrappedList) {
				this.nextIndex = startIndex; //index of the first item from the list tht will be returned
				this.upperBound = startIndex + pageSize; //index of the first item from the list tht will NOT be returned (the upper bound)
				this.wrappedList = wrappedList; //the list itself, for getting the items :)
			}

			#region IEnumerator Members
			object IEnumerator.Current {
				get {
					return current;
				}
			}

			[Remark("The main iteration method. Make checks if we are not exceeding the page boundaries"+
					"and check also if we are not exceeding the wrapped list boundaries...")]
			public bool MoveNext() {
				if(nextIndex < upperBound && nextIndex < wrappedList.Count) {
					current = wrappedList[nextIndex];
					nextIndex++; //prepare index for the next iteration round
					return true;
				}
				return false;
			}

			public void Reset() {
				throw new System.Exception("The method or operation is not implemented.");
			}
			#endregion

			#region IEnumerator<T> Members
			public T Current {
				get {
					return current;
				}
			}
			#endregion

			#region IDisposable Members
			public void Dispose() {
				//we dont care
			}
			#endregion

			#region IEnumerable<T> Members
			public IEnumerator<T> GetEnumerator() {
				return this;
			}
			#endregion

			#region IEnumerable Members
			IEnumerator IEnumerable.GetEnumerator() {
				return this;
			}
			#endregion
		}
	}
}