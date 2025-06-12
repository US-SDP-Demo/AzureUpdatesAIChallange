using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Xunit;

namespace SimpleFeedReader.Tests.Services
{
    public class AIServiceConfigurationTests
    {
        [Fact]
        public void KernelBuilder_Should_Configure_With_ApiKey()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("AzureOpenAIApiKey", "test-key"),
                    new KeyValuePair<string, string>("AzureOpenAIEndpoint", "https://test.openai.azure.com"),
                    new KeyValuePair<string, string>("AzureOpenAIDeploymentName", "gpt-4o-mini")
                })
                .Build();

            var kernelBuilder = Kernel.CreateBuilder();

            // Act & Assert - Should not throw
            var exception = Record.Exception(() => {
                Program.ConfigureKernelBuilder(kernelBuilder, configuration);
            });

            Assert.Null(exception);
        }

        [Fact]
        public void KernelBuilder_Should_Configure_With_GitHubToken()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("GITHUB_TOKEN", "github-token"),
                    new KeyValuePair<string, string>("AzureOpenAIEndpoint", "https://test.openai.azure.com"),
                    new KeyValuePair<string, string>("AzureOpenAIDeploymentName", "gpt-4o-mini")
                })
                .Build();

            var kernelBuilder = Kernel.CreateBuilder();

            // Act & Assert - Should not throw
            var exception = Record.Exception(() => {
                Program.ConfigureKernelBuilder(kernelBuilder, configuration);
            });

            Assert.Null(exception);
        }
    }
}