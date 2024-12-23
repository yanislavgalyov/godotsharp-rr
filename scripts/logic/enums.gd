class_name RREnums

enum Robot {
	RED = 0,
	GREEN = 1 << 0,  #1
	BLUE = 1 << 1,  #2
	YELLOW = 1 << 2,  #4
	SILVER = 1 << 3,  #8 - not used
}

enum Direction {
	NORTH = 0,
	EAST = 1,
	SOUTH = 2,
	WEST = 3,
}

enum Shape {
	VORTEX = -1,
	CIRCLE = 0,
	TRIANGLE = 1,
	SQUARE = 3,
	HEXAGON = 4,
}
