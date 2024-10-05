using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Configuration;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;


namespace OrderManagementSystem.util
{
    public class DBPropertyUtil
    {
        public static string GetPropertyString(string fileName)
        {
            var jsonData = File.ReadAllText(fileName);
            var jsonObject = JObject.Parse(jsonData);
            string connectionString = jsonObject["ConnectionString"].ToString();
            return connectionString;
        }
    }
}

