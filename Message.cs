using System;
using System.Collections.Generic;
using System.Net.Mqtt;
using System.Text;

namespace Serialino
{
    class Message
    {
        private const byte Digital = 68;
        private const byte HeartBeat = 72;
        private const byte Port = 80;

        private string _topic;
        private byte[] _payload;

        private readonly string _mqtt_prefix;

        public Message()
        {
            _mqtt_prefix = "/Serialino/";
            _payload = System.Text.Encoding.UTF8.GetBytes("");
        }

        public string Topic { get => _mqtt_prefix + _topic; set => _topic = value; }
        public byte[] Payload { get => _payload; set => _payload = value; }

        public bool mqttReceived(MqttApplicationMessage msg)
        {
            if (msg.Payload is null)
            {
                Console.WriteLine($"Message received in topic {msg.Topic}");
                return false;
            }
            else
            {
                Console.WriteLine($"Message received in topic {msg.Topic} : {System.Text.Encoding.UTF8.GetString(msg.Payload)}");
                Payload = msg.Payload;
                return true;
            }
        }

        public bool serialReceived(string msg)
        {
            Console.WriteLine($"serialReceived: {msg}");

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(msg);

            Console.WriteLine(bytes[2].ToString());

            switch (bytes[0])
            {
                case Digital:
                {
                    string value;

                    if (bytes[2] == 48)
                    {
                        value = "open";
                    }
                    else if (bytes[2] == 49)
                    {
                        value = "closed";
                    }
                    else
                    {
                        value = "error";
                    }

                    Console.WriteLine("Digital");
                    Topic = System.Text.Encoding.UTF8.GetString(new[] { bytes[1] });
                    //Payload = System.Text.Encoding.UTF8.GetBytes($"{{\"state\": {System.Text.Encoding.UTF8.GetString(new[] { bytes[2] })}}}");
                    Payload = System.Text.Encoding.UTF8.GetBytes($"{{\"state\": \"{value}\"}}");
                    //Payload = new[] { bytes[2] };
                    return true;
                }
                case Port:
                {
                    Console.WriteLine("Port");
                    Topic = System.Text.Encoding.UTF8.GetString(new[] { bytes[1] }) + "/port";

                    byte[] values = new byte[bytes.Length - 2];
                    Array.Copy(bytes, 2, values, 0, values.Length);

                    Payload = System.Text.Encoding.UTF8.GetBytes($"{{\"port\": {System.Text.Encoding.UTF8.GetString(values)}}}");
                    //Payload = values;

                    return true;
                }
		        case HeartBeat:
                {
                    Console.WriteLine("HeartBeat");
                    Topic = "heartbeat";

                    byte[] values = new byte[bytes.Length - 1];
                    Array.Copy(bytes, 1, values, 0, values.Length);

                    Payload = System.Text.Encoding.UTF8.GetBytes($"{{\"heartbeat\": {System.Text.Encoding.UTF8.GetString(values)}}}");
                    //Payload = values;
                    //Payload = bytes;

                    return true;
                }
                default:
                {
                    return false;
                }
            }
        }
    }
}