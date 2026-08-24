extends Node

## Presentation-only shell. The external .NET host owns GameState and returns
## only a renderer-safe projection; this node never resolves gameplay commands.

@onready var renderer: ModernRenderer = $ModernRenderer
const DOTNET_SCRIPT := "../../eng/dotnet.ps1"
const HOST_ASSEMBLY := "../../tools/Telengard.GodotHost/bin/Debug/net8.0/Telengard.GodotHost.dll"
const CONTENT_ROOT := "../../content"

func _ready() -> void:
	var output: Array = []
	var exit_code := OS.execute("powershell.exe", ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", ProjectSettings.globalize_path(DOTNET_SCRIPT), "exec", ProjectSettings.globalize_path(HOST_ASSEMBLY), "--content-root", ProjectSettings.globalize_path(CONTENT_ROOT)], output, true)
	if exit_code != 0:
		_show_error("Unable to bootstrap authoritative session (exit %s)." % exit_code)
		return
	var parsed: Variant = JSON.parse_string("".join(output))
	if not parsed is Dictionary or not parsed.has("frame"):
		_show_error("Bootstrap returned an invalid renderer projection.")
		return
	renderer.render_frame(parsed["frame"])

func _show_error(message: String) -> void:
	var label := Label.new()
	label.text = message
	label.position = Vector2(72, 365)
	label.add_theme_font_size_override("font_size", 20)
	add_child(label)
