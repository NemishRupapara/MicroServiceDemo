//using RabbitMQ.Client;
//using System.Text;

//public class RabbitMQPublisher
//{
//    private readonly IConfiguration _configuration;
//    private IConnection _connection;

//    public RabbitMQPublisher(IConfiguration configuration)
//    {
//        _configuration = configuration;
//        InitializeRabbitMQ();
//    }

//    private void InitializeRabbitMQ()
//    {
//        var factory = new ConnectionFactory()
//        {
//            HostName = _configuration["RabbitMQ:HostName"],
//            UserName = _configuration["RabbitMQ:UserName"],
//            Password = _configuration["RabbitMQ:Password"]
//        };
//        _connection = factory.CreateConnection();
//    }

//    public void SendProductRequest(int productId)
//    {
//        using (var channel = _connection.CreateModel())
//        {
//            // Declare a queue
//            channel.QueueDeclare(queue: "product_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);

//            string message = productId.ToString();
//            var body = Encoding.UTF8.GetBytes(message);

//            // Send the message to RabbitMQ
//            channel.BasicPublish(exchange: "", routingKey: "product_queue", basicProperties: null, body: body);
//            Console.WriteLine($" [x] Sent ProductId: {message}");
//        }
//    }
//}


using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using CartService.Models;

public class RabbitMQPublisher
{
    private readonly IConfiguration _configuration;
    private IConnection _connection;

    public RabbitMQPublisher(IConfiguration configuration)
    {
        _configuration = configuration;
        InitializeRabbitMQ();
    }

    private void InitializeRabbitMQ()
    {
        var factory = new ConnectionFactory()
        {
            HostName = _configuration["RabbitMQ:HostName"],
            UserName = _configuration["RabbitMQ:UserName"],
            Password = _configuration["RabbitMQ:Password"]
        };
        _connection = factory.CreateConnection();
    }

    public Product SendProductRequest(int productId)
    {
        using (var channel = _connection.CreateModel())
        {
            // Declare a queue for the reply (RPC)
            var replyQueue = channel.QueueDeclare(queue: "", durable: false, exclusive: true, autoDelete: true);
            var replyQueueName = replyQueue.QueueName;

            // Set up a correlation ID to match the request and response
            var correlationId = Guid.NewGuid().ToString();

            var properties = channel.CreateBasicProperties();
            properties.ReplyTo = replyQueueName;
            properties.CorrelationId = correlationId;

            string message = productId.ToString();
            var body = Encoding.UTF8.GetBytes(message);

            // Send the message to RabbitMQ
            channel.BasicPublish(exchange: "", routingKey: "product_queue", basicProperties: properties, body: body);
            Console.WriteLine($" [x] Sent ProductId: {message}");

            // Listen for the response in the reply queue
            var consumer = new EventingBasicConsumer(channel);
            string response = null;

            consumer.Received += (model, ea) =>
            {
                if (ea.BasicProperties.CorrelationId == correlationId)
                {
                    response = Encoding.UTF8.GetString(ea.Body.ToArray());
                    Console.WriteLine($" [x] Received response: {response}");
                }
            };

            channel.BasicConsume(consumer: consumer, queue: replyQueueName, autoAck: true);

            // Wait for the response (simplified for demo purposes)
            while (response == null)
            {
                Task.Delay(100).Wait();
            }

            //return response;
            var product = JsonSerializer.Deserialize<Product>(response);
            return product;
        }
    }
}
