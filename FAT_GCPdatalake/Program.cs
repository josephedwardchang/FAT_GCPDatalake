using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FAT_GCPdatalake
{
    class Program
    {
        protected static readonly ILog logger =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static void Main(string[] args)
        {
            XmlConfigurator.Configure();

            var bRet = false;
            //if (args.Count() > 1)
            {
                //var dbsvr = args[0].ToString().ToLower();
                //var file = args[1].ToString();

                //if (dbsvr.StartsWith("postgres"))
                //{
                //    Dictionary<string, string> csvHeader = new Dictionary<string, string>();

                //    try
                //    {
                //        // 1. read csv headers 1st and determine columns, limit 256MB files, otherwise refactor to use chunking
                //        using (var filestr = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                //        using (var strRead = new StreamReader(filestr))
                //        {
                //            var header = strRead.ReadUntil("\r\n", '"');
                //            string[] headers = header.SplitUntil("\t", '"');
                //            int count = 0;
                //            // add the default PK col
                //            csvHeader.Add("ID", "ID");
                //            foreach (string str in headers)
                //            {
                //                count++;
                //                csvHeader.Add("A" + count.ToString(), str.Replace(" ", "").Replace("(", "").Replace(")", ""));
                //            }
                //        }

                //        var psql = new PostgresqlDB(csvHeader, file);
                //        psql.InsertDataToDB(csvHeader, file);

                //        bRet = true;
                //    }
                //    catch (Exception ex)
                //    {
                //        string strErr = string.Format("File read failed: {0} - {1}", ex.Message, ex.InnerException == null ? "" : ex.InnerException.Message);
                //        logger.Debug(strErr);
                //        Console.WriteLine(strErr);
                //    }
                //}

                MqttClient.ConnectAsync().Wait();
                char c = '\0';
                while ((c = Console.ReadKey().KeyChar) != '\u001b')
                {

                }
            }



            if (!bRet)
            {
                Console.WriteLine("\n\rFAT_GCPDatalake");
            }
        }
    }
}
