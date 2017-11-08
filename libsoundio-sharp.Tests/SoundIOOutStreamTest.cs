using System;
using System.Threading;
using NUnit.Framework;

namespace LibSoundIOSharp.Tests
{
	[TestFixture]
	public class SoundIOOutStreamTest
	{

		[Test]
		public void Properties ()
		{
			var api = new SoundIO ();
			api.Connect ();
			try {
				api.FlushEvents ();
				var dev = api.GetOutputDevice (api.DefaultOutputDeviceIndex);
				using (var stream = dev.CreateOutStream ()) {
					foreach (var p in typeof (SoundIOOutStream).GetProperties ()) {
						try {
							p.GetValue (stream);
						} catch (Exception ex) {
							Assert.Fail ("Failed to get property " + p + " : " + ex);
						}
					}
				}
			} finally {
				api.Disconnect ();
				api.Dispose ();
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
					stream.BeginWrite (ref frameCount);
					stream.EndWrite ();
					stream.Start ();
					stream.Pause (true);
					Thread.Sleep (50);
					stream.Pause (false);
					Thread.Sleep (50);
					stream.Pause (true);
				}
			} finally {
				api.Disconnect ();
			}
		}
	}
}
