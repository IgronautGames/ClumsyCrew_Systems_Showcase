using System.Text;

namespace ClumsyCrew.UI
{
    /// <summary>
    /// Efficient string formatting utilities for UI display.
    /// 
    /// Includes:
    /// - Time and countdown formatting
    /// - FPS formatting
    /// - Large number abbreviation (1.2K, 3.5M, etc.)
    /// - Daytime (AM/PM) formatting
    /// 
    /// Uses pooled StringBuilder and preallocated string arrays to minimize GC allocations.
    /// </summary>
    public static class UIFormatters
    {
        static StringBuilder builder;
        static readonly string emprtyString = "";
        static readonly string coma = ".";
        static readonly string fps = "fps";
        static readonly string minus = "-";

        static readonly string amString = "AM";
        static readonly string pmString = "PM";
        static readonly string dString = "d";
        static readonly string hString = "h";
        static readonly string mString = "m";
        static readonly string sString = "s";

        static readonly string[] stringNumbers = new string[1000];
        static readonly string[] stringLetters = new string[23]
        {
            "", "K", "M", "B", "T", "q", "Q", "s", "S", "O", "N", "d", "D",
            "!", "@", "#", "$", "%", "^", "&", "*", "1000*", "e"
        };

        static UIFormatters()
        {
            // Pre-generate all numbers up to 999 for instant lookup
            for (int i = 0; i < 1000; i++)
                stringNumbers[i] = i.ToString();
        }

        #region Time Formatting
        /// <summary>
        /// Formats seconds into readable time strings.
        /// Type 1: "5s" / "3m" / "2h" / "1d"
        /// Type 2: "1h45m" / "3m22s"
        /// Type 3: "1d2h30m10s"
        /// </summary>
        public static string FormatToTime(int seconds, int types)
        {
            builder ??= new StringBuilder(20);
            builder.Length = 0;

            int minutes = 0, hours = 0, days = 0;
            if (seconds > 60)
            {
                minutes = seconds / 60;
                seconds -= minutes * 60;

                if (minutes > 60)
                {
                    hours = minutes / 60;
                    minutes -= hours * 60;

                    if (hours > 24)
                    {
                        days = hours / 24;
                        hours -= days * 24;
                    }
                }
            }

            switch (types)
            {
                case 1: // Single unit
                    AppendTimeUnit(days, hours, minutes, seconds, 1);
                    break;

                case 2: // Two-unit (e.g. 1h30m)
                    AppendTimeUnit(days, hours, minutes, seconds, 2);
                    break;

                case 3: // Full (e.g. 1d5h20m10s)
                    AppendTimeUnit(days, hours, minutes, seconds, 3);
                    break;
            }

            return builder.ToString();
        }

        private static void AppendTimeUnit(int d, int h, int m, int s, int mode)
        {
            if (mode == 1)
            {
                if (d > 0) builder.Append($"{stringNumbers[d]}{dString}");
                else if (h > 0) builder.Append($"{stringNumbers[h]}{hString}");
                else if (m > 0) builder.Append($"{stringNumbers[m]}{mString}");
                else builder.Append($"{stringNumbers[s]}{sString}");
            }
            else if (mode == 2)
            {
                if (d > 0) builder.Append($"{stringNumbers[d]}{dString}{stringNumbers[h]}{hString}");
                else if (h > 0) builder.Append($"{stringNumbers[h]}{hString}{stringNumbers[m]}{mString}");
                else builder.Append($"{stringNumbers[m]}{mString}{stringNumbers[s]}{sString}");
            }
            else
            {
                if (d > 0) builder.Append($"{stringNumbers[d]}{dString}");
                builder.Append($"{stringNumbers[h]}{hString}{stringNumbers[m]}{mString}");
                if (d <= 0) builder.Append($"{stringNumbers[s]}{sString}");
            }
        }
        #endregion

        #region Other Formatters
        public static string FormatToDayTime(int hours)
        {
            builder ??= new StringBuilder(20);
            builder.Length = 0;

            builder.Append(hours > 11 ? stringNumbers[hours - 12] : stringNumbers[hours]);
            builder.Append(hours > 11 ? pmString : amString);
            return builder.ToString();
        }

        public static string FormatFPS(int fpsNumb)
        {
            builder ??= new StringBuilder(10);
            builder.Length = 0;
            builder.Append(fpsNumb).Append(fps);
            return builder.ToString();
        }

        public static string FormatNumb(int numb)
        {
            builder ??= new StringBuilder(20);
            builder.Length = 0;

            float firstNumb = numb;
            int decimals = 0;

            while (firstNumb >= 1000)
            {
                firstNumb /= 1000;
                decimals++;
            }

            bool withDecimals = false;
            int decimalNumb = 0;
            float decimalN = firstNumb % (int)firstNumb;
            if (firstNumb < 10 && decimalN >= 0.1f)
            {
                decimalNumb = (int)(decimalN * 10);
                withDecimals = true;
            }

            if (firstNumb < 0)
            {
                builder.Append(minus);
                firstNumb = System.MathF.Abs(firstNumb);
            }

            builder.Append(stringNumbers[(int)firstNumb]);
            if (withDecimals && decimalNumb > 0)
                builder.Append(coma).Append(stringNumbers[decimalNumb]);

            if (decimals < 22)
                builder.Append(stringLetters[decimals]);
            else
                builder.Append(stringLetters[22]).Append(decimals * 3);

            return builder.ToString();
        }
        #endregion
    }
}
