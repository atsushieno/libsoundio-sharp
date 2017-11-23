using System;
using System.Collections.Generic;
using System.Linq;

namespace SoundIOSharp.Example
{
	class Sine
	{
		static Action<IntPtr, double> write_sample;
		static double seconds_offset = 0.0;
		static volatile bool want_pause = false;

		public static int Main (string [] args)
		{
			string device_id = null;
			string backend_name = null;
			bool raw = false;
			string stream_name = null;
			double latency = 0.0;
			int sample_rate = 0;
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
					else if (arg.StartsWith ("--name:"))
						stream_name = arg.Substring (arg.IndexOf (':') + 1);
					else if (arg.StartsWith ("--latency:"))
						latency = double.Parse (arg.Substring (arg.IndexOf (':') + 1));
					else if (arg.StartsWith ("--sample_rate:"))
						sample_rate = int.Parse (arg.Substring (arg.IndexOf (':') + 1));
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

			var device = device_id == null ? api.GetOutputDevice (api.DefaultOutputDeviceIndex) :
				   Enumerable.Range (0, api.OutputDeviceCount)
				   .Select (i => api.GetOutputDevice (i))
				   .FirstOrDefault (d => d.Id == device_id && d.IsRaw == raw);
			if (device == null) {
				Console.Error.WriteLine ("Output device " + device_id + " not found.");
				return 1;
			}
			Console.WriteLine ("output device: " + device.Name);
			if (device.ProbeError != 0) {
				Console.Error.WriteLine ("Cannot probe device " + device_id + ".");
				return 1;
			}

			var outstream = device.CreateOutStream ();

			outstream.WriteCallback = (min,max) => write_callback (outstream, min, max);
			outstream.UnderflowCallback = () => underflow_callback (outstream);
			if (stream_name != null)
				outstream.Name = stream_name;
			outstream.SoftwareLatency = latency;
			if (sample_rate != 0)
				outstream.SampleRate = sample_rate;
			
			if (device.SupportsFormat (SoundIODevice.Float32NE)) {
				outstream.Format = SoundIODevice.Float32NE;
				write_sample = write_sample_float32ne;
			} else if (device.SupportsFormat (SoundIODevice.Float64NE)) {
				outstream.Format = SoundIODevice.Float64NE;
				write_sample = write_sample_float64ne;
			} else if (device.SupportsFormat (SoundIODevice.S32NE)) {
				outstream.Format = SoundIODevice.S32NE;
				write_sample = write_sample_s32ne;
			} else if (device.SupportsFormat (SoundIODevice.S16NE)) {
				outstream.Format = SoundIODevice.S16NE;
				write_sample = write_sample_s16ne;
			} else {
				Console.Error.WriteLine ("No suitable format available.");
				return 1;
			}

			outstream.Open ();

			Console.Error.WriteLine ("Software latency: " + outstream.SoftwareLatency);
			Console.Error.WriteLine (
				@"
'p\n' - pause
'u\\n' - unpause
'P\\n' - pause from within callback
'c\\n' - clear buffer
'q\\n' - quit");

			if (outstream.LayoutErrorMessage != null)
				Console.Error.WriteLine ("Unable to set channel layout: " + outstream.LayoutErrorMessage);
			
			outstream.Start ();

			for (; ; ) {
				api.FlushEvents ();

				int c = Console.Read ();
				if (c == 'p') {
					outstream.Pause (true);
					Console.Error.WriteLine ("pause");
				} else if (c == 'P') {
					want_pause = true;
				} else if (c == 'u') {
					want_pause = false;
					outstream.Pause (false);
					Console.Error.WriteLine ("resume");
				} else if (c == 'c') {
					outstream.ClearBuffer ();
					Console.Error.WriteLine ("clear buffer");
				} else if (c == 'q') {
					break;
				}
			}

			outstream.Dispose ();
			device.RemoveReference ();
			api.Dispose ();

			return 0;
		}


		static void write_callback (SoundIOOutStream outstream, int frame_count_min, int frame_count_max)
		{
			double float_sample_rate = outstream.SampleRate;
			double seconds_per_frame = 1.0 / float_sample_rate;

			int frames_left = frame_count_max;
			int frame_count = 0;

			for (; ; ) {
				frame_count = frames_left;
				var results = outstream.BeginWrite (ref frame_count);

				if (frame_count == 0)
					break;

				SoundIOChannelLayout layout = outstream.Layout;

				double pitch = 440.0;
				double radians_per_second = pitch * 2.0 * Math.PI;
				for (int frame = 0; frame < frame_count; frame += 1) {
					double sample = Math.Sin ((seconds_offset + frame * seconds_per_frame) * radians_per_second);
					for (int channel = 0; channel < layout.ChannelCount; channel += 1) {

						var area = results.GetArea (channel);
						write_sample (area.Pointer, sample);
						area.Pointer += area.Step;
					}
				}
				seconds_offset = Math.IEEERemainder (seconds_offset + seconds_per_frame * frame_count, 1.0);

				outstream.EndWrite ();

				frames_left -= frame_count;
				if (frames_left <= 0)
					break;
			}

			outstream.Pause (want_pause);
		}

		static int underflow_callback_count = 0;
		static void underflow_callback (SoundIOOutStream outstream)
		{
			Console.Error.WriteLine ("underflow {0}", underflow_callback_count++);
		}

		static unsafe void write_sample_s16ne (IntPtr ptr, double sample)
		{
			short* buf = (short*) ptr;
			double range = (double) short.MaxValue - (double) short.MinValue;
			double val = sample * range / 2.0;
			*buf = (short) val;
		}

		static unsafe void write_sample_s32ne (IntPtr ptr, double sample)
		{
			int* buf = (int*)ptr;
			double range = (double) int.MaxValue - (double) int.MinValue;
			double val = sample * range / 2.0;
			*buf = (int) val;
		}

		static unsafe void write_sample_float32ne (IntPtr ptr, double sample)
		{
			float* buf = (float*)ptr;
			*buf = (float) sample;
		}

		static unsafe void write_sample_float64ne (IntPtr ptr, double sample)
		{
			double* buf = (double*)ptr;
			*buf = sample;
		}

	}
}
