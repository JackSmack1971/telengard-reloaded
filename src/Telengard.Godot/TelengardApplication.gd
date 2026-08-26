extends Node

## Presentation-only shell. The external .NET host owns GameState and returns
## only a renderer-safe projection; this node translates input to host intent.

@onready var renderer: ModernRenderer = $ModernRenderer
var _http: HTTPRequest
var _host_pid := -1
enum ClientState { STARTUP, TITLE, NEW_GAME, LOAD_GAME, CHARACTER_CREATION, INN, DUNGEON, PAUSE, DEATH, RETURN_TO_INN }
enum InputContext { GLOBAL, INN, EXPLORATION, COMBAT, MENU, DEATH }
var _client_state := ClientState.STARTUP
var _title_selection := ClientState.NEW_GAME
var _panel := ""
var _panel_selection := 0
var _previous_authoritative_scene := ""
var _resume_state := ClientState.INN
var _feedback := ""
var _clock_accumulator := 0.0
var _request_pending := false
var _queued_intents: Array[Dictionary] = []
var _initial_frame_attempts := 0
var _current_frame: Dictionary = {}
const DOTNET_EXECUTABLE := "../../.dotnet/dotnet.exe"
const HOST_ASSEMBLY := "../../tools/Telengard.GodotHost/bin/Debug/net8.0/Telengard.GodotHost.dll"
const CONTENT_ROOT := "../../content"
const GAMEPLAY_CONFIG := "../../tools/Telengard.GodotHost/first-slice.config.json"
const MAX_INITIAL_FRAME_ATTEMPTS := 20

func _ready() -> void:
	_register_input_actions()
	_http = HTTPRequest.new()
	add_child(_http)
	_http.request_completed.connect(_on_request_completed)
	_host_pid = OS.create_process(ProjectSettings.globalize_path(DOTNET_EXECUTABLE), ["exec", ProjectSettings.globalize_path(HOST_ASSEMBLY), "--serve", "--content-root", ProjectSettings.globalize_path(CONTENT_ROOT), "--gameplay-config", ProjectSettings.globalize_path(GAMEPLAY_CONFIG)])
	_request_frame.call_deferred()

func _input_context() -> InputContext:
	if _client_state == ClientState.DEATH: return InputContext.DEATH
	if _panel != "" or _client_state in [ClientState.TITLE, ClientState.NEW_GAME, ClientState.LOAD_GAME, ClientState.CHARACTER_CREATION, ClientState.PAUSE, ClientState.RETURN_TO_INN]: return InputContext.MENU
	if _client_state == ClientState.INN: return InputContext.INN
	var combat_value: Variant = _current_frame.get("combat", {})
	if combat_value is Dictionary:
		var combat := combat_value as Dictionary
		if not combat.is_empty(): return InputContext.COMBAT
	return InputContext.EXPLORATION

func _unhandled_input(event: InputEvent) -> void:
	var context := _input_context()
	if context == InputContext.DEATH:
		if event.is_action_pressed("ui_accept"):
			_client_state = ClientState.TITLE; _feedback = ""; _refresh_renderer()
		return
	if context == InputContext.MENU:
		_handle_menu_input(event); return
	if context == InputContext.INN:
		if event.is_action_pressed("enter_dungeon"): _send_intent({"type": "enter_dungeon"})
		elif event.is_action_pressed("toggle_pause"): _open_pause()
		elif _is_panel_action(event): _open_panel(_panel_action(event))
		return
	if context == InputContext.COMBAT:
		if event.is_action_pressed("combat_attack"): _send_combat_action("Attack")
		elif event.is_action_pressed("combat_defend"): _send_combat_action("Defend")
		elif event.is_action_pressed("combat_flee"): _send_combat_action("Flee")
		elif event.is_action_pressed("combat_spell"): _open_panel("SPELLS")
		elif event.is_action_pressed("interact"): _send_intent({"type": "interact"})
		return
	if context == InputContext.EXPLORATION:
		if _is_panel_action(event): _open_panel(_panel_action(event))
		elif event.is_action_pressed("interact"): _send_intent({"type": "interact"})
		elif event.is_action_pressed("move_north"): _send_intent({"type": "move", "direction": "North"})
		elif event.is_action_pressed("move_south"): _send_intent({"type": "move", "direction": "South"})
		elif event.is_action_pressed("move_east"): _send_intent({"type": "move", "direction": "East"})
		elif event.is_action_pressed("move_west"): _send_intent({"type": "move", "direction": "West"})
		elif event.is_action_pressed("toggle_pause"): _open_pause()

func _handle_menu_input(event: InputEvent) -> void:
	if _client_state == ClientState.TITLE:
		if event.is_action_pressed("ui_up") or event.is_action_pressed("ui_down"): _title_selection = ClientState.LOAD_GAME if _title_selection == ClientState.NEW_GAME else ClientState.NEW_GAME
		elif event.is_action_pressed("new_game"): _title_selection = ClientState.NEW_GAME; _client_state = ClientState.NEW_GAME
		elif event.is_action_pressed("load_game"): _title_selection = ClientState.LOAD_GAME; _client_state = ClientState.LOAD_GAME
		elif event.is_action_pressed("ui_accept"): _client_state = _title_selection
		elif event.is_action_pressed("ui_cancel"): _quit_client()
		_refresh_renderer(); return
	if _client_state in [ClientState.NEW_GAME, ClientState.LOAD_GAME]:
		if event.is_action_pressed("ui_cancel"): _client_state = ClientState.TITLE
		elif event.is_action_pressed("ui_accept"): _client_state = ClientState.CHARACTER_CREATION if _client_state == ClientState.NEW_GAME else ClientState.INN
		_refresh_renderer(); return
	if _client_state == ClientState.CHARACTER_CREATION:
		if event.is_action_pressed("ui_cancel"): _client_state = ClientState.NEW_GAME
		elif event.is_action_pressed("ui_accept"): _client_state = ClientState.INN
		_refresh_renderer(); return
	if _client_state == ClientState.PAUSE:
		if event.is_action_pressed("ui_cancel") or event.is_action_pressed("toggle_pause"):
			_client_state = _resume_state; _send_intent({"type": "time_mode", "mode": "Normal"})
		_refresh_renderer(); return
	if _client_state == ClientState.RETURN_TO_INN:
		if event.is_action_pressed("ui_accept"): _client_state = ClientState.INN
		_refresh_renderer(); return
	if _panel != "":
		var entries := _panel_entries()
		if event.is_action_pressed("ui_cancel"): _panel = ""
		elif event.is_action_pressed("ui_up"): _panel_selection = maxi(_panel_selection - 1, 0)
		elif event.is_action_pressed("ui_down"): _panel_selection = mini(_panel_selection + 1, maxi(entries.size() - 1, 0))
		elif event.is_action_pressed("ui_accept"): _panel_confirm()
		elif _is_panel_action(event): _open_panel(_panel_action(event))
		_refresh_renderer()

func _panel_entries() -> Array:
	if _panel == "SPELLS": return _current_frame.get("spells", [])
	if _panel == "INVENTORY": return _current_frame.get("inventory", [])
	if _panel == "JOURNAL": return _current_frame.get("journal", [])
	if _panel == "EQUIPMENT": return _current_frame.get("equipment", [])
	return []

func _panel_confirm() -> void:
	var entries := _panel_entries()
	if _panel == "SPELLS":
		if entries.is_empty(): _feedback = "No known spell is available."; return
		var spell_id := str(entries[_panel_selection])
		var combat_value: Variant = _current_frame.get("combat", {})
		if combat_value is Dictionary and not combat_value.is_empty():
			_send_intent({"type": "combat_action", "action": "CastSpell"}); _queued_intents.append({"type": "resolve_combat_action", "spell_id": spell_id})
		else:
			_feedback = "Spell casting is available during an encounter."
		_panel = ""
	elif _panel == "EQUIPMENT":
		var equipment: Array = _current_frame.get("equipment", [])
		if _panel_selection < equipment.size() and bool(equipment[_panel_selection].get("equipped", false)):
			_send_intent({"type": "unequip_item", "slot_id": str(equipment[_panel_selection].get("slot_id", ""))})
		else: _feedback = "Equip requires an authoritative item instance; none is exposed for this inventory entry."
	elif _panel == "INVENTORY":
		_open_panel("EQUIPMENT")

func _open_pause() -> void:
	_resume_state = _client_state; _client_state = ClientState.PAUSE; _send_intent({"type": "time_mode", "mode": "Paused"}); _refresh_renderer()

func _open_panel(panel: String) -> void:
	_panel = panel; _panel_selection = 0; _refresh_renderer()

func _is_panel_action(event: InputEvent) -> bool:
	return event.is_action_pressed("open_map") or event.is_action_pressed("open_journal") or event.is_action_pressed("open_inventory") or event.is_action_pressed("open_equipment") or event.is_action_pressed("open_spells")

func _panel_action(event: InputEvent) -> String:
	if event.is_action_pressed("open_map"): return "MAP"
	if event.is_action_pressed("open_journal"): return "JOURNAL"
	if event.is_action_pressed("open_inventory"): return "INVENTORY"
	if event.is_action_pressed("open_equipment"): return "EQUIPMENT"
	return "SPELLS"

func _process(delta: float) -> void:
	if _panel != "":
		_clock_accumulator = 0.0
		return
	if _client_state not in [ClientState.INN, ClientState.DUNGEON]:
		_clock_accumulator = 0.0
		return
	_clock_accumulator += delta
	if _host_pid > 0 and _clock_accumulator >= 0.1 and _http.get_http_client_status() != HTTPClient.STATUS_REQUESTING:
		var elapsed := _clock_accumulator; _clock_accumulator = 0.0; _send_intent({"type": "advance", "elapsed_seconds": elapsed})

func _register_input_actions() -> void:
	_register_key("move_north", KEY_W); _register_key("move_south", KEY_S); _register_key("move_east", KEY_D); _register_key("move_west", KEY_A); _register_key("enter_dungeon", KEY_E); _register_key("toggle_pause", KEY_ESCAPE); _register_key("open_map", KEY_M); _register_key("open_journal", KEY_J); _register_key("open_inventory", KEY_I); _register_key("open_equipment", KEY_TAB); _register_key("open_spells", KEY_K); _register_key("interact", KEY_F); _register_key("combat_attack", KEY_1); _register_key("combat_defend", KEY_2); _register_key("combat_flee", KEY_3); _register_key("combat_spell", KEY_4); _register_key("new_game", KEY_N); _register_key("load_game", KEY_L); _register_key("ui_up", KEY_UP); _register_key("ui_down", KEY_DOWN); _register_key("ui_accept", KEY_ENTER); _register_key("ui_cancel", KEY_ESCAPE)
	_register_joy("move_north", JOY_AXIS_LEFT_Y, -1.0); _register_joy("move_south", JOY_AXIS_LEFT_Y, 1.0); _register_joy("move_east", JOY_AXIS_LEFT_X, 1.0); _register_joy("move_west", JOY_AXIS_LEFT_X, -1.0); _register_joy_button("ui_accept", JOY_BUTTON_A); _register_joy_button("ui_cancel", JOY_BUTTON_B); _register_joy_button("ui_up", JOY_BUTTON_DPAD_UP); _register_joy_button("ui_down", JOY_BUTTON_DPAD_DOWN); _register_joy_button("enter_dungeon", JOY_BUTTON_A); _register_joy_button("toggle_pause", JOY_BUTTON_START); _register_joy_button("interact", JOY_BUTTON_X); _register_joy_button("combat_attack", JOY_BUTTON_A); _register_joy_button("combat_defend", JOY_BUTTON_B); _register_joy_button("combat_flee", JOY_BUTTON_Y); _register_joy_button("open_map", JOY_BUTTON_DPAD_UP); _register_joy_button("open_journal", JOY_BUTTON_DPAD_RIGHT); _register_joy_button("open_inventory", JOY_BUTTON_DPAD_DOWN); _register_joy_button("open_spells", JOY_BUTTON_DPAD_LEFT); _register_joy_button("open_equipment", JOY_BUTTON_LEFT_SHOULDER)

func _register_key(action: StringName, keycode: Key) -> void:
	if not InputMap.has_action(action): InputMap.add_action(action)
	var key := InputEventKey.new(); key.keycode = keycode; InputMap.action_add_event(action, key)

func _register_joy(action: StringName, axis: JoyAxis, value: float) -> void:
	if not InputMap.has_action(action): InputMap.add_action(action)
	var joy := InputEventJoypadMotion.new(); joy.axis = axis; joy.axis_value = value; InputMap.action_add_event(action, joy)

func _register_joy_button(action: StringName, button: JoyButton) -> void:
	if not InputMap.has_action(action): InputMap.add_action(action)
	var joy := InputEventJoypadButton.new(); joy.button_index = button; InputMap.action_add_event(action, joy)

func _request_frame() -> void:
	if _request_pending: return
	_initial_frame_attempts += 1; _request_pending = true
	var request_result := _http.request("http://127.0.0.1:18120/frame")
	if request_result != OK: _request_pending = false; _schedule_initial_frame_retry("Unable to request the authoritative session frame (%s)." % request_result)

func _send_intent(intent: Dictionary) -> void:
	if _request_pending:
		if intent.get("type", "") != "advance": _queued_intents.append(intent.duplicate(true))
		return
	_request_pending = true
	var request_result := _http.request("http://127.0.0.1:18120/command", ["Content-Type: application/json"], HTTPClient.METHOD_POST, JSON.stringify(intent))
	if request_result != OK: _request_pending = false; _show_error("Unable to submit authoritative intent (%s)." % request_result)

func _send_combat_action(action: String) -> void:
	if action == "CastSpell":
		_open_panel("SPELLS")
		return
	_send_intent({"type": "combat_action", "action": action}); _queued_intents.append({"type": "resolve_combat_action"})

func _on_request_completed(result: int, _response_code: int, _headers: PackedStringArray, body: PackedByteArray) -> void:
	_request_pending = false
	if result != HTTPRequest.RESULT_SUCCESS: _schedule_initial_frame_retry("Unable to reach authoritative session (network result %s)." % result); return
	_initial_frame_attempts = 0
	var parsed: Variant = JSON.parse_string(body.get_string_from_utf8())
	if not parsed is Dictionary: _show_error("Authoritative session returned invalid JSON."); return
	if parsed.has("frame"):
		_current_frame = parsed["frame"].duplicate(true)
		if _panel != "": _panel_selection = mini(_panel_selection, maxi(_panel_entries().size() - 1, 0))
		renderer.render_frame(parsed["frame"]); _feedback = str(parsed.get("error", "")); _update_authoritative_state(parsed["frame"]); _refresh_renderer()
	if not _queued_intents.is_empty(): _send_intent(_queued_intents.pop_front())

func _schedule_initial_frame_retry(error_message: String) -> void:
	if _client_state == ClientState.STARTUP and _initial_frame_attempts < MAX_INITIAL_FRAME_ATTEMPTS: get_tree().create_timer(0.25).timeout.connect(_request_frame, CONNECT_ONE_SHOT)
	else: _show_error(error_message)

func _update_authoritative_state(frame: Dictionary) -> void:
	if _client_state == ClientState.STARTUP: _client_state = ClientState.TITLE; _previous_authoritative_scene = str(frame.get("scene", "inn")); return
	if _client_state in [ClientState.TITLE, ClientState.NEW_GAME, ClientState.LOAD_GAME, ClientState.CHARACTER_CREATION, ClientState.PAUSE, ClientState.DEATH, ClientState.RETURN_TO_INN]: return
	var authoritative_scene := str(frame.get("scene", "inn"))
	if not bool(frame.get("hud", {}).get("alive", true)): _client_state = ClientState.DEATH
	elif _previous_authoritative_scene == "dungeon" and authoritative_scene == "inn": _client_state = ClientState.RETURN_TO_INN
	else: _client_state = ClientState.DUNGEON if authoritative_scene == "dungeon" else ClientState.INN
	_previous_authoritative_scene = authoritative_scene

func _refresh_renderer() -> void:
	renderer.set_client_state(ClientState.keys()[_client_state], _feedback); renderer.set_title_selection(ClientState.keys()[_title_selection]); renderer.set_panel(_panel, _panel_selection)

func _quit_client() -> void: _stop_host(); get_tree().quit()
func _exit_tree() -> void: _stop_host()
func _stop_host() -> void:
	if _host_pid > 0: OS.kill(_host_pid); _host_pid = -1
func _show_error(message: String) -> void: _feedback = message; _refresh_renderer()
