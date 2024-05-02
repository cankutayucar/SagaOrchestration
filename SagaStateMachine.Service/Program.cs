

using MassTransit;
using Microsoft.EntityFrameworkCore;
using SagaStateMachine.Service.StateDbContexts;
using SagaStateMachine.Service.StateInstances;
using SagaStateMachine.Service.StateMachines;
using Shared.Settings;

var builder = Host.CreateApplicationBuilder(args);



builder.Services.AddMassTransit(configurator =>
{

    configurator.AddSagaStateMachine<OrderStateMachine, OrderStateInstance>()
        .EntityFrameworkRepository(r =>
        {
            r.ConcurrencyMode = ConcurrencyMode.Pessimistic; // or use Optimistic, which requires RowVersion
            r.AddDbContext<DbContext, OrderStateDbContext>((provider, builder) =>
            {
                builder.UseSqlServer("Server=localhost;Database=OrderStateDb;Trusted_Connection=True;MultipleActiveResultSets=true");
            });
        });







    configurator.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("amqps://cqzmvjvj:sWnh_3prvLwKjGYCdtpEjOJwAN07uMzm@vulture.rmq.cloudamqp.com/cqzmvjvj");

        cfg.ReceiveEndpoint(RabbitMqSettings.StateMachine_Queue, e => e.ConfigureSaga<OrderStateInstance>(context));
    });
});





var host = builder.Build();




host.Run();
