
namespace CourierLogistics
{
    using CourierLogistics.Logistics;
    using CourierLogistics.Logistics.RealSingleShopSolution;
    using LogAnalyzer.Analyzer;
    using LogisticsService.API;
    using LogisticsService.FixedCourierService;
    using LogisticsService.ServiceParameters;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    //using System.Net.Http;
    using System.Windows.Forms;
    using static LogisticsService.API.GetOrderEvents;
    using static LogisticsService.API.GetShippingInfo;

    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void butStart_Click(object sender, EventArgs e)
        {
            //ShopStatistics stat = new ShopStatistics();
            //int rs = stat.Load(@"C:\Users\Виктор\source\repos\CourierLogistics\CourierLogistics\bin\Release\LogisticsReport8.xlsx");

            //string result = Helper.ReplaceSemicolon("1222; 743; \"ss dd ff ; kk mm\"; 723; NULL");


            //double ss = Math.Ceiling(10.0);
            //double ss1 = Math.Ceiling(10.11);

            //PermutationsGenerator generator = new PermutationsGenerator();
            //int rc1 = generator.Create(5);


            Main logistics = new Main();
            string shopsFile = Properties.Settings.Default.ShopsFile;
            string shopDeliveryTypesFile = Properties.Settings.Default.shopDeliveryTypesFile;
            string ordersFile = Properties.Settings.Default.OrdersFile;
            int rc = logistics.Create(shopsFile, shopDeliveryTypesFile, ordersFile);
        }

        private void butRealModel_Click(object sender, EventArgs e)
        {
            Main logistics = new Main();
            string shopsFile = Properties.Settings.Default.ShopsFile;
            string shopDeliveryTypesFile = Properties.Settings.Default.shopDeliveryTypesFile;
            string ordersFile = Properties.Settings.Default.OrdersFile;
            string statisticsFile = @"C:\Users\Виктор\source\repos\CourierLogistics\CourierLogistics\bin\Release\LogisticsReport10.xlsx";
            int rc = logistics.CreateReal(shopsFile, shopDeliveryTypesFile, ordersFile, statisticsFile);
        }

        private void butFloatCouriers_Click(object sender, EventArgs e)
        {
            Main logistics = new Main();
            string shopsFile = Properties.Settings.Default.ShopsFile;
            string shopDeliveryTypesFile = Properties.Settings.Default.shopDeliveryTypesFile;
            string ordersFile = Properties.Settings.Default.OrdersFile;
            string statisticsFile = @"C:\Users\Виктор\source\repos\CourierLogistics\CourierLogistics\bin\Release\LogisticsReport10.xlsx";
            int rc = logistics.CreateFloat(shopsFile, shopDeliveryTypesFile, ordersFile, statisticsFile);

        }

        private void butFloatOptimalModel_Click(object sender, EventArgs e)
        {
            Main logistics = new Main();
            string shopsFile = Properties.Settings.Default.ShopsFile;
            string ordersFile = Properties.Settings.Default.OrdersFile;
            string statisticsFile = @"C:\Users\Виктор\source\repos\CourierLogistics\CourierLogistics\bin\Release\LogisticsReport10.xlsx";
            int rc = logistics.CreateFloatOptimal(shopsFile, ordersFile, statisticsFile);
        }

        private void CopyCourierType(SourceData.Couriers.ICourierType srcCourierType, CourierParameters dstCourierType)
        {
            dstCourierType.VechicleType = (LogisticsService.Couriers.CourierVehicleType) srcCourierType.VechicleType;
            dstCourierType.MaxWeight = srcCourierType.MaxWeight;
            dstCourierType.HourlyRate = srcCourierType.HourlyRate;
            dstCourierType.MaxDistance = srcCourierType.MaxDistance;
            dstCourierType.MaxOrderCount = srcCourierType.MaxOrderCount;
            dstCourierType.Insurance = srcCourierType.Insurance;
            dstCourierType.GetOrderTime = srcCourierType.GetOrderTime;
            dstCourierType.HandInTime = srcCourierType.HandInTime;
            dstCourierType.StartDelay = srcCourierType.StartDelay;
            dstCourierType.FirstPay = srcCourierType.FirstPay;
            dstCourierType.SecondPay = srcCourierType.SecondPay;
            dstCourierType.FirstDistance = srcCourierType.FirstDistance;
            dstCourierType.AdditionalKilometerCost = srcCourierType.AdditionalKilometerCost;
            dstCourierType.IsTaxi = (dstCourierType.VechicleType == LogisticsService.Couriers.CourierVehicleType.YandexTaxi || dstCourierType.VechicleType == LogisticsService.Couriers.CourierVehicleType.GettTaxi);
        }

        private void butCompareDistance_Click(object sender, EventArgs e)
        {
            ServiceConfig config1 = ServiceConfig.Deserialize(@"C:\Tx\Config1.json");



            string statisticsFile = @"C:\Users\Виктор\source\repos\CourierLogistics\CourierLogistics\bin\Release\LogisticsReport10.xlsx";

            ShopStatistics stat = new ShopStatistics();
            int rc1 = stat.Load(statisticsFile);
            //CultureInfo cultureInfo = CultureInfo.GetCultureInfo("US");

            //using (StreamWriter sw = new StreamWriter(@"C:\Tx\AverageCost.Json", false, Encoding.UTF8))
            //{
            //    sw.WriteLine(@"""average_cost"": [");
            //    foreach (var courierStat in stat.CSStatistics.Statistics)
            //    {
            //        if (courierStat.Date.Day == 8)
            //        {
            //            sw.WriteLine($@"                  {{ ""shop_id"": {courierStat.ShopId}, ""vehicle_type"": {(int)courierStat.CourierType}, ""cost"":{string.Format(cultureInfo, "{0:0.00}", courierStat.OrderCost)}}}, ");
            //        }
            //    }

            //    sw.WriteLine("                ]");
            //    sw.Close();
            //}

            ServiceConfig config = new ServiceConfig();
            SourceData.Couriers.ICourierType car = new SourceData.Couriers.CourierType_Car();
            SourceData.Couriers.ICourierType bicycle = new SourceData.Couriers.CourierType_Bicycle();
            SourceData.Couriers.ICourierType onFoot = new SourceData.Couriers.CourierType_OnFoot();
            SourceData.Couriers.ICourierType yandexTaxi = new SourceData.Couriers.CourierType_YandexTaxi();
            SourceData.Couriers.ICourierType gettTaxi = new SourceData.Couriers.CourierType_GettTaxi();

            CourierParameters car_params = new CourierParameters();
            CourierParameters bicycle_params = new CourierParameters();
            CourierParameters onFoot_params = new CourierParameters();
            CourierParameters yandexTaxi_params = new CourierParameters();
            CourierParameters gettTaxi_params = new CourierParameters();

            CopyCourierType(car, car_params);
            CopyCourierType(bicycle, bicycle_params);
            CopyCourierType(onFoot, onFoot_params);
            CopyCourierType(yandexTaxi, yandexTaxi_params);
            CopyCourierType(gettTaxi, gettTaxi_params);

            config.couriers = new CourierParameters[] { car_params, bicycle_params, onFoot_params, yandexTaxi_params, gettTaxi_params };

            FunctionalParameters funcParameters = new FunctionalParameters();
            funcParameters.courier_alert_interval = 1;
            funcParameters.max_orders_at_traveling_salesman_problem = 8;
            funcParameters.max_orders_for_search_solution = 10;
            funcParameters.min_work_time = 4;
            funcParameters.taxi_alert_interval = 1;
            config.functional_parameters = funcParameters;

            AverageCostByVechicle[] averageCost = new AverageCostByVechicle[10000];
            int recCount = 0;

            foreach (var courierStat in stat.CSStatistics.Statistics)
            {
                if (courierStat.Date.Day == 8)
                {
                    AverageCostByVechicle rec = new AverageCostByVechicle();
                    rec.average_cost = courierStat.OrderCost;
                    rec.shop_id = courierStat.ShopId;
                    rec.vehicle_type = (LogisticsService.Couriers.CourierVehicleType) ((int) courierStat.CourierType);
                    averageCost[recCount++] = rec;
                }
            }

            if (recCount < averageCost.Length)
            {
                Array.Resize(ref averageCost, recCount);
            }

            config.average_cost = averageCost;

            //string json = config.Serialize();
            //File.WriteAllText(@"C:\Tx\Config.json", json, Encoding.UTF8);

            //ServiceConfig config1 = ServiceConfig.Deserialize(@"C:\Tx\Config1.json");

                //config.couriers = new LogisticsService.Couriers.ICourierType


            //LogisticsService.ServiceParameters.CourierParameters parameters = new LogisticsService.ServiceParameters.CourierParameters();
            //string json = parameters.Serialize();


            int rcx = 0;

            rcx = SendAck.Send(5);

            //CourierEvent[] ce;
            //rcx = GetCourierEvents.GetEvents(1, out ce);

            OrderEvent[] oe;
            rcx = GetOrderEvents.GetEvents(1, out oe);

            rcx = 0;

            //ShopEvent[] se;
            //rcx = GetShopEvents.GetEvents(1, out se);

            //Shipment shipment = new Shipment();
            //shipment.id = "123-456-7890";
            //shipment.status = 0;
            //shipment.shop_id = 1231;
            //shipment.delivery_service_id = 14;
            //shipment.courier_id = 144324234;
            //shipment.date_target = DateTime.Now;
            //shipment.date_target_end = shipment.date_target.AddMinutes(23);
            //shipment.orders = new int[] { 123, 456};

            //rcx = BeginShipment.Begin(shipment);

            //StatisticsRequest statRequest = new StatisticsRequest();
            //statRequest.shop_id = 1665;
            //statRequest.date_target = DateTime.Now.AddDays(-1).Date;
            //statRequest.types = new int[] { 0, 1, 2, 12, 14};
            //StatisticsResponse statResponse;
            //rcx = GetDeliveryStatistics.GetOrderDeliveryCost(statRequest, out statResponse);

            ShippingInfoRequestEx requestData = new ShippingInfoRequestEx();
            requestData.modes = new string[] { "walking", "driving", "cycling" };
            double[][] origins = new double[2][];
            double[][] destinations = new double[3][];
            origins[0] = new double[] { 54.00011, 53.00011};
            origins[1] = new double[] { 54.00012, 53.00012 };
            destinations[0] = new double[] { 54.00021, 53.00021};
            destinations[1] = new double[] { 54.00022, 53.00022};
            destinations[2] = new double[] { 54.00023, 53.00023};
            requestData.origins = origins;
            requestData.destinations = destinations;
            ShippingInfoResponse responsetData;
            rcx = GetShippingInfo.GetInfo(requestData, out responsetData);



            rcx = 0;
            // Send the POST Request to the Authentication Server
            // Error Here
            //string json = Task.Run(() => JsonConvert.SerializeObject(this));
            //HttpContent httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            //using (var httpClient = new HttpClient())
            //{
            //    // Error here
            //    var httpResponse = httpClient.PostAsync("URL HERE", httpContent);
            //    if (httpResponse.Result.Content != null)
            //    {
            //        // Error Here
            //        var responseContent = httpResponse.Result.Content.ReadAsStringAsync();
            //    }
            //}

            //NewEvents events = new NewEvents();
            //events.RequestType = 1;
            //events.RequestTime = DateTime.Now;

            //// Couriers
            //CourierEvents ce1 = new CourierEvents();
            //ce1.CourierId = 1;
            //ce1.DeliveryId = Guid.NewGuid().ToString();
            //ce1.EventTime = DateTime.Now;
            //ce1.ShopId = 1;
            //ce1.Type = 2;
            //ce1.WorkTime = new TimeInterval { StartTime = DateTime.Now.Date.AddHours(10), EndTime = DateTime.Now.Date.AddHours(19) };

            //CourierEvents ce2 = new CourierEvents();
            //ce2.CourierId = 2;
            //ce2.DeliveryId = null;
            //ce2.EventTime = DateTime.Now;
            //ce2.ShopId = 1;
            //ce2.Type =  1;
            //ce2.WorkTime = new TimeInterval { StartTime = DateTime.Now.Date.AddHours(10), EndTime = DateTime.Now.Date.AddHours(19) };

            //events.Couriers = new CourierEvents[] { ce1, ce2 };

            //// Shops
            //ShopEvents se1 = new ShopEvents();
            //se1.ShopId = 1;
            //se1.WorkTime = new TimeInterval { StartTime = DateTime.Now.Date.AddHours(9), EndTime = DateTime.Now.Date.AddHours(20) };

            //ShopOrder so1 = new ShopOrder();
            //so1.Id = 11;
            //so1.Type = 0;
            //so1.EventTime = DateTime.Now.Date.AddHours(8);
            //so1.DeliveryEndTime = DateTime.Now.Date.AddHours(12);
            //so1.Latitude = 20.123456;
            //so1.Longitude = 32.123456;
            //so1.Weight = 3.5;
            //ShippingMethod sm1 = new ShippingMethod();
            //sm1.Type = 1;
            //sm1.Distance12 = 1.3;
            //sm1.TimeOfDelivery12 = 15;
            //sm1.Distance21 = 1.35;
            //sm1.TimeOfDelivery12 = 15.1;

            //ShippingMethod sm2 = new ShippingMethod();
            //sm2.Type = 2;
            //sm2.Distance12 = 1.5;
            //sm2.TimeOfDelivery12 = 4.1;
            //sm2.Distance21 = 1.55;
            //sm2.TimeOfDelivery12 = 4.2;

            //so1.ShippingInfo = new ShippingMethod[] { sm1, sm2 };

            //se1.WorkTime = new TimeInterval { StartTime = DateTime.Now.Date.AddHours(9), EndTime = DateTime.Now.Date.AddHours(20) };

            //ShopOrder so2 = new ShopOrder();
            //so2.Id = 22;
            //so2.Type = 2;
            //so2.EventTime = DateTime.Now.Date.AddHours(10.2);
            //so2.DeliveryEndTime = DateTime.Now.Date.AddHours(12);
            //so2.Latitude = 22.123456;
            //so2.Longitude = 35.123456;
            //so2.Weight = 4.7;

            //so2.ShippingInfo = new ShippingMethod[] { sm1, sm2 };

            //se1.Orders = new ShopOrder[] { so1, so2 };
            //events.Shops = new ShopEvents[] { se1 };

            //string json = events.Serialize();

            int rc = CompareDistance(@"C:\Tz\source.csv");
        }

        private static int CompareDistance(string fileName)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Читаем файл целиком
                rc = 2;
                string[] rows = File.ReadAllLines(fileName);

                // 3. Сздаём имя файла результата
                rc = 3;
                string resultFile = Path.GetFileNameWithoutExtension(fileName) + "_cmp.csv";
                resultFile = Path.Combine(Path.GetDirectoryName(fileName), resultFile);

                // 4. Цикл обработки
                rc = 4;
                using (StreamWriter sw = new StreamWriter(resultFile, false))
                {
                    sw.WriteLine("id; type; latitude1; longitude1; latitude2; longitude2; y-dist; s-dist; y-duration");

                    for (int i = 1; i < rows.Length; i++)
                    {
                        //if  (i == 97634)
                        //{
                        //    i = i;
                        //}

                        // 4.1 Извлекаем строку
                        rc = 41;
                        string row = rows[i].Trim();
                        if (string.IsNullOrWhiteSpace(row))
                            continue;

                        // 4.2 Разбиваем на элементы
                        rc = 42;
                        string[] items = rows[i].Split(';');
                        if (items.Length < 12)
                            continue;

                        // 4.3 Извлекаем данные
                        rc = 43;
                        double latitude1 = Helper.ParseDouble(items[0]);
                        if (double.IsNaN(latitude1))
                            continue;
                        double longitude1 = Helper.ParseDouble(items[1]);
                        if (double.IsNaN(longitude1))
                            continue;
                        double latitude2 = Helper.ParseDouble(items[10]);
                        if (double.IsNaN(latitude2))
                            continue;
                        double longitude2 = Helper.ParseDouble(items[11]);
                        if (double.IsNaN(longitude2))
                            continue;
                        double distance = Helper.ParseDouble(items[6]);
                        if (double.IsNaN(distance))
                            continue;
                        DateTime duration = Helper.ParseDateTime(items[7]);
                        if (duration == DateTime.MinValue)
                            continue;
                        int id = Helper.ParseInt(items[3]);
                        if (id == int.MinValue)
                            continue;

                        string vtype = items[4].Trim();

                        double approxDist = 1.2 * Helper.Distance(latitude1, longitude1, latitude2, longitude2);

                        sw.WriteLine($"{id}; {vtype}; {latitude1}; {longitude1}; {latitude2}; {longitude2}; {distance}; {approxDist}; {duration.TimeOfDay}");
                    }

                    sw.Close();    
                }

                //double d = 1.2 * Helper.Distance(55.585, 37.8962, 55.563136, 37.855419);
                //double d = 1.2 * Helper.Distance(56.0247, 36.6057, 56.030517, 35.959363);

                // 5. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Compare Distance", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return rc;
            }
        }

        //FixedService service;
        //FixedServiceEx service;
        //FixedServiceEy service;
        FixedServiceEz service;

        private void butStartFixedService_Click(object sender, EventArgs e)
        {
            if (service != null)
                return;
            //string logPath = Path.ChangeExtension(Process.GetCurrentProcess().MainModule.FileName, "log");


            //string path = AppDomain.CurrentDomain.BaseDirectory;
            //string processName = Process.GetCurrentProcess().StartInfo;
            //var proc = Process.GetCurrentProcess();
            //service = new FixedService();
            //service = new FixedServiceEx();
            //service = new FixedServiceEy();
            service = new FixedServiceEz();
            //string jsonFile = @"C:\Users\Виктор\source\repos\CourierLogistics\LogisticsService\ServiceParameters.json";
            string jsonFile = @"ServiceParameters.json";
            int rc = service.Create(jsonFile);
            if (rc != 0)
                return;
            service.Start();

        }

        /// <summary>
        /// Событие формы Closed
        /// </summary>
        /// <param name="sender">Форма</param>
        /// <param name="e">Аргументы события</param>
        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (service != null)
            {
                try { service.Dispose(); service = null; } catch { }
            }

            try { TrayIcon.Dispose(); } catch { }
            try { if (locker != null) locker.Dispose(); } catch { }
        }

        Mutex locker = null;

        /// <summary>
        /// Событие формы Load
        /// </summary>
        /// <param name="sender">Форма</param>
        /// <param name="e">Аргументы события</param>
        private void MainForm_Load(object sender, EventArgs e)
        {
            AnalyzerConfig analyzerConfig = new AnalyzerConfig();
            analyzerConfig.OpenReport = true;
            analyzerConfig.ReportFile = @"C:\T2\Report\reprt.xlsx";
            ServiceLogAnalyzer analyzer = new ServiceLogAnalyzer(analyzerConfig);
            int rcd = analyzer.Create(@"C:\Users\Виктор\source\repos\CourierLogisticsEx\CourierLogistics\bin\Debug\CourierLogistics.log", @"C:\T2\Report\reprt.xlsx");

            try
            {
                // 1. Создам сервис
                service = new FixedServiceEz();
                //string jsonFile = @"C:\Users\Виктор\source\repos\CourierLogistics\LogisticsService\ServiceParameters.json";
                string jsonFile = @"ServiceParameters.json";
                int rc = service.Create(jsonFile);
                if (rc != 0)
                {
                    MessageBox.Show($"Не удалось создать сервис (rc = {rc}) !", "LogisticsService", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isTrayExit = true;
                    Close();
                    return;
                }

                // 2. Проверяем, что сервис с тем же service_id ещё не запущен
                try
                {
                    bool createdNew;
                    locker = new Mutex(true, $"580D37FF-D838-46E8-A350-DB10B6207E6C-{service.Config.functional_parameters.service_id}", out createdNew);
                    if (!createdNew)
                    {
                        MessageBox.Show($"LogisticsService c service_id = {service.Config.functional_parameters.service_id} уже запущен !", "LogisticsService", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        //locker.Dispose();
                        //locker = null;
                        isTrayExit = true;
                        Close();
                        return;
                    }
                }
                catch
                { }

                // 3. Запускаем сервис
                TrayIcon.Text = $"LogisticsService ver. {Application.ProductVersion} (service_id {service.Config.functional_parameters.service_id})";
                service.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "LogisticsService", MessageBoxButtons.OK, MessageBoxIcon.Error);
                isTrayExit = true;
                Close();
            }
        }

        /// <summary>
        /// Событие формы Shown
        /// </summary>
        /// <param name="sender">Форма</param>
        /// <param name="e">Аргументы события</param>
        private void MainForm_Shown(object sender, EventArgs e)
        {
            this.Visible = false;
        }

        /// <summary>
        /// Пункт ShowLog Tray-меню 
        /// </summary>
        /// <param name="sender">MenuItem</param>
        /// <param name="e">Аргументы события</param>
        private void menuTrayIcon_ShowLog_Click(object sender, EventArgs e)
        {
            try
            {
                Process notepadProcess = new Process();
                notepadProcess.StartInfo.Arguments = "";
                notepadProcess.StartInfo.FileName = service.LogFileName;
                notepadProcess.StartInfo.UseShellExecute = true;
                notepadProcess.Start();
            }
            catch
            { }
        }

        /// <summary>
        /// Пункт Exit Tray-меню 
        /// </summary>
        /// <param name="sender">MenuItem</param>
        /// <param name="e">Аргументы события</param>
        private void menuTrayIcon_Exit_Click(object sender, EventArgs e)
        {
            try
            {
                isTrayExit = true;
                this.Close();
            }
            catch
            { }
        }

        bool isTrayExit = false;

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && !isTrayExit)
            {
                e.Cancel = true;
                this.Visible = false;
            }
        }
    }
}
