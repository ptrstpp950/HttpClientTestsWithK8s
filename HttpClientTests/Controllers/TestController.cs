using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using IApplicationLifetime = Microsoft.AspNetCore.Hosting.IApplicationLifetime;

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
        private static readonly HttpClient ClientWithCustomHandler = new HttpClient(new SocketsHttpHandler()
        {
            PooledConnectionIdleTimeout = TimeSpan.FromSeconds(30),
            PooledConnectionLifetime = TimeSpan.FromSeconds(120)
        });

        private static readonly HttpClient ClientWithRetries = new HttpClient(
            new PolicyHttpMessageHandler(GetRetryPolicy())
            {
                InnerHandler = new SocketsHttpHandler()
            }
        );
        
        
        static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            Random jitterer = new Random();
            return HttpPolicyExtensions
                .HandleTransientHttpError() 
                .WaitAndRetryAsync(6,    // exponential back-off plus some jitter
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))  
                                    + TimeSpan.FromMilliseconds(jitterer.Next(0, 100))
                );
        }

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

        private static int _counter = 0;
        [HttpGet("timeout")]
        public ActionResult<string> GetTimeout()
        {
            Interlocked.Increment(ref _counter);
            if (_counter % 3 == 0)
            {
                return StatusCode(StatusCodes.Status504GatewayTimeout);
            }
            
            return Ok(Environment.MachineName);
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

        [HttpGet("customHandler/{count}/{url}")]
        public async Task<IDictionary<string,long>> GetCustomHandler(int count, string url)
        {
            url = WebUtility.UrlDecode(url);
            var dict = new Dictionary<string, long>();
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < count; i++)
            {
                var result = await ClientWithCustomHandler.GetAsync(url);
                var content = await result.Content.ReadAsStringAsync();
                dict[content] = dict.GetValueOrDefault(content) + 1;
            }
            sw.Stop();
            dict["TotalTime"] = sw.ElapsedMilliseconds;
            return dict;
        }

        [HttpGet("retryHandler/{count}/{url}")]
        public async Task<IDictionary<string, long>> GetWithRetries(int count, string url)
        {
            url = WebUtility.UrlDecode(url);
            var dict = new Dictionary<string, long>();
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < count; i++)
            {
                var result = await ClientWithRetries.GetAsync(url);
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