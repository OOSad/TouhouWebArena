extends Node

# Global game state variables
var player_nickname: String = "Anonymous Fairy"
var is_in_matchmaking: bool = false

func _ready():
	pass

# Function to set player nickname
func set_nickname(nickname: String):
	if nickname.strip_edges() != "":
		player_nickname = nickname
	else:
		player_nickname = "Anonymous Fairy"

# Function to get current nickname
func get_nickname() -> String:
	return player_nickname