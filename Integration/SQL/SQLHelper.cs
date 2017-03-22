using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.SQL
{
    public static class SQLHelper
    {
        public static string GetSQL(string file)
        {
            String path = AppDomain.CurrentDomain.BaseDirectory + "SQL";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            String sqlfile = path + Path.DirectorySeparatorChar + file + ".sql";
            if (!File.Exists(sqlfile))
                File.WriteAllText(sqlfile, "");

            return File.ReadAllText(sqlfile);
        }
    }
}
