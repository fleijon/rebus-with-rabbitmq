using Common.Messages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Retry.Simple;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Publisher
{
    internal class PublishService : IHostedService, IDisposable
    {
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
                var config = new Dictionary<string, string>();
                config.Add("x-queue-type", "streams");
                config.Add("x-max-length-bytes", "2000000000");
                config.Add("x-stream-max-segment-size-bytes", "100000000");
                Configure.With(activator)
                    .Options(b => b.SimpleRetryStrategy(maxDeliveryAttempts: 10))
                    .Logging(l => l.ColoredConsole(minLevel: Rebus.Logging.LogLevel.Error))
                    .Transport(t => t.UseRabbitMq(_options.Value?.ConnectionString, _options.Value?.InputQueueName)
                                     .AddClientProperties(config))
                    .Start();

                var c = 1;
                while (!cancellationToken.IsCancellationRequested)
                {
                    var message = $"Hi, I'm message nr {c.ToString()}";
                    _logger.LogInformation($"Publishing message nr [{c}] to message queue...");
                    activator.Bus.Publish(new StringMessage(message)).Wait();
                    await Task.Delay(5000);
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
