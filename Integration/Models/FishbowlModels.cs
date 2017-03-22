using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Models
{
    public class CarrierService
    {
        public String Carrier { get; set; }
        public String Service { get; set; }
    }
    public class KitIdentifer
    {
        public String Type { get; set; }
        public String UOM { get; set; }
        public String isKit { get; set; }
    }
    public class Stateconst
    {
        public System.Int32 ID { get; set; }
        public System.Int32 COUNTRYCONSTID { get; set; }
        public System.String NAME { get; set; }
        public System.String CODE { get; set; }
    }
    public class Countryconst
    {
        public System.Int32 ID { get; set; }
        public System.String ABBREVIATION { get; set; }
        public System.String NAME { get; set; }
        public System.Int16 UPS { get; set; }
    }
    public class CountryAndState
    {
        public Countryconst Country { get; set; }
        public Stateconst State { get; set; }
    }
    public class FBInventory
    {
        public String NUM { get; set; }
        public Double QTY { get; set; }

        public Double PRICE { get; set; }

        public Double WEIGHT { get; set; }
    }
    public class Shipment
    {
        public String SNUM { get; set; }
        public String BCAID { get; set; }
        public String CPO { get; set; }
        public String ORDERNUM { get; set; }


        public String CARRIERNAME { get; set; }
        public String SHIPSERVICE { get; set; }

        public String TRACKINGNUM { get; set; }
        public String ITEMID { get; set; }
        public Double QTYSHIPPED { get; set; }
    }

    public class SalesOrder
    {
        public SalesOrderHeader Header { get; set; }
        public List<SalesOrderItem> Detail { get; set; }

        public SalesOrder()
        {
            Header = new SalesOrderHeader();
            Detail = new List<SalesOrderItem>();
        }

        public String ToCSV()
        {
            var cfItemKeys = Detail.SelectMany(k => k.CustomFields.Keys).Distinct().ToList();
            return CsvHeader() + CsvItemHeader(cfItemKeys) + CsvOrder() + CsvItems(cfItemKeys);
        }

        private string CsvHeader()
        {
            StringWriter sw = new StringWriter();
            var csv = new CsvWriter(sw);
            csv.Configuration.HasHeaderRecord = true;
            csv.Configuration.QuoteAllFields = true;
            csv.WriteHeader(typeof(SalesOrderHeader));
            String ret = sw.ToString();
            ret = ret.TrimEnd(Environment.NewLine.ToCharArray()) + AppendCustomFieldHeader("CF-", Header.CustomFields.Keys.ToList());
            return ret + Environment.NewLine;
        }



        private string CsvItemHeader(List<String> cfKeys)
        {
            StringWriter sw = new StringWriter();
            var csv = new CsvWriter(sw);
            csv.Configuration.HasHeaderRecord = true;
            csv.Configuration.QuoteAllFields = true;
            csv.WriteHeader(typeof(SalesOrderItem));
            String ret = sw.ToString();
            ret = ret.TrimEnd(Environment.NewLine.ToCharArray()) + AppendCustomFieldHeader("CFI-", cfKeys);
            return ret + Environment.NewLine;
        }

        private string CsvOrder()
        {
            StringWriter sw = new StringWriter();
            var csv = new CsvWriter(sw);
            csv.Configuration.HasHeaderRecord = true;
            csv.Configuration.QuoteAllFields = true;
            csv.WriteRecord(this.Header);
            String ret = sw.ToString();

            ret = ret.TrimEnd(Environment.NewLine.ToCharArray()) + AppendCustomFieldValues(Header.CustomFields.Keys.ToList(), Header.CustomFields);

            return ret + Environment.NewLine;
        }

        private string CsvItems(List<String> cfKeys)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var i in Detail)
            {
                StringWriter sw = new StringWriter();
                var csv = new CsvWriter(sw);
                csv.Configuration.HasHeaderRecord = true;
                csv.Configuration.QuoteAllFields = true;
                csv.WriteRecord(i);
                String ret = sw.ToString();
                ret = ret.TrimEnd(Environment.NewLine.ToCharArray()) + AppendCustomFieldValues(cfKeys, i.CustomFields);
                sb.AppendLine(ret);
            }
            return sb.ToString();
        }

        private String AppendCustomFieldHeader(String Prefix, List<String> keys)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var i in keys)
            {
                sb.Append("," + '"' + Prefix + i + '"');
            }


            return sb.ToString();
        }
        private String AppendCustomFieldValues(List<string> keys, Dictionary<string, object> customFields)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var i in keys)
            {
                bool hasValue = customFields.ContainsKey(i);
                if (hasValue)
                {
                    var value = customFields[i];
                    sb.Append("," + '"' + value.ToString() + '"'); // TODO Formatting for different types
                }
                else
                {
                    sb.Append(",");
                }

            }
            return sb.ToString();
        }
    }

    public class SalesOrderHeader
    {
        public SalesOrderHeader()
        {
            this.Flag = "SO";
            this.CustomFields = new Dictionary<string, object>();
        }
        public String Flag { get; set; }
        public String SONum { get; set; }
        public String Status { get; set; }
        public String CustomerName { get; set; }
        public String CustomerContact { get; set; }
        public String BillToName { get; set; }
        public String BillToAddress { get; set; }
        public String BillToCity { get; set; }
        public String BillToState { get; set; }
        public String BillToZip { get; set; }
        public String BillToCountry { get; set; }
        public String ShipToName { get; set; }
        public String ShipToAddress { get; set; }
        public String ShipToCity { get; set; }
        public String ShipToState { get; set; }
        public String ShipToZip { get; set; }
        public String ShipToCountry { get; set; }
        public String CarrierName { get; set; }
        public String TaxRateName { get; set; }
        public String PriorityId { get; set; }
        public String PONum { get; set; }
        public String VendorPONum { get; set; }
        public String Date { get; set; }
        public String Salesman { get; set; }
        public String ShippingTerms { get; set; }
        public String PaymentTerms { get; set; }
        public String FOB { get; set; }
        public String Note { get; set; }
        public String QuickBooksClassName { get; set; }
        public String LocationGroupName { get; set; }
        public String FulfillmentDate { get; set; }
        public String URL { get; set; }
        public String CarrierService { get; set; }
        public String DateExpired { get; set; }
        public String Phone { get; set; }
        public String Email { get; set; }

        public Dictionary<String, Object> CustomFields { get; set; }

    }

    public class SalesOrderItem
    {
        public SalesOrderItem()
        {
            this.Flag = "Item";
            this.CustomFields = new Dictionary<string, object>();
        }

        public string Flag { get; set; }
        public string SOItemTypeID { get; set; }
        public string ProductNumber { get; set; }
        public string ProductDescription { get; set; }
        public string ProductQuantity { get; set; }
        public string UOM { get; set; }
        public string ProductPrice { get; set; }
        public string Taxable { get; set; }
        public string TaxCode { get; set; }
        public string Note { get; set; }
        public string QuickBooksClassName { get; set; }
        public string FulfillmentDate { get; set; }
        public string ShowItem { get; set; }
        public string KitItem { get; set; }
        public string RevisionLevel { get; set; }

        public Dictionary<String, Object> CustomFields { get; set; }
    }



}
