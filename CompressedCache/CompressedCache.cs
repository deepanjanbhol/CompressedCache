
namespace CompressedCache
{
    using System;
    using System.Threading.Tasks;

    using Newtonsoft.Json;

    /// <summary>
    /// Compressed cache using GZIP.
    /// Keeps the values in compressed format. While serving requests, decompresses and returns output.
    /// </summary>
    /// <typeparam name="TKey">Type of Input</typeparam>
    /// <typeparam name="TValue">Type of value.</typeparam>
    public class CompressedCache<TKey, TValue> : Cache<TKey, byte[]>, ICache<TKey, TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompressedCache{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="name">Input cache name.</param>
        /// <param name="timetoLive">Input time to live.</param>
        /// <param name="clock">Input clock.</param>
        public CompressedCache(string name, TimeSpan timetoLive, ISystemClock clock) : base(name, timetoLive, clock)
        {
        }

        /// <summary>
        /// Get or add from cache.
        /// </summary>
        /// <param name="key">Input key</param>
        /// <param name="valueFactory">Input value factory</param>
        /// <returns>Value from cache</returns>
        public virtual TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            return this.GetOrAdd(key, valueFactory, this.DefaultTimeToRefresh);
        }

        /// <summary>
        /// Get or add from cache.
        /// </summary>
        /// <param name="key">Input key</param>
        /// <param name="valueFactory">Input value factory</param>
        /// <param name="expirationTime">Input expiration time span</param>
        /// <returns>Value from cache</returns>
        public virtual TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory, TimeSpan expirationTime)
        {
            var output = base.GetOrAdd(key, (k) => GzipCompression.Compress(JsonConvert.SerializeObject(valueFactory(k))), expirationTime);
            return JsonConvert.DeserializeObject<TValue>(GzipCompression.Decompress(output));
        }

        /// <summary>
        /// Get or add in an async manner.
        /// </summary>
        /// <param name="key">Input key</param>
        /// <param name="valueFactory">Input value factory</param>
        /// <param name="expirationTime">Input expiration time span.</param>
        /// <returns>Value from cache.</returns>
        public virtual async Task<TValue> GetOrAddAsync(TKey key, Func<TKey, Task<TValue>> valueFactory, TimeSpan expirationTime)
        {
            var output = await base.GetOrAddAsync(key, (k) => this.GetCompressedTask(k, valueFactory), expirationTime);
            return JsonConvert.DeserializeObject<TValue>(GzipCompression.Decompress(output));
        }

        /// <summary>
        /// Get compressed value task
        /// </summary>
        /// <param name="key">input key</param>
        /// <param name="valueFactory">Input value factory task.</param>
        /// <returns>Task returning compressed value.</returns>
        private Task<byte[]> GetCompressedTask(TKey key, Func<TKey, Task<TValue>> valueFactory)
        {
            var fetchvalueTask = valueFactory(key);
            var compressTask = fetchvalueTask.ContinueWith(t => GzipCompression.Compress(JsonConvert.SerializeObject(t.Result)));
            return compressTask;
        }
    }
}
