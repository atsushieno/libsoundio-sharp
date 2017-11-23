using System;
using NUnit.Framework;

namespace SoundIOSharp.Tests
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
		public void Properties ()
		{
			var api = new SoundIO ();
			api.Connect ();
			try {
				api.FlushEvents ();
				var dev = api.GetOutputDevice (api.DefaultOutputDeviceIndex);
				foreach (var p in typeof (SoundIODevice).GetProperties ()) {
					try {
						p.GetValue (dev);
					} catch (Exception ex) {
						Assert.Fail ("Failed to get property " + p + " : " + ex);
					}
				}
			} finally {
				api.Disconnect ();
				api.Dispose ();
			}
		}
	}
}
