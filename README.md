# ePSXeQuitter

The app to close ePSXe using gamepad

## Description

Show menu window when Left Shoulder and Guide button (Xbox button) pressed while ePSXe runs. (the hotkey is customizable.)

## Features

- save/load states using gamepad.
- close ePSXe using gamepad.

![screenshot](https://github.com/gridgem/ePSXeQuitter/wiki/ePSXeQuitter_0_1_0.gif "screenshot")

## Requirement

- the gamepad acceptable XInput (like Xbox 360 Controller. I use DUALSHOCK 3 with [SCP Driver](http://forums.pcsx2.net/Thread-XInput-Wrapper-for-DS3-and-Play-com-USB-Dual-DS2-Controller).)
- XINPUT1_3.dll (included to [DirectX End-User Runtimes (June 2010)](https://www.microsoft.com/en-us/download/details.aspx?id=8109))
- .NET Framework 4.6

## Usage

1. Drag & Drop .iso/.bin file to this application icon. (or use it with command line. Read "More Usage" below.)
2. Press Left Shoulder and Guide Button (Xbox button) in game and it shows menu. (the game does NOT pause automatically and menu is unclickable and undragable.)
3. Select menu with pressing Up and Down button on gamepad and select menu pressing A Button.

(detailed behavior is written on "Application Behavior" section below.)

## Install

1. Put These files into the folder same as ePSXe.exe. (or other folder. Read "More Usage" below.)
- ePSXeQuitter.exe
- INIFileParser.dll
- WindowsInput.dll
- ePSXeQuitter.ini (option)

## Uninstall

1. Delete the installed file and the folder named "sstates_eQ" in the ePSXe folder.

## More Usage

This app accept command line option as same as ePSXe. (and passes options to ePSXe.)

"-loadbin" or "-cdrom" option is mandatory.

```
ePSXeQuitter -nogui -f -loadbin X:/path/to/game_title_file.iso
```

editting ePSXeQuitter.ini allows some tweaks.

- To install other folder, set "ExePath = X:\path\to\ePSXe.exe".
- To change hotkey, set like "Hotkey = L2, R2, Playstation".

## Thanks

- [INI File Parser](https://github.com/rickyah/ini-parser)
- [Windows Input Simulator](https://github.com/michaelnoonan/inputsimulator)
- [XInputDotNet](https://github.com/speps/XInputDotNet)
- [Launcher Icon Generator](https://romannurik.github.io/AndroidAssetStudio/icons-launcher.html)

## Author

gridgem

## Application Behavior

This application just operate files and simulate key press.

It checks the title ID with args to detect state files' name.

it executes ePSXe (the game starts) and get process ID.
 
When pressed gamepad left Shoulder button and Guide button (Xbox button), it shows menu if foreground window is same as saved process ID.

The window has 5 menus at first.

- Save State : save state to selected slot. 
- Load State : load slot from selected slot. 
- Save State and Exit: save state to selected slot and exit ePSXe. 
- Exit ePSXe without saving : trying close ePSXe.
- Cancel : just close this app's window.

#### Save / Load States

"Save State", "Load State" and "Save State and Exit" has submenu which contains state files associated with the title (max 5 files).

As you know we can't select slots declaratively, when load states, all the state files associated with the title (max 10 files including .pic files) are moved to backup folder named "sstate_eQ" in same folder as state folder. It copies selected state file as .000 - .004 file and simulate F3 key down, move back backup files to the original state folder.

When save state as same as loading it's impossible to detect which slot is selected, the state files (max 10 files) is move to backup folder, simulate F1 key down and move back state files except state file saved now.

## ToDo

- Better UI
- Better Error Handling
- .cue file support
- more distinct icon :smile:

## Contribute

Please correct my bad English.

## Notes

Once ePSXe lost focus when it is activated again display goes out of order at least in my environment, this app's menu doesn't activated to keep ePSXe activated.

Because ePSXe doesn't release focus, it reacts to the gamepad while selecting menu. Actually, when hotkey is pressed it saves state. (If it is in vain, the state file will be deleted.)

I want to implement DirectInput but I don't have handy DirectInput gamepads.

Though I know it's not good manner to close ePSXe using Alt + F4, it doesn't roll back the display resolution with Esc key in my environment.

After about 2 sec is elapsed from closing ePSXe, it kills the process because ePSXe process tend to remain there.

