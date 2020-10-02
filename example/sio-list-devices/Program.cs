using System;
using System.Linq;

namespace SoundIOSharp.Example
{
	class MainClass
	{
		public static int Main (string [] args)
		{
			bool watch = false;
			string backend = null;
			foreach (var arg in args) {
				switch (arg) {
				case "--watch":
					watch = true;
					continue;
				default:
					if (arg.StartsWith ("--backend:")) {
						backend = arg.Substring (arg.IndexOf (':') + 1);
						continue;
					}
					break;
				}
				ShowUsageToExit ();
				return 1;
			}

			using (var api = new SoundIO ()) {
				SoundIOBackend be = SoundIOBackend.None;
				if (Enum.TryParse (backend, out be)) {
					ShowUsageToExit ();
					return 1;
				}
				if (be == SoundIOBackend.None)
					api.Connect ();
				else
					api.ConnectBackend (be);

				api.FlushEvents ();
				if (watch) {
					api.OnDevicesChange = () => OnDeviceChange (api);
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
				PrintDevice (api.GetOutputDevice (i));
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
