using SoundForge;
using System;
using System.Text.RegularExpressions;

public class EntryPoint
{
    public void FromSoundForge(IScriptableApp app)
    {
        ForgeApp = app;
        string offsetInput = SfHelpers.WaitForInputString("Enter relative time offset or start with @ for absolute offset:");
        bool absoluteTimeJump = offsetInput.StartsWith("@");
        if (absoluteTimeJump)
        {
            // Chop off '@' sign so further parsing can continue
            offsetInput = offsetInput.Substring(1);
        }

        TimeSpan jump;
        try
        {
            jump = ParseJump(offsetInput);
        }
        catch
        {
            app.SetStatusText("Time input could not be parsed");
            return;
        }

        long offsetDiff = TimeSpanToSamples(app.ActiveWindow.File.SampleRate, jump);
        long currentOffset = 0;
        if (!absoluteTimeJump)
        {
            currentOffset = app.ActiveWindow.Cursor;
        }

        // Sum it up and clip value so that we don't get a negative samples value
        long newOffset = Math.Max(0, currentOffset + offsetDiff);

        app.ActiveWindow.SetCursorAndScroll(newOffset, DataWndScrollTo.NoMove);
        
        // Fetch value again to see if we hit the end
        newOffset = app.ActiveWindow.Cursor;

        // Personal preference: set status text to current HH:MM:SS,fff time, no matter what the current time display is
        ISfPositionFormatter formatter = app.ActiveWindow.Formatter;
        string newOffsetResult = formatter.Format(PositionFormatType.Time, newOffset, false);
        app.SetStatusText("Now at " + newOffsetResult);
    }

    public static long TimeSpanToSamples(uint sampleRate, TimeSpan offset)
    {
        // 1 tick = 1 / 10 000 of a millisecond or 100 nanoseconds
        long ticks = offset.Ticks;
        return sampleRate * ticks / 10000000;
    }

    public static TimeSpan ParseJump(string offsetInput)
    {
        bool moveToTheLeft = offsetInput.StartsWith("-");
        if (moveToTheLeft)
        {
            // Chop off '-' sign so further parsing can continue
            offsetInput = offsetInput.Substring(1);
        }

        TimeSpan jump = TimeSpan.Zero;
        // "s" suffix denotes seconds
        if (Regex.IsMatch(offsetInput, "^[0-9]+s$"))
        {
            jump = TimeSpan.FromSeconds(Int32.Parse(CutLastChars(1, offsetInput)));
        }
        else if (offsetInput.Contains(":") || offsetInput.Contains(","))
        {
            jump = ParseHoursMinutesSecondsAndMillis(offsetInput);
        }
        else
        {
            int milliSecondsValue;
            if (Int32.TryParse(offsetInput, out milliSecondsValue))
            {
                jump = TimeSpan.FromMilliseconds(milliSecondsValue);
            }
            else
            {
                throw new ArgumentException("Unrecognized input format");
            }
        }

        if (moveToTheLeft)
        {
            return jump.Negate();
        }
        return jump;
    }

    // Hours:Minutes:Seconds,millis (note: millis should either have 3 digits or be considered left-padded with zeroes)
    // This means "1,5" means 1 second + 5 millis, not one-and-a-half second
    private static TimeSpan ParseHoursMinutesSecondsAndMillis(string offsetInput)
    {
        TimeSpan jump = TimeSpan.Zero;
        // Either hours or minutes were found
        if (Regex.IsMatch(offsetInput, "^[0-9]+:"))
        {
            int colonOffset = offsetInput.IndexOf(":");
            jump = TimeSpan.FromMinutes(Int32.Parse(offsetInput.Substring(0, colonOffset)));
            offsetInput = offsetInput.Substring(colonOffset + 1);
        }

        // If there's still a colon then we found a minutes part
        if (Regex.IsMatch(offsetInput, "^[0-9]+:"))
        {
            jump = TimeSpan.FromTicks(jump.Ticks * 60); // If there was an hours component earlier on

            int colonOffset = offsetInput.IndexOf(":");
            jump = jump.Add(TimeSpan.FromMinutes(Int32.Parse(offsetInput.Substring(0, colonOffset))));
            offsetInput = offsetInput.Substring(colonOffset + 1);
        }
        else if (offsetInput.StartsWith(":"))
        {
            jump = TimeSpan.FromTicks(jump.Ticks * 60); // If there was an hours component earlier on
            offsetInput = offsetInput.Substring(1);
        }

        string seconds = String.Empty;
        string milliSeconds = String.Empty;
        if (Regex.IsMatch(offsetInput, "^[0-9]+,"))
        {
            int commaOffset = offsetInput.IndexOf(",");
            seconds = offsetInput.Substring(0, commaOffset);
            offsetInput = offsetInput.Substring(commaOffset + 1);
        }
        else if (Regex.IsMatch(offsetInput, "^[0-9]+$"))
        {
            seconds = offsetInput;
            offsetInput = String.Empty;
        }
        else if (offsetInput.StartsWith(","))
        {
            offsetInput = offsetInput.Substring(1);
        }

        milliSeconds = offsetInput;

        if (!String.IsNullOrWhiteSpace(seconds))
        {
            jump = jump.Add(TimeSpan.FromSeconds(Int32.Parse(seconds)));
        }

        if (!String.IsNullOrWhiteSpace(milliSeconds))
        {
            jump = jump.Add(TimeSpan.FromMilliseconds(Int32.Parse(milliSeconds)));
        }

        return jump;
    }

    public static IScriptableApp ForgeApp = null;
    public static void DPF(string sz) { ForgeApp.OutputText(sz); }
    public static void DPF(string fmt, object o) { ForgeApp.OutputText(String.Format(fmt, o)); }
    public static void DPF(string fmt, object o, object o2) { ForgeApp.OutputText(String.Format(fmt, o, o2)); }
    public static void DPF(string fmt, object o, object o2, object o3) { ForgeApp.OutputText(String.Format(fmt, o, o2, o3)); }
    public static string GETARG(string k, string d) { string val = Script.Args.ValueOf(k); if (val == null || val.Length == 0) val = d; return val; }
    public static int GETARG(string k, int d) { string s = Script.Args.ValueOf(k); if (s == null || s.Length == 0) return d; else return Script.Args.AsInt(k); }
    public static bool GETARG(string k, bool d) { string s = Script.Args.ValueOf(k); if (s == null || s.Length == 0) return d; else return Script.Args.AsBool(k); }

    public static string CutLastChars(int number, string input)
    {
        return input.Substring(0, input.Length - number);
    }
}
