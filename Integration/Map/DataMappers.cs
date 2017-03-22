using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Integration.Models;
using ShopifySharp;
using Integration.Configuration;
using FishbowlSDK;
using Integration.Extensions;
using SalesOrder = Integration.Models.SalesOrder;
using SalesOrderItem = Integration.Models.SalesOrderItem;
using Integration.Controller;
using System.Text.RegularExpressions;

namespace Integration.Map
{
    public static class DataMappers
    {
        public static FishbowlSDK.Customer MapCustomer(OrderMappingSettings mapping, ShopifyOrder o, String customerName, CountryAndState csa)
        {
            FishbowlSDK.Customer customer = new FishbowlSDK.Customer();
            customer.CustomerID = "-1";
            customer.Status = "Normal";
            //customer.DefPaymentTerms = cfg.Store.OrderSettings.PaymentTerms;
            customer.TaxRate = null;
            customer.Name = customerName;
           
            customer.LastChangedUser = mapping.Salesman;
            customer.CreditLimit = "0";
            customer.TaxExempt = false;
            customer.TaxExemptNumber = null;
            customer.TaxExemptSpecified = true;
            customer.ActiveFlag = true;
            customer.ActiveFlagSpecified = true;
            customer.AccountingID = null;
            customer.DefaultSalesman = mapping.Salesman;
            customer.DefaultCarrier = mapping.DefaultCarrier;

            customer.JobDepth = "1";
            FishbowlSDK.Address address = new FishbowlSDK.Address();
            address.Street = o.BillingAddress.Address1 + " " + o.BillingAddress.Address2;

            address.Name = o.BillingAddress.FirstName + " " + o.BillingAddress.LastName;

            address.Attn = address.Name;

            address.Residential = true;
            address.ResidentialSpecified = true;
            address.City = o.BillingAddress.City;
            address.State.Code = csa.State.CODE;
            address.State.Name = csa.State.NAME;

            address.Country.Name = csa.Country.NAME;
            address.Country.Code = csa.Country.ABBREVIATION;
            address.Country.ID = csa.Country.ID.ToString();

            address.Zip = o.BillingAddress.Zip;
            address.Type = "Main Office";
            address.TempAccount = null;
            address.Default = true;
            address.DefaultSpecified = true;

            address.AddressInformationList = new List<AddressInformation>()
            {
                new AddressInformation()
                {
                    Name = "Email",
                    Type = "Email",
                    Default = true,
                    DefaultSpecified = true,
                    Data = o.Customer.Email
                }
            };
            customer.Addresses.Add(address);
            return customer;
        }
       
        public static SalesOrder MapSalesOrderX(FishbowlController fb, string custName, ShopifyOrder o, OrderMappingSettings settings, String paymentTerms)
        {
            SalesOrder so = new SalesOrder();

            so.Header = MapHeader(fb, o, settings, paymentTerms);

            so.Detail = MapDetail(o, settings);

            return so;
        }
        private static SalesOrderHeader MapHeader(FishbowlController fb, ShopifyOrder o, OrderMappingSettings settings, String paymentTerms)
        {
            SalesOrderHeader ret = new SalesOrderHeader();

            ret.CustomFields = MapCustomFields(o, settings);
            var ship = o.ShippingAddress;

            var billto = o.BillingAddress;
            var shipto = ship;

            ret.SONum = "S-" + o.OrderNumber;
            ret.Status = settings.FBImportStatus;
            ret.CustomerName = o.Customer.FirstName+" "+o.Customer.LastName;
            ret.CustomerContact = shipto.FirstName + " " + shipto.LastName;
            ret.PaymentTerms = paymentTerms;

            ret.BillToName = billto.Name;
            ret.BillToAddress = String.Join("\n", billto.Address1);
            ret.BillToCity = billto.City;
            ret.BillToState = billto.ProvinceCode;
            ret.BillToZip = billto.Zip;
            ret.BillToCountry = billto.CountryCode;

            ret.ShipToName = shipto.Name;
            ret.ShipToAddress = String.Join("\n", shipto?.Address1);
            ret.ShipToCity = shipto.City;
            ret.ShipToState = shipto.ProvinceCode;
            ret.ShipToZip = shipto.Zip;
            ret.ShipToCountry = shipto.CountryCode;

            if (o.ShippingLines.Count() > 0)
            {
                var cs = fb.FindCarrierService(o.ShippingLines?.First().Code ?? settings.DefaultCarrier,o.ShippingLines.First());
                /*
                var cs = i.FindMapCarrierService(o.SOAPOrder.shippingDescription, "");
                */
                ret.CarrierName = cs.Carrier;
                ret.CarrierService = cs.Service;
            }
            else
            {
                ret.CarrierName = "Will Call";
            }

            
            ret.TaxRateName = (o.TaxLines.Count()>0)?o.TaxLines.First().Title:settings.TaxName;
            ret.PONum = o.Id.ToString();
            ret.VendorPONum = "";
            ret.Date = DateTime.Now.ToShortDateString();
            ret.Salesman = settings.Salesman;
            ret.ShippingTerms = settings.ShipTerms;
            ret.PaymentTerms = settings.PaymentTerms;
            ret.FOB = settings.FOB;

            if (!String.IsNullOrEmpty(o.Note))
            {
                ret.Note = Regex.Replace(o.Note, @"\t|\n|\r", "");
            }
            ret.LocationGroupName = settings.LocationGroup;
            ret.FulfillmentDate = null;
            ret.URL = o.OrderStatusUrl;
            ret.Phone = billto?.Phone ?? ship.Phone;
            ret.Email = o.Customer?.Email ?? o.Email ?? "";
            ret.DateExpired = null;
            /*
            ret.PriorityId = i.cfg.Integr
            ret.QuickBooksClassName = i.cfg.Integration.OrderQBClass;
            */
            return ret;
        }

        private static Dictionary<string, object> MapCustomFields(ShopifyOrder o, OrderMappingSettings settings)
        {
            Dictionary<String, object> ret = new Dictionary<string, object>();

            ret.Add("Shopping Cart Code", settings.StoreCode);
            ret.Add("Shopping Cart Order Number", o.OrderNumber);

            return ret;
        }
        
        private static List<SalesOrderItem> MapDetail(ShopifyOrder o, OrderMappingSettings settings)
        {
            List<SalesOrderItem> ret = new List<SalesOrderItem>();

            // Product Items
            foreach (ShopifyLineItem i in o.LineItems)
            {
                ret.Add(AddItem(i, o.TaxLines.ToList()));
            }


            //ret.AddRange(o.LineItems.ToList().Select(AddItem));
            //if (o.TaxLines.Count() > 0)
            //{
            //ret.AddRange(o.TaxLines.ToList().Select(AddTax));
            //}

            // Subtotal            
            
            // Discount Line
            if (o.TotalDiscounts != 0)
            {
                //ret.Add(AddSubtotal());
                foreach (ShopifyDiscountCode dc in o.DiscountCodes)
                {
                    ret.Add(AddDiscountLine(dc));
                }
                ret.Add(AddAllDiscountLineItems(o.TotalDiscounts.ToString("N2"),o.TaxLines.First().Title));
            }

            // Shipping Cost
            if (o.ShippingLines.Sum(k => k.Price) > 0)
            {
                ret.Add(AddShippingLine(o));
            }
            //ret.Add(AddTax(o.TaxLines.First()));
            foreach (var x in ret)
            {
                x.FulfillmentDate = DateTime.Now.ToShortDateString();
            }

            return ret;
        }
        private static SalesOrderItem AddTax(ShopifyTaxLine i)
        {

            /*ret.SOItemTypeID="70";
        ret.ProductQuantity="1";
        ret.ProductPrice=tax;
        ret.Taxable="FALSE";
        ret.UOM="ea";
        ret.ProductNumber="SalesTax";
        ret.ProductDescription="WC SalesTax"
        */

            return new SalesOrderItem()
            {
                SOItemTypeID = "70",
                ProductQuantity = "1",
                ProductPrice = i.Price.ToString(),
                UOM = "ea",
                ProductNumber= i.Title,
                TaxCode=i.Title
            };
        }
        private static SalesOrderItem AddItem(ShopifyLineItem i, List<ShopifyTaxLine> tax)
        {
            String title = (tax.Count() > 0) ? tax.First().Title : "NON";

            return new SalesOrderItem()
            {
                SOItemTypeID = "10",
                ProductNumber = i.SKU,
                UOM = "ea",
                ProductQuantity = i.Quantity.ToString(),
                ProductPrice = i.Price.ToString(),
                RevisionLevel = i.Id.ToString(),
                TaxCode=title,
                Taxable="TRUE"
            };
        }


        private static SalesOrderItem AddSubtotal()
        {
            return new SalesOrderItem()
            {
                SOItemTypeID = "40",
                ProductNumber = "Subtotal",
                Taxable = "FALSE",
                ShowItem = "TRUE",
                KitItem = "FALSE",
            };
        }
        private static SalesOrderItem AddAllDiscountLineItems(String discount, String code)
        {
            return new SalesOrderItem()
            {
                SOItemTypeID = "21",
                ProductNumber = "Store Credit",
                ProductDescription = "Misc. Credit",
                ProductQuantity = "1",
                ProductPrice = "-" + (discount),
                Taxable = "TRUE",
                TaxCode=code,
                UOM = "ea"
            };

        }
        private static SalesOrderItem AddDiscountLine(ShopifyDiscountCode dc)
        {
            // If Discount Percentage
            //var pctDecimal = (((o.TotalDiscounts * -1) / o.SubtotalPrice) * 100);
            //String pct = pctDecimal.ToString("F1");
            //String discName = $"Shopify_{pct}_Percent";
            String discName = dc.Code;

            return new SalesOrderItem()
            {
                ProductNumber = discName,
                ProductQuantity = "1",
                SOItemTypeID = "31",
                ProductPrice = dc.Amount,
                Taxable="FALSE",
                UOM="ea"
                //Note = (pctDecimal / 100).ToString("p").Replace(" ", "")
            };

        }

        private static SalesOrderItem AddShippingLine(ShopifyOrder o)
        {
            return new SalesOrderItem()
            {
                ProductNumber = "Shipping",
                ProductQuantity = "1",
                UOM = "ea",
                ProductDescription = "Shipping",
                SOItemTypeID = "60",
                Taxable="TRUE",
                TaxCode=o.ShippingLines.First().TaxLines.First().Title,
                ProductPrice = o.ShippingLines.Sum(k => k.Price).ToString()
            };
        }
    }
}
