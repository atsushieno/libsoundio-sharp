using System;
using System.Runtime.InteropServices;

namespace LibSoundIOSharp
{
	public class SoundIOApi : IDisposable
	{
		IntPtr handle;

		public SoundIOApi ()
		{
			handle = Natives.soundio_create ();
		}

		public void Dispose ()
		{
			Natives.soundio_destroy (handle);
		}

		public int BackendCount {
			get { return Natives.soundio_backend_count (handle); }
		}

		public int InputDeviceCount {
			get { return Natives.soundio_input_device_count (handle); }
		}

		public int OutputDeviceCount {
			get { return Natives.soundio_output_device_count (handle); }
		}

		public int DefaultInputDeviceIndex {
			get { return Natives.soundio_default_input_device_index (handle); }
		}

		public int DefaultOutputDeviceIndex {
			get { return Natives.soundio_default_output_device_index (handle); }
		}

		public SoundIOBackend GetBackend (int index)
		{
			return (SoundIOBackend) Natives.soundio_get_backend (handle, index);
		}

		public SoundIODevice GetInputDevice (int index)
		{
			return new SoundIODevice (Natives.soundio_get_input_device (handle, index));
		}

		public SoundIODevice GetOutputDevice (int index)
		{
			return new SoundIODevice (Natives.soundio_get_output_device (handle, index));
		}

		public void Connect ()
		{
			var ret = (SoundIoError) Natives.soundio_connect (handle);
			if (ret != SoundIoError.SoundIoErrorNone)
				throw new SoundIOException (ret);
		}

		public void ConnectBackend (SoundIOBackend backend)
		{
			var ret = (SoundIoError) Natives.soundio_connect_backend (handle, (SoundIoBackend) backend);
			if (ret != SoundIoError.SoundIoErrorNone)
				throw new SoundIOException (ret);
		}

		public void Disconnect ()
		{
			Natives.soundio_disconnect (handle);
		}

		public void FlushEvents ()
		{
			Natives.soundio_flush_events (handle);
		}

		public void WaitEvents ()
		{
			Natives.soundio_wait_events (handle);
		}

		public void Wakeup ()
		{
			Natives.soundio_wakeup (handle);
		}

		public void ForceDeviceScan ()
		{
			Natives.soundio_force_device_scan (handle);
		}

		public SoundIORingBuffer CreateRingBuffer (int capacity)
		{
			return new SoundIORingBuffer (Natives.soundio_ring_buffer_create (handle, capacity));
		}

		// static methods

		public static string VersionString {
			get { return Marshal.PtrToStringAnsi (Natives.soundio_version_string ()); }
		}

		public static int VersionMajor {
			get { return Natives.soundio_version_major (); }
		}

		public static int VersionMinor {
			get { return Natives.soundio_version_minor (); }
		}

		public static int VersionPatch {
			get { return Natives.soundio_version_patch (); }
		}

		public static string GetBackendName (SoundIOBackend backend)
		{
			return Marshal.PtrToStringAnsi (Natives.soundio_backend_name ((SoundIoBackend) backend));
		}

		public static bool HaveBackend (SoundIOBackend backend)
		{
			return Natives.soundio_have_backend ((SoundIoBackend) backend) != 0;
		}

		public static int GetBytesPerSample (SoundIOFormat format)
		{
			return Natives.soundio_get_bytes_per_sample ((SoundIoFormat) format);
		}

		public static int GetBytesPerFrame (SoundIOFormat format, int channelCount)
		{
			return Natives.soundio_get_bytes_per_frame ((SoundIoFormat) format, channelCount);
		}

		public static int GetBytesPerSecond (SoundIOFormat format, int channelCount, int sampleRate)
		{
			return Natives.soundio_get_bytes_per_second ((SoundIoFormat) format, channelCount, sampleRate);
		}

		public static string GetSoundFormatName (SoundIOFormat format)
		{
			return Marshal.PtrToStringAnsi (Natives.soundio_format_string ((SoundIoFormat) format));
		}
	}
}
