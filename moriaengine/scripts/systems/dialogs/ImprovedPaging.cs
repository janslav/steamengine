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
		int LinesCount {get;}
		[Remark("Returns an enumerator for a subset of data (for one single page)")]
		IEnumerable GetPage(int firstLineIndex, int maxLinesOnPage);
		IEnumerable GetPage(int pageNumber);
	}

	public interface IPageableCollection<T> : IPageableCollection {
		IEnumerable<T> GetPage(int firstLineIndex, int maxLinesOnPage);
		IEnumerable<T> GetPage(int pageNumber);
	}
}