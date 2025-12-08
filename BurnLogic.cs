using IMAPI2;
using IMAPI2FS;
using CDCloser;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using static System.Windows.Forms.AxHost;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.CodeDom;
using System.Collections.Generic;

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
        Task run_sta_task(Func<Task> action, CancellationToken ct)
        {
            return action();
        }

        public string[] list_recorders()
        {
            ensure_not_disposed();
            if (!disc_master.IsSupportedEnvironment) throw new InvalidOperationException("IMAPI2 is not supported on this system.");
            var list = new string[disc_master.Count];
            for(int i = 0; i < disc_master.Count; i++)
            {
                list[i] = disc_master[i];
            }
            return list;
        }

        public void select_recorder(string unique_id)
        {
            ensure_not_disposed();
            disc_recorder?.ReleaseExclusiveAccess();
            disc_recorder = new MsftDiscRecorder2();
            disc_recorder.InitializeDiscRecorder(unique_id);
            disc_format.Recorder = disc_recorder;
            disc_format.ClientName = "OptimalDisk";
        }

        public Task burn_files(List<SelectedFile> files, string volume_label, IProgress<BurnProgress> progress = null, CancellationToken ctoken = default)
        {
            ensure_not_disposed();
            if (disc_recorder == null) throw new InvalidOperationException("disc_recorder returned null");
            if (!files.All(file => File.Exists(file.path))) throw new InvalidOperationException("One of the provided files doesn't exist");

            return run_sta_task(async () =>
            {
                var streams_to_dispose = new List<ManagedStream>();
                ctoken.ThrowIfCancellationRequested();
                IFileSystemImage fsi = new MsftFileSystemImage
                {
                    VolumeName = volume_label,
                    FileSystemsToCreate = FsiFileSystems.FsiFileSystemISO9660 | FsiFileSystems.FsiFileSystemJoliet | FsiFileSystems.FsiFileSystemUDF
                };

                IFsiDirectoryItem root = fsi.Root;
                foreach (var item in files)
                {
                    ctoken.ThrowIfCancellationRequested();
                    var fs = new FileStream(item.path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    var istream = new CDCloser.ManagedStream(fs);
                    root.AddFile(item.filename, (FsiStream)(object)istream);
                    streams_to_dispose.Add(istream);
                }

                IFileSystemImageResult result = fsi.CreateResultImage();
                IMAPI2.IStream image_stream = (IMAPI2.IStream)(object)result.ImageStream;

                var sink = new DataFormatEventsSink(p => progress?.Report(p));
                event_cookie = ConnectionPointCookie.Advise(disc_format, sink, typeof(DDiscFormat2DataEvents));

                try
                {
                    disc_format.Write(image_stream);
                    progress?.Report(new BurnProgress
                    {
                        percent = 100,
                        completed = true,
                    });
                }
                finally
                {
                    event_cookie?.Unadvise();
                    event_cookie = null;

                    if(result != null && Marshal.IsComObject(result)) Marshal.ReleaseComObject(result);
                    if(result != null && Marshal.IsComObject(fsi)) Marshal.ReleaseComObject(fsi);
                    foreach(var stream in streams_to_dispose)
                    {
                        stream.Dispose();
                    }


                }
                await Task.CompletedTask;

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

    public class DataFormatEventsSink : DDiscFormat2DataEvents
    {
        private readonly Action<BurnProgress> report;

        public DataFormatEventsSink(Action<BurnProgress> report) => this.report = report;
        public void Update(object sender, object progress)
        {
            if (progress is IDiscFormat2DataEventArgs args)
            {
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
                report(burn_progress);
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
