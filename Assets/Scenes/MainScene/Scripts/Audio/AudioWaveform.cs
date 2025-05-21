using System;
using System.ComponentModel;

namespace Audio
{
    [Serializable]
    public class AudioWaveform
    {
        [Description("PCM amplitude samples (e.g. 1024 or 2048 values).")]
        public float[] samples;
        public float timestamp;
    }
}