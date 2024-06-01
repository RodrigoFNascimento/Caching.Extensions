using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Caching.Extensions
{
    public static class DistributedCacheExtensions
    {
        /// <summary>
        /// Gets the value associated with this key if present.
        /// </summary>
        /// <typeparam name="TItem">The type of the object to get.</typeparam>
        /// <param name="cache">The <see cref="IDistributedCache"/> instance this method extends.</param>
        /// <param name="key">The key of the value to get.</param>
        /// <returns>The value associated with this key, or <c>default(TItem)</c> if the key is not present.</returns>
        public static TItem Get<TItem>(
            this IDistributedCache cache,
            string key)
        {
            var cachedData = cache.Get(key);

            if (cachedData is null)
                return default;

            return Deserialize<TItem>(cachedData);
        }

        /// <summary>
        /// Asynchronously gets the value associated with this key if present.
        /// </summary>
        /// <typeparam name="TItem">The type of the object to get.</typeparam>
        /// <param name="cache">The <see cref="IDistributedCache"/> instance this method extends.</param>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="cancellationToken">Optional. The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// A task representing the asynchronous operation.
        /// The task result contains the value associated with this key,
        /// or <c>default(TItem)</c> if the key is not present.
        /// </returns>
        public static async Task<TItem> GetAsync<TItem>(
            this IDistributedCache cache,
            string key,
            CancellationToken cancellationToken = default)
        {
            var cachedData = await cache.GetAsync(key, cancellationToken);

            if (cachedData is null)
                return default;

            return Deserialize<TItem>(cachedData);
        }

        /// <summary>
        /// Asynchronously gets the value associated with this key if it exists,
        /// or generates a new entry using the provided key and a value from the given factory if the key is not found.
        /// </summary>
        /// <typeparam name="TItem">The type of the object to get.</typeparam>
        /// <param name="cache">The <see cref="IDistributedCache"/> instance this method extends.</param>
        /// <param name="key">The key of the entry to look for or create.</param>
        /// <param name="factory">The factory task that creates the value associated with this key if the key does not exist in the cache.</param>
        /// <param name="cancellationToken">Optional. The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public static async Task<TItem> GetOrCreateAsync<TItem>(
            this IDistributedCache cache,
            string key,
            Func<DistributedCacheEntryOptions, Task<TItem>> factory,
            CancellationToken cancellationToken = default)
        {
            var cachedItem = await cache.GetAsync(key, cancellationToken);

            if (cachedItem != null)
                return Deserialize<TItem>(cachedItem);

            var options = new DistributedCacheEntryOptions();
            var newItem = await factory(options).ConfigureAwait(false);

            if (newItem != null)
            {
                var serializedItem = Serialize(newItem);
                await cache.SetAsync(key, serializedItem, options, cancellationToken);
            }

            return newItem;
        }

        /// <summary>
        /// Associate a value with a key in the <see cref="IDistributedCache"/>.
        /// </summary>
        /// <typeparam name="TItem">The type of the object to set.</typeparam>
        /// <param name="cache">The <see cref="IDistributedCache"/> instance this method extends.</param>
        /// <param name="key">The key of the entry to add.</param>
        /// <param name="value">The value to associate with the key.</param>
        /// <param name="absoluteExpiration">The point in time at which the cache entry will expire.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static void Set<TItem>(
            this IDistributedCache cache,
            string key,
            TItem value,
            DateTimeOffset absoluteExpiration)
        {
            var serializedValue = Serialize(value);

            var options = new DistributedCacheEntryOptions()
            {
                AbsoluteExpiration = absoluteExpiration
            };

            cache.Set(key, serializedValue, options);
        }

        /// <summary>
        /// Associate a value with a key in the <see cref="IDistributedCache"/>.
        /// </summary>
        /// <typeparam name="TItem">The type of the object to set.</typeparam>
        /// <param name="cache">The <see cref="IDistributedCache"/> instance this method extends.</param>
        /// <param name="key">The key of the entry to add.</param>
        /// <param name="value">The value to associate with the key.</param>
        /// <param name="absoluteExpirationRelativeToNow">The duration from now after which the cache entry will expire.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static void Set<TItem>(
            this IDistributedCache cache,
            string key,
            TItem value,
            TimeSpan absoluteExpirationRelativeToNow)
        {
            var serializedValue = Serialize(value);

            var options = new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow
            };

            cache.Set(key, serializedValue, options);
        }

        /// <summary>
        /// Associate a value with a key in the <see cref="IDistributedCache"/>.
        /// </summary>
        /// <typeparam name="TItem">The type of the object to set.</typeparam>
        /// <param name="cache">The <see cref="IDistributedCache"/> instance this method extends.</param>
        /// <param name="key">The key of the entry to add.</param>
        /// <param name="value">The value to associate with the key.</param>
        /// <param name="options">The existing <see cref="DistributedCacheEntryOptions"/> instance to apply to the new entry.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static void Set<TItem>(
            this IDistributedCache cache,
            string key,
            TItem value,
            DistributedCacheEntryOptions options)
        {
            var serializedValue = Serialize(value);
            cache.Set(key, serializedValue, options);
        }

        /// <summary>
        /// Associate a value with a key in the <see cref="IDistributedCache"/>.
        /// </summary>
        /// <typeparam name="TItem">The type of the object to set.</typeparam>
        /// <param name="cache">The <see cref="IDistributedCache"/> instance this method extends.</param>
        /// <param name="key">The key of the entry to add.</param>
        /// <param name="value">The value to associate with the key.</param>
        /// <param name="absoluteExpiration">The point in time at which the cache entry will expire.</param>
        /// <param name="cancellationToken">
        /// Optional. The <see cref="CancellationToken"/>
        /// used to propagate notifications that the operation should be canceled.
        /// </param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static async Task SetAsync<TItem>(
            this IDistributedCache cache,
            string key,
            TItem value,
            DateTimeOffset absoluteExpiration,
            CancellationToken cancellationToken = default)
        {
            var serializedValue = Serialize(value);

            var options = new DistributedCacheEntryOptions()
            {
                AbsoluteExpiration = absoluteExpiration
            };

            await cache.SetAsync(key, serializedValue, options, cancellationToken);
        }

        /// <summary>
        /// Associate a value with a key in the <see cref="IDistributedCache"/>.
        /// </summary>
        /// <typeparam name="TItem">The type of the object to set.</typeparam>
        /// <param name="cache">The <see cref="IDistributedCache"/> instance this method extends.</param>
        /// <param name="key">The key of the entry to add.</param>
        /// <param name="value">The value to associate with the key.</param>
        /// <param name="absoluteExpirationRelativeToNow">The duration from now after which the cache entry will expire.</param>
        /// <param name="cancellationToken">
        /// Optional. The <see cref="CancellationToken"/>
        /// used to propagate notifications that the operation should be canceled.
        /// </param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static async Task SetAsync<TItem>(
            this IDistributedCache cache,
            string key,
            TItem value,
            TimeSpan absoluteExpirationRelativeToNow,
            CancellationToken cancellationToken = default)
        {
            var serializedValue = Serialize(value);

            var options = new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow
            };

            await cache.SetAsync(key, serializedValue, options, cancellationToken);
        }
        
        /// <summary>
        /// Associate a value with a key in the <see cref="IDistributedCache"/>.
        /// </summary>
        /// <typeparam name="TItem">The type of the object to set.</typeparam>
        /// <param name="cache">The <see cref="IDistributedCache"/> instance this method extends.</param>
        /// <param name="key">The key of the entry to add.</param>
        /// <param name="value">The value to associate with the key.</param>
        /// <param name="options">The existing <see cref="DistributedCacheEntryOptions"/> instance to apply to the new entry.</param>
        /// <param name="cancellationToken">
        /// Optional. The <see cref="CancellationToken"/>
        /// used to propagate notifications that the operation should be canceled.
        /// </param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static async Task SetAsync<TItem>(
            this IDistributedCache cache,
            string key,
            TItem value,
            DistributedCacheEntryOptions options,
            CancellationToken cancellationToken = default)
        {
            var serializedValue = Serialize(value);
            await cache.SetAsync(key, serializedValue, options, cancellationToken);
        }

        private static T Deserialize<T>(byte[] value)
        {
            var stringValue = Encoding.UTF8.GetString(value);
            return JsonSerializer.Deserialize<T>(stringValue);
        }

        private static byte[] Serialize<T>(T value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return JsonSerializer.SerializeToUtf8Bytes(value);
        }
    }
}
