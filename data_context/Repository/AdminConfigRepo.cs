using data_context.StorageContext;
using Microsoft.WindowsAzure.Storage.Table;
using models_and_validators.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using models_and_validators;
using models_and_validators.Common;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;

namespace data_context.Repository
{
    public interface IAdminConfigRepo
    {
        Task<List<AdminConfigResponse>> GetAll();
        Task<AdminConfigResponse> Get(string configName);
        Task<AdminConfigResponse> Insert(AdminConfigResponse entity);
        Task<AdminConfigResponse> Update(AdminConfigResponse entity);
        Task<AdminConfigResponse> Upsert(AdminConfigResponse entity);
        Task Delete(string configName);
    }

    public class AdminConfigRepo : IAdminConfigRepo
    {
        private readonly ITableStorage _storage;
        private readonly IMemoryCache _cache;
        public AdminConfigRepo(ITableStorage storage, IMemoryCache cache)
        {
            _storage = storage;
            _cache = cache;
        }
        public async Task<AdminConfigResponse> Get(string configName)
        {
            var response = await this.GetAll();

            return response.FirstOrDefault(x => x.FlagName == configName);

        }

        public async Task<List<AdminConfigResponse>> GetAll()
        {
            var configs = await _storage.GetAll<AdminConfiguration>(TableStorageEntityType.AdminConfiguration, PartitionKey.AdminConfiguration);
            var response = new List<AdminConfigResponse>();
            foreach (var config in configs)
            {
                var configuration = new AdminConfigResponse
                {
                    ConfigId = new Guid(config.ConfigId),
                    FlagName = config.FlagName,
                    FlagDescription = config.FlagDescription,
                    FlagValue = (Allow)Enum.Parse(typeof(Allow), config.FlagValue, true),
                };

                if ((Allow)Enum.Parse(typeof(Allow), config.FlagValue, true) == Allow.AllOff)
                {
                    var allowdedUsers = await _storage.GetAll<AllowExtenedUser>(TableStorageEntityType.AllowExtenedUser, configuration.ConfigId.ToString());
                    var user = allowdedUsers?.SelectMany(e => e.UserId.Split(',')).Select(s => s).ToList() ?? new List<string>();
                    configuration.UserId = user;
                }
                response.Add(configuration);
            }
            return response;
        }

        public async Task<AdminConfigResponse> Insert(AdminConfigResponse entity)
        {
            string Id = Guid.NewGuid().ToString();
            var adminConfig = new AdminConfiguration
            {
                RowKey = Id,
                PartitionKey = PartitionKey.AdminConfiguration,
                ConfigId = Id,
                FlagName = entity.FlagName,
                FlagDescription = entity.FlagDescription,
                FlagValue = entity.FlagValue.ToString(),
            };

            await _storage.Insert<AdminConfiguration>(TableStorageEntityType.AdminConfiguration, adminConfig);

            if (entity.FlagValue == Allow.AllOff && entity.UserId != null)
            {
                var chunkedList = this.ChunkBy(entity.UserId, 100).Select(e => string.Join(",", e)).ToList();

                foreach (var chunk in chunkedList)
                {
                    var user = new AllowExtenedUser
                    {
                        RowKey = Guid.NewGuid().ToString(),
                        PartitionKey = Id,
                        UserId = chunk
                    };
                    await _storage.Insert<AllowExtenedUser>(TableStorageEntityType.AllowExtenedUser, user);
                }
            }
            entity.ConfigId = new Guid(Id);
            return entity;
        }

        public async Task<AdminConfigResponse> Update(AdminConfigResponse entity)
        {
            var adminConfig = new AdminConfiguration
            {
                RowKey = entity.ConfigId.ToString(),
                PartitionKey = PartitionKey.AdminConfiguration,
                ConfigId = entity.ConfigId.ToString(),
                FlagName = entity.FlagName,
                FlagDescription = entity.FlagDescription,
                FlagValue = entity.FlagValue.ToString(),
            };
            await _storage.Update<AdminConfiguration>(TableStorageEntityType.AdminConfiguration, adminConfig);

            var existingData = await _storage.GetAll<AllowExtenedUser>(TableStorageEntityType.AllowExtenedUser, entity.ConfigId.ToString());

            foreach (var data in existingData)
            {
                await _storage.Delete<AllowExtenedUser>(TableStorageEntityType.AllowExtenedUser, data);
            }

            if (entity.FlagValue == Allow.AllOff && entity.UserId != null)
            {
                var chunkedList = this.ChunkBy(entity.UserId, 100).Select(e => string.Join(",", e)).ToList();

                foreach (var chunk in chunkedList)
                {
                    var user = new AllowExtenedUser
                    {
                        RowKey = Guid.NewGuid().ToString(),
                        PartitionKey = entity.ConfigId.ToString(),
                        UserId = chunk
                    };
                    await _storage.Update<AllowExtenedUser>(TableStorageEntityType.AllowExtenedUser, user);
                }
            }
            return entity;
        }

        public async Task<AdminConfigResponse> Upsert(AdminConfigResponse entity)
        {
            var adminConfig = new AdminConfiguration
            {
                RowKey = entity.ConfigId.ToString(),
                PartitionKey = PartitionKey.AdminConfiguration,
                ConfigId = entity.ConfigId.ToString(),
                FlagName = entity.FlagName,
                FlagDescription = entity.FlagDescription,
                FlagValue = entity.FlagValue.ToString(),
            };

            await _storage.Upsert<AdminConfiguration>(TableStorageEntityType.AdminConfiguration, adminConfig);

            var existingData = await _storage.GetAll<AllowExtenedUser>(TableStorageEntityType.AllowExtenedUser, entity.ConfigId.ToString());

            foreach (var data in existingData)
            {
                await _storage.Delete<AllowExtenedUser>(TableStorageEntityType.AllowExtenedUser, data);
            }

            if (entity.FlagValue == Allow.AllOff && entity.UserId != null)
            {
                var chunkedList = this.ChunkBy(entity.UserId, 100).Select(e => string.Join(",", e)).ToList();

                foreach (var chunk in chunkedList)
                {
                    var user = new AllowExtenedUser
                    {
                        RowKey = Guid.NewGuid().ToString(),
                        PartitionKey = entity.ConfigId.ToString(),
                        UserId = chunk
                    };
                    await _storage.Upsert<AllowExtenedUser>(TableStorageEntityType.AllowExtenedUser, user);
                }
            }
            return entity;
        }

        public async Task Delete(string configName)
        {
            var configuration = await _storage.GetAll<AdminConfiguration>(TableStorageEntityType.AdminConfiguration, PartitionKey.AdminConfiguration);

            var config = configuration.FirstOrDefault(x => x.FlagName == configName);
          
            await _storage.Delete<AdminConfiguration>(TableStorageEntityType.AdminConfiguration, config);

            var allowedUsers = await _storage.GetAll<AllowExtenedUser>(TableStorageEntityType.AllowExtenedUser, config.ConfigId.ToString());

            foreach (var user in allowedUsers)
            {
                await _storage.Delete<AllowExtenedUser>(TableStorageEntityType.AllowExtenedUser, user);
            }

        }

        private IEnumerable<List<T>> ChunkBy<T>(List<T> items, int chunkSize)
        {
            items = items ?? new List<T>(0);

            for (int i = 0; i < items.Count; i += chunkSize)
            {
                yield return items.GetRange(i, Math.Min(chunkSize, items.Count - i));
            }
        }
    }

    public class AdminConfigResponse
    {
        public Guid ConfigId { get; set; }
        public string FlagName { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public Allow FlagValue { get; set; }
        public string FlagDescription { get; set; }
        public List<string> UserId { get; set; }
    }
}
