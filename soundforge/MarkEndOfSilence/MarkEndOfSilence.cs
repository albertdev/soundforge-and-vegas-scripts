using SoundForge;
using System;
using System.Text.RegularExpressions;

public class EntryPoint
{
    public void FromSoundForge(IScriptableApp app)
    {
        ISfFileHost file = app.CurrentFile;
        ForgeApp = app;

        // How silent the file must get, from 0% to 100% (in effect -96 dB to 0dB)
        // Seems like 0.000001 is still sensitive, but it's not known what dB value that maps to.
        const double audioThreshold = 0.0;
        // How much seconds or milliseconds of silence should be left before the start of the next track.
        long silentLeadTime = file.SecondsToPosition(0.1);

        SfAudioSelection leftToSearch = new SfAudioSelection(file);

        long fileLength = file.Length;
        long previousMatch = -1;
        long endOfSilence = file.FindAudioAbove(leftToSearch, audioThreshold, true);

        int idUndo = file.BeginUndo("Mark End Of Silence");

        // Not sure whether FindAudioAbove ever returns e.g. a negative value when it is at the end of the file.
        if (endOfSilence > previousMatch)
        {
            previousMatch = endOfSilence;
            // Make sure we don't go to a negative offset
            long markerPosition = Math.Max(endOfSilence - silentLeadTime, 0);
            file.NewMarker(new SfAudioMarker(markerPosition));

            leftToSearch = new SfAudioSelection(endOfSilence, fileLength - endOfSilence);
        }
        else
        {
            leftToSearch = new SfAudioSelection(0, 0);
        }

        while (leftToSearch.Length > 0)
        {
            endOfSilence = file.FindAudioAbove(leftToSearch, audioThreshold, true);
            if (endOfSilence > previousMatch)
            {
                previousMatch = endOfSilence;
                // Make sure we don't go to a negative offset
                long markerPosition = Math.Max(endOfSilence - silentLeadTime, 0);
                file.NewMarker(new SfAudioMarker(markerPosition));

                leftToSearch = new SfAudioSelection(endOfSilence, fileLength - endOfSilence);
            }
            else
            {
                leftToSearch = new SfAudioSelection(0, 0);
            }
        }

        file.EndUndo(idUndo, false);
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
