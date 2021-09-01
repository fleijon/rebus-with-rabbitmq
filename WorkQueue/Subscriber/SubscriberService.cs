using Common.Messages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Routing.TypeBased;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Subscriber
{
    class SubscriberService : IHostedService, IDisposable
    {
        private readonly IOptions<RabbitMQOption> _rabbitMQOption;
        private readonly ILogger<Handler> _logger;

        public SubscriberService(IOptions<RabbitMQOption> rabbitMQOption, ILogger<Handler> logger)
        {
            _rabbitMQOption = rabbitMQOption;
            _logger = logger;
        }
        public void Dispose()
        {
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            using (var activator = new BuiltinHandlerActivator())
            {
                activator.Register(() => new Handler(_logger));

                Configure.With(activator)
                    .Logging(l => l.ColoredConsole(minLevel: Rebus.Logging.LogLevel.Warn))
                    .Transport(t => t.UseRabbitMq(_rabbitMQOption.Value?.ConnectionString, _rabbitMQOption.Value?.InputQueueName))
                    .Routing(r => r.TypeBased().MapAssemblyOf<DelayMessage>("publisher"))
                    .Start();

                while (!cancellationToken.IsCancellationRequested)
                {
                    activator.Bus.Subscribe<DelayMessage>().Wait();
                }
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

    }

    class Handler : IHandleMessages<DelayMessage>
    {
        private readonly ILogger<Handler> _logger;

        public Handler(ILogger<Handler> logger)
        {
            _logger = logger;
        }

        public async Task Handle(DelayMessage message)
        {
            _logger.Log(LogLevel.Information, $"Message received. Task: {message.TaskId.ToString()}. Will delay for: {message.Delay.ToString()} ms");
            await Task.Delay(message.Delay);
        }

    }
}
