using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace CallWebAPI
{
    public class GetACOList
    {
        public static IConfiguration Config;
        string directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        public static List<Configuration> GetDBConfigurations()
        {
            string connectionString;
            Config = new ConfigurationBuilder().AddJsonFile("ApplicationSettings.json").Build();
            string Environment = Config.GetSection("Environment").Value;
            if (Config.GetSection("Environment").Value == "Staging")
            {
                connectionString = Config.GetSection("StagingConnectionString").Value;
            }
            else
            {
                connectionString = Config.GetSection("LiveConnectionString").Value;
            }
            List<Configuration> list = new List<Configuration>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    string env = Config.GetSection("Environment").Value.ToString();
                    conn.Open();
                    //string query = "SELECT a.AcoKey, a.AcoName, CASE WHEN AcoGroupID = 1 THEN 'Innovista' ELSE 'Premier' END AS Server FROM dbo.ACOIncrementalLoad a WHERE SmartApp = 0 And SAppResult = 'Pass' And CountsReconsiliation = 0";                    
                    string query = "SELECT Name ,Key,Server, CASE WHEN AcoGroupID = 1 THEN 'ABC' ELSE 'EFG' END AS Server FROM dbo.TestDB a WHERE Key in (195,38,146,37)";//(38,37)
                    AddLog("Getting List of to run");
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        AddLog("Following ACO selected for Counts Reconciliation");
                        var reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            Configuration Configuration = new Configuration();
                            Configuration.Key = Convert.ToInt32(reader["Key"]);
                            Configuration.Name = reader["Name"].ToString();
                            Configuration.server = reader["Server"].ToString();
                            Configuration.Environment = Environment;
                            list.Add(Configuration);
                            AddLog(Configuration.Name);
                        }
                    }
                }
                catch (Exception e)
                {
                    AddLog(e.Message);
                    Console.WriteLine("Exception - GetConfigurations Method: " + e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            }
            return list;
        }
        public static void AddLog(string msg)
        {
            Console.WriteLine(msg);
            using (var file = new StreamWriter(@"ACO.txt", true))
            {
                file.WriteLine(msg);
                file.Close();
            }
        }
    }
}
