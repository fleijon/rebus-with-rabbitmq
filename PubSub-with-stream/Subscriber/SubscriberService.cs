using Common.Messages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Routing.TypeBased;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Subscriber
{
    class SubscriberService : IHostedService, IDisposable
    {
        public static int _readCount = 0;
        private static readonly int _maxReads = 5;
        private readonly IOptions<RabbitMQOption> _rabbitMQOption;
        private readonly ILogger<Handler> _logger;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;

        public SubscriberService(IOptions<RabbitMQOption> rabbitMQOption, ILogger<Handler> logger, IHostApplicationLifetime hostApplicationLifetime)
        {
            _rabbitMQOption = rabbitMQOption;
            _logger = logger;
            _hostApplicationLifetime = hostApplicationLifetime;
        }
        public void Dispose()
        {
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            using (var activator = new BuiltinHandlerActivator())
            {
                activator.Register(() => new Handler(_logger));

                var config = new Dictionary<string, string>();
                config.Add("x-queue-type", "streams");
                config.Add("x-max-length-bytes", "2000000000");
                config.Add("x-stream-max-segment-size-bytes", "100000000");
                config.Add("x-stream-offset", "first");

                Configure.With(activator)
                    .Logging(l => l.ColoredConsole(minLevel: Rebus.Logging.LogLevel.Error))
                    .Transport(t => t.UseRabbitMq(_rabbitMQOption.Value?.ConnectionString, _rabbitMQOption.Value?.InputQueueName)
                                     .Prefetch(10)
                                     .AddClientProperties(config))
                    .Routing(r => r.TypeBased().MapAssemblyOf<StringMessage>("publisher"))
                    .Start();

                while (!cancellationToken.IsCancellationRequested && _readCount < _maxReads)
                {
                    activator.Bus.Subscribe<StringMessage>().Wait(1000, cancellationToken);
                }
            }

            _hostApplicationLifetime.StopApplication();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

    }

    class Handler : IHandleMessages<StringMessage>, IHandleMessages<DateTimeMessage>
    {
        private readonly ILogger<Handler> _logger;

        public Handler(ILogger<Handler> logger)
        {
            _logger = logger;
        }

        public Task Handle(StringMessage message)
        {

            SubscriberService._readCount++;
            _logger.Log(LogLevel.Information, $"Got string: {message.Text}");
            return Task.CompletedTask;
        }

        public Task Handle(DateTimeMessage message)
        {
            _logger.Log(LogLevel.Information, $"Got DateTime: {message.DateTime}");
            return Task.CompletedTask;
        }

    }
}
