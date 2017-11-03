using System;
using System.Runtime.InteropServices;

namespace LibSoundIOSharp
{
	public class SoundIOInStream : IDisposable
	{
		internal SoundIOInStream (Pointer<SoundIoInStream> handle)
		{
			this.handle = handle;
		}

		Pointer<SoundIoInStream> handle;

		public void Dispose ()
		{
			Natives.soundio_instream_destroy (handle);
		}

		public void Open ()
		{
			var ret = (SoundIoError) Natives.soundio_instream_open (handle);
			if (ret != SoundIoError.SoundIoErrorNone)
				throw new SoundIOException (ret);
		}

		public void Start ()
		{
			var ret = (SoundIoError)Natives.soundio_instream_start (handle);
			if (ret != SoundIoError.SoundIoErrorNone)
				throw new SoundIOException (ret);
		}

		public SoundIOChannelArea [] BeginWrite (ref int frameCount)
		{
			IntPtr ptrs = default (IntPtr);
			unsafe {
				var hptr = new IntPtr (frameCount);
				var ret = (SoundIoError)Natives.soundio_instream_begin_read (handle, ptrs, hptr);
				if (ret != SoundIoError.SoundIoErrorNone)
					throw new SoundIOException (ret);
				frameCount = *((int*) hptr);
				var s = Marshal.PtrToStructure<SoundIoInStream> (handle);
				var count = s.layout.channel_count;
				var results = new SoundIOChannelArea [count];
				var arr = (IntPtr*) ptrs;
				for (int i = 0; i < count; i++)
					results [i] = new SoundIOChannelArea (arr [i]);
				return results;
			}
		}

		public void EndWrite ()
		{
			var ret = (SoundIoError) Natives.soundio_instream_end_read (handle);
			if (ret != SoundIoError.SoundIoErrorNone)
				throw new SoundIOException (ret);
		}

		public void Pause (bool pause)
		{
			var ret = (SoundIoError) Natives.soundio_instream_pause (handle, pause ? 1 : 0);
			if (ret != SoundIoError.SoundIoErrorNone)
				throw new SoundIOException (ret);
		}

		public double GetLatency ()
		{
			unsafe {
				double* dptr = null;
				IntPtr p = new IntPtr (dptr);
				var ret = (SoundIoError) Natives.soundio_instream_get_latency (handle, p);
				if (ret != SoundIoError.SoundIoErrorNone)
					throw new SoundIOException (ret);
				dptr = (double*) p;
				return *dptr;
			}
		}
	}
}
