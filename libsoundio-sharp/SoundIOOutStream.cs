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
			set { Marshal.WriteInt32 ((IntPtr) handle + format_offset, (int) value); }
		}
		static readonly int format_offset = (int)Marshal.OffsetOf<SoundIoOutStream> ("format");

		public int SampleRate {
			get { return GetValue ().sample_rate; }
			set { Marshal.WriteInt32 ((IntPtr) handle + sample_rate_offset, value); }
		}
		static readonly int sample_rate_offset = (int)Marshal.OffsetOf<SoundIoOutStream> ("sample_rate");

		static readonly int layout_offset = (int) Marshal.OffsetOf<SoundIoOutStream> ("layout");
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
				if (value == null)
					error_callback_native = null;
				else
					error_callback_native = stream => error_callback ();
				var ptr = Marshal.GetFunctionPointerForDelegate (error_callback_native);
				Marshal.WriteIntPtr (handle, error_callback_offset, ptr);
			}
		}
		static readonly int error_callback_offset = (int)Marshal.OffsetOf<SoundIoOutStream> ("error_callback");
		Action error_callback;
		Action<SoundIoOutStream> error_callback_native;

		// write_callback
		public Action<int, int> WriteCallback {
			get { return write_callback; }
			set {
				write_callback = value;
				if (value == null)
					write_callback_native = null;
				else
					write_callback_native = (h, frame_count_min, frame_count_max) => write_callback (frame_count_min, frame_count_max);
				var ptr = Marshal.GetFunctionPointerForDelegate (write_callback_native);
				Marshal.WriteIntPtr (handle, write_callback_offset, ptr);
			}
		}
		static readonly int write_callback_offset = (int)Marshal.OffsetOf<SoundIoOutStream> ("write_callback");
		Action<int, int> write_callback;
		Action<SoundIoOutStream, int, int> write_callback_native;

		// underflow_callback
		public Action UnderflowCallback {
			get { return underflow_callback; }
			set {
				underflow_callback = value;
				if (value == null)
					underflow_callback_native = null;
				else
					underflow_callback_native = h => underflow_callback ();
				var ptr = Marshal.GetFunctionPointerForDelegate (underflow_callback_native);
				Marshal.WriteIntPtr (handle, underflow_callback_offset, ptr);
			}
		}
		static readonly int underflow_callback_offset = (int)Marshal.OffsetOf<SoundIoOutStream> ("underflow_callback");
		Action underflow_callback;
		Action<SoundIoOutStream> underflow_callback_native;

		public string Name {
			get {
				var ptr = GetValue ().name;
				return ptr == IntPtr.Zero ? null : Marshal.PtrToStringAuto (ptr); 
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
				var size = Marshal.SizeOf<SoundIoDevice> ();
				var ret = (SoundIoError)soundio_outstream_begin_write (handle, out ptrs, ref frameCount);
				if (ret != SoundIoError.SoundIoErrorNone)
					throw new SoundIOException (ret);
				var s = GetValue ();
				var count = Layout.ChannelCount;
				var results = new SoundIOChannelArea [count];
				var arr = (IntPtr*)ptrs;
				for (int i = 0; i < count; i++)
					results [i] = new SoundIOChannelArea (arr [i]);
				return results;
			}
		}

		[DllImport ("soundio")]
		internal static extern int soundio_outstream_begin_write ([CTypeDetails ("Pointer<SoundIoOutStream>")]IntPtr outstream, [CTypeDetails ("Pointer<IntPtr>")]out IntPtr areas, [CTypeDetails ("Pointer<int>")]ref int frame_count);

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
