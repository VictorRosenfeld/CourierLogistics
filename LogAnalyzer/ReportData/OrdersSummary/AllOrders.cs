
namespace LogAnalyzer.ReportData.OrdersSummary
{
    using System;
    using System.Collections.Generic;
    using static LogisticsService.API.GetOrderEvents;
    using static LogisticsService.API.BeginShipment;
    using Newtonsoft.Json;
    using System.IO;

    /// <summary>
    /// Данные для OrdersSummary
    /// </summary>
    public class AllOrders
    {
        /// <summary>
        /// Коллекция OrderSummary
        /// </summary>
        private Dictionary<int, OrderSummary> orders;

        /// <summary>
        /// Коллекция OrderSummary
        /// </summary>
        public Dictionary<int, OrderSummary> Orders => orders;

        /// <summary>
        /// Параметрический конструктор класса AllOrders
        /// </summary>
        /// <param name="capacity">Начальная ёмкость коллекции</param>
        public AllOrders(int capacity = 25000)
        {
            if (capacity <= 0)
                capacity = 25000;
            orders = new Dictionary<int, OrderSummary>(capacity);
        }

        /// <summary>
        ///  Обновление состояния заказов по событиям заказов
        /// </summary>
        /// <param name="logDateTime">Время события по логу</param>
        /// <param name="events">События заказов</param>
        /// <returns>0 - события успешно обработаны; иначе - события не обработаны</returns>
        public int AddOrderEvent(DateTime logDateTime, OrderEvent[] events)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (events == null || events.Length <= 0)
                    return rc;

                // 3. Цикл обработки событий заказов
                rc = 3;
                for (int i = 0; i < events.Length; i++)
                {
                    // 3.1 Извлекаем заказ
                    rc = 31;
                    OrderEvent orderEvent = events[i];

                    // 3.2 Находим заказ в коллекции
                    rc = 32;
                    OrderSummary orderSummary;
                    if (!orders.TryGetValue(orderEvent.order_id, out orderSummary))
                    {
                        orderSummary = new OrderSummary(orderEvent.order_id, orderEvent.shop_id);
                        orders.Add(orderEvent.order_id, orderSummary);
                    }

                    // 3.3 Увеличивает счетчик событий заказа
                    rc = 33;
                    orderSummary.EventCount++;

                    // 3.4 Обрабатываем по типу события
                    rc = 34;

                    switch (orderEvent.type)
                    {
                        case 0: // поступление в магазин
                            orderSummary.Flags |= OrderFlags.Receipted;
                            orderSummary.ReceivedTime_Receipted = logDateTime;
                            orderSummary.EventTime_Receipted = orderEvent.date_event;
                            orderSummary.Type_Receipted = orderEvent.type;
                            orderSummary.TimeFrom_Receipted = orderEvent.delivery_frame_from;
                            orderSummary.TimeTo_Receipted = orderEvent.delivery_frame_to;
                            orderSummary.Weight_Receipted = orderEvent.weight;
                            orderSummary.DeliveryFlags_Receipted |= GetOrderDeliveryServiceFlags(orderEvent);
                            break;
                        case 1: // завершение сборки
                            orderSummary.Flags |= OrderFlags.Asssembled;
                            orderSummary.ReceivedTime_Assembled = logDateTime;
                            orderSummary.EventTime_Assembled = orderEvent.date_event;
                            orderSummary.Type_Assembled = orderEvent.type;
                            orderSummary.TimeFrom_Assembled = orderEvent.delivery_frame_from;
                            orderSummary.TimeTo_Assembled = orderEvent.delivery_frame_to;
                            orderSummary.Weight_Assembled = orderEvent.weight;
                            orderSummary.DeliveryFlags_Assembled |= GetOrderDeliveryServiceFlags(orderEvent);
                            break;
                        case 3: // отмена заказа
                            orderSummary.Flags |= OrderFlags.Canceled;
                            orderSummary.ReceivedTime_Canceled = logDateTime;
                            orderSummary.EventTime_Canceled = orderEvent.date_event;
                            orderSummary.Type_Canceled = orderEvent.type;
                            orderSummary.TimeFrom_Canceled = orderEvent.delivery_frame_from;
                            orderSummary.TimeTo_Canceled = orderEvent.delivery_frame_to;
                            orderSummary.Weight_Canceled = orderEvent.weight;
                            orderSummary.DeliveryFlags_Canceled |= GetOrderDeliveryServiceFlags(orderEvent);
                            break;
                    }
                }

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }
    
        /// <summary>
        ///  Обновление состояния заказов по отправленным командам отгрузки/рекомендации
        /// </summary>
        /// <param name="logDateTime">Время события по логу</param>
        /// <param name="commands">Команды</param>
        /// <returns>0 - команды успешно обработаны; иначе - команды не обработаны</returns>
        public int AddCommand(DateTime logDateTime, Shipment[] commands)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (commands == null || commands.Length <= 0)
                    return rc;

                // 3. Цикл обработки событий заказов
                rc = 3;
                JsonSerializer serializer = JsonSerializer.Create();

                for (int i = 0; i < commands.Length; i++)
                {
                    // 3.1 Извлекаем заказ
                    rc = 31;
                    Shipment shipment = commands[i];
                    if (shipment.orders == null || shipment.orders.Length <= 0)
                        continue;

                    // 3.2 Создаём json-представление отгрузки
                    rc = 32;
                    string json;
                    using (StringWriter sw = new StringWriter())
                    {
                        serializer.Serialize(sw, new Shipment[] { shipment });
                        sw.Close();
                        json = sw.ToString();
                    }

                    foreach (int orderId in shipment.orders)
                    {
                        // 3.3 Находим заказ в коллекции
                        rc = 33;
                        OrderSummary orderSummary;
                        if (!orders.TryGetValue(orderId, out orderSummary))
                        {
                            orderSummary = new OrderSummary(orderId, shipment.shop_id);
                            orders.Add(orderId, orderSummary);
                        }

                        // 3.4 Увеличивает счетчик комманд
                        rc = 34;
                        orderSummary.CommandCount++;

                        // 3.5 Обновляем флаги заказа
                        rc = 35;
                        if (shipment.status == 1) orderSummary.Flags |= OrderFlags.Shipped;

                        // 3.6 Сохраняем команду
                        rc = 36;
                        if (orderSummary.CommandCount == 1)
                        {
                            orderSummary.SentTime_First = logDateTime;
                            orderSummary.Status_First = shipment.status;
                            orderSummary.CommandText_First = json;
                        }

                        orderSummary.SentTime_Last = logDateTime;
                        orderSummary.Status_Last = shipment.status;
                        orderSummary.CommandText_Last = json;
                    }
                }

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        ///  Обновление состояния заказов по отправленным командам отмены
        /// </summary>
        /// <param name="logDateTime">Время события по логу</param>
        /// <param name="commands">Команды</param>
        /// <returns>0 - команды успешно обработаны; иначе - команды не обработаны</returns>
        public int AddCommand(DateTime logDateTime, RejectedOrder[] commands)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (commands == null || commands.Length <= 0)
                    return rc;

                // 3. Цикл обработки событий заказов
                rc = 3;
                JsonSerializer serializer = JsonSerializer.Create();

                for (int i = 0; i < commands.Length; i++)
                {
                    // 3.1 Извлекаем заказ
                    rc = 31;
                    RejectedOrder rejectedOrder = commands[i];
                    if (rejectedOrder.orders == null || rejectedOrder.orders.Length <= 0)
                        continue;

                    // 3.2 Создаём json-представление отгрузки
                    rc = 32;
                    string json;
                    using (StringWriter sw = new StringWriter())
                    {
                        serializer.Serialize(sw, new RejectedOrder[] { rejectedOrder });
                        sw.Close();
                        json = sw.ToString();
                    }

                    foreach (int orderId in rejectedOrder.orders)
                    {
                        // 3.3 Находим заказ в коллекции
                        rc = 33;
                        OrderSummary orderSummary;
                        if (!orders.TryGetValue(orderId, out orderSummary))
                        {
                            orderSummary = new OrderSummary(orderId, rejectedOrder.shop_id);
                            orders.Add(orderId, orderSummary);
                        }

                        // 3.4 Увеличивает счетчик комманд
                        rc = 34;
                        orderSummary.CommandCount++;

                        // 3.5 Обновляем флаги заказа
                        rc = 35;
                        if (rejectedOrder.status == 2 || rejectedOrder.status == 3)
                            orderSummary.Flags |= OrderFlags.Canceled;

                        // 3.6 Сохраняем команду
                        rc = 36;
                        if (orderSummary.CommandCount == 1)
                        {
                            orderSummary.SentTime_First = logDateTime;
                            orderSummary.Status_First = rejectedOrder.status;
                            orderSummary.CommandText_First = json;
                        }

                        orderSummary.SentTime_Last = logDateTime;
                        orderSummary.Status_Last = rejectedOrder.status;
                        orderSummary.CommandText_Last = json;
                    }
                }

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Получение флагов доступных сервисов для заказа
        /// </summary>
        /// <param name="orderEvent">Заказ</param>
        /// <returns>Флаги доступных сервисов</returns>
        private static DeliveryServiceFlags GetOrderDeliveryServiceFlags(OrderEvent orderEvent)
        {
            if (orderEvent == null ||
                orderEvent.service_available == null ||
                orderEvent.service_available.Length <= 0)
                return DeliveryServiceFlags.None;

            DeliveryServiceFlags flags = DeliveryServiceFlags.None;
            foreach (var service in orderEvent.service_available)
            {
                if (service.shop_id == orderEvent.shop_id)
                {
                    switch (service.dservice_id)
                    {
                        case 12: // Gett-taxi
                            flags |= DeliveryServiceFlags.GettTaxi;
                            break;
                        case 14: // Yandex-taxi
                            flags |= DeliveryServiceFlags.YandexTaxi;
                            break;
                        case 4: // Couriers
                            flags |= DeliveryServiceFlags.Courier;
                            break;
                    }
                }
            }

            return flags;
        }
    }
}
