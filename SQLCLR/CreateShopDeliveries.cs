using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using SQLCLR.Shop;

public partial class StoredProcedures
{
    /// <summary>
    /// ��������� ������� � �����
    /// </summary>
    private const string SELECT_AVAILABLE_COURIERS =
        "SELECT DISTINCT lsvCourierTypes.crtVehicleID, lsvCouriers.crsCourierID " +
        "FROM lsvCourierTypes INNER JOIN " +
        "        lsvCouriers ON lsvCourierTypes.crtVehicleID = lsvCouriers.crsVehicleID INNER JOIN " +
        "           lsvOrders INNER JOIN " +
        "              lsvOrderVehicleTypes ON lsvOrders.ordOrderID = lsvOrderVehicleTypes.ovtOrderID ON lsvCourierTypes.crtVehicleID = lsvOrderVehicleTypes.ovtVehicleID " +
        "WHERE (lsvOrders.ordShopID = @shop_id) AND " +
        "      (lsvCouriers.crsStatusID = 1) AND " +
        "      (lsvCouriers.crsShopID = @shop_id OR lsvCouriers.crsShopID = 0) AND " +
        "      (lsvOrders.ordStatusID = 1 OR lsvOrders.ordStatusID = 2);";

    /// <summary>
    /// ����� ������ ��������
    /// </summary>
    private const string SELECT_SHOP = "SELECT * FROM lsvShops WHERE shpShopID = @shop_id";



    [SqlProcedure]
    public static SqlInt32 CreateShopDeliveries(SqlInt32 serviceId, SqlInt32 shopId)
    {
        // 1. �������������
        int rc = 1;
        int rc1 = 1;

        try
        {
            // 2. ��������� �������� ������
            rc = 2;
            if (!SqlContext.IsAvailable)
                return rc;

            // 3. ��������� ���������� � ��������� ������� ������
            rc = 3;
            using (SqlConnection connection = new SqlConnection("context connection=true"))
            {
                // 3.1 ��������� ����������
                rc = 31;
                connection.Open();

                // 3.2 ������ ������ - �������
                Shop shop;
                rc1 = LoadShop(shopId.Value, connection, out shop);
                if (rc1 != 0)
                    return rc = 1000 * rc + rc1;

                // 3.3 ������ ������� - ������


            }

            return rc;
        }
        catch
        {
            return rc;
        }


        // Put your code here

        return 0;
    }

    /// <summary>
    /// ��������� ������ ��������
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="shop"></param>
    /// <returns></returns>
    private static int LoadShop(int shop_id, SqlConnection connection, out Shop shop)
    {
        // 1. �������������
        int rc = 1;
        shop = null;

        try
        {
            // 2. ��������� �������� ������
            rc = 2;
            if (connection == null || connection.State != ConnectionState.Open)
                return rc;

            // 3. �������� ������
            rc = 3;
            using (SqlCommand cmd = new SqlCommand(SELECT_SHOP, connection))
            {
                // 3.1 ������ �������
                rc = 31;
                SqlParameter param = new SqlParameter("@shop_id", SqlDbType.Int);
                param.IsNullable = false;
                param.Direction = ParameterDirection.Input;
                param.Value = shop_id;
                cmd.Parameters.Add(param);

                // 3.2 ��������� ������� � ������ ���������
                rc = 32;
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return rc;
                    shop = new Shop(shop_id);
                    shop.Latitude = reader.GetDouble(reader.GetOrdinal("shpLatitude"));
                    shop.Longitude = reader.GetDouble(reader.GetOrdinal("shpLongitude"));
                    shop.WorkStart = reader.GetTimeSpan(reader.GetOrdinal("shpWorkStart"));
                    shop.WorkEnd = reader.GetTimeSpan(reader.GetOrdinal("shpWorkEnd"));
                }
            }

            // 4. ����� - Ok
            rc = 0;
            return rc;
        }
        catch
        {
            return rc;
        }
    }
}
