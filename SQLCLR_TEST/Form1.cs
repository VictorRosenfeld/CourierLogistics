using SQLCLR.YandexGeoData;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SQLCLR_TEST
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void butGetGeoContext_Click(object sender, EventArgs e)
        {
            GeoRequestArgs requestArgs = new GeoRequestArgs();
            requestArgs.origins = new GeoPoint[79];
            requestArgs.destinations = new GeoPoint[79];
            requestArgs.modes = new string[] { "driving", "cycling", "walking" };
            GeoContext[] geoContext;

            int rc = GeoRequest.GetGeoContext(requestArgs, 100, out geoContext);
                int k = 0;

            for (int i = 5; i <= 1000; i++)
            {
                requestArgs.origins = new GeoPoint[i];
                requestArgs.destinations = new GeoPoint[i];
                rc = GeoRequest.GetGeoContext(requestArgs, 100, out geoContext);

                if (rc != 0)
                {
                    rc = rc;
                }
                else
                {
                    int i2 = i * i;
                    int n = i2 / 100;
                    if ((i2 % 100) > 0)
                        n++;
                    if (geoContext.Length != n)
                    {
                        Console.WriteLine($"{++k}. n = {i}. optimal = {n}, fact = {geoContext.Length}");
                    }
                }
            }
        }
    }
}
