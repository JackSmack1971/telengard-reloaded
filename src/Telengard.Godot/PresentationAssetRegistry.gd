class_name PresentationAssetRegistry
extends RefCounted

## Presentation-only stable-ID lookup. Missing entries remain conspicuous in
## development and never require authoritative state to contain a resource path.
const PLACEHOLDER_PREFIX := "placeholder:"

var _resources: Dictionary = {}

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
