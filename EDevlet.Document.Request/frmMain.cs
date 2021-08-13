using EDevlet.Document.Common;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EDevlet.Document.Request
{
    public partial class frmMain : Form
    {
        IConnection connection;
        private readonly string createDocument = "create_document_queue";
        private readonly string documentCreated = "document_created_queue";
        private readonly string documentCreateExchange = "document_create_exchange";

        IModel _channel;
        IModel channel => _channel ?? (_channel = GetChannel());



        public frmMain()
        {
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (connection == null || !connection.IsOpen)
                connection = GetConnection();

            btnCreateDocument.Enabled = true;

            channel.ExchangeDeclare(documentCreateExchange, "direct");

            channel.QueueDeclare(createDocument, false, false, false);
            channel.QueueBind(createDocument, documentCreateExchange, createDocument);

            channel.QueueDeclare(documentCreated, false, false, false);
            channel.QueueBind(documentCreated, documentCreateExchange, documentCreated);

            AddLog("Connection is open now");
        }

        private void btnCreateDocument_Click(object sender, EventArgs e)
        {
            var model = new CreateDocumentModel()
            {
                UserId = 1,
                DocumentType = DocumentType.Pdf
            };

            WriteToQueue(createDocument, model);

            frmSplash frmSplash = new frmSplash();
            frmSplash.Show();

            var consumerEvent = new EventingBasicConsumer(channel);

            consumerEvent.Received += (ch, ea) =>
            {
                var modelReceived = JsonConvert.DeserializeObject<CreateDocumentModel>(Encoding.UTF8.GetString(ea.Body.ToArray()));
                AddLog($"Received Data Url: {modelReceived.Url}");

                closeSplashScreen(frmSplash);
            };

            channel.BasicConsume(documentCreated, true, consumerEvent);
        }


        private void closeSplashScreen(frmSplash frmSplash)
        {
            if (frmSplash.InvokeRequired)
            {
                frmSplash.Invoke(new Action(() => closeSplashScreen(frmSplash)));
                return;
            }

            frmSplash.Close();
        }



        private void WriteToQueue(string queueName, CreateDocumentModel model)
        {
            var messageArr = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(model));

            channel.BasicPublish(documentCreateExchange, queueName, null, messageArr);

            AddLog("Message Published");
        }


        private IModel GetChannel()
        {
            return connection.CreateModel();
        }

        private IConnection GetConnection()
        {
            var connectionFactory = new ConnectionFactory()
            {
                Uri = new Uri(txtConnectionString.Text)
            };

            return connectionFactory.CreateConnection();
        }

        private void AddLog(string logStr)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => AddLog(logStr)));
                return;
            }

            logStr = $"[{DateTime.Now:dd.MM.yyyy HH:mm:ss}] - {logStr}";
            txtLog.AppendText($"{logStr}\n");

            // set the cursor to end
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }
    }
}
