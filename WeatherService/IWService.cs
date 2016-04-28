using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace WeatherService
{
    // ПРИМЕЧАНИЕ. Команду "Переименовать" в меню "Рефакторинг" можно использовать для одновременного изменения имени интерфейса "IWService" в коде и файле конфигурации.
    [ServiceContract]
    public interface IWService
    {
        [OperationContract]
        void SetAuthData(string server, uint port, string username, string password);

        [OperationContract]
        string GetCityName(int id);

        [OperationContract]
        int GetCityId(string name);

        [OperationContract]
        Dictionary<int, string> GetCitiesList();

        [OperationContract]
        void SaveSettings();
    }
}
