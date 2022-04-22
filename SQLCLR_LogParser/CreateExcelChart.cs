
namespace SQLCLR_LogParser
{
    using ClosedXML.Excel;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Windows.Forms;

    public class CreateExcelChart
    {
        public static int Create(string filename, int serviceId)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (string.IsNullOrWhiteSpace(filename) || !File.Exists(filename))
                    return rc;

                // 3. Создаём workbook
                rc = 3;
                string targetFilename = Path.ChangeExtension(filename, "xlsx");

                using (IXLWorkbook workbook = new XLWorkbook())
                {
                    // 4. Добавляем и форматируем лист
                    rc = 4;
                    IXLWorksheet worksheet = workbook.AddWorksheet("Elapsed Time");
                    worksheet.Cell(1, 1).Value = "Time";
                    worksheet.Cell(1, 2).Value = "Elapsed Time";
                    worksheet.Cell(1, 3).Value = "Orders";
                    worksheet.Range(1, 1, 1, 3).Style.Font.Bold = true;
                    worksheet.Range(1, 1, 1, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Range(1, 1, 1, 3).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    worksheet.Range(1, 1, 1, 3).SetAutoFilter();
                    worksheet.SheetView.FreezeRows(1);

                    // 5. Обрабатываем исходные данные
                    rc = 5;
                    int orderCount = 0;
                    //int shopCount = 0;
                    int elapsedTime = 0;
                    int ipos;
                    int row = 1;
                    int v;

                    using (StreamReader reader = new StreamReader(filename))
                    {
                        string line;

                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.StartsWith("@"))
                            {
                                rc = 42;
                                string dateTime = "";
                                string message_no = "";
                                string severity = "";
                                string message = "";

                                int iPos = line.IndexOf('>', 1);
                                if (iPos > 0)
                                {
                                    string[] headerItems = line.Substring(1, iPos - 1).Trim().Split(' ');
                                    switch (headerItems.Length)
                                    {
                                        case 4:
                                            dateTime = '@' + headerItems[0] + ' ' + headerItems[1];
                                            message_no = headerItems[2];
                                            severity = headerItems[3];
                                            message = line.Substring(iPos + 1).Trim();
                                            break;
                                        case 2:
                                            dateTime = '@' + headerItems[0] + ' ' + headerItems[1];
                                            severity = headerItems[2];
                                            message = line.Substring(iPos + 1).Trim();
                                            break;
                                        default:
                                            message = line;
                                            break;
                                    }
                                }

                                //const string shops = "Selected shops: ";
                                //const string orders = "Selected orders: ";
                                const string et = "Elapsed Time = ";
                                const string orders = "orders = ";
                                // @2022-04-19 16:44:29.110 101 info > lsvH4routes_clr. Service.ID = 2. Deliveries created (CreateDeliveries.rc = 0, orders =  32, Elapsed Time = 56534)

                                switch (message_no)
                                {
                                    //case "6":
                                    //    if (GetServiceID(message) == serviceId)
                                    //    {
                                    //        ipos = message.IndexOf(shops, StringComparison.CurrentCultureIgnoreCase);
                                    //        if (ipos > 0)
                                    //        {
                                    //            if (int.TryParse(message.Substring(ipos + shops.Length).Trim(), out v))
                                    //            { shopCount = v; }
                                    //        }
                                    //    }
                                    //    break;
                                    //case "9":
                                    //    if (GetServiceID(message) == serviceId)
                                    //    {
                                    //        ipos = message.IndexOf(orders, StringComparison.CurrentCultureIgnoreCase);
                                    //        if (ipos > 0)
                                    //        {
                                    //            if (int.TryParse(message.Substring(ipos + orders.Length).Trim(), out v))
                                    //            { orderCount = v; }
                                    //        }
                                    //    }
                                    //    break;
                                    case "101":
                                        if (GetServiceID(message) == serviceId)
                                        {
                                            orderCount = -1;
                                            ipos = message.IndexOf(orders, StringComparison.CurrentCultureIgnoreCase);
                                            if (iPos > 0)
                                            {
                                                int startIndex = ipos + orders.Length;
                                                int ipos1 = message.IndexOf(',', startIndex);
                                                if (ipos1 > 0)
                                                {

                                                    if (int.TryParse(message.Substring(startIndex, ipos1 - startIndex).Trim(), out v))
                                                    { orderCount = v; }
                                                }
                                            }

                                            elapsedTime = -1;
                                            ipos = message.IndexOf(et, StringComparison.CurrentCultureIgnoreCase);
                                            if (ipos > 0)
                                            {
                                                int startIndex = ipos + et.Length;
                                                if (int.TryParse(message.Substring(startIndex, message.Length - startIndex - 1).Trim(), out v))
                                                { elapsedTime = v; }
                                            }

                                            if (orderCount >= 0 || elapsedTime >= 0)
                                            {
                                                row++;
                                                worksheet.Cell(row, 1).Value = dateTime.Substring(12, 8);

                                                if (elapsedTime >= 0)
                                                { worksheet.Cell(row, 2).Value = elapsedTime; }
                                                if (orderCount >= 0)
                                                { worksheet.Cell(row, 3).Value = orderCount; }

                                            }
                                        }
                                        break;
                                }
                            }
                        }
                    }

                    // 6. Сохраняем файл
                    rc = 6;
                    workbook.SaveAs(targetFilename);
                }

                // 7. Открываем файл
                rc = 7;
                Process excel = new Process();
                excel.StartInfo.FileName = targetFilename;
                excel.StartInfo.UseShellExecute = true;
                excel.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                excel.Start();

                // 8. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Create Excel file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return rc;
            }
        }

        private static int GetServiceID(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return -1;
            int ipos = message.IndexOf("service_id = ");
            if (ipos < 0)
            {
                ipos = message.IndexOf("Service.ID = ");
                if (ipos < 0)
                    return -1;
            }

            int startpos = ipos + 13;
            ipos = message.IndexOf('.', startpos);
            if (ipos < 0)
                return -1;

            int id;
            if (int.TryParse(message.Substring(startpos, ipos - startpos).Trim(), out id))
                return id;
            return -1;
        }


        //@2022-04-22 00:00:50.423 46 info > s1DataRequest enter. args = <data service_id="2" all="0"/>
        //@2022-04-22 00:00:50.457 47 info > s1DataRequest exit. rc = 0, response = <data service_id="2"><shops><shop id="0" shop_id="4799" work_start="2022-04-22T00:00:00" work_end="2022-04-22T23:59:00" geo_lat="55.68099600" geo_lon="37.69286900" date_event="2022-04-22T00:00:50.420"/><shop id="0" shop_id="4801" work_start="2022-04-22T00:00:00" work_end="2022-04-22T23:59:00" geo_lat="55.83692400" geo_lon="37.41142700" date_event="2022-04-22T00:00:50.420"/><shop id="0" shop_id="5411" work_start="2022-04-22T00:00:00" work_end="2022-04-22T23:59:00" geo_lat="56.31739700" geo_lon="38.15435800" date_event="2022-04-22T00:00:50.420"/></shops></data>

        //@2022-04-22 00:00:50.490 48 info > s1Cmd4 enter. args = <confirmations service_id="2"><event id="0"/><event id="0"/><event id="0"/></confirmations>
        //@2022-04-22 00:00:50.523 49 info > s1Cmd4 exit (rc = 0)

        //@2022-04-22 00:04:51.773 52 info > s1Cmd1 enter. args = <deliveries service_id="2"><delivery guid="836d99a4-48dd-44d2-bcd2-a1ed25d17866" status="0" shop_id="4799" cause="0" delivery_service_id="50" courier_id="13" date_target="2022-04-22T00:04:51.000" date_target_end="2022-04-22T01:18:52.000" priority="5"><orders><order status="1" order_id="76362108"/></orders><alternative_delivery_service/><node_info calc_time="2022-04-22T00:04:51.000" cost="257.9742" weight="0.36" is_loop="False" start_delivery_interval="2022-04-22T00:04:51.000" end_delivery_interval="2022-04-22T01:18:52.000" reserve_time="74.02" delivery_time="45.13" execution_time="45.13"><node distance="0" duration="0"/><node distance="8318" duration="908"/><node distance="8664" duration="988"/></node_info><node_delivery_time><node delivery_time="23.0"/><node delivery_time="45.13"/><node delivery_time="68.6"/></node_delivery_time></delivery></deliveries>
        //@2022-04-22 00:04:51.897 53 info > s1Cmd1 exit. rc = 0, response = <result code="0"/>

        //@2022-04-22 00:20:51.927 58 info > s1Cmd3 enter. args = <rejections service_id="2"><rejection id="76357681" type_id="28" reason="TimeOver" error_code="-2"/><rejection id="76357681" type_id="16" reason="TimeOver" error_code="-2"/></rejections>
        //@2022-04-22 00:20:52.000 59 info > s1Cmd3 exit. rc = 0, response = <result code="0"/>

        //@2022-04-22 02:08:54.283 55 info > s1Cmd2 enter. args = <deliveries service_id="2"><delivery guid="3115c211-fd5f-4523-8221-e79d04907af7" status="1" shop_id="5411" cause="4" delivery_service_id="4" courier_id="1" date_target="2022-04-22T02:08:53.000" date_target_end="2022-04-22T02:10:05.000" priority="5"><orders><order status="2" order_id="76363862"/></orders><alternative_delivery_service/><node_info calc_time="2022-04-22T02:08:53.000" cost="196.4413" weight="6.11" is_loop="False" start_delivery_interval="2022-04-22T02:08:53.000" end_delivery_interval="2022-04-22T02:10:05.000" reserve_time="1.2" delivery_time="40.92" execution_time="40.92"><node distance="0" duration="0"/><node distance="4677" duration="655"/><node distance="4489" duration="589"/></node_info><node_delivery_time><node delivery_time="23.0"/><node delivery_time="40.92"/><node delivery_time="57.73"/></node_delivery_time></delivery></deliveries>
        //@2022-04-22 02:08:54.493 56 info > s1Cmd2 exit. rc = 0, response = <result code="0"/>

        public static int CreateS1(string filename)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (string.IsNullOrWhiteSpace(filename) || !File.Exists(filename))
                    return rc;

                // 3. Создаём workbook
                rc = 3;
                string targetFilename = Path.ChangeExtension(filename, "xlsx");

                using (IXLWorkbook workbook = new XLWorkbook())
                {
                    // 4. Добавляем и форматируем листы Data, Cmd1, Cmd2, Cmd3, Cmd4
                    rc = 4;
                    IXLWorksheet worksheetData = workbook.AddWorksheet("Data");
                    worksheetData.Cell(1, 1).Value = "Time";
                    worksheetData.Cell(1, 2).Value = "Elapsed Time";
                    worksheetData.Range(1, 1, 1, 2).Style.Font.Bold = true;
                    worksheetData.Range(1, 1, 1, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheetData.Range(1, 1, 1, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    worksheetData.Range(1, 1, 1, 2).SetAutoFilter();
                    worksheetData.SheetView.FreezeRows(1);

                    IXLWorksheet worksheetCmd1 = workbook.AddWorksheet("Cmd1");
                    worksheetCmd1.Cell(1, 1).Value = "Time";
                    worksheetCmd1.Cell(1, 2).Value = "Elapsed Time";
                    worksheetCmd1.Range(1, 1, 1, 2).Style.Font.Bold = true;
                    worksheetCmd1.Range(1, 1, 1, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheetCmd1.Range(1, 1, 1, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    worksheetCmd1.Range(1, 1, 1, 2).SetAutoFilter();
                    worksheetCmd1.SheetView.FreezeRows(1);

                    IXLWorksheet worksheetCmd2 = workbook.AddWorksheet("Cmd2");
                    worksheetCmd2.Cell(1, 1).Value = "Time";
                    worksheetCmd2.Cell(1, 2).Value = "Elapsed Time";
                    worksheetCmd2.Range(1, 1, 1, 2).Style.Font.Bold = true;
                    worksheetCmd2.Range(1, 1, 1, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheetCmd2.Range(1, 1, 1, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    worksheetCmd2.Range(1, 1, 1, 2).SetAutoFilter();
                    worksheetCmd2.SheetView.FreezeRows(1);

                    IXLWorksheet worksheetCmd3 = workbook.AddWorksheet("Cmd3");
                    worksheetCmd3.Cell(1, 1).Value = "Time";
                    worksheetCmd3.Cell(1, 2).Value = "Elapsed Time";
                    worksheetCmd3.Range(1, 1, 1, 2).Style.Font.Bold = true;
                    worksheetCmd3.Range(1, 1, 1, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheetCmd3.Range(1, 1, 1, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    worksheetCmd3.Range(1, 1, 1, 2).SetAutoFilter();
                    worksheetCmd3.SheetView.FreezeRows(1);

                    IXLWorksheet worksheetCmd4 = workbook.AddWorksheet("Cmd4");
                    worksheetCmd4.Cell(1, 1).Value = "Time";
                    worksheetCmd4.Cell(1, 2).Value = "Elapsed Time";
                    worksheetCmd4.Range(1, 1, 1, 2).Style.Font.Bold = true;
                    worksheetCmd4.Range(1, 1, 1, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheetCmd4.Range(1, 1, 1, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    worksheetCmd4.Range(1, 1, 1, 2).SetAutoFilter();
                    worksheetCmd4.SheetView.FreezeRows(1);

                    // 5. Обрабатываем исходные данные
                    rc = 5;
                    int row = 1;
                    int row1 = 1;
                    int row2 = 1;
                    int row3 = 1;
                    int row4 = 1;
                    DateTime t46;  // data enter
                    DateTime t47;  // data exit
                    DateTime t52;  // Cmd1 enter
                    DateTime t53;  // Cmd1 exit
                    DateTime t55;  // Cmd2 enter
                    DateTime t56;  // Cmd2 exit
                    DateTime t58;  // Cmd3 enter
                    DateTime t59;  // Cmd3 exit
                    DateTime t48;  // Cmd4 enter
                    DateTime t49;  // Cmd4 exit

                    using (StreamReader reader = new StreamReader(filename))
                    {
                        string line;
                        t46 = DateTime.MinValue;
                        t47 = DateTime.MinValue; 
                        t52 = DateTime.MinValue;  
                        t53 = DateTime.MinValue;  
                        t55 = DateTime.MinValue;  
                        t56 = DateTime.MinValue;  
                        t58 = DateTime.MinValue;  
                        t59 = DateTime.MinValue;  
                        t48 = DateTime.MinValue;  
                        t49 = DateTime.MinValue;  

                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.StartsWith("@"))
                            {
                                rc = 42;
                                string dateTime = "";
                                string message_no = "";
                                string severity = "";
                                string message = "";

                                int iPos = line.IndexOf('>', 1);
                                if (iPos > 0)
                                {
                                    string[] headerItems = line.Substring(1, iPos - 1).Trim().Split(' ');
                                    switch (headerItems.Length)
                                    {
                                        case 4:
                                            dateTime = '@' + headerItems[0] + ' ' + headerItems[1];
                                            message_no = headerItems[2];
                                            severity = headerItems[3];
                                            message = line.Substring(iPos + 1).Trim();
                                            break;
                                        case 2:
                                            dateTime = '@' + headerItems[0] + ' ' + headerItems[1];
                                            severity = headerItems[2];
                                            message = line.Substring(iPos + 1).Trim();
                                            break;
                                        default:
                                            message = line;
                                            break;
                                    }
                                }

                                switch (message_no)
                                {
                                    case "46":
                                        t46 = DateTime.Parse(dateTime.Substring(1));
                                        break;
                                    case "47":
                                        t47 = DateTime.Parse(dateTime.Substring(1));
                                        if (t46 != DateTime.MinValue)
                                        {
                                            row++;
                                            worksheetData.Cell(row, 1).Value = dateTime.Substring(12, 8);
                                            worksheetData.Cell(row, 2).Value = (int) (t47 - t46).TotalMilliseconds;
                                        }

                                        t46 = DateTime.MinValue;
                                        t47 = DateTime.MinValue; 
                                        break;
                                    case "52":
                                        t52 = DateTime.Parse(dateTime.Substring(1));
                                        break;
                                    case "53":
                                        t53 = DateTime.Parse(dateTime.Substring(1));
                                        if (t52 != DateTime.MinValue)
                                        {
                                            row1++;
                                            worksheetCmd1.Cell(row1, 1).Value = dateTime.Substring(12, 8);
                                            worksheetCmd1.Cell(row1, 2).Value = (int) (t53 - t52).TotalMilliseconds;
                                        }

                                        t52 = DateTime.MinValue;
                                        t53 = DateTime.MinValue; 
                                        break;
                                    case "55":
                                        t55 = DateTime.Parse(dateTime.Substring(1));
                                        break;
                                    case "56":
                                        t56 = DateTime.Parse(dateTime.Substring(1));
                                        if (t55 != DateTime.MinValue)
                                        {
                                            row2++;
                                            worksheetCmd2.Cell(row2, 1).Value = dateTime.Substring(12, 8);
                                            worksheetCmd2.Cell(row2, 2).Value = (int) (t56 - t55).TotalMilliseconds;
                                        }

                                        t55 = DateTime.MinValue;
                                        t56 = DateTime.MinValue; 
                                        break;
                                    case "58":
                                        t58 = DateTime.Parse(dateTime.Substring(1));
                                        break;
                                    case "59":
                                        t59 = DateTime.Parse(dateTime.Substring(1));
                                        if (t58 != DateTime.MinValue)
                                        {
                                            row3++;
                                            worksheetCmd3.Cell(row3, 1).Value = dateTime.Substring(12, 8);
                                            worksheetCmd3.Cell(row3, 2).Value = (int) (t59 - t58).TotalMilliseconds;
                                        }

                                        t58 = DateTime.MinValue;
                                        t59 = DateTime.MinValue; 
                                        break;
                                    case "48":
                                        t48 = DateTime.Parse(dateTime.Substring(1));
                                        break;
                                    case "49":
                                        t49 = DateTime.Parse(dateTime.Substring(1));
                                        if (t48 != DateTime.MinValue)
                                        {
                                            row4++;
                                            worksheetCmd4.Cell(row4, 1).Value = dateTime.Substring(12, 8);
                                            worksheetCmd4.Cell(row4, 2).Value = (int) (t49 - t48).TotalMilliseconds;
                                        }

                                        t48 = DateTime.MinValue;
                                        t49 = DateTime.MinValue; 
                                        break;
                                }
                            }
                        }
                    }

                    // 6. Сохраняем файл
                    rc = 6;
                    workbook.SaveAs(targetFilename);
                }

                // 7. Открываем файл
                rc = 7;
                Process excel = new Process();
                excel.StartInfo.FileName = targetFilename;
                excel.StartInfo.UseShellExecute = true;
                excel.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                excel.Start();

                // 8. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Create Excel file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return rc;
            }

        }
    }
}
