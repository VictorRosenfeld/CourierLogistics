
namespace SQLCLR.ExtraOrders
{
    using SQLCLR.Orders;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class GeoDistance
    {
        /// <summary>
        /// Earth Radius, meters
        /// </summary>
        private const double kEarthRadiusMs = 6376500;

        /// <summary>
        /// Подсчет сферического расстояния между
        /// двумя точками на Земной поверхности
        /// </summary>
        /// <param name="lat1">Широта точки 1</param>
        /// <param name="lon1">Долгота точки 1</param>
        /// <param name="lat2">Широта точки 2</param>
        /// <param name="lon2">Долгота точки 2</param>
        /// <returns>Расстояние между точками, метров</returns>
        public static double GetDistance(double lat1, double lon1, double lat2, double lon2)
        {
            //  The Haversine formula according to Dr. Math.
            //  http://mathforum.org/library/drmath/view/51879.html

            //  dlon = lon2 - lon1
            //  dlat = lat2 - lat1
            //  a = (sin(dlat/2))^2 + cos(lat1) * cos(lat2) * (sin(dlon/2))^2
            //  c = 2 * atan2(sqrt(a), sqrt(1-a)) 
            //  d = R * c

            //  Where
            //    * dlon is the change in longitude
            //    * dlat is the change in latitude
            //    * c is the great circle distance in Radians.
            //    * R is the radius of a spherical Earth.
            //    * The locations of the two points in 
            //        spherical coordinates (longitude and 
            //        latitude) are lon1,lat1 and lon2, lat2.


            double dDistance = double.NaN;

            double dLat1 = lat1 * (Math.PI / 180.0);
            double dLon1 = lon1 * (Math.PI / 180.0);
            double dLat2 = lat2 * (Math.PI / 180.0);
            double dLon2 = lon2 * (Math.PI / 180.0);

            double dLon = dLon2 - dLon1;
            double dLat = dLat2 - dLat1;

            // Intermediate result a.
            double a = Math.Pow(Math.Sin(dLat / 2.0), 2.0) +
                       Math.Cos(dLat1) * Math.Cos(dLat2) *
                       Math.Pow(Math.Sin(dLon / 2.0), 2.0);

            // Intermediate result c (great circle distance in Radians).
            double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));

            // Distance.
            dDistance = kEarthRadiusMs * c;

            return dDistance;
        }

        /// <summary>
        /// Расчет попарных расстояний между
        /// точками доставки заказов
        /// </summary>
        /// <param name="orders">Заказы</param>
        /// <returns>Попарные расстояния между точками</returns>
        public static double[,] CalcDistance(Order[] orders)
        {
            // 1. Инициализация
            
            try
            {
                // 2. Проверяем исходные данные
                if (orders == null || orders.Length <= 0)
                    return null;

                // 3. Производим расчеты
                double[,] dist = new double[orders.Length, orders.Length];

                for (int i = 0; i < orders.Length; i++)
                {
                    double lat1 = orders[i].Latitude;
                    double lon1 = orders[i].Longitude;

                    for (int j = i + 1; j < orders.Length; j++)
                    {
                        double d = GetDistance(lat1, lon1, orders[j].Latitude, orders[j].Longitude);
                        dist[i, j] = d;
                        dist[j, i] = d;
                    }
                }

                // 4. Выход - Ok
                return dist;
            }
            catch
            {
                return null;
            }

        }

    }
}
