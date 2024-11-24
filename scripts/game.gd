extends Node

@onready var current_scene_parent: Node = $CurrentSceneParent

func _ready() -> void:
	SceneCoordinator.append_scene("res://scenes/main_menu.tscn")
	pass
