
namespace DeliveryBuilder.Log
{
    internal class Messages
    {
        internal const string MSG_001 = "---> {0} Ver. {1} Service ID = {2}";
        internal const string MSG_002 = "<--- {0} Ver. {1} Service ID = {2}";

        internal const string MSG_666 = "Method {0}. {1}";
        internal const string MSG_667 = "Method {0}. rc = {1}";
        internal const string MSG_668 = "{0}({1})";

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

    }
}
