using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using FirebirdSql.Data.FirebirdClient;
using MySql.Data.MySqlClient;
using FishbowlSDK;
using Dapper;

using Integration.Configuration;
using Integration.Extensions;
using Integration.Models;
using Integration.SQL;
using System.IO;
using ShopifySharp;

namespace Integration.Controller
{
    public class FishbowlController : IDisposable
    {
        public event LogMsg OnLog;
        public delegate void LogMsg(String msg);

        private Config cfg;
        private IDbConnection db { get; set; }
        public FishbowlSDK.Fishbowl api { get; set; }
        public FishbowlController(Config cfg)
        {
            this.cfg = cfg;
            db = InitDB();
            api = InitAPI();
        }
        private IDbConnection InitDB()
        {
            if (cfg.FB.DBPath.ToLower().EndsWith(".fdb") || cfg.FB.DBPort == 3050)
            {
                return FBConnection();
            }
            else
            {
                return MYSQLConnection();

            }
            
        }

        
        public List<Product> GetCheckedProducts()
        {
            List<FishbowlSDK.Product> ret = new List<FishbowlSDK.Product>();

            var uploadList = GetProductNums();

            foreach (var i in uploadList)
            {
                ProductGetRqType rq = new ProductGetRqType();
                rq.Number = i;
                ProductGetRsType rs = (ProductGetRsType)api.sendAnyRequest(rq);
                if (rs.statusCode == "1000")
                {
                    ret.Add(rs.Product);
                }
                //Log("");
            }

            return ret;

        }
        public Dictionary<String, Double> GetInventory()
        {
            var i = db.Query<FBInventory>(FB.FB_GetInventory).ToList();
            return i.ToDictionary<FBInventory, String, Double>(l => l.NUM, l => l.QTY);
        }
        private List<String> GetProductNums()
        {
            var nums = db.Query<String>(FB.GetCheckedProductNums);
            return nums.ToList();
        }

        public List<Shipment> GetShipments(DateTime d, String ShoppingCartCode)
        {
            var shipments = db.Query<Shipment>(FB.FB_GetShipmentsToUpdate, new { dte = d, scc = ShoppingCartCode }).ToList();
            return shipments;
        }

        public void MarkCreated(List<string> createdOK)
        {
            foreach (string iNum in createdOK)
            {
                try

                {
                    String data = "Product,Upload to Shopify," + iNum + ",0";

                    var importrs = api.ImportCSV("ImportCustomFieldData", data);
                    if (importrs.statusCode.Equals("1000"))
                    {

                    }
                }
                catch (Exception ex)
                {

                }
                
            }

        }


        private FbConnection FBConnection()
        {
            String CSB = InitCSB();
            FbConnection db = new FbConnection(CSB);
            db.Open();
            return db;
        }
        private IDbConnection MYSQLConnection()
        {
            MySqlConnectionStringBuilder csb = new MySqlConnectionStringBuilder();
            csb.Database = cfg.FB.DBPath;
            csb.Server = cfg.FB.ServerAddress;
            csb.Port = Convert.ToUInt32(cfg.FB.DBPort);
            csb.UserID = cfg.FB.DBUser;
            csb.Password = cfg.FB.DBPass;
            return new MySqlConnection(csb.ToString());



            //string conString = "Server=" + cfg.FB.ServerAddress + ";UserId=" + cfg.FB.DBUser + ";Password=" + cfg.FB.DBPass + ";Database=" + cfg.FB.DBPath;
            //return new MySqlConnection(conString);
        }

        private string InitCSB()
        {
            FbConnectionStringBuilder csb = new FbConnectionStringBuilder();
            csb.DataSource = cfg.FB.ServerAddress;
            csb.Database = cfg.FB.DBPath;
            csb.UserID = cfg.FB.DBUser;
            csb.Password = cfg.FB.DBPass;
            csb.Port = cfg.FB.DBPort;
            csb.ServerType = FbServerType.Default;
            return csb.ToString();
        }

        public bool CheckSOExist(string orderID, string shoppingcart)
        {
            String so =
                db.Query<String>(FB.CheckSOExist, new { scon = orderID,scc=shoppingcart })
                    .SingleOrDefault();

            return !(string.IsNullOrEmpty(so));
        }

        private FishbowlSDK.Fishbowl InitAPI()
        {
            var newfb = new FishbowlSDK.Fishbowl(cfg.FB.ServerAddress, cfg.FB.ServerPort, cfg.FB.FBIAKey, cfg.FB.FBIAName, cfg.FB.FBIADesc, cfg.FB.Persistent, cfg.FB.Username, cfg.FB.Password);
            return newfb;
        }


        public void CreateCustomer(Customer customer)
        {
            api.SaveCustomer(customer, true);
        }

        public Customer LoadCustomer(string customerName)
        {
            return api.GetCustomer(customerName);
        }
        public string FindCustomerNameByEmail(object email)
        {
            return db.Query<String>("select customer.name from CONTACT join customer on customer.accountid = contact.ACCOUNTID where contact.DATUS = @eml and contact.typeid = 60", new { eml = email }).FirstOrDefault();
        }
        public bool CustomerExists(object customerName)
        {
            var text = db.Query<String>("select name from customer where name = @c", new { c = customerName }).SingleOrDefault();
            return customerName.Equals(text);
        }
        public CountryAndState GetCountryState(string Country, string State)
        {
            CountryAndState cas = new CountryAndState();

            Countryconst ct;
            Stateconst st;

            String testCountry =  Country.ToUpper();
            String testAbb = Country.Truncate(10).ToUpper();
            /// Get the country
            ct = db.Query<Countryconst>("select * from countryconst where UPPER(abbreviation) like @abb or UPPER(name) like @n", new { n = testCountry, abb = testAbb }).FirstOrDefault();

            // If we have no country, lookup just by state
            if (ct == null || ct.ID == null)
            {
                st = db.Query<Stateconst>("select * from stateconst where UPPER(name) like @st or UPPER(code) like UPPER(@abb)  ", new { st = ("%" + State + "%"), abb = ("%" + State.Truncate(21) + "%") }).FirstOrDefault();
            }
            else // If we have a country, include that in the lookup
            {
                st = db.Query<Stateconst>("select * from stateconst where UPPER(name) like UPPER(@st) or UPPER(code) like UPPER(@abb) and countryconstid = @cid ", new { st = ("%" + State + "%"), abb = ("%" + State.Truncate(21) + "%"), cid = ct.ID }).FirstOrDefault();
            }

            // If we have a state and no country
            if (st != null && ct == null)
            {
                // Lookup the country
                ct = db.Query<Countryconst>("select * from countryconst where id = @cid limit 1", new { cid = st.COUNTRYCONSTID }).FirstOrDefault();
            }

            if (st == null || ct == null)
            {
                throw new Exception("Cant find Country and Or State. [" + Country + "] [" + State + "] ");
            }

            cas.State = st;
            cas.Country = ct;

            return cas;
        }
        public bool SaveSalesOrder(String cName, Models.SalesOrder o, out string ErrorMessage)
        {
            ErrorMessage = "";
            o.Header.CustomerName = cName;
            var CSV = o.ToCSV();

            var importrs = api.ImportCSV("ImportSalesOrder", CSV);
            if (importrs.statusCode.Equals("1000"))
            {
                //File.WriteAllText("mage_order_" + o.Header.PONum + ".csv", CSV);
                return true;
            }
            else
            {
                File.WriteAllText("mage_order_" + o.Header.PONum + ".csv", CSV);
                ErrorMessage = importrs.statusMessage;
                File.WriteAllText("mage_error_" + o.Header.PONum + ".txt", ErrorMessage);
                return false;
            }

        }
        public String FindSONum(string poNum)
        {
            String str =
                db.Query<String>("select num from so where customerpo = @n", new { n = poNum }).FirstOrDefault();
            return str;
        }
        public CarrierService FindCarrierService(string data, ShopifyShippingLine sl)
        {
            string carrier = null;
            string service = null;

            if (sl != null)
            {
                carrier = sl.Title;
                //String serv = db.Query<String>("select carrierservice.name from carrierservice join carrier on carrier.id=carrierservice.id where carrier.name = @c and carrierservice.code = @ccode", new { c = carrier,ccode=sl.Code }).FirstOrDefault();
                String serv = null;
                if (!String.IsNullOrEmpty(serv))
                {
                    service = serv;
                }
               return new CarrierService() { Carrier = carrier, Service = service };          
            }
            return new CarrierService() { Carrier = data };
            
        }

        public KitIdentifer FindProductUOM(string productNumber)
        {
            //String uom = db.Query<String>("select uom.code from product join uom on uom.id = product.uomid where product.num = @n", new { n = productNumber }).SingleOrDefault();

            KitIdentifer ki = db.Query<KitIdentifer>("select product.defaultsoitemtype as Type, uom.code as UOM , product.kitflag as isKit from product join uom on uom.id = product.uomid where product.num = @n", new { n = productNumber }).SingleOrDefault();

            if (String.IsNullOrWhiteSpace(ki.UOM))
            {
                ki.UOM = "ea";
            }

            return ki;
        }

        public bool ApplyPayment(Payment fbpmt)
        {
            var rq = new MakePaymentRqType();
            rq.Payment = fbpmt;
            var rs = api.sendAnyRequest(rq) as MakePaymentRsType;
            if (rs.statusCode.Equals("1000"))
            {
                return true;
            }
            else
            {
                OnLog("Payment Error: " + rs.statusMessage);
                return false;
            }
        }

        public void Dispose()
        {
            if (api != null)
                api.Dispose();

            if (db != null)
                db.Dispose();
        }
    }
}