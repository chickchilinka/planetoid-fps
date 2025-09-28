using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Base.PlayerData.Data;
using Base.PlayerData.Interfaces;

namespace Base.PlayerData.Services
{
    public class PlayerDataService
    {
        private readonly IPlayerDataProvider _provider;
        private readonly IPlayerDataCache _cache;
        private readonly IPlayerDataConsumersCollector _collector;
        private readonly ISerializer _serializer;

        public PlayerDataService(
            IPlayerDataProvider provider,
            IPlayerDataCache cache,
            IPlayerDataConsumersCollector collector,
            ISerializer serializer)
        {
            _provider = provider;
            _cache = cache;
            _collector = collector;
            _serializer = serializer;
        }

        public async UniTask<(bool Success, string Error)> TryLoad()
        {
            var consumers = _collector.CollectImplementations().ToArray();
            var keys = consumers.Select(c => c.ModuleName).ToArray();

            try
            {
                if (await _provider.IsAvailable())
                    return await LoadWithRemote(consumers, keys);

                return await LoadFromCacheOnly(consumers, keys);
            }
            catch (Exception exception)
            {
                return (false, exception.Message);
            }
        }

        public async UniTask<(bool Success, string Error)> TrySave()
        {
            var consumers = _collector.CollectImplementations().ToArray();
            var keys = consumers.Select(c => c.ModuleName).ToArray();
            var newData = BuildPayloads(consumers);

            try
            {
                if (await _provider.IsAvailable())
                    return await SaveWithRemote(keys, newData);

                return await SaveOffline(keys, newData);
            }
            catch (Exception exception)
            {
                return (false, exception.Message);
            }
        }

        private Dictionary<string, string> BuildPayloads(IEnumerable<IPlayerDataConsumer> consumers)
            => consumers.ToDictionary(consumer => consumer.ModuleName, consumer => consumer.GetSerializedData(_serializer));

        private async UniTask<(bool, string)> LoadWithRemote(IPlayerDataConsumer[] consumers, string[] keys)
        {
            var remoteVersions = await _provider.GetVersions(keys);
            var cachedModules = await _cache.Load(keys);
            var cacheVersions = await _cache.GetVersions(keys);

            var pendingReadyForUpload = cachedModules
                .Where(moduleDataPair => moduleDataPair.Value.IsPendingUpload
                               && remoteVersions.TryGetValue(moduleDataPair.Key, out var remoteVersion)
                               && moduleDataPair.Value.BaseVersion == remoteVersion)
                .Select(moduleDataPair => moduleDataPair.Key)
                .ToArray();

            if (pendingReadyForUpload.Length > 0)
                await UploadBatch(pendingReadyForUpload, cachedModules);

            var keysToFetchFromRemote = keys.Where(key =>
                    !remoteVersions.TryGetValue(key, out var remoteVersion)
                    || !cacheVersions.TryGetValue(key, out var cacheVersion)
                    || cacheVersion < remoteVersion
                    || (cachedModules.TryGetValue(key, out var cachedEntry) && cachedEntry.IsPendingUpload &&
                        cachedEntry.BaseVersion != remoteVersion))
                .ToArray();

            var result = new Dictionary<string, PlayerModuleData>();

            if (keysToFetchFromRemote.Length > 0)
            {
                var fetchedFromRemote = await _provider.Load(keysToFetchFromRemote);
                await _cache.Save(fetchedFromRemote.Values.ToArray());
                Merge(result, fetchedFromRemote);

                var missingFromRemote = keysToFetchFromRemote
                    .Where(key => !fetchedFromRemote.ContainsKey(key))
                    .ToArray();

                if (missingFromRemote.Length > 0)
                {
                    var dataFromCache = await _cache.Load(missingFromRemote);
                    Merge(result, dataFromCache);
                }
            }

            var keysFromCacheOnly = keys.Except(keysToFetchFromRemote).ToArray();
            if (keysFromCacheOnly.Length > 0)
                Merge(result, await _cache.Load(keysFromCacheOnly));

            Apply(consumers, result);
            return (true, null);
        }

        private async UniTask UploadBatch(string[] keys, IReadOnlyDictionary<string, PlayerModuleData> cacheData)
        {
            foreach (var key in keys)
            {
                var moduleData = cacheData[key];
                moduleData.RemoteVersion++;
                moduleData.IsPendingUpload = false;
                await _provider.Save(new[] { moduleData });
                await _cache.Save(new[] { moduleData });
            }
        }

        private static void Merge(
            IDictionary<string, PlayerModuleData> target,
            IReadOnlyDictionary<string, PlayerModuleData> source)
        {
            foreach (var keyValuePair in source) target[keyValuePair.Key] = keyValuePair.Value;
        }

        private void Apply(IEnumerable<IPlayerDataConsumer> consumers,
            IReadOnlyDictionary<string, PlayerModuleData> data)
        {
            foreach (var consumer in consumers)
                if (data.TryGetValue(consumer.ModuleName, out var moduleData))
                    consumer.SetData(moduleData.Payload, _serializer);
        }

        private async UniTask<(bool, string)> LoadFromCacheOnly(IPlayerDataConsumer[] consumers, string[] keys)
        {
            var localVersions = await _cache.GetVersions(keys);
            if (keys.Any(key => !localVersions.ContainsKey(key)))
                return (false, "Provider unavailable and cache incomplete");

            Apply(consumers, await _cache.Load(keys));
            return (true, null);
        }

        private async UniTask<(bool, string)> SaveWithRemote(string[] keys, Dictionary<string, string> newPayloads)
        {
            var remoteVersions = await _provider.GetVersions(keys);
            var cachedModules = await _cache.Load(keys);

            var changedKeys = keys
                .Where(key =>
                    !cachedModules.TryGetValue(key, out var cachedEntry) || cachedEntry.Payload != newPayloads[key])
                .ToArray();

            foreach (var key in changedKeys)
            {
                var baseVersion = remoteVersions.GetValueOrDefault(key, 0);
                var entry = cachedModules.TryGetValue(key, out var cachedEntry)
                    ? cachedEntry
                    : new PlayerModuleData { ModuleName = key };

                entry.Payload = newPayloads[key];
                entry.BaseVersion = baseVersion;
                entry.RemoteVersion++;
                entry.IsPendingUpload = false;

                await _cache.Save(new[] { entry });
                await _provider.Save(new[] { entry });
            }

            return (true, null);
        }

        private async UniTask<(bool, string)> SaveOffline(string[] keys, Dictionary<string, string> newPayloads)
        {
            var cachedModules = await _cache.Load(keys);

            var touchedKeys = keys
                .Where(key =>
                    !cachedModules.TryGetValue(key, out var cachedEntry) || cachedEntry.Payload != newPayloads[key])
                .ToArray();

            foreach (var key in touchedKeys)
            {
                var entry = cachedModules.TryGetValue(key, out var cachedEntry)
                    ? cachedEntry
                    : new PlayerModuleData { ModuleName = key };

                entry.Payload = newPayloads[key];
                entry.IsPendingUpload = true;
                entry.BaseVersion = entry.RemoteVersion;
                entry.RemoteVersion++;
                await _cache.Save(new[] { entry });
            }

            return (true, null);
        }
    }
}