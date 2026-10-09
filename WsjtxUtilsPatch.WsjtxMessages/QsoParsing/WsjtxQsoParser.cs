using System;
using WsjtxUtilsPatch.WsjtxMessages.Messages;

namespace WsjtxUtilsPatch.WsjtxMessages.QsoParsing
{
    /// <summary>
    /// WSJT-X decode message QSO parser
    /// </summary>
    public static class WsjtxQsoParser
    {
        /// <summary>
        /// Attempt to parse as much information as possible about the OSO of a WSJT-X decode packet
        /// </summary>
        /// <param name="decode"></param>
        /// <returns></returns>
        public static WsjtxQso ParseDecode(Decode decode)
        {
            var mode = decode.DecodeModeNotationsToString();
            switch (mode)
            {
                // 77-bit payloads (FT2 uses the FT4 message format)
                case "FST4":
                case "FT2":
                case "FT4":
                case "FT8":
                case "MSK144":
                case "Q65":
                    return new WsjtxQsoParser77BitModes(decode).Parse();
                // 72-bit payloads
                case "JT4":
                case "JT9":
                case "JT65":
                case "T10":
                    return new WsjtxQsoParser72BitModes(decode).Parse();
                default:
                    throw new NotImplementedException($"A QSO parser for {mode} is not implemented");
            }
        }
    }
}
