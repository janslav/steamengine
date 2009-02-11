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

namespace SteamEngine.LScript {
	public interface IOpNodeHolder {
		void Replace(OpNode oldNode, OpNode newNode);
	}

	public interface ITriable {
		object TryRun(ScriptVars vars, object[] results);
	}

	//a little optimisation...some opnodes may know their return type
	public interface IKnownRetType {
		Type ReturnType {
			get;
		}
	}

	public abstract class OpNode {
		internal readonly string filename;
		internal int line;
		internal int column;
		protected Node origNode; //to get the string from
		internal IOpNodeHolder parent;

		protected OpNode(IOpNodeHolder parent, string filename, int line, int column, Node origNode) {
			this.filename = filename;
			this.line = line;
			this.column = column;
			this.parent = parent;
			this.origNode = origNode;
		}

		//helper method
		protected void ReplaceSelf(OpNode newNode) {
			//Console.WriteLine("Replacing self - {0} (type {1}) by {2} ({3}), parent {4} ({5})", this, this.GetType(), newNode, newNode.GetType(), parent, parent.GetType());
			newNode.parent = parent; //just to be sure
			parent.Replace(this, newNode);
		}

		internal static bool IsType(Node node, StrictConstants type) {
			if (node != null) {
				if (node.GetId() == (int) type) {
					return true;
				}
			}
			return false;
		}

		internal static bool IsAssigner(Node node) {
			if ((IsType(node, StrictConstants.WHITE_SPACE_ASSIGNER))
					|| (IsType(node, StrictConstants.OPERATOR_ASSIGNER))) {
				return true;
			}
			return false;
		}

		internal virtual string OrigString {
			get {
				return LScript.GetString(origNode);
			}
		}

		internal LScriptHolder ParentScriptHolder {
			get {//"topobj" of a node
				LScriptHolder parentAsHolder = parent as LScriptHolder;
				if (parentAsHolder != null) {
					return parentAsHolder;
				}
				OpNode parentAsOpNode = parent as OpNode;
				if (parentAsOpNode != null) {
					return parentAsOpNode.ParentScriptHolder;
				}
				throw new SEException("The parent is nor OpNode nor LScriptHolder... this can not happen?!");
			}
		}

		internal abstract object Run(ScriptVars vars);
	}
}