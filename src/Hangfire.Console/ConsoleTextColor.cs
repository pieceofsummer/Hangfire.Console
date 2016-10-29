namespace Hangfire.Console
{
    /// <summary>
    /// Text color values
    /// </summary>
    public class ConsoleTextColor
    {
        /// <summary>
        /// The color black.
        /// </summary>
        public static readonly ConsoleTextColor Black = new ConsoleTextColor("#000000");

        /// <summary>
        /// The color dark blue.
        /// </summary>
        public static readonly ConsoleTextColor DarkBlue = new ConsoleTextColor("#000080");

        /// <summary>
        /// The color dark green.
        /// </summary>
        public static readonly ConsoleTextColor DarkGreen = new ConsoleTextColor("#008000");

        /// <summary>
        /// The color dark cyan (dark blue-green).
        /// </summary>
        public static readonly ConsoleTextColor DarkCyan = new ConsoleTextColor("#008080");

        /// <summary>
        /// The color dark red.
        /// </summary>
        public static readonly ConsoleTextColor DarkRed = new ConsoleTextColor("#800000");

        /// <summary>
        /// The color dark magenta (dark purplish-red).
        /// </summary>
        public static readonly ConsoleTextColor DarkMagenta = new ConsoleTextColor("#800080");

        /// <summary>
        /// The color dark yellow (ochre).
        /// </summary>
        public static readonly ConsoleTextColor DarkYellow = new ConsoleTextColor("#808000");

        /// <summary>
        /// The color gray.
        /// </summary>
        public static readonly ConsoleTextColor Gray = new ConsoleTextColor("#c0c0c0");

        /// <summary>
        /// The color dark gray.
        /// </summary>
        public static readonly ConsoleTextColor DarkGray = new ConsoleTextColor("#808080");
        
        /// <summary>
        /// The color blue.
        /// </summary>
        public static readonly ConsoleTextColor Blue = new ConsoleTextColor("#0000ff");

        /// <summary>
        /// The color green.
        /// </summary>
        public static readonly ConsoleTextColor Green = new ConsoleTextColor("#00ff00");

        /// <summary>
        ///  The color cyan (blue-green).
        /// </summary>
        public static readonly ConsoleTextColor Cyan = new ConsoleTextColor("#00ffff");

        /// <summary>
        /// The color red.
        /// </summary>
        public static readonly ConsoleTextColor Red = new ConsoleTextColor("#ff0000");

        /// <summary>
        /// The color magenta (purplish-red).
        /// </summary>
        public static readonly ConsoleTextColor Magenta = new ConsoleTextColor("#ff00ff");

        /// <summary>
        /// The color yellow.
        /// </summary>
        public static readonly ConsoleTextColor Yellow = new ConsoleTextColor("#ffff00");

        /// <summary>
        /// The color white.
        /// </summary>
        public static readonly ConsoleTextColor White = new ConsoleTextColor("#ffffff");
        
        private readonly string _color;

        private ConsoleTextColor(string color)
        {
            _color = color;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return _color;
        }

        /// <summary>
        /// Implicitly converts <see cref="ConsoleTextColor"/> to <see cref="string"/>.
        /// </summary>
        public static implicit operator string(ConsoleTextColor color) => color?._color;
    }
}
