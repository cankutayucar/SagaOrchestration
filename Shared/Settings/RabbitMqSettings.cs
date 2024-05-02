using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Settings
{
    public static class RabbitMqSettings
    {
        public const string StateMachine_Queue = "state-machine-queue";
        public const string Stock_Order_Created_Event_Queue = "stock-order-created-event-queue";
        public const string Order_Order_Completed_Event_Queue = "order-order-completed-event-queue";
        public const string Order_Order_Failed_Event_Queue = "order-order-failed-event-queue";
        public const string Stock_Rollback_Message_Queue = "stock-rollback-message-queue";
        public const string Payment_Started_Event_Queue = "payment-started-event-queue";
    }
}
