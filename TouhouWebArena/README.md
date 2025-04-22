# Touhou Web Arena

Inspired by Touhou 09: Phantasmagoria of Flower View.

This project is a web-based multiplayer game utilizing Unity and Netcode for GameObjects.

## Overview

Touhou Web Arena is a Touhou 09: Phantasmagoria of Flower View-inspired, multiplayer scrolling bullet hell game. This is a game I always wanted to make as I think the concept is very cool for a 1v1 PvP game, especially if we can get it running seamlessly on modern web browsers. There's also the potential to expand the project beyond the PoFV original characters, which is exciting to think about.

Two players share a screen, but are confined to their own quadrants, Player 1 on the left, Player 2 on the right. The players can shoot a stream of bullets forward, and move in all eight directions. Enemies like fairies and spirits filter into each individual playspace (fairies in predetermined spline paths, and spirits from the general area on top of the playspaces), and when killed, a bullet (in the case of fairies) or a spirit (in the case of other spirits) is sent to the other side of the playfield, providing the first and most basic layer of interaction between players.

Each player has 5 HP, represented by five orbs yin-yang orbs on top of the screen. All stage hazards (so far) deal exactly 1 damage, meaning the player can get hit a total of 4 times, with the 5th hit being lethal damage. Matches operate on a point system - first to two points wins, and a round is decided by who can deplete the other's HP first.

Players can shoot and move in all eight directions. Movement is "binary", there is no acceleration to it. You're either moving, or you aren't. Holding down the left Shift key activates Focus Mode. In Focus Mode, your speed drops by half, allowing for more precise movement. Your hitbox also becomes visible.

Holding Shift also brings out your Scope Style. Scope Style is a zone of influence around the character, whose sole purpose is to activate spirit enemies on contact. Activated spirits are much less resilient and easier to kill.

Killing a certain fairy every set number of waves (we have it set to 2 currently) sends over an Extra Attack to the opponent's side. This is a character-specific move that happens automatically after killing said fairies.

The players have access to a spellbar, a gauge that sits at the bottom of the screen. The bar is separated into segments (0 to 1 or 25%, 1 to 2 or 50%, 2 to 3 or 75%, and 3 to 4 or 100%). Over time, and when killing enemies (we don't have the killing enemy functionality for the bar yet), a bar slowly fills up, the passive bar. This bar indicates the maximum amount a player can "charge" the bar.

By holding down the shoot key, or Z by default, a new bar overlays the slow-filling passive bar, the active bar. The active bar indicates the maximum level of charge the bar has. By filling it to these predetermined spots at 25%, 50%, 75% or 100%, different things happen.

At 25% bar, a character specific Charge Attack comes out. This does not consume any segments of the bar, so players can immediately go back to charging more charge attacks after using them.

At 50%, 75% and 100% active charge, the player unleashes, or declares, a Spellcard. This is a unique pattern of bullets that appears on the enemy's screen. The higher level spellcard you use, the more complex the pattern becomes to dodge for the enemy.

Using a spellcard consumes the part of the passive bar that was used. For example, if you have a 100% passive bar and you use a level 2 spellcard, 25% of your passive bar will be gone and you will be left with 75%.

## Getting Started

This project runs on Unity. Our networking solution is Netcode for GameObjects. Simply download the game and run the .exe. The plan is to eventually migrate the project to a WebGL build so players can easily go to a website and play the game without having to download anything, but for now, the game is a standalone executable. The server is run on a local machine for now and cannot be changed. Click connect on the main menu screen to connect to the server, and then you'll be able to enter the queue for games.

## Documentation

Detailed documentation can be found in the `/docs` directory.
*   [Networking Overview](docs/networking_overview.md)
*   [Spellcard System](docs/spellcard_system.md)

## Project Structure

*   `/Scripts`: Contains all C# scripts, organized into subfolders by functionality (e.g., `/Player`, `/Networking`, `/UI`, `/Gameplay/Spellcards`).
*   `/Art`: Contains visual assets like Sprites, Animations, Effects.
*   `/Prefabs`: Contains game object prefabs, organized into subfolders (e.g., `/Player`, `/Enemies`, `/Projectiles`).
*   `/Resources`: Holds loadable assets, primarily ScriptableObjects defining game data like Spellcards.
*   `/Scenes`: Contains all Unity scenes (e.g., MainMenu, GameLevel).
*   `/PhysicsMaterials`: Contains specific physics materials (e.g., ReimuExtraAttackMaterial).
*   `/docs`: Contains detailed documentation files.