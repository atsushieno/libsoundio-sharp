using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LibSoundIOSharp
{
	public class SoundIODevice
	{
		internal SoundIODevice (Pointer<SoundIoDevice> handle)
		{
			this.handle = handle;
		}

		readonly Pointer<SoundIoDevice> handle;

		// Equality (based on handle and native func)

		public override bool Equals (object other)
		{
			var d = other as SoundIODevice;
			return d != null && (this.handle == d.handle || Natives.soundio_device_equal (this.handle, d.handle) != 0);
		}

		public override int GetHashCode ()
		{
			return (int) (IntPtr) handle;
		}

		public static bool operator == (SoundIODevice obj1, SoundIODevice obj2)
		{
			return (object)obj1 == null ? (object)obj2 == null : obj1.Equals (obj2);
		}

		public static bool operator != (SoundIODevice obj1, SoundIODevice obj2)
		{
			return (object)obj1 == null ? (object) obj2 != null : !obj1.Equals (obj2);
		}

		// fields

		SoundIoDevice GetValue ()
		{
			return Marshal.PtrToStructure<SoundIoDevice> (handle);

		}

		public SoundIODeviceAim Aim {
			get { return (SoundIODeviceAim) GetValue ().aim; }
		}

		public SoundIOFormat CurrentFormat {
			get { return (SoundIOFormat) GetValue ().current_format; }
		}

		public SoundIOChannelLayout CurrentLayout {
			get {
				return new SoundIOChannelLayout ((IntPtr) handle + current_layout_offset);
			}
		}
		static readonly int current_layout_offset = (int)Marshal.OffsetOf<SoundIoDevice> ("current_layout");

		public int FormatCount {
			get { return GetValue ().format_count; }
		}

		public IEnumerable<SoundIOFormat> Formats {
			get {
				var ptr = GetValue ().formats;
				for (int i = 0; i < FormatCount; i++)
					yield return (SoundIOFormat) Marshal.ReadInt32 (ptr, i);
			}
		}

		public string Id {
			get { return Marshal.PtrToStringAnsi (GetValue ().id); }
		}

		public bool IsRaw {
			get { return GetValue ().is_raw != 0; }
		}

		public int LayoutCount {
			get { return GetValue ().layout_count; }
		}

		public IEnumerable<SoundIOChannelLayout> Layouts {
			get {
				var ptr = GetValue ().layouts;
				for (int i = 0; i < LayoutCount; i++)
					yield return new SoundIOChannelLayout (ptr + i * Marshal.SizeOf<SoundIoChannelLayout> ());
			}
		}

		public string Name {
			get { return Marshal.PtrToStringAnsi (GetValue ().name); }
		}
		static readonly int name_offset = (int)Marshal.OffsetOf<SoundIoDevice> ("name");

		public int ProbeError {
			get { return GetValue ().probe_error; }
		}

		public int ReferenceCount {
			get { return GetValue ().ref_count; }
		}

		public int SampleRateCount {
			get { return GetValue ().sample_rate_count; }
		}

		public IEnumerable<SoundIOSampleRateRange> SampleRates {
			get {
				var ptr = GetValue ().sample_rates;
				for (int i = 0; i < SampleRateCount; i++)
					yield return new SoundIOSampleRateRange (
						Marshal.ReadInt32 (ptr, i * 2),
						Marshal.ReadInt32 (ptr, i * 2 + 1));
			}
		}

		public double SoftwareLatencyCurrent {
			get { return GetValue ().software_latency_current; }
		}

		public double SoftwareLatencyMin {
			get { return GetValue ().software_latency_min; }
		}

		public double SoftwareLatencyMax {
			get { return GetValue ().software_latency_max; }
		}

		public SoundIO SoundIO {
			get { return new SoundIO (GetValue ().soundio); }
		}

		// functions

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

		public static readonly SoundIOFormat S16NE = BitConverter.IsLittleEndian ? SoundIOFormat.S16LE : SoundIOFormat.S16BE;
		public static readonly SoundIOFormat U16NE = BitConverter.IsLittleEndian ? SoundIOFormat.U16LE : SoundIOFormat.U16BE;
		public static readonly SoundIOFormat S24NE = BitConverter.IsLittleEndian ? SoundIOFormat.S24LE : SoundIOFormat.S24BE;
		public static readonly SoundIOFormat U24NE = BitConverter.IsLittleEndian ? SoundIOFormat.U24LE : SoundIOFormat.U24BE;
		public static readonly SoundIOFormat S32NE = BitConverter.IsLittleEndian ? SoundIOFormat.S32LE : SoundIOFormat.S32BE;
		public static readonly SoundIOFormat U32NE = BitConverter.IsLittleEndian ? SoundIOFormat.U32LE : SoundIOFormat.U32BE;
		public static readonly SoundIOFormat Float32NE = BitConverter.IsLittleEndian ? SoundIOFormat.Float32LE : SoundIOFormat.Float32BE;
		public static readonly SoundIOFormat Float64NE = BitConverter.IsLittleEndian ? SoundIOFormat.Float64LE : SoundIOFormat.Float64BE;
		public static readonly SoundIOFormat Float32FE = !BitConverter.IsLittleEndian ? SoundIOFormat.Float32LE : SoundIOFormat.Float32BE;
		public static readonly SoundIOFormat Float64FE = !BitConverter.IsLittleEndian ? SoundIOFormat.Float64LE : SoundIOFormat.Float64BE;

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

		public SoundIOInStream CreateInStream ()
		{
			return new SoundIOInStream (Natives.soundio_instream_create (handle));
		}

		public SoundIOOutStream CreateOutStream ()
		{
			return new SoundIOOutStream (Natives.soundio_outstream_create (handle));
		}
	}
}
