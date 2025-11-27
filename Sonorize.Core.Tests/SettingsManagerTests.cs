using Sonorize.Core.Settings;
using System.Text.Json.Serialization;

namespace Sonorize.Core.Tests;

public class SettingsManagerTests : IDisposable
{
    private readonly string _testDirectory;

    private enum TestEnum { First, Second }

    private class TestSettings
    {
        public string TestString { get; set; } = "Default";
        public int TestInt { get; set; } = 100;
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TestEnum TestEnumValue { get; set; } = TestEnum.First;
    }

    public SettingsManagerTests()
    {
        // Create a unique temporary directory for each test run to ensure isolation.
        _testDirectory = Path.Combine(Path.GetTempPath(), "SonorizeTests_" + Guid.NewGuid().ToString());
        _ = Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        // Clean up the temporary directory after all tests in the class have run.
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    private SettingsManager<TestSettings> CreateManager(string fileName)
    {
        // A custom constructor that redirects the SettingsManager to our temporary directory.
        return new SettingsManager<TestSettings>(fileName, _testDirectory);
    }

    [Fact]
    public void Save_WhenCalled_CreatesDirectoryAndFile()
    {
        // Arrange
        SettingsManager<TestSettings> manager = CreateManager("test_settings.json");
        TestSettings settings = new() { TestString = "Hello" };
        string expectedPath = Path.Combine(_testDirectory, "test_settings.json");

        // Act
        manager.Save(settings);

        // Assert
        Assert.True(Directory.Exists(_testDirectory));
        Assert.True(File.Exists(expectedPath));
    }

    [Fact]
    public void Save_ThenLoad_ReturnsEquivalentObject()
    {
        // Arrange
        SettingsManager<TestSettings> manager = CreateManager("save_load_test.json");
        TestSettings originalSettings = new()
        {
            TestString = "Specific Value",
            TestInt = 42,
            TestEnumValue = TestEnum.Second
        };

        // Act
        manager.Save(originalSettings);
        TestSettings loadedSettings = manager.Load();

        // Assert
        Assert.NotNull(loadedSettings);
        Assert.NotSame(originalSettings, loadedSettings); // Should be a new instance
        Assert.Equal(originalSettings.TestString, loadedSettings.TestString);
        Assert.Equal(originalSettings.TestInt, loadedSettings.TestInt);
        Assert.Equal(originalSettings.TestEnumValue, loadedSettings.TestEnumValue);
    }

    [Fact]
    public void Load_WhenFileDoesNotExist_ReturnsNewObjectAndCreatesFile()
    {
        // Arrange
        SettingsManager<TestSettings> manager = CreateManager("non_existent.json");
        string expectedPath = Path.Combine(_testDirectory, "non_existent.json");

        // Act
        TestSettings loadedSettings = manager.Load();

        // Assert
        Assert.NotNull(loadedSettings);
        Assert.Equal("Default", loadedSettings.TestString); // Default value from class constructor
        Assert.Equal(100, loadedSettings.TestInt);
        Assert.True(File.Exists(expectedPath)); // Should create the file on first load
    }

    [Fact]
    public void Load_WhenFileIsCorrupt_ReturnsNewObjectAndDoesNotThrow()
    {
        // Arrange
        SettingsManager<TestSettings> manager = CreateManager("corrupt.json");
        string filePath = Path.Combine(_testDirectory, "corrupt.json");
        File.WriteAllText(filePath, "{ \"TestString\": \"Hello\",, \"TestInt\": 123 "); // Invalid JSON

        // Act
        TestSettings loadedSettings = manager.Load();

        // Assert
        Assert.NotNull(loadedSettings);
        Assert.Equal("Default", loadedSettings.TestString); // Should fall back to defaults
    }

    [Fact]
    public void Save_WithEnums_SerializesAsStrings()
    {
        // Arrange
        SettingsManager<TestSettings> manager = CreateManager("enum_test.json");
        TestSettings settings = new() { TestEnumValue = TestEnum.Second };
        string filePath = Path.Combine(_testDirectory, "enum_test.json");

        // Act
        manager.Save(settings);
        string jsonContent = File.ReadAllText(filePath);

        // Assert
        Assert.Contains("\"TestEnumValue\": \"Second\"", jsonContent);
        Assert.DoesNotContain("\"TestEnumValue\": 1", jsonContent);
    }
}