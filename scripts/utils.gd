extends Node

const WIDTH: int = 16
const HEIGHT: int = 16


static func is_single_robot(robot: int) -> bool:
	var count = 0
	while robot > 0:
		count += robot & 1
		robot >>= 1
	return count <= 1
