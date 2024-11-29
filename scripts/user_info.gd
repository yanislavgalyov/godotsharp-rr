extends Control

@onready var label: Label = $Label


func _ready() -> void:
	SilentWolf.Auth.sw_login_complete.connect(_on_login_complete)
	SilentWolf.Auth.sw_session_check_complete.connect(_on_session_check_complete)
	SilentWolf.Auth.sw_logout_complete.connect(_on_sw_logout_complete)


func _on_login_complete(sw_result: Dictionary) -> void:
	if sw_result.success:
		label.text = "logged in as %s" % SilentWolf.Auth.logged_in_player
		GlobalRR.user_logged_in.emit()
	else:
		label.text = str(sw_result.error)


func _on_session_check_complete(sw_result: Variant) -> void:
	if sw_result is Dictionary:
		if sw_result.success:
			label.text = "logged in as %s" % SilentWolf.Auth.logged_in_player
			GlobalRR.user_logged_in.emit()


func _on_sw_logout_complete(_isTrue: bool, _error: String) -> void:
	label.text = "anonymous"
