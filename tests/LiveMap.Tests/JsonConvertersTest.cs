using livemap.data;
using livemap.json;
using Newtonsoft.Json;

namespace LiveMap.Tests;

/// <summary>
///     Tests for custom JSON converters to ensure proper null handling.
///     NOTE: TileTypeJsonConverter tests are omitted due to VintagestoryAPI dependency,
///     but the converter has been fixed to handle nulls properly (WriteJson now writes null explicitly).
/// </summary>
public class JsonConvertersTest {
    private readonly JsonSerializerSettings _settings = new() {
        Formatting = Formatting.None,
        NullValueHandling = NullValueHandling.Include
    };

    #region ColorJsonConverter Tests

    [Fact]
    public void ColorJsonConverter_SerializeNonNullValue_ShouldProduceHexString() {
        // Arrange
        Color color = new(0xFF5733);
        TestObject<Color?> obj = new() { Value = color };

        // Act
        string json = JsonConvert.SerializeObject(obj, _settings);

        // Assert
        Assert.Contains("\"#FF5733\"", json);
    }

    [Fact]
    public void ColorJsonConverter_SerializeNullValue_ShouldProduceNull() {
        // Arrange
        TestObject<Color?> obj = new() { Value = null };

        // Act
        string json = JsonConvert.SerializeObject(obj, _settings);

        // Assert
        Assert.Contains("null", json);
        Assert.DoesNotContain("None", json);
    }

    [Fact]
    public void ColorJsonConverter_DeserializeHexString_ShouldProduceColor() {
        // Arrange
        string json = "{\"value\":\"#FF5733\"}";

        // Act
        TestObject<Color?>? obj = JsonConvert.DeserializeObject<TestObject<Color?>>(json);

        // Assert
        Assert.NotNull(obj);
        Assert.NotNull(obj.Value);
        Assert.Equal((uint)0xFF5733, obj.Value.Value.ToUInt());
    }

    [Fact]
    public void ColorJsonConverter_DeserializeNull_ShouldProduceNull() {
        // Arrange
        string json = "{\"value\":null}";

        // Act
        TestObject<Color?>? obj = JsonConvert.DeserializeObject<TestObject<Color?>>(json);

        // Assert
        Assert.NotNull(obj);
        Assert.Null(obj.Value);
    }

    #endregion

    #region OpacityJsonConverter Tests

    [Fact]
    public void OpacityJsonConverter_SerializeNonNullValue_ShouldProduceDouble() {
        // Arrange
        Opacity opacity = new(0.5);
        TestObject<Opacity?> obj = new() { Value = opacity };

        // Act
        string json = JsonConvert.SerializeObject(obj, _settings);

        // Assert
        Assert.Contains("0.5", json);
    }

    [Fact]
    public void OpacityJsonConverter_SerializeNullValue_ShouldProduceNull() {
        // Arrange
        TestObject<Opacity?> obj = new() { Value = null };

        // Act
        string json = JsonConvert.SerializeObject(obj, _settings);

        // Assert
        Assert.Contains("null", json);
        Assert.DoesNotContain("None", json);
    }

    [Fact]
    public void OpacityJsonConverter_DeserializeDouble_ShouldProduceOpacity() {
        // Arrange
        string json = "{\"value\":0.8}";

        // Act
        TestObject<Opacity?>? obj = JsonConvert.DeserializeObject<TestObject<Opacity?>>(json);

        // Assert
        Assert.NotNull(obj);
        Assert.NotNull(obj.Value);
        Assert.Equal(0.8, obj.Value.Value.ToDouble());
    }

    [Fact]
    public void OpacityJsonConverter_DeserializeNull_ShouldProduceNull() {
        // Arrange
        string json = "{\"value\":null}";

        // Act
        TestObject<Opacity?>? obj = JsonConvert.DeserializeObject<TestObject<Opacity?>>(json);

        // Assert
        Assert.NotNull(obj);
        Assert.Null(obj.Value);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void ComplexObject_WithNullableProperties_ShouldSerializeWithoutNoneValues() {
        // Arrange
        ComplexTestObject obj = new() {
            Name = "Test",
            Color = null,
            Opacity = new Opacity(0.7)
        };

        // Act
        string json = JsonConvert.SerializeObject(obj, _settings);

        // Assert
        Assert.DoesNotContain("None", json);
        Assert.Contains("\"color\":null", json);
        Assert.Contains("\"opacity\":0.7", json);
    }

    [Fact]
    public void ComplexObject_WithAllNullProperties_ShouldSerializeOnlyWithNulls() {
        // Arrange
        ComplexTestObject obj = new() {
            Name = "Test",
            Color = null,
            Opacity = null
        };

        // Act
        string json = JsonConvert.SerializeObject(obj, _settings);

        // Assert
        Assert.DoesNotContain("None", json);
        int nullCount = json.Split("null").Length - 1;
        Assert.Equal(2, nullCount); // Two null properties
    }

    #endregion

    #region Test Helper Classes

    private class TestObject<T> {
        [JsonProperty("value")]
        public T? Value { get; set; }
    }

    private class ComplexTestObject {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("color")]
        public Color? Color { get; set; }

        [JsonProperty("opacity")]
        public Opacity? Opacity { get; set; }
    }

    #endregion
}
