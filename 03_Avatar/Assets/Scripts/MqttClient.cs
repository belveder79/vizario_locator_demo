

using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;
using System.Security.Cryptography.X509Certificates;

#if UNITY_ANDROID || UNITY_IOS
using UnityEngine.Networking;
#endif


//using JSCallback = System.Func<Jint.Native.JsValue, Jint.Native.JsValue[], Jint.Native.JsValue>;
using JSCallback = System.Func<string[], string>;

internal class MQTTClient : IDisposable
{
    private static MQTTClient _instance = null;

    private static MQTTClient GetInstance()
    {
        if (_instance == null)
        {
            Debug.Log("create new instace.");
            _instance = new MQTTClient();
        }
        return _instance;
    }

    public class HostData
    {
        //todo host data?
        //public string hostUrl = "127.0.0.1";
        public string hostUrl = "192.168.43.1";
        public int hostPort = 1883;
    }

    private IMqttClient mqttClient;

    private static Dictionary<int, JSCallback> callbacks = new Dictionary<int, JSCallback>();

    private HostData hostData = new HostData();

    public string clientId = "";
    public bool connected = false;

    private void Awake()
    {
        //clientId = "thiscompanion";
        clientId = "UnityDLL";
        Debug.Log(GetType() + "::clientId=" + clientId);
    }


    public void Dispose()
    {
        Debug.Log("--------------- Dispoese -------------");

        mqttClient.DisconnectAsync();

    }

    private async void InitClientWithData(string url, int port, X509Certificate caCert)
    {
        mqttClient = new MqttFactory().CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithClientId(clientId)
            .WithTcpServer(url, port)
            .Build();

        if(caCert != null) {
          options = new MqttClientOptionsBuilder()
              .WithClientId(clientId)
              .WithTcpServer(url, port)
              //.WithCredentials(username, psw)
              .WithTls(new MqttClientOptionsBuilderTlsParameters()
              {
                  AllowUntrustedCertificates = false,
                  UseTls = true,
                  // Certificates = new List<byte[]> { File.ReadAllBytes(p) },
                  Certificates = new List<X509Certificate> { new X509Certificate2(caCert) },
                  CertificateValidationCallback = delegate { return true; },
                  IgnoreCertificateChainErrors = false,
                  IgnoreCertificateRevocationErrors = false
              })
              .WithCleanSession()
              .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V310)
              .Build();
        }


        // Connecting

        mqttClient.UseConnectedHandler(async e =>
        {
            Debug.Log("### CONNECTED WITH SERVER ###");
            connected = true;
                //SubscribeImpl("test");
            });

        mqttClient.UseApplicationMessageReceivedHandler(e =>
        {
                //Debug.Log("### RECEIVED APPLICATION MESSAGE ###");
                //Debug.Log($"+ Topic = {e.ApplicationMessage.Topic}");
                //Debug.Log($"+ Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
                //Debug.Log($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
                //Debug.Log($"+ Retain = {e.ApplicationMessage.Retain}");

            var topic = e.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            string[] args = { topic, payload };

            foreach (var cb in callbacks.Values)
            {
                cb.Invoke(args);
            }
        });
        Debug.Log("connect to " + url);
        await mqttClient.ConnectAsync(options);


    }

    public void SetID(string ClientID)
    {
        clientId = ClientID;
    }

    public void InitClientImpl()
    {
        var url = hostData.hostUrl;
        var port = hostData.hostPort;
        InitClientWithData(url, port, null);
    }

    public void InitClientImpl(X509Certificate cert)
    {
        var url = hostData.hostUrl;
        var port = hostData.hostPort;
        InitClientWithData(url, port, cert);
    }

    public async

        void SubscribeImpl(string topic)
    {
        try
        {
            if (mqttClient != null)
            {

                Debug.Log("SubscribeImpl subscribing => " + topic);
                await mqttClient.SubscribeAsync(topic);


            }
        }
        catch (Exception e)
        {
            Debug.LogError("SubscribeImpl Error => " + e);
            connected = false;
        }
    }

    public
        async

        void UnsubscribeImpl(string topic)
    {
        if (mqttClient != null)
        {

            await mqttClient.UnsubscribeAsync(topic);

        }
    }

    public

        async

        void PublishImple(string topic, string payload)
    {

        if (mqttClient != null)
        {
            await Task.Run(() => mqttClient.PublishAsync(topic, payload));
        }
        else
        {
            Debug.Log("MQTT PublishImple ERROR => mqttClient is null");
        }

    }

    public static void Subscribe(string topic)
    {
        MQTTClient.GetInstance().SubscribeImpl(topic);
    }

    public static void Unsubscribe(string topic)
    {
        MQTTClient.GetInstance().UnsubscribeImpl(topic);
    }

    public static void CallDispose()
    {
        MQTTClient.GetInstance().Dispose();
        MQTTClient._instance = null;
    }



    public void RegisterCallbackImple(JSCallback fct)
    {
        var index = fct.GetHashCode();
        if (callbacks.ContainsKey(index))
        {
            callbacks[index] = fct;
        }
        else
        {
            callbacks.Add(index, fct);
        }
    }

    public void UnregisterCallbackImple(JSCallback fct)
    {
        var index = fct.GetHashCode();
        if (callbacks.ContainsKey(index))
        {
            callbacks.Remove(index);
        }
    }

    public void SetHostImpl(string url, int port)
    {
        hostData.hostUrl = url;
        hostData.hostPort = port;
        Debug.Log("host set");
    }

    /// STATIC EXPORTS

    public static void Publish(string topic, string payload)
    {
        GetInstance().PublishImple(topic, payload);
    }

    public static void RegisterCallback(JSCallback fct)
    {
        GetInstance().RegisterCallbackImple(fct);
    }

    public static void UnregisterCallback(JSCallback fct)
    {
        GetInstance().UnregisterCallbackImple(fct);
    }

    public static void SetHost(string url, int port)
    {
        GetInstance().SetHostImpl(url, port);
    }

    public static void StartClient()
    {
        GetInstance().InitClientImpl();
    }

    public static void StartClientWithCert(X509Certificate cert)
    {
        GetInstance().InitClientImpl(cert);
    }

    public static string GetGuid()
    {
        return GetInstance().clientId;
    }

    public static void SetClientID(string ClientID)
    {
        GetInstance().SetID(ClientID);
    }

    public static bool IsConnected()
    {
        return GetInstance().connected;
    }
}
