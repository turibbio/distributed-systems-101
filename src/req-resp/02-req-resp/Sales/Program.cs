﻿using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

namespace Sales
{
    class Program
    {
        public static void Main()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ConfirmSelect();
                channel.QueueDeclare(queue: "sales",
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var receivedBody = ea.Body.ToArray();
                    var receivedMessage = Encoding.UTF8.GetString(receivedBody);
                    Console.WriteLine($"Received {receivedMessage} with correlation ID {ea.BasicProperties.CorrelationId}");

                    var replyProps = channel.CreateBasicProperties();
                    replyProps.CorrelationId = ea.BasicProperties.CorrelationId;

                    string replyMessage = $"Order {ea.BasicProperties.CorrelationId} on its way...";
                    var replyBody = Encoding.UTF8.GetBytes(replyMessage);

                    channel.BasicPublish(exchange: "",
                                     routingKey: ea.BasicProperties.ReplyTo,
                                     basicProperties: replyProps,
                                     body: replyBody);
                    channel.WaitForConfirmsOrDie(new TimeSpan(0, 0, 5));
                
                    Console.WriteLine($"Sent {replyMessage}");
                };
                channel.BasicConsume(queue: "sales",
                                     autoAck: true,
                                     consumer: consumer);

                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();
            }
        }
    }
}