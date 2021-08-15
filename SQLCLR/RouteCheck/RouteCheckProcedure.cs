using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Text;

//namespace SQLCLR.RouteCheck
//{
public partial class StoredProcedure
{
    /// <summary>
    /// Проверка маршрута
    /// </summary>
    /// <param name="request">Запрос</param>
    /// <param name="response">Отклик</param>
    /// <returns>0 - отгрузка построена; иначе - отгрузка не построена</returns>
    public static SqlInt32 RouteCheck(SqlString request, out SqlString response)
    {
        // 1. Инициализация
        int rc = 1;
        response = null;

        try
        {
            // 2. Проверяем исходные данные
            rc = 2;
            if (request.IsNull)
                return rc;

            // 3.
            return rc;
        }
        catch
        {
            return rc;
        }
    }

}
//}
