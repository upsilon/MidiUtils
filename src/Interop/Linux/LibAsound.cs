/* MidiUtils

LICENSE - The MIT License (MIT)

Copyright (c) 2019 Yoichi Kimura

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Runtime.InteropServices;

#pragma warning disable IDE1006

namespace MidiUtils.Interop.Linux
{
    #region "alsa/seq.h"

    [Flags]
    internal enum SeqStreams : int
    {
        SND_SEQ_OPEN_OUTPUT = 1,
        SND_SEQ_OPEN_INPUT = 2,
        SND_SEQ_OPEN_DUPLEX = SND_SEQ_OPEN_INPUT | SND_SEQ_OPEN_OUTPUT,
    }

    internal enum SeqMode : int
    {
        None = 0x0000,
        SND_SEQ_NONBLOCK = 0x0001,
    }

    internal static partial class LibAsound
    {
        public const string LibraryName = "libasound.so.2";

        [DllImport(LibraryName, CharSet = CharSet.Ansi)]
        public static extern int snd_seq_open(out SafeSeqHandle handle, string name, SeqStreams streams, SeqMode mode);

        [DllImport(LibraryName)]
        public static extern int snd_seq_close(SafeSeqHandle handle);

        [DllImport(LibraryName)]
        public static extern int snd_seq_poll_descriptors_count(SafeSeqHandle handle, PollEvent events);

        [DllImport(LibraryName)]
        public static extern int snd_seq_poll_descriptors(SafeSeqHandle handle, ref PollFd pfds, uint space, PollEvent events);

        [DllImport(LibraryName)]
        public static extern unsafe int snd_seq_event_input(SafeSeqHandle handle, out snd_seq_event* ev);

        [DllImport(LibraryName)]
        public static extern int snd_seq_event_input_pending(SafeSeqHandle seq, int fetch_sequencer);
    }

    [Flags]
    internal enum SeqPortCapability : uint
    {
        SND_SEQ_PORT_CAP_READ = 1 << 0,
        SND_SEQ_PORT_CAP_WRITE = 1 << 1,
        SND_SEQ_PORT_CAP_SYNC_READ = 1 << 2,
        SND_SEQ_PORT_CAP_SYNC_WRITE = 1 << 3,
        SND_SEQ_PORT_CAP_DUPLEX = 1 << 4,
        SND_SEQ_PORT_CAP_SUBS_READ = 1 << 5,
        SND_SEQ_PORT_CAP_SUBS_WRITE = 1 << 6,
        SND_SEQ_PORT_CAP_NO_EXPORT = 1 << 7,
    }

    [Flags]
    internal enum SeqPortType : uint
    {
        SND_SEQ_PORT_TYPE_SPECIFIC = 1 << 0,
        SND_SEQ_PORT_TYPE_MIDI_GENERIC = 1 << 1,
        SND_SEQ_PORT_TYPE_MIDI_GM = 1 << 2,
        SND_SEQ_PORT_TYPE_MIDI_GS = 1 << 3,
        SND_SEQ_PORT_TYPE_MIDI_XG = 1 << 4,
        SND_SEQ_PORT_TYPE_MIDI_MT32 = 1 << 5,
        SND_SEQ_PORT_TYPE_MIDI_GM2 = 1 << 6,
        SND_SEQ_PORT_TYPE_SYNTH = 1 << 10,
        SND_SEQ_PORT_TYPE_DIRECT_SAMPLE = 1 << 11,
        SND_SEQ_PORT_TYPE_SAMPLE = 1 << 12,
        SND_SEQ_PORT_TYPE_HARDWARE = 1 << 16,
        SND_SEQ_PORT_TYPE_SOFTWARE = 1 << 17,
        SND_SEQ_PORT_TYPE_SYNTHESIZER = 1 << 18,
        SND_SEQ_PORT_TYPE_PORT = 1 << 19,
        SND_SEQ_PORT_TYPE_APPLICATION = 1 << 20,
    }

    #endregion

    #region "alsa/seqmid.h"

    internal static partial class LibAsound
    {
        [DllImport(LibraryName, CharSet = CharSet.Ansi)]
        public static extern int snd_seq_create_simple_port(SafeSeqHandle seq, string name, SeqPortCapability caps, SeqPortType type);

        [DllImport(LibraryName, CharSet = CharSet.Ansi)]
        public static extern int snd_seq_set_client_name(SafeSeqHandle seq, string name);
    }

    #endregion

    #region "alsa/seq_midi_event.h"

    internal static partial class LibAsound
    {
        [DllImport(LibraryName)]
        public static extern int snd_midi_event_new(UIntPtr bufsize, out SafeMidiParserHandle rdev);

        [DllImport(LibraryName)]
        public static extern void snd_midi_event_free(SafeMidiParserHandle dev);

        [DllImport(LibraryName)]
        public static extern void snd_midi_event_no_status(SafeMidiParserHandle dev, int on);

        [DllImport(LibraryName)]
        public static extern int snd_midi_event_decode(SafeMidiParserHandle dev, ref byte buf, int count, in snd_seq_event ev);
    }

    #endregion

    #region "alsa/seq_event.h"

    internal enum snd_seq_event_type : byte
    {
        SND_SEQ_EVENT_CLIENT_EXIT = 61,
        SND_SEQ_EVENT_SYSEX = 130,
    }

    [StructLayout(LayoutKind.Sequential)]
    struct snd_seq_addr
    {
        public byte client;
        public byte port;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct snd_seq_real_time
    {
        public uint tv_sec;
        public uint tv_nsec;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct snd_seq_timestamp
    {
        [FieldOffset(0)]
        public uint tick;

        [FieldOffset(0)]
        public snd_seq_real_time time;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct snd_seq_event
    {
        public snd_seq_event_type type;
        public byte flags;
        public byte tag;
        public byte queue;
        public snd_seq_timestamp time;
        public snd_seq_addr source;
        public snd_seq_addr dest;
        public uint data_ext_len;
        public IntPtr data_ext_ptr;
    }

    #endregion

    #region "alsa/error.h"

    internal static partial class LibAsound
    {
        [DllImport(LibraryName, CharSet = CharSet.Ansi)]
        public static extern string snd_strerror(int errnum);
    }

    #endregion
}
