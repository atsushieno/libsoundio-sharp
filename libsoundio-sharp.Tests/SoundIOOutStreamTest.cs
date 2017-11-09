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
							switch (p.Name) {
							case "Layout":
								var cl = (SoundIOChannelLayout) p.GetValue (stream);
								foreach (var pcl in typeof (SoundIOChannelLayout).GetProperties ())
									Console.Error.WriteLine (pcl + " : " + pcl.GetValue (cl));
								break;
							default:
								p.GetValue (stream);
								break;
							}
						} catch (Exception ex) {
							Assert.Fail ("Failed to get property " + p + " : " + ex.InnerException);
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
					stream.WriteCallback = (min, max) => {
						int frameCount = max;
						stream.BeginWrite (ref frameCount);
					};
					stream.Start ();
					stream.Pause (true);
					Thread.Sleep (50);
					stream.EndWrite ();
				}
			} finally {
				api.Disconnect ();
			}
		}
	}
}
