﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.3615
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Server.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "2.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Server.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;?xml version=&quot;1.0&quot; encoding=&quot;utf-8&quot; ?&gt;
        ///
        ///&lt;component xmlns=&quot;tecgraf.scs.core&quot;
        ///           xmlns:xi=&quot;http://www.w3.org/2001/XMLSchema-instance&quot;&gt;
        ///  &lt;id&gt;
        ///    &lt;name&gt;Hello&lt;/name&gt;
        ///    &lt;version&gt;1.0.0&lt;/version&gt;
        ///    &lt;platformSpec&gt;.Net FrameWork 3.5&lt;/platformSpec&gt;
        ///  &lt;/id&gt;
        ///  &lt;facets&gt;
        ///    &lt;facet&gt;
        ///      &lt;name&gt;IHello&lt;/name&gt;
        ///      &lt;repId&gt;IDL:demoidl/hello/IHello:1.0&lt;/repId&gt;
        ///      &lt;servant assembly=&quot;DemoDelegate.Server&quot;&gt;Server.HelloServant&lt;/servant&gt;
        ///    &lt;/facet&gt;
        ///  &lt;/facets&gt;
        ///&lt;/component&gt;
        ///
        ///.
        /// </summary>
        internal static string ComponentModel {
            get {
                return ResourceManager.GetString("ComponentModel", resourceCulture);
            }
        }
    }
}
