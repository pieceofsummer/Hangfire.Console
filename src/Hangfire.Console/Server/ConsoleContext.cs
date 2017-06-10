using Hangfire.Console.Progress;
using Hangfire.Console.Serialization;
using Hangfire.Console.Storage;
using Hangfire.Server;
using System;
using System.Globalization;
using System.Threading;

namespace Hangfire.Console.Server
{
    internal class ConsoleContext
    {
        private readonly ConsoleId _consoleId;
        private readonly IConsoleStorage _storage;
        private double _lastTimeOffset;
        private int _nextProgressBarId;

        public static ConsoleContext FromPerformContext(PerformContext context)
        {
            if (context == null)
            {
                // PerformContext might be null because of refactoring, or during tests
                return null;
            }

            if (!context.Items.ContainsKey("ConsoleContext"))
            {
                // Absence of ConsoleContext means ConsoleServerFilter was not properly added
                return null;
            }

            return (ConsoleContext)context.Items["ConsoleContext"];
        }

        public ConsoleContext(ConsoleId consoleId, IConsoleStorage storage)
        {
            if (consoleId == null)
                throw new ArgumentNullException(nameof(consoleId));
            if (storage == null)
                throw new ArgumentNullException(nameof(storage));

            _consoleId = consoleId;
            _storage = storage;

            _lastTimeOffset = 0;
            _nextProgressBarId = 0;

            _storage.InitConsole(_consoleId);
        }

        public ConsoleTextColor TextColor { get; set; }
        
        public void AddLine(ConsoleLine line)
        {
            if (line == null)
                throw new ArgumentNullException(nameof(line));

            line.TimeOffset = Math.Round((DateTime.UtcNow - _consoleId.DateValue).TotalSeconds, 3);

            if (_lastTimeOffset >= line.TimeOffset)
            {
                // prevent duplicate lines collapsing
                line.TimeOffset = _lastTimeOffset + 0.0001;
            }

            _lastTimeOffset = line.TimeOffset;
            
            _storage.AddLine(_consoleId, line);
        }
        
        public void WriteLine(string value)
        {
            AddLine(new ConsoleLine() { Message = value ?? "", TextColor = TextColor });
        }

        public IProgressBar WriteProgressBar(int value, ConsoleTextColor color)
        {
            var progressBarId = Interlocked.Increment(ref _nextProgressBarId);

            var progressBar = new DefaultProgressBar(this, progressBarId.ToString(CultureInfo.InvariantCulture), color);

            // set initial value
            progressBar.SetValue(value);

            return progressBar;
        }

        public void Expire(TimeSpan expireIn)
        {
            _storage.Expire(_consoleId, expireIn);
        }

        public void FixExpiration()
        {
            TimeSpan ttl = _storage.GetConsoleTtl(_consoleId);
            if (ttl <= TimeSpan.Zero)
            {
                // ConsoleApplyStateFilter not called yet, or current job state is not final.
                // Either way, there's no need to expire console here.
                return;
            }
            
            _storage.Expire(_consoleId, ttl);
        }
    }
}
