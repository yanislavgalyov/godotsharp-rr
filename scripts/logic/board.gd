extends RefCounted
class_name Board

var width: int
var height: int
var size: int
var direction_increment = []
var quadrants = []
var walls = []
var robots = {}
var goals = []
var random_goals = []
var goal = null
var is_freestyle = true

var debug_quadrant_indices: Array = []
var debug_robot_positions: Array = []

static var ANYROBOT = RREnums.Robot.RED | RREnums.Robot.GREEN | RREnums.Robot.BLUE | RREnums.Robot.YELLOW


func _init(_width, _height):
	self.width = _width
	self.height = _height
	self.size = self.width * self.height
	direction_increment.resize(4)
	direction_increment[RREnums.Direction.NORTH] = -width
	direction_increment[RREnums.Direction.EAST] = 1
	direction_increment[RREnums.Direction.SOUTH] = width
	direction_increment[RREnums.Direction.WEST] = -1

	quadrants.resize(4)
	for i in range(direction_increment.size()):
		walls.append([])
		for j in range(self.width * self.height):
			walls[i].append(false)
	robots = {}
	goals = []
	random_goals = []
	goal = null


func is_solved() -> bool:
	if goal == null:
		return false

	if goal.robot == ANYROBOT:
		return robots.values().has(goal.position)

	return goal.position == robots[goal.robot]


func move_robot(robot: RREnums.Robot, direction: RREnums.Direction):
	var current_position = robots[robot]
	var move_to_position = current_position
	var temp_position = current_position
	var counter = 0

	while true:
		if walls[direction][temp_position]:
			break

		temp_position += direction_increment[direction]
		if robots.values().has(temp_position):
			break

		move_to_position = temp_position

		counter += 1
		if (
			(counter > Utils.WIDTH and (direction == RREnums.Direction.EAST or direction == RREnums.Direction.WEST))
			or (counter > Utils.HEIGHT and (direction == RREnums.Direction.NORTH or direction == RREnums.Direction.SOUTH))
		):
			return null

	if move_to_position != current_position:
		robots[robot] = move_to_position
		return Move.new(robot, current_position, move_to_position)

	return null


func move_robot_to_position(robot: RREnums.Robot, position: int):
	if not is_robot_pos(position):
		robots[robot] = position


func get_position_as_xy(position: int) -> Vector2:
	var x = position % width
	@warning_ignore("integer_division")
	var y = position / width
	return Vector2(x, y)


func get_robot_position(robot: RREnums.Robot):
	return robots[robot]


static func create_board_random():
	var index_list = range(4)
	index_list.shuffle()

	return create_board_quadrants(index_list[0] + randi() % 4 * 4, index_list[1] + randi() % 4 * 4, index_list[2] + randi() % 4 * 4, index_list[3] + randi() % 4 * 4)


static func create_board_quadrants(quadrant_nw, quadrant_ne, quadrant_se, quadrant_sw):
	var b = Board.new(Utils.WIDTH, Utils.HEIGHT)
	if OS.is_debug_build():
		b.debug_quadrant_indices.append_array([quadrant_nw, quadrant_ne, quadrant_se, quadrant_sw])
	b.add_quadrant(quadrant_nw, 0)
	b.add_quadrant(quadrant_ne, 1)
	b.add_quadrant(quadrant_se, 2)
	b.add_quadrant(quadrant_sw, 3)
	b.add_outer_walls()
	b.set_robots()
	b.set_goal_random()

	return b


# REGION: ROBOTS


func set_robots():
	robots = {}
	if is_freestyle:
		set_robots_random()
	else:
		set_robot(RREnums.Robot.RED, 14 + 2 * width)
		set_robot(RREnums.Robot.GREEN, 1 + 2 * width)
		set_robot(RREnums.Robot.BLUE, 13 + 11 * width)
		set_robot(RREnums.Robot.YELLOW, 15 + 0 * width)
		set_robot(RREnums.Robot.SILVER, 15 + 7 * width)


func set_robot_positions(robot_positions: Array):
	if robot_positions and robot_positions.size() == 4:
		robots[RREnums.Robot.RED] = robot_positions[0]
		robots[RREnums.Robot.GREEN] = robot_positions[1]
		robots[RREnums.Robot.BLUE] = robot_positions[2]
		robots[RREnums.Robot.YELLOW] = robot_positions[3]


func set_robots_random():
	var rgby = [RREnums.Robot.RED, RREnums.Robot.GREEN, RREnums.Robot.BLUE, RREnums.Robot.YELLOW]
	while true:
		for robot in rgby:
			var position: int
			while true:
				position = randi() % self.size
				if set_robot(robot, position):
					break
		if not is_solution_01():
			break

	if OS.is_debug_build():
		debug_robot_positions.append_array(robots.values())


func set_robot(robot: RREnums.Robot, position: int):
	if position < 0 or position >= self.size or is_obstacle(position) or robots.values().has(position):
		return false
	robots[robot] = position
	return true


func is_robot_pos(position: int):
	return robots.values().has(position)


func is_solution_01() -> bool:
	if goal == null:
		return false

	for robot in robots.keys():
		if goal.robot != robot and goal.robot != ANYROBOT:
			continue

		var old_robot_pos = robots[robot]
		if goal.position == old_robot_pos:
			return true

		var dir: int = -1
		for dirIncr in direction_increment:
			dir += 1
			var new_robot_pos = old_robot_pos
			var prev_robot_pos = old_robot_pos
			while true:
				if walls[dir][new_robot_pos]:
					if goal.position == new_robot_pos:
						return true
					break

				if is_robot_pos(new_robot_pos):
					if goal.position == prev_robot_pos:
						return true

				prev_robot_pos = new_robot_pos
				new_robot_pos += dirIncr
	return false


# REGION: QUADRANTS


# TODO: bug with walls
func add_quadrant(q_num: int, q_pos: int) -> Board:
	quadrants[q_pos] = q_num
	var quadrant = QUADRANTS[q_num]
	# q_pos (quadrant target position): 0==NW, 1==NE, 2==SE, 3==SW
	var qy: int
	var qx: int

	# add walls
	for y in range(quadrant.height / 2):
		qy = y
		for x in range(quadrant.width / 2):
			qx = x
			for dir in range(4):
				walls[(dir + q_pos) & 3][transform_quadrant_position(qx, qy, q_pos)] = (
					walls[(dir + q_pos) & 3][transform_quadrant_position(qx, qy, q_pos)] or quadrant.walls[dir][qx + qy * quadrant.width]
				)

	# for quadrant right border walls
	qx = 8
	for y in range(quadrant.height / 2):
		walls[(RREnums.Direction.WEST + q_pos) & 3][transform_quadrant_position(qx, qy, q_pos)] = (
			walls[(RREnums.Direction.WEST + q_pos) & 3][transform_quadrant_position(qx, qy, q_pos)] or quadrant.walls[RREnums.Direction.EAST][qx - 1 + qy * quadrant.width]
		)

	# for quadrant bottom border walls
	qy = 8
	for x in range(quadrant.width / 2):
		qx = x
		walls[(RREnums.Direction.NORTH + q_pos) & 3][transform_quadrant_position(qx, qy, q_pos)] = (
			walls[(RREnums.Direction.NORTH + q_pos) & 3][transform_quadrant_position(qx, qy, q_pos)] or quadrant.walls[RREnums.Direction.SOUTH][qx + (qy - 1) * quadrant.width]
		)

	# add goals
	for g in quadrant.goals:
		add_goal(transform_quadrant_x(g.x, g.y, q_pos), transform_quadrant_y(g.x, g.y, q_pos), g.robot, g.shape)

	return self


func transform_quadrant_position(qx: int, qy: int, q_pos: int) -> int:
	return transform_quadrant_x(qx, qy, q_pos) + transform_quadrant_y(qx, qy, q_pos) * width


func transform_quadrant_x(qx: int, qy: int, q_pos: int) -> int:
	# q_pos (quadrant target position): 0==NW, 1==NE, 2==SE, 3==SW
	match q_pos:
		1:
			return self.width - 1 - qy
		2:
			return self.width - 1 - qx
		3:
			return qy
		_:
			return qx


func transform_quadrant_y(qx: int, qy: int, q_pos: int) -> int:
	# q_pos (quadrant target position): 0==NW, 1==NE, 2==SE, 3==SW
	match q_pos:
		1:
			return qx
		2:
			return self.height - 1 - qy
		3:
			return self.height - 1 - qx
		_:
			return qy


# REGION: WALLS


func add_outer_walls() -> Board:
	return set_outer_walls()


func set_outer_walls() -> Board:
	for x in range(width):
		set_wall(x, 0, RREnums.Direction.NORTH)
		set_wall(x, height - 1, RREnums.Direction.SOUTH)

	for y in range(height):
		set_wall(0, y, RREnums.Direction.WEST)
		set_wall(width - 1, y, RREnums.Direction.EAST)

	return self


func add_wall(x: int, y: int, directions: String) -> Board:
	set_walls(x, y, directions)
	return self


func set_walls(x: int, y: int, directions: String):
	if directions.find("N") >= 0:
		set_wall(x, y, RREnums.Direction.NORTH)
		set_wall(x, y - 1, RREnums.Direction.SOUTH)

	if directions.find("E") >= 0:
		set_wall(x, y, RREnums.Direction.EAST)
		set_wall(x + 1, y, RREnums.Direction.WEST)

	if directions.find("S") >= 0:
		set_wall(x, y, RREnums.Direction.SOUTH)
		set_wall(x, y + 1, RREnums.Direction.NORTH)

	if directions.find("W") >= 0:
		set_wall(x, y, RREnums.Direction.WEST)
		set_wall(x - 1, y, RREnums.Direction.EAST)


func set_wall(x: int, y: int, direction: RREnums.Direction):
	if x >= 0 and x < width and y >= 0 and y < height:
		walls[direction][x + y * width] = true


func is_wall(position, direction):
	return walls[direction][position]


func is_obstacle(position):
	return is_wall(position, 0) and is_wall(position, 1) and is_wall(position, 2) and is_wall(position, 3)


# REGION: GOALS


func set_goal_random():
	if goals.is_empty():
		goal = null
		return

	if random_goals.is_empty():
		random_goals = goals.duplicate()
		random_goals.shuffle()

	goal = random_goals[0]
	random_goals.remove_at(0)

	if is_solution_01() and not random_goals.is_empty():
		var last_resort_goal = goal
		set_goal_random()
		random_goals.append(last_resort_goal)


func add_goal(x: int, y: int, robot: RREnums.Robot, shape: RREnums.Shape) -> Board:
	var g = Goal.new(x, y, robot, shape)
	goals.append(g)
	if goal == null:
		goal = g

	return self


# REGION

static var QUADRANTS = [
	(
		Board
		. new(Utils.WIDTH, Utils.HEIGHT)  # 1A
		. add_wall(1, 0, "E")
		. add_wall(4, 1, "NW")
		. add_goal(4, 1, RREnums.Robot.RED, RREnums.Shape.CIRCLE)  # R
		. add_wall(1, 2, "NE")
		. add_goal(1, 2, RREnums.Robot.GREEN, RREnums.Shape.TRIANGLE)  # G
		. add_wall(6, 3, "SE")
		. add_goal(6, 3, RREnums.Robot.YELLOW, RREnums.Shape.HEXAGON)  # Y
		. add_wall(0, 5, "S")
		. add_wall(3, 6, "SW")
		. add_goal(3, 6, RREnums.Robot.BLUE, RREnums.Shape.SQUARE)  # B
		. add_wall(7, 7, "NESW")
	),
	(
		Board
		. new(Utils.WIDTH, Utils.HEIGHT)  # 2A
		. add_wall(3, 0, "E")
		. add_wall(5, 1, "SE")
		. add_goal(5, 1, RREnums.Robot.GREEN, RREnums.Shape.HEXAGON)  # G
		. add_wall(1, 2, "SW")
		. add_goal(1, 2, RREnums.Robot.RED, RREnums.Shape.SQUARE)  # R
		. add_wall(0, 3, "S")
		. add_wall(6, 4, "NW")
		. add_goal(6, 4, RREnums.Robot.YELLOW, RREnums.Shape.CIRCLE)  # Y
		. add_wall(2, 6, "NE")
		. add_goal(2, 6, RREnums.Robot.BLUE, RREnums.Shape.TRIANGLE)  # B
		. add_wall(7, 7, "NESW")
	),
	(
		Board
		. new(Utils.WIDTH, Utils.HEIGHT)  # 3A
		. add_wall(3, 0, "E")
		. add_wall(5, 2, "SE")
		. add_goal(5, 2, RREnums.Robot.BLUE, RREnums.Shape.HEXAGON)  # B
		. add_wall(0, 4, "S")
		. add_wall(2, 4, "NE")
		. add_goal(2, 4, RREnums.Robot.GREEN, RREnums.Shape.CIRCLE)  # G
		. add_wall(7, 5, "SW")
		. add_goal(7, 5, RREnums.Robot.RED, RREnums.Shape.TRIANGLE)  # R
		. add_wall(1, 6, "NW")
		. add_goal(1, 6, RREnums.Robot.YELLOW, RREnums.Shape.SQUARE)  # Y
		. add_wall(7, 7, "NESW")
	),
	(
		Board
		. new(Utils.WIDTH, Utils.HEIGHT)  # 4A
		. add_wall(3, 0, "E")
		. add_wall(6, 1, "SW")
		. add_goal(6, 1, RREnums.Robot.BLUE, RREnums.Shape.CIRCLE)  # B
		. add_wall(1, 3, "NE")
		. add_goal(1, 3, RREnums.Robot.YELLOW, RREnums.Shape.TRIANGLE)  # Y
		. add_wall(5, 4, "NW")
		. add_goal(5, 4, RREnums.Robot.GREEN, RREnums.Shape.SQUARE)  # G
		. add_wall(2, 5, "SE")
		. add_goal(2, 5, RREnums.Robot.RED, RREnums.Shape.HEXAGON)  # R
		. add_wall(7, 5, "SE")
		. add_goal(7, 5, ANYROBOT, RREnums.Shape.VORTEX)  # W*
		. add_wall(0, 6, "S")
		. add_wall(7, 7, "NESW")
	),
	(
		Board
		. new(Utils.WIDTH, Utils.HEIGHT)  # 1B
		. add_wall(4, 0, "E")
		. add_wall(6, 1, "SE")
		. add_goal(6, 1, RREnums.Robot.YELLOW, RREnums.Shape.HEXAGON)  # Y
		. add_wall(1, 2, "NW")
		. add_goal(1, 2, RREnums.Robot.GREEN, RREnums.Shape.TRIANGLE)  # G
		. add_wall(0, 5, "S")
		. add_wall(6, 5, "NE")
		. add_goal(6, 5, RREnums.Robot.BLUE, RREnums.Shape.SQUARE)  # B
		. add_wall(3, 6, "SW")
		. add_goal(3, 6, RREnums.Robot.RED, RREnums.Shape.CIRCLE)  # R
		. add_wall(7, 7, "NESW")
	),
	(
		Board
		. new(Utils.WIDTH, Utils.HEIGHT)  # 2B
		. add_wall(4, 0, "E")
		. add_wall(2, 1, "NW")
		. add_goal(2, 1, RREnums.Robot.YELLOW, RREnums.Shape.CIRCLE)  # Y
		. add_wall(6, 3, "SW")
		. add_goal(6, 3, RREnums.Robot.BLUE, RREnums.Shape.TRIANGLE)  # B
		. add_wall(0, 4, "S")
		. add_wall(4, 5, "NE")
		. add_goal(4, 5, RREnums.Robot.RED, RREnums.Shape.SQUARE)  # R
		. add_wall(1, 6, "SE")
		. add_goal(1, 6, RREnums.Robot.GREEN, RREnums.Shape.HEXAGON)  # G
		. add_wall(7, 7, "NESW")
	),
	(
		Board
		. new(Utils.WIDTH, Utils.HEIGHT)  # 3B
		. add_wall(3, 0, "E")
		. add_wall(1, 1, "SW")
		. add_goal(1, 1, RREnums.Robot.RED, RREnums.Shape.TRIANGLE)  # R
		. add_wall(6, 2, "NE")
		. add_goal(6, 2, RREnums.Robot.GREEN, RREnums.Shape.CIRCLE)  # G
		. add_wall(2, 4, "SE")
		. add_goal(2, 4, RREnums.Robot.BLUE, RREnums.Shape.HEXAGON)  # B
		. add_wall(0, 5, "S")
		. add_wall(7, 5, "NW")
		. add_goal(7, 5, RREnums.Robot.YELLOW, RREnums.Shape.SQUARE)  # Y
		. add_wall(7, 7, "NESW")
	),
	(
		Board
		. new(Utils.WIDTH, Utils.HEIGHT)  # 4B
		. add_wall(4, 0, "E")
		. add_wall(2, 1, "SE")
		. add_goal(2, 1, RREnums.Robot.RED, RREnums.Shape.HEXAGON)  # R
		. add_wall(1, 3, "SW")
		. add_goal(1, 3, RREnums.Robot.GREEN, RREnums.Shape.SQUARE)  # G
		. add_wall(0, 4, "S")
		. add_wall(6, 4, "NW")
		. add_goal(6, 4, RREnums.Robot.YELLOW, RREnums.Shape.TRIANGLE)  # Y
		. add_wall(5, 6, "NE")
		. add_goal(5, 6, RREnums.Robot.BLUE, RREnums.Shape.CIRCLE)  # B
		. add_wall(3, 7, "SE")
		. add_goal(3, 7, ANYROBOT, RREnums.Shape.VORTEX)  # W*
		. add_wall(7, 7, "NESW")
	),
	(
		Board
		. new(Utils.WIDTH, Utils.HEIGHT)  # 1C
		. add_wall(1, 0, "E")
		. add_wall(3, 1, "NW")
		. add_goal(3, 1, RREnums.Robot.GREEN, RREnums.Shape.TRIANGLE)  # G
		. add_wall(6, 3, "SE")
		. add_goal(6, 3, RREnums.Robot.YELLOW, RREnums.Shape.HEXAGON)  # Y
		. add_wall(1, 4, "SW")
		. add_goal(1, 4, RREnums.Robot.RED, RREnums.Shape.CIRCLE)  # R
		. add_wall(0, 6, "S")
		. add_wall(4, 6, "NE")
		. add_goal(4, 6, RREnums.Robot.BLUE, RREnums.Shape.SQUARE)  # B
		. add_wall(7, 7, "NESW")
	),
	(
		Board
		. new(Utils.WIDTH, Utils.HEIGHT)  # 2C
		. add_wall(5, 0, "E")
		. add_wall(3, 2, "NW")
		. add_goal(3, 2, RREnums.Robot.YELLOW, RREnums.Shape.CIRCLE)  # Y
		. add_wall(0, 3, "S")
		. add_wall(5, 3, "SW")
		. add_goal(5, 3, RREnums.Robot.BLUE, RREnums.Shape.TRIANGLE)  # B
		. add_wall(2, 4, "NE")
		. add_goal(2, 4, RREnums.Robot.RED, RREnums.Shape.SQUARE)  # R
		. add_wall(4, 5, "SE")
		. add_goal(4, 5, RREnums.Robot.GREEN, RREnums.Shape.HEXAGON)  # G
		. add_wall(7, 7, "NESW")
	),
	(
		Board
		. new(Utils.WIDTH, Utils.HEIGHT)  # 3C
		. add_wall(1, 0, "E")
		. add_wall(4, 1, "NE")
		. add_goal(4, 1, RREnums.Robot.GREEN, RREnums.Shape.CIRCLE)  # G
		. add_wall(1, 3, "SW")
		. add_goal(1, 3, RREnums.Robot.RED, RREnums.Shape.TRIANGLE)  # R
		. add_wall(0, 5, "S")
		. add_wall(5, 5, "NW")
		. add_goal(5, 5, RREnums.Robot.YELLOW, RREnums.Shape.SQUARE)  # Y
		. add_wall(3, 6, "SE")
		. add_goal(3, 6, RREnums.Robot.BLUE, RREnums.Shape.HEXAGON)  # B
		. add_wall(7, 7, "NESW")
	),
	(
		Board
		. new(Utils.WIDTH, Utils.HEIGHT)  # 4C
		. add_wall(2, 0, "E")
		. add_wall(5, 1, "SW")
		. add_goal(5, 1, RREnums.Robot.BLUE, RREnums.Shape.CIRCLE)  # B
		. add_wall(7, 2, "SE")
		. add_goal(7, 2, ANYROBOT, RREnums.Shape.VORTEX)  # W*
		. add_wall(0, 3, "S")
		. add_wall(3, 4, "SE")
		. add_goal(3, 4, RREnums.Robot.RED, RREnums.Shape.HEXAGON)  # R
		. add_wall(6, 5, "NW")
		. add_goal(6, 5, RREnums.Robot.GREEN, RREnums.Shape.SQUARE)  # G
		. add_wall(1, 6, "NE")
		. add_goal(1, 6, RREnums.Robot.YELLOW, RREnums.Shape.TRIANGLE)  # Y
		. add_wall(7, 7, "NESW")
	),
	(
		Board
		. new(Utils.WIDTH, Utils.HEIGHT)  # 1D
		. add_wall(5, 0, "E")
		. add_wall(1, 3, "NW")
		. add_goal(1, 3, RREnums.Robot.RED, RREnums.Shape.CIRCLE)  # R
		. add_wall(6, 4, "SE")
		. add_goal(6, 4, RREnums.Robot.YELLOW, RREnums.Shape.HEXAGON)  # Y
		. add_wall(0, 5, "S")
		. add_wall(2, 6, "NE")
		. add_goal(2, 6, RREnums.Robot.GREEN, RREnums.Shape.TRIANGLE)  # G
		. add_wall(3, 6, "SW")
		. add_goal(3, 6, RREnums.Robot.BLUE, RREnums.Shape.SQUARE)  # B
		. add_wall(7, 7, "NESW")
	),
	(
		Board
		. new(Utils.WIDTH, Utils.HEIGHT)  # 2D
		. add_wall(2, 0, "E")
		. add_wall(5, 2, "SE")
		. add_goal(5, 2, RREnums.Robot.GREEN, RREnums.Shape.HEXAGON)  # G
		. add_wall(6, 2, "NW")
		. add_goal(6, 2, RREnums.Robot.YELLOW, RREnums.Shape.CIRCLE)  # Y
		. add_wall(1, 5, "SW")
		. add_goal(1, 5, RREnums.Robot.RED, RREnums.Shape.SQUARE)  # R
		. add_wall(0, 6, "S")
		. add_wall(4, 7, "NE")
		. add_goal(4, 7, RREnums.Robot.BLUE, RREnums.Shape.TRIANGLE)  # B
		. add_wall(7, 7, "NESW")
	),
	(
		Board
		. new(Utils.WIDTH, Utils.HEIGHT)  # 3D
		. add_wall(4, 0, "E")
		. add_wall(0, 2, "S")
		. add_wall(6, 2, "SE")
		. add_goal(6, 2, RREnums.Robot.BLUE, RREnums.Shape.HEXAGON)  # B
		. add_wall(2, 4, "NE")
		. add_goal(2, 4, RREnums.Robot.GREEN, RREnums.Shape.CIRCLE)  # G
		. add_wall(3, 4, "SW")
		. add_goal(3, 4, RREnums.Robot.RED, RREnums.Shape.TRIANGLE)  # R
		. add_wall(5, 6, "NW")
		. add_goal(5, 6, RREnums.Robot.YELLOW, RREnums.Shape.SQUARE)  # Y
		. add_wall(7, 7, "NESW")
	),
	(
		Board
		. new(Utils.WIDTH, Utils.HEIGHT)  # 4D
		. add_wall(4, 0, "E")
		. add_wall(6, 2, "NW")
		. add_goal(6, 2, RREnums.Robot.YELLOW, RREnums.Shape.TRIANGLE)  # Y
		. add_wall(2, 3, "NE")
		. add_goal(2, 3, RREnums.Robot.BLUE, RREnums.Shape.CIRCLE)  # B
		. add_wall(3, 3, "SW")
		. add_goal(3, 3, RREnums.Robot.GREEN, RREnums.Shape.SQUARE)  # G
		. add_wall(1, 5, "SE")
		. add_goal(1, 5, RREnums.Robot.RED, RREnums.Shape.HEXAGON)  # R
		. add_wall(0, 6, "S")
		. add_wall(5, 7, "SE")
		. add_goal(5, 7, ANYROBOT, RREnums.Shape.VORTEX)  # W*
		. add_wall(7, 7, "NESW")
	),
]
