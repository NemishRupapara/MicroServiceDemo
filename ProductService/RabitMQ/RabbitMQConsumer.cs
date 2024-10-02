//using Microsoft.AspNetCore.Connections;
//using Microsoft.EntityFrameworkCore.Metadata;
//using ProductService.Services;
//using RabbitMQ.Client;
//using RabbitMQ.Client.Events;
//using System.Text;
//using System.Threading.Tasks;

//public class RabbitMQConsumer
//{
//    private readonly IConfiguration _configuration;
//    private readonly ProductServiceClass _productService;
//    private IConnection _connection;
//    private RabbitMQ.Client.IModel _channel;

//    public RabbitMQConsumer(IConfiguration configuration, ProductServiceClass productService)
//    {
//        _configuration = configuration;
//        _productService = productService;
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
//        _channel = _connection.CreateModel();

//        _channel.QueueDeclare(queue: "product_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);
//    }

//    public void StartListening()
//    {
//        var consumer = new EventingBasicConsumer(_channel);

//        consumer.Received += async (model, ea) =>
//        {
//            var body = ea.Body.ToArray();
//            var message = Encoding.UTF8.GetString(body);
//            int productId = int.Parse(message);

//            // Fetch product details from ProductService
//            var product =  _productService.GetById(productId);

//            Console.WriteLine($" [x] Received ProductId: {productId}");
//            // You can process the product here or publish it to another queue for CartService
//        };

//        _channel.BasicConsume(queue: "product_queue", autoAck: true, consumer: consumer);
//    }
//}






//using RabbitMQ.Client.Events;
//using RabbitMQ.Client;
//using ProductService.Services;
//using System.Text;

//public class RabbitMQConsumer
//{
//    private readonly IConfiguration _configuration;
//    private readonly IServiceScopeFactory _scopeFactory;
//    private IConnection _connection;
//    private RabbitMQ.Client.IModel _channel;

//    public RabbitMQConsumer(IConfiguration configuration, IServiceScopeFactory scopeFactory)
//    {
//        _configuration = configuration;
//        _scopeFactory = scopeFactory;
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
//        _channel = _connection.CreateModel();

//        _channel.QueueDeclare(queue: "product_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);
//    }

//    public void StartListening()
//    {
//        var consumer = new EventingBasicConsumer(_channel);

//        consumer.Received += async (model, ea) =>
//        {
//            var body = ea.Body.ToArray();
//            var message = Encoding.UTF8.GetString(body);
//            int productId = int.Parse(message);

//            // Create a scope to resolve ProductServiceClass and DbContext for each message
//            using (var scope = _scopeFactory.CreateScope())
//            {
//                var productService = scope.ServiceProvider.GetRequiredService<ProductServiceClass>();
//                var product = productService.GetById(productId);

//                Console.WriteLine($" [x] Received ProductId: {productId}");
//                // You can process the product here or publish it to another queue for CartService
//            }
//        };

//        _channel.BasicConsume(queue: "product_queue", autoAck: true, consumer: consumer);
//    }
//}



using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using ProductService.Services;
using System.Text.Json;

public class RabbitMQConsumer
{
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private IConnection _connection;
    private RabbitMQ.Client.IModel _channel;

    public RabbitMQConsumer(IConfiguration configuration, IServiceScopeFactory scopeFactory)
    {
        _configuration = configuration;
        _scopeFactory = scopeFactory;
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
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(queue: "product_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);
    }

    public void StartListening()
    {
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            int productId = int.Parse(message);

            using (var scope = _scopeFactory.CreateScope())
            {
                var productService = scope.ServiceProvider.GetRequiredService<ProductServiceClass>();
                var product = productService.GetById(productId);

                Console.WriteLine($" [x] Received ProductId: {productId}");

                // Reply with the product data
                var responseProps = _channel.CreateBasicProperties();
                responseProps.CorrelationId = ea.BasicProperties.CorrelationId;

                //var responseMessage = product != null ? product.Name : "Product not found";
                var responseMessage = product != null
                                  ? JsonSerializer.Serialize(product)
                                  : JsonSerializer.Serialize(new { Error = "Product not found" });
                var responseBytes = Encoding.UTF8.GetBytes(responseMessage);

                _channel.BasicPublish(
                    exchange: "",
                    routingKey: ea.BasicProperties.ReplyTo,
                    basicProperties: responseProps,
                    body: responseBytes);
            }
        };

        _channel.BasicConsume(queue: "product_queue", autoAck: true, consumer: consumer);
    }
}
