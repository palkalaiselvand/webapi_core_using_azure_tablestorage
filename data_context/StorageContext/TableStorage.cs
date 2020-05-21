using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using models_and_validators.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace data_context.StorageContext
{
    public interface ITableStorage
    {
        Task<List<T>> GetAll<T>(TableStorageEntityType tableName, string partitionKey) where T : ITableEntity, new();
        Task<T> Get<T>(TableStorageEntityType tableName, TableQuery<T> query) where T : ITableEntity, new();
        Task Insert<T>(TableStorageEntityType tableName, T entity) where T : ITableEntity, new();
        Task Update<T>(TableStorageEntityType tableName, T entity) where T : ITableEntity, new();
        Task Upsert<T>(TableStorageEntityType tableName, T entity) where T : ITableEntity, new();
        Task Delete<T>(TableStorageEntityType tableName, T entity) where T : ITableEntity, new();
    }

    public class TableStorage : ITableStorage
    {
        private readonly IConfiguration _configuration;
        public TableStorage(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<T> Get<T>(TableStorageEntityType tableName, TableQuery<T> query) where T : ITableEntity, new()
        {
            try
            {
                CloudTable table = GetCloudTable(tableName);

                query.TakeCount = 1;

                var querySegment = await table.ExecuteQuerySegmentedAsync(query, null);

                return querySegment.Results.FirstOrDefault();
            }
            catch (StorageException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<List<T>> GetAll<T>(TableStorageEntityType tableName, string partitionKey) where T : ITableEntity, new()
        {
            var retVal = new List<T>();

            try
            {
                CloudTable table = GetCloudTable(tableName);

                TableQuery<T> query = new TableQuery<T>();
                query.Where(TableQuery.GenerateFilterCondition(nameof(ITableEntity.PartitionKey), QueryComparisons.Equal, partitionKey));

                TableContinuationToken token = null;
                do
                {
                    var results = await table.ExecuteQuerySegmentedAsync(query, token);
                    retVal.AddRange(results.Results);
                    token = results.ContinuationToken;
                } while (token != null);
            }
            catch (StorageException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return retVal;
        }

        public async Task Insert<T>(TableStorageEntityType tableName, T entity) where T : ITableEntity, new()
        {
            await Execute(tableName, TableOperation.Insert(entity));
        }

        public async Task Update<T>(TableStorageEntityType tableName, T entity) where T : ITableEntity, new()
        {
            await Execute(tableName, TableOperation.Replace(entity));
        }

        public async Task Upsert<T>(TableStorageEntityType tableName, T entity) where T : ITableEntity, new()
        {
            await Execute(tableName, TableOperation.InsertOrReplace(entity));
        }

        public async Task Delete<T>(TableStorageEntityType tableName, T entity) where T : ITableEntity, new()
        {
            await Execute(tableName, TableOperation.Delete(entity));
        }

        protected async Task<TableResult> Execute(TableStorageEntityType tableName, TableOperation operation)
        {
            TableResult result = null;
            try
            {
                CloudTable table = GetCloudTable(tableName);
                try
                {
                    result = await table.ExecuteAsync(operation);

                }
                catch (StorageException ex)
                {
                    if (ex.RequestInformation?.HttpStatusCode == 404 && (await table.CreateIfNotExistsAsync()))
                    {
                        result = await table.ExecuteAsync(operation);
                    }
                    else
                    {
                        throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }

        protected virtual CloudTable GetCloudTable(TableStorageEntityType tableName)
        {
            var storageAccount = CloudStorageAccount.Parse(_configuration.GetConnectionString("storage_connectionstring"));
            var tableClient = storageAccount.CreateCloudTableClient();

            CloudTable table = tableClient.GetTableReference(tableName.ToString().ToLower());
            return table;
        }
    }

}
