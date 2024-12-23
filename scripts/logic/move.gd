extends RefCounted
class_name Move

var robot: RREnums.Robot
var old_position: int
var new_position: int


func _init(_robot: RREnums.Robot, _old_position: int, _new_position: int) -> void:
	self.robot = _robot
	self.old_position = _old_position
	self.new_position = _new_position
