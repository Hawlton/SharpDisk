using IMAPI2;
using IMAPI2FS;
using CDCloser;
using System.Runtime.InteropServices;
using static System.Windows.Forms.AxHost;
using System.Diagnostics;

using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace CDCloser
{
    public class BurnProgress
    {
        public int percent { get; init; }
        public uint sector { get; init; }
        public uint last_written_sector { get; init; }
        public uint remaining_sectors { get; init; }
        public bool completed { get; init; }
    }

    public class BurnLogic : IDisposable
    {
        private IDiscMaster2 disc_master;
        private IDiscFormat2Data disc_format;
        private IMAPI2.MsftDiscRecorder2 disc_recorder;
        private ConnectionPointCookie? event_cookie;
        private bool disposed;

        public BurnLogic()
        {
            if(Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                throw new InvalidOperationException("BurnLogic must be created on an STA thread.");
            }
            disc_master = new MsftDiscMaster2();
            disc_format = new MsftDiscFormat2Data();
        }

        //idk how this works either. I even tried hard interrupting the task and that doesn't seem to have any effect.
        Task run_sta_task(Action action, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource();
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                    tcs.SetResult();
                }
                catch (OperationCanceledException)
                {
                    tcs.SetCanceled(ct);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }

        public Dictionary<string, string> get_recorders()
        {
            ensure_not_disposed();
            MsftDiscRecorder2 temp_recorder = null;
            if (!disc_master.IsSupportedEnvironment) throw new InvalidOperationException("IMAPI2 is not supported on this environment");
            var recorder_dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach(string unique_id in disc_master)
            {
                temp_recorder = new MsftDiscRecorder2();
                temp_recorder.InitializeDiscRecorder(unique_id);

                string path;
                var obj_array = temp_recorder.VolumePathNames as object[];
                path = obj_array.FirstOrDefault()?.ToString();

                if (!String.IsNullOrEmpty(path))
                {
                    recorder_dict[path] = unique_id;
                }

                Marshal.ReleaseComObject(temp_recorder);
                temp_recorder = null;
            }
            return recorder_dict;

        }
        

        public void select_recorder(string unique_id)
        {
            ensure_not_disposed();
            if(disc_recorder != null)
            {
                try
                {
                    disc_recorder.ReleaseExclusiveAccess();
                }
                catch { }
                if (Marshal.IsComObject(disc_recorder)) Marshal.ReleaseComObject(disc_recorder);
            }
            disc_recorder = new MsftDiscRecorder2();
            disc_format = new MsftDiscFormat2Data();

            disc_recorder.InitializeDiscRecorder(unique_id);
            if (!disc_format.IsRecorderSupported(disc_recorder)) throw new InvalidOperationException("This recorder doesn't support data burning");
            disc_format.Recorder = disc_recorder;
            disc_format.ClientName = "SharpDisk";
        }


        public Task burn_files(List<SelectedFile> files, string volume_label, bool close_media, IProgress<BurnProgress> progress = null, CancellationToken ctoken = default)
        {
            ensure_not_disposed();
            if (disc_recorder == null) throw new InvalidOperationException("disc_recorder returned null");
            if (!files.All(file => File.Exists(file.path))) throw new InvalidOperationException("One of the provided files doesn't exist");

            string recorder_id = disc_recorder.ActiveDiscRecorder;

            return run_sta_task(() =>
            {
                ctoken.ThrowIfCancellationRequested();

                var local_recorder = new MsftDiscRecorder2();
                local_recorder.InitializeDiscRecorder(recorder_id);

                var local_format = new MsftDiscFormat2Data();
                local_format.Recorder = local_recorder;
                local_format.ClientName = "SharpDisk";

                IFileSystemImage fsi = new MsftFileSystemImage { VolumeName = volume_label, };
                ConnectionPointCookie local_cookie = null;

                if (!local_format.MediaPhysicallyBlank && !local_format.MediaHeuristicallyBlank) throw new InvalidOperationException("Media must be physically blank");
                if (!local_format.IsCurrentMediaSupported(local_recorder)) throw new InvalidOperationException("Current media is not supported");
                if (!local_format.IsRecorderSupported(local_recorder)) throw new InvalidOperationException("This recorder is not supported for data burning");
                IMAPI2.IMAPI_MEDIA_PHYSICAL_TYPE mediaType = local_format.CurrentPhysicalMediaType;

                if (mediaType == IMAPI2.IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_CDROM ||
                    mediaType == IMAPI2.IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_CDR ||
                    mediaType == IMAPI2.IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_CDRW)
                {
                    fsi.FileSystemsToCreate = FsiFileSystems.FsiFileSystemISO9660 | FsiFileSystems.FsiFileSystemJoliet;
                }
                else fsi.FileSystemsToCreate = FsiFileSystems.FsiFileSystemUDF;
                fsi.FreeMediaBlocks = local_format.FreeSectorsOnMedia;

                IFsiDirectoryItem root = fsi.Root;
                foreach (var item in files)
                {
                    ctoken.ThrowIfCancellationRequested();
                    root.AddTree(item.path, false);
                }

                IFileSystemImageResult result = fsi.CreateResultImage();
                IMAPI2.IStream image_stream = (IMAPI2.IStream)(object)result.ImageStream;
                local_format.ForceMediaToBeClosed = close_media;
                //Set write speed here later

                var sink = new DataFormatEventsSink(p => progress?.Report(p));
                local_cookie = ConnectionPointCookie.Advise(local_format, sink, typeof(DDiscFormat2DataEvents));

                try
                {
                    local_format.Write(image_stream);
                    progress?.Report(new BurnProgress
                    {
                        percent = 100,
                        completed = true,
                    });
                }
                finally
                {
                    local_cookie?.Unadvise();
                    local_cookie = null;

                    if(result != null && Marshal.IsComObject(result)) Marshal.ReleaseComObject(result);
                    if(result != null && Marshal.IsComObject(fsi)) Marshal.ReleaseComObject(fsi);
                    if (Marshal.IsComObject(local_format)) Marshal.ReleaseComObject(local_format);
                    if (Marshal.IsComObject(local_recorder)) Marshal.ReleaseComObject(local_recorder);
                    
                    //disc_recorder.ReleaseExclusiveAccess();
                }

            }, ctoken);

        }

        void ensure_not_disposed()
        {
            if (disposed) throw new ObjectDisposedException("BurnLogic");
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            try
            {
                disc_recorder?.ReleaseExclusiveAccess();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error releasing exclusive access: " + ex.Message);
            }

            if(event_cookie != null)
            {
                event_cookie.Unadvise();
                event_cookie = null;
            }

            if(disc_format != null && Marshal.IsComObject(disc_format)) Marshal.ReleaseComObject(disc_format);
            if(disc_recorder != null && Marshal.IsComObject(disc_recorder)) Marshal.ReleaseComObject(disc_recorder);
            if(disc_master != null && Marshal.IsComObject(disc_master)) Marshal.ReleaseComObject(disc_master);

            disc_format = null;
            disc_recorder = null;
            disc_master = null;
        }
    }


    //Everything under here is also pretty much a mystery. I've never done any sort of threading with an interop library like this before.
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public class DataFormatEventsSink : DDiscFormat2DataEvents
    {
        private readonly Action<BurnProgress> report;

        public DataFormatEventsSink(Action<BurnProgress> report) => this.report = report;
        public void Update(object sender, object progress)
        {
            Debug.WriteLine("Update called");
            if (progress is IDiscFormat2DataEventArgs args)
            {
                Debug.WriteLine($"Current Action: {args.CurrentAction}, Sector Count: {args.SectorCount}");
                uint total = (uint)args.SectorCount;
                uint written = (uint)args.LastWrittenLba + 1;
                int percent = total == 0 ? 0 : (int)Math.Min(100, (written * 100.0 / total));
                var burn_progress = new BurnProgress
                {
                    percent = percent,
                    sector = (uint)args.CurrentAction,
                    last_written_sector = written,
                    remaining_sectors = total > written ? total - written : 0
                };
                Debug.WriteLine($"Reporting Progress: {percent}%");
                report(burn_progress);
            }
            else
            {
                Debug.WriteLine($"Progress object type: {progress?.GetType().FullName ?? "null"}");
            }
        }
    }
    

    public class ConnectionPointCookie
    {
        private ComTypes.IConnectionPoint? con_point;
        private int cookie;

        private ConnectionPointCookie(ComTypes.IConnectionPoint cp, int cookie)
        {
            this.con_point = cp;
            this.cookie = cookie;
        }

        public static ConnectionPointCookie Advise(object source, object sink, Type event_interface)
        {
            if(source == null) throw new ArgumentNullException("source");
            if (sink == null) throw new ArgumentNullException("sink");
            if (!event_interface.IsInterface) throw new ArgumentException("Event interface is missing");

            var cpc = source as ComTypes.IConnectionPointContainer;
            if (cpc == null) throw new ArgumentException("Connection points not supported");

            Guid iid = event_interface.GUID;
            cpc.FindConnectionPoint(ref iid, out ComTypes.IConnectionPoint cp);
            cp.Advise(sink, out int cookie);
            return new ConnectionPointCookie(cp, cookie);
        }

        public void Unadvise()
        {
            if(con_point != null && cookie != 0)
            {
                con_point.Unadvise(cookie);
                cookie = 0;
                con_point = null;
            }
        }

    }
}
