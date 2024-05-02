using MassTransit;
using SagaStateMachine.Service.StateInstances;
using Shared.Messages;
using Shared.OrderEvents;
using Shared.PaymentEvents;
using Shared.Settings;
using Shared.StockEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SagaStateMachine.Service.StateMachines
{
    public class OrderStateMachine : MassTransitStateMachine<OrderStateInstance>
    {
        // gelebilecek eventler bu şekilde tanımlanır. ve state machinede temsil edilir.
        public Event<OrderStartedEvent> OrderStartedEvent { get; set; }
        public Event<StockReservedEvent> StockReservedEvent { get; set; }
        public Event<StockNotReservedEvent> StockNotReservedEvent { get; set; }
        public Event<PaymentCompletedEvent> PaymentCompletedEvent { get; set; }
        public Event<PaymentFailedEvent> PaymentFailedEvent { get; set; }

        public State OrderCreated { get; set; }
        public State StockReserved { get; set; }
        public State StockNotReserved { get; set; }
        public State PaymentCompleted { get; set; }
        public State PaymentFailed { get; set; }


        public OrderStateMachine()
        {
            InstanceState(instance => instance.CurrentState);
            //Event fonksiyonu gelen eventlere göre aksiyon almamızı sağlayan bir fonksiyondur.


            // sana gelen event OrderStartedEvent ise x ile içerisine gir. OrderStartedEvent bir tetikleyici eventtir.

            // ya yeni bir correlationId oluştur ya da gelen eventin correlationId'sini al.

            // order state machinenin databesesindeki OrderId ile gelen eventin OrderId'si eşleşirse yeni bir satır ekleme eğer eşleşmiyorsa yeni bir id oluştur ve satır ekle

            // eğer gelen event OrderStartedEvent ise CorrelateBy methodu ile  order state machinenin databesesindeki order state instacesindeki orderid ile gelen @event'deki orderid'sini kıyaslıyoruz. kıyas neticesinde eğer böyle bir instance varsa yeni bir sipariş olmadığını anlıyoruz kayıt etmiyoruz. eğer böyle bir instance yoksa SelectId ile yeni bir id oluşturuyoruz ve yeni satır oluşturuyoruz.
            Event(() => OrderStartedEvent, orderStateInstance =>
            orderStateInstance.CorrelateBy<int>(database =>
            database.OrderId, @event =>
            @event.Message.OrderId).SelectId(e =>
            Guid.NewGuid()));

            // tetikleyici event dışındaki tüm eventler taşıdıkları correlationid ile eşlelen veritabanındaki state instance'ı bulur ve onun üzerinde işlem yapar.

            Event(() => StockReservedEvent,
                orderStateInstance => orderStateInstance.CorrelateById(@event =>
                @event.Message.CorrelationId));


            Event(() => StockNotReservedEvent,
                orderStateInstance => orderStateInstance.CorrelateById(@event =>
                @event.Message.CorrelationId));


            Event(() => PaymentCompletedEvent,
                orderStateInstance => orderStateInstance.CorrelateById(@event =>
                @event.Message.CorrelationId));


            Event(() => PaymentFailedEvent,
                orderStateInstance => orderStateInstance.CorrelateById(@event =>
                @event.Message.CorrelationId));



            // tetikleyici event geldiğinde state machine'de ilk karşılayıcı state, initially fonksiyonu tarafından tanımlanmış olan initial olacaktır



            //When() gelen eventin hangi event olduğunu kontrol etmemizi sağlayan bir fonksiyondur.
            // Then() gelen eventin karşılandığı durumda yapılacak işlemleri belirlememizi sağlayan bir fonksiyondur. oluşturulacak olan state instancenin propertylerine tetikleyici evente gelen hangi propertylerin atanacağını belirleriz. ayrıca ara işlem olarakta birden fazla then() fonksiyonu kullanılabilir.

            // context.Instance => veritabanına katşılık gelir
            // context.Data => gelen eventin içeriğine karşılık gelir


            //TransitionTo() fonksyionu aracılığıyla InstanceState(instance => instance.CurrentState); burada belirttiğimiz statetiyi değiştiriyoruz

            //Send() fonksiyonu aracılığıyla apilere event gönderiyoruz.


            Initially(When(OrderStartedEvent)
                .Then(context =>
                {
                    context.Instance.OrderId = context.Data.OrderId;
                    context.Instance.BuyerId = context.Data.BuyerId;
                    context.Instance.TotalPrice = context.Data.TotalPrice;
                    context.Instance.CreatedDate = DateTime.UtcNow;
                })
                .TransitionTo(OrderCreated)
                .Send(new Uri($"queue:{RabbitMqSettings.Stock_Order_Created_Event_Queue}"),
                context => new OrderCreatedEvent(context.Instance.CorrelationId)
                {
                    OrderItems = context.Data.OrderItems
                }));



            //During() fonlsiyonu ile tetikleyici eventten sonraki durumlar kontrol edilir.


            During(OrderCreated,
                When(StockReservedEvent)
                .TransitionTo(StockReserved)
                .Send(new Uri($"queue:{RabbitMqSettings.Payment_Started_Event_Queue}"),
                context => new PaymentStartedEvent(context.Instance.CorrelationId)
                {
                    OrderItems = context.Data.OrderItems,
                    TotalPrice = context.Instance.TotalPrice
                }),
                When(StockNotReservedEvent)
                .TransitionTo(StockNotReserved)
                .Send(new Uri($"queue:{RabbitMqSettings.Order_Order_Failed_Event_Queue}"),
                context => new OrderFailedEvent
                {
                    OrderId = context.Instance.OrderId,
                    Message = context.Data.Message
                }));


            // finalize() fonksiyonu ile olayların başarıyla tamamlandığı belirlenir ve durum silinir state machine veri tabanından bu durum silinir.


            During(StockReserved,
                When(PaymentCompletedEvent)
                .TransitionTo(PaymentCompleted)
                .Send(new Uri($"queue:{RabbitMqSettings.Order_Order_Completed_Event_Queue}"),
                context => new OrderCompletedEvent
                {
                    OrderId = context.Instance.OrderId
                })
                .Finalize(),
                When(PaymentFailedEvent)
                .TransitionTo(PaymentFailed)
                .Send(new Uri($"queue:{RabbitMqSettings.Order_Order_Failed_Event_Queue}"), context =>
                new OrderFailedEvent()
                {
                    OrderId = context.Instance.OrderId,
                    Message = context.Data.Message
                })
                .Send(new Uri($"queue:{RabbitMqSettings.Stock_Rollback_Message_Queue}"), context =>
                new StockRollbackMessage()
                {
                    OrderItems = context.Data.OrderItems
                }));


            SetCompletedWhenFinalized();
        }


    }
}
