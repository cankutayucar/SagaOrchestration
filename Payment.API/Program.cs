using MassTransit;
using Payment.API.Consumers;
using Shared.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(configurator =>
{
    configurator.AddConsumer<PaymentStartedEventConsumer>();
    configurator.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("amqps://cqzmvjvj:sWnh_3prvLwKjGYCdtpEjOJwAN07uMzm@vulture.rmq.cloudamqp.com/cqzmvjvj");
        cfg.ReceiveEndpoint(RabbitMqSettings.Payment_Started_Event_Queue, e =>
        {
            e.ConfigureConsumer<PaymentStartedEventConsumer>(context);
        });
    });
});
var app = builder.Build();




app.Run();
