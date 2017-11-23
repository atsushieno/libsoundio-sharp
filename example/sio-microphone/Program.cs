using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace SoundIOSharp.Example
{
	class Microphone
	{
		static SoundIORingBuffer ring_buffer = null;

		static SoundIOFormat [] prioritized_formats = {
			SoundIODevice.Float32NE,
			SoundIODevice.Float32FE,
			SoundIODevice.S32NE,
			SoundIODevice.S32FE,
			SoundIODevice.S24NE,
			SoundIODevice.S24FE,
			SoundIODevice.S16NE,
			SoundIODevice.S16FE,
			SoundIODevice.Float64NE,
			SoundIODevice.Float64FE,
			SoundIODevice.U32NE,
			SoundIODevice.U32FE,
			SoundIODevice.U24NE,
			SoundIODevice.U24FE,
			SoundIODevice.U16NE,
			SoundIODevice.U16FE,
			SoundIOFormat.S8,
			SoundIOFormat.U8,
			SoundIOFormat.Invalid,
		};

		static readonly int [] prioritized_sample_rates = {
			48000,
			44100,
			96000,
			24000,
			0,
		};

		public static int Main (string [] args)
		{
			string in_device_id = null, out_device_id = null;
			string backend_name = null;
			bool in_raw = false, out_raw = false;

			double microphone_latency = 0.2; // seconds

			foreach (var arg in args) {
				switch (arg) {
				case "--in_raw":
					in_raw = true;
					continue;
				case "--out_raw":
					out_raw = true;
					continue;
				default:
					if (arg.StartsWith ("--backend:"))
						backend_name = arg.Substring (arg.IndexOf (':') + 1);
					else if (arg.StartsWith ("--in-device:"))
						in_device_id = arg.Substring (arg.IndexOf (':') + 1);
					else if (arg.StartsWith ("--out-device:"))
						out_device_id = arg.Substring (arg.IndexOf (':') + 1);
					else if (arg.StartsWith ("--latency:"))
						microphone_latency = double.Parse (arg.Substring (arg.IndexOf (':') + 1));
					continue;
				}
			}

			var api = new SoundIO ();

			var backend = backend_name == null ? SoundIOBackend.None : (SoundIOBackend)Enum.Parse (typeof (SoundIOBackend), backend_name);
			if (backend == SoundIOBackend.None)
				api.Connect ();
			else
				api.ConnectBackend (backend);
			Console.WriteLine ("backend: " + api.CurrentBackend);

			api.FlushEvents ();

			var in_device = in_device_id == null ? api.GetInputDevice (api.DefaultInputDeviceIndex) :
				Enumerable.Range (0, api.InputDeviceCount)
				.Select (i => api.GetInputDevice (i))
				.FirstOrDefault (d => d.Id == in_device_id && d.IsRaw == in_raw);
			if (in_device == null) {
				Console.Error.WriteLine ("Input device " + in_device_id + " not found.");
				return 1;
			}
			Console.WriteLine ("input device: " + in_device.Name);
			if (in_device.ProbeError != 0) {
				Console.Error.WriteLine ("Cannot probe input device " + in_device_id + ".");
				return 1;
			}

			var out_device = out_device_id == null ? api.GetOutputDevice (api.DefaultOutputDeviceIndex) :
				Enumerable.Range (0, api.OutputDeviceCount)
				.Select (i => api.GetOutputDevice (i))
				.FirstOrDefault (d => d.Id == out_device_id && d.IsRaw == out_raw);
			if (out_device == null) {
				Console.Error.WriteLine ("Output device " + out_device_id + " not found.");
				return 1;
			}
			Console.WriteLine ("output device: " + out_device.Name);
			if (out_device.ProbeError != 0) {
				Console.Error.WriteLine ("Cannot probe output device " + out_device_id + ".");
				return 1;
			}

			out_device.SortDeviceChannelLayouts ();
			var layout = SoundIODevice.BestMatchingChannelLayout (out_device, in_device);

			if (layout.IsNull)
				throw new InvalidOperationException ("channel layouts not compatible"); // panic()

			var sample_rate = prioritized_sample_rates.FirstOrDefault (sr => in_device.SupportsSampleRate (sr) && out_device.SupportsSampleRate (sr));

			if (sample_rate == default (int))
				throw new InvalidOperationException ("incompatible sample rates"); // panic()
			var fmt = prioritized_formats.FirstOrDefault (f => in_device.SupportsFormat (f) && out_device.SupportsFormat (f));

			if (fmt == default (SoundIOFormat))
				throw new InvalidOperationException ("incompatible sample formats"); // panic()

			var instream = in_device.CreateInStream ();
			instream.Format = fmt;
			instream.SampleRate = sample_rate;
			instream.Layout = layout;
			instream.SoftwareLatency = microphone_latency;
			instream.ReadCallback = (fmin, fmax) => read_callback (instream, fmin, fmax);

			instream.Open ();

			var outstream = out_device.CreateOutStream ();
			outstream.Format = fmt;
			outstream.SampleRate = sample_rate;
			outstream.Layout = layout;
			outstream.SoftwareLatency = microphone_latency;
			outstream.WriteCallback = (fmin, fmax) => write_callback (outstream, fmin, fmax);
			outstream.UnderflowCallback = () => underflow_callback (outstream);

			outstream.Open ();

			int capacity = (int) (microphone_latency * 2 * instream.SampleRate * instream.BytesPerFrame);
			ring_buffer = api.CreateRingBuffer (capacity);
			var buf = ring_buffer.WritePointer;
			int fill_count = (int) (microphone_latency * outstream.SampleRate * outstream.BytesPerFrame);
			// FIXME: there should be more efficient way for memset()
			for (int i = 0; i < fill_count; i++)
				Marshal.WriteByte (buf, i, 0);
			ring_buffer.AdvanceWritePointer (fill_count);

			instream.Start ();
			outstream.Start ();

			for (;;)
				api.WaitEvents ();

			outstream.Dispose ();
			instream.Dispose ();
			in_device.RemoveReference ();
			out_device.RemoveReference ();
			api.Dispose ();
			return 0;
		}

		const int ring_buffer_duration_seconds = 30;
		static byte [] arr;
		static FileStream fs;
		static void read_callback (SoundIOInStream instream, int frame_count_min, int frame_count_max)
		{
			var write_ptr = ring_buffer.WritePointer;
			int free_bytes = ring_buffer.FreeCount;
			int free_count = free_bytes / instream.BytesPerFrame;

			if (frame_count_min > free_count)
				Console.Error.WriteLine ("ring buffer overflow"); // panic()

			int write_frames = Math.Min (free_count, frame_count_max);
			int frames_left = write_frames;

			for (; ; ) {
				int frame_count = frames_left;

				var areas = instream.BeginRead (ref frame_count);

				if (frame_count == 0)
					break;

				if (areas.IsEmpty) {
					// Due to an overflow there is a hole. Fill the ring buffer with
					// silence for the size of the hole.
					for (int i = 0; i < frame_count * instream.BytesPerFrame; i++)
						Marshal.WriteByte (write_ptr + i, 0);
					Console.Error.WriteLine ("Dropped {0} frames due to internal overflow", frame_count);
				} else {
					int chCount = instream.Layout.ChannelCount;
					int copySize = instream.BytesPerSample;
					for (int frame = 0; frame < frame_count; frame += 1) {
						unsafe {
							for (int ch = 0; ch < chCount; ch += 1) {
								var area = areas.GetArea (ch);
								Buffer.MemoryCopy ((void*)area.Pointer, (void*)write_ptr, copySize, copySize);
								area.Pointer += area.Step;
								write_ptr += copySize;
							}
						}
					}
				}

				instream.EndRead ();

				frames_left -= frame_count;
				if (frames_left <= 0)
					break;
			}

			int advance_bytes = write_frames * instream.BytesPerFrame;
			ring_buffer.AdvanceWritePointer (advance_bytes);
		}

		static void write_callback (SoundIOOutStream outstream, int frame_count_min, int frame_count_max)
		{
			SoundIOChannelAreas areas = default (SoundIOChannelAreas);
			int frames_left = 0;
			int frame_count = 0;

			var read_ptr = ring_buffer.ReadPointer;
			int fill_bytes = ring_buffer.FillCount;
			int fill_count = fill_bytes / outstream.BytesPerFrame;

			if (frame_count_min > fill_count) {
				// Ring buffer does not have enough data, fill with zeroes.
				frames_left = frame_count_min;
				for (; ; ) {
					frame_count = frames_left;
					if (frame_count <= 0)
						return;
					areas = outstream.BeginWrite (ref frame_count);
					if (frame_count <= 0)
						return;
					var chCount = outstream.Layout.ChannelCount;
					for (int frame = 0; frame < frame_count; frame += 1) {
						for (int ch = 0; ch < chCount; ch += 1) {
							var area = areas.GetArea (ch);
							// FIXME: there should be more efficient way for memset(ptr, 0);
							for (int i = 0; i < outstream.BytesPerSample; i++)
								Marshal.WriteByte (area.Pointer, 0);
							area.Pointer += area.Step;
						}
					}
					outstream.EndWrite ();
					frames_left -= frame_count;
				}
			}

			int read_count = Math.Min (frame_count_max, fill_count);
			frames_left = read_count;

			while (frames_left > 0) {
				frame_count = frames_left;

				areas = outstream.BeginWrite (ref frame_count);

				if (frame_count <= 0)
					break;

				var chCount = outstream.Layout.ChannelCount;
				var copySize = outstream.BytesPerSample;
				for (int frame = 0; frame < frame_count; frame += 1) {
					unsafe {
						for (int ch = 0; ch < chCount; ch += 1) {
							var area = areas.GetArea (ch);
							Buffer.MemoryCopy ((void*)read_ptr, (void*) area.Pointer, copySize, copySize);
							area.Pointer += area.Step;
							read_ptr += outstream.BytesPerSample;
						}
					}
				}
				outstream.EndWrite ();

				frames_left -= frame_count;
			}
			ring_buffer.AdvanceReadPointer (read_count * outstream.BytesPerFrame);
		}

		static int underflow_callback_count = 0;
		static void underflow_callback (SoundIOOutStream outstream)
		{
			Console.Error.WriteLine ("underflow {0}", underflow_callback_count++);
		}
	}
}
