
namespace DeliveryBuilder.Log
{
    /// <summary>
    /// Тексты/Шаблоны сообщений
    /// </summary>
    internal class Messages
    {
        internal const string MSG_001 = "---> {0} Ver. {1} Service ID = {2}";
        internal const string MSG_002 = "<--- {0} Ver. {1} Service ID = {2}";

        //internal const string MSG_666 = "Method {0}. {1}";
        //internal const string MSG_667 = "Method {0}. rc = {1}";
        //internal const string MSG_668 = "{0}({1})";
        internal const string MSG_669 = "Method {0} rc = {1}. Exception = {2}";
        internal const string MSG_670 = "Method {0}. Exception = {1}";

        internal const string MSG_003 = "Config = null";
        internal const string MSG_004 = "Тэг functional_parameters не задан";
        internal const string MSG_005 = "Тэг functional_parameters. Параметр {0} не задан или имеет недопустимое значение";
        internal const string MSG_006 = "Тэг salesman_levels не задан или имеет недопустимые значения";
        internal const string MSG_007 = "Тэг cloud_parameters не задан";
        internal const string MSG_008 = "Тэг cloud_parameters. Параметр {0} не задан или имеет недопустимое значение";
        internal const string MSG_009 = "Тэг service. Параметр {0} не задан или имеет недопустимое значение";

        internal const string MSG_010 = @"Тэг <type_name name=""walking"" не найден";
        internal const string MSG_011 = @"Тэг <type_name name=""cycling"" не найден";
        internal const string MSG_012 = @"Тэг <type_name name=""driving"" не найден";
        internal const string MSG_013 = @"Тэг <type_name id=""{0}"" не найден";

        internal const string MSG_014 = @"Taблица tblVehicles. VehicleID = {0}. Поле vhsData. yandex type=""{1}"" не поддерживается"; 
        internal const string MSG_015 = @"Taблица tblVehicles. VehicleID = {0}. Поле vhsData. Обязателыный параметр {1} не найден или имеет недопусимое значение"; 
        internal const string MSG_016 = @"Taблица tblVehicles. VehicleID = {0}. Поле vhsData пусто или имеет недопустимое значение"; 
        internal const string MSG_017 = @"Taблица tblVehicles. VehicleID = {0}. Поле vhsData. calc_method=""{1}"" не поддерживается"; 

        internal const string MSG_018 = @"Входные данные. Курьер courier_id=""{0}"". Неизвестный courier_type=""{1}"""; 
        internal const string MSG_019 = @"Входные данные. Курьер courier_id=""{0}"" courier_type=""{1}"". Базовый тип для vehicleId = {2} не найден"; 

        internal const string MSG_020 = @"Строка подключения к БД LSData пуста"; 
        internal const string MSG_021 = @"БД LSData. Не удалось установить соединение"; 
        internal const string MSG_022 = @"БД LSData. Не удалось установить соединение. Exception: {0}"; 
        internal const string MSG_023 = @"БД LSData. Не удалось зарузить конфиг"; 
        internal const string MSG_024 = @"БД LSData. Не удалось зарузить конфиг. Exception: {0}"; 
        internal const string MSG_025 = @"Тест параметров построителя не прошел"; 
        internal const string MSG_026 = @"Объект для работы с гео-данными не создан (rc = {0})"; 
        internal const string MSG_027 = @"БД LSData. Не удалось зарузить пороги для средней стоимости доставки"; 
        internal const string MSG_028 = @"БД LSData. Не удалось зарузить пороги для средней стоимости доставки. Exception: {0}"; 
        internal const string MSG_029 = @"Объект для работы с средними стоимости доставки не создан (rc = {0})"; 
        internal const string MSG_030 = @"БД LSData. Не удалось зарузить параметры способов доставки"; 
        internal const string MSG_031 = @"БД LSData. Не удалось зарузить параметры способов доставки. Exception: {0}"; 
        internal const string MSG_032 = @"Объект для работы с курьерами не создан (rc = {0})"; 
        internal const string MSG_033 = @"Объект для работы с заказами не создан (rc = {0})"; 
        internal const string MSG_034 = @"Объект c пределами на число заказов при полном переборе не создан (rc = {0})"; 

        internal const string MSG_035 = @"БД ExternalDb. Не удалось установить соединение"; 
        internal const string MSG_036 = @"БД ExternalDb. Не удалось установить соединение. Exception: {0}"; 

        internal const string MSG_037 = @"БД ExternalDb. Не удалось отправить сердцебиение (rc = {0})"; 
        internal const string MSG_038 = @"БД ExternalDb. Не удалось отправить сердцебиение (rc = {0}). Exception: {0}"; 

        internal const string MSG_039 = @"БД ExternalDb. Не удалось отправить запрос данных (rc = {0})"; 
        internal const string MSG_040 = @"БД ExternalDb. Не удалось отправить запрос данных (rc = {0}). Exception: {0}"; 

        internal const string MSG_041 = @"БД ExternalDb. Не удалось получить данные (rc = {0})"; 
        internal const string MSG_042 = @"БД ExternalDb. Не удалось получить данные (rc = {0}). Exception: {0}"; 

        internal const string MSG_043 = @"Данные с неизвестным типом сообщения '{0}'"; 
        internal const string MSG_044 = @"Не удалось десериализовать сообщение типа {0}: {1}"; 
        internal const string MSG_045 = @"Не удалось обновить данные о заказах (rc = {0}): {1}"; 
        internal const string MSG_046 = @"Не удалось обновить данные о курьерах (rc = {0}): {1}"; 
        internal const string MSG_047 = @"Не удалось обновить данные о магазинах (rc = {0}): {1}"; 

        internal const string MSG_048 = @"Не удалось построить команду на отгрузку из очереди (rc = {0})"; 
        internal const string MSG_049 = @"БД ExternalDb. Не удалось отправить команду на отгрузку из очереди (rc = {0})"; 
        internal const string MSG_050 = @"БД ExternalDb. Не удалось отправить команду на отгрузку из очереди (rc = {0}). Exception: {0}"; 

        internal const string MSG_051 = @"RecalcDeliveries Enter. service_id = {0}"; 
        internal const string MSG_052 = @"RecalcDeliveries Exit. service_id = {0}, rc = {1}, shops = {2}, orders = {3}, elapsed_time = {4}"; 

        internal const string MSG_053 = @"БД ExternalDb. Не удалось отправить команду отмены (rc = {0})"; 
        internal const string MSG_054 = @"БД ExternalDb. Не удалось отправить команду отмены (rc = {0}). Exception: {0}"; 

        internal const string MSG_055 = @"Не удалось построить команду c рекомендациями (rc = {0})"; 
        internal const string MSG_056 = @"БД ExternalDb. Не удалось отправить команду c рекомендациями (rc = {0})"; 
        internal const string MSG_057 = @"БД ExternalDb. Не удалось отправить команду c рекомендациями (rc = {0}). Error: {0}"; 
        internal const string MSG_058 = @"Не удалось построить команду c отгрузками (rc = {0})"; 
        internal const string MSG_059 = @"БД ExternalDb. Не удалось отправить команду c отрузками (rc = {0})"; 
        internal const string MSG_060 = @"БД ExternalDb. Не удалось отправить команду c отгузками (rc = {0}). Error: {0}"; 
        internal const string MSG_061 = @"БД ExternalDb. Не отправленная команда: {0}"; 
        internal const string MSG_062 = @"Не удалось отправить команду на отгрузку из очереди (rc = {0})"; 
        internal const string MSG_063 = @"Не удалось обработать покрытие (rc = {0})"; 

        internal const string MSG_064 = @"Не удалось построить CalcThreadContext (shops = {0}, orders = {1})"; 
        internal const string MSG_065 = @"Не удалось построить покрытие (rc = {0}, shops = {1}, orders = {2})";
        //                Logger.WriteToLog(301, $"CalcThread enter. service_id = {context.ServiceId}. order_count = {context.OrderCount}, shop_id = {context.ShopFrom.Id}, courier_id = {context.ShopCourier.Id}, level = {context.MaxRouteLength}", 0);

        internal const string MSG_066 = @"CalcThread enter. service_id = {0}, shop_id = {1}, orders = {2}, level = {3}, courier_id = {4}, vehicle_id = {5}"; 
        internal const string MSG_067 = @"CalcThread exit rc = {0}, elapsed_time = {1}, threads = {2}. service_id = {3}, shop_id = {4}, orders = {5}, level = {6}, courier_id = {7}, vehicle_id = {8}"; 

        internal const string MSG_068 = @"ReceiveData. queuing_order = {0}, message_type = {1}, message_body = {2}"; 
        internal const string MSG_069 = @"SendDeliveries. cmd_type = {0}, message_type = {1}, сmd = {2}"; 
        internal const string MSG_070 = @"RejectOrders. cmd_type = {0}, message_type = {1}, сmd = {2}"; 
        internal const string MSG_071 = @"SendDataRequest. message_type = {0}, request = {1}"; 
        internal const string MSG_072 = @"SendHeartbeat. message_type = {0}, message = {1}"; 
        internal const string MSG_073 = @"Timer_Elapsed. heartbeat_count = {0}, geo_count = {1}, recalc_count = {2}, queue_count = {3}"; 
        internal const string MSG_074 = @"DispatchQueue. item_count = {0} -> {1}"; 
        internal const string MSG_075 = @"GeoCache.Refresh. count = {0} -> {1}"; 

        internal const string MSG_076 = @"Calcs.GetCalcThreadContext enter. service_id = {0}"; 
        internal const string MSG_077 = @"Calcs.GetCalcThreadContext. service_id = {0}, after 2."; 
        internal const string MSG_078 = @"Calcs.GetCalcThreadContext. service_id = {0}, shop_id = {1}"; 
        internal const string MSG_079 = @"Calcs.GetCalcThreadContext. service_id = {0}, shop_id = {1}, shop_orders = {2}"; 
        internal const string MSG_080 = @"Calcs.GetCalcThreadContext. service_id = {0}, shop_id = {1}, shop_couriers = {2}"; 
        internal const string MSG_081 = @"Calcs.GetCalcThreadContext. service_id = {0}, shop_id = {1}, courier_vehicles = {2}"; 
        internal const string MSG_082 = @"Calcs.GetCalcThreadContext. service_id = {0}, shop_id = {1}, order_vehicles = {2}"; 
        internal const string MSG_083 = @"Calcs.GetCalcThreadContext. service_id = {0}, shop_id = {1}, enabled_vehicles = {2}"; 
        internal const string MSG_084 = @"Calcs.GetCalcThreadContext. service_id = {0}, shop_id = {1}. Способ доставки VehicleID = {2} не доступен"; 
        internal const string MSG_085 = @"Calcs.GetCalcThreadContext. service_id = {0}, context_count = {1}"; 
        internal const string MSG_086 = @"Calcs.GetCalcThreadContext exit rc = {0}. service_id = {1}, context_count = {2}"; 

        internal const string MSG_087 = @"Received orders. service_id = {0}, shop_id = {1}, order_id = {2}({3}), interval({4}) = ({5} - {6}), weight = {7}, dservices = [{8}]"; 
   }
}
