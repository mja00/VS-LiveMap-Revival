using System.Collections.Concurrent;

namespace livemap.util;

/// <summary>
///     Thread-safe LRU (Least Recently Used) cache implementation.
///     Provides O(1) get and put operations with automatic eviction when capacity is exceeded.
/// </summary>
public class LRUCache<TKey, TValue> where TKey : notnull {
    private readonly int _capacity;
    private readonly ConcurrentDictionary<TKey, LinkedListNode<CacheItem>> _cache;
    private readonly LinkedList<CacheItem> _accessOrder;
    private readonly object _lock = new();

    public LRUCache(int capacity) {
        if (capacity <= 0) {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than 0");
        }

        _capacity = capacity;
        _cache = new ConcurrentDictionary<TKey, LinkedListNode<CacheItem>>();
        _accessOrder = new LinkedList<CacheItem>();
    }

    /// <summary>
    ///     Gets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to get.</param>
    /// <param name="value">When this method returns, contains the value associated with the specified key, if found; otherwise, the default value.</param>
    /// <returns>true if the cache contains an element with the specified key; otherwise, false.</returns>
    public bool TryGet(TKey key, out TValue? value) {
        if (_cache.TryGetValue(key, out var node)) {
            lock (_lock) {
                // Move to front (most recently used)
                _accessOrder.Remove(node);
                _accessOrder.AddFirst(node);
            }

            value = node.Value.Value;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    ///     Adds or updates a value in the cache.
    /// </summary>
    /// <param name="key">The key of the value to add or update.</param>
    /// <param name="value">The value to add or update.</param>
    public void Add(TKey key, TValue value) {
        lock (_lock) {
            if (_cache.TryGetValue(key, out var existingNode)) {
                // Update existing
                existingNode.Value.Value = value;
                _accessOrder.Remove(existingNode);
                _accessOrder.AddFirst(existingNode);
            } else {
                // Add new
                if (_cache.Count >= _capacity) {
                    // Evict least recently used (last node)
                    var lastNode = _accessOrder.Last;
                    if (lastNode != null) {
                        _cache.TryRemove(lastNode.Value.Key, out _);
                        _accessOrder.RemoveLast();
                    }
                }

                var newNode = new LinkedListNode<CacheItem>(new CacheItem(key, value));
                _cache[key] = newNode;
                _accessOrder.AddFirst(newNode);
            }
        }
    }

    /// <summary>
    ///     Removes the value with the specified key from the cache.
    /// </summary>
    /// <param name="key">The key of the value to remove.</param>
    /// <returns>true if the element was successfully removed; otherwise, false.</returns>
    public bool Remove(TKey key) {
        lock (_lock) {
            if (_cache.TryRemove(key, out var node)) {
                _accessOrder.Remove(node);
                return true;
            }

            return false;
        }
    }

    /// <summary>
    ///     Removes all items from the cache.
    /// </summary>
    public void Clear() {
        lock (_lock) {
            _cache.Clear();
            _accessOrder.Clear();
        }
    }

    /// <summary>
    ///     Gets the number of items currently in the cache.
    /// </summary>
    public int Count => _cache.Count;

    private class CacheItem {
        public TKey Key { get; }
        public TValue Value { get; set; }

        public CacheItem(TKey key, TValue value) {
            Key = key;
            Value = value;
        }
    }
}
