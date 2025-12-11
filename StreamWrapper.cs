using System;
using System.CodeDom;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace CDCloser
{
    //Wraps System.IO.Stream in COM IStream
    //this entire class is pretty much useless for this application
    public class ManagedStream: IStream, IDisposable
    {
        private Stream stream;
        private bool disposed;

        public ManagedStream(Stream _stream)
        {
            stream = _stream ?? throw new ArgumentNullException(nameof(_stream));
        } 

        public void Read(byte[] buffer, int bytes_requested, IntPtr bytes_read_ptr)
        {
            ensure_not_disposed();
            int bytes_read = stream.Read(buffer, 0, bytes_requested);
            if (bytes_read_ptr != IntPtr.Zero) Marshal.WriteInt32(bytes_read_ptr, bytes_read);
            
        }

        public void Write(byte[] source_buffer, int bytes_to_write, IntPtr bytes_written_ptr)
        {
            ensure_not_disposed();
            stream.Write(source_buffer, 0, bytes_to_write);
            if (bytes_written_ptr != IntPtr.Zero) Marshal.WriteInt32(bytes_written_ptr, bytes_to_write);
            
        }

        public void Seek(long offset, int origin, IntPtr new_position_ptr)
        {
            ensure_not_disposed();
            long new_position = stream.Seek(offset, (SeekOrigin)origin);
            if(new_position_ptr != IntPtr.Zero) Marshal.WriteInt64(new_position_ptr, new_position);
        }

        public void SetSize(long new_size)
        {
            ensure_not_disposed();
            stream.SetLength(new_size);
        }
        
        public void CopyTo(IStream destination_stream, long bytes_to_copy, IntPtr total_bytes_read_ptr, IntPtr total_bytes_written_ptr)
        {
            ensure_not_disposed();
            if (destination_stream is null) throw new ArgumentNullException(nameof(destination_stream));
            if (bytes_to_copy < 0) throw new ArgumentOutOfRangeException(nameof(bytes_to_copy));

            const int buffer_size_bytes = 64 * 1024;
            byte[] transfer_buffer = new byte[buffer_size_bytes];

            long remaining_bytes = bytes_to_copy;
            long total_bytes_read = 0;
            long total_bytes_written = 0;

            while(remaining_bytes > 0)
            {
                int read_request = (int)Math.Min(buffer_size_bytes, remaining_bytes);
                int bytes_read_this_cycle = stream.Read(transfer_buffer, 0, read_request);
                if (bytes_read_this_cycle <= 0) break;

                total_bytes_read += bytes_read_this_cycle;
                remaining_bytes -= bytes_read_this_cycle;

                destination_stream.Write(transfer_buffer, bytes_read_this_cycle, IntPtr.Zero);
                total_bytes_written += bytes_read_this_cycle;
            }

            if(total_bytes_read_ptr != IntPtr.Zero)
            {
                Marshal.WriteInt64(total_bytes_read_ptr, total_bytes_read);
            }

            if(total_bytes_written_ptr != IntPtr.Zero)
            {
                Marshal.WriteInt64(total_bytes_written_ptr, total_bytes_written);
            }

        }

        public void Clone(out IStream ppstm)
        {
            ensure_not_disposed();
            if(!stream.CanSeek) throw new NotSupportedException("Underlying stream does not support seeking, cannot clone");

            long original_position = stream.Position;
            Stream clone_stream = new MemoryStream();
            stream.Position = 0;
            stream.CopyTo(clone_stream);
            stream.Position = original_position;
            clone_stream.Position = 0;
            ppstm = new ManagedStream(clone_stream);

        }

        public void Stat(out STATSTG stat_pointer, int stat_flags)
        {
            ensure_not_disposed();
            stat_pointer = new STATSTG();
            stat_pointer.cbSize = stream.Length;
            stat_pointer.type = 2; 
            stat_pointer.grfMode = 0;
            stat_pointer.grfLocksSupported = 0;
        }

        public void Revert()
        {
            //placeholder so error goes away
        }

        public void LockRegion(long offset, long width, int lock_type)
        {
            throw new NotSupportedException("LockRegion is not supported");
        }

        public void UnlockRegion(long offset, long width, int lock_type)
        {
            throw new NotSupportedException("UnlockRegion is not supported");
        }


        public void Commit(int commit_flags)
        {
            ensure_not_disposed();
            stream.Flush();
        }


        private void ensure_not_disposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(ManagedStream));
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            stream?.Dispose();
            stream = null;
        }


    }
}
