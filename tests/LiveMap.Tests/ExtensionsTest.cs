using System.Collections.Generic;
using livemap.util;
using Xunit;

namespace LiveMap.Tests
{
    public class ExtensionsTest
    {
        [Fact]
        public void AddIfNotExists_ShouldAddOnlyIfNew()
        {
            List<string> list = new List<string> { "a", "b" };

            list.AddIfNotExists("c");
            Assert.Equal(3, list.Count);
            Assert.Contains("c", list);

            list.AddIfNotExists("b");
            Assert.Equal(3, list.Count); // Should still be 3
        }

        // DeepCopy requires BaseOptions or T constraint matching object options.
        // Let's mock a simple class IF BaseOptions is accessible, or just skipping for now if too coupled.
        // Looking at codebase, BaseOptions is likely in `livemap.layer.marker.options`.
        // Let's check if we can reference it easily.
    }
}
