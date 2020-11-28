
namespace LogisticsService.Log
{
    /// <summary>
    /// Шаблоны сообщений, выводимых в лог
    /// </summary>
    public class MessagePatterns
    {
        public const string METHOD_FAIL = "0666 Error > Method {0}. {1}";
        public const string METHOD_RC = "0667 Error > Method {0}. rc = {1}";
        public const string METHOD_CALL = "0668 Error > {0}({1})";

        public const string BEGIN_SHIPMENT_REQUEST = "0001 Info > {0}";
        public const string BEGIN_SHIPMENT_POST_DATA = "0002 Info > {0}";
        public const string BEGIN_SHIPMENT_RESPONSE = "0003 Info > {0}";
        public const string BEGIN_SHIPMENT_ERROR_RESPONSE = "0004 Error > StatusCode {0}: {1}";

        public const string REJECT_ORDER_REQUEST = "0005 Info > {0}";
        public const string REJECT_ORDER_POST_DATA = "0006 Info > {0}";
        public const string REJECT_ORDER_RESPONSE = "0007 Info > {0}";
        public const string REJECT_ORDER_ERROR_RESPONSE = "0008 Error > StatusCode {0}: {1}";

        public const string COURIER_EVENTS_REQUEST = "0009 Info > {0}";
        public const string COURIER_EVENTS_RESPONSE = "0010 Info > {0}";
        public const string COURIER_EVENTS_ERROR_RESPONSE = "0011 Error > StatusCode {0}: {1}";

        public const string ORDER_EVENTS_REQUEST = "0012 Info > {0}";
        public const string ORDER_EVENTS_RESPONSE = "0013 Info > {0}";
        public const string ORDER_EVENTS_ERROR_RESPONSE = "0014 Error > StatusCode {0}: {1}";

        public const string SHOP_EVENTS_REQUEST = "0015 Info > {0}";
        public const string SHOP_EVENTS_RESPONSE = "0016 Info > {0}";
        public const string SHOP_EVENTS_ERROR_RESPONSE = "0017 Error > StatusCode {0}: {1}";

        public const string SEND_ACK_REQUEST = "0018 Info > {0}";
        public const string SEND_ACK_POST_DATA = "0019 Info > {0}";
        public const string SEND_ACK_RESPONSE = "0020 Info > {0}";
        public const string SEND_ACK_ERROR_RESPONSE = "0021 Error > StatusCode {0}: {1}";

        public const string TIME_DIST_REQUEST = "0022 Info > {0}";
        public const string TIME_DIST_POST_DATA = "0023 Info > {0}";
        public const string TIME_DIST_RESPONSE = "0024 Info > {0}";
        public const string TIME_DIST_ERROR_RESPONSE = "0025 Error > StatusCode {0}: {1}";

        public const string QUEUE_TIMER_ELAPSED = "0026 Info > QueueTimer_Elapsed ItemCount = {0}";
        public const string CREATE_SHIPMENT_QUEUE = "0027 Info > Создание очереди отгрузок ({0})";
        public const string QUEUE_INFO_TIMER_ELAPSED = "0028 Info > QueueInfoTimer_Elapsed ItemCount = {0}";

        public const string START_SERVICE = "0029 Info > ---> {0}. Ver = {1}";
        public const string STOP_SERVICE  = "0030 Info > <--- {0}. Ver = {1}";

        public const string GEO_CACHE_INFO = "0031 Info > GeoCache capacity: {0}. ItemCount: {1}";

        public const string QUEUE_STATE = "0032 Info > Состояние очереди:";
        public const string QUEUE_ITEM = "0033 Info >    Элемент {0}: {1}";

        public const string SHIPMENT_FROM_CHECKING_QUEUE1 = "0034 Info > (((( Отгрузки по событию из очереди предотвращения утечек";
        public const string SHIPMENT_FROM_CHECKING_QUEUE2 = "0035 Info > )))) Отгрузки по событию из очереди предотвращения утечек";

        public const string REJECT_ORDER_FROM_CHECKING_QUEUE1 = "0036 Info > (((( Отказ в доставке заказа по событию из очереди предотвращения утечек";
        public const string REJECT_ORDER_FROM_CHECKING_QUEUE2 = "0037 Info > )))) Отказ в доставке заказа по событию из очереди предотвращения утечек";

        public const string REJECT_ORDER_BY_TIME = "0038 WARNING > Для заказа {0} из магазина {1} истекло время доставки в срок. (delivery_frame_to: {2}, calc_time: {3})";
        public const string REJECT_ORDER_BY_COURIER = "0039 WARNING > Для заказа {0} из магазина {1} нет курьера в магазине. (delivery_frame_to: {2}, calc_time: {3})";

        public const string REJECT_ASSEMBLED_ORDER_BY_COURIER = "0040 WARNING > Собранный заказа {0} из магазина {1} не может быть доставлен в срок. (delivery_frame_to: {2}, calc_time: {3})";
        public const string REJECT_RECEIPTED_ORDER_BY_COURIER = "0041 WARNING > Принятый заказ {0} из магазина {1} не может быть доставлен в срок. (delivery_frame_to: {2}, calc_time: {3})";

        public const string CHECKING_QUEUE_INFO_TIMER_ELAPSED = "0042 Info > CheckingQueueTimer_Elapsed CheckingItemCount = {0}";

        public const string GEO_CACHE_PUT_INFO_ERROR = "0043 Error > GeoCache.PutLocationInfo. Способ доставки {0}. Исходных точек: {1}. Точек назначения: {2}. Код ошибки - {3}";

    }
}
