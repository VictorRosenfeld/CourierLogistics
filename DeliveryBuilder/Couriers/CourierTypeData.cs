
namespace DeliveryBuilder.Couriers
{
    using System;
    using System.ComponentModel;
    using System.Xml.Serialization;

    /// <summary>
    /// Данные способа доставки для расчета
    /// стоимости и времени доставки
    /// </summary>
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(ElementName = "vehicle", Namespace = "", IsNullable = false)]
    public class CourierTypeData
    {
        /// <summary>
        /// ID курьера
        /// </summary>
        [XmlAttribute(AttributeName = "id")]
        public int Id { get; set; }

        /// <summary>
        /// Флаг: true - такси; false - курьер
        /// </summary>
        [XmlAttribute(AttributeName = "istaxi")]
        public bool IsTaxi { get; set; }

        /// <summary>
        /// Параметры способа доставки
        /// </summary>
        [XmlArrayItem("parameter", IsNullable = false)]
        public VehicleParameter[] parameters { get; set; }

        /// <summary>
        /// Маппер ID способа доставки
        /// в ID способов доставки различных служб
        /// </summary>
        [XmlElement(ElementName = "mapper")]
        /// <remarks/>
        public VehicleTypeMapper Mapper { get; set; }

        /// <summary>
        /// Флаг: true - экземпляр подготовлен для использования; false - экземпляр не подготовлен для использования
        /// </summary>
        [XmlIgnore]
        public bool IsCreated { get; private set; }

        /// <summary>
        /// Отсортированные имена параметров
        /// </summary>
        private string[] parameterName;

        /// <summary>
        /// Подготовка экземпляра для использования
        /// </summary>
        /// <returns>0 - экземпляр подготовлен; иначе - экземпляр не подготовлен</returns>
        public int Create()
        {
            // 1. Инициализация
            int rc = 1;
            IsCreated = false;
            parameterName = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (parameters == null || parameters.Length <= 0)
                    return rc;

                // 3. Создаём индекс для быстрой выборки параметров способа доставки
                rc = 3;
                parameterName = new string[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                    parameterName[i] = parameters[i].Name.ToLower();

                Array.Sort(parameterName, parameters);

                // 4. Выход - Ok
                rc = 0;
                IsCreated = true;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Поиск параметра по имени
        /// (поиск не чувствителен к регистру)
        /// </summary>
        /// <param name="name">Имя параметра</param>
        /// <returns>Параметр или null</returns>
        public VehicleParameter FindParameter(string name)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (string.IsNullOrWhiteSpace(name))
                    return null;
                if (!IsCreated)
                    return null;

                // 3. Находим параметр
                int index = Array.BinarySearch(parameterName, name.ToLower());
                if (index < 0)
                    return null;

                // 4. Выход - Ok
                return parameters[index];
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Выбор значения double-параметра
        /// </summary>
        /// <param name="name">Имя параметра</param>
        /// <returns>Значение параметра или NaN</returns>
        public double GetDoubleParameterValue(string name)
        {
            try
            {
                // 2. Извлекаем параметр
                VehicleParameter param = FindParameter(name);
                if (param == null)
                    return double.NaN;

                // 3. Преобразуем значение параметра из string в double
                return double.Parse(param.Value, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                return double.NaN;
            }
        }

        /// <summary>
        /// Выбор значения int-параметра
        /// </summary>
        /// <param name="name">Имя параметра</param>
        /// <returns>Значение параметра или int.MinValue</returns>
        public int GetIntParameterValue(string name)
        {
            try
            {
                // 2. Извлекаем параметр
                VehicleParameter param = FindParameter(name);
                if (param == null)
                    return int.MinValue;

                // 3. Преобразуем значение параметра из string в int
                return int.Parse(param.Value, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                return int.MinValue;
            }
        }

        /// <summary>
        /// Выбор значения string-параметра
        /// </summary>
        /// <param name="name">Имя параметра</param>
        /// <returns>Значение параметра или null</returns>
        public string GetStringParameterValue(string name)
        {
            try
            {
                // 2. Извлекаем параметр
                VehicleParameter param = FindParameter(name);
                if (param == null)
                    return null;

                // 3. Выход - Ok
                return param.Value;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Параметр способа доставки
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class VehicleParameter
    {
        /// <summary>
        /// Имя параметра
        /// </summary>
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Значение параметра
        /// </summary>
        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }

        /// <summary>
        /// Тип значения
        /// </summary>
        [XmlAttribute(AttributeName = "type")]
        public string ValueType { get; set; }
    }

    /// <summary>
    /// Маппер способа доставки 
    /// в коды других подсистем
    /// </summary>
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class VehicleTypeMapper
    {
        /// <summary>
        /// Гео-тип способа передвижения Yandex
        /// </summary>
        [XmlElement(ElementName = "yandex")]
        public YandexType Yandex { get; set; }

        /// <summary>
        /// Тип способа доставки, поступающий 
        /// от сервиса S1 c данными о курьерах
        /// </summary>
        [XmlElement(ElementName = "courier")]
        public InputType Input { get; set; }

        /// <summary>
        /// Код сервиса пулинга, соответствующий
        /// данному способу доставки
        /// </summary>
        [XmlElement(ElementName = "dservice")]
        public DServiceType DService { get; set; }
    }

    /// <summary>
    /// Гео-тип Yandex
    /// </summary>
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class YandexType
    {
        /// <summary>
        /// Гео-тип Yandex
        /// </summary>
        [XmlAttribute(AttributeName = "type")]
        public string Value { get; set; }

        /// <summary>
        /// ID гео-типа Yandex
        /// </summary>
        [XmlIgnore()]
        public int Id  { get; set; }
    }

    /// <summary>
    /// Тип способа доставки в данных о курьерах от сервиса S1
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class InputType
    {
        /// <summary>
        /// Тип способа доставки в данных о курьерах от сервиса S1
        /// </summary>
        [XmlAttribute(AttributeName = "type")]
        public int Value { get; set; }
    }

    /// <summary>
    /// Тип сервиса пулинга
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class DServiceType
    {
        /// <remarks/>
        [XmlAttribute(AttributeName = "type")]
        public int Value { get; set; }
    }
}
