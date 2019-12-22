namespace CompressedCache
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface to implement the cache.
    /// </summary>
    /// <typeparam name="TKey">Type of key</typeparam>
    /// <typeparam name="TValue">Type of value.</typeparam>
    public interface ICache<TKey, TValue>
    {
        /// <summary>
        /// Gets or adds value from cache for input key
        /// </summary>
        /// <param name="key">Input key</param>
        /// <param name="valueFactory">Input value factory</param>
        /// <returns>Value from cache.</returns>
        TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory);

        /// <summary>
        /// Gets or adds value from cache for input key
        /// </summary>
        /// <param name="key">Input key</param>
        /// <param name="valueFactory">Input value factory</param>
        /// <param name="expirationTime">Input expiration time</param>
        /// <returns>Value from cache.</returns>
        TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory, TimeSpan expirationTime);

        /// <summary>
        /// Gets or adds in an async manner
        /// </summary>
        /// <param name="key">Input key</param>
        /// <param name="valueFactory">Input value factory</param>
        /// <param name="expirationTime">expiration time</param>
        /// <returns>Value from cache</returns>
        Task<TValue> GetOrAddAsync(TKey key, Func<TKey, Task<TValue>> valueFactory, TimeSpan expirationTime);
    }
}
