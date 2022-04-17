
using Microsoft.SqlServer.Server;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;

public partial class StoredProcedures
{
    /// <summary>
    /// Запуск построения маршрутов в отдельном процессе
    /// </summary>
    /// <param name="programPath">Путь к запускаемому процессу</param>
    /// <param name="serverName">Имя сервера</param>
    /// <param name="dbName">Имя БД</param>
    /// <param name="service_id">ID сервиса логистики</param>
    /// <param name="calc_time">Время, на которое следует выполнить расчет</param>
    /// <param name="timeout">Timeout для ожидания завершения запускаемого процесса</param>
    /// <returns>0 - все маршруты успешно построены; иначе - маршруты не построены</returns>
    [SqlProcedure]
    public static SqlInt32 StartCreateDeliveries(SqlString programPath, SqlString serverName, SqlString dbName, SqlInt32 service_id, SqlDateTime calc_time, SqlInt32 timeout)
    {
        // 1. Инициализация
        int rc = 1;

        try
        {
            // 2. Проверяем исходные данные
            rc = 2;
            if (string.IsNullOrWhiteSpace(programPath.Value) || !File.Exists(programPath.Value))
                return rc;
            if (string.IsNullOrWhiteSpace(serverName.Value))
                return rc;
            if (string.IsNullOrWhiteSpace(dbName.Value))
                return rc;
            if (timeout.Value < - 1)
                return rc;

            // 3. Формируем аргументы вызывамого процесса
            rc = 3;
            string args = @"""" + serverName.Value + @""" """ + dbName.Value + @""" " + service_id.ToString() + @" """ + calc_time.Value.ToString()+ @"""";

            // 4. Запускаем процесс и дожидаемся его завершения
            rc = 4;
            using (Process calcProcess = Process.Start(programPath.Value, args))
            {
                if (calcProcess.WaitForExit(timeout.Value))
                {
                    rc = (calcProcess.ExitCode == 0 ? 0 : 10000 * rc + calcProcess.ExitCode);
                }
                else
                {
                    rc = 41;
                }
            }

            // 5. Выход...
            return rc;
        }
        catch
        {
            return rc;
        }
    }
}
