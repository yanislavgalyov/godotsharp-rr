extends Node

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	configure_silent_wolf()

func configure_silent_wolf() -> void:
	var sw_secret_config: ConfigFile = ConfigFile.new()
	var err: Error = sw_secret_config.load("res://secrets/silent_wolf.cfg")

	if (err != OK):
		return

	var section: String = sw_secret_config.get_sections()[0]
	var api_key: String = sw_secret_config.get_value(section, "api_key")
	var game_id: String = sw_secret_config.get_value(section, "game_id")

	SilentWolf.configure({"api_key": api_key, "game_id": game_id, "log_level": 0})
	SilentWolf.configure_scores({"open_scene_on_close": "res://scenes/main_menu.tscn"})
	SilentWolf.auth_config.redirect_to_scene = "res://scenes/main_menu.tscn"

	SilentWolf.Auth.auto_login_player()
