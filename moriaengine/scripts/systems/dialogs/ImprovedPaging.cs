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

	[Remark("The ancestor of all generated classes that manage and return the paged data to display")]
	public abstract class AbstractViewDescriptor : IPageableCollection<IDataFieldView> {
		[Remark("Implement the method to return an initialized instance of AbstractPage. This "+
				"should be then used in foreach block or somehow (as an IEnumerable)")]
		public abstract IEnumerable<IDataFieldView> GetPage(int firstLineIndex, int pageSize);

		[Remark("The OutOfBounds guard will be implemented later")]
		public abstract int LineCount { get; }

		#region IPageableCollection Members
		IEnumerable IPageableCollection.GetPage(int firstLineIndex, int maxLinesOnPage) {
			throw new System.Exception("The method or operation is not implemented.");
		}
		#endregion
	}

	[Remark("This will be used to return the desired page of objects to be displayed"+
			"it will also hold all the IDataFieldViews belonging to the ViewableClass")]
	public abstract class AbstractPage : IEnumerable<IDataFieldView>, IEnumerator<IDataFieldView> {
		protected int currentIndex;
		protected int upperBound;
		protected IDataFieldView current;

		[Remark("This method will be used by IPageableCollection to prepare the Enumerator" +
				"- set the indices")]
		public AbstractPage Initialize(int startIndex, int pageSize) {
			//initialize indices and return the current instance for usage
			this.currentIndex = startIndex;
			this.upperBound = startIndex + pageSize;
			return this;
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
			throw new System.Exception("The method or operation is not implemented.");
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