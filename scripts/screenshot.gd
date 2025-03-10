extends Node

func _ready() -> void:
    process_mode = Node.PROCESS_MODE_ALWAYS

# generate a screenshot of the game window only in debug and use input for "TakeScreenshot" action
func _input(event: InputEvent) -> void:
    if OS.is_debug_build() and event.is_action_released("TakeScreenshot"):
        var img: Image = get_viewport().get_texture().get_image()
        var ticks: float = Time.get_unix_time_from_system()
        var file_name = "user://screenshot_" + str(ticks) + ".png"
        img.save_png(file_name)
