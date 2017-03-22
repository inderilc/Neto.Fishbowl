using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShopifySharp;
using Integration.Configuration;
using FishbowlSDK;
using System.Dynamic;
using Integration.Models;
using System.IO;

namespace Integration.Controller
{
    public class ShopifyController
    {
        private Config cfg { get; set; }

        private KeyValuePair<String, IntegrationConfiguration> site { get; set; }
        private ShopifyOrderService service {get;set;}

        private ShopifyProductService productService { get; set; }
        private ShopifyProductVariantService variantService { get; set; }
        public event LogMsg OnLog;
        public delegate void LogMsg(String msg);

        public ShopifyController(Config cfg)
        {
            this.cfg = cfg;
        }

        public ShopifyController(KeyValuePair<String, IntegrationConfiguration> site)
        {
            this.site = site;
            //service = new ShopifyOrderService(this.site.Value.ShopifyConfig.StoreURL, this.site.Value.ShopifyConfig.APIKey);
        }


        public List<ShopifyOrder> GetOrders()
        {
         List<ShopifyOrder> ret = new List<ShopifyOrder>();
         if (site.Value.StoreConfig.Actions.SyncOrders)
            {
                service = new ShopifyOrderService(site.Value.ShopifyConfig.StoreURL, site.Value.ShopifyConfig.APIKey);
                foreach (KeyValuePair<String, bool> status in site.Value.StoreConfig.OrderDownload.OrderStatus)
                {
                    if (status.Value)
                    {
                        //var orders = service.ListAsync(new ShopifySharp.Filters.ShopifyOrderFilter() { CreatedAtMin = site.Value.StoreConfig.OrderDownload.LastDownloadedAt, CreatedAtMax = DateTime.Now, Status = status.Key });

                        Boolean keepdownloading = true;
                        int page = 1;
                        while (keepdownloading)
                        {
                            var orders = service.ListAsync(new ShopifySharp.Filters.ShopifyOrderFilter() { Status = status.Key,Page=page,Limit=250, CreatedAtMin=site.Value.StoreConfig.OrderDownload.LastDownloadedAt});
                            orders.Wait();
                            if (orders.Status == System.Threading.Tasks.TaskStatus.RanToCompletion && orders.Result.Count() > 0)
                            {
                                ret.AddRange(orders.Result.Where(k => ((k.FinancialStatus == "pending"||k.FinancialStatus=="paid"))&&(k.FulfillmentStatus!= "fulfilled")));
                                page++;
                            }
                            else
                            {
                                keepdownloading = false;
                            }
                        }
                    }
                }

                /*
                foreach (KeyValuePair<String, bool> status in site.Value.StoreConfig.OrderDownload.OrderFinancialStatus)
                {
                    if (status.Value)
                    {
                        var orders =  service.ListAsync(new ShopifySharp.Filters.ShopifyOrderFilter() { CreatedAtMin = site.Value.StoreConfig.OrderDownload.LastDownloadedAt, CreatedAtMax = DateTime.Now, FinancialStatus = status.Key });
                        ret.AddRange(orders.Result);
                    }
                }
                foreach (KeyValuePair<String, bool> status in site.Value.StoreConfig.OrderDownload.OrderFulfillmentStatus)
                {
                    if (status.Value)
                    {
                        var orders = service.ListAsync(new ShopifySharp.Filters.ShopifyOrderFilter() { CreatedAtMin = site.Value.StoreConfig.OrderDownload.LastDownloadedAt, CreatedAtMax = DateTime.Now, FulfillmentStatus = status.Key });
                        ret.AddRange(orders.Result);
                    }
                }
                */
            }
            return ret.Where(k=>(!k.Tags.Contains(site.Value.StoreConfig.OrderDownload.DownloadedStatus))).ToList(); //selects the orders not having "Exported" tag
        }
        public List<ShopifyOrder> GetTestOrder()
        {
            List<ShopifyOrder> ret = new List<ShopifyOrder>();
            service = new ShopifyOrderService(site.Value.ShopifyConfig.StoreURL, site.Value.ShopifyConfig.APIKey);


            var order = service.GetAsync(site.Value.StoreConfig.OrderDownload.TestOrderNumber);
            order.Wait();
            ret.Add(order.Result);
            return ret;
        }

        public async void UpdateOrderStatus(long orderid, string new_status_id)
        {

            service = new ShopifyOrderService(site.Value.ShopifyConfig.StoreURL, site.Value.ShopifyConfig.APIKey);
            var order = await service.GetAsync(orderid);

            order.Tags = new_status_id;

            ShopifyOrder request = await service.UpdateAsync(order);
            return;
        }

        public bool CreatedProduct(Product p, string imagePath)
        {
            productService = new ShopifyProductService(site.Value.ShopifyConfig.StoreURL, site.Value.ShopifyConfig.APIKey);
            ShopifyProduct shopifyPro = MapFBtoShopify(p, imagePath);
            var request = productService.CreateAsync(shopifyPro); //create the product

            request.Wait();

            return request.Status == System.Threading.Tasks.TaskStatus.RanToCompletion;
        }

        private string GetParent(string path, string parentName)
        {
            var dir = new DirectoryInfo(path);

            if (dir.Parent == null)
            {
                return null;
            }

            if (dir.Parent.Name == parentName)
            {
                return dir.Parent.FullName;
            }

            return this.GetParent(dir.Parent.FullName, parentName);
        }


        public async Task<List<ShopifyProduct>> GetInventory()
        {
            productService = new ShopifyProductService(site.Value.ShopifyConfig.StoreURL, site.Value.ShopifyConfig.APIKey);
            List<ShopifyProduct> ret = new List<ShopifyProduct>();
            IEnumerable<ShopifyProduct> products = await productService.ListAsync();
            ret.AddRange(products);
            
            return ret;

        }

        public async Task<bool> UpdateInventory(ShopifyProductVariant vr)
        {

            variantService = new ShopifyProductVariantService(site.Value.ShopifyConfig.StoreURL, site.Value.ShopifyConfig.APIKey);

            var variant = await variantService.GetAsync(vr.Id.Value);

            variant.InventoryQuantity = vr.InventoryQuantity;
            variant.InventoryQuantityAdjustment = vr.InventoryQuantity - vr.OldInventoryQuantity;

            var request = variantService.UpdateAsync(variant);

            request.Wait();


            return (request.Status==System.Threading.Tasks.TaskStatus.RanToCompletion);
        }

        private ShopifyProduct MapFBtoShopify(Product p, String ImagePath)
        {
            
            ShopifyProduct ret = new ShopifyProduct();

            ShopifyProductVariant productVar = new ShopifyProductVariant();
            List<ShopifyProductVariant> list = new List<ShopifyProductVariant>();
            
            string fileName = ImagePath + "\\" + p.ID + ".jpg";
            
            productVar.Price = Convert.ToDouble(p.Price);
            productVar.SKU = p.Num;
            productVar.Weight = Convert.ToDouble(p.Weight);
            productVar.Barcode = p.UPC;
            productVar.Taxable = p.TaxableFlag;
            productVar.InventoryQuantity = 1;
            productVar.InventoryManagement= "shopify";
           // productVar.FulfillmentService = "continue";
            //productVar.InventoryPolicy = "continue";
            //productVar.WeightUnit = p.WeightUOMID;

            list.Add(productVar);
            var product = new ShopifyProduct()
            {
                Title = p.Description,
                BodyHtml = p.Details,
                CreatedAt = DateTime.Now,
                Variants=list
            };

            if (File.Exists(fileName))
            {
                byte[] imageData = File.ReadAllBytes(fileName);
                List<ShopifyProductImage> imgList = new List<ShopifyProductImage>();
                ShopifyProductImage image = new ShopifyProductImage();
                image.Attachment = Convert.ToBase64String(imageData);
                imgList.Add(image);
                product.Images = imgList;
            }
            
            return product;
        }

        public bool UpdateShipmentStatus(long orderid, String MethodString, string Tracking, List<Shipment> items)
        {
            
            var service = new ShopifyFulfillmentService(site.Value.ShopifyConfig.StoreURL, site.Value.ShopifyConfig.APIKey);
            List<ShopifyLineItem> itms = items.Select(k => new ShopifyLineItem
            {
                Quantity = Convert.ToInt32(k.QTYSHIPPED),
                ProductId = Convert.ToInt32(k.ITEMID)
            }

            ).ToList();

            var fulfillment = new ShopifyFulfillment()
            {
                TrackingCompany = MethodString,
                TrackingNumber = Tracking,
                LineItems=itms
            };

            if (MethodString.Contains("StarTrack"))
            {
                fulfillment.TrackingUrl = "http://www.startrack.com.au/track-trace/?id="+Tracking;
            }
            else if (MethodString.Contains("CourierPost"))
            {
                fulfillment.TrackingUrl = "http://trackandtrace.courierpost.co.nz/search/"+Tracking;
            }

            var response = service.CreateAsync(orderid, fulfillment, true);

            response.Wait();

            return (response.Status==System.Threading.Tasks.TaskStatus.RanToCompletion);

        }
    }
}
