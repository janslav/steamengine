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
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using SteamEngine.Common;
	
namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	public partial class MultiItemDef : ItemDef {
		private DynamicMultiItemComponentDescription[] components;

		private List<DMICDLoadHelper> loadHelpers = new List<DMICDLoadHelper>();
		internal List<MultiRegionRectangleHelper> rectangleHelpers = new List<MultiRegionRectangleHelper>();

		protected override void LoadScriptLine(string filename, int line, string param, string args) {
			switch (param) {
				case "component":
					loadHelpers.Add(new DMICDLoadHelper(filename, line, args));
					return;
				case "multiregion":
					rectangleHelpers.Add(new MultiRegionRectangleHelper(args));
					return;
			}
 			base.LoadScriptLine(filename, line, param, args);
		}

		protected override void On_Create(Thing t) {
			base.On_Create(t);
			if (components == null) {
				List<DynamicMultiItemComponentDescription> dmicds = new List<DynamicMultiItemComponentDescription>(loadHelpers.Count);
				foreach (DMICDLoadHelper helper in loadHelpers) {
					DynamicMultiItemComponentDescription dmicd = helper.Resolve();
					if (dmicd != null) {
						dmicds.Add(dmicd);
					}
				}
				components = dmicds.ToArray();
				loadHelpers = null;
			}

			MultiItem mi = (MultiItem) t;

			int n = components.Length;
			if (n > 0) {
				Item[] items = new Item[n];
				for (int i = 0; i<n; i++) {
					items[i] = components[i].Create(t.X, t.Y, t.Z, t.M); ;
				}
				mi.components = items;
			}

			if (mi.IsOnGround) {
				mi.InitMultiRegion();
			}
		}

		protected class DMICDLoadHelper {
			string filename;
			internal string args;
			int line;

			internal DMICDLoadHelper(string filename, int line, string args) {
				this.filename = filename;
				this.line = line;
				this.args = args;
			}

			internal DynamicMultiItemComponentDescription Resolve() {
				try {
					return DynamicMultiItemComponentDescription.Parse(args);
				} catch (FatalException) {
					throw;
				} catch (Exception ex) {
					Logger.WriteWarning(filename,line,ex);
				}
				return null;
			}
		}

		public override void Unload() {
			base.Unload();
			components = null;
			loadHelpers = new List<DMICDLoadHelper>();
		}


		internal class MultiRegionRectangleHelper {
			public static Regex rectRE = new Regex(@"(?<x1>-?(0x)?\d+)\s*(,|/s+)\s*(?<y1>-?(0x)?\d+)\s*(,|/s+)\s*(?<x2>-?(0x)?\d+)\s*(,|/s+)\s*(?<y2>-?(0x)?\d+)",
				RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);

			private int startX;
			private int startY;
			private int endX;
			private int endY;

			internal MultiRegionRectangleHelper(string args) {
				Match m = rectRE.Match(args);
				if (m.Success) {
					GroupCollection gc = m.Groups;
					startX = TagMath.ParseInt32(gc["x1"].Value);
					startY = TagMath.ParseInt32(gc["y1"].Value);
					endX = TagMath.ParseInt32(gc["x2"].Value);
					endY = TagMath.ParseInt32(gc["y2"].Value);
				} else {
					throw new SEException("Unrecognized Rectangle format ('"+args+"')");
				}
			}

			internal Rectangle2D CreateRect(IPoint2D p) {
				return new Rectangle2D(
					new Point2D((ushort) (p.X + startX), (ushort) (p.Y + startY)),
					new Point2D((ushort) (p.X + endX), (ushort) (p.Y + endY)));
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

			internal Item Create(ushort centerX, ushort centerY, sbyte centerZ, byte m) {
				return (Item) def.Create(
					(ushort) (centerX+offsetX),
					(ushort) (centerY+offsetY),
					(sbyte) (centerZ+offsetZ), m);
			}

			internal static Regex dmicdRE = new Regex(@"\s*(?<defname>[a-z_0-9]+)\s*(,|\s)\s*(?<x>-?\d+)\s*(,|\s)\s*(?<y>-?\d+)\s*((,|\s)\s*(?<z>-?\d+))?\s*",
				RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);

			internal static DynamicMultiItemComponentDescription Parse(string str) {
				Match m = dmicdRE.Match(str);
				if (m.Success) {
					GroupCollection gc=m.Groups;
					string defname = gc["defname"].Value;
					ItemDef def = ThingDef.Get(defname) as ItemDef;
					if (def == null) {
						uint model;
						if (ConvertTools.TryParseUInt32(defname, out model)) {
							def = ThingDef.FindItemDef(model) as ItemDef;
						}
						if (def == null) {
							throw new SEException("Unrecognized Itemdef in Component parse: '"+defname+"'");
						}
					}

					short x = TagMath.ParseInt16(gc["x"].Value);
					short y = TagMath.ParseInt16(gc["y"].Value);
					string zstr=gc["z"].Value;
					sbyte z;
					if (zstr.Length>0) {
						z = TagMath.ParseSByte(zstr);
					} else {
						z = 0;
					}
					return new DynamicMultiItemComponentDescription(def, x, y, z);
				}
				throw new SEException("Invalid input string for Component parse: '"+str+"'");
			}
		}
	}

	[Dialogs.ViewableClass]
	public partial class MultiItem : Item {
		protected MultiRegion region;

		public int ComponentCount { get {
			if (components != null) {
				return components.Length;
			}
			return 0;
		} }
		
		public Item GetComponent(int index) {
			if (components != null) {
				if ((index > 0) && (index < this.components.Length)) {
					return components[index];
				}
			}
			return null;
		}

		public override void On_Destroy() {
			base.On_Destroy();
			if (components != null) {
				foreach (Item i in components) {
					if (i != null) {
						i.Delete();
					}
				}
			}
			if (region != null) {
				region.Delete();
			}
		}

		public override void On_AfterLoad() {
			base.On_AfterLoad();
			if (this.IsOnGround) {
				InitMultiRegion();
			}
		}

		public override Region Region {
			get {
				return region;
			}
		}

		internal virtual void InitMultiRegion() {
			int n = TypeDef.rectangleHelpers.Count;
			if (n > 0) {
				Rectangle2D[] newRectangles = new Rectangle2D[n];
				for (int i = 0; i<n; i++) {
					newRectangles[i] = TypeDef.rectangleHelpers[i].CreateRect(this);
				}
				region = new MultiRegion(this, newRectangles);
			}
		}
	}

	[Dialogs.ViewableClass]
	public class MultiRegion : DynamicRegion {
		public readonly MultiItem multiItem;

		public MultiRegion() {
			throw new NotSupportedException("The constructor without paramaters is not supported");
		}

		public MultiRegion(MultiItem multiItem, Rectangle2D[] rectangles)
			: base(multiItem.P(), rectangles) {
			this.multiItem = multiItem;
		}

		public override string ToString() {
			return GetType().Name+" "+Name;
		}

		public override string Name {
			get {
				if (multiItem != null) {
					return multiItem.Name;
				}
				return "MultiRegion wihout MultiItem";
			}
			set {
				throw new NotSupportedException("Renaming MultiRegions is not supported");
			}
		}
	}
}
