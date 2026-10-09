using System;
using System.Linq;
using WsjtxUtilsPatch.WsjtxMessages.Messages;

namespace WsjtxUtilsPatch.WsjtxMessages.QsoParsing
{
    /// <summary>
    /// QSO parser for WSJT-X modes with 72-bit message payloads (JT4, JT9, JT65 and T10)
    /// </summary>
    /// <remarks>
    /// 72-bit payloads are one of: "CQ|QRZ [modifier] callsign grid4" (the gridsquare
    /// is always four characters), "callsign callsign grid4|report|RO|RRR|RR73|73"
    /// (JT65 VHF deep sync appends "OOO" as the third field), or free text of up
    /// to thirteen characters from the character set 0-9A-Z +-./?. The shorthands
    /// "RO", "RRR", "RR73" and "73" can also be decoded without callsigns.
    /// </remarks>
    internal class WsjtxQsoParser72BitModes : BaseOsoParser
    {
        private readonly Decode _decodeMessage;
        private readonly string[] _parts;
        private readonly bool _usedPriori;
        private int _deCallSignIndex;
        private int _dxCallSignIndex;

        /// <summary>
        /// Constructs a QSO parser for WSJT-X modes with 72-bit message payloads
        /// </summary>
        /// <param name="decodeMessage"></param>
        public WsjtxQsoParser72BitModes(Decode decodeMessage)
        {
            _decodeMessage = decodeMessage;
            _parts = NormalizedSplit(_decodeMessage.Message, out _usedPriori);
        }

        /// <summary>
        /// Parse the QSO
        /// </summary>
        /// <returns></returns>
        public override WsjtxQso Parse()
        {
            WsjtxQso result = new WsjtxQso(_decodeMessage);
            result.UsedPriori = _usedPriori;
            _deCallSignIndex = 1;
            _dxCallSignIndex = 0;

            // a bare shorthand or a single free-text word
            if (_parts.Length < 2)
            {
                ParseBareShorthand(result);
                return result;
            }

            // "TNX 73 GL" is valid free text with signoff semantics
            if (string.Join(" ", _parts) == "TNX 73 GL")
            {
                _deCallSignIndex = _dxCallSignIndex = -1;
                result.QsoState = WsjtxQsoState.Signoff;
                return result;
            }

            if (IsCQ(_parts.First()))
            {
                ParseCQ(result);
            }
            else if (_parts.Length == 2)
            {
                ParseTwoParts(result);
            }
            else if (_parts.Length == 3)
            {
                ParseThreeParts(result);
            }
            else
            {
                ParseFourOrMoreParts(result);
            }

            ParseCallsigns(result);
            return result;
        }

        #region Private Methods
        /// <summary>
        /// Parse a shorthand decoded without callsigns
        /// </summary>
        /// <param name="qso"></param>
        private void ParseBareShorthand(WsjtxQso qso)
        {
            _deCallSignIndex = _dxCallSignIndex = -1;
            if (_parts.Length == 0)
                return;

            switch (_parts[0])
            {
                case "RRR":
                case "RR73":
                    qso.QsoState = WsjtxQsoState.Rogers;
                    break;
                case "73":
                    qso.QsoState = WsjtxQsoState.Signoff;
                    break;
                case "RO":
                    qso.Report = "RO";
                    qso.QsoState = WsjtxQsoState.RogerReport;
                    break;
                default:
                    qso.Report = _parts[0];
                    break;
            }
        }

        /// <summary>
        /// Parse a CQ or QRZ message
        /// </summary>
        /// <param name="qso"></param>
        private void ParseCQ(WsjtxQso qso)
        {
            _dxCallSignIndex = -1;
            qso.QsoState = WsjtxQsoState.CallingCq;

            // "CQ DX K1ABC FN42": the modifier is repacked into the first callsign
            // field, a bare "CQ nnn" carries no callsign at all
            if (_parts.Length == 4)
            {
                qso.CallingModifier = _parts[1];
                _deCallSignIndex++;
            }

            // find the gridsquare if provided (always four characters in 72-bit payloads)
            if (_deCallSignIndex + 1 < _parts.Length && TryGetGridsquare(_parts[_deCallSignIndex + 1], out string gridSquare))
            {
                qso.GridSquare = gridSquare;
            }
        }

        /// <summary>
        /// Parse a two part message
        /// </summary>
        /// <param name="qso"></param>
        private void ParseTwoParts(WsjtxQso qso)
        {
            // is this two callsigns with nothing else?
            if (IsValidCallsign(_parts[0]) && IsValidCallsign(_parts[1]))
            {
                qso.QsoState = WsjtxQsoState.CallingStation;
            }
            else
            {
                _deCallSignIndex = _dxCallSignIndex = -1;
                qso.Report = string.Join(" ", _parts);
            }
        }

        /// <summary>
        /// Parse a three part message
        /// </summary>
        /// <param name="qso"></param>
        private void ParseThreeParts(WsjtxQso qso)
        {
            var lastPart = _parts[_parts.Length - 1];
            qso.Report = lastPart;

            if (lastPart == "RRR" || lastPart == "RR73")
            {
                qso.QsoState = WsjtxQsoState.Rogers;
            }
            else if (lastPart == "73")
            {
                qso.QsoState = WsjtxQsoState.Signoff;
            }
            else if (lastPart == "RO")
            {
                qso.QsoState = WsjtxQsoState.RogerReport;
            }
            else if (lastPart == "OOO")
            {
                // JT65 VHF deep sync appends "OOO" in place of a report
                qso.QsoState = WsjtxQsoState.Report;
            }
            else if (TryGetReceptionReport(lastPart, out string report))
            {
                qso.QsoState = lastPart.StartsWith("R") ?
                        WsjtxQsoState.RogerReport :
                        WsjtxQsoState.Report;
            }
            else if (TryGetGridsquare(lastPart, out string gridSquare))
            {
                qso.Report = string.Empty;
                qso.GridSquare = gridSquare;
                qso.QsoState = WsjtxQsoState.CallingStation;
            }
            else
            {
                _deCallSignIndex = _dxCallSignIndex = -1;
                qso.Report = string.Join(" ", _parts);
            }
        }

        /// <summary>
        /// Parse a four or more part message
        /// </summary>
        /// <param name="qso"></param>
        private void ParseFourOrMoreParts(WsjtxQso qso)
        {
            var lastPart = _parts[_parts.Length - 1];

            // 72-bit structured payloads carry at most three tokens apart from CQ
            // messages; treat a longer message with two leading callsigns and a
            // gridsquare or report at the end like a three part message
            if (IsValidCallsign(_parts[0]) && IsValidCallsign(_parts[1]))
            {
                if (TryGetGridsquare(lastPart, out string gridSquare))
                {
                    qso.GridSquare = gridSquare;
                    qso.QsoState = WsjtxQsoState.CallingStation;
                }
                else if (TryGetReceptionReport(lastPart, out string report))
                {
                    qso.Report = string.Join(" ", _parts.Skip(2));
                    qso.QsoState = lastPart.StartsWith("R") ?
                        WsjtxQsoState.RogerReport :
                        WsjtxQsoState.Report;
                }
                else
                {
                    _deCallSignIndex = _dxCallSignIndex = -1;
                    qso.Report = string.Join(" ", _parts);
                }
            }
            else
            {
                _deCallSignIndex = _dxCallSignIndex = -1;
                qso.Report = string.Join(" ", _parts);
            }
        }

        /// <summary>
        /// Parse callsigns if available
        /// </summary>
        /// <param name="qso"></param>
        private void ParseCallsigns(WsjtxQso qso)
        {
            if (_deCallSignIndex > -1 && _deCallSignIndex < _parts.Length && TryGetCallsign(_parts[_deCallSignIndex], out string deCallsign))
            {
                qso.DECallsign = deCallsign;
            }

            if (_dxCallSignIndex > -1 && _dxCallSignIndex < _parts.Length && TryGetCallsign(_parts[_dxCallSignIndex], out string dxCallsign))
            {
                qso.DXCallsign = dxCallsign;
            }
        }
        #endregion

        #region Static Methods
        /// <summary>
        /// Cleans a 72-bit mode message split into relevant parts
        /// </summary>
        /// <param name="message"></param>
        /// <param name="usedPriori"></param>
        /// <returns></returns>
        private static string[] NormalizedSplit(string message, out bool usedPriori)
        {
            var parts = message
                .Split(new[] { ' ' }, options: StringSplitOptions.RemoveEmptyEntries)
                .Where(part => part != "?")
                .ToArray();

            // strip trailing decoder flags (e.g. JT65 "a3" AP flags, JT4 "f2" sync flags)
            return StripTrailingDecoderFlags(parts, out usedPriori);
        }

        /// <summary>
        /// Is the part a CQ or QRZ
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        private static bool IsCQ(string part)
        {
            return part == "CQ" || part == "QRZ";
        }
        #endregion
    }
}
