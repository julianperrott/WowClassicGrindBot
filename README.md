# MasterOfPuppets - World of Warcraft Classic Grind Bot 

Star * this repo if you like it... or might be vaguely useful !

- Uses a modified version of the addon: https://github.com/FreeHongKongMMO/Happy-Pixels to read the game state. Sorry but the addon is coded for an english wow client and would need to be modified to work with any other languages.

- Uses Blazor Server to show the bot state in a browser, and so can be monitored from your phone or a tablet while you do something else.
https://docs.microsoft.com/en-us/aspnet/core/blazor

- No DLL injection or memory watching, just screen capture, mouse and keyboard clicking.

- Written from scratch in C#.

- All classes are working except Hunter.

![Screenshot](https://raw.githubusercontent.com/julianperrott/WowClassicGrindBot/master/images/Screenshot.png)


# Getting it working

## 1. Download this repository

Put the contents of the repo into a folder. e.g "C:\WowClassicGrindBot". I am going to refer to this folder from now on, so just substitute your own folder path.

## 2. Install the Addon

In this repo is a folder called Addons e.g.  C:\WowClassicGrindBot\Addons. Copy the contents into your wow classic Addons folder. e.g. c:\World of Warcraft\_classic_\Interface\AddOns. You will need to restart Wow if it is running.

There are 2 addons:

* Bindpad - This makes it easier to bind keys to commands or macros. e.g. F1-F12
* DataToColor - This is the addon which reads and displays the game state.


## 3. Build the bot

You will probably already have Visual Studio or Visual Studio Code installed. You need to build the bot using either one of them, or use powershell.

You will need .net core 3.1 SDK installed. https://dotnet.microsoft.com/download/dotnet-core/3.1 

e.g. Build from powershell

    cd C:\WowClassicGrindBot\BlazorServer
    dotnet build

![Build](https://raw.githubusercontent.com/julianperrott/WowClassicGrindBot/master/images/build.png)

## 4. Configure the Addon Reader

The bot reads the game state using small blocks of colour shown at the top of the screen by an Addon. This needs to be configured.

1. Delete the existing config.json file found in c:\WowClassicGrindBot\BlazorServer, we are going to recreate it. This is important as your screen may not be the same size as mine 1920 x 1080.
2. Edit the batch script in c:\WowClassicGrindBot\BlazorServer called run.bat, change it to point at where you have put the repo BlazorServer folder e.g.

        start "C:\Program Files (x86)\Google\Chrome\Application\chrome.exe" "http://localhost:5000"
        c:
        cd C:\WowClassicGrindBot\BlazorServer
        dotnet run
        pause

3. Execute the run.bat. This will start the bot and Chrome, Wow must be already running. If you get "Unable to find the Wow process is it running ?" in the console window then it can't find wow.exe.

4. You should see the 'Addon configuration' screen (See screenshot below).

5. The bot needs to record the position of the blocks of colour and to do this we need to put the addon into configuration mode by typing /dc. Follow the instructions on the configuration page and Save the configuration.

6. Restart the bot and when it starts it should show the dashboard page.

![AddonConfig](https://raw.githubusercontent.com/julianperrott/WowClassicGrindBot/master/images/AddonConfig.png)

## 5. Configure the Wow Client - Interface Options

We need to make sure that certain interface options are set. The most important are Click to move and screen flashing on low health. See the list below.

### Interface Options

From the main menu (ESC) set the following:

* Interface Options - Controls - Auto Loot - Ticked.
* Interface Options - Controls - Interact on Left click - Not ticked.
* Interface Options - Combat - Do Not Flash Screen at Low Health - Ticked.
* Interface Options - Combat - Auto Self Cast - Ticked.
* Interface Options - Camera - Auto-Follow Speed - Fast
* Interface Options - Camera - Camera Following Style - Always
* Interface Options - Mouse - Click-to-Move - Ticked
* Interface Options - Mouse - Click-to-Move Camera Style - Always

## 6. Configure the Wow Client - Key Bindings:

The "Interact with Target" keybind is super important as it allows the bot to turn towards and approach the target.

From the main menu (ESC) set the following:

Key Bindings:

| Command |  Key |
| ---- | ---- |
|  Interact With Target | H  |


## 7. Configure the Wow Client - Bindpad addon

Bindpad allows keys to be easily bound to commands and macros. Type /bindpad to show it.

For each of the following click + to add a new key binding. The most important ones are marked with a *.

|  Key |  Command | Description |
| ---- | ---- | --- |
|  i |  /use hearthstone | |
|  o |  /use Chestnut Mare Bridle | Your mount here|
|  u | /tar targettarget | Warlock only |
|  y | /cast counterspell | Mage only |
|  t | /cast blink | Mage only |
|  F1 | See below | Buff weapon 16 (Melee classes) |
|  F2 | See below | Buff weapon 17 (Melee classes)|
| * F3 | /cleartarget  | |
| * F4 | /use Superior Healing Potion | Heal |
|  F5 | See below  | Delete various crap |
| F6 | /equipslot 18 Wicked Throwing Dagger | Equip thown (Rogue) |
| F7 | /cast Desperate Prayer | Heal - Priest only |
| F8 | /cancelform | Druid |
| * F9 | /stand |  |
| * F10 | /stopattack | |
| F11 | /cast Power Infusion | Priest only |
| F12 | /tar pet | Warlock only |
| L | /cast Ice Barrier | Mage only |


    Rogue weapon buff (use 17 for second weapon):
        /use Instant Poison V 
        /use 16
        /click StaticPopup1Button1 

    Melee weapon buff:
        /use Dense Sharpening Stone
        /use 16
        /click StaticPopup1Button1         

    Delete various
        /run for b=0,4 do for s=1,GetContainerNumSlots(b) do local n=GetContainerItemLink(b,s) if n and (strfind(n,"Slimy") or strfind(n,"Red Wolf") or strfind(n,"Mystery") or strfind(n,"Spider L")) then PickupContainerItem(b,s) DeleteCursorItem() end end end


## 8. Setting up the class file

Each class has a configuration file in /Json/class e.g. the config for a Rogue it is in file C:\WowClassicGrindBot\Json\class\Rogue.json.

Take a look at the other class files for examples of what you can do (BTW hunter is not supported.). Your class file probably exists and just needs to be edited to set the pathing file name.

### Path

The path that the class follows is a json file in C:\WowClassicGrindBot\Json\path\ which contains a list of x & y coordinates the bot will traverse while looking for mobs.

        "PathFilename": "58_Winterspring.2.json", // the path to walk when alive
        "SpiritPathFilename": "58_Winterspring_SpiritHealer.2.json", // the path from the spirit healer back to the main path.
        "PathThereAndBack": true, // if true walks the path and the walks it backwards.
        "PathReduceSteps": true,  // uses every other coordinate.

### Commands

The rest of the file contains a set of commands 

e.g.

    {
        "Name": "Slice And Dice",
        "Key": "3",
        "MinEnergy": 25,
        "MinComboPoints": 2,
        "Cooldown": 3,
        "Requirement": "Slice And Dice"
      }, 

Commands have the following parameters, only a subset will be used by each command.

| Property Name | Description | Default value |
| --- | --- | --- |
| Name | Name of the command | |
| HasCastBar | Does the spell have a cast bar | false |
| StopBeforeCast | Should the char stop moving before casting the spell | false |
| Key | The key to click (ConsoleKey) | |
| PressDuration | How many milliseconds to press the key for |  250 |
| ShapeShiftForm | For druids the shapeshift form to be in to cast this spell | None |
| CastIfAddsVisible | If the bot can "See" any adds | false |
| Cooldown | The cooldown in seconds until the command can be done again | 0 |
| MinMana | The minimum Mana required to cast the spell | 0 |
| MinRage | The minimum Rage required to cast the spell | 0 |
| MinEnergy | The minimum Energy required to cast the spell | 0 |
| MinComboPoints | The minimum combo points required to cast the spell | 0 |
| Requirement | A single "Requirement" (See below) which must be true | |
| Requirements | A list of "Requirements" which must be true |  |
| WaitForWithinMelleRange| Wait after casting for the mob to be in melee range | false |
| ResetOnNewTarget | Reset the cooldown if the target changes | false |
| Log | Write to the log when this key is evaluated | true |
| DelayAfterCast | The delay in milliseconds after the spell is cast | 1500 |
| DelayBeforeCast | A delay in milliseconds before this spell is cast | 0 |
| Cost | For Adhoc actions the priority | 18 |
| InCombat | Can it be cast in combat | false |

### Pull Action

This is the sequence of commands that are used when pulling a mob.

### Combat Action

The sequence of commands that are used when in combat and trying to kill a mob. The combat action does the first available command on the list. The action then runs again re-evaluating the list before choosing the first available command again, and so on until the mob is dead.

### Adhoc Actions

These commands are done when not in combat and are not on cooldown.

## Requirement

A requirement is something that must be true for the command to run. Not all commands need a requirement, some just rely on a cooldown or a mana amount. A requirement can be put into a list if there is more than one.

e.g.

      {
        "Name": "Soul Shard",
        "Key": "9",
        "HasCastBar": true,
        "Requirements": ["TargetHealth%<36", "not BagItem:6265:3"], <--- Requirement List
        "MinMana": 55
      },
      {
        "Name": "Curse of Weakness",
        "Key": "6",
        "Cooldown": 10,
        "ResetOnNewTarget": true,
        "Requirement": "not Curse of Weakness", <--- Single Requirement
        "MinMana": 20,
        "Log": false
      },

#### Value base requirements

Value base requirements are made up on a [ Health% or TargetHealth% or Mana%] [< or >] [Numeric Value].

e.g.
* "Health%>70",
* "TargetHealth%<10",
* "Mana%<40",

#### npcID requirements

If a particular npc is required then this requirement can be used.

e.g.
* "not npcID:6195", - don't cast on npcId 6195 https://classic.wowhead.com/npc=6195
* "npcID:6195", - only cast on npcId 6195 https://classic.wowhead.com/npc=6195

#### Bag requirements

If an item must be in your bag then you can use this requirement. Useful to determine when to create warlock Healthstone or soul shards.

It has the format BagItem:[itemid]:[count]

e.g. 
* "BagItem:6265:1 - Must have a soulshard in the bag https://classic.wowhead.com/item=6265
* "not BagItem:19007:1" - Must have a lesser Healthstone in the bag https://classic.wowhead.com/item=19007
* "not BagItem:6265:3"- Must not have 3 soulshards in the bag.

#### Buff / Debuff

Allow requirements about what buffs you have or the target has to be evaluated.

e.g.
* "not Well Fed" - I am not well fed.
* "not Thorns" - I don't have the thorns buff.
* "Shooting" - I am out of shooting.

| Class | Buff |
| --- | --- |
| All | "Well Fed" |
| All | "Eating" |
| All | "Drinking" |
| All | "Mana Regeneration" |
| All | "OutOfCombatRange" |
| All | "InCombatRange" |
| All | "Shooting" |
| Priest | "Fortitude" |
| Priest | "InnerFire" |
| Priest | "Divine Spirit" |
| Priest | "Renew" |
| Priest | "Shield" |
| Priest | "Shadow Word: Pain" |
| Paladin | "Seal" |
| Paladin |  "Aura" |
| Paladin |  "Blessing" |
| Druid | "Mark of the Wild" |
| Druid | "Thorns" |
| Druid Debuff | "Demoralizing Roar"
| Druid Debuff | "Faerie Fire"
| Mage | "Frost Armor" |
| Mage | "Arcane Intellect" |
| Mage | "Ice Barrier" |
| Rogue | "Slice And Dice" |
| Warrior | "Battle Shout" |
| Warlock | "Demon Skin" |
| Warlock | "Has Pet" |
| Warlock | "Curse of Weakness" |
| Warrior Debuff | "Rend" |
 
# User Interface

## Other devices

The user interface is shown in a browser on port 5000 http://localhost:5000. This allows you to view it from another device on your lan.

To access you PC port 5000 from another device, you will need to open up port 5000 in your firewall.

Control Panel\System and Security\Windows Defender Firewall - Advanced Settings

* Inbound Rules
* New Rule
* Port
* TCP
* Specific local ports: 5000
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

![Route](https://raw.githubusercontent.com/julianperrott/WowClassicGrindBot/master/images/Route.png)

### Actions

This component contains a button to allow the bot to be enabled and disabled.

This displays the finite state machine. The state is action which can run and has the highest priority. What determines if the action can run are its pre-conditions such as having a target or being in combat. The executing action is allowed to complete before the next action is determined.

Some actions (combat,pull target) contain a list of spells which can be cast. The combat task evaluates its spells in order with the first available being cast. The action then gives up control so that a higher priority task can take over (e.g. Healing potion).

The visualisation of the pre-conditions and spell requirements makes it easier to understand what the bot is doing and determine if the class file needs to be tweaked.

![Actions](https://raw.githubusercontent.com/julianperrott/WowClassicGrindBot/master/images/actionsComponent.png)