# CompressedCache
general purpose compressed cache

`General purpose compressed cache` to store very large objects. You can store any object in key value format.
The value will be GZip compressed before adding to the cache.

Cache supports both sync and async ways to add objects

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
