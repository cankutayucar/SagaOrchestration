using MassTransit;
using MongoDB.Driver;
using Shared.Settings;
using Stock.API.Consumers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(configurator =>
{
    configurator.AddConsumer<OrderCreatedEventConsumer>();
    configurator.AddConsumer<StockRollbackMessageConsumer>();
    configurator.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("amqps://cqzmvjvj:sWnh_3prvLwKjGYCdtpEjOJwAN07uMzm@vulture.rmq.cloudamqp.com/cqzmvjvj");


        cfg.ReceiveEndpoint(RabbitMqSettings.Stock_Order_Created_Event_Queue, e =>
        {
            e.ConfigureConsumer<OrderCreatedEventConsumer>(context);
        });
        cfg.ReceiveEndpoint(RabbitMqSettings.Stock_Rollback_Message_Queue, e =>
        {
            e.ConfigureConsumer<StockRollbackMessageConsumer>(context);
        });
    });
});

builder.Services.AddSingleton<Stock.API.Services.MongoDbService>();


var app = builder.Build();


using var scope = builder.Services.BuildServiceProvider().CreateScope();

var mongoDbService = scope.ServiceProvider.GetRequiredService<Stock.API.Services.MongoDbService>();


var collection = mongoDbService.GetCollection<Stock.API.Models.Stock>();
var ss = await collection.FindAsync(filter: x => true);

if (!ss.Any())
{
    mongoDbService.GetCollection<Stock.API.Models.Stock>().InsertOne(new Stock.API.Models.Stock { ProductId = 1, Count = 100 });
    mongoDbService.GetCollection<Stock.API.Models.Stock>().InsertOne(new Stock.API.Models.Stock { ProductId = 2, Count = 200 });
    mongoDbService.GetCollection<Stock.API.Models.Stock>().InsertOne(new Stock.API.Models.Stock { ProductId = 3, Count = 300 });
}



    app.Run();
