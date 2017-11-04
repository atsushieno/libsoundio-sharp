using System;
using System.Runtime.InteropServices;

namespace LibSoundIOSharp
{
	public class SoundIOOutStream : IDisposable
	{
		internal SoundIOOutStream (Pointer<SoundIoOutStream> handle)
		{
			this.handle = handle;
		}

		Pointer<SoundIoOutStream> handle;

		public void Dispose ()
		{
			Natives.soundio_outstream_destroy (handle);
		}
		// Equality (based on handle)

		public override bool Equals (object other)
		{
			var d = other as SoundIOOutStream;
			return d != null && (this.handle == d.handle);
		}

		public override int GetHashCode ()
		{
			return (int)(IntPtr)handle;
		}

		public static bool operator == (SoundIOOutStream obj1, SoundIOOutStream obj2)
		{
			return (object)obj1 == null ? (object)obj2 == null : obj1.Equals (obj2);
		}

		public static bool operator != (SoundIOOutStream obj1, SoundIOOutStream obj2)
		{
			return (object)obj1 == null ? (object)obj2 != null : !obj1.Equals (obj2);
		}

		// functions

		public void Open ()
		{
			var ret = (SoundIoError) Natives.soundio_outstream_open (handle);
			if (ret != SoundIoError.SoundIoErrorNone)
				throw new SoundIOException (ret);
		}

		public void Start ()
		{
			var ret = (SoundIoError)Natives.soundio_outstream_start (handle);
			if (ret != SoundIoError.SoundIoErrorNone)
				throw new SoundIOException (ret);
		}

		public SoundIOChannelArea [] BeginWrite (ref int frameCount)
		{
			IntPtr ptrs = default (IntPtr);
			unsafe {
				var hptr = new IntPtr (frameCount);
				var ret = (SoundIoError)Natives.soundio_outstream_begin_write (handle, ptrs, hptr);
				if (ret != SoundIoError.SoundIoErrorNone)
					throw new SoundIOException (ret);
				frameCount = *((int*) hptr);
				var s = Marshal.PtrToStructure<SoundIoOutStream> (handle);
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
			var ret = (SoundIoError) Natives.soundio_outstream_end_write (handle);
			if (ret != SoundIoError.SoundIoErrorNone)
				throw new SoundIOException (ret);
		}

		public void ClearBuffer ()
		{
			Natives.soundio_outstream_clear_buffer (handle);
		}

		public void Pause (bool pause)
		{
			var ret = (SoundIoError) Natives.soundio_outstream_pause (handle, pause ? 1 : 0);
			if (ret != SoundIoError.SoundIoErrorNone)
				throw new SoundIOException (ret);
		}

		public double GetLatency ()
		{
			unsafe {
				double* dptr = null;
				IntPtr p = new IntPtr (dptr);
				var ret = (SoundIoError) Natives.soundio_outstream_get_latency (handle, p);
				if (ret != SoundIoError.SoundIoErrorNone)
					throw new SoundIOException (ret);
				dptr = (double*) p;
				return *dptr;
			}
		}
	}
}
