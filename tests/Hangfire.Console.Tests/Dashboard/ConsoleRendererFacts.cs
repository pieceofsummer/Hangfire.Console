using Hangfire.Common;
using Hangfire.Console.Dashboard;
using Hangfire.Console.Serialization;
using Hangfire.Console.Storage;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Hangfire.Console.Tests.Dashboard
{
    public class ConsoleRendererFacts
    {
        private readonly ConsoleId _consoleId;
        private readonly Mock<IConsoleStorage> _storage;

        public ConsoleRendererFacts()
        {
            _consoleId = new ConsoleId("1", new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            _storage = new Mock<IConsoleStorage>();
        }

        [Fact]
        public void RenderLine_Basic()
        {
            var line = new ConsoleLine() { TimeOffset = 0, Message = "test" };
            var builder = new StringBuilder();

            ConsoleRenderer.RenderLine(builder, line, new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            Assert.Equal("<div class=\"line\"><span data-moment-title=\"1451606400\">+ <1ms</span>test</div>", builder.ToString());
        }

        [Fact]
        public void RenderLine_WithOffset()
        {
            var line = new ConsoleLine() { TimeOffset = 1.108, Message = "test" };
            var builder = new StringBuilder();

            ConsoleRenderer.RenderLine(builder, line, new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            Assert.Equal("<div class=\"line\"><span data-moment-title=\"1451606401\">+1.108s</span>test</div>", builder.ToString());
        }

        [Fact]
        public void RenderLine_WithNegativeOffset()
        {
            var line = new ConsoleLine() { TimeOffset = -1.206, Message = "test" };
            var builder = new StringBuilder();

            ConsoleRenderer.RenderLine(builder, line, new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            Assert.Equal("<div class=\"line\"><span data-moment-title=\"1451606398\">-1.206s</span>test</div>", builder.ToString());
        }
        
        [Fact]
        public void RenderLine_WithColor()
        {
            var line = new ConsoleLine() { TimeOffset = 0, Message = "test", TextColor = "#ffffff" };
            var builder = new StringBuilder();

            ConsoleRenderer.RenderLine(builder, line, new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            Assert.Equal("<div class=\"line\" style=\"color:#ffffff\"><span data-moment-title=\"1451606400\">+ <1ms</span>test</div>", builder.ToString());
        }

        [Fact]
        public void RenderLine_WithProgress()
        {
            var line = new ConsoleLine() { TimeOffset = 0, Message = "3", ProgressValue = 17 };
            var builder = new StringBuilder();

            ConsoleRenderer.RenderLine(builder, line, new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            Assert.Equal("<div class=\"line pb\" data-id=\"3\"><span data-moment-title=\"1451606400\">+ <1ms</span><div class=\"pv\" style=\"width:17%\" data-value=\"17\"></div></div>", builder.ToString());
        }

        [Fact]
        public void RenderLine_WithProgressAndColor()
        {
            var line = new ConsoleLine() { TimeOffset = 0, Message = "3", ProgressValue = 17, TextColor = "#ffffff" };
            var builder = new StringBuilder();

            ConsoleRenderer.RenderLine(builder, line, new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            Assert.Equal("<div class=\"line pb\" style=\"color:#ffffff\" data-id=\"3\"><span data-moment-title=\"1451606400\">+ <1ms</span><div class=\"pv\" style=\"width:17%\" data-value=\"17\"></div></div>", builder.ToString());
        }
        
        [Fact]
        public void RenderLines_ThrowsException_IfBuilderIsNull()
        {
            Assert.Throws<ArgumentNullException>("builder", () => ConsoleRenderer.RenderLines(null, Enumerable.Empty<ConsoleLine>(), DateTime.UtcNow));
        }

        [Fact]
        public void RenderLines_RendersNothing_IfLinesIsNull()
        {
            var builder = new StringBuilder();
            ConsoleRenderer.RenderLines(builder, null, DateTime.UtcNow);

            Assert.Equal(0, builder.Length);
        }

        [Fact]
        public void RenderLines_RendersNothing_IfLinesIsEmpty()
        {
            var builder = new StringBuilder();
            ConsoleRenderer.RenderLines(builder, Enumerable.Empty<ConsoleLine>(), DateTime.UtcNow);

            Assert.Equal(0, builder.Length);
        }
        
        [Fact]
        public void RenderLines_RendersAllLines()
        {
            var builder = new StringBuilder();
            ConsoleRenderer.RenderLines(builder, new[] 
            {
                new ConsoleLine() { TimeOffset = 0, Message = "line1" },
                new ConsoleLine() { TimeOffset = 1, Message = "line2" }
            }, new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            Assert.Equal(
                "<div class=\"line\"><span data-moment-title=\"1451606400\">+ <1ms</span>line1</div>" +
                "<div class=\"line\"><span data-moment-title=\"1451606401\">+1s</span>line2</div>", 
                builder.ToString());
        }

        [Fact]
        public void RenderLineBuffer_ThrowsException_IfBuilderIsNull()
        {
            Assert.Throws<ArgumentNullException>("builder", () => ConsoleRenderer.RenderLineBuffer(null, _storage.Object, _consoleId, 0));
        }

        [Fact]
        public void RenderLineBuffer_ThrowsException_IfStorageIsNull()
        {
            Assert.Throws<ArgumentNullException>("storage", () => ConsoleRenderer.RenderLineBuffer(new StringBuilder(), null, _consoleId, 0));
        }

        [Fact]
        public void RenderLineBuffer_ThrowsException_IfConsoleIdIsNull()
        {
            Assert.Throws<ArgumentNullException>("consoleId", () => ConsoleRenderer.RenderLineBuffer(new StringBuilder(), _storage.Object, null, 0));
        }

        [Fact]
        public void RenderLineBuffer_RendersEmpty_WithNegativeN_IfStartIsNegative()
        {
            SetupStorage(CreateState(ProcessingState.StateName, _consoleId.DateValue));

            var builder = new StringBuilder();
            ConsoleRenderer.RenderLineBuffer(builder, _storage.Object, _consoleId, -1);

            Assert.Equal("<div class=\"line-buffer\" data-n=\"-1\"></div>", builder.ToString());
        }
        
        [Fact]
        public void RenderLineBuffer_RendersAllLines_IfStartIsZero()
        {
            SetupStorage(CreateState(ProcessingState.StateName, _consoleId.DateValue),
                new ConsoleLine() { TimeOffset = 0, Message = "line1" },
                new ConsoleLine() { TimeOffset = 1, Message = "line2" });
            
            var builder = new StringBuilder();
            ConsoleRenderer.RenderLineBuffer(builder, _storage.Object, _consoleId, 0);

            Assert.Equal(
                "<div class=\"line-buffer\" data-n=\"2\">" +
                "<div class=\"line\"><span data-moment-title=\"1451606400\">+ <1ms</span>line1</div>" +
                "<div class=\"line\"><span data-moment-title=\"1451606401\">+1s</span>line2</div>" +
                "</div>", builder.ToString());
        }
        
        [Fact]
        public void RenderLineBuffer_RendersLines_FromStartOffset()
        {
            SetupStorage(CreateState(ProcessingState.StateName, _consoleId.DateValue),
                new ConsoleLine() { TimeOffset = 0, Message = "line1" },
                new ConsoleLine() { TimeOffset = 1, Message = "line2" });

            var builder = new StringBuilder();
            ConsoleRenderer.RenderLineBuffer(builder, _storage.Object, _consoleId, 1);

            Assert.Equal(
                "<div class=\"line-buffer\" data-n=\"2\">" +
                "<div class=\"line\"><span data-moment-title=\"1451606401\">+1s</span>line2</div>" +
                "</div>", builder.ToString());
        }

        [Fact]
        public void RenderLineBuffer_RendersEmpty_WithLineCount_IfNoMoreLines()
        {
            SetupStorage(CreateState(ProcessingState.StateName, _consoleId.DateValue),
                new ConsoleLine() { TimeOffset = 0, Message = "line1" },
                new ConsoleLine() { TimeOffset = 1, Message = "line2" });

            var builder = new StringBuilder();
            ConsoleRenderer.RenderLineBuffer(builder, _storage.Object, _consoleId, 2);

            Assert.Equal("<div class=\"line-buffer\" data-n=\"2\"></div>", builder.ToString());
        }

        [Fact]
        public void RenderLineBuffer_RendersEmpty_WithMinusTwo_IfStateNotFound()
        {
            SetupStorage(null,
                new ConsoleLine() { TimeOffset = 0, Message = "line1" },
                new ConsoleLine() { TimeOffset = 1, Message = "line2" });
            
            var builder = new StringBuilder();
            ConsoleRenderer.RenderLineBuffer(builder, _storage.Object, _consoleId, 2);

            Assert.Equal("<div class=\"line-buffer\" data-n=\"-2\"></div>", builder.ToString());
        }

        [Fact]
        public void RenderLineBuffer_RendersEmpty_WithMinusOne_IfStateIsNotProcessing()
        {
            SetupStorage(CreateState(SucceededState.StateName, _consoleId.DateValue),
                new ConsoleLine() { TimeOffset = 0, Message = "line1" },
                new ConsoleLine() { TimeOffset = 1, Message = "line2" });

            var builder = new StringBuilder();
            ConsoleRenderer.RenderLineBuffer(builder, _storage.Object, _consoleId, 2);

            Assert.Equal("<div class=\"line-buffer\" data-n=\"-1\"></div>", builder.ToString());
        }

        [Fact]
        public void RenderLineBuffer_RendersEmpty_WithMinusOne_IfStateIsAnotherProcessing()
        {
            SetupStorage(CreateState(ProcessingState.StateName, _consoleId.DateValue.AddMinutes(1)),
                new ConsoleLine() { TimeOffset = 0, Message = "line1" },
                new ConsoleLine() { TimeOffset = 1, Message = "line2" });

            var builder = new StringBuilder();
            ConsoleRenderer.RenderLineBuffer(builder, _storage.Object, _consoleId, 2);

            Assert.Equal("<div class=\"line-buffer\" data-n=\"-1\"></div>", builder.ToString());
        }

        [Fact]
        public void RenderLineBuffer_AggregatesMultipleProgressLines()
        {
            SetupStorage(CreateState(ProcessingState.StateName, _consoleId.DateValue),
                new ConsoleLine() { TimeOffset = 0, Message = "0", ProgressValue = 1 },
                new ConsoleLine() { TimeOffset = 1, Message = "1", ProgressValue = 3 },
                new ConsoleLine() { TimeOffset = 2, Message = "0", ProgressValue = 5 });
            
            var builder = new StringBuilder();
            ConsoleRenderer.RenderLineBuffer(builder, _storage.Object, _consoleId, 0);

            Assert.Equal(
                "<div class=\"line-buffer\" data-n=\"3\">" +
                "<div class=\"line pb\" data-id=\"0\"><span data-moment-title=\"1451606400\">+ <1ms</span><div class=\"pv\" style=\"width:5%\" data-value=\"5\"></div></div>" +
                "<div class=\"line pb\" data-id=\"1\"><span data-moment-title=\"1451606401\">+1s</span><div class=\"pv\" style=\"width:3%\" data-value=\"3\"></div></div>" +
                "</div>", builder.ToString());
        }
        
        private StateData CreateState(string stateName, DateTime startedAt)
        {
            return new StateData()
            {
                Name = stateName,
                Data = new Dictionary<string, string>()
                {
                    ["StartedAt"] = JobHelper.SerializeDateTime(startedAt)
                }
            };
        }

        private void SetupStorage(StateData stateData, params ConsoleLine[] lines)
        {
            _storage.Setup(x => x.GetState(It.IsAny<ConsoleId>()))
                .Returns(stateData);
            _storage.Setup(x => x.GetLineCount(It.IsAny<ConsoleId>()))
                .Returns(lines.Length);
            _storage.Setup(x => x.GetLines(It.IsAny<ConsoleId>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns((ConsoleId id, int start, int end) => lines.Where((x, i) => i >= start && i <= end));
        }
    }
}
