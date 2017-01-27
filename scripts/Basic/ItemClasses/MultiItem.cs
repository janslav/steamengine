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
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Shielded;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public partial class MultiItemDef : ItemDef {
		private readonly Shielded<DynamicMultiItemComponentDescription[]> components = new Shielded<DynamicMultiItemComponentDescription[]>();
		private readonly ShieldedSeq<DMICDLoadHelper> loadHelpers = new ShieldedSeq<DMICDLoadHelper>();
		private readonly ShieldedSeq<MultiRegionRectangleHelper> rectangleHelpers = new ShieldedSeq<MultiRegionRectangleHelper>();

		internal IEnumerable<MultiRegionRectangleHelper> RectangleHelpers {
			get {
				SeShield.AssertInTransaction();
				return this.rectangleHelpers;
			}
		}

		protected override void LoadScriptLine(string filename, int line, string param, string args) {
			switch (param) {
				case "component":
					this.loadHelpers.Add(new DMICDLoadHelper(filename, line, args));
					return;
				case "multiregion":
					this.rectangleHelpers.Add(new MultiRegionRectangleHelper(args));
					return;
			}
			base.LoadScriptLine(filename, line, param, args);
		}

		protected override void On_Create(Thing t) {
			SeShield.AssertInTransaction();

			base.On_Create(t);
			if (this.components.Value == null) {
				var dmicds = new List<DynamicMultiItemComponentDescription>();
				foreach (var helper in this.loadHelpers) {
					var dmicd = helper.Resolve();
					if (dmicd != null) {
						dmicds.Add(dmicd);
					}
				}
				this.components.Value = dmicds.ToArray();
				this.loadHelpers.Clear();
			}

			var mi = (MultiItem) t;

			var componentsValue = this.components.Value;
			var n = componentsValue.Length;
			if (n > 0) {
				var items = new Item[n];
				for (var i = 0; i < n; i++) {
					items[i] = componentsValue[i].Create(t.X, t.Y, t.Z, t.M); ;
				}
				mi.components = items;
			}

			if (mi.IsOnGround) {
				mi.InitMultiRegion();
			}
		}

		protected class DMICDLoadHelper {
			private readonly string filename;
			private readonly string args;
			private readonly int line;

			internal DMICDLoadHelper(string filename, int line, string args) {
				this.filename = filename;
				this.line = line;
				this.args = args;
			}

			internal DynamicMultiItemComponentDescription Resolve() {
				try {
					return DynamicMultiItemComponentDescription.Parse(this.args);
				} catch (FatalException) {
					throw;
				} catch (TransException) {
					throw;
				} catch (Exception ex) {
					Logger.WriteWarning(this.filename, this.line, ex);
				}
				return null;
			}
		}

		public override void Unload() {
			SeShield.AssertInTransaction();

			base.Unload();
			this.components.Value = null;
			this.loadHelpers.Clear();
		}

		internal class MultiRegionRectangleHelper {
			public static readonly Regex rectRe = new Regex(@"(?<x1>-?(0x)?\d+)\s*(,|/s+)\s*(?<y1>-?(0x)?\d+)\s*(,|/s+)\s*(?<x2>-?(0x)?\d+)\s*(,|/s+)\s*(?<y2>-?(0x)?\d+)",
				RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

			private readonly int startX;
			private readonly int startY;
			private readonly int endX;
			private readonly int endY;

			internal MultiRegionRectangleHelper(string args) {
				var m = rectRe.Match(args);
				if (m.Success) {
					var gc = m.Groups;
					this.startX = ConvertTools.ParseInt32(gc["x1"].Value);
					this.startY = ConvertTools.ParseInt32(gc["y1"].Value);
					this.endX = ConvertTools.ParseInt32(gc["x2"].Value);
					this.endY = ConvertTools.ParseInt32(gc["y2"].Value);
				} else {
					throw new SEException("Unrecognized Rectangle format ('" + args + "')");
				}
			}

			internal ImmutableRectangle CreateRect(IPoint2D p) {
				return new ImmutableRectangle((ushort) (p.X + this.startX), (ushort) (p.Y + this.startY),
											  (ushort) (p.X + this.endX), (ushort) (p.Y + this.endY));
			}
		}


		protected class DynamicMultiItemComponentDescription {
			public readonly short offsetX;
			public readonly short offsetY;
			public readonly sbyte offsetZ;
			public readonly ItemDef def;

			internal DynamicMultiItemComponentDescription(ItemDef def, short offsetX, short offsetY, sbyte offsetZ) {
				this.offsetX = offsetX;
				this.offsetY = offsetY;
				this.offsetZ = offsetZ;
				this.def = def;
			}

			internal Item Create(int centerX, int centerY, int centerZ, byte m) {
				return (Item) this.def.Create(
					centerX + this.offsetX,
					centerY + this.offsetY,
					centerZ + this.offsetZ, m);
			}

			internal static Regex dmicdRE = new Regex(@"\s*(?<defname>[a-z_0-9]+)\s*(,|\s)\s*(?<x>-?\d+)\s*(,|\s)\s*(?<y>-?\d+)\s*((,|\s)\s*(?<z>-?\d+))?\s*",
				RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

			internal static DynamicMultiItemComponentDescription Parse(string str) {
				var m = dmicdRE.Match(str);
				if (m.Success) {
					var gc = m.Groups;
					var defname = gc["defname"].Value;
					var def = GetByDefname(defname) as ItemDef;
					if (def == null) {
						int model;
						if (ConvertTools.TryParseInt32(defname, out model)) {
							def = FindItemDef(model) as ItemDef;
						}
						if (def == null) {
							throw new SEException("Unrecognized Itemdef in Component parse: '" + defname + "'");
						}
					}

					var x = ConvertTools.ParseInt16(gc["x"].Value);
					var y = ConvertTools.ParseInt16(gc["y"].Value);
					var zstr = gc["z"].Value;
					sbyte z;
					if (zstr.Length > 0) {
						z = ConvertTools.ParseSByte(zstr);
					} else {
						z = 0;
					}
					return new DynamicMultiItemComponentDescription(def, x, y, z);
				}
				throw new SEException("Invalid input string for Component parse: '" + str + "'");
			}
		}
	}

	[ViewableClass]
	public partial class MultiItem : Item {
		protected MultiRegion region;

		public int ComponentCount {
			get {
				if (this.components != null) {
					return this.components.Length;
				}
				return 0;
			}
		}

		public Item GetComponent(int index) {
			if (this.components != null) {
				if ((index > 0) && (index < this.components.Length)) {
					return this.components[index];
				}
			}
			return null;
		}

		public override void On_Destroy() {
			base.On_Destroy();
			if (this.components != null) {
				foreach (var i in this.components) {
					i?.Delete();
				}
			}
			this.region?.Delete();
		}

		public override void On_AfterLoad() {
			base.On_AfterLoad();
			if (this.IsOnGround) {
				this.InitMultiRegion();
			}
		}

		public override Region Region => this.region;

		internal virtual void InitMultiRegion() {
			SeShield.AssertInTransaction();

			this.region = new MultiRegion(this, this.TypeDef.RectangleHelpers.Select(h => h.CreateRect(this)).ToArray());
			// TODO - pouzit region.Place(P()) a v pripade false poresit co delat s neuspechem!!
		}
	}

	[ViewableClass]
	public class MultiRegion : DynamicRegion {
		public readonly MultiItem multiItem;

		public MultiRegion() {
			throw new SEException("The constructor without paramaters is not supported");
		}

		public MultiRegion(MultiItem multiItem, ImmutableRectangle[] rectangles)
			: base(rectangles) {
			this.multiItem = multiItem;
		}

		public override string ToString() {
			return this.GetType().Name + " " + this.Name;
		}

		public override string Name {
			get {
				if (this.multiItem != null) {
					return this.multiItem.Name;
				}
				return "MultiRegion wihout MultiItem";
			}
			//			set {
			//				throw new SEException("Renaming MultiRegions is not supported");
			//			}
		}
	}
}
