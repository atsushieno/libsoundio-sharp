using System;
using System.Threading;
using NUnit.Framework;

namespace SoundIOSharp.Tests
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
								var cl = stream.Layout;
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
		public void SoftwareLatencyOffset ()
		{
			var api = new SoundIO ();
			api.Connect ();
			try {
				api.FlushEvents ();
				var dev = api.GetOutputDevice (api.DefaultOutputDeviceIndex);
				Assert.AreNotEqual (0, dev.GetNearestSampleRate (1), "nearest sample rate is 0...?");
				using (var stream = dev.CreateOutStream ()) {
					Assert.AreEqual (0, stream.SoftwareLatency, "existing non-zero latency...?");
					stream.SoftwareLatency = 0.5;
					Assert.AreEqual (0.5, stream.SoftwareLatency, "wrong software latency");
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
				var wait = new ManualResetEvent (false);
				using (var stream = dev.CreateOutStream ()) {
					stream.Open ();
					stream.WriteCallback = (min, max) => {
						int frameCount = max;
						var results = stream.BeginWrite (ref frameCount);
						for (int channel = 0; channel < stream.Layout.ChannelCount; channel += 1) {
							var area = results.GetArea (channel);
							// FIXME: do write samples
							area.Pointer += area.Step;
						}
						stream.EndWrite ();
						wait.Set ();
					};
					stream.Start ();
					stream.Pause (true);
					wait.WaitOne ();
				}
			} finally {
				api.Disconnect ();
			}
		}
	}
}
