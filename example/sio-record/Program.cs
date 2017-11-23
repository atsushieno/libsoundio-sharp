using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace SoundIOSharp.Example
{
	class Record
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
			string device_id = null;
			string backend_name = null;
			bool raw = false;
			string outfile = null;

			foreach (var arg in args) {
				switch (arg) {
				case "--raw":
					raw = true;
					continue;
				default:
					if (arg.StartsWith ("--backend:"))
						backend_name = arg.Substring (arg.IndexOf (':') + 1);
					else if (arg.StartsWith ("--device:"))
						device_id = arg.Substring (arg.IndexOf (':') + 1);
					else
						outfile = arg;
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

			var device = device_id == null ? api.GetInputDevice (api.DefaultInputDeviceIndex) :
				Enumerable.Range (0, api.InputDeviceCount)
				.Select (i => api.GetInputDevice (i))
				.FirstOrDefault (d => d.Id == device_id && d.IsRaw == raw);
			if (device == null) {
				Console.Error.WriteLine ("device " + device_id + " not found.");
				return 1;
			}
			Console.WriteLine ("device: " + device.Name);
			if (device.ProbeError != 0) {
				Console.Error.WriteLine ("Cannot probe device " + device_id + ".");
				return 1;
			}

			var sample_rate = prioritized_sample_rates.First (sr => device.SupportsSampleRate (sr));

			var fmt = prioritized_formats.First (f => device.SupportsFormat (f));

			var instream = device.CreateInStream ();
			instream.Format = fmt;
			instream.SampleRate = sample_rate;
			instream.ReadCallback = (fmin, fmax) => read_callback (instream, fmin, fmax);
			instream.OverflowCallback = () => overflow_callback (instream);

			instream.Open ();

			const int ring_buffer_duration_seconds = 30;
			int capacity = (int)(ring_buffer_duration_seconds * instream.SampleRate * instream.BytesPerFrame);
			ring_buffer = api.CreateRingBuffer (capacity);
			var buf = ring_buffer.WritePointer;

			instream.Start ();

			Console.WriteLine ("Type CTRL+C to quit by killing process...");
			using (var fs = File.OpenWrite (outfile)) {
				var arr = new byte [capacity];
				unsafe {
					fixed (void* arrptr = arr) {
						for (; ; ) {
							api.FlushEvents ();
							Thread.Sleep (1000);
							int fill_bytes = ring_buffer.FillCount;
							var read_buf = ring_buffer.ReadPointer;

							Buffer.MemoryCopy ((void*)read_buf, arrptr, fill_bytes, fill_bytes);
							fs.Write (arr, 0, fill_bytes);
							ring_buffer.AdvanceReadPointer (fill_bytes);
						}
					}
				}
			}
			instream.Dispose ();
			device.RemoveReference ();
			api.Dispose ();
			return 0;
		}

		static void read_callback (SoundIOInStream instream, int frame_count_min, int frame_count_max)
		{
			var write_ptr = ring_buffer.WritePointer;
			int free_bytes = ring_buffer.FreeCount;
			int free_count = free_bytes / instream.BytesPerFrame;

			if (frame_count_min > free_count)
				throw new InvalidOperationException ("ring buffer overflow"); // panic()

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
					for (int frame = 0; frame < frame_count; frame += 1) {
						int chCount = instream.Layout.ChannelCount;
						int copySize = instream.BytesPerSample;
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

		static int overflow_callback_count = 0;
		static void overflow_callback (SoundIOInStream instream)
		{
			Console.Error.WriteLine ("overflow {0}", overflow_callback_count++);
		}
	}
}
