using System;
using SoundForge;

/// <summary>
/// Deletes markers but leaves regions intact. Backup your Regions list first!
/// </summary>
public class EntryPoint
{
    public void FromSoundForge(IScriptableApp app)
    {
        ForgeApp = app;
        SfAudioMarkerList markers = app.CurrentFile.Markers;

        // Iterate in reverse because the list will automatically shrink
        for (int i = markers.Count - 1; i >= 0; i--) {
            SfAudioMarker marker = markers[i];
            if (! marker.IsRegion) {
                markers.Remove(marker);
            }
        }
    }
    
    public static IScriptableApp ForgeApp = null;
    public static void DPF(string sz) { ForgeApp.OutputText(sz); }
    public static void DPF(string fmt, object o) { ForgeApp.OutputText(String.Format(fmt, o)); }
    public static void DPF(string fmt, object o, object o2) { ForgeApp.OutputText(String.Format(fmt, o, o2)); }
    public static void DPF(string fmt, object o, object o2, object o3) { ForgeApp.OutputText(String.Format(fmt, o, o2, o3)); }
    public static string GETARG(string k, string d) { string val = Script.Args.ValueOf(k); if (val == null || val.Length == 0) val = d; return val; }
    public static int GETARG(string k, int d) { string s = Script.Args.ValueOf(k); if (s == null || s.Length == 0) return d; else return Script.Args.AsInt(k); }
    public static bool GETARG(string k, bool d) { string s = Script.Args.ValueOf(k); if (s == null || s.Length == 0) return d; else return Script.Args.AsBool(k); }
}
