using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace LibSoundIOSharp
{
	public class SoundIOChannelLayout
	{
		public static int BuiltInCount {
			get { return Natives.soundio_channel_layout_builtin_count (); }
		}

		public static SoundIOChannelLayout GetBuiltIn (int index)
		{
			return new SoundIOChannelLayout (Natives.soundio_channel_layout_get_builtin (index));
		}

		public static SoundIOChannelLayout GetDefault (int channelCount)
		{
			var handle = Natives.soundio_channel_layout_get_default (channelCount);
			return (IntPtr) handle == IntPtr.Zero ? null : new SoundIOChannelLayout (handle);
		}

		public static SoundIOChannelId ParseChannelId (string name)
		{
			var ptr = Marshal.StringToHGlobalAnsi (name);
			try {
				return (SoundIOChannelId)Natives.soundio_parse_channel_id (ptr, name.Length);
			} finally {
				Marshal.FreeHGlobal (ptr);
			}
		}

		// TODO: bind soundio_best_matching_channel_layout().

		// instance members

		internal SoundIOChannelLayout (Pointer<SoundIoChannelLayout> handle)
		{
			this.handle = handle;
		}

		readonly Pointer<SoundIoChannelLayout> handle;

		internal SoundIoChannelLayout GetValue ()
		{
			return Marshal.PtrToStructure<SoundIoChannelLayout> (handle);
		}

		public int ChannelCount {
			get { return GetValue ().channel_count; }
		}

		public string Name {
			get { return Marshal.PtrToStringAnsi (GetValue ().name); }
		}

		public IEnumerable<SoundIOChannelId> Channels {
			get {
				return GetValue ().channels.Cast<SoundIOChannelId> ();
			}
		}

		public override bool Equals (object other)
		{
			var s = other as SoundIOChannelLayout;
			return s != null && (handle == s.handle || Natives.soundio_channel_layout_equal (handle, s.handle) != 0);
		}

		public override int GetHashCode ()
		{
			return handle.GetHashCode ();
		}

		public string DetectBuiltInName ()
		{
			if (Natives.soundio_channel_layout_detect_builtin (handle) != 0)
				return Marshal.PtrToStringAnsi (GetValue ().name);
			return null;
		}

		public int FindChannel (SoundIOChannelId channel)
		{
			return Natives.soundio_channel_layout_find_channel (handle, (SoundIoChannelId) channel);
		}
	}
}
