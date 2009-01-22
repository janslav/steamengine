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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Globalization;
using PerCederberg.Grammatica.Parser;
using SteamEngine.Common;

namespace SteamEngine.LScript {
	public class OpNode_TemplateItem : OpNode, IOpNodeHolder, ITriable, IKnownRetType {
		private OpNode defNode;
		private OpNode amountNode;
		private readonly bool isnewbie;

		internal OpNode_TemplateItem(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, bool isnewbie, OpNode defNode, OpNode amountNode)
			: base(parent, filename, line, column, origNode) {
			this.isnewbie = isnewbie;
			this.defNode = defNode;
			this.amountNode = amountNode;
		}

		public virtual void Replace(OpNode oldNode, OpNode newNode) {
			if (defNode == oldNode) {
				defNode = newNode;
				return;
			}
			if (amountNode == oldNode) {
				amountNode = newNode;
				return;
			}
			throw new Exception("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
		}

		internal override object Run(ScriptVars vars) {
			Thing t = (Thing) vars.defaultObject;
			vars.self = t;

			IThingFactory tf;
			uint amount;
			try {
				tf = (IThingFactory) defNode.Run(vars);
				amount = Convert.ToUInt32(amountNode.Run(vars));
			} finally {
				vars.self = t;
			}

			try {
				AbstractItem i = t.NewItem(tf, amount);
				vars.self = i;
				return i;
			} catch (InterpreterException ie) {
				ie.AddTrace(this);
				throw ie;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating (TEMPLATE)ITEM expression",
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			Thing t = (Thing) vars.defaultObject;
			vars.self = t;
			IThingFactory tf = (IThingFactory) results[0];
			uint amount = 1;
			if (results.Length > 1) {
				amount = Convert.ToUInt32(results[1]);
			}
			try {
				AbstractItem i = t.NewItem(tf, amount);
				vars.self = i;
				return i;
			} catch (InterpreterException ie) {
				ie.AddTrace(this);
				throw ie;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating (TEMPLATE)ITEM expression",
					this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public override string ToString() {
			return (isnewbie ? "ITEMNEWBIE" : "ITEM") + "(" + defNode + ", " + amountNode + ")";
		}

		public Type ReturnType {
			get {
				return typeof(AbstractItem);
			}
		}
	}
}