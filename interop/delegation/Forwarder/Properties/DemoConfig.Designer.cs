﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace tecgraf.openbus.interop.delegation.Properties {
    
    
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
        [global::System.Configuration.DefaultSettingValueAttribute("localhost")]
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
        [global::System.Configuration.DefaultSettingValueAttribute("2089")]
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
        [global::System.Configuration.DefaultSettingValueAttribute("..\\..\\..\\admin\\InteropDelegation.key")]
        public string privateKey {
            get {
                return ((string)(this["privateKey"]));
            }
            set {
                this["privateKey"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool useSSL {
            get {
                return ((bool)(this["useSSL"]));
            }
            set {
                this["useSSL"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("CurrentUser")]
        public string clientUser {
            get {
                return ((string)(this["clientUser"]));
            }
            set {
                this["clientUser"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("c14566a2f94e24d576a9a02d706d8358e2462322")]
        public string clientThumbprint {
            get {
                return ((string)(this["clientThumbprint"]));
            }
            set {
                this["clientThumbprint"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("C:\\\\Users\\\\Cadu\\\\Dropbox\\\\Tecgraf\\\\core210x\\\\BUS01.ior")]
        public string busIORFile {
            get {
                return ((string)(this["busIORFile"]));
            }
            set {
                this["busIORFile"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("CurrentUser")]
        public string serverUser {
            get {
                return ((string)(this["serverUser"]));
            }
            set {
                this["serverUser"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("256a1ce837292a3c80b4e82b36741316f63fe46a")]
        public string serverThumbprint {
            get {
                return ((string)(this["serverThumbprint"]));
            }
            set {
                this["serverThumbprint"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("58001")]
        public string serverSSLPort {
            get {
                return ((string)(this["serverSSLPort"]));
            }
            set {
                this["serverSSLPort"] = value;
            }
        }
    }
}
