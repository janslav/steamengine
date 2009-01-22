/*
 * AlternativeElement.cs
 *
 * This work is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published
 * by the Free Software Foundation; either version 2 of the License,
 * or (at your option) any later version.
 *
 * This work is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software 
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307
 * USA
 *
 * As a special exception, the copyright holders of this library give
 * you permission to link this library with independent modules to
 * produce an executable, regardless of the license terms of these
 * independent modules, and to copy and distribute the resulting
 * executable under terms of your choice, provided that you also meet,
 * for each linked independent module, the terms and conditions of the
 * license of that module. An independent module is a module which is
 * not derived from or based on this library. If you modify this
 * library, you may extend this exception to your version of the
 * library, but you are not obligated to do so. If you do not wish to
 * do so, delete this exception statement from your version.
 *
 * Copyright (c) 2003 Per Cederberg. All rights reserved.
 */

using System.IO;

namespace PerCederberg.Grammatica.Parser.RE {

	/**
	 * A regular expression alternative element. This element matches
	 * the longest alternative element.
	 *
	 * @author   Per Cederberg, <per at percederberg dot net>
	 * @version  1.0
	 */
	internal class AlternativeElement : Element {

		/**
		 * The first alternative element.
		 */
		private Element elem1;

		/**
		 * The second alternative element.
		 */
		private Element elem2;

		/**
		 * Creates a new alternative element.
		 * 
		 * @param first          the first alternative
		 * @param second         the second alternative
		 */
		public AlternativeElement(Element first, Element second) {
			elem1 = first;
			elem2 = second;
		}

		/**
		 * Creates a copy of this element. The copy will be an
		 * instance of the same class matching the same strings.
		 * Copies of elements are necessary to allow elements to cache
		 * intermediate results while matching strings without
		 * interfering with other threads.
		 * 
		 * @return a copy of this element
		 */
		public override object Clone() {
			return new AlternativeElement(elem1, elem2);
		}

		/**
		 * Returns the length of a matching string starting at the
		 * specified position. The number of matches to skip can also
		 * be specified, but numbers higher than zero (0) cause a
		 * failed match for any element that doesn't attempt to
		 * combine other elements.
		 *
		 * @param m              the matcher being used 
		 * @param str            the string to match
		 * @param start          the starting position
		 * @param skip           the number of matches to skip
		 * 
		 * @return the length of the longest matching string, or
		 *         -1 if no match was found
		 */
		public override int Match(Matcher m,
								  string str,
								  int start,
								  int skip) {

			int length = 0;
			int length1 = -1;
			int length2 = -1;
			int skip1 = 0;
			int skip2 = 0;

			while (length >= 0 && skip1 + skip2 <= skip) {
				length1 = elem1.Match(m, str, start, skip1);
				length2 = elem2.Match(m, str, start, skip2);
				if (length1 >= length2) {
					length = length1;
					skip1++;
				} else {
					length = length2;
					skip2++;
				}
			}
			return length;
		}

		/**
		 * Prints this element to the specified output stream.
		 * 
		 * @param output         the output stream to use
		 * @param indent         the current indentation
		 */
		public override void PrintTo(TextWriter output, string indent) {
			output.WriteLine(indent + "Alternative 1");
			elem1.PrintTo(output, indent + "  ");
			output.WriteLine(indent + "Alternative 2");
			elem2.PrintTo(output, indent + "  ");
		}
	}
}
