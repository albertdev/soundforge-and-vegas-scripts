using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using ScriptPortal.Vegas;
using System.Windows.Forms;
using System.Text.RegularExpressions;

public class EntryPoint
{
    TextBox InputBox = new TextBox();
    Button okButton = new Button();

    public void FromVegas(Vegas vegas)
    {
        App = vegas;
        string offsetInput = WaitForInputString("Enter relative time offset:");

        if (String.IsNullOrWhiteSpace(offsetInput))
        {
            return;
        }

        bool snapToFrames = true;
        if (offsetInput.StartsWith("*"))
        {
            snapToFrames = false;
            offsetInput = offsetInput.Substring(1);
        }

        TimeSpan jump;
        try
        {
            jump = ParseJump(offsetInput);
        }
        catch
        {
            App.ShowError("Time input could not be parsed");
            return;
        }

        if (snapToFrames)
        {
            //double frameDuration = 1.0 / App.Project.Video.FrameRate;
            long currentFrame = App.Transport.CursorPosition.FrameCount;

            long numFramesToMove =  Timecode.FromNanos(jump.Ticks).FrameCount;
            if (numFramesToMove == 0)
            {
                numFramesToMove = jump.Ticks > 0 ? 1 : -1;
            }

            App.Transport.CursorPosition = ProjectTimecode.FromFrames(App.Project, currentFrame + numFramesToMove);
        }
        else
        {
            // Sum it up and clip value so that we don't get a negative offset (because jump can be negative).
            // Ticks is the same unit as what VEGAS calls "nanos": 100 nanoseconds or 1/10 000 of a millisecond.
            long currentOffset = App.Transport.CursorPosition.Nanos;
            long newOffset = Math.Max(0, currentOffset + jump.Ticks);
            App.Transport.CursorPosition = ProjectTimecode.FromPositionNanos(App.Project, newOffset);
        }
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

    public static string CutLastChars(int number, string input)
    {
        return input.Substring(0, input.Length - number);
    }

    private string WaitForInputString(string message)
    {
        // Code based on "Render Audio Tracks.cs" from default VEGAS scripts
        Form form = new Form();
        form.SuspendLayout();
        form.AutoScaleMode = AutoScaleMode.Font;
        form.AutoScaleDimensions = new SizeF(6F, 13F);
        form.FormBorderStyle = FormBorderStyle.FixedDialog;
        form.StartPosition = FormStartPosition.CenterParent;
        form.MaximizeBox = false;
        form.MinimizeBox = false;
        form.HelpButton = false;
        form.ShowInTaskbar = false;
        form.Text = "Waiting for Input";
        form.AutoSize = true;
        form.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        TableLayoutPanel layout = new TableLayoutPanel();
        layout.AutoSize = true;
        layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        layout.GrowStyle = TableLayoutPanelGrowStyle.AddRows;
        layout.ColumnCount = 3;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        form.Controls.Add(layout);

        Label label = new Label();
        label.Text = message;
        label.AutoSize = false;
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.Margin = new Padding(8, 8, 8, 4);
        label.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        layout.Controls.Add(label);
        layout.SetColumnSpan(label, 3);

        InputBox.Text = "";
        InputBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        InputBox.Margin = new Padding(8, 8, 8, 4);
        layout.Controls.Add(InputBox);
        layout.SetColumnSpan(InputBox, 3);

        FlowLayoutPanel buttonPanel = new FlowLayoutPanel();
        buttonPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonPanel.Size = Size.Empty;
        buttonPanel.AutoSize = true;
        buttonPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        buttonPanel.Margin = new Padding(8, 8, 8, 8);
        buttonPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        layout.Controls.Add(buttonPanel);
        layout.SetColumnSpan(buttonPanel, 3);

        Button cancelButton = new Button();
        cancelButton.Text = "Cancel";
        cancelButton.FlatStyle = FlatStyle.System;
        cancelButton.DialogResult = DialogResult.Cancel;
        buttonPanel.Controls.Add(cancelButton);
        form.CancelButton = cancelButton;

        okButton.Text = "OK";
        okButton.FlatStyle = FlatStyle.System;
        okButton.DialogResult = DialogResult.OK;
        buttonPanel.Controls.Add(okButton);
        form.AcceptButton = okButton;

        form.ResumeLayout();

        DialogResult result = form.ShowDialog(App.MainWindow);
        if (DialogResult.OK == result)
        {
            return InputBox.Text;
        }
        else
        {
            return null;
        }
    }

    private Vegas _app;
    private Vegas App
    {
        get
        {
            return _app;
        }
        set
        {
            _app = value;
        }
    }
}
