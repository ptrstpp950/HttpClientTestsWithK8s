using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HttpClientTests.Controllers
{
    [ApiController]
    [Route("")]
    public class TestController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TestController> _logger;

        private static readonly HttpClient Client = new HttpClient();
        private static readonly HttpClient ClientWithDefaultHandler = new HttpClient(new HttpClientHandler());

        public TestController(IHttpClientFactory httpClientFactory, ILogger<TestController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpGet]
        public string Get()
        {
            return Environment.MachineName;
        }

        [HttpGet("inside/{count}/{url}")]
        public async Task<IDictionary<string,long>> GetInside(int count, string url)
        {
            url = WebUtility.UrlDecode(url);
            var dict = new Dictionary<string, long>();
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < count; i++)
            {
                var client = _httpClientFactory.CreateClient();
                var result = await client.GetAsync(url);
                var content = await result.Content.ReadAsStringAsync();
                dict[content] = dict.GetValueOrDefault(content) + 1;
            }
            sw.Stop();
            dict["TotalTime"] = sw.ElapsedMilliseconds;
            return dict;
        }

        [HttpGet("outside/{count}/{url}")]
        public async Task<IDictionary<string,long>> GetOutside(int count, string url)
        {
            url = WebUtility.UrlDecode(url);
            var dict = new Dictionary<string, long>();
            var sw = Stopwatch.StartNew();
            var client = _httpClientFactory.CreateClient();
            for (var i = 0; i < count; i++)
            {
                var result = await client.GetAsync(url);
                var content = await result.Content.ReadAsStringAsync();
                dict[content] = dict.GetValueOrDefault(content) + 1;
            }
            sw.Stop();
            dict["TotalTime"] = sw.ElapsedMilliseconds;
            return dict;
        }
        
        [HttpGet("shortened/{count}/{url}")]
        public async Task<IDictionary<string,long>> GetShortened(int count, string url)
        {
            url = WebUtility.UrlDecode(url);
            var dict = new Dictionary<string, long>();
            var sw = Stopwatch.StartNew();
            var client = _httpClientFactory.CreateClient("shortenedhandlerlifetime");
            for (var i = 0; i < count; i++)
            {
                var result = await client.GetAsync(url);
                var content = await result.Content.ReadAsStringAsync();
                dict[content] = dict.GetValueOrDefault(content) + 1;
            }
            sw.Stop();
            dict["TotalTime"] = sw.ElapsedMilliseconds;
            return dict;
        }
        
        [HttpGet("defaultHttpHandler/{count}/{url}")]
        public async Task<IDictionary<string,long>> GetDefaultHttpHandler(int count, string url)
        {
            url = WebUtility.UrlDecode(url);
            var dict = new Dictionary<string, long>();
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < count; i++)
            {
                var result = await ClientWithDefaultHandler.GetAsync(url);
                var content = await result.Content.ReadAsStringAsync();
                dict[content] = dict.GetValueOrDefault(content) + 1;
            }
            sw.Stop();
            dict["TotalTime"] = sw.ElapsedMilliseconds;
            return dict;
        }

        [HttpGet("singleton/{count}/{url}")]
        public async Task<IDictionary<string,long>> GetSingleton(int count, string url)
        {
            url = WebUtility.UrlDecode(url);
            var dict = new Dictionary<string, long>();
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < count; i++)
            {
                var result = await Client.GetAsync(url);
                var content = await result.Content.ReadAsStringAsync();
                dict[content] = dict.GetValueOrDefault(content) + 1;
            }
            sw.Stop();
            dict["TotalTime"] = sw.ElapsedMilliseconds;
            return dict;
        }
        [HttpGet("manual/{count}/{path}/{ipList}")]
        public async Task<IDictionary<string,long>> GetSingleton(int count, string path, string ipList)
        {
            path = WebUtility.UrlDecode(path).TrimStart('/');
            var dict = new Dictionary<string, long>();
            var urlList = ipList.Split(",")
                .Select(ip => ip.StartsWith("http") ? ip : $"http://{ip.TrimEnd('/')}/{path}")
                .ToArray();
            var clients = urlList
                .Select(ip => new HttpClient()).ToArray();

            var clientCount = clients.Count();
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < count; i++)
            {
                var client = clients[i % clientCount];
                var url = urlList[i % clientCount];
                var result = await client.GetAsync(url);
                var content = await result.Content.ReadAsStringAsync();
                dict[content] = dict.GetValueOrDefault(content) + 1;
            }
            sw.Stop();
            dict["TotalTime"] = sw.ElapsedMilliseconds;
            dict["ClientsCount"] = urlList.Distinct().Count();
            return dict;
        }
    }
}