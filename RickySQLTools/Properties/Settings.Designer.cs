﻿//------------------------------------------------------------------------------
// <auto-generated>
//     這段程式碼是由工具產生的。
//     執行階段版本:4.0.30319.42000
//
//     對這個檔案所做的變更可能會造成錯誤的行為，而且如果重新產生程式碼，
//     變更將會遺失。
// </auto-generated>
//------------------------------------------------------------------------------

namespace RickySQLTools.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "14.0.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Server=192.168.137.1\\SQLExpress; DataBase=TSSO; uid=sa;pwd=P@ssw0rd")]
        public string DefConnectionString {
            get {
                return ((string)(this["DefConnectionString"]));
            }
            set {
                this["DefConnectionString"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(@"<?xml version=""1.0"" standalone=""yes""?>
<Programs>
  <Progs>
    <ProgID>frmTables</ProgID>
    <ProgName>Tables</ProgName>
    <ModuleName>RickySQLTools</ModuleName>
    <SEQ>2</SEQ>
  </Progs>
  <Progs>
    <ProgID>frmXmlCompare</ProgID>
    <ProgName>Xml Compare</ProgName>
    <ModuleName>RickySQLTools</ModuleName>
    <SEQ>3</SEQ>
  </Progs>
  <Progs>
    <ProgID>frmFakeData</ProgID>
    <ProgName>Fake Data</ProgName>
    <ModuleName>RickySQLTools</ModuleName>
    <SEQ>4</SEQ>
  </Progs>
  <Progs>
    <ProgID>frmPOCOGenrator</ProgID>
    <ProgName>POCO Gen</ProgName>
    <ModuleName>RickySQLTools</ModuleName>
    <SEQ>5</SEQ>
  </Progs>
</Programs>")]
        public string Progs {
            get {
                return ((string)(this["Progs"]));
            }
            set {
                this["Progs"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("RickySQLTools.Models")]
        public string DefNamespace {
            get {
                return ((string)(this["DefNamespace"]));
            }
            set {
                this["DefNamespace"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("using System;\r\nusing System.Collections.Generic;\r\nusing System.Linq;\r\nusing Syste" +
            "m.Text;\r\nusing System.Threading.Tasks;\r\nusing System.ComponentModel.DataAnnotati" +
            "ons;")]
        public string DefUsing {
            get {
                return ((string)(this["DefUsing"]));
            }
            set {
                this["DefUsing"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool DefIncludeAttr {
            get {
                return ((bool)(this["DefIncludeAttr"]));
            }
            set {
                this["DefIncludeAttr"] = value;
            }
        }
    }
}
