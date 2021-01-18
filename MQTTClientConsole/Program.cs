using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Connecting;

namespace MQTTClientConsole
{
    class Program
    {
        static IMqttClient _mqttClient;
        static string _sensorTemperatureTopic = "sensors/temp";
        static string _lcdTopic = "peripherals/lcd_display";
        static string _ledTopic = "peripherals/led";

        static void Main(string[] args)
        {
            InitAsync(CancellationToken.None).Wait();

            while (true) {

            }
        }

        static async Task InitAsync(CancellationToken cancellationToken) {
            var factory = new MqttFactory();

            if(_mqttClient == null)
                _mqttClient = factory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithClientId("ConsoleClientMQTTApp")
                .WithTcpServer("[hostname]", 1885)
                .WithCredentials("[username]", "[password]")
                .WithCleanSession()
                .Build();

            var result = await _mqttClient.ConnectAsync(options, cancellationToken);
            
            if(result.ResultCode == MqttClientConnectResultCode.Success) {
                await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                    .WithTopic(_sensorTemperatureTopic)
                    .WithExactlyOnceQoS()
                    .Build());
            }
            
            _mqttClient.UseDisconnectedHandler(async e => {
                Console.WriteLine("### DISCONNECTED FROM SERVER ###");
                await Task.Delay(TimeSpan.FromSeconds(5));

                try
                {
                    result = await _mqttClient.ConnectAsync(options, cancellationToken);
                }
                catch
                {
                    Console.WriteLine("### RECONNECTING FAILED ###");
                }
            });

            _mqttClient.UseApplicationMessageReceivedHandler(e => {
                if(e.ApplicationMessage.Topic == _sensorTemperatureTopic) {
                    var temperature = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                    Console.WriteLine($"Temperature {temperature}");
                }

                Task.Run(() => _mqttClient.PublishAsync("hello/world"));
            });

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(_lcdTopic)
                .WithPayload("Hello world from my console app")
                .WithExactlyOnceQoS()
                .Build();

            await _mqttClient.PublishAsync(message, cancellationToken);
        }
    }
}
