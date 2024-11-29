## Godot Ricochet Robot

A fan game based on Alex Randolph's puzzle board game [Ricochet Robots](https://en.wikipedia.org/wiki/Ricochet_Robots)

Developed by using [Godot_v4.3-stable_mono_win64](https://github.com/godotengine/godot/releases/download/4.3-stable/Godot_v4.3-stable_mono_win64.zip)

## SilentWolf integration

To integrate SilentWolf into your project, follow these steps:

1. Create configuration file\
   Create a file named `silent_wolf.cfg` in the `.\secrets` directory.
2. Add the following content\
   Replace `SILENTWOLF-API-KEY` and `GAME-ID` with your actual API key and game ID.

```
[silent_wolf]
api_key="SILENTWOLF-API-KEY"
game_id="GAME-ID"
```

Ensure the `.\secrets directory` and the `silent_wolf.cfg` file are properly secured to protect sensitive information.
