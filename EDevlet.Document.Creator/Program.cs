using EDevlet.Document.Common;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDevlet.Document.Creator
{
    class Program
    {
        static IConnection connection;
        private static readonly string createDocument = "create_document_queue";
        private static readonly string documentCreated = "document_created_queue";
        private static readonly string documentCreateExchange = "document_create_exchange";

        static IModel _channel;
        static IModel channel => _channel ?? (_channel = GetChannel());


        static void Main(string[] args)
        {
            connection = GetConnection();

            channel.ExchangeDeclare(documentCreateExchange, "direct");

            channel.QueueDeclare(createDocument, false, false, false);
            channel.QueueBind(createDocument, documentCreateExchange, createDocument);

            channel.QueueDeclare(documentCreated, false, false, false);
            channel.QueueBind(documentCreated, documentCreateExchange, documentCreated);

            var consumerEvent = new EventingBasicConsumer(channel);

            consumerEvent.Received += (ch, ea) =>
            {
                var modelJson = Encoding.UTF8.GetString(ea.Body.ToArray());
                var model = JsonConvert.DeserializeObject<CreateDocumentModel>(modelJson);

                Console.WriteLine($"Received Data : {modelJson}");

                // Create document

                Task.Delay(5000).Wait();

                // document goes to ftp

                model.Url = "http://www.turkiye.gov.tr/docs/x.pdf";

                WriteToQueue(documentCreated, model);
            };

            channel.BasicConsume(createDocument, true, consumerEvent);

            Console.WriteLine($"{documentCreateExchange} listening");

            Console.ReadLine();
        }



        private static void WriteToQueue(string queueName, CreateDocumentModel model)
        {
            var messageArr = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(model));

            channel.BasicPublish(documentCreateExchange, queueName, null, messageArr);

            Console.WriteLine("Message Published");
        }


        private static IModel GetChannel()
        {
            return connection.CreateModel();
        }

        private static IConnection GetConnection()
        {
            var connectionFactory = new ConnectionFactory()
            {
                Uri = new Uri("amqp://guest:guest@localhost:5672")
            };

            return connectionFactory.CreateConnection();
        }
    }
}
