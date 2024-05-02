using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Order.API.Consumers;
using Order.API.Contexts;
using Order.API.Models;
using Order.API.ViewModels;
using Shared.Messages;
using Shared.OrderEvents;
using Shared.Settings;

var builder = WebApplication.CreateBuilder(args);




builder.Services.AddMassTransit(configurator =>
{
    configurator.AddConsumer<OrderCompletedEventConsumer>();
    configurator.AddConsumer<OrderFailedEventConsumer>();
    configurator.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("amqps://cqzmvjvj:sWnh_3prvLwKjGYCdtpEjOJwAN07uMzm@vulture.rmq.cloudamqp.com/cqzmvjvj");

        cfg.ReceiveEndpoint(RabbitMqSettings.Order_Order_Completed_Event_Queue, e =>
        {
            e.ConfigureConsumer<OrderCompletedEventConsumer>(context);
        });
        cfg.ReceiveEndpoint(RabbitMqSettings.Order_Order_Failed_Event_Queue, e =>
        {
            e.ConfigureConsumer<OrderFailedEventConsumer>(context);
        });
    });
});

builder.Services.AddDbContext<OrderDbContext>(options =>
{
    options.UseSqlServer("Server=.;Database=SagaStateMachine;Trusted_Connection=SSPI;Encrypt=false;TrustServerCertificate=true");
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI();



app.MapPost("/create-order", async ([FromBody] CreateOrderVM createOrderVM, [FromServices] OrderDbContext dbContext, [FromServices] IPublishEndpoint publishEndpoint, [FromServices] ISendEndpointProvider sendEndpointProvider) =>
{
    Order.API.Models.Order order = new()
    {
        BuyerId = createOrderVM.BuyerId,
        CreatedDate = DateTime.UtcNow,
        OrderStatus = Order.API.Enums.OrderStatus.Suspend,
        TotalPrice = createOrderVM.OrderItemVMs.Sum(x => x.Count * x.Price),
        OrderItems = createOrderVM.OrderItemVMs.Select(x => new OrderItem
        {
            ProductId = x.ProductId,
            Count = x.Count,
            Price = x.Price
        }).ToList()
    };
    await dbContext.Orders.AddAsync(order);
    await dbContext.SaveChangesAsync();


    OrderStartedEvent orderStartedEvent = new()
    {
        BuyerId = createOrderVM.BuyerId,
        OrderId = order.Id,
        OrderItems = createOrderVM.OrderItemVMs.Select(x => new OrderItemMessage
        {
            ProductId = x.ProductId,
            Count = x.Count,
            Price = x.Price
        }).ToList(),
        TotalPrice = createOrderVM.OrderItemVMs.Sum(x => x.Count * x.Price)
    };

    var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{RabbitMqSettings.StateMachine_Queue}"));
    await endpoint.Send(orderStartedEvent);
});

app.Run();
