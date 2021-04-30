using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace HttpClientTests
{
    public class TestClass
    {
        private static readonly HttpClient ClientWithDefaultHandler = new HttpClient(new HttpClientHandler());
        private readonly HttpClient _client;
        private readonly string _url;
        
        public TestClass()
        {
            var services = new ServiceCollection();
            services.AddHttpClient();
            var serviceProvider = services.BuildServiceProvider();
            var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
            _client = httpClientFactory.CreateClient();
            _url = "https://google.com";
        }
        
        [Benchmark]
        public async Task<string> FromHttpClientFactory()
        {
            var result = await _client.GetAsync(_url);
            var content = await result.Content.ReadAsStringAsync();
            return content;
        }
        
        [Benchmark]
        public async Task<string> HttpClientWithHttpClientHandler()
        {
            var result = await ClientWithDefaultHandler.GetAsync(_url);
            var content = await result.Content.ReadAsStringAsync();
            return content;
        }
    }
}