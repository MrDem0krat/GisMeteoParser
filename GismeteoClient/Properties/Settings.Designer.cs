﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан программой.
//     Исполняемая версия:4.0.30319.42000
//
//     Изменения в этом файле могут привести к неправильной работе и будут потеряны в случае
//     повторной генерации кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace GismeteoClient.Properties {
    
    
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
        [global::System.Configuration.DefaultSettingValueAttribute("Resources/graphics/weather_icons/")]
        public string ImagesPath {
            get {
                return ((string)(this["ImagesPath"]));
            }
            set {
                this["ImagesPath"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string USdbLogin {
            get {
                return ((string)(this["USdbLogin"]));
            }
            set {
                this["USdbLogin"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string USdbServer {
            get {
                return ((string)(this["USdbServer"]));
            }
            set {
                this["USdbServer"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public uint USdbPort {
            get {
                return ((uint)(this["USdbPort"]));
            }
            set {
                this["USdbPort"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool isDbDataSaved {
            get {
                return ((bool)(this["isDbDataSaved"]));
            }
            set {
                this["isDbDataSaved"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("4258")]
        public int USCityID {
            get {
                return ((int)(this["USCityID"]));
            }
            set {
                this["USCityID"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Брянск")]
        public string USCityName {
            get {
                return ((string)(this["USCityName"]));
            }
            set {
                this["USCityName"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int USRefreshPeriod {
            get {
                return ((int)(this["USRefreshPeriod"]));
            }
            set {
                this["USRefreshPeriod"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool USCanClose {
            get {
                return ((bool)(this["USCanClose"]));
            }
            set {
                this["USCanClose"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::Weather.WeatherItem LastForecastNight {
            get {
                return ((global::Weather.WeatherItem)(this["LastForecastNight"]));
            }
            set {
                this["LastForecastNight"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::Weather.WeatherItem LastForecastMorning {
            get {
                return ((global::Weather.WeatherItem)(this["LastForecastMorning"]));
            }
            set {
                this["LastForecastMorning"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::Weather.WeatherItem LastForecastDay {
            get {
                return ((global::Weather.WeatherItem)(this["LastForecastDay"]));
            }
            set {
                this["LastForecastDay"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::Weather.WeatherItem LastForecastEvening {
            get {
                return ((global::Weather.WeatherItem)(this["LastForecastEvening"]));
            }
            set {
                this["LastForecastEvening"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::Weather.WeatherItem LastForecastNow {
            get {
                return ((global::Weather.WeatherItem)(this["LastForecastNow"]));
            }
            set {
                this["LastForecastNow"] = value;
            }
        }
    }
}
