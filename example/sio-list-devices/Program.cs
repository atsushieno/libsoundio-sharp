﻿using System;
using System.Linq;

namespace LibSoundIOSharp.Samples
{
	class MainClass
	{
		public static int Main (string [] args)
		{
			bool watch = false;
			bool short_output = false;
			string backend = null;
			foreach (var arg in args) {
				switch (arg) {
				case "--watch":
					watch = true;
					continue;
				case "--short":
					short_output = true;
					continue;
				default:
					if (arg.StartsWith ("--backend:")) {
						backend = arg.Substring (arg.IndexOf (':'));
						continue;
					}
					break;
				}
				ShowUsageToExit ();
				return 1;
			}

			using (var api = new SoundIO ()) {
				SoundIOBackend be = SoundIOBackend.None;
				if (Enum.TryParse<SoundIOBackend> (backend, out be)) {
					ShowUsageToExit ();
					return 1;
				}
				if (be == SoundIOBackend.None)
					api.Connect ();
				else
					api.ConnectBackend (be);

				api.FlushEvents ();
				if (watch) {
					api.OnDevicesChange = OnDeviceChange;
					Console.WriteLine ("Type [ENTER] to exit.");
					Console.ReadLine ();
				} else
					DoListDevices (api);
			}


			return 0;
		}

		static void DoListDevices (SoundIO api)
		{
			Console.WriteLine ("Inputs");
			for (int i = 0; i < api.InputDeviceCount; i++)
				PrintDevice (api.GetInputDevice (i));
			Console.WriteLine ("Outputs");
			for (int i = 0; i < api.OutputDeviceCount; i++)
				PrintDevice (api.GetInputDevice (i));
		}

		static void PrintDevice (SoundIODevice dev)
		{
			Console.WriteLine ($"  {dev.Id} - {dev.Name}");
			foreach (var pi in typeof (SoundIODevice).GetProperties ())
				Console.WriteLine ($"    {pi.Name}: {pi.GetValue (dev)}");
		}

		static void OnDeviceChange (SoundIO api)
		{
			DoListDevices (api);
		}

		static void ShowUsageToExit ()
		{
			Console.Error.WriteLine (@"Arguments:
--watch		watch devices.
--short		short output.
--backend:xxx	specify backend to use.

libsoundio version: {0}

available backends: {1}
",
			                         SoundIO.VersionString,
			                         string.Join (", ", Enum.GetValues (typeof (SoundIOBackend))
			                                      .Cast<SoundIOBackend> ()
			                                      .Where (b => SoundIO.HaveBackend (b))));
		}
	}
}
