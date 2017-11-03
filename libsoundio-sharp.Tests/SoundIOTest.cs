using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace LibSoundIOSharp.Tests
{
	[TestFixture]
	public class SoundIOTest
	{
		[Test]
		public void EnumerateBackends ()
		{
			var obj = new SoundIOApi ();
			Assert.AreNotEqual (0, obj.BackendCount, "no backend?");
			var nameList = new List<string> ();
			for (int i = 0; i < obj.BackendCount; i++) {
				nameList.Add (SoundIOApi.GetBackendName (obj.GetBackend (i)));
			}
			string names = string.Join (", ", nameList);
			Assert.AreNotEqual (string.Empty, names, "no backend names?");
			obj.Dispose ();
		}

		[Test]
		public void HaveBackend ()
		{
			Assert.IsTrue (SoundIOApi.HaveBackend (SoundIOBackend.Dummy), "not even a Dummy?");
			if (Environment.OSVersion.Platform != PlatformID.Unix)
				Assert.IsFalse (SoundIOApi.HaveBackend (SoundIOBackend.Alsa), "Wait, your Windows has ALSA??");
			else
				Assert.IsFalse (SoundIOApi.HaveBackend (SoundIOBackend.Wasapi), "Wut, you have WASAPI on your Unix platform?");
		}

		[Test]
		public void ChannelLayoutCount ()
		{
			Assert.AreNotEqual (0, SoundIOChannelLayout.BuiltInCount, "no built in channel layout?");
			for (int i = 0; i < SoundIOChannelLayout.BuiltInCount; i++) {
				var l = SoundIOChannelLayout.GetBuiltIn (i);
				var name = l.DetectBuiltInName ();
				Assert.AreNotEqual (null, name, "It should be built-in...");
			}
			Assert.IsNull (SoundIOChannelLayout.GetDefault (0), "soundio returned non-null layout for zero-channels??");
			for (int channels = 1; channels < 10; channels++) {
				var l = SoundIOChannelLayout.GetDefault (channels);
				if (l != null) // some channels would give null e.g. there is no 9ch audio...
					Assert.IsNotNull (l.DetectBuiltInName (), $"channel layout for {channels} has no builtin name...");
			}
		}

		[Test]
		public void Formats ()
		{
			foreach (SoundIOFormat f in Enum.GetValues (typeof (SoundIOFormat)))
				Assert.IsNotNull (SoundIOApi.GetSoundFormatName (f), $"name expected for {f}");
		}

		[Test]
		public void Devices ()
		{
			var api = new SoundIOApi ();
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
	}
}
