/*
 * ParseException.cs
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

using System;
using System.Collections;
using System.Text;

namespace PerCederberg.Grammatica.Parser {

	/**
	 * A parse exception.
	 *
	 * @author   Per Cederberg, <per at percederberg dot net>
	 * @version  1.1
	 */
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
	public class ParseException : Exception {

		/**
		 * The error type enumeration.
		 */
		public enum ErrorType {

			/*
			 * The internal error type is only used to signal an error
			 * that is a result of a bug in the parser or tokenizer
			 * code.
			 */
			[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1705:LongAcronymsShouldBePascalCased", MessageId = "Member")]
			INTERNAL,

			/*
			 * The I/O error type is used for stream I/O errors. 
			 */
			IO,

			/*
			 * The unexpected end of file error type is used when end
			 * of file is encountered instead of a valid token.
			 */
			[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1705:LongAcronymsShouldBePascalCased", MessageId = "Member")]
			[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
			UNEXPECTED_EOF,

			/*
			 * The unexpected character error type is used when a
			 * character is read that isn't handled by one of the
			 * token patterns.
			 */
			[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1705:LongAcronymsShouldBePascalCased", MessageId = "Member")]
			[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
			UNEXPECTED_CHAR,

			/*
			 * The unexpected token error type is used when another
			 * token than the expected one is encountered.
			 */
			[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1705:LongAcronymsShouldBePascalCased", MessageId = "Member")]
			[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
			UNEXPECTED_TOKEN,

			/*
			 * The invalid token error type is used when a token
			 * pattern with an error message is matched. The
			 * additional information provided should contain the
			 * error message.
			 */
			[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1705:LongAcronymsShouldBePascalCased", MessageId = "Member")]
			[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
			INVALID_TOKEN,

			/*
			 * The analysis error type is used when an error is
			 * encountered in the analysis. The additional information
			 * provided should contain the error message.
			 */
			[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1705:LongAcronymsShouldBePascalCased", MessageId = "Member")]
			ANALYSIS
		}

		/**
		 * The error type.
		 */
		private ErrorType type;

		/**
		 * The additional information string.
		 */
		private string info;

		/**
		 * The additional details information. This variable is only
		 * used for unexpected token errors.
		 */
		private ArrayList details;

		/**
		 * The line number.
		 */
		private int line;

		/**
		 * The column number.
		 */
		private int column;

		/**
		 * Creates a new parse exception.
		 * 
		 * @param type           the parse error type
		 * @param info           the additional information
		 * @param line           the line number, or -1 for unknown
		 * @param column         the column number, or -1 for unknown
		 */
		public ParseException(ErrorType type,
							  string info,
							  int line,
							  int column)
			: this(type, info, null, line, column) {
		}

		/**
		 * Creates a new parse exception. This constructor is only
		 * used to supply the detailed information array, which is
		 * only used for expected token errors. The list then contains
		 * descriptions of the expected tokens.
		 * 
		 * @param type           the parse error type
		 * @param info           the additional information
		 * @param details        the additional detailed information
		 * @param line           the line number, or -1 for unknown
		 * @param column         the column number, or -1 for unknown
		 */
		public ParseException(ErrorType type,
							  string info,
							  ArrayList details,
							  int line,
							  int column) {

			this.type = type;
			this.info = info;
			this.details = details;
			this.line = line;
			this.column = column;
		}

		/**
		 * The message property. This property contains the detailed
		 * exception error message.
		 */
		public override string Message {
			get {
				return GetMessage();
			}
		}

		/**
		 * Returns the error type. 
		 * 
		 * @return the error type
		 */
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public ErrorType GetErrorType() {
			return type;
		}

		/**
		 * Returns the additional error information.
		 * 
		 * @return the additional error information
		 */
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public string GetInfo() {
			return info;
		}

		/**
		 * Returns the additional detailed error information. 
		 * 
		 * @return the additional detailed error information
		 */
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public ArrayList GetDetails() {
			return new ArrayList(details);
		}

		/**
		 * Returns the line number where the error occured.
		 * 
		 * @return the line number of the error, or 
		 *         -1 if unknown
		 */
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public int GetLine() {
			return line;
		}

		/**
		 * Returns the column number where the error occured.
		 * 
		 * @return the column number of the error, or 
		 *         -1 if unknown
		 */
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public int GetColumn() {
			return column;
		}

		/**
		 * Returns a default error message.
		 * 
		 * @return a default error message
		 */
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public string GetMessage() {
			StringBuilder buffer = new StringBuilder();

			// Add error description
			buffer.Append(GetErrorMessage());

			// Add line and column
			if (line > 0 && column > 0) {
				buffer.Append(", on line: ");
				buffer.Append(line);
				buffer.Append(" column: ");
				buffer.Append(column);
			}

			return buffer.ToString();
		}

		/**
		 * Returns the error message. This message will contain all the 
		 * information available, except for the line and column number 
		 * information.
		 * 
		 * @return the error message
		 */
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public string GetErrorMessage() {
			StringBuilder buffer = new StringBuilder();

			// Add type and info
			switch (type) {
				case ErrorType.IO:
					buffer.Append("I/O error: ");
					buffer.Append(info);
					break;
				case ErrorType.UNEXPECTED_EOF:
					buffer.Append("unexpected end of file");
					break;
				case ErrorType.UNEXPECTED_CHAR:
					buffer.Append("unexpected character '");
					buffer.Append(info);
					buffer.Append("'");
					break;
				case ErrorType.UNEXPECTED_TOKEN:
					buffer.Append("unexpected token ");
					buffer.Append(info);
#if DEBUG
					if (details != null) {
						buffer.Append(", expected ");
						if (details.Count > 1) {
							buffer.Append("one of ");
						}
						buffer.Append(GetMessageDetails());
					}
#endif
					break;
				case ErrorType.INVALID_TOKEN:
					buffer.Append(info);
					break;
				case ErrorType.ANALYSIS:
					buffer.Append(info);
					break;
				default:
					buffer.Append("internal error");
					if (info != null) {
						buffer.Append(": ");
						buffer.Append(info);
					}
					break;
			}

			return buffer.ToString();
		}

		/**
		 * Returns a string containing all the detailed information in
		 * a list. The elements are separated with a comma.
		 * 
		 * @return the detailed information string
		 */
		private string GetMessageDetails() {
			StringBuilder buffer = new StringBuilder();

			for (int i = 0; i < details.Count; i++) {
				if (i > 0) {
					buffer.Append(", ");
					if (i + 1 == details.Count) {
						buffer.Append("or ");
					}
				}
				buffer.Append(details[i]);
			}

			return buffer.ToString();
		}
	}
}
