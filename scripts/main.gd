extends Node2D

signal board_solved

const WHITE = Color(1, 1, 1, 0.7)
const TRANSPARENT = Color(1, 1, 1, 0)

var board: Board
var robot_sprites = {}

var selected_robot: RREnums.Robot

var solved_boards_count = 0
var total_moves_count = 0

var current_board_moves = []
var is_frozen = false

@onready var countdown_timer: CountdownTimer = $CountdownTimer
@onready var goals_container: Node = $Goals
@onready var final_goals_container: Node = $FinalGoals
@onready var robots_container: Node = $Robots
@onready var walls_container: Node = $Walls
@onready var tile_map_layer: TileMapLayer = $TileMapLayer
@onready var total_boards_label: Label = $TotalBoardsLabel
@onready var total_moves_label: Label = $TotalMovesLabel
@onready var times_up_label: Label = $TimesUpLabel


func _ready():
	robot_sprites = {RREnums.Robot.RED: $Robots/Red, RREnums.Robot.GREEN: $Robots/Green, RREnums.Robot.BLUE: $Robots/Blue, RREnums.Robot.YELLOW: $Robots/Yellow}

	countdown_timer.countdown_timer_done.connect(_on_countdown_timer_done)

	setup_once()
	setup_board()

	board_solved.connect(_on_board_solved)


func _physics_process(_delta):
	if Input.is_action_just_pressed("Up"):
		move_robot(RREnums.Direction.NORTH)
	elif Input.is_action_just_pressed("Right"):
		move_robot(RREnums.Direction.EAST)
	elif Input.is_action_just_pressed("Down"):
		move_robot(RREnums.Direction.SOUTH)
	elif Input.is_action_just_pressed("Left"):
		move_robot(RREnums.Direction.WEST)
	elif Input.is_action_just_pressed("Undo"):
		undo_move_robot()
	elif Input.is_action_just_pressed("Reset"):
		reset_board()


func _input(event):
	if event.is_action_pressed("Red"):
		set_selected_robot(RREnums.Robot.RED)
	elif event.is_action_pressed("Green"):
		set_selected_robot(RREnums.Robot.GREEN)
	elif event.is_action_pressed("Blue"):
		set_selected_robot(RREnums.Robot.BLUE)
	elif event.is_action_pressed("Yellow"):
		set_selected_robot(RREnums.Robot.YELLOW)
	elif event.is_action_pressed("Exit"):
		SceneCoordinator.call_deferred("append_scene", "res://scenes/main_menu.tscn")
	elif event.is_action_pressed("StartMode"):
		SceneCoordinator.call_deferred("append_scene", "res://scenes/main.tscn")
	elif event.is_action_pressed("DebugSolve") and OS.is_debug_build():
		emit_signal("board_solved")


func setup_once():
	solved_boards_count = 0
	total_moves_count = 0


func setup_board():
	current_board_moves.clear()

	for key in robot_sprites:
		set_robot_shader_param(key, TRANSPARENT)

	for child in goals_container.get_children():
		goals_container.remove_child(child)
		child.queue_free()

	for child in final_goals_container.get_children():
		final_goals_container.remove_child(child)
		child.queue_free()

	for child in walls_container.get_children():
		walls_container.remove_child(child)
		child.queue_free()

	total_boards_label.text = "Boards: %d" % solved_boards_count
	total_moves_label.text = "Moves: %d" % total_moves_count

	board = Board.create_board_random()
	# HACK: can test specific boards
	#board = Board.create_board_quadrants(13, 6, 0, 15)
	#board.set_robot_positions([115, 170, 97, 173])

	if OS.is_debug_build():
		print("quadrants:", board.debug_quadrant_indices)
		print("robots:", board.debug_robot_positions)

	var current_goal = board.goal
	var goals = board.goals

	if not current_goal:
		printerr("Missing goal")
		return

	var current_goal_shape: GoalSprite2D = GoalSprite2D.instantiate_scene(current_goal.shape)
	current_goal_shape.setup(tile_map_layer.map_to_local(Vector2(8, 7)), current_goal.robot, false)
	final_goals_container.add_child(current_goal_shape)

	var current_goal_robot = RobotSprite2D.instantiate_scene(current_goal.robot)
	if current_goal_robot:
		current_goal_robot.position = tile_map_layer.map_to_local(Vector2(7, 7))
		final_goals_container.add_child(current_goal_robot)

	for goal in goals:
		var goal_node: GoalSprite2D = GoalSprite2D.instantiate_scene(goal.shape)
		if goal_node:
			goal_node.setup(tile_map_layer.map_to_local(Vector2(goal.x, goal.y)), goal.robot, goal == current_goal)
			goals_container.add_child(goal_node)

	for key in robot_sprites:
		set_robot_position(key)

	set_selected_robot(current_goal.robot)

	var walls = board.walls
	for dir in range(walls.size()):
		for pos in range(walls[dir].size()):
			if walls[dir][pos]:
				set_wall(dir, pos)


func set_selected_robot(robot: RREnums.Robot):
	if selected_robot != null:
		set_robot_shader_param(selected_robot, TRANSPARENT)
	if robot != Board.ANYROBOT:
		selected_robot = robot
		set_robot_shader_param(selected_robot, WHITE)


func set_robot_shader_param(robot: RREnums.Robot, color: Color):
	if robot_sprites[robot].material and robot_sprites[robot].material is ShaderMaterial:
		(robot_sprites[robot].material as ShaderMaterial).set_shader_parameter("background_color", color)


func move_robot(direction: RREnums.Direction):
	if is_frozen or selected_robot == null:
		return

	var move = board.move_robot(selected_robot, direction)
	if move != null:
		set_robot_position(selected_robot)
		total_moves_count += 1
		total_moves_label.text = "Moves: %d" % total_moves_count
		current_board_moves.push_back(move)
		if board.is_solved():
			emit_signal("board_solved")


func undo_move_robot():
	if current_board_moves.size() > 0:
		var move = current_board_moves.pop_back()
		set_selected_robot(move.robot)
		board.move_robot_to_position(move.robot, move.old_position)
		set_robot_position(move.robot)
		total_moves_count -= 1
		total_moves_label.text = "Moves: %d" % total_moves_count


func reset_board():
	while current_board_moves.size() > 0:
		undo_move_robot()

	var current_goal = board.goal
	set_selected_robot(current_goal.robot if current_goal else null)


func _on_board_solved():
	solved_boards_count += 1
	setup_board()


func _on_countdown_timer_done():
	is_frozen = true
	times_up_label.show()
	if solved_boards_count > 0:
		GlobalRR.call_deferred("save_score", solved_boards_count)


func set_wall(direction: RREnums.Direction, board_position: int):
	var tile_size = tile_map_layer.tile_set.tile_size
	var half_width = tile_size.x / 2
	var half_height = tile_size.y / 2

	var xy: Vector2 = board.get_position_as_xy(board_position)
	var x = xy.x
	var y = xy.y
	var v2 = tile_map_layer.map_to_local(Vector2(x, y))

	var start: Vector2
	var end: Vector2
	match direction:
		RREnums.Direction.NORTH:
			start = Vector2(v2.x - half_width, v2.y - half_height)
			end = Vector2(v2.x + half_width, v2.y - half_height)
		RREnums.Direction.EAST:
			start = Vector2(v2.x + half_width, v2.y - half_height)
			end = Vector2(v2.x + half_width, v2.y + half_height)
		RREnums.Direction.SOUTH:
			start = Vector2(v2.x + half_width, v2.y + half_height)
			end = Vector2(v2.x - half_width, v2.y + half_height)
		RREnums.Direction.WEST:
			start = Vector2(v2.x - half_width, v2.y + half_height)
			end = Vector2(v2.x - half_width, v2.y - half_height)

	var line = Line2D.new()
	line.name = "Line2D_%d_%d_%d" % [x, y, board_position]
	line.add_point(start)
	line.add_point(end)
	line.width = 6.0
	line.default_color = Color(0, 0, 0)
	walls_container.add_child(line)


func set_robot_position(robot: RREnums.Robot):
	var robot_sprite = robot_sprites[robot]
	var robot_position = board.get_robot_position(robot)
	var robotxy: Vector2 = board.get_position_as_xy(robot_position)
	var robot_x = robotxy.x
	var robot_y = robotxy.y
	robot_sprite.position = tile_map_layer.map_to_local(Vector2(robot_x, robot_y))
