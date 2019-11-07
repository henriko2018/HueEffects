using System.IO;
using System.Threading.Tasks;
using HueEffects.Web.Models;
using HueEffects.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HueEffects.Web.UnitTests
{
	public class StorageServiceTests
    {
        private readonly StorageService _sut;
        private readonly string _testPath;

        public StorageServiceTests()
        {
            _testPath = Path.GetTempPath();
            var environmentMock = new Mock<IWebHostEnvironment>();
            environmentMock
                .Setup(env => env.ContentRootPath)
                .Returns(_testPath);
            var loggerMock = new Mock<ILogger<StorageService>>();
            _sut = new StorageService(environmentMock.Object, loggerMock.Object);
        }

		[Fact]
		public async Task Given_file_does_not_exist_then_returns_default_values()
		{
            // Given
            var fileName = Path.Combine(_testPath, nameof(WarmupEffectConfig) + ".json");
            if (File.Exists(fileName))
                File.Delete(fileName);

            // When
            var config = await _sut.LoadConfig<WarmupEffectConfig>();

            // Then
            var defaultConfig = new WarmupEffectConfig();
            Assert.NotNull(config);
            Assert.Equal(defaultConfig.UseMaxTemp, config.UseMaxTemp);
        }

        [Fact]
        public async Task Given_file_exists_then_returns_saved_values()
        {
            // Given
            var savedConfig = new WarmupEffectConfig { UseMaxTemp = 123 };
            await _sut.SaveConfig(savedConfig);

            // When
            var loadedConfig = await _sut.LoadConfig<WarmupEffectConfig>();

            // Then
            Assert.NotNull(loadedConfig);
            Assert.Equal(savedConfig.UseMaxTemp, loadedConfig.UseMaxTemp);
        }
    }
}
