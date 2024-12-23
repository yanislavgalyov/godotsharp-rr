extends Sprite2D
class_name GoalSprite2D

const WHITE = Color(1, 1, 1, 0.7)
const RED = Color(1, 0, 0)
const GREEN = Color(0, 1, 0)
const BLUE = Color(0, 0, 1)
const YELLOW = Color(1, 1, 0)

static var vortex_goal_scene = preload("res://scenes/goals/vortex_goal.tscn")
static var circle_goal_scene = preload("res://scenes/goals/circle_goal.tscn")
static var triangle_goal_scene = preload("res://scenes/goals/triangle_goal.tscn")
static var square_goal_scene = preload("res://scenes/goals/square_goal.tscn")
static var hexagon_goal_scene = preload("res://scenes/goals/hexagon_goal.tscn")

var shader: Shader = preload("res://shaders/goal_background.gdshader")


static func instantiate_scene(shape: RREnums.Shape) -> GoalSprite2D:
	var node: GoalSprite2D
	match shape:
		RREnums.Shape.VORTEX:
			node = vortex_goal_scene.instantiate() as GoalSprite2D
		RREnums.Shape.CIRCLE:
			node = circle_goal_scene.instantiate() as GoalSprite2D
		RREnums.Shape.TRIANGLE:
			node = triangle_goal_scene.instantiate() as GoalSprite2D
		RREnums.Shape.SQUARE:
			node = square_goal_scene.instantiate() as GoalSprite2D
		RREnums.Shape.HEXAGON:
			node = hexagon_goal_scene.instantiate() as GoalSprite2D
		_:
			node = null

	return node


func setup(_position: Vector2, _robot: RREnums.Robot, _is_target: bool) -> void:
	position = _position
	modulate = get_color(_robot)
	modulate.a = 1.0 if _is_target else 0.3

	if _is_target:
		var shader_material = ShaderMaterial.new()
		shader_material.shader = shader
		shader_material.set_shader_parameter("background_color", WHITE)
		material = shader_material


func get_color(robot: RREnums.Robot) -> Color:
	match robot:
		RREnums.Robot.RED:
			return RED
		RREnums.Robot.GREEN:
			return GREEN
		RREnums.Robot.BLUE:
			return BLUE
		RREnums.Robot.YELLOW:
			return YELLOW

	return WHITE
