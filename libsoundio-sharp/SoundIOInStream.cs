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

		// Equality (based on handle)

		public override bool Equals (object other)
		{
			var d = other as SoundIOInStream;
			return d != null && (this.handle == d.handle);
		}

		public override int GetHashCode ()
		{
			return (int)(IntPtr)handle;
		}

		public static bool operator == (SoundIOInStream obj1, SoundIOInStream obj2)
		{
			return (object)obj1 == null ? (object)obj2 == null : obj1.Equals (obj2);
		}

		public static bool operator != (SoundIOInStream obj1, SoundIOInStream obj2)
		{
			return (object)obj1 == null ? (object)obj2 != null : !obj1.Equals (obj2);
		}

		// fields

		SoundIoInStream GetValue ()
		{
			return Marshal.PtrToStructure<SoundIoInStream> (handle);
		}

		public SoundIODevice Device {
			get { return new SoundIODevice (GetValue ().device); }
		}

		public SoundIOFormat Format {
			get { return (SoundIOFormat) GetValue ().format; }
		}

		public int SampleRate {
			get { return GetValue ().sample_rate; }
		}

		static readonly int layout_offset = (int) Marshal.OffsetOf<SoundIoInStream> ("layout");
		public SoundIOChannelLayout Layout {
			get { return new SoundIOChannelLayout ((IntPtr) handle + layout_offset); }
		}

		public double SoftwareLatency {
			get { return GetValue ().software_latency; }
		}

		// error_callback
		public Action ErrorCallback {
			get { return error_callback; }
			set {
				error_callback = value;
				error_callback_native = _ => error_callback ();
				var ptr = Marshal.GetFunctionPointerForDelegate (error_callback_native);
				Marshal.WriteIntPtr (handle, error_callback_offset, ptr);
			}
		}
		static readonly int error_callback_offset = (int)Marshal.OffsetOf<SoundIoInStream> ("error_callback");
		Action error_callback;
		delegate void error_callback_delegate (IntPtr handle);
		error_callback_delegate error_callback_native;

		// read_callback
		public Action<int,int> ReadCallback {
			get { return read_callback; }
			set {
				read_callback = value;
				read_callback_native = (_, minFrameCount, maxFrameCount) => read_callback (minFrameCount, maxFrameCount);
				var ptr = Marshal.GetFunctionPointerForDelegate (read_callback_native);
				Marshal.WriteIntPtr (handle, read_callback_offset, ptr);
			}
		}
		static readonly int read_callback_offset = (int)Marshal.OffsetOf<SoundIoInStream> ("read_callback");
		Action<int, int> read_callback;
		delegate void read_callback_delegate (IntPtr handle, int min, int max);
		read_callback_delegate read_callback_native;

		// overflow_callback
		public Action OverflowCallback {
			get { return overflow_callback; }
			set {
				overflow_callback = value;
				overflow_callback_native = _ => overflow_callback ();
				var ptr = Marshal.GetFunctionPointerForDelegate (overflow_callback_native);
				Marshal.WriteIntPtr (handle, overflow_callback_offset, ptr);
			}
		}
		static readonly int overflow_callback_offset = (int)Marshal.OffsetOf<SoundIoInStream> ("overflow_callback");
		Action overflow_callback;
		delegate void overflow_callback_delegate (IntPtr handle);
		overflow_callback_delegate overflow_callback_native;

		public string Name {
			get {
				var ptr = GetValue ().name;
				return ptr == IntPtr.Zero ? null : Marshal.PtrToStringAnsi (ptr);
			}
		}

		public bool NonTerminalHint {
			get { return GetValue ().non_terminal_hint != 0; }
		}

		public int BytesPerFrame {
			get { return GetValue ().bytes_per_frame; }
		}

		public int BytesPerSample {
			get { return GetValue ().bytes_per_sample; }
		}

		public string LayoutErrorMessage {
			get {
				var code = (SoundIoError) GetValue ().layout_error;
				return code == SoundIoError.SoundIoErrorNone ? null : Marshal.PtrToStringAnsi (Natives.soundio_strerror ((int) code));
			}
		}

		// functions

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

		public SoundIOChannelArea [] BeginRead (ref int frameCount)
		{
			IntPtr ptrs = default (IntPtr);
			unsafe {
				var hptr = new IntPtr (frameCount);
				var ret = (SoundIoError)Natives.soundio_instream_begin_read (handle, ptrs, hptr);
				if (ret != SoundIoError.SoundIoErrorNone)
					throw new SoundIOException (ret);
				frameCount = *((int*) hptr);
				var s = Marshal.PtrToStructure<SoundIoInStream> (handle);
				var count = Layout.ChannelCount;
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
