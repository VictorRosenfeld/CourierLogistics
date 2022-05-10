
/// <summary>
/// Работа с гео-данными
/// </summary>
namespace DeliveryBuilder.Geo
{
    using System;

    /// <summary>
    /// Гео-кэш
    /// </summary>
    public class GeoCache
    {
        /// <summary>
        /// Построение hash-значения для координат (latitude, longitude)
        /// </summary>
        /// <param name="latitude">Широта</param>
        /// <param name="longitude">Долгота</param>
        /// <returns>Hash-значение координат</returns>
        private static uint GetCoordinateHash(double latitude, double longitude)
        {
            double plat = Math.Abs(latitude);
            uint ilat = (uint)plat;
            uint flat = (uint)((plat - ilat + 0.00005) * 10000);
            double plon = Math.Abs(longitude);
            uint ilon = (uint)plon;
            uint flon = (uint)((plon - ilon + 0.00005) * 10000);
            ilat ^= ilon;
            ilat %= 42;
            return (ilat * 100000000) + (flat*10000) + flon;
        }

        /// <summary>
        /// Построение ключа для пары хэш-значений точек
        /// </summary>
        /// <param name="hash1">Хэш 1</param>
        /// <param name="hash2">Хэш 2</param>
        /// <returns>Ключ</returns>
        private static ulong GetKey(uint hash1, uint hash2)
        {
            return (ulong) hash1 << 32 | hash2;
        }

    }
}
