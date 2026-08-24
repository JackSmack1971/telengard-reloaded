extends Node

## Presentation-only shell. The external .NET host owns GameState and returns
## only a renderer-safe projection; this node never resolves gameplay commands.

@onready var renderer: ModernRenderer = $ModernRenderer
var _http: HTTPRequest
var _host_pid := -1
enum ClientState { STARTUP, TITLE, NEW_GAME, LOAD_GAME, CHARACTER_CREATION, INN, DUNGEON, PAUSE, DEATH, RETURN_TO_INN }
var _client_state := ClientState.STARTUP
var _previous_authoritative_scene := ""
var _feedback := ""
var _clock_accumulator := 0.0
var _request_pending := false
var _queued_intent: Dictionary = {}
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
	if _client_state == ClientState.TITLE:
		if event.is_action_pressed("new_game"):
			_client_state = ClientState.NEW_GAME
		elif event.is_action_pressed("load_game"):
			_client_state = ClientState.LOAD_GAME
		elif event.is_action_pressed("ui_accept"):
			_client_state = ClientState.NEW_GAME
		elif event.is_action_pressed("ui_cancel"):
			_quit_client()
		_refresh_renderer()
		return
	if _client_state == ClientState.NEW_GAME or _client_state == ClientState.LOAD_GAME:
		if event.is_action_pressed("ui_cancel"):
			_client_state = ClientState.TITLE
			_refresh_renderer()
		elif event.is_action_pressed("ui_accept"):
			_client_state = ClientState.CHARACTER_CREATION if _client_state == ClientState.NEW_GAME else ClientState.INN
			_refresh_renderer()
		return
	if _client_state == ClientState.CHARACTER_CREATION:
		if event.is_action_pressed("ui_cancel"):
			_client_state = ClientState.NEW_GAME
			_refresh_renderer()
		elif event.is_action_pressed("ui_accept"):
			_client_state = ClientState.INN
			_refresh_renderer()
		return
	if _client_state == ClientState.PAUSE:
		if event.is_action_pressed("ui_cancel") or event.is_action_pressed("toggle_pause"):
			_client_state = _authoritative_state()
			_send_intent({"type": "time_mode", "mode": "Normal"})
			_refresh_renderer()
		return
	if _client_state == ClientState.DEATH:
		if event.is_action_pressed("ui_accept"):
			_client_state = ClientState.TITLE
			_refresh_renderer()
		return
	if _client_state == ClientState.RETURN_TO_INN:
		if event.is_action_pressed("ui_accept"):
			_client_state = ClientState.INN
			_refresh_renderer()
		return
	if _client_state == ClientState.DUNGEON and event.is_action_pressed("move_north"):
		_send_intent({"type": "move", "direction": "North"})
	elif _client_state == ClientState.DUNGEON and event.is_action_pressed("move_south"):
		_send_intent({"type": "move", "direction": "South"})
	elif _client_state == ClientState.DUNGEON and event.is_action_pressed("move_east"):
		_send_intent({"type": "move", "direction": "East"})
	elif _client_state == ClientState.DUNGEON and event.is_action_pressed("move_west"):
		_send_intent({"type": "move", "direction": "West"})
	elif _client_state == ClientState.INN and event.is_action_pressed("enter_dungeon"):
		_send_intent({"type": "enter_dungeon"})
	elif (_client_state == ClientState.INN or _client_state == ClientState.DUNGEON) and event.is_action_pressed("toggle_pause"):
		_client_state = ClientState.PAUSE
		_send_intent({"type": "time_mode", "mode": "Paused"})
		_refresh_renderer()

func _process(delta: float) -> void:
	if _client_state not in [ClientState.INN, ClientState.DUNGEON]:
		_clock_accumulator = 0.0
		return
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
	_register_key("new_game", KEY_N)
	_register_key("load_game", KEY_L)
	_register_joy("move_north", JOY_AXIS_LEFT_Y, -1.0)
	_register_joy("move_south", JOY_AXIS_LEFT_Y, 1.0)
	_register_joy("move_east", JOY_AXIS_LEFT_X, 1.0)
	_register_joy("move_west", JOY_AXIS_LEFT_X, -1.0)
	_register_key("ui_accept", KEY_ENTER)
	_register_key("ui_cancel", KEY_ESCAPE)

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
	if _request_pending:
		return
	_request_pending = true
	var request_result := _http.request("http://127.0.0.1:18120/frame")
	if request_result != OK:
		_request_pending = false
		_show_error("Unable to request the authoritative session frame (%s)." % request_result)

func _send_intent(intent: Dictionary) -> void:
	if _request_pending:
		if intent.get("type", "") != "advance":
			_queued_intent = intent.duplicate(true)
		return
	_request_pending = true
	var request_result := _http.request("http://127.0.0.1:18120/command", ["Content-Type: application/json"], HTTPClient.METHOD_POST, JSON.stringify(intent))
	if request_result != OK:
		_request_pending = false
		_show_error("Unable to submit authoritative intent (%s)." % request_result)

func _on_request_completed(result: int, response_code: int, _headers: PackedStringArray, body: PackedByteArray) -> void:
	_request_pending = false
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
		_update_authoritative_state(parsed["frame"])
		_refresh_renderer()
	if not _queued_intent.is_empty():
		var queued_intent := _queued_intent
		_queued_intent = {}
		_send_intent(queued_intent)

func _update_authoritative_state(frame: Dictionary) -> void:
	if _client_state == ClientState.STARTUP:
		_client_state = ClientState.TITLE
		_previous_authoritative_scene = str(frame.get("scene", "inn"))
		return
	if _client_state in [ClientState.TITLE, ClientState.NEW_GAME, ClientState.LOAD_GAME, ClientState.CHARACTER_CREATION, ClientState.PAUSE, ClientState.DEATH, ClientState.RETURN_TO_INN]:
		return
	var authoritative_scene := str(frame.get("scene", "inn"))
	if not bool(frame.get("hud", {}).get("alive", true)):
		_client_state = ClientState.DEATH
	elif _previous_authoritative_scene == "dungeon" and authoritative_scene == "inn":
		_client_state = ClientState.RETURN_TO_INN
	else:
		_client_state = ClientState.DUNGEON if authoritative_scene == "dungeon" else ClientState.INN
	_previous_authoritative_scene = authoritative_scene

func _authoritative_state() -> int:
	return ClientState.DUNGEON if renderer.current_scene() == "dungeon" else ClientState.INN

func _refresh_renderer() -> void:
	renderer.set_client_state(ClientState.keys()[_client_state], _feedback)

func _quit_client() -> void:
	get_tree().quit()

func _show_error(message: String) -> void:
	var label := Label.new()
	label.text = message
	label.position = Vector2(72, 365)
	label.add_theme_font_size_override("font_size", 20)
	add_child(label)
