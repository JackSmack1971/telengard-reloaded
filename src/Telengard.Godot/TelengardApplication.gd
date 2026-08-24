extends Node

## Presentation-only shell. The external .NET host owns GameState and returns
## only a renderer-safe projection; this node never resolves gameplay commands.

@onready var renderer: ModernRenderer = $ModernRenderer
var _http: HTTPRequest
var _host_pid := -1
var _input_context := "INN"
var _feedback := ""
var _clock_accumulator := 0.0
var _paused := false
const DOTNET_SCRIPT := "../../eng/dotnet.ps1"
const HOST_ASSEMBLY := "../../tools/Telengard.GodotHost/bin/Debug/net8.0/Telengard.GodotHost.dll"
const CONTENT_ROOT := "../../content"

func _ready() -> void:
	_register_input_actions()
	_http = HTTPRequest.new()
	add_child(_http)
	_http.request_completed.connect(_on_request_completed)
	_host_pid = OS.create_process("powershell.exe", ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", ProjectSettings.globalize_path(DOTNET_SCRIPT), "exec", ProjectSettings.globalize_path(HOST_ASSEMBLY), "--serve", "--content-root", ProjectSettings.globalize_path(CONTENT_ROOT)])
	_request_frame.call_deferred()

func _unhandled_input(event: InputEvent) -> void:
	if _input_context == "EXPLORATION" and event.is_action_pressed("move_north"):
		_send_intent({"type": "move", "direction": "North"})
	elif _input_context == "EXPLORATION" and event.is_action_pressed("move_south"):
		_send_intent({"type": "move", "direction": "South"})
	elif _input_context == "EXPLORATION" and event.is_action_pressed("move_east"):
		_send_intent({"type": "move", "direction": "East"})
	elif _input_context == "EXPLORATION" and event.is_action_pressed("move_west"):
		_send_intent({"type": "move", "direction": "West"})
	elif _input_context == "INN" and event.is_action_pressed("enter_dungeon"):
		_send_intent({"type": "enter_dungeon"})
	elif event.is_action_pressed("toggle_pause"):
		_paused = not _paused
		_send_intent({"type": "time_mode", "mode": "Paused" if _paused else "Normal"})

func _process(delta: float) -> void:
	_clock_accumulator += delta
	if _host_pid > 0 and _clock_accumulator >= 0.1 and _http.get_http_client_status() != HTTPClient.STATUS_REQUESTING:
		var elapsed := _clock_accumulator
		_clock_accumulator = 0.0
		_send_intent({"type": "advance", "elapsed_seconds": elapsed})

func _register_input_actions() -> void:
	_register_key("move_north", KEY_W)
	_register_key("move_south", KEY_S)
	_register_key("move_east", KEY_D)
	_register_key("move_west", KEY_A)
	_register_key("enter_dungeon", KEY_E)
	_register_key("toggle_pause", KEY_ESCAPE)
	_register_joy("move_north", JOY_AXIS_LEFT_Y, -1.0)
	_register_joy("move_south", JOY_AXIS_LEFT_Y, 1.0)
	_register_joy("move_east", JOY_AXIS_LEFT_X, 1.0)
	_register_joy("move_west", JOY_AXIS_LEFT_X, -1.0)

func _register_key(action: StringName, keycode: Key) -> void:
	if not InputMap.has_action(action):
		InputMap.add_action(action)
	var key := InputEventKey.new()
	key.keycode = keycode
	InputMap.action_add_event(action, key)

func _register_joy(action: StringName, axis: JoyAxis, value: float) -> void:
	var joy := InputEventJoypadMotion.new()
	joy.axis = axis
	joy.axis_value = value
	InputMap.action_add_event(action, joy)

func _request_frame() -> void:
	_http.request("http://127.0.0.1:18120/frame")

func _send_intent(intent: Dictionary) -> void:
	_http.request("http://127.0.0.1:18120/command", ["Content-Type: application/json"], HTTPClient.METHOD_POST, JSON.stringify(intent))

func _on_request_completed(result: int, response_code: int, _headers: PackedStringArray, body: PackedByteArray) -> void:
	if result != HTTPRequest.RESULT_SUCCESS:
		_show_error("Unable to reach authoritative session (network result %s)." % result)
		return
	var parsed: Variant = JSON.parse_string(body.get_string_from_utf8())
	if not parsed is Dictionary:
		_show_error("Authoritative session returned invalid JSON.")
		return
	if parsed.has("frame"):
		renderer.render_frame(parsed["frame"])
		_feedback = str(parsed.get("error", ""))
		if not _feedback.is_empty():
			_show_error(_feedback)
		_input_context = "EXPLORATION" if parsed["frame"].get("scene", "inn") == "dungeon" else "INN"

func _show_error(message: String) -> void:
	var label := Label.new()
	label.text = message
	label.position = Vector2(72, 365)
	label.add_theme_font_size_override("font_size", 20)
	add_child(label)
