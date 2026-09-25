class_name VictoryDialogueDB
extends RefCounted

## Modular Database for Post-Match Victory Quotes.
## Pairs each line with the winner's face as PoFV's own face id (0 no, 1 n2, 2 hp, 3 an, 4 sw,
## 5 dp, 6 pr, 7 sp, 8 lo), taken from the pl*_match.msg scripts where the line is PoFV's;
## PostMatch converts it to the portrait sheet's column.
## Supports specific matchups (e.g. Reimu vs Marisa) and generic fallbacks.

# Expression Guide (Column 0-8 in portrait sheet):
# Col 0: Angry / irritated
# Col 1: Calm / composed / neutral
# Col 2: Smug / playful smirk
# Col 3: Proud / beaming smile
# Col 4: Sweating / flustered
# Col 5: Shouting / enthusiastic
# Col 6: Sighing / bored
# Col 7: Shocked / surprised
# Col 8: Defeated / dizzy / bruised (used for loser)

const QUOTES: Array[Dictionary] = [
	# Reimu vs Marisa
	{
		"winner_id": "reimu",
		"loser_id": "marisa",
		"winner_expression": 0,
		"text": "Forests don't have any plants as fancy as a flower, do they? And that's why you're weak."
	},
	{
		"winner_id": "reimu",
		"loser_id": "marisa",
		"winner_expression": 1,
		"text": "A flower petal falls more naturally, you know. Unnatural straightness like that isn't going to work."
	},
	{
		"winner_id": "reimu",
		"loser_id": "marisa",
		"winner_expression": 0,
		"text": "Can you see the fairy with the flower? That's the proof that the flower's not natural. Since fairies are nature itself, they're having an uproar with the unnatural flowers."
	},
	# Marisa vs Reimu
	{
		"winner_id": "marisa",
		"loser_id": "reimu",
		"winner_expression": 0,
		"text": "Today, it's your turn. Cooking, and et cetera."
	},
	{
		"winner_id": "marisa",
		"loser_id": "reimu",
		"winner_expression": 0,
		"text": "You worry too much. I don't think all the flowers blooming at once is a bad sign."
	},
	{
		"winner_id": "marisa",
		"loser_id": "reimu",
		"winner_expression": 0,
		"text": "Did you know? The cherry blossoms in Kourindou turned unnaturally white. I think that's a sign of a disaster."
	},
	# Reimu vs Reimu (Mirror Match)
	{
		"winner_id": "reimu",
		"loser_id": "reimu",
		"winner_expression": 4,
		"text": "Who're you?"
	},
	# Marisa vs Marisa (Mirror Match)
	{
		"winner_id": "marisa",
		"loser_id": "marisa",
		"winner_expression": 0,
		"text": "I'm heading back. Playing alone is no fun."
	},
	# Generic Reimu
	{
		"winner_id": "reimu",
		"loser_id": "*",
		"winner_expression": 4,
		"text": "Can I really kill time in a place like this? This is all the flowers' fault."
	},
	{
		"winner_id": "reimu",
		"loser_id": "*",
		"winner_expression": 1,
		"text": "Spring breeze making flowers fall is just about the right level."
	},
	# Generic Marisa
	{
		"winner_id": "marisa",
		"loser_id": "*",
		"winner_expression": 4,
		"text": "Too many flowers! My color perception is going berserk, ze."
	},
	{
		"winner_id": "marisa",
		"loser_id": "*",
		"winner_expression": 4,
		"text": "The fragrance is strong too, my nose is going to malfunction, ze."
	},
	# Youmu vs Reimu
	{
		"winner_id": "youmu",
		"loser_id": "reimu",
		"winner_expression": 6,
		"text": "Since you're so carefree, I wonder if the disaster is worsening?"
	},
	# Youmu vs Marisa
	{
		"winner_id": "youmu",
		"loser_id": "marisa",
		"winner_expression": 0,
		"text": "Please don't just make merry, won't you investigate the disaster a little?"
	},
	# Youmu vs Youmu (Mirror Match)
	{
		"winner_id": "youmu",
		"loser_id": "youmu",
		"winner_expression": 1,
		"text": "I have come across death on the path of a ghost."
	},
	# Generic Youmu
	{
		"winner_id": "youmu",
		"loser_id": "*",
		"winner_expression": 6,
		"text": "Playing around here is a waste of time."
	},
	{
		"winner_id": "youmu",
		"loser_id": "*",
		"winner_expression": 6,
		"text": "Feels like Yuyuko-sama might know something..."
	},
	# Reimu vs Youmu
	{
		"winner_id": "reimu",
		"loser_id": "youmu",
		"winner_expression": 1,
		"text": "Must be nice; otherworldly people can be carefree all the time."
	},
	{
		"winner_id": "reimu",
		"loser_id": "youmu",
		"winner_expression": 2,
		"text": "Why don't we have a flower viewing in the other world, instead of always at the shrine?"
	},
	# Marisa vs Youmu
	{
		"winner_id": "marisa",
		"loser_id": "youmu",
		"winner_expression": 1,
		"text": "Oh yeah. I haven't been to that world lately. Although this world is overwhelmingly gorgeous."
	},
	{
		"winner_id": "marisa",
		"loser_id": "youmu",
		"winner_expression": 1,
		"text": "If the flowering incident can reach the Netherworld, then maybe that one cherry tree might bloom, too."
	},
	# Cirno Matchups
	{
		"winner_id": "cirno",
		"loser_id": "reimu",
		"winner_expression": 1,
		"text": "The shrine maiden can't do anything this time!"
	},
	{
		"winner_id": "cirno",
		"loser_id": "marisa",
		"winner_expression": 1,
		"text": "No matter how much humans try, they can't use the power of fairies!"
	},
	{
		"winner_id": "cirno",
		"loser_id": "marisa",
		"winner_expression": 1,
		"text": "Even with all that layering, it's useless! You can't keep out of the cold."
	},
	{
		"winner_id": "cirno",
		"loser_id": "youmu",
		"winner_expression": 2,
		"text": "You can't scare me with that blade! Why? Because I am the strongest."
	},
	{
		"winner_id": "cirno",
		"loser_id": "cirno",
		"winner_expression": 5,
		"text": "When it comes to me, I'm the strongest."
	},
	{
		"winner_id": "cirno",
		"loser_id": "*",
		"winner_expression": 2,
		"text": "Make fun of me and your tongue will get scalded!"
	},
	{
		"winner_id": "cirno",
		"loser_id": "*",
		"winner_expression": 2,
		"text": "There is nothing I can't freeze!"
	},
	# Opponents vs Cirno
	{
		"winner_id": "reimu",
		"loser_id": "cirno",
		"winner_expression": 4,
		"text": "You're the one fairy we don't need in spring."
	},
	{
		"winner_id": "marisa",
		"loser_id": "cirno",
		"winner_expression": 5,
		"text": "I don't get why you like the cold at all. I don't get your existence either."
	},
	{
		"winner_id": "youmu",
		"loser_id": "cirno",
		"winner_expression": 5,
		"text": "Your chill and the freezing temperature of ghosts. Shall I test which one is colder?"
	},
	# Sakuya vs Others
	{
		"winner_id": "sakuya",
		"loser_id": "reimu",
		"winner_expression": 1,
		"text": "It seems you haven't solved the incident yet. What a pity."
	},
	{
		"winner_id": "sakuya",
		"loser_id": "marisa",
		"winner_expression": 2,
		"text": "Speed alone won't solve this incident. You lack composure."
	},
	{
		"winner_id": "sakuya",
		"loser_id": "cirno",
		"winner_expression": 5,
		"text": "Should you continue to torment frogs, you will find yourself in a regrettable situation with a frog youkai some day."
	},
	{
		"winner_id": "sakuya",
		"loser_id": "youmu",
		"winner_expression": 1,
		"text": "Your swordsmanship is hasty. A half-ghost shouldn't lose her cool so easily."
	},
	{
		"winner_id": "sakuya",
		"loser_id": "*",
		"winner_expression": 1,
		"text": "Time flows on, but for you it seems to have stopped."
	},
	{
		"winner_id": "sakuya",
		"loser_id": "*",
		"winner_expression": 2,
		"text": "Now then, I should return to the mansion before tea time."
	},
	# Opponents vs Sakuya
	{
		"winner_id": "reimu",
		"loser_id": "sakuya",
		"winner_expression": 0,
		"text": "Throwing knives at a shrine maiden is bad luck, you know."
	},
	{
		"winner_id": "marisa",
		"loser_id": "sakuya",
		"winner_expression": 2,
		"text": "Time stop is cheap! Next time, fight me without parlor tricks!"
	},
	{
		"winner_id": "youmu",
		"loser_id": "sakuya",
		"winner_expression": 1,
		"text": "My blade cuts faster than the flow of time itself."
	},
	{
		"winner_id": "cirno",
		"loser_id": "sakuya",
		"winner_expression": 2,
		"text": "I can freeze time too! Well... I can freeze water, which is basically the same thing!"
	},
	# Reisen Matchups (pl04_match.msg)
	{
		"winner_id": "reisen",
		"loser_id": "reimu",
		"winner_expression": 6,
		"text": "Your wavelength alternates between very long and very short, doesn't it."
	},
	{
		"winner_id": "reisen",
		"loser_id": "reimu",
		"winner_expression": 0,
		"text": "There are people like that from time to time, with this kind of wavelength. That voice synchronizes with no one, yet meets resistance from no one either. How curious."
	},
	{
		"winner_id": "reisen",
		"loser_id": "marisa",
		"winner_expression": 0,
		"text": "Is your wavelength relatively normal? You seem quite stable despite how you look."
	},
	{
		"winner_id": "reisen",
		"loser_id": "sakuya",
		"winner_expression": 1,
		"text": "Your wavelength is a bit long. Could it be that you're gradually becoming more carefree?"
	},
	{
		"winner_id": "reisen",
		"loser_id": "youmu",
		"winner_expression": 5,
		"text": "Your wavelength is on the longer side. Well, looking at you, that seems obvious."
	},
	{
		"winner_id": "reisen",
		"loser_id": "reisen",
		"winner_expression": 7,
		"text": "Oh? My eyes might be a bit bloodshot today..."
	},
	{
		"winner_id": "reisen",
		"loser_id": "cirno",
		"winner_expression": 4,
		"text": "Your wavelength is short. Is that because you have such a fierce temperament?"
	},
	{
		"winner_id": "reisen",
		"loser_id": "*",
		"winner_expression": 4,
		"text": "The reason why people get along or don't is because every living thing possesses its own wavelength."
	},
	{
		"winner_id": "reisen",
		"loser_id": "*",
		"winner_expression": 0,
		"text": "Looking at someone's wavelength tells you roughly their personality. Those with short ones have violent temperaments, while those with long ones are easygoing."
	},
	# Beating Yuuka (each winner's plXX_match.msg, opponent 90)
	{
		"winner_id": "reimu",
		"loser_id": "yuuka",
		"winner_expression": 1,
		"text": "Another troublesome one has turned up, huh."
	},
	{
		"winner_id": "marisa",
		"loser_id": "yuuka",
		"winner_expression": 1,
		"text": "Isn't it you? Anyone would say you're the suspicious one."
	},
	{
		"winner_id": "sakuya",
		"loser_id": "yuuka",
		"winner_expression": 1,
		"text": "Could I have some sunflower seeds? A lot of them. That oil is rather handy, you know."
	},
	{
		"winner_id": "youmu",
		"loser_id": "yuuka",
		"winner_expression": 4,
		"text": "Looking at you reminds me of someone. That carefree air, and those red and white clothes."
	},
	{
		"winner_id": "reisen",
		"loser_id": "yuuka",
		"winner_expression": 5,
		"text": "Your wavelength is off-the-charts long. A flower's wavelength is like that too."
	},
	{
		"winner_id": "cirno",
		"loser_id": "yuuka",
		"winner_expression": 1,
		"text": "I'm gonna turn you into an ice flower!"
	},
	# Yuuka Matchups (pl09_match.msg)
	{
		"winner_id": "yuuka",
		"loser_id": "reimu",
		"winner_expression": 1,
		"text": "Oh, long time no see. Still working as a two-bit shrine maiden, I see."
	},
	{
		"winner_id": "yuuka",
		"loser_id": "marisa",
		"winner_expression": 0,
		"text": "Oh, you're still alive? You've grown up quite a bit, haven't you."
	},
	{
		"winner_id": "yuuka",
		"loser_id": "sakuya",
		"winner_expression": 0,
		"text": "How about an evening primrose? No, I was just picturing a flower that would suit you."
	},
	{
		"winner_id": "yuuka",
		"loser_id": "youmu",
		"winner_expression": 1,
		"text": "How about a skunk cabbage? No, I was just picturing a flower that would suit you."
	},
	{
		"winner_id": "yuuka",
		"loser_id": "reisen",
		"winner_expression": 1,
		"text": "How about a Chinese lantern? No, I was just picturing a flower that would suit you."
	},
	{
		"winner_id": "yuuka",
		"loser_id": "cirno",
		"winner_expression": 4,
		"text": "How about a saxifrage, the one that grows beneath the snow? No, I was just picturing a flower that would suit you."
	},
	{
		"winner_id": "yuuka",
		"loser_id": "yuuka",
		"winner_expression": 1,
		"text": "Even sunflowers get tired, don't they."
	},
	{
		"winner_id": "yuuka",
		"loser_id": "*",
		"winner_expression": 0,
		"text": "A flower blooms by gathering the faint colours held in the soil, and when its petals fall, it returns to the soil. Living things are flowers too. They bloom beautifully by gathering the colours around them, and once they have bloomed, they give that colour back."
	},
	{
		"winner_id": "yuuka",
		"loser_id": "*",
		"winner_expression": 0,
		"text": "Few people can make their own flower bloom. Why? Because there is no end to the arrogant idea of blooming all by oneself. Blooming is the soil's doing. So once you have bloomed, don't forget to give back to the soil."
	},
	# Clownpiece: not in PoFV, so no match script; her one line is the user's.
	{
		"winner_id": "clownpiece",
		"loser_id": "*",
		"winner_expression": 2,
		"text": "It's Lunatic Time!"
	}
]

## Retrieves a random victory dialogue preset matching the winner and loser IDs.
static func get_random_quote(winner_id: String, loser_id: String) -> Dictionary:
	var w_norm := winner_id.to_lower().strip_edges()
	var l_norm := loser_id.to_lower().strip_edges()
	if "reimu" in w_norm: w_norm = "reimu"
	elif "marisa" in w_norm: w_norm = "marisa"
	elif "sakuya" in w_norm: w_norm = "sakuya"
	elif "youmu" in w_norm: w_norm = "youmu"
	elif "cirno" in w_norm: w_norm = "cirno"
	elif "reisen" in w_norm or "udonge" in w_norm: w_norm = "reisen"
	elif "yuuka" in w_norm: w_norm = "yuuka"
	elif "clownpiece" in w_norm: w_norm = "clownpiece"
	if "reimu" in l_norm: l_norm = "reimu"
	elif "marisa" in l_norm: l_norm = "marisa"
	elif "sakuya" in l_norm: l_norm = "sakuya"
	elif "youmu" in l_norm: l_norm = "youmu"
	elif "cirno" in l_norm: l_norm = "cirno"
	elif "reisen" in l_norm or "udonge" in l_norm: l_norm = "reisen"
	elif "yuuka" in l_norm: l_norm = "yuuka"
	elif "clownpiece" in l_norm: l_norm = "clownpiece"
	
	var exact_matches: Array[Dictionary] = []
	var generic_matches: Array[Dictionary] = []
	
	for entry in QUOTES:
		var w_match: bool = (entry.winner_id == w_norm or entry.winner_id == "*")
		var l_match: bool = (entry.loser_id == l_norm or entry.loser_id == "*")
		if w_match and l_match:
			if entry.loser_id != "*":
				exact_matches.append(entry)
			else:
				generic_matches.append(entry)
	
	if not exact_matches.is_empty():
		return exact_matches[randi() % exact_matches.size()]
	elif not generic_matches.is_empty():
		return generic_matches[randi() % generic_matches.size()]
	
	# Ultimate fallback if no match found
	return {
		"winner_id": w_norm,
		"loser_id": l_norm,
		"winner_expression": 1,
		"text": "Victory belongs to %s!" % w_norm.capitalize()
	}
