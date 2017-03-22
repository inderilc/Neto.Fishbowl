using Integration.Configuration;
using Integration.Controller;
using Integration.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ShopifySharp;
using FishbowlSDK;
using Integration.Models;
using SalesOrder = Integration.Models.SalesOrder;
using SalesOrderItem = Integration.Models.SalesOrderItem;

namespace Integration
{
    public class ShopifyIntegration : IDisposable
    {
        public event LogMsg OnLog;
        public delegate void LogMsg(String msg);

        private Config cfg { get; set; }
        private FishbowlController fb { get; set; }
        private ShopifyController sc { get; set; }

        public ShopifyIntegration(Config cfg)
        {
            this.cfg = cfg;
            if (sc == null)
            {
                sc = new ShopifyController(cfg);
            }
            if (fb == null)
            {
                fb = new FishbowlController(cfg);
            }
        }

        public void Run()
        {
            DownloadOrders();
            ShippingTracking();
        }

        private void ShippingTracking()
        {
            Dictionary<String, DateTime> timeStampped = new Dictionary<string, DateTime>();

            foreach (KeyValuePair<String, IntegrationConfiguration> site in cfg.ShopifySites)
            {
                if (site.Value.StoreConfig.Actions.SyncShipments)
                {
                    sc = new ShopifyController(site);
                    Log("Updating Shipments for site "+site.Key);
                    var shipments = fb.GetShipments(site.Value.StoreConfig.ShippingTracking.LastShippingUpdated, site.Value.StoreConfig.Mapping.StoreCode);
                    Log("Total Shipments to Updated: " + shipments.Count);

                    if (shipments.Count > 0)
                    {
                        var shp = shipments.GroupBy(k => k.CPO);

                        foreach (var s in shp)
                        {
                            var orderid = (long)Convert.ToDouble(s.First().CPO);
                            var first = s.First();
                            if (orderid > 0)
                            {
                                var trks = s.ToList().GroupBy(k => k.TRACKINGNUM);
                                foreach (var t in trks)
                                {
                                    bool updated = sc.UpdateShipmentStatus(orderid, first.CARRIERNAME, t.Key, t.ToList());

                                    if (updated)
                                    {
                                        Log($"Updated Order [{first.ORDERNUM}] / [{first.CPO}] / [{first.SNUM}] with Tracking : [{t.Key}]");
                                    }
                                    else
                                    {
                                        Log($"UNABLE TO UPDATE Order [{first.ORDERNUM}] / [{first.CPO}] / [{first.SNUM}] with Tracking : [{t.Key}]");
                                    }
                                }
                            }
                            else
                            {
                                Log($"Skipping Order [{first.SNUM}] Customer PO [{first.CPO}] to mark ship, possibly not a Cart Order.");
                            }
                        }
                    }

                    timeStampped.Add(site.Key, DateTime.Now);
                }
            }
            foreach (KeyValuePair<String, DateTime> s in timeStampped)
            {
                cfg.ShopifySites[s.Key].StoreConfig.ShippingTracking.LastShippingUpdated = s.Value;
            }
            Config.Save(cfg);
        }

        private async void DownloadOrders()
        {
            Dictionary<String, DateTime> timeStampped = new Dictionary<string, DateTime>();

            foreach (KeyValuePair<String, IntegrationConfiguration> site in cfg.ShopifySites)
            {
                if (site.Value.StoreConfig.Actions.SyncOrders)
                {
                    sc = new ShopifyController(site);
                    List<ShopifyOrder> orders = new List<ShopifyOrder>();
                    Log("Downloading Orders from " + site.Key);
                    if (site.Value.StoreConfig.OrderDownload.TestOrderNumber == 0)
                    {
                        List<ShopifyOrder> standradorders = sc.GetOrders();
                        orders.AddRange(standradorders);
                    }
                    else
                    {
                        List<ShopifyOrder> testorder = sc.GetTestOrder();
                        orders.AddRange(testorder);
                    }
                    Log("Downloaded " + orders.Count + " orders from site " + site.Key);

                    foreach (ShopifyOrder o in orders)
                    {
                        String paymentTerms = "PREPAID";
                        string ErrorMessage = "";
                        bool importOrder = false;
                        if (o.FinancialStatus == "pending")
                        {
                            Customer cust = fb.api.GetCustomer(o.Customer.FirstName + " " + o.Customer.LastName);
                            if (cust != null && cust.DefPaymentTerms == "NET30")
                            {
                                importOrder = true;
                                paymentTerms = cust.DefPaymentTerms;
                            }
                        }
                        else
                        {
                            importOrder = true;
                        }

                        if (importOrder)
                        {
                            bool SoExists = fb.CheckSOExist(o.Id.ToString(), site.Value.StoreConfig.Mapping.StoreCode);
                            String custName = ValidateCreateCustomer(o, site.Value.StoreConfig.Mapping);

                            SalesOrder So = DataMappers.MapSalesOrderX(fb, custName, o, site.Value.StoreConfig.Mapping, paymentTerms);

                            if (SoExists)
                            {
                                Log($"Order Exists, Skipping. Order ID [{o.Id}] Increment ID [{o.OrderNumber}] ");
                                // Update Shopify Tag
                                sc.UpdateOrderStatus(o.Id.Value, site.Value.StoreConfig.OrderDownload.DownloadedStatus);
                            }
                            else
                            {
                                Log($"Creating Order...");
                                //bool validated = ValidateOrder(out o);
                                bool validated = false;
                                So = QuickValidateItemsX(So, out validated);

                                if (validated)
                                {
                                    Log($"Order Validated");
                                    bool saveso = false;

                                    try
                                    {
                                        Log("Saving Fishbowl Sales Order...");
                                        saveso = fb.SaveSalesOrder(custName, So, out ErrorMessage);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log("Error Saving Sales Order... " + ex.Message);
                                    }

                                    String soNum = fb.FindSONum(So.Header.PONum);
                                    if (saveso)
                                    {
                                        Log("Sales Order " + soNum + " Imported in Fishbowl.");
                                        sc.UpdateOrderStatus(o.Id.Value, site.Value.StoreConfig.OrderDownload.DownloadedStatus);
                                    }

                                    // Does the Fishbowl Order Exist?
                                    //String soNum = fb.FindSONum(So.Header.PONum);


                                    if (saveso)
                                    {
                                        // Apply Payments
                                        ShopifyTransactionService tranSvc = new ShopifyTransactionService(site.Value.ShopifyConfig.StoreURL, site.Value.ShopifyConfig.APIKey);
                                        IEnumerable<ShopifyTransaction> req = await tranSvc.ListAsync(o.Id.Value);

                                        var pmt = req.ToList();
                                        if (pmt.Sum(k => k.Amount) > 0.00 && o.ProcessingMethod == "direct")
                                        {
                                            String account = site.Value.StoreConfig.Mapping.PaymentMethodsToAccounts[pmt.First().PaymentDetails.CreditCardCompany];
                                            String method = pmt.First().PaymentDetails.CreditCardCompany;

                                            var fbpmt = new Payment()
                                            {
                                                SalesOrderNumber = soNum,
                                                Amount = pmt.Sum(k => k.Amount).ToString(),
                                                PaymentMethod = method,
                                                DepositAccountName = account
                                            };

                                            var result = fb.ApplyPayment(fbpmt);
                                        }
                                        sc.UpdateOrderStatus(o.Id.Value, site.Value.StoreConfig.OrderDownload.DownloadedStatus);

                                    }
                                }
                                else
                                {
                                    Log($"Order did not validate, skipping.");
                                    // Error
                                }
                            }
                        }
                    }
                    timeStampped.Add(site.Key, DateTime.Now);
                }
            }


            foreach (KeyValuePair<String, DateTime> s in timeStampped)
            {
                cfg.ShopifySites[s.Key].StoreConfig.ShippingTracking.LastShippingUpdated = s.Value;
            }
            Config.Save(cfg);

        }


        private SalesOrder QuickValidateItemsX(SalesOrder o, out bool validated)
        {
            foreach (SalesOrderItem soi in o.Detail)
            {
                if (soi.SOItemTypeID == "10")
                {
                    //soi.UOM = fb.FindProductUOM(soi.ProductNumber);

                    KitIdentifer ki = fb.FindProductUOM(soi.ProductNumber);

                    soi.UOM = ki.UOM;
                    if (ki.isKit.Equals("1"))
                    {
                        soi.SOItemTypeID = "80";
                    }

                }

            }
            validated = true;
            return o;
        }


        private static bool ValidateOrder(ShopifyOrder o)
        {

            //ValidateCreateDiscounts(o, i);
            return true;
            //throw new NotImplementedException();

            // Check Items
            // Check Stuff
            // Check Whatever
        }
        private String ValidateCreateCustomer(ShopifyOrder Order, OrderMappingSettings oMapping)
        {
            
            String CheckName = Order.Customer.FirstName + " " + Order.Customer.LastName;



            String name;

            // Does the customer exist with the first order name?

            bool IsCustomerExists = fb.CustomerExists(CheckName);
            if (!IsCustomerExists)
            {
                // Maybe it does not, so check by email address.
                String CustomerNameByEmail = fb.FindCustomerNameByEmail(Order.Customer.Email);
                if (!String.IsNullOrWhiteSpace(CustomerNameByEmail))
                {
                    name = CustomerNameByEmail;
                    CheckName = name;
                }
                // If it does not exist at all, try creating the customer
                else
                {
                    Log("Creating Customer Name: " + CheckName);
                    CreateCustomer(CheckName, Order, oMapping);
                    Log("Customer Created!");
                }
            }
            // Load the Customer so we have the entire object later.
            Log("Loading Customer "+CheckName+" from Fishbowl");
            Customer fbCustomer = fb.LoadCustomer(CheckName);
            
            if (fbCustomer == null)
            {
                throw new Exception(
                    "Cannot continue if a Customer Name is Missing, Or Cannot Be Loaded from Fishbowl. " +
                    CheckName);
            }
            return fbCustomer.Name;
        }
        private void CreateCustomer(string customerName, ShopifyOrder Order, OrderMappingSettings mapping)
        {
            Log("Creating Fishbowl Customer " + customerName);
            var cas = fb.GetCountryState(Order.BillingAddress.Country, Order.BillingAddress.Province);
            var customer = DataMappers.MapCustomer(mapping, Order, customerName, cas);
            fb.CreateCustomer(customer);
        }


        public void Log(String msg)
        {
            if (OnLog != null)
            {
                OnLog(msg);
            }
        }

        private void InitConnections()
        {
            if (fb == null)
            {
                Log("Connecting to Fishbowl");
                fb = new FishbowlController(cfg);
            }

            if (sc == null)
            {
                Log("Connecting to Shopify Store");
                sc = new ShopifyController(cfg);
            }
        }

        private void LogException(Exception ex)
        {
            String msg = ex.Message;
            Log(msg);
            File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "exception.txt", ex.ToString() + Environment.NewLine);
        }


        public void Dispose()
        {
            if (fb != null)
                fb.Dispose();

        }
    }
}
