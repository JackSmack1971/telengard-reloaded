class_name PresentationAssetRegistry
extends RefCounted

## Presentation-only stable-ID lookup. Missing entries remain conspicuous in
## development and never require authoritative state to contain a resource path.
const PLACEHOLDER_PREFIX := "placeholder:"

var _resources: Dictionary = {}

const DEFAULT_PLACEHOLDER_COLORS := {
	"feature.fountain.azure": Color("55b9d6"),
	"azure-fountain": Color("55b9d6"),
	"feature.altar.stone": Color("b58ad8"),
	"stone-altar": Color("b58ad8"),
	"feature.pit.bottomless": Color("d36f78"),
	"bottomless-pit": Color("d36f78"),
	"feature.teleporter.network": Color("72d6b0"),
	"network-teleporter": Color("72d6b0"),
	"cave-ooze": Color("8ed16b"),
	"cave-rat": Color("c49a6c"),
	"crypt-wight": Color("d9d5c8"),
	"gargoyle-sentinel": Color("a7a9b8"),
	"goblin-skirmisher": Color("9ecb68"),
	"lesser-imp": Color("d97878"),
	"rust-beetle": Color("c78c51"),
	"skeleton-guard": Color("d8d0bd")
}

func _init(entries: Dictionary = {}) -> void:
	for key in entries:
		if _resources.has(key):
			push_error("Duplicate presentation asset key: %s" % key)
			continue
		_resources[key] = entries[key]

func resolve(key: String) -> String:
	if _resources.has(key) and not str(_resources[key]).is_empty():
		return str(_resources[key])
	return PLACEHOLDER_PREFIX + key

func placeholder_color(key: String) -> Color:
	return DEFAULT_PLACEHOLDER_COLORS.get(key, Color("d9ad62"))
