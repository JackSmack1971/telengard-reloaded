extends Node2D
class_name ModernRenderer

## Visual-only Modern presentation prototype.
## The host supplies a dictionary made from ModernRenderFrame. This node never
## resolves commands or mutates authoritative simulation state.

const TILE_SIZE := 48.0
const MAP_ORIGIN := Vector2(420.0, 150.0)
const PANEL_COLOR := Color("182033")
const PANEL_EDGE := Color("3b4966")
const FLOOR_COLOR := Color("26334b")
const VISITED_COLOR := Color("344968")
const CURRENT_COLOR := Color("4c8fc6")
const FEATURE_COLOR := Color("d9ad62")
const UNKNOWN_COLOR := Color("111827")
const ASSET_REGISTRY_SCRIPT := preload("res://PresentationAssetRegistry.gd")

var _frame: Dictionary = {}
var _elapsed := 0.0
var _asset_registry = ASSET_REGISTRY_SCRIPT.new()
var _client_state := "STARTUP"
var _feedback := ""
var _title_selection := "NEW_GAME"
var _panel := ""


func render_frame(frame: Dictionary) -> void:
	_frame = frame.duplicate(true)
	queue_redraw()

func set_client_state(client_state: String, feedback: String) -> void:
	_client_state = client_state
	_feedback = feedback
	queue_redraw()

func set_title_selection(title_selection: String) -> void:
	_title_selection = title_selection
	queue_redraw()

func set_panel(panel: String) -> void:
	_panel = panel
	queue_redraw()

func current_scene() -> String:
	return str(_frame.get("scene", "inn"))


func _process(delta: float) -> void:
	_elapsed += delta
	if not _frame.is_empty() and _frame.get("environment", {}).get("dynamic_lighting", false):
		queue_redraw()


func _draw() -> void:
	draw_rect(Rect2(Vector2.ZERO, get_viewport_rect().size), Color("0c111d"))
	_draw_header()
	_draw_world()
	_draw_hud()


func _draw_header() -> void:
	var font := ThemeDB.fallback_font
	draw_string(font, Vector2(36, 48), "MODERN TELENGARD", HORIZONTAL_ALIGNMENT_LEFT, -1, 26, Color("e5edf7"))
	draw_string(font, Vector2(38, 73), _client_state, HORIZONTAL_ALIGNMENT_LEFT, -1, 14, Color("8295b5"))
	draw_line(Vector2(36, 92), Vector2(1244, 92), PANEL_EDGE, 1.0)


func _draw_world() -> void:
	draw_rect(Rect2(36, 120, 820, 540), PANEL_COLOR, true)
	draw_rect(Rect2(36, 120, 820, 540), PANEL_EDGE, false, 1.0)

	if _frame.is_empty():
		var font := ThemeDB.fallback_font
		draw_string(font, Vector2(72, 365), "Waiting for a PresentationState projection", HORIZONTAL_ALIGNMENT_LEFT, -1, 20, Color("b4c3d9"))
		draw_string(font, Vector2(72, 394), "Call render_frame() from the presentation host.", HORIZONTAL_ALIGNMENT_LEFT, -1, 14, Color("8295b5"))
		return
	var current_position: Variant = _frame.get("player_position", {})
	var current_floor := int(current_position.get("floor", 1)) if current_position is Dictionary else 1

	for tile in _frame.get("tiles", []):
		var position: Dictionary = tile.get("position", {})
		if int(position.get("floor", -1)) != current_floor:
			continue
		var cell := MAP_ORIGIN + Vector2(float(position.get("x", 0)), float(position.get("y", 0))) * TILE_SIZE
		var knowledge: String = str(tile.get("knowledge", "observed"))
		var color := UNKNOWN_COLOR
		if knowledge == "visited":
			color = VISITED_COLOR
		elif knowledge == "observed":
			color = FLOOR_COLOR
		elif knowledge == "current":
			color = CURRENT_COLOR
		draw_rect(Rect2(cell, Vector2(TILE_SIZE - 2.0, TILE_SIZE - 2.0)), color, true)
		draw_rect(Rect2(cell, Vector2(TILE_SIZE - 2.0, TILE_SIZE - 2.0)), PANEL_EDGE, false, 1.0)
		_draw_connections(cell, str(tile.get("connections", "None")), color)

	for feature in _frame.get("features", []):
		var position: Dictionary = feature.get("position", {})
		if int(position.get("floor", -1)) != current_floor:
			continue
		var cell := MAP_ORIGIN + Vector2(float(position.get("x", 0)), float(position.get("y", 0))) * TILE_SIZE
		var center := cell + Vector2(TILE_SIZE * 0.5 - 1.0, TILE_SIZE * 0.5 - 1.0)
		var diamond := PackedVector2Array([
			center + Vector2(0, -12),
			center + Vector2(12, 0),
			center + Vector2(0, 12),
			center + Vector2(-12, 0)
		])
		var presentation_key := str(feature.get("presentation_key", feature.get("definition_id", "feature.unknown")))
		var resource_id := _asset_registry.resolve(presentation_key)
		var feature_color := _asset_registry.placeholder_color(presentation_key)
		draw_colored_polygon(diamond, feature_color if resource_id.begins_with(ASSET_REGISTRY_SCRIPT.PLACEHOLDER_PREFIX) else FEATURE_COLOR)
		if resource_id.begins_with(ASSET_REGISTRY_SCRIPT.PLACEHOLDER_PREFIX):
			draw_string(ThemeDB.fallback_font, cell + Vector2(2, TILE_SIZE + 14), _short_identity(presentation_key), HORIZONTAL_ALIGNMENT_LEFT, -1, 10, Color("f0c674"))

	var combat_value: Variant = _frame.get("combat", {})
	var combat: Dictionary = combat_value if combat_value is Dictionary else {}
	if not combat.is_empty():
		var monster: Dictionary = combat.get("monster", {})
		var monster_position: Dictionary = monster.get("position", {})
		var monster_cell := MAP_ORIGIN + Vector2(float(monster_position.get("x", 0)), float(monster_position.get("y", 0))) * TILE_SIZE
		var monster_key := str(monster.get("presentation_key", monster.get("definition_id", "monster.unknown")))
		var monster_center := monster_cell + Vector2(TILE_SIZE * 0.5 - 1.0, TILE_SIZE * 0.5 - 1.0)
		draw_circle(monster_center, 14.0, _asset_registry.placeholder_color(monster_key))
		draw_string(ThemeDB.fallback_font, monster_cell + Vector2(2, -6), _short_identity(monster_key), HORIZONTAL_ALIGNMENT_LEFT, -1, 10, Color("f0c674"))

	if not _frame.is_empty():
		var player_position_value: Variant = _frame.get("player_position", {})
		var player_position: Dictionary = player_position_value if player_position_value is Dictionary else {}
		var player_cell := MAP_ORIGIN + Vector2(float(player_position.get("x", 0)), float(player_position.get("y", 0))) * TILE_SIZE
		var pulse := 1.0 + sin(_elapsed * 4.0) * 0.08
		var player_center := player_cell + Vector2(TILE_SIZE * 0.5 - 1.0, TILE_SIZE * 0.5 - 1.0)
		if _frame.get("environment", {}).get("dynamic_lighting", false):
			draw_circle(player_center, 38.0 * pulse, Color(0.22, 0.54, 0.85, 0.12))
		draw_circle(player_center, 10.0 * pulse, Color("dceeff"))


func _draw_hud() -> void:
	draw_rect(Rect2(884, 120, 360, 540), PANEL_COLOR, true)
	draw_rect(Rect2(884, 120, 360, 540), PANEL_EDGE, false, 1.0)
	var font := ThemeDB.fallback_font
	var hud_value: Variant = _frame.get("hud", {})
	var hud: Dictionary = hud_value if hud_value is Dictionary else {}
	draw_string(font, Vector2(916, 164), "ADVENTURER", HORIZONTAL_ALIGNMENT_LEFT, -1, 18, Color("e5edf7"))
	draw_string(font, Vector2(916, 196), "Level %s" % hud.get("level", 1), HORIZONTAL_ALIGNMENT_LEFT, -1, 16, Color("b4c3d9"))
	draw_string(font, Vector2(916, 236), "HP   %s / %s" % [hud.get("hit_points", 0), hud.get("max_hit_points", 0)], HORIZONTAL_ALIGNMENT_LEFT, -1, 15, Color("d36f78"))
	draw_string(font, Vector2(916, 264), "SP   %s / %s" % [hud.get("spell_power", 0), hud.get("max_spell_power", 0)], HORIZONTAL_ALIGNMENT_LEFT, -1, 15, Color("7cb9e8"))
	draw_string(font, Vector2(916, 304), "Carried gold   %s" % hud.get("carried_gold", 0), HORIZONTAL_ALIGNMENT_LEFT, -1, 14, Color("d9ad62"))
	draw_string(font, Vector2(916, 330), "Secured gold   %s" % hud.get("secured_gold", 0), HORIZONTAL_ALIGNMENT_LEFT, -1, 14, Color("d9ad62"))
	var player_position_value: Variant = _frame.get("player_position", {})
	var player_position: Dictionary = player_position_value if player_position_value is Dictionary else {}
	draw_string(font, Vector2(916, 356), "Floor %s" % player_position.get("floor", "-"), HORIZONTAL_ALIGNMENT_LEFT, -1, 14, Color("b4c3d9"))
	draw_string(font, Vector2(916, 378), "Map: unknown / visited / visible", HORIZONTAL_ALIGNMENT_LEFT, -1, 12, Color("8295b5"))

	var combat_value: Variant = _frame.get("combat", {})
	var combat: Dictionary = combat_value if combat_value is Dictionary else {}
	if not combat.is_empty():
		draw_line(Vector2(916, 370), Vector2(1212, 370), PANEL_EDGE, 1.0)
		draw_string(font, Vector2(916, 408), "ENCOUNTER", HORIZONTAL_ALIGNMENT_LEFT, -1, 15, Color("e5edf7"))
		draw_string(font, Vector2(916, 438), str(combat.get("phase", "contact")), HORIZONTAL_ALIGNMENT_LEFT, -1, 14, Color("b4c3d9"))
		draw_string(font, Vector2(916, 466), "Threat   %s" % combat.get("threat_level", "unknown"), HORIZONTAL_ALIGNMENT_LEFT, -1, 14, Color("d9ad62"))
	if _client_state == "INN" or _client_state == "DUNGEON":
		if _client_state == "DUNGEON":
			draw_string(font, Vector2(916, 522), "F  Interact   R/T  Stairs down/up   L  Leave", HORIZONTAL_ALIGNMENT_LEFT, -1, 12, Color("8295b5"))
		else:
			draw_string(font, Vector2(916, 522), "E  Enter dungeon   M  Map   Esc  Pause", HORIZONTAL_ALIGNMENT_LEFT, -1, 12, Color("8295b5"))
		draw_string(font, Vector2(916, 544), "I  Inventory   K  Spells   Esc  Pause", HORIZONTAL_ALIGNMENT_LEFT, -1, 12, Color("8295b5"))
		if not combat.is_empty():
			draw_string(font, Vector2(916, 590), "1 Attack  2 Defend  3 Flee", HORIZONTAL_ALIGNMENT_LEFT, -1, 12, Color("d9ad62"))
			draw_string(font, Vector2(916, 610), "4 Spell   5 Item", HORIZONTAL_ALIGNMENT_LEFT, -1, 12, Color("d9ad62"))
	if _panel != "":
		_draw_panel(font)

	if _client_state != "INN" and _client_state != "DUNGEON":
		_draw_overlay(font)

func _draw_overlay(font: Font) -> void:
	draw_rect(Rect2(180, 190, 760, 330), Color(0.05, 0.07, 0.12, 0.96), true)
	draw_rect(Rect2(180, 190, 760, 330), PANEL_EDGE, false, 2.0)
	draw_string(font, Vector2(230, 260), _overlay_title(), HORIZONTAL_ALIGNMENT_LEFT, -1, 30, Color("e5edf7"))
	draw_string(font, Vector2(230, 312), _overlay_body(), HORIZONTAL_ALIGNMENT_LEFT, -1, 17, Color("b4c3d9"))
	draw_string(font, Vector2(230, 360), _overlay_prompt(), HORIZONTAL_ALIGNMENT_LEFT, -1, 16, Color("d9ad62"))
	if not _feedback.is_empty():
		draw_string(font, Vector2(230, 420), _feedback, HORIZONTAL_ALIGNMENT_LEFT, 660, 14, Color("d36f78"))

func _draw_panel(font: Font) -> void:
	draw_rect(Rect2(160, 150, 800, 430), Color(0.04, 0.06, 0.1, 0.98), true)
	draw_rect(Rect2(160, 150, 800, 430), PANEL_EDGE, false, 2.0)
	draw_string(font, Vector2(205, 205), _panel, HORIZONTAL_ALIGNMENT_LEFT, -1, 26, Color("e5edf7"))
	draw_string(font, Vector2(205, 245), "Presentation view — authoritative state is unchanged", HORIZONTAL_ALIGNMENT_LEFT, -1, 13, Color("8295b5"))
	var values: Array = _frame.get("inventory", []) if _panel == "INVENTORY" else _frame.get("spells", []) if _panel == "SPELLS" else _frame.get("journal", [])
	if _panel == "MAP":
		draw_string(font, Vector2(205, 290), "Map legend: dark unknown · blue observed · slate visited", HORIZONTAL_ALIGNMENT_LEFT, -1, 16, Color("b4c3d9"))
		draw_string(font, Vector2(205, 330), "Close with Esc. The map remains governed by the renderer-safe projection.", HORIZONTAL_ALIGNMENT_LEFT, -1, 14, Color("b4c3d9"))
	else:
		if values.is_empty():
			draw_string(font, Vector2(205, 300), "No entries are currently known.", HORIZONTAL_ALIGNMENT_LEFT, -1, 17, Color("b4c3d9"))
		for index in values.size():
			draw_string(font, Vector2(205, 300 + index * 28), "• %s" % values[index], HORIZONTAL_ALIGNMENT_LEFT, -1, 17, Color("b4c3d9"))

func _overlay_title() -> String:
	return {
		"STARTUP": "Starting Telengard",
		"TITLE": "Telengard Reloaded",
		"NEW_GAME": "New expedition",
		"LOAD_GAME": "Load expedition",
		"CHARACTER_CREATION": "Create adventurer",
		"PAUSE": "Paused",
		"DEATH": "The expedition has ended",
		"RETURN_TO_INN": "Returned safely"
	}.get(_client_state, _client_state)

func _overlay_body() -> String:
	return {
		"STARTUP": "Connecting to the authoritative simulation…",
		"TITLE": "Choose a session to begin.",
		"NEW_GAME": "A new hosted session is ready.",
		"LOAD_GAME": "Load flow is ready for the persistence slice.",
		"CHARACTER_CREATION": "Choose a character mode before entering the inn.",
		"PAUSE": "Simulation time is paused. Presentation focus is captured.",
		"DEATH": "Review the result, then return to the session title.",
		"RETURN_TO_INN": "Your secured progress is shown by the authoritative HUD."
	}.get(_client_state, "")

func _overlay_prompt() -> String:
	if _client_state == "TITLE":
		return "[%s] New game     [%s] Load game     Enter  Select     Esc  Quit" % ["X" if _title_selection == "NEW_GAME" else " ", "X" if _title_selection == "LOAD_GAME" else " "]
	return {
		"STARTUP": "",
		"NEW_GAME": "Enter  Continue     Esc  Back",
		"LOAD_GAME": "Enter  Continue     Esc  Back",
		"CHARACTER_CREATION": "Enter  Confirm     Esc  Back",
		"PAUSE": "Esc  Resume",
		"DEATH": "Enter  Return to title",
		"RETURN_TO_INN": "Enter  Continue at inn"
	}.get(_client_state, "")


func _scene_label() -> String:
	if _frame.is_empty():
		return "renderer prototype"
	var environment: Dictionary = _frame.get("environment", {})
	var scene := "INN" if _frame.get("scene", "dungeon") == "inn" else "DUNGEON"
	return "%s · %s" % [scene, environment.get("theme_id", "unknown")]

func _draw_connections(cell: Vector2, connections: String, color: Color) -> void:
	var center := cell + Vector2(TILE_SIZE * 0.5 - 1.0, TILE_SIZE * 0.5 - 1.0)
	var half := TILE_SIZE * 0.5 - 5.0
	if connections.contains("North"):
		draw_line(center, center + Vector2(0, -half), color.lightened(0.25), 3.0)
	if connections.contains("South"):
		draw_line(center, center + Vector2(0, half), color.lightened(0.25), 3.0)
	if connections.contains("East"):
		draw_line(center, center + Vector2(half, 0), color.lightened(0.25), 3.0)
	if connections.contains("West"):
		draw_line(center, center + Vector2(-half, 0), color.lightened(0.25), 3.0)

func _short_identity(identity: String) -> String:
	var parts := identity.split(".")
	return parts[-1].replace("-", " ").to_upper()
