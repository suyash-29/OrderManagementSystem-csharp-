using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data.SqlClient;
using OrderManagementSystem.util;
using OrderManagementSystem.exception;


namespace OrderManagementSystem.util
{
    public  class DBUtil
    {
        public static SqlConnection GetDBConn()
        {
            try
            {
                string connectionString = DBPropertyUtil.GetPropertyString("appsettings.json");
                SqlConnection conn = new SqlConnection(connectionString);
                conn.Open();
                return conn;
            }
            catch (SqlException ex)
            {
                throw new DatabaseException("Failed to connect to the database: " + ex.Message, ex);
            }
        }
    }
}
