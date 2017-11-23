using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace SoundIOSharp.Tests
{
	[TestFixture]
	public class SoundIOTest
	{
		[Test]
		public void EnumerateBackends ()
		{
			var obj = new SoundIO ();
			Assert.AreNotEqual (0, obj.BackendCount, "no backend?");
			var nameList = new List<string> ();
			for (int i = 0; i < obj.BackendCount; i++) {
				nameList.Add (SoundIO.GetBackendName (obj.GetBackend (i)));
			}
			string names = string.Join (", ", nameList);
			Assert.AreNotEqual (string.Empty, names, "no backend names?");
			obj.Dispose ();
		}

		[Test]
		public void HaveBackend ()
		{
			Assert.IsTrue (SoundIO.HaveBackend (SoundIOBackend.Dummy), "not even a Dummy?");
			if (Environment.OSVersion.Platform != PlatformID.Unix)
				Assert.IsFalse (SoundIO.HaveBackend (SoundIOBackend.Alsa), "Wait, your Windows has ALSA??");
			else
				Assert.IsFalse (SoundIO.HaveBackend (SoundIOBackend.Wasapi), "Wut, you have WASAPI on your Unix platform?");
		}

		[Test]
		public void Formats ()
		{
			foreach (SoundIOFormat f in Enum.GetValues (typeof (SoundIOFormat)))
				Assert.IsNotNull (SoundIO.GetSoundFormatName (f), $"name expected for {f}");
		}

		[Test]
		public void ApplicationName ()
		{
			var api = new SoundIO ();
			Assert.AreEqual ("SoundIo", api.ApplicationName, "default app name unexpected");
			api.ApplicationName = "MyApp";
			Assert.AreEqual ("MyApp", api.ApplicationName, "app_name not assigned");
			api.Dispose ();
		}

		[Ignore ("this does not seem to be the way how this event is used. It is raised when soem device caused fatal errors. I cannot really unit test that.")]
		[Test]
		public void OnBackendDisconnect ()
		{
			var api = new SoundIO ();
			string msg = null;
			int err = -1;
			api.OnBackendDisconnect = e => {
				msg = "disconnected";
				err = e;
			};
			api.Connect ();
			api.FlushEvents ();
			api.Disconnect ();
			Assert.AreEqual ("disconnected", msg, "msg not set");
			Assert.AreEqual (0, err, "either error occured or event not fired");
			api.Dispose ();
		}
	}
}
