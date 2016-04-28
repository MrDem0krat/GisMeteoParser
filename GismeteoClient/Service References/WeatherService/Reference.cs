﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан программой.
//     Исполняемая версия:4.0.30319.42000
//
//     Изменения в этом файле могут привести к неправильной работе и будут потеряны в случае
//     повторной генерации кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace GismeteoClient.WeatherService {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="WeatherService.IWService")]
    public interface IWService {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IWService/SetAuthData", ReplyAction="http://tempuri.org/IWService/SetAuthDataResponse")]
        void SetAuthData(string server, uint port, string username, string password);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IWService/SetAuthData", ReplyAction="http://tempuri.org/IWService/SetAuthDataResponse")]
        System.Threading.Tasks.Task SetAuthDataAsync(string server, uint port, string username, string password);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IWService/GetCityName", ReplyAction="http://tempuri.org/IWService/GetCityNameResponse")]
        string GetCityName(int id);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IWService/GetCityName", ReplyAction="http://tempuri.org/IWService/GetCityNameResponse")]
        System.Threading.Tasks.Task<string> GetCityNameAsync(int id);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IWService/GetCityId", ReplyAction="http://tempuri.org/IWService/GetCityIdResponse")]
        int GetCityId(string name);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IWService/GetCityId", ReplyAction="http://tempuri.org/IWService/GetCityIdResponse")]
        System.Threading.Tasks.Task<int> GetCityIdAsync(string name);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IWService/GetCitiesList", ReplyAction="http://tempuri.org/IWService/GetCitiesListResponse")]
        System.Collections.Generic.Dictionary<int, string> GetCitiesList();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IWService/GetCitiesList", ReplyAction="http://tempuri.org/IWService/GetCitiesListResponse")]
        System.Threading.Tasks.Task<System.Collections.Generic.Dictionary<int, string>> GetCitiesListAsync();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IWService/SaveSettings", ReplyAction="http://tempuri.org/IWService/SaveSettingsResponse")]
        void SaveSettings();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IWService/SaveSettings", ReplyAction="http://tempuri.org/IWService/SaveSettingsResponse")]
        System.Threading.Tasks.Task SaveSettingsAsync();
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface IWServiceChannel : GismeteoClient.WeatherService.IWService, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class WServiceClient : System.ServiceModel.ClientBase<GismeteoClient.WeatherService.IWService>, GismeteoClient.WeatherService.IWService {
        
        public WServiceClient() {
        }
        
        public WServiceClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public WServiceClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public WServiceClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public WServiceClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public void SetAuthData(string server, uint port, string username, string password) {
            base.Channel.SetAuthData(server, port, username, password);
        }
        
        public System.Threading.Tasks.Task SetAuthDataAsync(string server, uint port, string username, string password) {
            return base.Channel.SetAuthDataAsync(server, port, username, password);
        }
        
        public string GetCityName(int id) {
            return base.Channel.GetCityName(id);
        }
        
        public System.Threading.Tasks.Task<string> GetCityNameAsync(int id) {
            return base.Channel.GetCityNameAsync(id);
        }
        
        public int GetCityId(string name) {
            return base.Channel.GetCityId(name);
        }
        
        public System.Threading.Tasks.Task<int> GetCityIdAsync(string name) {
            return base.Channel.GetCityIdAsync(name);
        }
        
        public System.Collections.Generic.Dictionary<int, string> GetCitiesList() {
            return base.Channel.GetCitiesList();
        }
        
        public System.Threading.Tasks.Task<System.Collections.Generic.Dictionary<int, string>> GetCitiesListAsync() {
            return base.Channel.GetCitiesListAsync();
        }
        
        public void SaveSettings() {
            base.Channel.SaveSettings();
        }
        
        public System.Threading.Tasks.Task SaveSettingsAsync() {
            return base.Channel.SaveSettingsAsync();
        }
    }
}