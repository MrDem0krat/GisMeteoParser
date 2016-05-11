using System;

namespace Weather
{
    /// <summary>
    /// Сущность, описывающая прогноз погоды на конкретный день
    /// </summary>
    public interface IWeatherItem
    {
        /// <summary>
        /// ID города, для которого предоставлен прогноз
        /// </summary>
        int CityID { get; set; }

        /// <summary>
        /// Часть дня, на которую предоставляется прогноз
        /// </summary>
        DayPart PartOfDay { get; set; }

        /// <summary>
        /// День, на который прогноз.
        /// </summary>
        DateTime Date { get; set; }

        /// <summary>
        /// Обещанная температура.
        /// </summary>
        int Temperature { get; set; }

        /// <summary>
        /// Температура по ощущениям
        /// </summary>
        int TemperatureFeel { get; set; }

        /// <summary>
        /// Cостояние погоды
        /// </summary>
        string Condition { get; set; }

        /// <summary>
        /// Имя изображения, подходящего под состояние погоды
        /// </summary>
        string TypeImage { get; set; }

        /// <summary>
        /// Влажность.
        /// </summary>
        int Humidity { get; set; }

        /// <summary>
        /// Давление.
        /// </summary>
        int Pressure { get; set; }

        /// <summary>
        /// Направление ветра
        /// </summary>
        string WindDirection { get; set; }

        /// <summary>
        /// Скорость ветра
        /// </summary>
        float WindSpeed { get; set; }

        /// <summary>
        /// Время обновления данных
        /// </summary>
        DateTime RefreshTime { get; set; }

        /// <summary>
        /// Возвращает строку с русский названием части суток
        /// </summary>
        /// <param name="part">Часть суток для перевода</param>
        /// <returns></returns>
        string GetRusDayPart();
    }

    

    /// <summary>
    /// Определяет часть суток
    /// </summary>
    public enum DayPart
    {
        Night = 0,
        Morning = 1,
        Day = 2,
        Evening = 3
    }

    /// <summary>
    /// Перечесление дней, для которых может быть получен прогноз: Сегодня, Завтра, Послезавтра
    /// </summary>
    public enum DayToParse
    {
        Today = 1,
        Tomorrow = 2,
        DayAfterTomorrow = 3
    }
}
