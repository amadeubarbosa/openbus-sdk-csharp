﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.269
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace tecgraf.openbus.interop.simple.Properties {
    
    
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
        public string hostName {
            get {
                return ((string)(this["hostName"]));
            }
            set {
                this["hostName"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("..\\..\\..\\..\\..\\Resources\\Openbus.config")]
        public string logFile {
            get {
                return ((string)(this["logFile"]));
            }
            set {
                this["logFile"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("interop_hello_csharp")]
        public string entityName {
            get {
                return ((string)(this["entityName"]));
            }
            set {
                this["entityName"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("9000")]
        public ushort hostPort {
            get {
                return ((ushort)(this["hostPort"]));
            }
            set {
                this["hostPort"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("InteropHello.key")]
        public string xmlPrivateKey {
            get {
                return ((string)(this["xmlPrivateKey"]));
            }
            set {
                this["xmlPrivateKey"] = value;
            }
        }
    }
}
