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

using MidiUtils.Interop.Linux;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static MidiUtils.Interop.Linux.LibAsound;
using static MidiUtils.Interop.Linux.LibC;

namespace MidiUtils.IO
{
    public class AlsaMidiIn : IMidiIn
    {
        public string ClientName { get; }

        public bool IsPlaying { get; private set; }
        public bool IsDisposed { get; private set; }

        public event EventHandler<ReceivedMidiEventEventArgs> ReceivedMidiEvent;
        public event EventHandler<ReceivedExclusiveMessageEventArgs> ReceivedExclusiveMessage;
        public event EventHandler Opened;
        public event EventHandler Closed;

        private SafeSeqHandle seqencerHandle;
        private SafeMidiParserHandle parserHandle;

        private bool pollLoopActive;
        private Task pollLoopTask;

        public AlsaMidiIn(string clientName)
            => this.ClientName = clientName;

        public void Start()
        {
            this.InitializeSequencer();
            this.InitializeMidiParser();

            this.Opened?.Invoke(this, EventArgs.Empty);

            this.pollLoopActive = true;
            this.pollLoopTask = Task.Run(() => this.PollLoop());
        }

        public void Stop()
        {
            this.pollLoopActive = false;
            this.pollLoopTask?.Wait();
        }

        public void Dispose()
        {
            this.Stop();
            this.seqencerHandle?.Dispose();
            this.parserHandle?.Dispose();
        }

        private void InitializeSequencer()
        {
            int ret;

            ret = snd_seq_open(out this.seqencerHandle, "default", SeqStreams.SND_SEQ_OPEN_INPUT, SeqMode.None);
            if (ret < 0)
                throw new AlsaException($"{nameof(snd_seq_open)} fails", ret);

            ret = snd_seq_set_client_name(this.seqencerHandle, this.ClientName);
            if (ret < 0)
                throw new AlsaException($"{nameof(snd_seq_set_client_name)} fails", ret);

            var portId = snd_seq_create_simple_port(this.seqencerHandle, this.ClientName,
                SeqPortCapability.SND_SEQ_PORT_CAP_WRITE | SeqPortCapability.SND_SEQ_PORT_CAP_SUBS_WRITE,
                SeqPortType.SND_SEQ_PORT_TYPE_APPLICATION);
            if (portId < 0)
                throw new AlsaException($"{nameof(snd_seq_create_simple_port)} fails", errnum: portId);
        }

        private void InitializeMidiParser()
        {
            var bufsize = 4;
            var ret = snd_midi_event_new((UIntPtr)bufsize, out this.parserHandle);
            if (ret < 0)
                throw new AlsaException($"{nameof(snd_midi_event_new)} fails", ret);

            // disable MIDI command merging (always write the command byte)
            snd_midi_event_no_status(this.parserHandle, on: 1);
        }

        private void PollLoop()
        {
            try
            {
                var pollFdsCount = snd_seq_poll_descriptors_count(this.seqencerHandle, PollEvent.POLLIN);
                Span<PollFd> pollFds = stackalloc PollFd[pollFdsCount];

                snd_seq_poll_descriptors(this.seqencerHandle, ref MemoryMarshal.GetReference(pollFds), (uint)pollFds.Length, PollEvent.POLLIN);

                while (this.pollLoopActive)
                {
                    if (poll(ref MemoryMarshal.GetReference(pollFds), (uint)pollFds.Length, timeout: 1000) > 0)
                        this.OnReceive();
                }
            }
            finally
            {
                this.pollLoopActive = false;
            }
        }

        private void OnReceive()
        {
            unsafe ref readonly snd_seq_event RetrieveEvent()
            {
                var ret = snd_seq_event_input(this.seqencerHandle, out var evt);
                if (ret < 0)
                    throw new AlsaException($"{nameof(snd_seq_event_input)} fails", ret);

                return ref Unsafe.AsRef<snd_seq_event>(evt);
            }

            do
            {
                ref readonly var evt = ref RetrieveEvent();

                switch (evt.type)
                {
                    case snd_seq_event_type.SND_SEQ_EVENT_CLIENT_EXIT:
                        this.Closed?.Invoke(this, EventArgs.Empty);
                        break;
                    case snd_seq_event_type.SND_SEQ_EVENT_SYSEX:
                        this.HandleMidiSysexEvent(evt);
                        break;
                    default:
                        this.HandleMidiEvent(evt);
                        break;
                }
            }
            while (snd_seq_event_input_pending(this.seqencerHandle, fetch_sequencer: 0) > 0);
        }

        private void HandleMidiSysexEvent(in snd_seq_event evt)
        {
            unsafe ReadOnlySpan<byte> GetExternalData(in snd_seq_event e)
                => new Span<byte>(e.data_ext_ptr.ToPointer(), (int)e.data_ext_len);

            var message = GetExternalData(evt).ToArray();
            this.ReceivedExclusiveMessage?.Invoke(this, new ReceivedExclusiveMessageEventArgs(message, this));
        }

        private void HandleMidiEvent(in snd_seq_event evt)
        {
            const int ENOENT = 2;

            Span<byte> buffer = stackalloc byte[4];
            var length = snd_midi_event_decode(this.parserHandle, ref MemoryMarshal.GetReference(buffer), buffer.Length, evt);
            if (length < 0)
            {
                if (length == -ENOENT)
                    return; // evt is not MIDI message

                throw new AlsaException($"{nameof(snd_midi_event_decode)} fails", errnum: length);
            }

            var midiEvent = MidiEvent.FromBytes(buffer.Slice(0, length));
            this.ReceivedMidiEvent?.Invoke(this, new ReceivedMidiEventEventArgs(midiEvent, this));
        }
    }
}
