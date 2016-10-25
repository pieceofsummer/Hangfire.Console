using Hangfire.Console.Dashboard;
using Hangfire.Console.Serialization;
using System;
using System.Text;
using Xunit;

namespace Hangfire.Console.Tests.Dashboard
{
    public class ConsoleRendererFacts
    {
        [Fact]
        public void RendersLine()
        {
            var line = new ConsoleLine() { TimeOffset = 0, Message = "test" };
            var builder = new StringBuilder();

            ConsoleRenderer.RenderLine(builder, line, new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            Assert.Equal("<div class=\"line\"><span data-moment-title=\"1451606400\">+ <1ms</span>test</div>", builder.ToString());
        }

        [Fact]
        public void RendersLine_WithOffset()
        {
            var line = new ConsoleLine() { TimeOffset = 1.108, Message = "test" };
            var builder = new StringBuilder();

            ConsoleRenderer.RenderLine(builder, line, new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            Assert.Equal("<div class=\"line\"><span data-moment-title=\"1451606401\">+1.108s</span>test</div>", builder.ToString());
        }

        [Fact]
        public void RendersLine_WithNegativeOffset()
        {
            var line = new ConsoleLine() { TimeOffset = -1.206, Message = "test" };
            var builder = new StringBuilder();

            ConsoleRenderer.RenderLine(builder, line, new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            Assert.Equal("<div class=\"line\"><span data-moment-title=\"1451606398\">-1.206s</span>test</div>", builder.ToString());
        }
        
        [Fact]
        public void RendersLine_WithColor()
        {
            var line = new ConsoleLine() { TimeOffset = 0, Message = "test", TextColor = "#ffffff" };
            var builder = new StringBuilder();

            ConsoleRenderer.RenderLine(builder, line, new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            Assert.Equal("<div class=\"line\" style=\"color:#ffffff\"><span data-moment-title=\"1451606400\">+ <1ms</span>test</div>", builder.ToString());
        }
    }
}
