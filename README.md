<p align="center">
  <img src="https://raw.githubusercontent.com/julianperrott/WowClassicGrindBot/master/images/starme.png" alt="Star this Repo"/>
</p>

# Master Of Puppets

- The project current goal is to support `Burning Crusade Classic`

- Addon: https://github.com/FreeHongKongMMO/Happy-Pixels to read the game state. Over the time its been heavily rewritten and optimized.

- Frontend: uses [Blazor](https://docs.microsoft.com/en-us/aspnet/core/blazor) to show the state in a browser.

- Backend: written in C#. Screen capture, mouse and keyboard clicking. No memory tampering and DLL injection.

- Further detail about the architecture can be found in [Blog post](http://www.codesin.net/post/wowbot/).

- Pathing: Indoors pathfinding only works properly if `PathFilename` is exists. For outdoor there are multiple solutions:
    * V1 Local - In processs [PPather](https://github.com/namreeb/PPather).
    * V1 Remote - Out of process [PPather](https://github.com/Xian55/WowClassicGrindBot/tree/main/PathingAPI).
    * V3 Remote - Out of process [AmeisenNavigation](https://github.com/Xian55/AmeisenNavigation/tree/feature/guess-z-coord-after-rewrite)

# Features

- Game fullscreen or windowed mode
- Addon supports all available client languages
- Most of the classes should work. Some classes have more support than others.
- Highly configurable Combat rotation described in `ClassConfiguration`
- Utilizing the Actionbar related APIs to retrive ActionbarSlot(usable, cost)
- Based on the `ClassConfiguration` file populate Actionbar
- Pathfinding in the current zone to the grind location
- Grind mobs in the described `PathFilename`
- Blacklist certain NPCs
- Loot and Skinning
- Vendor goods
- Repair equipments
- Corpse run

# Media

![Screenshot](https://raw.githubusercontent.com/Xian55/WowClassicGrindBot/main/images/Screenshot.png)

[![YouTube Video](https://img.youtube.com/vi/CIMgbh5LuCc/0.jpg)](https://www.youtube.com/watch?v=CIMgbh5LuCc)

# Issues and Ideas

Create an issue with the given template.

# Contributing

You are welcome to create pull requests. Some ideas of things that could be improved:

* This readme
* The route recording and editing
* More route and class profiles

# Contribution

* Runtime Class Profile picker
* Runtime Path Profile autocomplete search
* Frontend Dark mode
* Improved Loot Goal
* Added Skinning Goal
* Introduced a concept of Produce/Consume corpses. In short killing multiple enemies in a single combat, can loot and skin them all.
* `ActionbarPopulator` based on class config
* `DataConfig`: change where the external data(DBC, MPQ, profiles) can be found
* Edit the loaded profile from frontend
* `NPCNameFinder`: extended to friendly/neutral units
* Remap essential keybindings and support more Actionbar slots up to `34`
* Added a new input system to handle modifier keys
* Support more 4:3 aspect ratio based resolution
* Addon is rewritten/reorganized with performance in mind(caching and reduce cell paint) to achive game refresh rate speed

# Getting it working

## 1. Download this repository

Put the contents of the repo into a folder. e.g "C:\WowClassicGrindBot". I am going to refer to this folder from now on, so just substitute your own folder path.

## 2 Using V1 Local/Remote Pathhing: Download the MPQ route files

This files are required for to find paths from where you are to the grind area, vendor and repair.

* Classic: [**common-2.MPQ**](https://drive.google.com/file/d/1k80qqC02Xvpxfy5JQjzAkoixj8b4-EEP/view?usp=sharing) (1.7Gb)
* TBC: [**expansion.MPQ**](https://mega.nz/file/Of4i2YQS#egDGj-SXi9RigG-_8kPITihFsLom2L1IFF-ltnB3wmU) (1.8Gb)

Copy the previusly mentioned files to **\Json\MPQ** folder (e.g. C:\WowClassicGrindBot\Json\MPQ)

## 3.1 System Requirements

* Windows 7 and above
* [.netCore 3.1 **x86** SDK](https://dotnet.microsoft.com/download/dotnet-core/3.1 )
* Nvidia Control panel settings
    * Make sure the `Image Sharpening` under the `Manage 3D Settings`-> Global settings or Program Settings(for WoW) is set to `Sharpening Off, Scaling disabled`!
* Resultions which based on 4:3 aspect ratio 1024x768, 1920x1080, 4k
* Check these settings in the game client. Other values will stop the bot from being able to read the addon data.
  * System > Advanced > Constrast: 50
  * System > Advanced > Brightness: 50
  * System > Advanced > Gamma from: 1.0
  * System > Render Scale: 100%
  * Disable Glow effect - type in the chat `/console ffxGlow 0`
  * To keep/save this settings make sure to properly shutdown the game.

## 3.2 Optional - Replace default game Font

I would highly suggest to replace the default World of Warcraft font with a much **Bolder** one with [this guide](https://tbc.wowhead.com/guides/changing-wow-text-font)

Should be only concerned about `Friz Quadrata: the "Everything Else" Font` which is the `FRIZQT__.ttf` named file.

Example - [Robot-Medium](https://fonts.google.com/specimen/Roboto?thickness=5) - Shows big improvement to the `NpcNameFinder` compontent which is responsible to find - friendly, enemy, corpse - names above NPCs head.

## 4. Build the application

One of the following IDE or command line
* Visual Studio
* Visual Studio Code
* Powershell

You will need .net core 3.1 **x86** SDK installed. **Note:** you need the **x86** version, not the **x64** one.

e.g. Build from Powershell

```ps
cd C:\WowClassicGrindBot
dotnet build
```

![Build](https://raw.githubusercontent.com/julianperrott/WowClassicGrindBot/master/images/build.png)

## 5. Configuration process

The bot reads the game state using small blocks of colour shown at the top of the screen by an Addon. This needs to be configured.

1. Edit the batch script in `C:\WowClassicGrindBot\BlazorServer` called run.bat, change it to point at where you have put the repo BlazorServer folder

    e.g.
    ```ps
    start "C:\Program Files (x86)\Google\Chrome\Application\chrome.exe" "http://localhost:5000"
    c:
    cd C:\WowClassicGrindBot\BlazorServer
    dotnet run
    pause
    ```

2. Execute the `run.bat`. This will start the bot and Chrome, Wow must be already running. If you get `"Unable to find the Wow process is it running ?"` in the console window then it can't find exe.

3. When running the BlazorServer for the first time you will have to follow a setup process:
    * Just start the game and wait in the character selection screen.
    * Click `2. Addon Configuration`
    * Click `Find InstallPath` -> `InstallPath` should be filled otherwise, fill out manually
    * Fill the `Author` freely
    * Fill the `Title` freely
    * Then press `Install & Save` button -> Log should see `AddonConfigurator.Install successful`
    * Required to restart the Game 
    * Enter world with your desired character
    * Click `5. Frame Configuration`
    * Click `Auto Configure and Restart`

4. Under the `Addon Configuration` you can check if theres a **newer version available** for the addon. In that case just press the `install` button then have to restart the game client and the bot it self in order to use it properly. 

## 5. The bot should restart and show the dashboard page.

## 6. Configure the Wow Client - Interface Options

We need to make sure that certain interface options are set. The most important are Click to move and screen flashing on low health. See the list below.

### Interface Options

From the main menu (ESC) set the following under Interface Options:

| Interface Option | Value |
| ---- | ---- |
| Controls - Auto Loot | **Ticked** |
| Controls - Interact on Left click | **Not ticked** |
| Combat - Do Not Flash Screen at Low Health | **Ticked** |
| Combat - Auto Self Cast | **Ticked** |
| Names - NPC Names | **Ticked** |
| Names - Enemy Units (V) | **Not ticked** |
| Camera - Auto-Follow Speed | **Fast** |
| Camera - Camera Following Style | **Always** |
| Mouse - Click-to-Move | **Ticked** |
| Mouse - Click-to-Move Camera Style | **Always** |

## 7. Configure the Wow Client - Key Bindings

The "Interact with Target" keybind is super important as it allows the bot to turn towards and approach the target.
The "Target Last Target " keybind helps with looting.

From the main menu (ESC) set the following:

"Movement Keys" Key Bindings:
| Command | Key | ClassConfiguration KeyAction | Desciption |
| ---- | ---- | ---- | ---- |
| Jump | Spacebar | JumpKey | ---- |
| Sit/Move Down | X | StandUpKey | Used after drinking or eating |

"Targeting" Key Bindings:

| Command | Key | ClassConfiguration KeyAction | Desciption |
| ---- | ---- | ---- | ---- |
| Target Nearest Enemy | Tab | TargetNearestTargetKey | ---- |
| Target Pet | Multiply | TargetPetKey | Only pet based class |
| Target Last Target | G | TargetLastTargetKey | ---- |
| Interact With Target | I | InteractKey | ---- |
| Assist Target | F | TargetTargetOfTargetKey | ---- |
| Pet attack | Subtract | PetAttackKey | Only pet based class |

## 7.1. Actionbar Key Bindings:

The default class profiles assumes the following `Keybinds` setup and using English Keyboard layout.
In total, `34` key supported.

Highly recommended to use the default setup, in order to get properly working the `ActionBarSlotCost` and `ActionBarSlotUsable`!

| ActionSlot | Key | Description |
| --- | --- | --- |
| 1-10 | 1,2,3 .. 9,0,-,= | 0 is the 10th key. |
| Bottom Right ActionBar | - | - |
| 49-58 | N1,N2,N3 .. N9,N0 | N means Numpad - 0 is the 10th key |
| Bottom Left ActionBar | - | - |
| 61-72 | F1,F2,F3 .. F11,F12 | F means Functions |

## 8. Configure the Wow Client - Bindpad addon

Bindpad allows keys to be easily bound to commands and macros. Type `/bindpad` to show it.

For each of the following click + to add a new key binding.

|  Key |  Command |
| ---- | ---- |
| Delete | /stopattack<br>/stopcasting<br>/petfollow |
| Insert | /cleartarget |

## 9. Setting up the class file (Final step)

Each class has a configuration file in `/Json/class` e.g. the config for a Rogue it is in file `C:\WowClassicGrindBot\Json\class\Rogue.json`.

The configuration file determines what spells you cast when pulling and in combat, where to vend and repair and what buffs you give yourself.

Take a look at the class files in `/Json/class` for examples of what you can do. Your class file probably exists and just needs to be edited to set the pathing file name, but note they may be set up for level 60.

### Path

The path that the class follows is a `json` file in `/Json/path/` which contains a list of `x` & `y` coordinates the bot will traverse while looking for mobs.
```json
"PathFilename": "58_Winterspring.2.json",   // the path to walk when alive
"PathThereAndBack": true,                   // if true walks the path and the walks it backwards.
"PathReduceSteps": true,                    // uses every other coordinate.
```
### **KeyAction** - Commands

Each `KeyAction` has its own properties to describe what the action is all about. 

Can specify conditions with `Requirement(s)` in order to create a matching action for the situation.

| Property Name | Description | Default value |
| --- | --- | --- |
| Name | Name of the command. For the `ActionBarPopulator`, lowercase means macro. | `""` |
| HasCastBar | Does the spell have a cast bar | `false` |
| StopBeforeCast | Should the char stop moving before casting the spell | `false` |
| Key | The key to click (`ConsoleKey`) | `""` |
| PressDuration | How many milliseconds to press the key for | `50` |
| Form | Shapeshift/Stance form to be in to cast this spell | `Form.None` |
| CastIfAddsVisible | If the bot can "See" any adds | `false` |
| Charge | How many times shoud this Command be used in sequence and ignore its Cooldown | `1` |
| Cooldown | The cooldown in milliseconds until the command can be done again | `0` |
| School | Indicate what type of element. `SchoolMask.`<br>(`Physical, Holy, Fire, Nature, Frost, Shadow, Arcane`) | `SchoolMask.None` |
| MinMana | The minimum `Mana` required to cast the spell | `0` |
| MinRage | The minimum `Rage` required to cast the spell | `0` |
| MinEnergy | The minimum `Energy` required to cast the spell | `0` |
| MinComboPoints | The minimum combo points required to cast the spell | `0` |
| WhenUsable | When not in in-game cooldown(`GCD` included) and have the min resource(mana,rage,energy) to use it. | `false` |
| Requirement | A single "Requirement" (See below) which must be true | |
| Requirements | A list of "Requirements" which must be true | |
| WaitForWithinMeleeRange | `PullGoal` only - After casting wait for the mob to be in melee range. Will be repeated until the conditions are met | `false` |
| WaitForGCD | Indicates should wait for the GCD | `true` |
| ResetOnNewTarget | Reset the cooldown if the target changes | `false` |
| Log | Write to the log when this key is evaluated | `true` |
| DelayBeforeCast | A delay in milliseconds before this spell is cast | `0` |
| DelayAfterCast | The delay in milliseconds after the spell is cast | `1450` |
| AfterCastWaitBuff | After the cast happened, should wait until __(player debuff/buff or target debuff/buff)__ changed | `false` |
| AfterCastWaitNextSwing | After the cast wait for the next melee swing to land | `false` | 
| Cost | For Adhoc goals the priority | `18` |
| InCombat | Can it be cast in combat | `false` |
| StepBackAfterCast | Hero will go back for X milliseconds after casting this spell , usable for spells like `Frost Nova` | `0` |
| PathFilename | For NPC goals, this is a short path to get close to the NPC to avoid walls etc. | `""` |
| UseWhenTargetIsCasting | Checks for the target casting/channeling any spell (possible values: `null` -> ignore / `false` -> when enemy not casting / `true` -> when enemy casting) | `null` |

Some of these properties are optional and not required to be specified. However can create pretty complex conditions and branches to suit the situation.

e.g. - bare minimum for a spell which has castbar. **Note:** _there are some spells which dosen't have visible castbar. like `"Throw"`, `"Shoot"`, `"Auto Shot"`_
```json
{
    "Name": "Frostbolt",
    "Key": "1",
    "HasCastBar": true,   //<-- Must be indicated the spell has a castbar
}
```

e.g. - bare minimum for a spell which is instant (no castbar)
```json
{
    "Name": "Earth Shock",
    "Key": "6"
}
```

Theres are only two specially named KeyAction `Food` and `Water` which is reserved for eating and drinking.

They already have some prebaked `Requirement` conditions in order to avoid mistype the definition. 

The bare minimum for `Food` and `Water` is looks something like this.
```json
{
    "Name": "Food",
    "Key": "-",
    "Requirement": "Health%<50"
},
{
    "Name": "Water",
    "Key": "=",
    "Requirement": "Mana%<50"
}
```

e.g. for Rogue ability
```json
{
    "Name": "Slice And Dice",
    "Key": "3",
    "MinEnergy": 25,
    "MinComboPoints": 2,
    "Cooldown": 3000,
    "Requirement": "not Slice And Dice"
}
```

---
### Pull Goal

This is the `Sequence` of `KeyAction(s)` that are used when pulling a mob.

e.g.
```json
"Pull": {
    "Sequence": [
        {
            "Name": "Concussive Shot",
            "Key": "9",
            "StopBeforeCast": true,
            "Requirements": ["HasRangedWeapon", "!InMeleeRange", "HasAmmo"]
        }
    ]
}
```

### Combat Goal

The `Sequence` of `KeyAction(s)` that are used when in combat and trying to kill a mob. 

The combat goal does the first available command on the list. 

The goal then runs again re-evaluating the list before choosing the first available command again, and so on until the mob is dead.

e.g.
```json
"Combat": {
    "Sequence": [
        {
            "Name": "Fireball",
            "Key": "2",
            "HasCastBar": true,
            "MinMana": 30,
            "Requirement": "TargetHealth%>20"
        },
        {
            "Name": "AutoAttack",
            "Requirement": "not AutoAttacking"
        },
        {
            "Name": "Approach",
            "Log": false
        }
    ]
}
```

### Adhoc Goals

These `Sequence` of `KeyAction(s)` are done when not in combat and are not on cooldown. Suitable for personal buffs.

e.g.
```json
"Adhoc": {
    "Sequence": [
        {
            "Name": "Frost Armor",
            "Key": "3",
            "MinMana": 60,
            "Requirement": "not Frost Armor"
        },
        {
            "Name": "Food",
            "Key": "=",
            "Requirement": "Health%<30"
        },
        {
            "Name": "Water",
            "Key": "-",
            "Requirement": "Mana%<30"
        }
    ]
}
```

### Parallel Goals

These `Sequence` of `KeyAction(s)` are done when not in combat and are not on cooldown. 

The keypresses happens simultaneously on all `KeyAction(s)` which mets the `Requirement`.

Suitable for `Food` and `Water`.

e.g.
```json
"Parallel": {
    "Sequence": [
        {
            "Name": "Food",
            "Key": "=",
            "Requirement": "Health%<50"
        },
        {
            "Name": "Water",
            "Key": "-",
            "Requirement": "Mana%<50"
        }
    ]
}
```
### NPC Goals

These command are for vendoring and repair.

e.g.
```json
"NPC": {
    "Sequence": [
    {
        "Name": "Repair",
        "Key": "C",
        "Requirement": "Items Broken",
        "PathFilename": "Tanaris_GadgetzanKrinkleGoodsteel.json",
        "Cost": 6
    },
    {
        "Name": "Sell",
        "Key": "C",
        "Requirement": "BagFull",
        "PathFilename": "Tanaris_GadgetzanKrinkleGoodsteel.json",
        "Cost": 6
    }
    ]
}
```

The "Key" is a key that is bound to a macro. The macro needs to target the NPC, and if necessary open up the repair or vendor page. The bot will click the key and the npc will be targetted. Then it will click the interact button which will cause the bot to move to the NPC and open the NPC options, this may be enough to get the auto repair and auto sell greys to happen. But the bot will click the button again in case there are further steps (e.g. SelectGossipOption), or you have many greys or items to sell.

e.g. Sell macro - bound to the `"C"` key using BindPad or Key bindings
```cs
/tar Jannos Ironwill
/run DataToColor[1]:sell({"Light Leather","Cheese","Light Feather"});
```

e.g. Repair macro
```cs
/tar Vargus
/script SelectGossipOption(1)
```

e.g. Delete various items
```cs
/run for b=0,4 do for s=1,GetContainerNumSlots(b) do local n=GetContainerItemLink(b,s) if n and (strfind(n,"Slimy") or strfind(n,"Red Wolf") or strfind(n,"Mystery") or strfind(n,"Spider L")) then PickupContainerItem(b,s) DeleteCursorItem() end end end
```

Because some NPCs are hard to reach, there is the option to add a short path to them e.g. `"Tanaris_GadgetzanKrinkleGoodsteel.json"`. The idea is that the start of the path is easy to get to and is a short distance from the NPC, you record a path from the easy to reach spot to the NPC with a distance between spots of 1. When the bot needs to vend or repair it will path to the first spot in the list, then walk closely through the rest of the spots, once they are walked it will press the defined Key, then walk back through the path.

e.g. `Tanaris_GadgetzanKrinkleGoodsteel.json` in the `Json/path` folder looks like this:
```json
[{"X":51.477,"Y":29.347},{"X":51.486,"Y":29.308},{"X":51.495,"Y":29.266},{"X":51.503,"Y":29.23},{"X":51.513,"Y":29.186},{"X":51.522,"Y":29.147},{"X":51.531,"Y":29.104},{"X":51.54,"Y":29.063},{"X":51.551,"Y":29.017},{"X":51.559,"Y":28.974},{"X":51.568,"Y":28.93},{"X":51.578,"Y":28.889},{"X":51.587,"Y":28.853},{"X":51.597,"Y":28.808}]
```
If you have an NPC that is easy to get to such as the repair NPC in Arathi Highlands then the path only needs to have one spot in it. e.g.
```json
[{"X":45.8,"Y":46.6}]
```

Short Path Example:

![Short Path Example](https://raw.githubusercontent.com/julianperrott/WowClassicGrindBot/master/images/NPCPath.png)

### Repeatable Quests Handin

In theory if there is a repeatable quest to collect items, you could set up a NPC task as follows. See 'Bag requirements' for Requirements format.
```json
{
    "Name": "Handin",
    "Key": "K",
    "Requirements": ["BagItem:12622:5","BagItem:12623:5"],
    "PathFilename": "Path_to_NPC.json",
    "Cost": 6
}
```

# Requirement

A requirement is something that must be evaluated to be `true` for the `KeyAction` to run. 

Not all `KeyAction` needs a requirement, some just rely on a `Cooldown` or `MinMana`/`MinRage`/`MinEnergy`. 

A requirement can be put into a list if there is more than one.

e.g.
```json
{
    "Name": "Soul Shard",
    "Key": "9",
    "HasCastBar": true,
    "Requirements": ["TargetHealth%<36", "not BagItem:6265:3"],     //<--- Requirement List
    "MinMana": 55
},
{
    "Name": "Curse of Weakness",
    "Key": "6",
    "WhenUsable": true,
    "ResetOnNewTarget": true,
    "Requirement": "!Curse of Weakness",                           //<--- Single Requirement
    "MinMana": 20
}
```

### **Negate a requirement**

Every requirement can be negated by adding one of the `Negate keyword` in front of the requirement.

Formula: `[Negate keyword][requirement]`

| Negate keyword |
| --- |
| `"not "` |
| `"!"` |

e.g.
```json
"Requirement": "not Curse of Weakness"
"Requirement": "!BagItem:6265:3"
```
---

### **And / Or multiple Requirements**

Two or more Requirement can be merged into a single Requirement object. 

By default every Requirement is concataneted with `[and]` operator which means in order to execute the `KeyAction`, every member in the `RequirementsObject` must be evaluated to `true`. However this consctruct allows to concatanete with `[or]`.

Formula: `[Requirement1][Operator][[RequirementN]`

| Operator | Description |
| --- | --- |
| "&&" | And |
| "\|\|" | Or |

**Note:** _Currently only one type of the [Operator] is handled in a single Requirement. So mixing [&&] and [||] is not supported._

e.g.
```json
"Requirements": ["Has Pet", "TargetHealth%<70||TargetCastingSpell"]     // || in action
"Requirements": ["not Form:Druid_Bear", "Health%<50||MobCount>2"]       // || in action
```
---
### **Value base requirements**

Value base requirement is the most basic way to create a condition. 

Formula: `[Keyword][Operator][Numeric integer value]`

**Note:** `[Numeric integer value]` always the _right-hand_ side expression value

| Operator | Description | 
| --- | --- |
| `==` | Equals |
| `<=` | Less then or Equals |
| `>=` | Greater then or Equals |
| `<` | Less then |
| `>` | Greater then |

| Keyword | Description |
| --- | --- |
| `Health%` | Player health in percentage |
| `TargetHealth%` | Target health in percentage |
| `PetHealth%` | Pet health in percentage |
| `Mana%` | Player mana in percentage |
| `BagCount` | How many items in the player inventory |
| `MobCount` | How many detected, alive, and currently fighting mob around the player |
| `MinRange` | Minimum distance(yard) between the player and the target  |
| `MaxRange` | Maximum distance(yard) between the player and the target |
| `LastAutoShotMs` | Time since last detected AutoShot happened in milliseconds |
| `LastMainHandMs` | Time since last detected Main Hand Melee swing happened in milliseconds |

For the `MinRange` and `MaxRange` gives an approximation range distance between the player and target.

**Note:** _Every class has it own unique way to find these values by using different in game items/spells/interact ways._

| MinRange | MaxRange | alias Description |
| --- | --- | --- |
| 0 | 5 | "InMeleeRange" |
| 5 | 15 | "IsInDeadZoneRange" |

e.g.
```json
"Requirement": "Health%>70"
"Requirement": "TargetHealth%<=10"
"Requirement": "PetHealth%<10"
"Requirement": "Mana%<=40"
"Requirement": "BagCount>80"
"Requirement": "MobCount>1"
"Requirement": "MinRange<5"
"Requirement": "MinRange>15"
"Requirement": "MaxRange>20"
"Requirement": "MaxRange>35"
"Requirement": "LastAutoShotMs<=500"
"Requirement": "LastMainHandMs<=500"
```

---
### **npcID requirements**

If a particular npc is required then this requirement can be used.

Formula: `npcID:[Numeric integer value]`

e.g.

* `"Requirement": "not npcID:6195"` - target is not [6195](https://tbc.wowhead.com/npc=6195)
* `"Requirement": "npcID:6195"` - target is [6195](https://tbc.wowhead.com/npc=6195)

---
### **Bag requirements**

If an `itemid` must be in your bag with given `count` quantity then can use this requirement. 

Useful to determine when to create warlock Healthstone or soul shards.

Formula: `BagItem:[itemid]:[count]`

e.g.

* `"Requirement": "BagItem:5175"` - Must have a [Earth Totem](https://tbc.wowhead.com/item=5175) in bag
* `"Requirement": "BagItem:6265:3"` - Must have atleast [3x Soulshard](https://tbc.wowhead.com/item=6265) in bag
* `"Requirement": "not BagItem:19007:1"` - Must not have a [Lesser Healthstone](https://tbc.wowhead.com/item=19007) in bag
* `"Requirement": "not BagItem:6265:3"` - Must not have [3x Soulshard](https://tbc.wowhead.com/item=6265) in bag

---
### **Form requirements**

If the player must be in the specified `form` use this requirement. 

Useful to determine when to switch Form for the given situation.

Formula: `Form:[form]`

| form |
| --- |
| None
| Druid_Bear |
| Druid_Aquatic |
| Druid_Cat |
| Druid_Travel |
| Druid_Moonkin |
| Druid_Flight |
| Druid_Cat_Prowl |
| Priest_Shadowform |
| Rogue_Stealth |
| Rogue_Vanish |
| Shaman_GhostWolf |
| Warrior_BattleStance |
| Warrior_DefensiveStance |
| Warrior_BerserkerStance |
| Paladin_Devotion_Aura |
| Paladin_Retribution_Aura |
| Paladin_Concentration_Aura |
| Paladin_Shadow_Resistance_Aura |
| Paladin_Frost_Resistance_Aura |
| Paladin_Fire_Resistance_Aura |
| Paladin_Crusader_Aura |

e.g.
```json
"Requirement": "Form:Druid_Bear"       // Must be in `Druid_Bear` form
"Requirement": "not Form:Druid_Cat"    // Shoudn't be in `Druid_Cat` form
```

---
### **Race requirements**

If the character must be the specified `race` use this requirement. 

Useful to determine Racial abilities.

Formula: `Race:[race]`

| race | 
| --- |
| None |
| Human |
| Orc |
| Dwarf |
| NightElf |
| Undead |
| Tauren |
| Gnome |
| Troll |
| Goblin |
| BloodElf |
| Draenei |

e.g. 
```json
"Requirement": "Race:Orc"          // Must be `Orc` race
"Requirement": "not Race:Human"    // Shoudn't be `Human` race
```

---
### **Spell requirements**

If a given Spell `name` or `id` must be known by the player then you can use this requirement. 

Useful to determine when the given `Spell` is exists in the spellbook.

It has the following formulas:

* `Spell:[name]`. The `name` only works with the English client name.
* `Spell:[id]`

e.g.

* `"Requirement": "Spell:687"` - Must have know [`id=687`](https://tbc.wowhead.com/item=687)
* `"Requirement": "Spell:Demon Skin"` - Must have known the given `name`
* `"Requirement": "not Spell:702"` - Must not have known the given [`id=702`](https://tbc.wowhead.com/item=702)
* `"Requirement": "not Spell:Curse of Weakness"` - Must not have known the given `name`

---
### **Talent requirements**

If a given Talent `name` must be known by the player then you can use this requirement. 

Useful to determine when the given Talent is learned. Also can specify how many points have to be spent minimium with `rank`.

Formula: `Talent:[name]:[rank]`. The `name` only works with the English client name.

e.g.

```json
"Requirement": "Talent:Improved Corruption"    // Must known the given `name`
"Requirement": "Talent:Improved Corruption:5"  // Must know the given `name` and atleast with `rank`
"Requirement": "not Talent:Suppression"        // Must have not know the given `name` 
```
---
### **Buff / Debuff / General boolean condition requirements**

Allow requirements about what buffs/debuffs you have or the target has or in general some boolean based requirements.

| General boolean condition | Desciption |
| --- | --- |
| `"TargetYieldXP"` | The target yields experience upon death. (Grey Level) |
| `"TargetCastingSpell"` | Target casts any spell |
| `"Swimming"` | The player is currently swimming. |
| `"Falling"` | The player is currently falling down, not touching the ground. |
| `"Has Pet"` | The player's pet is alive |
| `"Pet Happy"` | Pet happienss is green |
| `"BagFull"` | Inventory is full |
| `"Items Broken"` | Has any broken(red) worn item |
| `"HasRangedWeapon"` | Has equipped ranged weapon (wand/crossbow/bow/gun) |
| `"HasAmmo"` | AmmoSlot has equipped ammo and count is greater than zero |
| `"InMeleeRange"` | Target is approximately 0-5 yard range |
| `"InDeadZoneRange"` | Target is approximately 5-11 yard range |
| `"InCombatRange"` | Class based - Have any ability which allows you to attack target from current place |
| `"OutOfCombatRange"` | Negated value of "InCombatRange" |
| `"AutoAttacking"` | Auto spell `Auto Attack` is active |
| `"Shooting"` | (wand) Auto spell `Shoot` is active |
| `"AutoShot"` | (hunter) Auto spell `Auto Shot` is active |
| `"HasMainHandEnchant"` | Indicates that main hand weapon has active poison/sharpening stone/shaman buff effect |
| `"HasOffHandEnchant"` | Indicates that off hand weapon has active poison/sharpening stone/shaman buff effect |

<table>
<tr><th>Buffs</th><th>Debuffs</th></tr>
<tr valign="top"><td>

| Class | Buff Condition |
| --- | --- |
| All | `"Well Fed"` |
| All | `"Eating"` |
| All | `"Drinking"` |
| All | `"Mana Regeneration"` |
| All | `"Clearcasting"` |
| Druid | `"Mark of the Wild"` |
| Druid | `"Thorns"` |
| Druid | `"TigersFury"` |
| Druid | `"Prowl"` |
| Druid | `"Rejuvenation"` |
| Druid | `"Regrowth"` |
| Mage | `"Frost Armor"` |
| Mage | `"Ice Armor"` |
| Mage | `"Arcane Intellect"` |
| Mage | `"Ice Barrier"` |
| Mage | `"Ward"` |
| Mage | `"Fire Power"` |
| Mage | `"Mana Shield"` |
| Mage | `"Presence of Mind"` |
| Mage | `"Arcane Power"` |
| Paladin | `"Seal"` |
| Paladin | `"Aura"` |
| Paladin | `"Blessing"` |
| Paladin | `"Blessing of Might"` |
| Priest | `"Fortitude"` |
| Priest | `"InnerFire"` |
| Priest | `"Divine Spirit"` |
| Priest | `"Renew"` |
| Priest | `"Shield"` |
| Rogue | `"Slice And Dice"` |
| Rogue | `"Stealth"` |
| Warlock | `"Demon Armor"` |
| Warlock | `"Demon Skin"` |
| Warlock | `"Shadow Trance"` |
| Warlock | `"Soulstone Resurraction"` |
| Warlock | `"Soul Link"` |
| Warrior | `"Battle Shout"` |
| Shaman | `"Lightning Shield"` |
| Shaman | `"Water Shield"` |
| Shaman | `"Shamanistic Focus"` |
| Shaman | `"Focused"` |
| Shaman | `"Stoneskin"` |
| Hunter | `"Aspect of the Cheetah"` |
| Hunter | `"Aspect of the Pack"` |
| Hunter | `"Aspect of the Beast"` |
| Hunter | `"Aspect of the Hawk"` |
| Hunter | `"Aspect of the Wild"` |
| Hunter | `"Aspect of the Monkey"` |
| Hunter | `"Rapid Fire"` |
| Hunter | `"Quick Shots"` |

</td>
<td valign="top">

| Class | Debuff Condition |
| --- | --- |
| Druid | `"Demoralizing Roar"` |
| Druid | `"Faerie Fire"` |
| Druid | `"Rip"` |
| Druid | `"Moonfire"` |
| Druid | `"Entangling Roots"` |
| Druid | `"Rake"` |
| Mage | `"Frostbite"` |
| Mage | `"Slow"` |
| Priest | `"Shadow Word: Pain"` |
| Warlock | `"Curse of"` |
| Warlock | `"Curse of Weakness"` |
| Warlock | `"Curse of Elements"` |
| Warlock | `"Curse of Recklessness"` |
| Warlock | `"Curse of Shadow"` |
| Warlock | `"Curse of Agony"` |
| Warlock | `"Siphon Life"` |
| Warlock | `"Corruption"` |
| Warlock | `"Immolate"` |
| Warrior | `"Rend"` |
| Hunter | `"Serpent Sting"` |

</td></tr> </table>

e.g.
```json
"Requirement": "not Well Fed"      // I am not well fed.
"Requirement": "not Thorns"        // I don't have the thorns buff.
"Requirement": "AutoAttacking"     // "Auto Attack" spell is active.
"Requirement": "Shooting"          // "Shoot" spell is active.
"Requirement": "Items Broken"      // Some of my armor is broken (red).
"Requirement": "BagFull"           // Inventory is full.
"Requirement": "HasRangedWeapon"   // Has an item equipped at the ranged slot.
"Requirement": "InMeleeRange"      // Determines if the target is in melee range (0-5 yard)
```

---
### **SpellInRange requirements**

Allow requirements about spell range to be used, the spell in question depends upon the class being played.

`"SpellInRange:0"` or `"not SpellInRange:0"` for a Warrior is Charge and for a Mage is Fireball. 

This might be useful if you were close enough for a Fireball, but not for a Frostbolt.

Formula: `SpellInRange:[Numeric integer value]`

| Class | Spell | id |
| --- | --- | --- |
| ROGUE | Sinister Strike | 0 |
| ROGUE | Throw | 1 |
| ROGUE | Shoot Gun | 2 |
| DRUID | Wrath | 0 |
| DRUID | Bash | 1 |
| DRUID | Rip | 2 |
| DRUID | Maul | 3 |
| WARRIOR | Charge | 0 |
| WARRIOR | Rend | 1 |
| WARRIOR | Shoot Gun | 2 |
| PRIEST | Shadow Word: Pain | 0 |
| PRIEST | Shoot | 1 |
| PRIEST | Mind Flay | 2 |
| PRIEST | Mind Blast | 3 |
| PRIEST | Smite | 4 |
| PALADIN | Judgement | 0 |
| MAGE | Fireball | 0 |
| MAGE | Shoot| 1 |
| MAGE | Pyroblast | 2 |
| MAGE | Frostbolt | 3 |
| MAGE | Fire Blast | 4 |
| HUNTER | Raptor Strike | 0 |
| HUNTER | Auto Shot | 1 |
| HUNTER | Serpent Sting | 2 |
| WARLOCK | Shadow Bolt | 0 |
| WARLOCK | Shoot | 1 |
| SHAMAN | Lightning Bolt | 0 |
| SHAMAN | Earth Shock | 1 |

e.g.
```json
"Requirement": "SpellInRange:4"
"Requirements": ["Health%<80", "SpellInRange:2"]
```

---
### **Target Casting Spell requirement**

Combined with the `KeyAction.UseWhenTargetIsCasting` property, this requirement can limit on which enemy target spell your character will react or ignore.

Firstly `"TargetCastingSpell"` as it is without mentioning any spellID. Simply tells if the target is doing any cast at all.

Secondly can specify the following Format `"TargetCastingSpell:spellID1|spellID2|..."` which translates to "if Target is casting spellID1 OR spellID2 OR ...".

e.g. Rogue_20.json
```json
{
    "Name": "Kick",
    "UseWhenTargetIsCasting": true
},
{
    "Name": "Kick",
    "Requirement": "not TargetCastingSpell"
},
{
    "Name": "Kick",
    "UseWhenTargetIsCasting": true,
    "Requirement": "TargetCastingSpell:9053|11443"
},
{
    "Name": "Kick",
    "Requirement": "not TargetCastingSpell:11443"
}
```

# Modes

The default mode for the bot is to grind, but there are other modes. The mode is set in the root of the class file.

e.g. Rogue.json
```json
{
    "PathFilename": "Herb_EPL.json",
    "Mode": "AttendedGather",
    "GatherFindKeys":  [1,2]
}
```

The available modes are:

| Mode | Description |
| --- | --- |
| `"Grind"` | This is the default mode where the bot will pull mobs and follow a route |
| `"CorpseRun"` | This mode only has 2 goals. The "Wait" goal waits while you are alive. The "CorpseRun" will run back to your corpse when you die. This can be useful if you are farming an instance and die, the bot will run you back some or all of the way to the instance entrance. |
| `"AttendedGather"` | When this mode is active and the Gather tab in the UI is selected, it will run the path and scan the minimap for the yellow nodes which indicate a herb or mining node. When it finds a node it will stop and alert you by playing a youtube video, you will then have to manually pick the herb/mine and then start the bot again. |
| `"AttendedGrind"` | This is useful if you want to control the path the bot takes, but want it to pull and kill any targets you select. |

# User Interface

## Other devices

The user interface is shown in a browser on port **5000** [http://localhost:5000](http://localhost:5000). This allows you to view it from another device on your lan.

To access you PC port **5000** from another device, you will need to open up port **5000** in your firewall.

Control Panel\System and Security\Windows Defender Firewall - Advanced Settings

* Inbound Rules
* New Rule
* Port
* TCP
* Specific local ports: **5000**
* Allow the Connection
* Private, Public, Domain (You may get away with just Private)
* Name "Wow bot"

## Components

The UI has the following components:

### Screenshot

![Screenshot Component](https://raw.githubusercontent.com/julianperrott/WowClassicGrindBot/master/images/screenshotComponent.png)

### Player Summary

Show the player state. A hyper link to wowhead appears for the mob you are fighting so you can check out what it drops.

![Player Summary](https://raw.githubusercontent.com/julianperrott/WowClassicGrindBot/master/images/PlayerSummary.png)

### Route

This component shows:

* The main path
* The spirit healer path
* Your location
* The location of any deaths
* Pathed routes

![Route](https://raw.githubusercontent.com/julianperrott/WowClassicGrindBot/master/images/Route.png)

Pathed routes are shown in Green.

![Pathed route](https://raw.githubusercontent.com/julianperrott/WowClassicGrindBot/master/images/PathedRoute.png)

### Goals

This component contains a button to allow the bot to be enabled and disabled.

This displays the finite state machine. The state is goal which can run and has the highest priority. What determines if the goal can run are its pre-conditions such as having a target or being in combat. The executing goal is allowed to complete before the next goal is determined.

Some goals (combat,pull target) contain a list of spells which can be cast. The combat task evaluates its spells in order with the first available being cast. The goal then gives up control so that a higher priority task can take over (e.g. Healing potion).

The visualisation of the pre-conditions and spell requirements makes it easier to understand what the bot is doing and determine if the class file needs to be tweaked.

![Goals](https://raw.githubusercontent.com/julianperrott/WowClassicGrindBot/master/images/actionsComponent.png)

# Recording a Path

Various path are needed by the bot:

The path to run when grinding (PathFilename in root of class files).
```json
"PathFilename": "16_LochModan.json",
"PathThereAndBack": true,
"PathReduceSteps": false,
```

The short path to get to the vendor/repairer when there are obstacles close to them (PathFilename withing NPC task):
```json
{
    "Name": "Sell",
    "Key": "C",
    "Requirement": "BagFull",
    "PathFilename": "Tanaris_GadgetzanKrinkleGoodsteel.json",
    "Cost": 6
}
```

## Recording a new path

To record a new path place your character where the start of the path should be, then click on the 'Record Path' option on the left hand side of the bot's browser window. Then click 'Record New'.

![New Path](https://raw.githubusercontent.com/julianperrott/WowClassicGrindBot/master/images/Path_New.png)

Now walk the path the bot should take.

If you make a mistake you can remove spots by clicking on them on the list on the right. Then either enter new values for the spot or click 'Remove'.

For tricky parts you may want to record spots close together by using the 'Distance between spots' slider (Smaller number equals closer together).

Once the path is complete click 'Save'. This path will be saved with a generic filename e.g. `Path_20201108112650.json`, you will need to go into your `/Json/path` and rename it to something sensible.

![Recording Path](https://raw.githubusercontent.com/julianperrott/WowClassicGrindBot/master/images/Path_Recording.png)

## Types of paths

### There and back 

```json
"PathThereAndBack": true
```

These paths are run from one end to the other and then walked backwards back to the start. So the end does not need to be near the start.

![There and back path](https://raw.githubusercontent.com/julianperrott/WowClassicGrindBot/master/images/Path_Thereandback.png)

### Joined up

```json
"PathThereAndBack": false
```

These paths are run from one end to the other and then repeated. So the path needs to join up with itself i.e. the end needs to be near the start.

![Circular path](https://raw.githubusercontent.com/julianperrott/WowClassicGrindBot/master/images/Path_Circular.png)

## Tips  

Try to avoid the path getting too close to:

* Obstactles like trees, houses.
* Steep hills or cliffs (falling off one can make the bot get stuck).
* Camps/groups of mobs.
* Elite mob areas, or solo elite paths.

The best places to grind are:

* Places with non casters i.e. beasts. So that they come to you when agro'd.
* Places where mobs are far apart (so you don't get adds).
* Places with few obstacles.
* Flat ground.


# Pathing

Pathing is built into the bot so you don't need to do anything special except download the MPQ files. You can though run it on its own server to visualise routes as they are created by the bot, or to play with route finding.

The bot will try to calculate a path in the following situations:

* Travelling to a vendor or repair.
* Doing a corpse run.
* Resuming the grind path at startup, after killing a mob, or if the distance to the next stop in the path is not a short distance.

## Video:

[![Pathing Video Youtube](images/PathingApi.png)](https://www.youtube.com/watch?v=Oz_jFZfxSdc&t=254s&ab_channel=JulianPerrott)

## Running on its own server.

In visual studio just set PathingAPI as the startup project or from the command line:

```ps
cd C:\WowClassicGrindBot\PathingAPI
dotnet run
```

Then in a browser go to http://localhost:5001

There are 3 pages:

* Watch API Calls
* Search
* Swagger

Requests to the API can be done in a new browser tab like this or via the Swagger tab. You can then view the result in the Watch API calls viewer.

```
http://localhost:5001/api/PPather/MapRoute?map1=1446&x1=51.0&y1=29.3&map2=1446&x2=38.7&y2=20.1
```

Search gives some predefined locations to search from and to.

## Running it along side the bot

In visual studio right click the solution and set Multiple startup projects to BlazorServer and PathingApi and run.

Or from 2 command lines dotnet run each.

```ps
cd c:\WowClassicGrindBot\PathingAPI
dotnet run
```

```ps
cd c:\WowClassicGrindBot\BlazorServer
dotnet run
```

## As a library used within the bot

The bot will use the PathingAPI to work out routes, these are shown on the route map as green points.

![Pathed Route](https://raw.githubusercontent.com/julianperrott/WowClassicGrindBot/master/images/PathedRoute.png)


# Macros

Warlock `heal` macro used in warlock profiles.
```css
#showtooltip
/cast [nocombat] Create Healthstone
/use Minor Healthstone
/use Lesser Healthstone
/use Healthstone
/use Greater Healthstone
/use Major Healthstone
/use Master Healthstone
```

Hunter `feedpet` macro replace `Roasted Quail` with the proper diet
```css
#showtooltip
/cast Feed Pet
/use Roasted Quail
```

Hunter `sumpet` macro
```css
#showtooltip
/cast [target=pet,dead] Revive Pet
/cast [target=pet,noexists] Call Pet
```

Rogue weapon enchant (use 17 for second weapon):
```css
/use Instant Poison V 
/use 16
/click StaticPopup1Button1 
```

Melee weapon enchant:
```css
/use Dense Sharpening Stone
/use 16
/click StaticPopup1Button1
```