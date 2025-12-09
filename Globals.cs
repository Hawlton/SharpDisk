using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using IMAPI2;
using IMAPI2FS;


namespace CDCloser
{
    internal class Globals
    {
        public static string to_human_readable(long bytes)
        {
            string[] suffix = { "B", "KB", "MB", "GB" };
            int i = 0;
            double double_bytes = bytes;
            while (double_bytes >= 1024 && i < suffix.Length - 1)
            {
                double_bytes /= 1024;
                i++;
            }
            return $"{double_bytes:0.##} {suffix[i]}";
        }

        public static bool is_media_present(string drive_root)
        {
            if (String.IsNullOrWhiteSpace(drive_root)) return false;
            var letter = drive_root.Substring(0, 1);
            try
            {
                string query = $"SELECT MediaLoaded FROM Win32_CDROMDrive WHERE Drive = '{letter}:'";
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return (bool)obj["MediaLoaded"];
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"Error checking media presence: {ex.Message}");
            }
            return false;
        }
        
        //all this just to get optical media capacity
        public static long get_media_capacity(string drive_root)
        {
            if(String.IsNullOrWhiteSpace(drive_root)) return 0;
            char letter = drive_root[0];
            long total_sectors = 0;
            long total_bytes = 0;

            MsftDiscMaster2 disc_master = null;
            MsftDiscRecorder2 recorder = null;
            IDiscFormat2Data format_data = null;

            try
            {
                disc_master = new MsftDiscMaster2();
                if (!disc_master.IsSupportedEnvironment)
                {
                    Debug.WriteLine("IMAPI2 environment not supported.");
                    return 0;
                }
                foreach (string unique_id in disc_master)
                {
                    recorder = new MsftDiscRecorder2();
                    recorder.InitializeDiscRecorder(unique_id);
                    string[] paths;
                    
                    var object_array = recorder.VolumePathNames as object[];
                    paths = object_array?.Select(o => o.ToString()).ToArray() ?? Array.Empty<string>();
                    if (paths.Length == 0) continue;
                    if (paths[0][0] == letter)
                    {
                        format_data = new MsftDiscFormat2Data();
                        format_data.Recorder = recorder;

                        if (format_data.MediaHeuristicallyBlank)
                        {
                            total_sectors = format_data.TotalSectorsOnMedia;
                            total_bytes = total_sectors * 2048;
                            Debug.WriteLine($"Media Type: {format_data.CurrentPhysicalMediaType}");
                            Debug.WriteLine($"Total Sectors: {total_sectors}");
                            Debug.WriteLine($"Total Bytes: {total_bytes}");
                        }
                        else
                        {
                            Debug.WriteLine("Media is not blank.");
                        }
                        break;
                    }
                    Marshal.ReleaseComObject(recorder);
                    recorder = null;
                }
                if (total_sectors == 0) Debug.WriteLine("No matching recorder found or media not blank.");
                return total_bytes;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving media capacity: {ex.Message}");
                return 0;
            }
            finally
            {
                if (format_data != null) Marshal.ReleaseComObject(format_data);
                if (recorder != null) Marshal.ReleaseComObject(recorder);
                if (disc_master != null) Marshal.ReleaseComObject(disc_master);
            }

        }
        
    }
    public class SelectedFile
    {
        public string filename { get; init; } = string.Empty;
        public long byte_size { get; init; }
        public string path { get; init; } = string.Empty;
        public string readable_size => Globals.to_human_readable(byte_size);
    }
}
