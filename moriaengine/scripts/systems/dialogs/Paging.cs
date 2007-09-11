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

	[Remark("The ancestor of all generated classes that manage and return the paged data to display."+
			"It implements two interfaces - first for paging the data fields, second for paging the action buttons"+
			"both types will be available by two similar GetPage methods.")]
	public abstract class AbstractDataView : IPageableCollection<IDataFieldView>, IPageableCollection<ButtonDataFieldView> {
		[Remark("Implement the method to return an initialized instance of AbstractPage. This " +
				"should be then used in foreach block or somehow (as an IEnumerable) for iterating through the data fields")]
		IEnumerable<IDataFieldView> IPageableCollection<IDataFieldView>.GetPage(int firstLineIndex, int maxFieldsOnPage) {
			return DataFieldsPage(firstLineIndex, maxFieldsOnPage);
		}

		[Remark("Similar as the previous method but for iterating over the action buttons pages")]
		IEnumerable<ButtonDataFieldView> IPageableCollection<ButtonDataFieldView>.GetPage(int firstLineIndex, int maxButtonsOnPage) {
			return ActionButtonsPage(firstLineIndex, maxButtonsOnPage);
		}
		
		[Remark("This will be the real implementation of GetPage for data fields")]
		protected abstract IEnumerable<IDataFieldView> DataFieldsPage(int firstLineIndex, int maxLinesOnPage);
		[Remark("This will be the real implementation of GetPage for action buttons")]
		protected abstract IEnumerable<ButtonDataFieldView> ActionButtonsPage(int firstLineIndex, int maxLinesOnPage);

		[Remark("The OutOfBounds guard will be implemented later")]
		public abstract int LineCount { get; }

		[Remark("Name that will be displayed in the Info dialog headline - description of the infoized class")]
		public abstract string Name {get;}

		#region IPageableCollection Members
		IEnumerable IPageableCollection.GetPage(int firstLineIndex, int maxLinesOnPage) {
			throw new System.Exception("The method or operation is not implemented.");
		}
		#endregion

		[Remark("This will be used to return the desired page of objects to be displayed" +
			"it will also hold all the IDataFieldViews belonging to the ViewableClass")]
		public abstract class AbstractPage : IEnumerable<IDataFieldView>, IEnumerator<IDataFieldView> {
			//increased everytime the MoveNext method will be invoked
			protected int nextIndex;
			//this is the upper bound (the lines count) - it will never be reached (there is only upperbound-1 fields to display)
			protected int upperBound;
			//this is the current field we are displaying - it will be used in Enumerators methods
			protected IDataFieldView current;

			[Remark("This method will be used by IPageableCollection to prepare the Enumerator" +
				   "- set the indices.")]
			public AbstractPage(int startIndex, int maxFiledsOnPage) {
				//initialize indices and prepare for usage
				this.nextIndex = startIndex;
				this.upperBound = startIndex + maxFiledsOnPage;				
			}

			#region IEnumerable<IDataFieldView> Members
			[Remark("Interface method used for iterating - it will return itself, but prepared for iterating")]
			public IEnumerator<IDataFieldView> GetEnumerator() {
				return this;
			}
			#endregion

			#region IEnumerable Members
			[Remark("Interface method used for iterating - it will return itself, but prepared for iterating")]
			IEnumerator IEnumerable.GetEnumerator() {
				return this;
			}
			#endregion

			#region IEnumerator<IDataFieldView> Members
			[Remark("Yet another interface property - returns the prepared field for displaying")]
			public IDataFieldView Current {
				get {
					return current;
				}
			}
			#endregion

			#region IDisposable Members
			[Remark("Do nothing, we don't need to dispose anything in some special way")]
			public void Dispose() {
				//we dont care
			}
			#endregion

			#region IEnumerator Members

			[Remark("This is the most important method - it will ensure the iterating on the fields " +
					"belonging to the desired page")]
			public abstract bool MoveNext();

			public void Reset() {
				throw new System.Exception("The method or operation is not implemented.");
			}

			object IEnumerator.Current {
				get {
					return current;
				}
			}
			#endregion
		}			
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