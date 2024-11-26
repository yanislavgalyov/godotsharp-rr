extends Control

@onready var start_button: Button = $MarginContainer/HBoxContainer/VBoxContainer/StartButton
@onready var exit_button: Button = $MarginContainer/HBoxContainer/VBoxContainer/ExitButton
@onready var login_button: Button = $MarginContainer/HBoxContainer/VBoxContainer/LoginButton
@onready var logout_button: Button = $MarginContainer/HBoxContainer/VBoxContainer/LogoutButton
@onready var ranking_button: Button = $MarginContainer/HBoxContainer/VBoxContainer/RankingButton


func _ready() -> void:
	start_button.pressed.connect(on_start_pressed)
	login_button.button_down.connect(on_login_button_down)
	logout_button.button_down.connect(on_logout_button_down)
	exit_button.pressed.connect(on_exit_button_pressed)
	ranking_button.button_down.connect(on_ranking_button_down)
	Events.user_logged_in.connect(on_user_logged_in)

	if SilentWolf.Auth.logged_in_player:
		login_button.hide()
		logout_button.show()


func on_start_pressed() -> void:
	SceneCoordinator.append_scene("res://scenes/main.tscn")


func on_login_button_down() -> void:
	SceneCoordinator.append_scene("res://addons/silent_wolf/Auth/Login.tscn")


func on_logout_button_down() -> void:
	SilentWolf.Auth.logout_player()
	Events.user_logged_out.emit()
	login_button.show()
	logout_button.hide()


func on_exit_button_pressed() -> void:
	get_tree().quit()


func on_user_logged_in() -> void:
	login_button.hide()
	logout_button.show()


func on_ranking_button_down() -> void:
	SceneCoordinator.append_scene("res://addons/silent_wolf/Scores/Leaderboard.tscn")
