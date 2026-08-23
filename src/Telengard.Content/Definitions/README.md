# Content definitions

`ContentPackLoader.Load` reads a content pack from a directory. The pack must
contain a `manifest.json` with a nonblank `contentVersion` (the loader also
accepts the specification spelling `content_version`). Definition properties
accept both the schema's snake_case names and the equivalent .NET casing. Each
JSON file under the existing type directories defines one item:

```text
manifest.json
monsters/*.json
items/*.json
spells/*.json
features/*.json
talents/*.json
loot_tables/*.json
bands/*.json
```

Definitions use the fields of the corresponding validated in-memory schema;
map-valued fields are direct JSON objects so arbitrary map keys remain valid.
Files are read in ordinal filename order, ids must be unique within each
catalog, and loot-table item references and monster loot-table references must
resolve. Band ranges are inclusive, limited to the first-slice floors 1–5, and
must not overlap. Missing optional type directories are allowed so later
content slices can add their files incrementally. The loaded
`ContentPack.ContentVersion` is an explicit input for consumers; it does not
change save schema versioning. Band ecology identifiers remain optional until
their owning content tickets add the referenced catalogs.
