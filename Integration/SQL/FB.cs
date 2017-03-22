using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.SQL
{
    public static class FB
    {
        public static String CheckSOExist => SQLHelper.GetSQL("FB_CheckSOExist");

        public static String GetCheckedProductNums => SQLHelper.GetSQL("FB_GetCheckedProductNums");

        public static String FB_GetInventory => SQLHelper.GetSQL("FB_GetInventory");
        public static String FB_GetShipmentsToUpdate => SQLHelper.GetSQL("FB_GetShipmentsToUpdate");
    }
}
