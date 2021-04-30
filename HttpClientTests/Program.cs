using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HttpClientTests
{
    public class Program
    {
        public static void Main(string[] args)
        {
         //   AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                //     webBuilder.ConfigureKestrel(options =>
                //     {
                // //        options.ListenLocalhost(5002, o => o.Protocols = HttpProtocols.Http2);
                //  //       options.ListenLocalhost(5002, o => o.Protocols = HttpProtocols.Http2);
                //         options.ListenLocalhost(5000, o => o.Protocols = HttpProtocols.Http2);
                //         //options.ConfigureEndpointDefaults(lo => lo.Protocols = HttpProtocols.Http2);
                //     });
                    webBuilder.UseStartup<Startup>();
                });
    }
}