using System;
using NUnit.Framework;

namespace SoundIOSharp.Tests
{
	[TestFixture]
	public class SoundIOInStreamTest
	{

		[Test]
		public void Properties ()
		{
			var api = new SoundIO ();
			api.Connect ();
			try {
				api.FlushEvents ();
				var dev = api.GetInputDevice (api.DefaultInputDeviceIndex);
				using (var stream = dev.CreateInStream ()) {
					foreach (var p in typeof (SoundIOInStream).GetProperties ()) {
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
