using System;
using System.IO.Ports;
using System.Net.Mqtt;
using System.Threading;
using System.Threading.Tasks;

namespace Serialino
{
    class Program
    {
        private static IMqttClient mqttClient;
        private static readonly SerialPort serialPort = new SerialPort();
        //private static readonly byte[] serialBuffer = new byte[64]; 
        private static readonly AutoResetEvent _close = new AutoResetEvent(false);

        private static async Task Main(string[] args)
        {
            SerialOpen();
            await MQTTConnectAsync();
            await MQTTSubscribeAsync();

            Console.CancelKeyPress += new ConsoleCancelEventHandler(OnExit);
            _close.WaitOne();
        }

        protected static void OnExit(object sender, ConsoleCancelEventArgs args)
        {
            _close.Set();
        }

        private static async Task MQTTConnectAsync()
        {
            MqttConfiguration config = new MqttConfiguration
            {
                BufferSize = 128 * 1024,
                Port = 8883,
                KeepAliveSecs = 10,
                WaitTimeoutSecs = 2,
                MaximumQualityOfService = MqttQualityOfService.AtMostOnce,
                AllowWildcardsInTopicFilters = true
            };

            //mqttClient = await MqttClient.CreateAsync("192.168.178.220", config);
            //mqttClient = await MqttClient.CreateAsync("172.17.0.1", config);
            mqttClient = await MqttClient.CreateAsync("Inlcude IP Adress", config);
            await mqttClient.ConnectAsync(new MqttClientCredentials(clientId: "Serialino", userName: "MQTTUser", password: "MQTTPass"), cleanSession: true);
        }

        private static async Task MQTTSendAsync(string topic, byte[] payload)
        {
            MqttApplicationMessage msg = new MqttApplicationMessage(topic, payload);
            await mqttClient.PublishAsync(msg, MqttQualityOfService.AtMostOnce);
        }

        private static async Task MQTTSubscribeAsync()
        {
            await mqttClient.SubscribeAsync("/Serialino", MqttQualityOfService.AtMostOnce);

            mqttClient
                .MessageStream
                .Subscribe(msg =>
                {
                    Message message = new Message();
                    if (message.mqttReceived(msg))
                    {
                        SerialSend(message.Payload);
                    }
                });
        }

        private static void SerialOpen()
        {
            serialPort.PortName = SerialPort.GetPortNames()[0];
            serialPort.BaudRate = 9600;
            serialPort.Parity = Parity.None;
            serialPort.DataBits = 8;

            byte[] newline = new[] { (byte)13, (byte)10 };
            serialPort.NewLine = System.Text.Encoding.UTF8.GetString(newline);
            //serialPort.ReadBufferSize
            //serialPort.ReadTimeout = 5;
  
            serialPort.DataReceived += new SerialDataReceivedEventHandler(OnSerialEvent);
            serialPort.Open();
        }

        private static void SerialSend(byte[] msg)
        {
            serialPort.Write(msg, 0, msg.Length);
            // Sollte nicht notwendig sein
            //serialPort.DiscardInBuffer();
            //serialPort.DiscardOutBuffer();
        }

        private static void OnSerialEvent(object sender, SerialDataReceivedEventArgs args)
        {
            Message msg = new Message();

            SerialPort port = sender as SerialPort;

            Console.WriteLine("EVENT!");
            Console.Write("B");
            Console.Write(port.BytesToRead.ToString());
            Console.WriteLine();

            if(port.BytesToRead > 0)
            {
                if(msg.serialReceived(port.ReadLine()))
                {
                    MQTTSendAsync(msg.Topic, msg.Payload);

                    if(port.BytesToRead > 0)
                    {
                        OnSerialEvent(sender, args);
                    }                    
                }
            }
        }


        private static async void OnSerialEventAsync(object sender, SerialDataReceivedEventArgs args)
        {
            SerialPort port = sender as SerialPort;           
            
            Message msg = new Message();
            //System.Text.Encoding.UTF8

            //Console.WriteLine(port.ReadExisting());
            int i = port.BytesToRead;

/*
            byte[] buffer = new byte[i];
            port.Read(buffer, 0, i);

            foreach (byte b in buffer)
            {
                Console.Write(b.ToString());
            }
*/
            //Console.WriteLine(System.Text.Encoding.UTF8.GetString(buffer));
            
            Console.Write(">");
            Console.Write(port.ReadLine());
            Console.Write("<");
            Console.Write(port.BytesToRead.ToString());
            Console.WriteLine(">");
            OnSerialEventAsync(sender, args);
            

            if(msg.serialReceived(port.ReadLine()))
            {
                await MQTTSendAsync(msg.Topic, msg.Payload);
            }


/*
            if(port.BytesToRead > 0)
            {
                OnSerialEventAsync(sender, args);
            }
*/
        }
    }
}