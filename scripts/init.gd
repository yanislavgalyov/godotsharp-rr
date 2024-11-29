extends Node

const SWLogger = preload("res://addons/silent_wolf/utils/SWLogger.gd")


func _ready() -> void:
	configure_silent_wolf()


func configure_silent_wolf() -> void:
	var sw_secret_config: ConfigFile = ConfigFile.new()
	var err: Error = sw_secret_config.load("res://secrets/silent_wolf.cfg")

	if err != OK:
		SWLogger.error(err)
		return

	var section: String = sw_secret_config.get_sections()[0]
	var api_key: String = sw_secret_config.get_value(section, "api_key")
	var game_id: String = sw_secret_config.get_value(section, "game_id")

	SilentWolf.configure({"api_key": api_key, "game_id": game_id, "log_level": -1})  # -1 does not log even anything, even errors
	SilentWolf.configure_scores({"open_scene_on_close": "res://scenes/main_menu.tscn"})
	SilentWolf.auth_config.redirect_to_scene = "res://scenes/main_menu.tscn"

	SilentWolf.Auth.auto_login_player()


func save_score(score: int) -> void:
	if SilentWolf.Auth.logged_in_player:
		await SilentWolf.Scores.save_score(SilentWolf.Auth.logged_in_player, score).sw_save_score_complete
