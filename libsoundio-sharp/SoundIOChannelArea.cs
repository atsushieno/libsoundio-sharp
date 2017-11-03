using System;
namespace LibSoundIOSharp
{
	public class SoundIOChannelArea
	{
		internal SoundIOChannelArea (Pointer<SoundIoChannelArea> handle)
		{
			this.handle = handle;
		}

		Pointer<SoundIoChannelArea> handle;
	}
}
