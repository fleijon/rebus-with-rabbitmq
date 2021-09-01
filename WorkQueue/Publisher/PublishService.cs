using Common.Messages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rebus.Activation;
using Rebus.Config;
using System;
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

                Configure.With(activator)
                    .Logging(l => l.ColoredConsole(minLevel: Rebus.Logging.LogLevel.Warn))
                    .Transport(t => t.UseRabbitMq(_options.Value?.ConnectionString, _options.Value?.InputQueueName))
                    .Start();

                var c = 1;
                while (!cancellationToken.IsCancellationRequested)
                {
                    var message = $"Hi, I'm message nr {c}";
                    _logger.LogInformation($"Publishing message with Task {c} to message queue...");
                    activator.Bus.Publish(new DelayMessage() { Delay = (1 + c % 2) * 5000, TaskId = c }).Wait();
                    await Task.Delay(4000);
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
