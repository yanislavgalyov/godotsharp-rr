extends Control

@onready var start_button: Button = $MarginContainer/HBoxContainer/VBoxContainer/StartButton
@onready var exit_button: Button = $MarginContainer/HBoxContainer/VBoxContainer/ExitButton

@onready var main_scene = preload("res://scenes/main.tscn")

func _ready() -> void:
	start_button.pressed.connect(on_start_pressed)
	exit_button.button_down.connect(on_exit_button_down)

func on_start_pressed():
	get_tree().change_scene_to_packed(main_scene)

func on_exit_button_down():
	get_tree().quit()
