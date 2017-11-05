using System;
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
	}
}
