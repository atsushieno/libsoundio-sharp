using System;
namespace LibSoundIOSharp
{
	public class SoundIODevice
	{
		internal SoundIODevice (IntPtr handle)
		{
			this.handle = handle;
		}

		readonly IntPtr handle;

		public void AddReference ()
		{
			Natives.soundio_device_ref (handle);
		}

		public void RemoveReference ()
		{
			Natives.soundio_device_unref (handle);
		}

		public void SortDeviceChannelLayouts ()
		{
			Natives.soundio_device_sort_channel_layouts (handle);
		}

		public override bool Equals (object other)
		{
			var d = other as SoundIODevice;
			return d != null && (this.handle == d.handle || Natives.soundio_device_equal (this.handle, d.handle) != 0);
		}

		public override int GetHashCode ()
		{
			return (int) handle;
		}

		public bool SupportsFormat (SoundIOFormat format)
		{
			return Natives.soundio_device_supports_format (handle, (SoundIoFormat) format) != 0;
		}

		public bool SupportsSampleRate (int sampleRate)
		{
			return Natives.soundio_device_supports_sample_rate (handle, sampleRate) != 0;
		}

		public int GetNearestSampleRate (int sampleRate)
		{
			return Natives.soundio_device_nearest_sample_rate (handle, sampleRate);
		}
	}
}
