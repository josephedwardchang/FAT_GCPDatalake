using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using MQTTnet;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using System.Configuration;

namespace FAT_GCPdatalake
{
    public class MqttClient
    {
        private static readonly ILog m_logger =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static IManagedMqttClient client = null;
        protected static IBusinessLogic m_businessLogic = null;

        // Call this 1st
        // connect to broker
        //
        public static async Task ConnectAsync()
        {
            string clientId = Guid.NewGuid().ToString();
            string mqttURI = ConfigurationManager.AppSettings["mqttHost"]; // "localhost"; // "{ REPLACE THIS WITH YOUR QT SERVER URI HERE }";        
            string mqttUser = null; // "{ REPLACE THIS WITH YOUR MOTT USER HERE";
            string mqttPassword = null; // "{ REPLACE THIS WITH YOUR NQTT PASSWORD HERE }";
            int mqttPort = Convert.ToInt32(ConfigurationManager.AppSettings["mqttPort"]); // 1883; // "{ REPLACE THIS WITH YOUR MOTT PORT HERE }";
            bool mqttSecure = Convert.ToBoolean(ConfigurationManager.AppSettings["mqttUseSecure"]); // false; // "{ IF YOU ARE USING SSL Port THEN SET true OTHERUISE SET false }";

            // initialize the business logic
            m_businessLogic = new GCPDatalake(); // Helper.DependencyFactory.Resolve<IBusinessLogic>();

            var messageBuilder = new MqttClientOptionsBuilder()
                .WithClientId(clientId)
                .WithCredentials(mqttUser, mqttPassword)
                .WithTcpServer(mqttURI, mqttPort)
                .WithCleanSession();

            var options = mqttSecure
                ? messageBuilder
                    .WithTls()
                    .Build()
                : messageBuilder
                    .Build();

            var managedOptions = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(options)
                .Build();

            client = new MqttFactory().CreateManagedMqttClient();

            //
            // Define the handlers
            //
            client.UseConnectedHandler(e => ConnectedHandler(e));
            client.UseDisconnectedHandler(e => DisconnectedHandler(e));
            client.UseApplicationMessageReceivedHandler(e => MsgRecvHandler(e));

            await client.StartAsync(managedOptions);
        }

        public static IManagedMqttClient GetMqttClient()
        {
            return client;
        }

        public static async Task PublishAsync(string topic, string payload, bool retainFlag = true, int qos = 1)
        {
            if (client != null)
            {
                await client.PublishAsync(new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
                    .WithRetainFlag(retainFlag)
                    .Build());
            }
        }

        public static void ConnectedHandler(MqttClientConnectedEventArgs e)
        {
            string strMsg = "MqttClient: connected successfully to MQTT broker";
            m_logger.Debug(strMsg);
            ConnectMqttAnyDevice();
            return;
        }

        public static void DisconnectedHandler(MqttClientDisconnectedEventArgs e)
        {
            string strMsg = "MqttClient: disconnected from MQTT broker";
            m_logger.Debug(strMsg);
            return;
        }

        public static void MsgRecvHandler(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                string topic = e.ApplicationMessage.Topic.Trim();

                if (string.IsNullOrEmpty(topic) == false && m_businessLogic != null)
                {
                    string payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                    // parse the msg
                    var jsonmsginfo = Newtonsoft.Json.JsonConvert.DeserializeObject<JsonMsg>(payload);

                    string strMsg = string.Format("MqttClient Topic: {0}, Msg recvd: {1}", topic, payload);
                    string strDevId = jsonmsginfo.id;
                    m_logger.DebugFormat("{0}: {1} - {2}", strDevId, topic, strMsg);

                    #region Maybe useful later
                    //// distribute the payload depending on topic keyword
                    //// ToDo: make this OOP using Chain of command
                    //// maybe do this as demo for new hires
                    //if (topic.ToLower().Contains("/heartbeat"))
                    //{
                    //    try
                    //    {
                    //        var dta = JsonConvert.DeserializeObject<HeartbeatModel>(payload);
                    //        m_businessLogic.SendHeartBeatAsync(dta).Wait();
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        string strErr = string.Format("MqttClient recv handler heartbeat json parser: {0} - {1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : "");
                    //        m_logger.Debug(strErr);
                    //    }
                    //}
                    //else if (topic.ToLower().Contains("/savedata"))
                    //{
                    //    var dev = topic.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    //    m_businessLogic.SaveDataAsync(payload, dev[0]).Wait();
                    //}
                    //else if (topic.ToLower().Contains("/config"))
                    //{
                    //    try
                    //    {
                    //        var dta = JsonConvert.DeserializeObject<DeviceModel>(payload);
                    //        m_businessLogic.SaveConfigAsync(dta).Wait();
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        string strErr = string.Format("MqttClient recv handler heartbeat json parser: {0} - {1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : "");
                    //        m_logger.Debug(strErr);
                    //    }
                    //}
                    #endregion

                    // put under Task.Run so this handler doesn't have to wait SaveToDB
                    if (topic.ToLower().Contains(ConfigurationManager.AppSettings["SubscribeTopic"]))
                    {
                        var bRet = Task.Run<bool>(() => {
                            return (m_businessLogic.SaveToDB(payload, strDevId));
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                string strErr = string.Format("MqttClient recv handler: {0} - {1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : "");
                m_logger.Debug(strErr);
            }
        }

        public static async Task SubscribeAsync(string topic, int qos = 1)
        {
            if (client != null)
            {
                await client.SubscribeAsync(new MqttTopicFilterBuilder()
                    .WithTopic(topic)
                    .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
                    .Build());
            }
        }

        public static bool ConnectMqttAnyDevice()
        {
            if (m_businessLogic != null)
            {
                var strTopic = ConfigurationManager.AppSettings["SubscribeTopic"];
                var qosLevel = Convert.ToInt32(ConfigurationManager.AppSettings["QOS"]);
                if (!string.IsNullOrEmpty(strTopic))
                {
                    // parse CSV topics
                    var strTopics = strTopic.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string str in strTopics)
                    {
                        SubscribeAsync(str, qosLevel).Wait();
                    }
                }
                
                return true;
            }
            return false;
        }
    }
}
