using livemap.util;

namespace LiveMap.Tests;

public class LRUCacheTest {
    [Fact]
    public void Constructor_WithValidCapacity_ShouldCreateCache() {
        var cache = new LRUCache<string, int>(10);
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void Constructor_WithZeroCapacity_ShouldThrow() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LRUCache<string, int>(0));
    }

    [Fact]
    public void Constructor_WithNegativeCapacity_ShouldThrow() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LRUCache<string, int>(-1));
    }

    [Fact]
    public void Add_SingleItem_ShouldIncreaseCount() {
        var cache = new LRUCache<string, int>(10);
        cache.Add("key1", 100);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void TryGet_ExistingKey_ShouldReturnTrueAndValue() {
        var cache = new LRUCache<string, int>(10);
        cache.Add("key1", 100);

        bool found = cache.TryGet("key1", out int value);

        Assert.True(found);
        Assert.Equal(100, value);
    }

    [Fact]
    public void TryGet_NonExistingKey_ShouldReturnFalseAndDefault() {
        var cache = new LRUCache<string, int>(10);

        bool found = cache.TryGet("nonexistent", out int value);

        Assert.False(found);
        Assert.Equal(0, value);
    }

    [Fact]
    public void TryGet_WithNullableValue_NonExistingKey_ShouldReturnNull() {
        var cache = new LRUCache<string, string?>(10);

        bool found = cache.TryGet("nonexistent", out string? value);

        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public void Add_ExistingKey_ShouldUpdateValue() {
        var cache = new LRUCache<string, int>(10);
        cache.Add("key1", 100);
        cache.Add("key1", 200);

        cache.TryGet("key1", out int value);

        Assert.Equal(200, value);
        Assert.Equal(1, cache.Count); // Should not duplicate
    }

    [Fact]
    public void Add_ExceedingCapacity_ShouldEvictLeastRecentlyUsed() {
        var cache = new LRUCache<string, int>(3);
        cache.Add("key1", 1);
        cache.Add("key2", 2);
        cache.Add("key3", 3);
        cache.Add("key4", 4); // Should evict key1

        Assert.Equal(3, cache.Count);
        Assert.False(cache.TryGet("key1", out _));
        Assert.True(cache.TryGet("key2", out _));
        Assert.True(cache.TryGet("key3", out _));
        Assert.True(cache.TryGet("key4", out _));
    }

    [Fact]
    public void TryGet_UpdatesLRUOrder() {
        var cache = new LRUCache<string, int>(3);
        cache.Add("key1", 1);
        cache.Add("key2", 2);
        cache.Add("key3", 3);

        // Access key1, making it most recently used
        cache.TryGet("key1", out _);

        // Add key4, should evict key2 (now least recently used)
        cache.Add("key4", 4);

        Assert.True(cache.TryGet("key1", out _));
        Assert.False(cache.TryGet("key2", out _)); // Evicted
        Assert.True(cache.TryGet("key3", out _));
        Assert.True(cache.TryGet("key4", out _));
    }

    [Fact]
    public void Add_UpdateExisting_ShouldUpdateLRUOrder() {
        var cache = new LRUCache<string, int>(3);
        cache.Add("key1", 1);
        cache.Add("key2", 2);
        cache.Add("key3", 3);

        // Update key1, making it most recently used
        cache.Add("key1", 10);

        // Add key4, should evict key2 (now least recently used)
        cache.Add("key4", 4);

        Assert.True(cache.TryGet("key1", out int value));
        Assert.Equal(10, value);
        Assert.False(cache.TryGet("key2", out _)); // Evicted
        Assert.True(cache.TryGet("key3", out _));
        Assert.True(cache.TryGet("key4", out _));
    }

    [Fact]
    public void Remove_ExistingKey_ShouldReturnTrueAndDecreaseCount() {
        var cache = new LRUCache<string, int>(10);
        cache.Add("key1", 100);

        bool removed = cache.Remove("key1");

        Assert.True(removed);
        Assert.Equal(0, cache.Count);
        Assert.False(cache.TryGet("key1", out _));
    }

    [Fact]
    public void Remove_NonExistingKey_ShouldReturnFalse() {
        var cache = new LRUCache<string, int>(10);

        bool removed = cache.Remove("nonexistent");

        Assert.False(removed);
    }

    [Fact]
    public void Clear_ShouldRemoveAllItems() {
        var cache = new LRUCache<string, int>(10);
        cache.Add("key1", 1);
        cache.Add("key2", 2);
        cache.Add("key3", 3);

        cache.Clear();

        Assert.Equal(0, cache.Count);
        Assert.False(cache.TryGet("key1", out _));
        Assert.False(cache.TryGet("key2", out _));
        Assert.False(cache.TryGet("key3", out _));
    }

    [Fact]
    public void Clear_EmptyCache_ShouldNotThrow() {
        var cache = new LRUCache<string, int>(10);
        cache.Clear();
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public async Task ConcurrentAdd_ShouldHandleThreadSafely() {
        var cache = new LRUCache<int, int>(1000);
        var tasks = new List<Task>();

        // 10 threads each adding 100 items
        for (int t = 0; t < 10; t++) {
            int threadId = t;
            tasks.Add(Task.Run(() => {
                for (int i = 0; i < 100; i++) {
                    int key = threadId * 100 + i;
                    cache.Add(key, key);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Cache capacity is 1000, so all items should fit
        Assert.Equal(1000, cache.Count);
    }

    [Fact]
    public async Task ConcurrentReadWrite_ShouldHandleThreadSafely() {
        var cache = new LRUCache<int, int>(100);

        // Pre-populate cache
        for (int i = 0; i < 100; i++) {
            cache.Add(i, i * 10);
        }

        var tasks = new List<Task>();
        var exceptions = new List<Exception>();

        // 5 reader threads
        for (int t = 0; t < 5; t++) {
            tasks.Add(Task.Run(() => {
                try {
                    for (int i = 0; i < 1000; i++) {
                        cache.TryGet(i % 100, out _);
                    }
                } catch (Exception ex) {
                    lock (exceptions) {
                        exceptions.Add(ex);
                    }
                }
            }));
        }

        // 5 writer threads
        for (int t = 0; t < 5; t++) {
            tasks.Add(Task.Run(() => {
                try {
                    for (int i = 0; i < 1000; i++) {
                        cache.Add(i % 100, i);
                    }
                } catch (Exception ex) {
                    lock (exceptions) {
                        exceptions.Add(ex);
                    }
                }
            }));
        }

        await Task.WhenAll(tasks);

        Assert.Empty(exceptions);
        Assert.Equal(100, cache.Count);
    }

    [Fact]
    public async Task ConcurrentEviction_ShouldNotThrow() {
        var cache = new LRUCache<int, int>(10);
        var tasks = new List<Task>();
        var exceptions = new List<Exception>();

        // Multiple threads trying to add items that will cause evictions
        for (int t = 0; t < 10; t++) {
            int threadId = t;
            tasks.Add(Task.Run(() => {
                try {
                    for (int i = 0; i < 100; i++) {
                        cache.Add(threadId * 100 + i, i);
                    }
                } catch (Exception ex) {
                    lock (exceptions) {
                        exceptions.Add(ex);
                    }
                }
            }));
        }

        await Task.WhenAll(tasks);

        Assert.Empty(exceptions);
        Assert.Equal(10, cache.Count); // Should maintain capacity
    }

    [Fact]
    public async Task ConcurrentMixedOperations_ShouldMaintainConsistency() {
        var cache = new LRUCache<int, int>(50);
        var tasks = new List<Task>();
        var exceptions = new List<Exception>();

        // Mix of add, get, remove operations
        for (int t = 0; t < 10; t++) {
            int threadId = t;
            tasks.Add(Task.Run(() => {
                try {
                    for (int i = 0; i < 100; i++) {
                        int key = i % 50;
                        switch (i % 3) {
                            case 0:
                                cache.Add(key, threadId);
                                break;
                            case 1:
                                cache.TryGet(key, out _);
                                break;
                            case 2:
                                cache.Remove(key);
                                break;
                        }
                    }
                } catch (Exception ex) {
                    lock (exceptions) {
                        exceptions.Add(ex);
                    }
                }
            }));
        }

        await Task.WhenAll(tasks);

        Assert.Empty(exceptions);
        Assert.True(cache.Count <= 50); // Should not exceed capacity
    }

    [Fact]
    public async Task StressTest_RapidEvictions_ShouldMaintainIntegrity() {
        var cache = new LRUCache<int, string>(5);
        var exceptions = new List<Exception>();

        var task = Task.Run(() => {
            try {
                // Rapidly add items to force continuous eviction
                for (int i = 0; i < 10000; i++) {
                    cache.Add(i, $"value_{i}");

                    // Verify count never exceeds capacity
                    if (cache.Count > 5) {
                        throw new InvalidOperationException($"Cache exceeded capacity: {cache.Count}");
                    }
                }
            } catch (Exception ex) {
                lock (exceptions) {
                    exceptions.Add(ex);
                }
            }
        });

        await task;

        Assert.Empty(exceptions);
        Assert.Equal(5, cache.Count);
    }

    [Fact]
    public void CapacityOne_ShouldWorkCorrectly() {
        var cache = new LRUCache<string, int>(1);

        cache.Add("key1", 1);
        Assert.Equal(1, cache.Count);

        cache.Add("key2", 2);
        Assert.Equal(1, cache.Count);
        Assert.False(cache.TryGet("key1", out _));
        Assert.True(cache.TryGet("key2", out int value));
        Assert.Equal(2, value);
    }

    [Fact]
    public void IntKeys_ShouldWorkCorrectly() {
        var cache = new LRUCache<int, string>(10);
        cache.Add(1, "one");
        cache.Add(2, "two");

        Assert.True(cache.TryGet(1, out string? value1));
        Assert.Equal("one", value1);
        Assert.True(cache.TryGet(2, out string? value2));
        Assert.Equal("two", value2);
    }

    [Fact]
    public void NullValues_ShouldBeAllowed() {
        var cache = new LRUCache<string, string?>(10);
        cache.Add("key1", null);

        bool found = cache.TryGet("key1", out string? value);

        Assert.True(found);
        Assert.Null(value);
    }
}
