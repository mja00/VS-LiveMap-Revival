using System.IO;
using System.Reflection;
using System.Text;
using livemap.httpd;

namespace LiveMap.Tests;

/// <summary>
///     Tests for the WebServer file cache implementation.
///     Tests cache behavior including CachedResource functionality and content type detection.
/// </summary>
public class WebServerCacheTest : IDisposable {
    private readonly string _testWebDir;

    public WebServerCacheTest() {
        // Create a temporary directory for test files
        _testWebDir = Path.Combine(Path.GetTempPath(), $"livemap-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testWebDir);
    }

    public void Dispose() {
        // Clean up test directory
        if (Directory.Exists(_testWebDir)) {
            try {
                Directory.Delete(_testWebDir, true);
            } catch {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void GetContentType_Html_ReturnsTextHtml() {
        string result = GetContentType("test.html");
        Assert.Equal("text/html", result);
    }

    [Fact]
    public void GetContentType_Htm_ReturnsTextHtml() {
        string result = GetContentType("test.htm");
        Assert.Equal("text/html", result);
    }

    [Fact]
    public void GetContentType_Js_ReturnsApplicationJavascript() {
        string result = GetContentType("test.js");
        Assert.Equal("application/javascript", result);
    }

    [Fact]
    public void GetContentType_Css_ReturnsTextCss() {
        string result = GetContentType("test.css");
        Assert.Equal("text/css", result);
    }

    [Fact]
    public void GetContentType_Png_ReturnsImagePng() {
        string result = GetContentType("test.png");
        Assert.Equal("image/png", result);
    }

    [Fact]
    public void GetContentType_Jpg_ReturnsImageJpeg() {
        string result = GetContentType("test.jpg");
        Assert.Equal("image/jpeg", result);
    }

    [Fact]
    public void GetContentType_Jpeg_ReturnsImageJpeg() {
        string result = GetContentType("test.jpeg");
        Assert.Equal("image/jpeg", result);
    }

    [Fact]
    public void GetContentType_Gif_ReturnsImageGif() {
        string result = GetContentType("test.gif");
        Assert.Equal("image/gif", result);
    }

    [Fact]
    public void GetContentType_Json_ReturnsApplicationJson() {
        string result = GetContentType("test.json");
        Assert.Equal("application/json", result);
    }

    [Fact]
    public void GetContentType_Ico_ReturnsImageXIcon() {
        string result = GetContentType("test.ico");
        Assert.Equal("image/x-icon", result);
    }

    [Fact]
    public void GetContentType_Svg_ReturnsImageSvgXml() {
        string result = GetContentType("test.svg");
        Assert.Equal("image/svg+xml", result);
    }

    [Fact]
    public void GetContentType_Woff_ReturnsFontWoff() {
        string result = GetContentType("test.woff");
        Assert.Equal("font/woff", result);
    }

    [Fact]
    public void GetContentType_Woff2_ReturnsFontWoff2() {
        string result = GetContentType("test.woff2");
        Assert.Equal("font/woff2", result);
    }

    [Fact]
    public void GetContentType_Txt_ReturnsTextPlain() {
        string result = GetContentType("test.txt");
        Assert.Equal("text/plain", result);
    }

    [Fact]
    public void GetContentType_Xml_ReturnsApplicationXml() {
        string result = GetContentType("test.xml");
        Assert.Equal("application/xml", result);
    }

    [Fact]
    public void GetContentType_Unknown_ReturnsOctetStream() {
        string result = GetContentType("test.unknown");
        Assert.Equal("application/octet-stream", result);
    }

    [Fact]
    public void GetContentType_NoExtension_ReturnsOctetStream() {
        string result = GetContentType("test");
        Assert.Equal("application/octet-stream", result);
    }

    [Fact]
    public void GetContentType_CaseInsensitive_ReturnsCorrectType() {
        string result1 = GetContentType("test.HTML");
        string result2 = GetContentType("test.Html");
        string result3 = GetContentType("test.html");

        Assert.Equal("text/html", result1);
        Assert.Equal("text/html", result2);
        Assert.Equal("text/html", result3);
    }

    [Fact]
    public void CachedResource_ImplementsIResource() {
        // Arrange
        byte[] testData = Encoding.UTF8.GetBytes("Test content");
        var resource = new WebServer.CachedResource(testData, "test.html");

        // Act & Assert
        Assert.NotNull(resource.Name);
        Assert.Equal("test.html", resource.Name);
        Assert.Equal((ulong)testData.Length, resource.Length);
        Assert.Null(resource.Modified);
        Assert.Null(resource.ContentType);
    }

    [Fact]
    public async Task CachedResource_GetContentAsync_ReturnsStreamWithCorrectData() {
        // Arrange
        byte[] testData = Encoding.UTF8.GetBytes("Test content");
        var resource = new WebServer.CachedResource(testData, "test.html");

        // Act
        using Stream stream = await resource.GetContentAsync();

        // Assert
        Assert.NotNull(stream);
        byte[] readData = new byte[testData.Length];
        int bytesRead = await stream.ReadAsync(readData, 0, readData.Length);
        Assert.Equal(testData.Length, bytesRead);
        Assert.Equal(testData, readData);
    }

    [Fact]
    public async Task CachedResource_GetContentAsync_StreamIsAtBeginning() {
        // Arrange
        byte[] testData = Encoding.UTF8.GetBytes("Test content");
        var resource = new WebServer.CachedResource(testData, "test.html");

        // Act
        using Stream stream = await resource.GetContentAsync();

        // Assert - Stream should be readable from the start
        Assert.True(stream.CanRead);
        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public async Task CachedResource_WriteAsync_WritesCorrectData() {
        // Arrange
        byte[] testData = Encoding.UTF8.GetBytes("Test content");
        var resource = new WebServer.CachedResource(testData, "test.html");
        using MemoryStream target = new();

        // Act
        await resource.WriteAsync(target, 1024);

        // Assert
        Assert.Equal(testData.Length, target.Length);
        byte[] writtenData = target.ToArray();
        Assert.Equal(testData, writtenData);
    }

    [Fact]
    public async Task CachedResource_WriteAsync_WithLargeBuffer_WritesCorrectly() {
        // Arrange
        byte[] testData = Encoding.UTF8.GetBytes("Test content");
        var resource = new WebServer.CachedResource(testData, "test.html");
        using MemoryStream target = new();

        // Act - Use a buffer size larger than the data
        await resource.WriteAsync(target, 4096);

        // Assert
        Assert.Equal(testData.Length, target.Length);
        byte[] writtenData = target.ToArray();
        Assert.Equal(testData, writtenData);
    }

    [Fact]
    public async Task CachedResource_WriteAsync_WithSmallBuffer_WritesCorrectly() {
        // Arrange
        byte[] testData = Encoding.UTF8.GetBytes("Test content");
        var resource = new WebServer.CachedResource(testData, "test.html");
        using MemoryStream target = new();

        // Act - Use a buffer size smaller than the data
        await resource.WriteAsync(target, 4);

        // Assert
        Assert.Equal(testData.Length, target.Length);
        byte[] writtenData = target.ToArray();
        Assert.Equal(testData, writtenData);
    }

    [Fact]
    public async Task CachedResource_CalculateChecksumAsync_ReturnsZero() {
        // Arrange
        byte[] testData = Encoding.UTF8.GetBytes("Test content");
        var resource = new WebServer.CachedResource(testData, "test.html");

        // Act
        ulong checksum = await resource.CalculateChecksumAsync();

        // Assert
        Assert.Equal(0UL, checksum);
    }

    [Fact]
    public void CachedResource_WithEmptyData_HasZeroLength() {
        // Arrange
        byte[] emptyData = [];
        var resource = new WebServer.CachedResource(emptyData, "empty.html");

        // Assert
        Assert.Equal(0UL, resource.Length);
    }

    [Fact]
    public async Task CachedResource_WithEmptyData_GetContentAsync_ReturnsEmptyStream() {
        // Arrange
        byte[] emptyData = [];
        var resource = new WebServer.CachedResource(emptyData, "empty.html");

        // Act
        using Stream stream = await resource.GetContentAsync();

        // Assert
        Assert.Equal(0, stream.Length);
        int bytesRead = await stream.ReadAsync(new byte[1], 0, 1);
        Assert.Equal(0, bytesRead);
    }

    [Fact]
    public async Task CachedResource_WithBinaryData_PreservesData() {
        // Arrange - Create binary data (not text)
        byte[] binaryData = [0x00, 0x01, 0x02, 0xFF, 0xFE, 0xFD];
        var resource = new WebServer.CachedResource(binaryData, "binary.bin");

        // Act
        using Stream stream = await resource.GetContentAsync();
        byte[] readData = new byte[binaryData.Length];
        await stream.ReadAsync(readData, 0, readData.Length);

        // Assert
        Assert.Equal(binaryData, readData);
    }

    [Fact]
    public void CachedResource_WithNullName_AllowsNull() {
        // Arrange
        byte[] testData = Encoding.UTF8.GetBytes("Test");
        var resource = new WebServer.CachedResource(testData, null!);

        // Assert
        Assert.Null(resource.Name);
    }

    // Helper method to access private GetContentType using reflection
    private static string GetContentType(string fileName) {
        var method = typeof(WebServer).GetMethod("GetContentType", BindingFlags.NonPublic | BindingFlags.Static);
        if (method == null) {
            throw new InvalidOperationException("GetContentType method not found");
        }

        return method.Invoke(null, [fileName]) as string ?? string.Empty;
    }
}
