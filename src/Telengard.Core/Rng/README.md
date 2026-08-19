# Core Rng

`DeterministicRng` derives named streams from the world seed, generator
version, and stable scope values. The seed material is canonical binary data:
the world seed is a little-endian `Int64`; strings are UTF-8 with little-endian
`UInt32` byte lengths; and scopes have an explicit little-endian `UInt32` count
and remain in caller order. This avoids culture-sensitive numeric formatting
and delimiter collisions.

Streams are independent and expose only deterministic primitive draws;
simulation systems own how those draws affect authoritative state.
