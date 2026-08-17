# Core Rng

`DeterministicRng` derives named streams from the world seed, generator
version, and stable scope values. Streams are independent and expose only
deterministic primitive draws; simulation systems own how those draws affect
authoritative state.
