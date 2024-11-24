extends Control
class_name CountdownTimer

@onready var label: Label = $Label
@onready var timer: Timer = $Timer

@export var countdown_in_seconds: float = 120.0

signal countdown_timer_done


# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	timer.timeout.connect(_on_timer_timeout)
	timer.start(countdown_in_seconds)


func _physics_process(_delta: float) -> void:
	label.text = time_to_string()


func reset() -> void:
	timer.start(countdown_in_seconds)


func _on_timer_timeout():
	countdown_timer_done.emit()


func time_to_string() -> String:
	var time_left = timer.time_left
	var seconds = fmod(time_left, 60)
	var minutes = time_left / 60
	var format_string = "%02d : %02d"
	var actual_string = format_string % [minutes, seconds]
	return actual_string
