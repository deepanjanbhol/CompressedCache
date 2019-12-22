namespace CompressedCache
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Timers;

    /// <summary>
    /// cache.
    /// </summary>
    /// <typeparam name="TKey">Type of key</typeparam>
    /// <typeparam name="TValue">Type of value</typeparam>
    public class Cache<TKey, TValue> : IDisposable, ICache<TKey, TValue>
    {
        /// <summary>
        /// Default time to refresh for cache.
        /// </summary>
        protected readonly TimeSpan DefaultTimeToRefresh = TimeSpan.FromHours(1);

        /// <summary>
        /// Time to live in cache. after this they are evicted by Run eviction task.
        /// </summary>
        private readonly TimeSpan timeToLiveInCache;

        /// <summary>
        /// Name of the cache.
        /// </summary>
        private readonly string cacheName;

        /// <summary>
        /// Cache of all the items
        /// </summary>
        private ConcurrentDictionary<TKey, CacheEntry> cache;

        /// <summary>
        /// Concurrent queue containing all the keys that need to be refreshed.
        /// Processing task drains this queue at fixed interval to update the cache.
        /// </summary>
        private ConcurrentQueue<QueueItem> updateQueue = new ConcurrentQueue<QueueItem>();

        /// <summary>
        /// The clock
        /// </summary>
        private ISystemClock systemClock;

        /// <summary>
        /// The timer
        /// </summary>
        private Timer timer;

        /// <summary>
        /// Task that drains the update queue and updated all the values in cache.
        /// </summary>
        private Task processQueueTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableCache{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="name">Input cache name.</param>
        /// <param name="timetoLive">Input time to live.</param>
        /// <param name="clock">Input system clock.</param>
        public Cache(string name, TimeSpan timetoLive, ISystemClock clock)
        { 
            this.cacheName = name;
            this.timeToLiveInCache = timetoLive;
            this.cache = new ConcurrentDictionary<TKey, CacheEntry>();
            this.systemClock = clock;
            this.timer = new Timer(this.timeToLiveInCache.TotalMilliseconds);
            this.timer.Elapsed += (s, a) => this.RunEviction();
            this.timer.Enabled = true;
            this.processQueueTask = this.ProcessQueue();
        }

        /// <summary>
        /// Gets or adds value from cache for input key
        /// </summary>
        /// <param name="key">Input key</param>
        /// <param name="valueFactory">Input value factory</param>
        /// <returns>Value from cache.</returns>
        public virtual TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            return this.GetOrAdd(key, valueFactory, this.DefaultTimeToRefresh);
        }

        /// <summary>
        /// Gets or adds value from cache for input key
        /// </summary>
        /// <param name="key">Input key</param>
        /// <param name="valueFactory">Input value factory</param>
        /// <param name="timeToRefresh">Input time to refresh</param>
        /// <returns>Value from cache.</returns>
        public virtual TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory, TimeSpan timeToRefresh)
        {
            bool cacheHit = true;
            var item = this.cache.GetOrAdd(
                key,
                _ =>
                {
                    cacheHit = false;
                    return new CacheEntry(
                        () => { return valueFactory(_); },
                        this.systemClock.UtcNow + timeToRefresh,
                        this.systemClock.UtcNow);
                });

            if (item.ExpirationTime < this.systemClock.UtcNow)
            {
                this.cache[key] = new CacheEntry(
                    () => { return valueFactory(key); },
                    this.systemClock.UtcNow + timeToRefresh,
                    this.systemClock.UtcNow);
            }

            return item.CacheValue.Value;
        }

        /// <summary>
        /// Gets or adds in an async manner
        /// </summary>
        /// <param name="key">Input key</param>
        /// <param name="valueFactory">Input value factory</param>
        /// <param name="timeToRefresh">Time to refresh</param>
        /// <returns>Value from cache</returns>
        public virtual async Task<TValue> GetOrAddAsync(TKey key, Func<TKey, Task<TValue>> valueFactory, TimeSpan timeToRefresh)
        {
            bool cacheHit = false;
            TValue value = default(TValue);
            CacheEntry cacheEntry;

            if (this.cache.TryGetValue(key, out cacheEntry))
            {
                if (cacheEntry.ExpirationTime < this.systemClock.UtcNow)
                {
                    // Queue item for refresh.
                    this.QueueItemForRefresh(key, valueFactory, timeToRefresh);
                }

                cacheHit = true;
                value = cacheEntry.CacheValue.Value;
            }
            else
            {
                value = await valueFactory(key);
                this.cache[key] = new CacheEntry(
                    () => value,
                    this.systemClock.UtcNow + timeToRefresh,
                    this.systemClock.UtcNow);
            }

            return value;
        }

        /// <summary>
        /// Releases all cached resources.
        /// </summary>
        public void Dispose()
        {
            try
            {
                this.processQueueTask = null;
                this.timer.Dispose();
                this.cache.Clear();
                this.cache = null;
                this.updateQueue = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cache: {this.cacheName} dispose failed with exception: {ex}");
            }
        }

        /// <summary>
        /// Queue item for refresh in cache.
        /// </summary>
        /// <param name="key">Input key</param>
        /// <param name="valueFactory">Input value factory</param>
        /// <param name="timeToRefresh">Input time to refresh.</param>
        private void QueueItemForRefresh(TKey key, Func<TKey, Task<TValue>> valueFactory, TimeSpan timeToRefresh)
        {
            try
            {
                this.updateQueue.Enqueue(new QueueItem { Key = key, ValueFactory = valueFactory, TimeToRefresh = timeToRefresh });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Queuing Item with key: {key} for refresh failed for cache name: {this.cacheName} with exception: {ex}");
            }
        }

        /// <summary>
        /// Run the eviction of entries in cache.
        /// </summary>
        private void RunEviction()
        {
            try
            {
                var now = this.systemClock.UtcNow;

                Console.WriteLine($"Cache size for cache name: {this.cacheName} is : {this.cache.Count}");

                // Get the cache entries which have not been touched in past 'time to live' time span.
                var keysExpired = this.cache.Where(t => t.Value.LastUpdateTime.Add(this.timeToLiveInCache) < now).Select(kvp => kvp.Key);
                foreach (var key in keysExpired)
                {
                    CacheEntry evictedEntry;
                    bool removalResult = this.cache.TryRemove(key, out evictedEntry);

                    Console.WriteLine($"Run Eviction: removed {key} from cache: {this.cacheName} with result: {removalResult}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Run Eviction failed for cache name: {this.cacheName} failed with exception: {ex}");
            }
        }

        /// <summary>
        /// Create task for processing the queue.
        /// </summary>
        /// <returns>Corresponding task</returns>
        private Task ProcessQueue()
        {
            var task = Task.Factory.StartNew(
            async () =>
            {
                while (true)
                {
                    while (!updateQueue.IsEmpty)
                    {
                        QueueItem queueItem;
                        if (updateQueue.TryDequeue(out queueItem))
                        {
                            try
                            {
                                var value = await queueItem.ValueFactory(queueItem.Key);
                                this.cache[queueItem.Key] = new CacheEntry(
                                    () => value,
                                    this.systemClock.UtcNow + queueItem.TimeToRefresh,
                                    this.systemClock.UtcNow);

                                Console.WriteLine($"ProcessQueue:: Successfully updated cache for Key: {queueItem.Key}, expiration time: {queueItem.TimeToRefresh}");
                            }
                            catch (Exception ex)
                            {
                                // TODO:: enqueue in the queue again ? 
                                // What about the case of poison pill in that case.
                                Console.WriteLine($"ProcessQueue:: Fetching value for Key: {queueItem.Key} failed with exception: {ex}");
                            }
                        }
                        else
                        {
                            // Log that couldn't dequeue
                            Console.WriteLine("ProcessQueue couldn't dequeue from the queue.");
                        }
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            },
            TaskCreationOptions.LongRunning);

            return task;
        }

        /// <summary>
        /// Queue item for update queue.
        /// </summary>
        public class QueueItem
        {
            /// <summary>
            /// Gets or sets Input key that needs to be refreshed
            /// </summary>
            public TKey Key { get; set; }

            /// <summary>
            /// Gets or sets Method to fetch the latest value.
            /// </summary>
            public Func<TKey, Task<TValue>> ValueFactory { get; set; }

            /// <summary>
            /// Gets or sets time to refresh.
            /// </summary>
            public TimeSpan TimeToRefresh { get; set; }
        }

        /// <summary>
        /// Cache entry in the cache
        /// </summary>
        public class CacheEntry
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="CacheEntry"/> class.
            /// </summary>
            /// <param name="valueFactory">Input value factory</param>
            /// <param name="expirationTime">Input expiration time.</param>
            /// <param name="currentTime">Input current time.</param>
            public CacheEntry(Func<TValue> valueFactory, DateTime expirationTime, DateTime currentTime)
            {
                this.CacheValue = new Lazy<TValue>(() => valueFactory());
                this.ExpirationTime = expirationTime;
                this.LastUpdateTime = currentTime;
            }

            /// <summary>
            /// Gets or sets Cached value.
            /// </summary>
            public Lazy<TValue> CacheValue { get; set; }

            /// <summary>
            /// Gets or sets Expiration time.
            /// </summary>
            public DateTime ExpirationTime { get; set; }

            /// <summary>
            /// Gets or sets time in which value has been updated/added last
            /// </summary>
            public DateTime LastUpdateTime { get; set; }
        }
    }
}
