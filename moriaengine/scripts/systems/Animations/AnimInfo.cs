/***************************************************************************
 *                                  Body.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id: Body.cs 4 2006-06-15 04:28:39Z mark $
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	public sealed class AnimInfo {
		static Dictionary<ushort,AnimInfo> animsByModel = new Dictionary<ushort,AnimInfo>();
		private static BodyType[] bodyTable;

		ushort model;
		CharacterDef charDef;
		BodyAnimType bodyAnimType;

		//taken from runuo
		private enum BodyType : byte {
			Empty,
			Monster,
			Sea,
			Animal,
			Human,
			Equipment
		}

		//taken from runuo
		public static void Bootstrap() {
			string filename = Path.Combine(Globals.scriptsPath, "bodyTable.cfg");
	        if (File.Exists(filename)) {
	            using (StreamReader ip = new StreamReader(filename)) {
	                bodyTable = new BodyType[1000];

	                string line;

	                while ((line = ip.ReadLine()) != null) {
	                    if (line.Length == 0 || line.StartsWith("#"))
	                        continue;

	                    string[] split = line.Split('\t');

	                    try {
	                        int bodyID = int.Parse(split[0]);
	                        BodyType type = (BodyType) Enum.Parse(typeof(BodyType), split[1], true);

	                        if (bodyID >= 0 && bodyID < bodyTable.Length)
	                            bodyTable[bodyID] = type;
	                    } catch {
	                        Logger.WriteWarning("Invalid bodyTable entry:");
	                        Console.WriteLine(line);
	                    }
	                }
	            }
	        } else {
	            Logger.WriteWarning(filename+" does not exist");
	            bodyTable = new BodyType[0];
	        }
		}

		private AnimInfo(ushort model) {
			this.model = model;
		}

		public static AnimInfo Get(ushort model) {
			AnimInfo info;
			if (animsByModel.TryGetValue(model, out info)) {
				return info;
			}
			info = new AnimInfo(model);
			info.charDef = (CharacterDef) ThingDef.FindCharDef(model);
			animsByModel[model] = info;

			BodyType fromTable = BodyType.Empty;
			if (model < bodyTable.Length) {
				fromTable = bodyTable[model];
			}
			switch (fromTable) {
				case BodyType.Human:
					info.bodyAnimType = BodyAnimType.Human;
					break;
				case BodyType.Animal:
					info.bodyAnimType = BodyAnimType.Animal;
					break;
				case BodyType.Monster:
					info.bodyAnimType = BodyAnimType.Monster;
					break;
				case BodyType.Sea:
					info.bodyAnimType = BodyAnimType.SeaAnimal;
					break;
				case BodyType.Equipment:
					info.bodyAnimType = BodyAnimType.Equipment;
					break;
			}
			if (IsMaleModel(model)) {
				info.bodyAnimType |= BodyAnimType.Male;
			}
			if (IsFemaleModel(model)) {
				info.bodyAnimType |= BodyAnimType.Female;
			}
			if (IsGhostModel(model)) {
				info.bodyAnimType |= BodyAnimType.Ghost;
			}

			return info;
		}

		public ushort Model {
			get {
				return model;
			}
		}

		public uint AnimsAvailable {
			get {
				if (charDef != null) {
					return charDef.AnimsAvailable;
				}
				return uint.MaxValue;
			}
		}

		public BodyAnimType BodyAnimType {
			get {
				return bodyAnimType;
			}
		}

		public bool IsMale {
			get {
				return (bodyAnimType & BodyAnimType.Female) != BodyAnimType.Female;
			}
		}

		public bool IsFemale {
			get {
				return (bodyAnimType & BodyAnimType.Female) == BodyAnimType.Female;
			}
		}

	    public static bool IsMaleModel(int model) {
			switch (model) {
				case 183:
				case 185:
				case 400:
				case 402:
				case 605:
				case 607:
				case 750:
					return true;
			}
			return false;
	    }

	    public static bool IsFemaleModel(int model) {
			switch (model) {
				case 184:
				case 186:
				case 401:
				case 403:
				case 606:
				case 608:
				case 751:
					return true;
			}
			return false;
	    }

	    public static bool IsGhostModel(int model) {
			switch (model) {
				case 402:
				case 403:
				case 607:
				case 608:
				case 970:
					return true;
			}
			return false;
	    }
	}
}