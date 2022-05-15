
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


    }
}
