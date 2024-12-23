extends CanvasLayer

# inspired by:
# https://github.com/DaviD4Chirino/Awesome-Scene-Manager/blob/main/addons/awesome_scene_manager/autoloads/SceneManager.gd

signal loading_scene(progress: float)
signal scene_loaded

var scene_ancestor: Node

var scene_to_load: String = ""
var loading_resource: bool = false
var progress: Array = []


func _ready():
	hide()
	var ancestors: Array[Node] = get_tree().get_nodes_in_group("ancestor")
	if ancestors.size() > 0:
		scene_ancestor = ancestors[0]


func _process(_delta: float):
	if not loading_resource:
		return
	var thread_status = ResourceLoader.load_threaded_get_status(scene_to_load, progress)

	match thread_status:
		ResourceLoader.THREAD_LOAD_IN_PROGRESS:
			loading_scene.emit(progress[0])

		ResourceLoader.THREAD_LOAD_LOADED:
			scene_loaded.emit()
			loading_resource = false

		ResourceLoader.THREAD_LOAD_INVALID_RESOURCE:
			push_error("Resource %s invalid" % scene_to_load)
			loading_resource = false

		ResourceLoader.THREAD_LOAD_FAILED:
			push_error("Failed to load the resource %s" % scene_to_load)
			loading_resource = false


func set_ancestor(ancestor: Node) -> void:
	scene_ancestor = ancestor


func append_scene(
	## The path to the scene you want to change into
	scene_path: String,
	scene_parent: Node = scene_ancestor,
	free_old: bool = true,
):
	show()

	scene_to_load = scene_path

	# we start to load the scene
	ResourceLoader.load_threaded_request(scene_to_load)
	loading_resource = true

	# we change the scene after it loads
	await scene_loaded

	if free_old and scene_parent:
		for child in scene_parent.get_children():
			scene_parent.remove_child(child)
			child.queue_free()

	# var scene_parent: Node = scene_to_swap.get_parent()
	# We store the scene position to add the new node to that exact position
	# var scene_position: int = scene_to_swap.get_index()
	var new_scene = ResourceLoader.load_threaded_get(scene_to_load).instantiate()

	scene_parent.add_child(new_scene)

	hide()
