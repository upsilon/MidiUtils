/* MidiUtils

LICENSE - The MIT License (MIT)

Copyright (c) 2013-2015 Tomona Nanase
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
using System.Collections.Generic;

namespace MidiUtils.IO
{
    public interface IMidiIn: IDisposable
    {
        bool IsPlaying { get; }

        event EventHandler<ReceivedMidiEventEventArgs> ReceivedMidiEvent;
        event EventHandler<ReceivedExclusiveMessageEventArgs> ReceivedExclusiveMessage;

        /// <summary>
        /// デバイスが開かれた時に発生します。
        /// </summary>
        event EventHandler Opened;

        /// <summary>
        /// デバイスが閉じられた時に発生します。
        /// </summary>
        event EventHandler Closed;

        /// <summary>
        /// MIDI-IN からの入力を開始します。
        /// </summary>
        void Start();

        /// <summary>
        /// MIDI-IN からの入力を停止します。
        /// </summary>
        void Stop();
    }

    public class ReceivedMidiEventEventArgs : EventArgs
    {
        #region -- Public Properties --

        public MidiEvent Event { get; private set; }

        public IMidiIn MidiIn { get; private set; }

        #endregion

        #region -- Constructors --

        public ReceivedMidiEventEventArgs(MidiEvent @event, IMidiIn midiIn)
        {
            Event = @event;
            MidiIn = midiIn;
        }

        #endregion
    }

    public class ReceivedExclusiveMessageEventArgs : EventArgs
    {
        #region -- Public Properties --

        public IEnumerable<byte> Message { get; private set; }

        public IMidiIn MidiIn { get; private set; }

        #endregion

        #region -- Constructors --

        public ReceivedExclusiveMessageEventArgs(IEnumerable<byte> message, IMidiIn midiIn)
        {
            Message = message;
            MidiIn = midiIn;
        }

        #endregion
    }
}
