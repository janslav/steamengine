using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerCederberg.Grammatica.Parser {

	public class RecursionTooDeepException : Exception {
		public RecursionTooDeepException(string s) : base(s) {

		}

		public RecursionTooDeepException(string s, Exception e) : base(s, e) {
		}
	}
}
