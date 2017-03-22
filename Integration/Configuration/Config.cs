using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Configuration
{
    public class Config
    {
        public ServiceConfig Service { get; set; }
        public Dictionary<String, IntegrationConfiguration> ShopifySites { get; set; }
        public FishbowlConfig FB { get; set; }
        private static IntegrationConfiguration test1 { get; set; }
        public static Config Load()
        {
            test1 = new IntegrationConfiguration();

            var cfg = LoadFromDisk(AppDomain.CurrentDomain.BaseDirectory + "config.json");
            Save(cfg);
            return cfg;
        }

        public static void Save(Config cfg)
        {
            SaveToDisk(AppDomain.CurrentDomain.BaseDirectory + "config.json", cfg);
        }

        public static Config LoadFromDisk(String filename)
        {
            if (File.Exists(filename))
            {
                String json = File.ReadAllText(filename);
                var cc = JsonConvert.DeserializeObject<Config>(json);
                return cc;
            }
            else
            {
                return new Config()
                {
                    FB = new FishbowlConfig(),
                    ShopifySites = new Dictionary<String, IntegrationConfiguration>() { { "Test01", test1 } }   
                };
            }
        }
        public static void SaveToDisk(String filename, Config config)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Include;
            serializer.Formatting = Formatting.Indented;

            using (StreamWriter sw = new StreamWriter(filename))
            {
                using (JsonWriter jw = new JsonTextWriter(sw))
                {
                    serializer.Serialize(sw, config);
                }
            }
        }

    }

    public class FishbowlConfig
    {
        public Int32 FBIAKey { get; set; }
        public string FBIAName { get; set; }
        public string FBIADesc { get; set; }
        public string ServerAddress { get; set; }
        public Int32 ServerPort { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool Persistent { get; set; }

        public string DBPath { get; set; }
        public Int32 DBPort { get; set; }

        public string DBUser { get; set; }
        public string DBPass { get; set; }
        public string DBImages { get; set; }
    }
    public class IntegrationConfiguration
    {
        public ShopifyConfiguration ShopifyConfig { get; set; }
        public StoreConfiguration StoreConfig { get; set; }

        public IntegrationConfiguration()
        {
            this.ShopifyConfig = new ShopifyConfiguration();
            this.StoreConfig = new StoreConfiguration();
        }

    }
    public class ShopifyConfiguration
    {
        public String StoreURL { get; set; }
        public String APIKey { get; set; }
    }
    
    public class StoreConfiguration
    {
        public ActionsConfig Actions { get; set; }
        public OrderMappingSettings Mapping { get; set; } 
        public OrderdownloadSettings OrderDownload { get; set; }
        public InventoryUpdateSettings InventoryUpdate { get; set; }
        public ShippingTrackingSettings  ShippingTracking { get; set; }
        public ProductCreateSettings ProductCreate { get; set; }

        public StoreConfiguration()
        {
            this.Actions = new ActionsConfig();
            this.Mapping = new OrderMappingSettings();
            this.OrderDownload = new OrderdownloadSettings();
            this.InventoryUpdate = new InventoryUpdateSettings();
            this.ShippingTracking = new ShippingTrackingSettings();
            this.ProductCreate = new ProductCreateSettings();
        }
    }

    public class OrderMappingSettings
    {
        public string DefaultCarrier { get; set; }
        public string DefaultCustomer { get; set; }
        public string Salesman { get; set; }
        public string LocationGroup { get; set; }
        public string ShipTerms { get; set; }
        public string PaymentTerms { get; set; }
        public string FOB { get; set; }
        public string StoreCode { get; set; }
        public string TaxName { get; set; }
        public double TaxRate { get; set; }
        public string FBImportStatus { get; set; }
        public Dictionary<String, String> CarrierSearchNames { get; set; }
        public Dictionary<String, String> PaymentMethodsToAccounts { get; set; }

        public OrderMappingSettings()
        {
            this.CarrierSearchNames = new Dictionary<string, string>();
            this.PaymentMethodsToAccounts = new Dictionary<string, string>();
        }

    }

    public class ActionsConfig
    {
        public bool SyncOrders { get; set; }
        //public bool SyncInventory { get; set; }
        public bool SyncShipments { get; set; }
        //public bool CreateCheckedProducts { get; set; }
    }
    public class OrderdownloadSettings
    {
        public DateTime LastDownloadedAt { get; set; }
        public String DownloadedStatus { get; set; }
        public long TestOrderNumber { get; set; }
        public Dictionary<String, bool> OrderStatus { get; set; }
        public Dictionary<String, bool> OrderFinancialStatus { get; set; }
        public Dictionary<String, bool> OrderFulfillmentStatus { get; set; }

        public OrderdownloadSettings()
        {
            this.OrderStatus = new Dictionary<String, bool>();
            this.OrderFinancialStatus = new Dictionary<String, bool>();
            this.OrderFulfillmentStatus=new Dictionary<String, bool>();
        }

    }
    public class ServiceConfig
    {
        public String InstanceName { get; set; }
        public String Description { get; set; }
        public int IntervalMinutes { get; set; }
    }
    public class InventoryUpdateSettings
    {

    }
    public class ShippingTrackingSettings
    {
        public DateTime LastShippingUpdated { get; set; }
    }
    public class ProductCreateSettings
    {

    }

}
