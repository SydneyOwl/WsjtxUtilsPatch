# WsjtxUtilsPatch

A fork of [KC3PIB/WsjtxUtils](https://github.com/KC3PIB/WsjtxUtils) with patches for JTDX
compatibility, additional decode mode notations, and QSO parsers for the 72-bit message family.

**Note: Although most APIs stay the same as WsjtxUtils, it is still not recommended to reference
this NuGet package in your project. This package has been customized for use in other projects
and may not meet your requirements. Additionally, this package cannot be found via NuGet search
and can only be downloaded by explicitly specifying the package name and version.**

Original repo: https://github.com/KC3PIB/WsjtxUtils. Many thanks to KC3PIB!

```
<PackageReference Include="WsjtxUtilsPatch.WsjtxMessages" Version="1.1.0" />
<PackageReference Include="WsjtxUtilsPatch.WsjtxUdpServer" Version="1.1.0" />
```

## Changes vs upstream KC3PIB/WsjtxUtils

### WsjtxUdpServer

- Rewrote the server around `UdpClient` instead of raw `Socket` and removed the
  `NETFRAMEWORK`/net48 code paths (upstream 1.0.5/1.0.6 era changes).
- `HandleRawMessageAsync` is now `virtual`, so subclasses can observe raw frames.
- Fire-and-forget raw handler tasks now log their exceptions instead of losing them.
- Target frameworks: net6.0 / net8.0 / net9.0 only.

### WsjtxMessages

- **1.0.7** — fixed the JTDX/WSJT-X mode notation collision in
  `Decode.DecodeModeNotationsToString()`. JTDX reuses the same single-character notations with
  different meanings: `:` is FT4 on JTDX but Q65 on WSJT-X, and `+` is T10 on JTDX but FT4 on
  WSJT-X. The source is told apart by the message id (JTDX always uses `JTDX` or `JTDX - <name>`).
- **1.1.0** — added the missing decode notations: `#*` and `##` (JT65 VHF variants), `$*` and
  `$#` (JT4 — a bare `$` is never actually emitted by the decoder), `^` (MSK144 long averaging)
  and `*` (FT2 from WSJT-X Improved 3.1.0).
- **1.1.0** — new QSO parser for the 72-bit message family (JT4, JT9, JT65, T10);
  `WsjtxQsoParser.ParseDecode()` previously threw `NotImplementedException` for those modes.
- **1.1.0** — decoded messages now have trailing decoder annotation flags (e.g. the AP flags
  `a3`/`a23`, JT4 sync flags `f2`/`d23`, Q65 flags `q1`) stripped before parsing, so `Report`
  no longer contains the flag and the flag now sets `UsedPriori` reliably. This also fixes
  three-character AP flags such as `a23`, which the old `a[1-7]` pattern could not match.

## Decode line notations

The 5th field of a decoder output line is the mode notation; `Decode.DecodeModeNotationsToString()`
maps it to a mode name. Sources:

| Notation | WSJT-X | WSJT-X Improved | JTDX |
| -------- | ------ | --------------- | ---- |
| `~` | FT8 | FT8 | FT8 |
| `+` | FT4 | FT4 | T10 |
| `` ` `` | FST4 | FST4 | — |
| `:` | Q65 | Q65 | FT4 |
| `@` | JT9 | JT9 | JT9 (all submodes) |
| `#` | JT65 | JT65 | JT65 (no VHF variants) |
| `#*` | JT65 (VHF deep sync) | — | — |
| `##` | JT65 (VHF, message text ends with `OOO`) | — | — |
| `$` | JT4 (kept as a fallback, never emitted) | — | — |
| `$*` | JT4 (sync polarity +1) | — | — |
| `$#` | JT4 (sync polarity −1) | — | — |
| `&` | MSK144 | MSK144 | — |
| `^` | MSK144 (long averaging) | — | — |
| `*` | — | FT2 | — |

Notes:

- JTDX's combined "JT9+JT65" mode runs both decoders and emits each decode with its own source
  notation (`#` for the JT65 decoder, `@` for the JT9 decoder), so the mapping stays unambiguous.
- WSPR, WSPR-2 and FST4W decodes are delivered as the dedicated `WSPRDecode` message type and
  never produce `Decode` messages.
- ISCAT and Echo do not produce `Decode` messages; QRA64 is dead code in both WSJT-X and JTDX —
  none of them are mapped.

## QSO parsing

`WsjtxQsoParser.ParseDecode(decode)` extracts the QSO state, callsigns, gridsquare and signal
report from a decoded message:

| Mode | Payload family | Parser |
| ---- | -------------- | ------ |
| FT8, FT4, FT2, MSK144, Q65, FST4 | 77-bit | `WsjtxQsoParser77BitModes` |
| JT4, JT9, JT65, T10 | 72-bit | `WsjtxQsoParser72BitModes` (new in 1.1.0) |

FT2 (WSJT-X Improved 3.1.0) reuses the FT4 decoder and the FT4-style 77-bit payload, so it is
routed to the 77-bit parser.

The resulting `WsjtxQso` carries:

- `QsoState`: `CallingCq`, `CallingStation`, `Report`, `RogerReport`, `Rogers`, `Signoff` or
  `Unknown` (free text).
- `DECallsign` / `DXCallsign`: the sending station is the second callsign of a two-call message,
  the addressee is the first.
- `GridSquare`: 4 characters for 72-bit payloads, 4 or 6 for 77-bit payloads.
- `Report`: the signal report (`-15`, `R-15`, `+05`, `R+05`, `RO`, `OOO` …).
- `CallingModifier`: the `DX`/`TEST`/`POTA`… part of a CQ, when present.
- `UsedPriori`: whether a-prefixed AP decoder flags were present.

72-bit specifics handled by the parser: `CQ`/`QRZ`/`CQ nnn`/`CQ DX …` calls, compound callsigns
(`CQ PJ2/K1ABC FN42`), bare shorthands decoded without callsigns (`RRR`, `RR73`, `73`, `RO`),
`TNX 73 GL`, JT65 VHF `OOO` messages and T10 reports down to −33 dB.

## Version history

- **1.1.0** — 72-bit QSO parser (JT4/JT9/JT65/T10), `#*`/`##`/`$*`/`$#`/`^`/`*` decode
  notations, FT2 support, trailing decoder-flag stripping (also fixes `a23` and Report
  pollution in the 77-bit parser).
- **1.0.7** — JTDX vs WSJT-X single-character notation collision fix (`:`/`+`).
- **1.0.6** — `UdpClient` server rewrite, `HandleRawMessageAsync` made virtual, raw handler
  exception logging, net6/net8/net9 only.
- **1.0.5** — initial fork maintenance release over upstream 1.0.4.
