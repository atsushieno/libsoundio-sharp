using System;
using NUnit.Framework;

namespace LibSoundIOSharp.Tests
{
	[TestFixture]
	public class SoundIODeviceTest
	{

		[Test]
		public void Devices ()
		{
			var api = new SoundIO ();
			api.Connect ();
			try {
				api.FlushEvents ();
				Assert.IsTrue (api.DefaultInputDeviceIndex >= -1, "default input device index");
				Assert.IsTrue (api.DefaultOutputDeviceIndex >= -1, "default output device index");
				for (int i = 0; i < api.OutputDeviceCount; i++) {
					var dev = api.GetOutputDevice (i);

				}
				for (int i = 0; i < api.InputDeviceCount; i++) {
					var dev = api.GetInputDevice (i);
				}
			} finally {
				api.Disconnect ();
			}
		}

		[Test]
		public void WithDefaultOutputDevice ()
		{
			var api = new SoundIO ();
			api.Connect ();
			try {
				api.FlushEvents ();
				var dev = api.GetOutputDevice (api.DefaultOutputDeviceIndex);
				Assert.AreNotEqual (0, dev.GetNearestSampleRate (1), "nearest sample rate is 0...?");
				using (var stream = dev.CreateOutStream ()) {
					stream.Open ();
					int frameCount = 1024;
					//stream.BeginWrite (ref frameCount);
					//stream.EndWrite ();
				}
			} finally {
				api.Disconnect ();
			}
		}
	}
}
