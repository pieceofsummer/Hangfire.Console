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
            _consoleId = consoleId ?? throw new ArgumentNullException(nameof(consoleId));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));

            _lastTimeOffset = 0;
            _nextProgressBarId = 0;

            _storage.InitConsole(_consoleId);
        }

        public ConsoleTextColor TextColor { get; set; }
        
        public void AddLine(ConsoleLine line)
        {
            if (line == null)
                throw new ArgumentNullException(nameof(line));

            lock (this)
            {
                line.TimeOffset = Math.Round((DateTime.UtcNow - _consoleId.DateValue).TotalSeconds, 3);

                if (_lastTimeOffset >= line.TimeOffset)
                {
                    // prevent duplicate lines collapsing
                    line.TimeOffset = _lastTimeOffset + 0.0001;
                }

                _lastTimeOffset = line.TimeOffset;

                _storage.AddLine(_consoleId, line);
            }
        }
        
        public void WriteLine(string value, ConsoleTextColor color)
        {
            AddLine(new ConsoleLine() { Message = value ?? "", TextColor = color ?? TextColor });
        }

        public IProgressBar WriteProgressBar(string name, double value, ConsoleTextColor color)
        {
            var progressBarId = Interlocked.Increment(ref _nextProgressBarId);

            var progressBar = new DefaultProgressBar(this, progressBarId.ToString(CultureInfo.InvariantCulture), name, color);

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
            var ttl = _storage.GetConsoleTtl(_consoleId);
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
