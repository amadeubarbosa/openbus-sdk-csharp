﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.269
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace tecgraf.openbus.interop.multiplexing.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "10.0.0.0")]
    internal sealed partial class DemoConfig : global::System.Configuration.ApplicationSettingsBase {
        
        private static DemoConfig defaultInstance = ((DemoConfig)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new DemoConfig())));
        
        public static DemoConfig Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("ubu")]
        public string busHostName {
            get {
                return ((string)(this["busHostName"]));
            }
            set {
                this["busHostName"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("9000")]
        public ushort busHostPort {
            get {
                return ((ushort)(this["busHostPort"]));
            }
            set {
                this["busHostPort"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("11000")]
        public ushort bus2HostPort {
            get {
                return ((ushort)(this["bus2HostPort"]));
            }
            set {
                this["bus2HostPort"] = value;
            }
        }
    }
}
