using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Text;

//namespace SQLCLR.RouteCheck
//{
public partial class StoredProcedure
{
    /// <summary>
    /// �������� ��������
    /// </summary>
    /// <param name="request">������</param>
    /// <param name="response">������</param>
    /// <returns>0 - �������� ���������; ����� - �������� �� ���������</returns>
    public static SqlInt32 RouteCheck(SqlString request, out SqlString response)
    {
        // 1. �������������
        int rc = 1;
        response = null;

        try
        {
            // 2. ��������� �������� ������
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
