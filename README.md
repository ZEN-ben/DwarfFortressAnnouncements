# Dwarf Fortress Announcements

## Instructions
1. Download the latest release "dwarf_fortress_announcements_1_x_x.zip" file.
2. Extract anywhere and start "Dwarf Fortress Log.exe"
3. Optionally: to help me develop this tool further, leave feedback!
   - [Bay12 official Dwarf Fortress forum](http://www.bay12forums.com/smf/index.php?topic=179885.0)
   - [Reddit thread](https://www.reddit.com/r/dwarffortress/comments/uui414/presenting_dfa_customizable_overlay_for/)

## Introduction
This tool adds an overlay to Dwarf Fortress to display the game log. 

<img src="https://user-images.githubusercontent.com/2108992/169597728-e12b83db-f920-4d9b-8002-9f74b893eb2c.png" width="600">

## Configuration
The overlay is configurable by adjusting the config.yaml file:
``` yaml
readback: 5000 # characters
height: 400
width: 432
offsetX: 7
offsetY: 40
opacity: 0.75

customColors:
    - name: CombatRedDark
      hex: "#400000"
    - name: CombatRedMedium
      hex: "#800000"
    - name: CombatRedLight
      hex: "#FF0000"

rules:
#   - regex: <any valid regex> # make sure to escape YAML
#     skip: <true|false> (optional) # any messages that match this regex will not be shown
#     foreground: <color token> (optional) # see https://docs.microsoft.com/en-us/dotnet/api/system.windows.media.colors?view=windowsdesktop-6.0
#     background: <color token> (optional) # defaults to transparent

  - regex: STARTING NEW GAME
    foreground: White
    background: DarkGreen
    
    
  - regex: has been found dead.
    foreground: Black
    background: Orchid

# Combat
  - regex: lightly tapping the target!
    skip: true

  - regex: tapping
    foreground: LightGray
    background: CombatRedDark

```
