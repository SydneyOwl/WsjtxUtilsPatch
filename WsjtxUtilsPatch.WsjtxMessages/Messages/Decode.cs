using System;

namespace WsjtxUtilsPatch.WsjtxMessages.Messages
{
    /// <summary>
    /// WSJT-X Decode message
    /// </summary>
    /// <remarks>
    /// The decode message is sent when a new decode is completed
    /// </remarks>
    public class Decode : WsjtxMessage, IWsjtxDirectionOut
    {
        /// <summary>
        /// Constructs a default WSJT-X Decode message
        /// </summary>
        public Decode() : base(string.Empty, MessageType.Decode)
        {
            Mode = string.Empty;
            Message = string.Empty;
        }

        /// <summary>
        /// Is this a new decode or a replay
        /// </summary>
        public bool New { get; set; }

        /// <summary>
        /// Milli-seconds since midnight
        /// </summary>
        public uint Time { get; set; }

        /// <summary>
        /// Signal-to-noise ratio in dB
        /// </summary>
        public int Snr { get; set; }

        /// <summary>
        /// Time offset in seconds relative to the local computer clock
        /// </summary>
        public float OffsetTimeSeconds { get; set; }

        /// <summary>
        /// Frequency offset from nominal in Hertz
        /// </summary>
        public uint OffsetFrequencyHz { get; set; }

        /// <summary>
        /// The selected WSJT-X mode
        /// </summary>
        public string Mode { get; set; }

        /// <summary>
        /// The message exchanged
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Low confidence decodes are flagged in protocols where the decoder
        /// has knows that a decode has a higher than normal probability
        /// of being false, they should not be reported on publicly
        /// accessible services without some attached warning or further validation.
        /// </summary>
        public bool LowConfidence { get; set; }

        /// <summary>
        /// Off air decodes are those that result from playing back a .WAV file.
        /// </summary>
        public bool OffAir { get; set; }

        /// <summary>
        /// Convert the mode notation into a more friendly mode string
        /// </summary>
        /// <remarks>
        /// https://physics.princeton.edu/pulsar/k1jt/wsjtx-doc/wsjtx-main-2.5.4.html#_decoded_lines
        /// WSJT-X and JTDX reuse the same single-character notations with different meanings:
        /// JTDX emits ':' for FT4 and '+' for T10, while WSJT-X emits ':' for Q65 and '+' for FT4.
        /// The message id tells the sources apart.
        /// </remarks>
        /// <returns></returns>
        public string DecodeModeNotationsToString()
        {
            return Mode switch
            {
                "`" => "FST4",
                "+" => IsJtdxSource ? "T10" : "FT4",
                "~" => "FT8",
                "$" => "JT4",
                "@" => "JT9",
                "#" => "JT65",
                ":" => IsJtdxSource ? "FT4" : "Q65",
                "&" => "MSK144",
                _ => string.Empty
            };
        }

        /// <summary>
        /// JTDX always uses "JTDX" as message id, optionally suffixed with " - &lt;rig-name&gt;"
        /// or " - test" for multi-instance setups; the id cannot be replaced.
        /// </summary>
        private bool IsJtdxSource => Id == "JTDX" || (Id?.StartsWith("JTDX - ", StringComparison.Ordinal) ?? false);

        #region IWsjtxDirectionOut
        /// <summary>
        ///  Using the <see cref="WsjtxMessageReader"/>, deserialize the values to the current message
        /// </summary>
        /// <param name="messageReader"></param>
        public override void ReadMessage(WsjtxMessageReader messageReader)
        {
            base.ReadMessage(messageReader);

            New = messageReader.ReadBool();
            Time = messageReader.ReadUInt32();
            Snr = messageReader.ReadInt32();
            OffsetTimeSeconds = (float)messageReader.ReadDouble();
            OffsetFrequencyHz = messageReader.ReadUInt32();
            Mode = messageReader.ReadString();
            Message = messageReader.ReadString();
            LowConfidence = messageReader.ReadBool();
            OffAir = messageReader.ReadBool();
        }
        #endregion
    }
}
