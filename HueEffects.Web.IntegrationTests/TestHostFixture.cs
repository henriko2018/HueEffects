using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace HueEffects.Web.IntegrationTests
{
    /// <summary>
    /// One instance of this will be created per test collection.
    /// </summary>
    public class TestHostFixture : ICollectionFixture<CustomWebApplicationFactory>
    {
        public readonly HttpClient Client;
        public readonly CustomWebApplicationFactory WebApplicationFactory;

        public TestHostFixture()
        {
            WebApplicationFactory = new CustomWebApplicationFactory();
            Client = WebApplicationFactory.CreateClient(new WebApplicationFactoryClientOptions
            { AllowAutoRedirect = false });
        }
    }

    [CollectionDefinition("Integration tests collection")]
    public class IntegrationTestsCollection : ICollectionFixture<TestHostFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
