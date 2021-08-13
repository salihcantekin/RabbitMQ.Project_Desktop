using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

namespace Consumer.ConsoleApp
{
    public class Program
    {
        private static string connectionString = "amqp://guest:guest@localhost:5672";
        private static string queueName;
        private static IConnection connection;
        private static IModel _channel;
        private static IModel channel => _channel ??= CreateOrGetChannel();

        static void Main(string[] args)
        {
            queueName = args.Length > 0 ? args[0] : "test_queue";

            connection = GetConnection();

            channel.QueueDeclare(queueName, exclusive: false);
            channel.QueueBind(queueName, "exchange_dir", queueName);

            var consumerEvent = new EventingBasicConsumer(channel);

            consumerEvent.Received += (ch, e) => 
            {
                var byteArr = e.Body.ToArray();
                var bodyStr = Encoding.UTF8.GetString(byteArr);

                Console.WriteLine($"Received Data: {bodyStr}");

                channel.BasicAck(e.DeliveryTag, false);
            };


            channel.BasicConsume(queueName, false, consumerEvent);

            Console.WriteLine($"{queueName} listening....\n\n\n");



            Console.ReadLine();
        }



        private static IModel CreateOrGetChannel()
        {
            return connection.CreateModel();
        }


        private static IConnection GetConnection()
        {
            ConnectionFactory factory = new ConnectionFactory()
            {
                Uri = new Uri(connectionString, UriKind.RelativeOrAbsolute)
            };

            return factory.CreateConnection();
        }
    }
}
