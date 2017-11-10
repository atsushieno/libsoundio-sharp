using System;
using System.Collections.Generic;
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
		Action<IntPtr> error_callback_native;

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
		Action<IntPtr,int,int> write_callback_native;

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
		Action<IntPtr> underflow_callback_native;

		// FIXME: this should be taken care in more centralized/decent manner... we don't want to write
		// this kind of code anywhere we need string marshaling.
		List<IntPtr> allocated_hglobals = new List<IntPtr> ();

		public string Name {
			get { return Marshal.PtrToStringAnsi (GetValue ().name); }
			set {
				unsafe {
					var existing = GetValue ().name;
					if (allocated_hglobals.Contains (existing)) {
						allocated_hglobals.Remove (existing);
						Marshal.FreeHGlobal (existing);
					}
					var ptr = Marshal.StringToHGlobalAnsi (value);
					Marshal.WriteIntPtr (handle, name_offset, ptr);
					allocated_hglobals.Add (ptr);
				}
			}
		}
		static readonly int name_offset = (int)Marshal.OffsetOf<SoundIoOutStream> ("name");

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

		public struct WriteResults
		{
			static readonly int native_size = Marshal.SizeOf<SoundIoChannelArea> ();

			internal WriteResults (IntPtr head, int channelCount, int frameCount)
			{
				this.head = head;
				this.channel_count = channelCount;
				this.frame_count = frameCount;
			}

			IntPtr head;
			int channel_count;
			int frame_count;

			public SoundIOChannelArea GetArea (int channel)
			{
				return new SoundIOChannelArea (head + native_size * channel);
			}

			public int ChannelCount => channel_count;
			public int FrameCount => frame_count;
		}

		public WriteResults BeginWrite (ref int frameCount)
		{
			IntPtr ptrs = default (IntPtr);
			int nativeFrameCount = frameCount;
			unsafe {
				var frameCountPtr = &nativeFrameCount;
				var ptrptr = &ptrs;
				var ret = (SoundIoError)Natives.soundio_outstream_begin_write (handle, (IntPtr) ptrptr, (IntPtr) frameCountPtr);
				frameCount = *frameCountPtr;
				if (ret != SoundIoError.SoundIoErrorNone)
					throw new SoundIOException (ret);
				return new WriteResults (ptrs, Layout.ChannelCount, frameCount);
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
