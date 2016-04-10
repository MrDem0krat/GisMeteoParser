﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserLib
{
    /// <summary>
    /// Сущность, описывающая прогноз погоды на конкретный день
    /// </summary>
    public interface IWeatherInfo
    {
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
    }

    /// <summary>
    /// Перечисление доступных для прогноза частей суток
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
