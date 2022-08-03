using MongoDB.Driver;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Data.MongoDb;

public sealed class NamEcommerceMongoDbContext : IDbContext
{
    private readonly MongoClient _mongoClient;
    private readonly IMongoDatabase _database;

    public NamEcommerceMongoDbContext(string connectionString)
    {
        _mongoClient = new MongoClient(connectionString);
        _database = _mongoClient.GetDatabase(NamEcommerceMongoDataDefaults.DbName);
    }

    public IMongoDatabase Database => _database;

    public async Task<TEntity> AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : AppAggregateEntity
    {
        var entities = GetCollection<TEntity>();
        await entities.InsertOneAsync(entity, new() { BypassDocumentValidation = false }, cancellationToken)
            .ConfigureAwait(false);
        return entity;
    }

    public async Task<TEntity?> FindAsync<TEntity>(Guid key, CancellationToken cancellationToken = default) where TEntity : AppAggregateEntity
    {
        var entities = GetCollection<TEntity>();
        var result = await entities.FindAsync(FilterById<TEntity>(key), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return await result.SingleOrDefaultAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<TEntity>> GetDataAsync<TEntity>() where TEntity : AppAggregateEntity
    {
        var entities = GetCollection<TEntity>();
        var result = await entities.FindAsync(FilterDefinition<TEntity>.Empty).ConfigureAwait(false);
        return result.ToEnumerable();
    }

    public Task RemoveAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : AppAggregateEntity
    {
        var entities = GetCollection<TEntity>();
        return entities.DeleteOneAsync(FilterById(entity), cancellationToken);
    }

    public async Task<TEntity> UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : AppAggregateEntity
    {
        var entities = GetCollection<TEntity>();
        await entities.ReplaceOneAsync(FilterById(entity), entity, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return entity;
    }

    private IMongoCollection<TEntity> GetCollection<TEntity>() where TEntity : AppAggregateEntity
        => Database.GetCollection<TEntity>(typeof(TEntity).Name);
    private static FilterDefinition<TEntity> FilterById<TEntity>(TEntity entity)
        where TEntity : AppAggregateEntity
        => Builders<TEntity>.Filter.Eq(e => e.Id, entity.Id);
    private static FilterDefinition<TEntity> FilterById<TEntity>(Guid id)
        where TEntity : AppAggregateEntity
        => Builders<TEntity>.Filter.Eq(e => e.Id, id);
}
