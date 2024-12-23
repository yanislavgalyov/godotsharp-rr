extends RefCounted
class_name Goal

var x: int
var y: int
var position: int
var robot: RREnums.Robot
var shape: RREnums.Shape


func _init(_x: int, _y: int, _robot: RREnums.Robot, _shape: RREnums.Shape):
	self.x = _x
	self.y = _y
	self.robot = _robot
	self.shape = _shape
	self.position = x + y * Utils.WIDTH
