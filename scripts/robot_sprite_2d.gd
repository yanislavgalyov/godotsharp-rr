extends Sprite2D
class_name RobotSprite2D

static var red_robot_scene = preload("res://scenes/robots/red_robot.tscn")
static var green_robot_scene = preload("res://scenes/robots/green_robot.tscn")
static var blue_robot_scene = preload("res://scenes/robots/blue_robot.tscn")
static var yellow_robot_scene = preload("res://scenes/robots/yellow_robot.tscn")


static func instantiate_scene(robot: RREnums.Robot) -> RobotSprite2D:
	var node: RobotSprite2D
	match robot:
		RREnums.Robot.RED:
			node = red_robot_scene.instantiate() as RobotSprite2D
			node.name = "goal red robot"
		RREnums.Robot.GREEN:
			node = green_robot_scene.instantiate() as RobotSprite2D
			node.name = "goal green robot"
		RREnums.Robot.BLUE:
			node = blue_robot_scene.instantiate() as RobotSprite2D
			node.name = "goal blue robot"
		RREnums.Robot.YELLOW:
			node = yellow_robot_scene.instantiate() as RobotSprite2D
			node.name = "goal yellow robot"
		_:
			node = null

	return node
