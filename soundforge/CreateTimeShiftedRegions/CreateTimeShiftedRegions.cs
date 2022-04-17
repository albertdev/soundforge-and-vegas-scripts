using System;
using SoundForge;

public class EntryPoint
{
    public void FromSoundForge(IScriptableApp app)
    {
        ForgeApp = app;
        SfAudioMarkerList markers = app.CurrentFile.Markers;
        
        // All units except the time shifting are in samples
        AddIfMissing(markers, "reg001-7",            0,   139398096, -164);
        AddIfMissing(markers, "reg008",      139398096,   151327152, -122);
        AddIfMissing(markers, "reg009",      151327152,   160449024, -159);
        AddIfMissing(markers, "reg010-16",   160449024,   191386467, -315);
        AddIfMissing(markers, "reg017",      191386467,   195252224, -370);
        AddIfMissing(markers, "reg018",      195252224,   197153664, -315);
        AddIfMissing(markers, "reg019",      197153664,   204191067, -346);
        AddIfMissing(markers, "reg020-21",   204191067,   205714434, -608);
        AddIfMissing(markers, "reg022",      205714434,   210720000, -261);
        AddIfMissing(markers, "reg023-24",   210720000,   228201009, -220);
        AddIfMissing(markers, "reg025-27",   228201009,   233560000, -304);
        AddIfMissing(markers, "reg028-30",   233560000,   274571271, -261);
        AddIfMissing(markers, "reg031-35",   274571271,   293771025, -304);
        AddIfMissing(markers, "reg036-38",   293771025,   296020606, -260);
        AddIfMissing(markers, "reg039-44",   296020606,   310616736, -304);
        AddIfMissing(markers, "reg045-50",   310616736,   328136736, -358);
        AddIfMissing(markers, "reg051-54",   328136736,   368325981, -326);
    }

    // Adds regions to the project when their id seems to be missing.
    // For each request there will be two regions: the requested one and one where the id is "outXXX" and the boundaries have been timeshifted
    // The start and end parameters are in samples, the shift parameter uses milliseconds
    public static void AddIfMissing(SfAudioMarkerList markers, string name, long start, long end, int timeshift)
    {
        int i = 0;
        while (i < markers.Count && !markers[i].Name.Equals(name))
        {
            i++;
        }
        bool regionMissing = (i >= markers.Count);
        if (regionMissing)
        {
            markers.AddRegion(start, end - start, name);
        }
        
        // Check if the timeshifted version is needed and whether it exists
        if (timeshift != 0)
        {
            i = 0;
            string shiftedName = "out" + name;
            while (i < markers.Count && !markers[i].Name.Equals(shiftedName))
            {
                i++;
            }
            if (i < markers.Count && regionMissing)
            {
                // The original region was missing so we recreate the outXXX region
                markers.RemoveAt(i);
                i = markers.Count;
            }
            // Add timeshifted region
            if (i >= markers.Count)
            {
                long timeShiftInSamples = TimeSpan.FromMilliseconds(timeshift).Ticks * ForgeApp.CurrentFile.SampleRate / 10000000;
                // Calculate time-shifted bounds, then look for the closest zero-crossing to avoid pops
                long newStart = start - timeShiftInSamples;
                long newEnd = end - timeShiftInSamples;
                newStart = ForgeApp.CurrentFile.SnapPositionToZeroCrossing(newStart, 0, -1, 0);
                newEnd = ForgeApp.CurrentFile.SnapPositionToZeroCrossing(newEnd, 0, 1, 0);
                markers.AddRegion(newStart, newEnd - newStart, shiftedName);
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
