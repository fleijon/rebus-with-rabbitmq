using Common.Messages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rebus.Activation;
using Rebus.Config;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Publisher
{
    internal class PublishService : IHostedService, IDisposable
    {
        static readonly string JsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rebus_subscriptions.json");
        private readonly IOptions<RabbitMQOption> _options;
        private readonly ILogger<PublishService> _logger;

        public PublishService(IOptions<RabbitMQOption> options, ILogger<PublishService> logger)
        {
            _options = options;
            _logger = logger;
        }

        public void Dispose()
        {
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using (var activator = new BuiltinHandlerActivator())
            {

                Configure.With(activator)
                    .Logging(l => l.ColoredConsole(minLevel: Rebus.Logging.LogLevel.Warn))
                    .Transport(t => t.UseRabbitMq(_options.Value?.ConnectionString, _options.Value?.InputQueueName))
                    .Start();

                var c = 1;
                while (!cancellationToken.IsCancellationRequested)
                {
                    var message = $"Hi, I'm message nr {c.ToString()}";
                    _logger.LogInformation("Publishing message to message queue...");
                    activator.Bus.Publish(new StringMessage(message)).Wait();
                    await Task.Delay(10000);
                    c++;
                }

            }

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }


}
