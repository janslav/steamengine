//------------------------------------------------------------------------------
// <auto-generated>
//     Tento kód byl generován nástrojem.
//     Verze modulu runtime:2.0.50727.3603
//
//     Změny tohoto souboru mohou způsobit nesprávné chování a budou ztraceny,
//     dojde-li k novému generování kódu.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SteamEngine.CompiledScripts {
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using SteamEngine;
	using SteamEngine.Timers;
	using SteamEngine.Persistence;
	using SteamEngine.Common;
	
	
	public partial class PotionDef : ItemDef {
		
		private FieldValue emptyFlask;
		
		public PotionDef(String defname, String filename, Int32 headerLine) : 
				base(defname, filename, headerLine) {
			this.emptyFlask = this.InitTypedField("emptyFlask", null, typeof(ItemDef));
		}
		
		public ItemDef EmptyFlask {
			get {
				return ((ItemDef)(this.emptyFlask.CurrentValue));
			}
			set {
				this.emptyFlask.CurrentValue = value;
			}
		}
		
		protected override SteamEngine.Thing CreateImpl() {
			return new Potion(this);
		}
		
		public new static void Bootstrap() {
			SteamEngine.ThingDef.RegisterThingDef(typeof(PotionDef), typeof(Potion));
		}
	}
	
	[SteamEngine.DeepCopyableClassAttribute()]
	[SteamEngine.Persistence.SaveableClassAttribute()]
	public partial class Potion : Item {
		
		[SteamEngine.DeepCopyImplementationAttribute()]
		public Potion(Potion copyFrom) : 
				base(copyFrom) {
		}
		
		public Potion(PotionDef myDef) : 
				base(myDef) {
		}
		
		public new PotionDef TypeDef {
			get {
				return ((PotionDef)(base.Def));
			}
		}
	}
}
