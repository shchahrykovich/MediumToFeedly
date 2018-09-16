using ConsoleRouter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MTFConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new AppHostBuilder()
                              .WithRoute("sync(controller=SyncToMedium,action=Sync)")
                              .WithRoute("{controller=help} {action=help}")
                              .WithHelp("This tool updates feedly feeds with medium subscriptions.");
            var host = builder.Build();
            host.Run(args);
        }
    }
}