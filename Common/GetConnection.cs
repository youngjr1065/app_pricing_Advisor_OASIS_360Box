using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Box360_Optimization
{
    public static class GetConnection
    {
        private static string bipsConnectionString = @"Provider=IBMDADB2;Data Source=BIPS; UID=jeng077;PWD=Black5391_;";
        private static string accessConnectionString = @"Provider=Microsoft.ACE.OLEDB.15.0;Data Source=PATH\XXXXXXXXX.accdb; Persist Security Info=False;";

        public static OleDbConnection BIPSConn
        {
            get
            {
                return GetOleDbConnection(bipsConnectionString);
            }
        }

        public static OleDbConnection AccessConnection
        {
            get
            {
                return GetOleDbConnection(accessConnectionString);
            }
        }

        private static OleDbConnection GetOleDbConnection(string connectionString)
        {
            OleDbConnection conn = new OleDbConnection(connectionString);
            try
            {
                conn.Open();
            }

            catch (Exception ex)
            {
                ////Logfile.Log(Logfile.MessageType.Error, sModule, Logfile.FormatException(ex));
                Console.WriteLine(ex.Message);
                throw;
            }
            return conn;
        }
    }
}
