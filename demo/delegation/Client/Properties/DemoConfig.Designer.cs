﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.261
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Client.Properties {
    
    
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
        [global::System.Configuration.DefaultSettingValueAttribute("9000")]
        public short hostPort {
            get {
                return ((short)(this["hostPort"]));
            }
            set {
                this["hostPort"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("demo")]
        public string userLogin {
            get {
                return ((string)(this["userLogin"]));
            }
            set {
                this["userLogin"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("demo")]
        public string userPassword {
            get {
                return ((string)(this["userPassword"]));
            }
            set {
                this["userPassword"] = value;
            }
        }
    }
}
