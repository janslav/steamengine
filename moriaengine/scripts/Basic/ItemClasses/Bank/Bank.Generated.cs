//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.239
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
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
	
	
	public partial class BankDef : ContainerDef {
		
		private FieldValue gump;
		
		public BankDef(String defname, String filename, Int32 headerLine) : 
				base(defname, filename, headerLine) {
			this.gump = this.InitTypedField("gump", -1, typeof(Int16));
		}
		
		public Int16 Gump {
			get {
				return ((Int16)(this.gump.CurrentValue));
			}
			set {
				this.gump.CurrentValue = value;
			}
		}
		
		protected override SteamEngine.Thing CreateImpl() {
			return new Bank(this);
		}
		
		public new static void Bootstrap() {
			SteamEngine.ThingDef.RegisterThingDef(typeof(BankDef), typeof(Bank));
		}
	}
	
	[SteamEngine.DeepCopyableClassAttribute()]
	[SteamEngine.Persistence.SaveableClassAttribute()]
	public partial class Bank : Container {
		
		[SteamEngine.DeepCopyImplementationAttribute()]
		public Bank(Bank copyFrom) : 
				base(copyFrom) {
		}
		
		public Bank(BankDef myDef) : 
				base(myDef) {
		}
		
		public new BankDef TypeDef {
			get {
				return ((BankDef)(base.Def));
			}
		}
	}
}
