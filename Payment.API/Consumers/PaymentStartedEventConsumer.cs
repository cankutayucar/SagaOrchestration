using MassTransit;
using Shared.PaymentEvents;
using Shared.Settings;

namespace Payment.API.Consumers
{
    public class PaymentStartedEventConsumer(ISendEndpointProvider sendEndpointProvider) : IConsumer<PaymentStartedEvent>
    {
        public async Task Consume(ConsumeContext<PaymentStartedEvent> context)
        {
            var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{RabbitMqSettings.StateMachine_Queue}"));
            if (true)
            {
                PaymentCompletedEvent paymentCompletedEvent = new(context.Message.CorrelationId)
                {

                };
                await endpoint.Send(paymentCompletedEvent);
            }
            else
            {
                PaymentFailedEvent paymentFailedEvent = new(context.Message.CorrelationId)
                {
                    Message = "Payment failed",
                    OrderItems = context.Message.OrderItems
                };
                await endpoint.Send(paymentFailedEvent);
            }
        }
    }
}
