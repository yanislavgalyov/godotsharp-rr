extends TextureRect

const SWLogger = preload("res://addons/silent_wolf/utils/SWLogger.gd")


func _ready():
	SilentWolf.Auth.sw_login_complete.connect(_on_login_complete)


func _input(event: InputEvent) -> void:
	if event.is_action_pressed("Exit"):
		SceneCoordinator.call_deferred("append_scene", "res://scenes/main_menu.tscn")


func _on_LoginButton_pressed() -> void:
	var username = $"FormContainer/UsernameContainer/Username".text
	var password = $"FormContainer/PasswordContainer/Password".text
	var remember_me = $"FormContainer/RememberMeCheckBox".is_pressed()
	SWLogger.debug("Login form submitted, remember_me: " + str(remember_me))
	SilentWolf.Auth.login_player(username, password, true)
	show_processing_label()


func _on_login_complete(sw_result: Dictionary) -> void:
	if sw_result.success:
		login_success()
	else:
		login_failure(sw_result.error)


func login_success() -> void:
	SWLogger.info("logged in as: " + str(SilentWolf.Auth.logged_in_player))
	# get_tree().change_scene_to_file(SilentWolf.auth_config.redirect_to_scene)
	SceneCoordinator.append_scene(SilentWolf.auth_config.redirect_to_scene)


func login_failure(error: String) -> void:
	hide_processing_label()
	SWLogger.info("log in failed: " + str(error))
	$"FormContainer/ErrorMessage".text = error
	$"FormContainer/ErrorMessage".show()


func show_processing_label() -> void:
	$"FormContainer/ProcessingLabel".show()
	$"FormContainer/ProcessingLabel".show()


func hide_processing_label() -> void:
	$"FormContainer/ProcessingLabel".hide()


func _on_LinkButton_pressed() -> void:
	# get_tree().change_scene_to_file(SilentWolf.auth_config.reset_password_scene)
	SceneCoordinator.append_scene(SilentWolf.auth_config.reset_password_scene)


func _on_back_button_pressed():
	SceneCoordinator.call_deferred("append_scene", SilentWolf.auth_config.redirect_to_scene)
	# get_tree().change_scene_to_file(SilentWolf.auth_config.redirect_to_scene)


func _on_register_button_pressed() -> void:
	SceneCoordinator.call_deferred("append_scene", SilentWolf.auth_config.register_scene)
	# get_tree().change_scene_to_file(SilentWolf.auth_config.register_scene)
