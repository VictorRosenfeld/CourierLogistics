
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
                    worksheet.Range(1, 1, 1, 2).Style.Font.Bold = true;
                    worksheet.Range(1, 1, 1, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Range(1, 1, 1, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    worksheet.Range(1, 1, 1, 2).SetAutoFilter();
                    worksheet.SheetView.FreezeRows(1);

                    // 5. Обрабатываем исходные данные
                    rc = 5;
                    int orderCount = 0;
                    int shopCount = 0;
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
                                            ipos = message.IndexOf(et, StringComparison.CurrentCultureIgnoreCase);
                                            if (ipos > 0)
                                            {
                                                int startIndex = ipos + et.Length;
                                                if (int.TryParse(message.Substring(startIndex, message.Length - startIndex - 1).Trim(), out v))
                                                {
                                                    elapsedTime = v;
                                                    row++;
                                                    worksheet.Cell(row, 1).Value = dateTime.Substring(12, 8);
                                                    worksheet.Cell(row, 2).Value = elapsedTime;
                                                    //if (shopCount > 0)
                                                    //{ worksheet.Cell(row, 3).Value = shopCount;}
                                                    //if (orderCount > 0)
                                                    //{ worksheet.Cell(row, 4).Value = orderCount;}

                                                    //shopCount = 0;
                                                    //orderCount = 0;
                                                }
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
    }
}
