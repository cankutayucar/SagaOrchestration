using MassTransit;
using MongoDB.Driver;
using Shared.Messages;
using Stock.API.Services;

namespace Stock.API.Consumers
{
    public class StockRollbackMessageConsumer(MongoDbService mongoDbService) : IConsumer<StockRollbackMessage>
    {
        public async Task Consume(ConsumeContext<StockRollbackMessage> context)
        {
            var collection = mongoDbService.GetCollection<Stock.API.Models.Stock>();
            foreach (var orderItem in context.Message.OrderItems)
            {
                var stock = await (await collection.FindAsync(s => s.ProductId == orderItem.ProductId)).FirstOrDefaultAsync();
                stock.Count += orderItem.Count;
                await collection.FindOneAndReplaceAsync(s => s.ProductId == orderItem.ProductId, stock);
            }
        }
    }
}
