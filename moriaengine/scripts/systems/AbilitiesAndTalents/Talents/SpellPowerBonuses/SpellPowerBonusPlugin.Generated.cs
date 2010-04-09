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
	
	
	public partial class SpellPowerBonusPluginDef : EffectDurationPluginDef {
		
		private FieldValue spell;
		
		public SpellPowerBonusPluginDef(String defname, String filename, Int32 headerLine) : 
				base(defname, filename, headerLine) {
			this.spell = this.InitTypedField("spell", null, typeof(SpellDef));
		}
		
		public SpellDef Spell {
			get {
				return ((SpellDef)(this.spell.CurrentValue));
			}
			set {
				this.spell.CurrentValue = value;
			}
		}
		
		protected override SteamEngine.Plugin CreateImpl() {
			return new SpellPowerBonusPlugin();
		}
		
		public new static void Bootstrap() {
			SteamEngine.PluginDef.RegisterPluginDef(typeof(SpellPowerBonusPluginDef), typeof(SpellPowerBonusPlugin));
		}
	}
	
	[SteamEngine.DeepCopyableClassAttribute()]
	[SteamEngine.Persistence.SaveableClassAttribute()]
	public partial class SpellPowerBonusPlugin : EffectDurationPlugin {
		
		[SteamEngine.DeepCopyImplementationAttribute()]
		public SpellPowerBonusPlugin(SpellPowerBonusPlugin copyFrom) : 
				base(copyFrom) {
		}
		
		[SteamEngine.Persistence.LoadingInitializerAttribute()]
		public SpellPowerBonusPlugin() {
		}
		
		public new SpellPowerBonusPluginDef TypeDef {
			get {
				return ((SpellPowerBonusPluginDef)(base.Def));
			}
		}
	}
}
