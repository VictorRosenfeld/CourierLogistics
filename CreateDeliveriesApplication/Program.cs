
namespace CreateDeliveriesApplication
{
    using CreateDeliveriesApplication.Deliveries;
    using CreateDeliveriesApplication.Log;
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Консольное приложение, выполняющее расчеты
    /// </summary>
    class Program
    {
        /// <summary>
        /// Команда скрытия окна при вызове ShowWindow
        /// </summary>
        const int SW_HIDE = 0;

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        /// <summary>
        /// Вход в консольное приложение
        /// </summary>
        /// <param name="args">
        /// args[0]  - server name;
        /// args[1]  - db name;
        /// args[2]  - service ID;
        /// args[3]  - calc time;
        /// </param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        static int Main(string[] args)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 1+ Скрываем окно консоли
                rc = 10;
                IntPtr hWnd = Process.GetCurrentProcess().MainWindowHandle;
                if (hWnd != IntPtr.Zero)
                { ShowWindow(hWnd, SW_HIDE); }

                // 2. Проверяем исходные данные
                rc = 2;
                if (args == null || args.Length < 4)
                    return rc;
                string serverName = args[0];
                if (string.IsNullOrWhiteSpace(serverName))
                    return rc = 21;

                string dbName = args[1];
                if (string.IsNullOrWhiteSpace(dbName))
                    return rc = 22;

                int serviceId = -1;
                if (!int.TryParse(args[2], out serviceId))
                    return rc = 23;

                DateTime calcTime;
                if (!DateTime.TryParse(args[3], out calcTime))
                    return rc = 24;

                // 3. Запускаем расчеты
                rc = 3;
                CalcConfig config = new CalcConfig();
                config.ServerName = serverName.Trim();
                config.DbName = dbName.Trim();
                config.ServiceId = serviceId;
                config.CalcTime = calcTime;
                int rc1 = CreateDeliveries.Create(config);

                // 4. Выход
                return rc1;
            }
            catch (Exception ex)
            {
#if debug
                Logger.WriteToLog(1000, $"Main exception: {ex.Message}", 0);
#endif
                return rc;
            }
        }
    }
}
