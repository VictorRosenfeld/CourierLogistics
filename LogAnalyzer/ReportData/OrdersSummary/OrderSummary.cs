
namespace LogAnalyzer.ReportData.OrdersSummary
{
    using System;

    /// <summary>
    /// Сводка по заказу
    /// </summary>
    public class OrderSummary
    {
        /// <summary>
        /// Id заказа
        /// </summary>
        public int OrderId { get; private set; }

        /// <summary>
        /// Id магазина
        /// </summary>
        public int ShopId { get; private set; }

        /// <summary>
        /// Флаги заказа
        /// </summary>
        public OrderFlags Flags { get; set; }

        /// <summary>
        /// Количество принятых событий для заказа
        /// </summary>
        public int EventCount { get; set; }

        #region Last receipted event

        /// <summary>
        /// Время получения Receipted-события
        /// </summary>
        public DateTime ReceivedTime_Receipted { get; set; }

        /// <summary>
        /// Время наступления Receipted-события
        /// </summary>
        public DateTime EventTime_Receipted { get; set; }

        /// <summary>
        /// Тип события
        /// </summary>
        public int Type_Receipted { get; set; }

        /// <summary>
        /// Время начала интервала вручения
        /// </summary>
        public DateTime TimeFrom_Receipted { get; set; }

        /// <summary>
        /// Время конца интервала вручения
        /// </summary>
        public DateTime TimeTo_Receipted { get; set; }

        /// <summary>
        /// Вес заказа, кг
        /// </summary>
        public double Weight_Receipted { get; set; }

        /// <summary>
        /// Флаги доступных способов доставки
        /// </summary>
        public DeliveryServiceFlags DeliveryFlags_Receipted { get; set; }

        #endregion Last receipted event

        #region Last assembled event

        /// <summary>
        /// Время получения Receipted-события
        /// </summary>
        public DateTime ReceivedTime_Assembled { get; set; }

        /// <summary>
        /// Время наступления Receipted-события
        /// </summary>
        public DateTime EventTime_Assembled { get; set; }

        /// <summary>
        /// Тип события
        /// </summary>
        public int Type_Assembled { get; set; }

        /// <summary>
        /// Время начала интервала вручения
        /// </summary>
        public DateTime TimeFrom_Assembled { get; set; }

        /// <summary>
        /// Время конца интервала вручения
        /// </summary>
        public DateTime TimeTo_Assembled { get; set; }

        /// <summary>
        /// Вес заказа, кг
        /// </summary>
        public double Weight_Assembled { get; set; }

        /// <summary>
        /// Флаги доступных способов доставки
        /// </summary>
        public DeliveryServiceFlags DeliveryFlags_Assembled { get; set; }

        #endregion Last assembled event
              
        #region Last canceled event

        /// <summary>
        /// Время получения Receipted-события
        /// </summary>
        public DateTime ReceivedTime_Canceled { get; set; }

        /// <summary>
        /// Время наступления Receipted-события
        /// </summary>

        public DateTime EventTime_Canceled { get; set; }

        /// <summary>
        /// Тип события
        /// </summary>
        public int Type_Canceled { get; set; }

        /// <summary>
        /// Время начала интервала вручения
        /// </summary>
        public DateTime TimeFrom_Canceled { get; set; }

        /// <summary>
        /// Время конца интервала вручения
        /// </summary>
        public DateTime TimeTo_Canceled { get; set; }

        /// <summary>
        /// Вес заказа, кг
        /// </summary>
        public double Weight_Canceled { get; set; }

        /// <summary>
        /// Флаги доступных способов доставки
        /// </summary>
        public DeliveryServiceFlags DeliveryFlags_Canceled { get; set; }

        #endregion Last canceled event

        /// <summary>
        /// Общее число переданных команд
        /// </summary>
        public int CommandCount { get; set; }

        #region First delivery/recommendation command

        /// <summary>
        /// Время отправки команды
        /// </summary>
        public DateTime SentTime_First { get; set; }

        /// <summary>
        /// Статус команды
        /// </summary>
        public int Status_First { get; set; }

        public string CommandText_First { get; set; }

        #endregion First delivery/recommendation command

        #region Last delivery/recommendation command

        /// <summary>
        /// Время отправки команды
        /// </summary>
        public DateTime SentTime_Last { get; set; }

        /// <summary>
        /// Статус команды
        /// </summary>
        public int Status_Last { get; set; }

        public string CommandText_Last { get; set; }

        #endregion Last delivery/recommendation command

        /// <summary>
        /// Параметрический конструктор класса OrderSummary
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="shopId"></param>
        public OrderSummary(int orderId, int shopId)
        {
            OrderId = orderId;
            ShopId = shopId;
        }
    }
}
