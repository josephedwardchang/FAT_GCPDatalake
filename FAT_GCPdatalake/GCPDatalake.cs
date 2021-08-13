using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.BigQuery.V2;
using log4net;

namespace FAT_GCPdatalake
{
    public class GCPDatalake : IBusinessLogic
    {
        private static readonly ILog m_logger =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public bool SaveToDB(string strData, string strDevId)
        {
            bool retVal = false;

            try
            {
                var client = BigQueryClient.Create(ConfigurationManager.AppSettings["GCPId"]);

                #region working Test GCP read bigQuery public data
                //var table = client.GetTable("bigquery-public-data", "samples", "shakespeare");
                //var sql = $"SELECT corpus AS title, COUNT(word) AS unique_words FROM {table} GROUP BY title ORDER BY unique_words DESC LIMIT 10";
                //var results = client.ExecuteQuery(sql, parameters: null);
                //List<string> lst = new List<string>();
                //foreach (var row in results)
                //{
                //    lst.Add(string.Format($"{row["title"]}: {row["unique_words"]}"));
                //}
                #endregion

                #region working Test GCP write to bigQuery public data
                var datasetId = ConfigurationManager.AppSettings["GCPDatasetId"];
                var tableId = ConfigurationManager.AppSettings["GCPTableId"];
                var createDatasetOptions = new CreateDatasetOptions()
                {
                    //// Specify the geographic location where the dataset should reside.
                    //Location = location
                };
                var dataset = client.GetDataset(datasetId);

                #region Delete dataset and create items again
                //var delDatasetOptions = new DeleteDatasetOptions()
                //{
                //    // Specify the geographic location where the dataset should reside.
                //    DeleteContents = true
                //};
                //client.DeleteDataset(datasetId, delDatasetOptions);
                //dataset = client.CreateDataset(datasetId, options: createDatasetOptions);
                //// Create schema for new table.
                //var schema = new TableSchemaBuilder
                //{
                //    { "name", BigQueryDbType.String },
                //    { "msg", BigQueryDbType.String },
                //    { "tstamp", BigQueryDbType.DateTime }
                //}.Build();
                //// Create the table
                //var table = dataset.CreateTable(tableId: tableId, schema: schema);
                #endregion

                var guid = Guid.NewGuid();
                var table = dataset.GetTable(tableId: tableId);
                var tstamp = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ffffff");
                BigQueryInsertRow[] rows = new BigQueryInsertRow[]
                {
                    //// The insert ID is optional, but can avoid duplicate data
                    //// when retrying inserts.
                    //new BigQueryInsertRow(insertId: "row1") {
                    //    { "name", "Washington" },
                    //    { "msg", "WA" }
                    //},
                    //new BigQueryInsertRow(insertId: "row2") {
                    //    { "name", strDevId },
                    //    { "msg", "CO" }
                    //}
                    new BigQueryInsertRow(insertId: guid.ToString()) {
                        { "name", strDevId },
                        { "msg", strData },
                        { "tstamp", tstamp }
                    }
                };
                
                var result = table.InsertRows(rows);  //client.InsertRows(datasetId, tableId, rows);
                
                #endregion

                retVal = (result.Status == BigQueryInsertStatus.AllRowsInserted);
                string strMsg = string.Format("GCPDatalake savetodb: {0} - {1}", retVal, tstamp);
                m_logger.Debug(strMsg);
            }
            catch(Exception ex)
            {
                string strErr = string.Format("GCPDatalake fail savetodb: {0} - {1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : "");
                m_logger.Debug(strErr);
            }

            return retVal;
        }
    }
}
