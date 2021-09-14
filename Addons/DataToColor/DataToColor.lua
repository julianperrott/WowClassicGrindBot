----------------------------------------------------------------------------
--  DataToColor - display player position as color
----------------------------------------------------------------------------

local Load = select(2, ...)
local DataToColor = unpack(Load)
local Range = DataToColor.Libs.RangeCheck

-- Trigger between emitting game data and frame location data
SETUP_SEQUENCE = false
-- Exit process trigger
EXIT_PROCESS_STATUS = 0
-- Total number of data frames generated
local NUMBER_OF_FRAMES = 100
-- Set number of pixel rows
local FRAME_ROWS = 1
-- Size of data squares in px. Varies based on rounding errors as well as dimension size. Use as a guideline, but not 100% accurate.
local CELL_SIZE = 1 -- 1-9 
-- Spacing in px between data squares.
local CELL_SPACING = 1 -- 0 or 1
-- Item slot trackers initialization
local itemNum = 0
local equipNum = 0
local actionNum = 1
local bagNum = -1
local globalCounter = 0
-- Global table of all items player has
local items = {}
local itemsPlaceholderComparison = {}
local enchantedItemsList = {}
-- How often item frames change
local ITEM_ITERATION_FRAME_CHANGE_RATE = 6
-- How often the actionbar frames change
local ACTION_BAR_ITERATION_FRAME_CHANGE_RATE = 5

local MAX_POWER_TYPE = 1000000
local MAX_ACTION_IDX = 1000

-- Action bar configuration for which spells are tracked
local MAIN_MIN = 1
local MAIN_MAX = 12
local BOTTOM_LEFT_MIN = 61
local BOTTOM_LEFT_MAX = 72

-- Timers
DataToColor.timeUpdateSec = 0.1
DataToColor.globalTime = 0
DataToColor.lastLoot = 0

DataToColor.frames = nil
DataToColor.r = 0

DataToColor.uiErrorMessage = 0
DataToColor.lastCombatDamageDealerCreature = 0
DataToColor.lastCombatCreature = 0
DataToColor.lastCombatCreatureDied = 0

-- Note: Coordinates where player is standing (max: 10, min: -10)
-- Note: Player direction is in radians (360 degrees = 2π radians)
-- Note: Player health/mana is taken out of 100% (0 - 1)

local buffList
function DataToColor:createBuffList()
    local t = {}
    t[0] = "Food"
    t[1] = "Drink"
    t[2] = "Well Fed"
    t[3] = "Mana Regeneration"

    if self.C.CHARACTER_CLASS == "PRIEST" then
        t[10] = "Fortitude"
        t[11] = "Inner Fire"
        t[12] = "Renew"
        t[13] = "Shield"
        t[14] = "Spirit"
    elseif self.C.CHARACTER_CLASS == "DRUID" then
        t[10] = "Mark of the Wild"
        t[11] = "Thorns"
        t[12] = "Fury"
    elseif self.C.CHARACTER_CLASS == "PALADIN" then
        t[10] = "Aura"
        t[11] = "Blessing"
        t[12] = "Seal"
    elseif self.C.CHARACTER_CLASS == "MAGE" then
        t[10] = "Armor"
        t[11] = "Arcane Intellect"
        t[12] = "Ice Barrier"
        t[13] = "Ward"
        t[14] = "Fire Power"
    elseif self.C.CHARACTER_CLASS == "ROGUE" then
        t[10] = "Slice and Dice"
    elseif self.C.CHARACTER_CLASS == "WARRIOR" then
        t[10] = "Battle Shout"
    elseif self.C.CHARACTER_CLASS == "WARLOCK" then
        t[10] = "Demon"
        t[11] = "Soul Link"
        t[12] = "Soulstone Resurrection"
        t[13] = "Shadow Trance"
    elseif self.C.CHARACTER_CLASS == "SHAMAN" then
        t[10] = "Lightning Shield"
    elseif self.C.CHARACTER_CLASS == "HUNTER" then
        t[10] = "Aspect of"
        t[11] = "Rapid Fire"
        t[12] = "Quick Shots"
    end
    return t
end

local debuffList
function DataToColor:createDebuffTargetList()
    local t = {}
    if self.C.CHARACTER_CLASS == "PRIEST" then 
        t[0] = "Pain"
    elseif self.C.CHARACTER_CLASS == "DRUID" then
        t[0] = "Roar"
        t[1] = "Faerie Fire"
        t[2] = "Rip"
    elseif self.C.CHARACTER_CLASS == "PALADIN" then
    elseif self.C.CHARACTER_CLASS == "MAGE" then
        t[0] = "Frostbite"
    elseif self.C.CHARACTER_CLASS == "ROGUE" then
    elseif self.C.CHARACTER_CLASS == "WARRIOR" then
        t[0] = "Rend"
    elseif self.C.CHARACTER_CLASS == "WARLOCK" then
        t[0] = "Curse of"
        t[1] = "Corruption"
        t[2] = "Immolate"
        t[3] = "Siphon Life"
    elseif self.C.CHARACTER_CLASS == "HUNTER" then
        t[0] = "Serpect Sting"
    end
    return t
end

local spellInRangeList
function DataToColor:createSpellInrangeList()
    if self.C.CHARACTER_CLASS == "ROGUE" then
        spellInRangeList = {
            "Sinister Strike", --1
            "Throw", --2
            "Shoot Gun" --4
        };
    elseif self.C.CHARACTER_CLASS == "DRUID" then
        spellInRangeList = {
            "Wrath", --1
            "Bash", --2
            "Rip" --3
        };
    elseif self.C.CHARACTER_CLASS == "WARRIOR" then
        spellInRangeList = {
            "Charge", --1
            "Rend", --2
            "Shoot Gun", --4
        };        
    elseif self.C.CHARACTER_CLASS == "PRIEST" then
        spellInRangeList = {
            "Shadow Word: Pain", --1
            "Mind Blast", --2
            "Mind Flay", --4
            "Shoot", --8
        };
    elseif self.C.CHARACTER_CLASS == "PALADIN" then
        spellInRangeList = {
            "Judgement" --1
        };
    elseif self.C.CHARACTER_CLASS == "MAGE" then
        spellInRangeList = {
            "Fireball", --1
            "Shoot",
            "Pyroblast",
            "Frostbolt",
            "Fire Blast"
        };        
    elseif self.C.CHARACTER_CLASS == "HUNTER" then
        spellInRangeList = {
            "Raptor Strike", --1
            "Auto Shot", --2
            "Serpent Sting" --3
        };        
    elseif self.C.CHARACTER_CLASS == "WARLOCK" then
        spellInRangeList = {
            "Shadow Bolt",
            "Shoot"
        };
    elseif self.C.CHARACTER_CLASS == "SHAMAN" then
        spellInRangeList = {
            "Lightning Bolt",
            "Earth Shock"
        }
    end
end


function DataToColor:RegisterSlashCommands()
    self:RegisterChatCommand('dc', 'StartSetup')
    self:RegisterChatCommand('dccpu', 'GetCPUImpact')
end

function DataToColor:StartSetup()
    if not SETUP_SEQUENCE then
        SETUP_SEQUENCE = true
    else
        SETUP_SEQUENCE = false
    end
end

function DataToColor:Print(...)
	(_G.DEFAULT_CHAT_FRAME):AddMessage(strjoin('', '|cff00b3ff', 'DataToColor:|r ', ...)) -- I put DEFAULT_CHAT_FRAME as a fail safe.
end

function DataToColor:error(msg)
    self:log("|cff0000ff" .. msg .. "|r")
    self:log(msg)
    self:log(debugstack())
    error(msg)
end

-- Check if two tables are identical
function ValuesAreEqual(t1, t2, ignore_mt)
    local ty1 = type(t1)
    local ty2 = type(t2)
    if ty1 ~= ty2 then return false end
    -- non-table types can be directly compared
    if ty1 ~= 'table' and ty2 ~= 'table' then return t1 == t2 end
    -- as well as tables which have the metamethod __eq
    local mt = getmetatable(t1)
    if not ignore_mt and mt and mt.__eq then return t1 == t2 end
    for k1, v1 in pairs(t1) do
        local v2 = t2[k1]
        if v2 == nil or not ValuesAreEqual(v1, v2) then return false end
    end
    for k2, v2 in pairs(t2) do
        local v1 = t1[k2]
        if v1 == nil or not ValuesAreEqual(v1, v2) then return false end
    end
    return true
end

-- Discover player's direction in radians (360 degrees = 2π radians)
function DataToColor:GetPlayerFacing()
    --local p = Minimap
    --local m = ({p:GetChildren()})[9]
    local facing = GetPlayerFacing() or 0

    if facing ~= nil then
        return facing
    else
        return 0
    end
end

-- This function runs when addon is initialized/player logs in
-- Decides length of white box
function DataToColor:OnInitialize()
    self:SetupRequirements()
    self:CreateFrames(NUMBER_OF_FRAMES)
    self:RegisterSlashCommands()

    -- handle error events
    UIErrorsFrame:UnregisterEvent("UI_ERROR_MESSAGE")

    self:RegisterEvent("UI_ERROR_MESSAGE", 'OnUIErrorMessage')
    self:RegisterEvent("COMBAT_LOG_EVENT_UNFILTERED", 'OnCombatEvent')
    self:RegisterEvent('LOOT_CLOSED','OnLootClosed')
    self:RegisterEvent('MERCHANT_SHOW','OnMerchantShow')

    buffList = self:createBuffList()
    debuffList = self:createDebuffTargetList()
    self:createSpellInrangeList();

    --LoggingChat(1);
    self:Update()
    self:Print("We're in")
end

function DataToColor:SetupRequirements()
    SetCVar("autoInteract", 1);
    SetCVar("autoLootDefault", 1)
    -- /run SetCVar("cameraSmoothStyle", 2) --always
	SetCVar('Contrast',50,'[]')
	SetCVar('Brightness',50,'[]')
	SetCVar('Gamma',1,'[]')
end

local UpdateFuncCache={};
function DataToColor:Update()
	self.globalTime = self.globalTime + 1
    if self.globalTime > (256 * 256 * 256 - 1) then
        self.globalTime = 0
    end

    --self:Print(self.globalTime)
 
    local func = UpdateFuncCache[self]
    if not func then
        func = function() self:Update(); end;
        UpdateFuncCache[self] = func;
    end
    C_Timer.After(self.timeUpdateSec, func);
end

-- Function to mass generate all of the initial frames for the pixel reader
function DataToColor:CreateFrames(n)
    -- Note: Use single frame and update color on game update call
    local function UpdateFrameColor(f)
        -- set the frame color to random values
        xCoordi, yCoordi = self:GetCurrentPlayerPosition()
        if xCoordi == nil or yCoordi == nil then
            xCoordi = 0
            yCoordi = 0
        end
        -- Makes a 5px by 5px square. Might be 6x5 or 6x5.
        -- This is APPROXIMATE MATH. startingFrame is the x start, startingFramey is the "y" start (both starts are in regard to pixel position on the main frame)
        function MakePixelSquareArr(col, slot)
            --if type(slot) ~= "number" or slot < 0 or slot >= NUMBER_OF_FRAMES then
            --    self:error("Invalid slot value")
            --end
            
            --if type(col) ~= "table" then
            --    self:error("Invalid color value (" .. tostring(col) .. ")")
            --end
            
            self.frames[slot + 1]:SetBackdropColor(col[1], col[2], col[3], 1)
        end
        -- Number of loops is based on the number of generated frames declared at beginning of script
        
        if not SETUP_SEQUENCE then
            MakePixelSquareArr(self:integerToColor(0), 0)
            -- The final data square, reserved for additional metadata.
            MakePixelSquareArr(self:integerToColor(2000001), NUMBER_OF_FRAMES - 1)
            -- Position related variables --
            MakePixelSquareArr(self:fixedDecimalToColor(xCoordi), 1) --1 The x-coordinate
            MakePixelSquareArr(self:fixedDecimalToColor(yCoordi), 2) --2 The y-coordinate
            MakePixelSquareArr(self:fixedDecimalToColor(DataToColor:GetPlayerFacing()), 3) --3 The direction the player is facing in radians
            MakePixelSquareArr(self:integerToColor(self:GetZoneName(0)), 4) -- Get name of first 3 characters of zone
            MakePixelSquareArr(self:integerToColor(self:GetZoneName(3)), 5) -- Get name of last 3 characters of zone
            MakePixelSquareArr(self:fixedDecimalToColor(self:CorpsePosition("x") * 10), 6) -- Returns the x coordinates of corpse
            MakePixelSquareArr(self:fixedDecimalToColor(self:CorpsePosition("y") * 10), 7) -- Return y coordinates of corpse
            -- Boolean variables --
            MakePixelSquareArr(self:integerToColor(self:Base2Converter()), 8)
            -- Start combat/NPC related variables --
            MakePixelSquareArr(self:integerToColor(self:getHealthMax(self.C.unitPlayer)), 10) --8 Represents maximum amount of health
            MakePixelSquareArr(self:integerToColor(self:getHealthCurrent(self.C.unitPlayer)), 11) --9 Represents current amount of health
            MakePixelSquareArr(self:integerToColor(self:getManaMax(self.C.unitPlayer)), 12) --10 Represents maximum amount of mana
            MakePixelSquareArr(self:integerToColor(self:getManaCurrent(self.C.unitPlayer)), 13) --11 Represents current amount of mana
            MakePixelSquareArr(self:integerToColor(self:getPlayerLevel()), 14) --12 Represents character level
            MakePixelSquareArr(self:integerToColor(self:getRange()), 15) -- 15 Represents if target is within 0-5 5-15 15-20, 20-30, 30-35, or greater than 35 yards
            MakePixelSquareArr(self:integerToColor(self:GetTargetName(0)), 16) -- Characters 1-3 of target's name
            MakePixelSquareArr(self:integerToColor(self:GetTargetName(3)), 17) -- Characters 4-6 of target's name
            MakePixelSquareArr(self:integerToColor(self:getHealthMax(self.C.unitTarget)), 18) -- Return the maximum amount of health a target can have
            MakePixelSquareArr(self:integerToColor(self:getHealthCurrent(self.C.unitTarget)), 19) -- Returns the current amount of health the target currently has
            -- Begin Items section --
            -- there are 5 item slots: main backpack and 4 pouches
            -- Indexes one slot from each bag each frame. SlotN (1-16) and bag (0-4) calculated here:
            if self:Modulo(globalCounter, ITEM_ITERATION_FRAME_CHANGE_RATE) == 0 then
                itemNum = itemNum + 1
                equipNum = equipNum + 1
                bagNum = bagNum + 1

                if itemNum >= 21 then
                    itemNum = 1
                end
                if bagNum >= 5 then
                    bagNum = 0
                end

                -- Worn inventory start.
                -- Starts at beginning once we have looked at all desired slots.
                if equipNum > 24 then
                    equipNum = 1
                end

                -- Reseting global counter to prevent integer overflow
                if globalCounter > 10000 then
                    globalCounter = 1000
                end
            end
            if self:Modulo(globalCounter, ACTION_BAR_ITERATION_FRAME_CHANGE_RATE) == 0 then
                actionNum = actionNum + 1
                if actionNum >= 84 then
                    actionNum = 1
                end
            end
            -- Controls rate at which item frames change.
            globalCounter = globalCounter + 1

            -- Bag contents - Uses data pixel positions 20-29
            for bagNo = 0, 4 do
                -- Returns item ID and quantity
                MakePixelSquareArr(self:integerToColor(self:itemName(bagNo, itemNum)), 20 + bagNo * 2) -- 20,22,24,26,28
                -- Return item slot number
                MakePixelSquareArr(self:integerToColor(bagNo * 20 + itemNum), 21 + bagNo * 2) -- 21,23,25,27,29
                MakePixelSquareArr(self:integerToColor(self:itemInfo(bagNo, itemNum)), 60 + bagNo ) -- 60,61,62,63,64
            end

            local equipName = self:equipName(equipNum)
            -- Equipment ID
            MakePixelSquareArr(self:integerToColor(equipName), 30)
            -- Equipment slot
            MakePixelSquareArr(self:integerToColor(equipNum), 31)
            
            -- Amount of money in coppers
            MakePixelSquareArr(self:integerToColor(self:Modulo(self:getMoneyTotal(), 1000000)), 32) -- 13 Represents amount of money held (in copper)
            MakePixelSquareArr(self:integerToColor(floor(self:getMoneyTotal() / 1000000)), 33) -- 14 Represents amount of money held (in gold)
           
            -- Start main action page (page 1)
            MakePixelSquareArr(self:integerToColor(self:isActionUseable(1,24)), 34) 
            MakePixelSquareArr(self:integerToColor(self:isActionUseable(25,48)), 35) 
            MakePixelSquareArr(self:integerToColor(self:isActionUseable(49,72)), 36) 
            MakePixelSquareArr(self:integerToColor(self:isActionUseable(73,96)), 42) 

            local freeSlots, bagType = GetContainerNumFreeSlots(bagNum)
            if bagType == nil then
                bagType = 0
            end
            MakePixelSquareArr(self:integerToColor(bagType * 1000000 + bagNum * 100000 + freeSlots * 1000 + self:bagSlots(bagNum)), 37) -- BagType + Index + FreeSpace + BagSlots


            MakePixelSquareArr(self:integerToColor(self:getHealthMax(self.C.unitPet)), 38)
            MakePixelSquareArr(self:integerToColor(self:getHealthCurrent(self.C.unitPet)), 39)
            -- 40

            -- Profession levels:
            -- tracks our skinning level
            --MakePixelSquareArr(self:integerToColor(self:GetProfessionLevel("Skinning")), 41) -- Skinning profession level
            -- tracks our fishing level
            --MakePixelSquareArr(self:integerToColor(self:GetProfessionLevel("Fishing")), 42) -- Fishing profession level
            MakePixelSquareArr(self:integerToColor(self:getAuraMaskForClass(UnitBuff, self.C.unitPlayer, buffList)), 41);
            -- 42 used by keys
            
            MakePixelSquareArr(self:integerToColor(self:getTargetLevel()), 43)

            MakePixelSquareArr(self:integerToColor(DataToColor:actionbarCost(actionNum)), 44)
            --MakePixelSquareArr(self:integerToColor(self:GetGossipIcons()), 45) -- Returns which gossip icons are on display in dialogue box

            MakePixelSquareArr(self:integerToColor(self:PlayerClass()), 46) -- Returns player class as an integer
            MakePixelSquareArr(self:integerToColor(self:isUnskinnable()), 47) -- Returns 1 if creature is unskinnable
            MakePixelSquareArr(self:integerToColor(self:shapeshiftForm()), 48) -- Shapeshift id https://wowwiki.fandom.com/wiki/API_GetShapeshiftForm
            MakePixelSquareArr(self:integerToColor(self:areSpellsInRange()), 49) -- Are spells in range

            MakePixelSquareArr(self:integerToColor(self:getUnitXP(self.C.unitPlayer)), 50) -- Player Xp
            MakePixelSquareArr(self:integerToColor(self:getUnitXPMax(self.C.unitPlayer)), 51) -- Player Level Xp
            MakePixelSquareArr(self:integerToColor(self.uiErrorMessage), 52) -- Last UI Error message
            self.uiErrorMessage=0;

            MakePixelSquareArr(self:integerToColor(DataToColor:CastingInfoSpellId()), 53) -- Spell being cast
            MakePixelSquareArr(self:integerToColor(DataToColor:ComboPoints()), 54) -- Combo points for rogue / druid
            MakePixelSquareArr(self:integerToColor(self:getAuraMaskForClass(UnitDebuff, self.C.unitTarget, debuffList)), 55); -- target debuffs

            MakePixelSquareArr(self:integerToColor(DataToColor:targetNpcId()), 56) -- target id
            MakePixelSquareArr(self:integerToColor(DataToColor:getGuid(self.C.unitTarget)),57) -- target reasonably uniqueId
            MakePixelSquareArr(self:integerToColor(DataToColor:GetBestMap()),58) -- MapId

            MakePixelSquareArr(self:integerToColor(DataToColor:IsTargetOfTargetPlayerAsNumber()),59) -- IsTargetOfTargetPlayerAsNumber
            -- 60-64 = Bag item info
            MakePixelSquareArr(self:integerToColor(self.lastCombatCreature),65) -- Combat message creature
            MakePixelSquareArr(self:integerToColor(self.lastCombatDamageDealerCreature),66) -- Combat message last damage dealer creature
            MakePixelSquareArr(self:integerToColor(self.lastCombatCreatureDied),67) -- Last Killed Unit

            MakePixelSquareArr(self:integerToColor(DataToColor:getGuid(self.C.unitPet)),68) -- pet guid
            MakePixelSquareArr(self:integerToColor(DataToColor:getGuid(self.C.unitPetTarget)),69) -- pet target

            -- Timers
            MakePixelSquareArr(self:integerToColor(self.globalTime), 70)
            MakePixelSquareArr(self:integerToColor(self.lastLoot), 71)

            self:HandlePlayerInteractionEvents()
        end

        if SETUP_SEQUENCE then
            -- Emits meta data in data square index 0 concerning our estimated cell size, number of rows, and the numbers of frames
            MakePixelSquareArr(self:integerToColor(CELL_SPACING * 10000000 + CELL_SIZE * 100000 + 1000 * FRAME_ROWS + NUMBER_OF_FRAMES), 0)
            -- Assign pixel squares a value equivalent to their respective indices.
            for i = 1, NUMBER_OF_FRAMES - 1 do
                MakePixelSquareArr(self:integerToColor(i), i)
            end
        end
        -- Note: Use this area to set color for individual pixel frames
        -- Cont: For example self.frames[0] = playerXCoordinate while self.frames[1] refers to playerXCoordinate
    end
    -- Function used to generate a single frame
    local function setFramePixelBackdrop(f)
        f:SetBackdrop({
            bgFile = "Interface\\AddOns\\DataToColor\\white.tga",
            insets = {top = 0, left = 0, bottom = 0, right = 0},
        })
    end
    
    local function genFrame(name, x, y)
        local f = CreateFrame("Frame", name, UIParent, BackdropTemplateMixin and "BackdropTemplate") or CreateFrame("Frame", name, UIParent)
        f:SetPoint("TOPLEFT", x * (CELL_SIZE + CELL_SPACING), -y * (CELL_SIZE + CELL_SPACING))
        f:SetHeight(CELL_SIZE)
        f:SetWidth(CELL_SIZE) -- Change this to make white box wider
        setFramePixelBackdrop(f)
        f:SetFrameStrata("DIALOG")
        f:SetBackdropColor(0, 0, 0, 1)
        return f
    end
    
    n = n or 0
    
    local frame = 1 -- try 1
    local frames = {}
    
    -- background frame
    local backgroundframe = genFrame("frame_0", 0, 0)
    backgroundframe:SetHeight(FRAME_ROWS * (CELL_SIZE + CELL_SPACING))
    backgroundframe:SetWidth(floor(n / FRAME_ROWS) * (CELL_SIZE + CELL_SPACING))
    backgroundframe:SetFrameStrata("HIGH")
    backgroundframe:SetBackdropColor(0, 0, 0, 1)
    
    --local windowCheckFrame = CreateFrame("Frame", "frame_windowcheck", UIParent)
    --windowCheckFrame:SetPoint("TOPLEFT", 120, -200)
    --windowCheckFrame:SetHeight(5)
    --windowCheckFrame:SetWidth(5)
    --windowCheckFrame:SetFrameStrata("LOW")
    --setFramePixelBackdrop(windowCheckFrame)
    --windowCheckFrame:SetBackdropColor(0.5, 0.1, 0.8, 1)
    
    -- creating a new frame to check for open BOE and BOP windows
    --local bindingCheckFrame = CreateFrame("Frame", "frame_bindingcheck", UIParent)
    -- 90 and 200 are the x and y offsets from the default "CENTER" position
    --bindingCheckFrame:SetPoint("CENTER", 90, 200)
    --bindingCheckFrame:SetHeight(5)
    --bindingCheckFrame:SetWidth(5)
    --bindingCheckFrame:SetFrameStrata("LOW")
    --setFramePixelBackdrop(bindingCheckFrame)
    --bindingCheckFrame:SetBackdropColor(0.5, 0.1, 0.8, 1)
    
    -- Note: Use for loop based on input to generate "n" number of frames
    for frame = 0, n - 1 do
        local y = self:Modulo(frame, FRAME_ROWS) -- those are grid coordinates (1,2,3,4 by  1,2,3,4 etc), not pixel coordinates
        local x = floor(frame / FRAME_ROWS)
        -- Put frame information in to an object/array
        frames[frame + 1] = genFrame("frame_"..tostring(frame), x, y)
    end
    
    -- Assign self.frames to frame list generated above
    self.frames = frames
    self.frames[1]:SetScript("OnUpdate", function() UpdateFrameColor(f) end)
end

-- Use Astrolabe function to get current player position
function DataToColor:GetCurrentPlayerPosition()
    local map = C_Map.GetBestMapForUnit(self.C.unitPlayer)
    if map ~= nil then
        local position = C_Map.GetPlayerMapPosition(map, self.C.unitPlayer)
        -- Resets map to correct zone ... removed in 8.0.1, needs to be tested to see if zone auto update
        -- SetMapToCurrentZone()
        return position:GetXY()
    else
        return;
    end
end

-- Base 2 converter for up to 24 boolean values to a single pixel square.
function DataToColor:Base2Converter()
    -- 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384
    return self:MakeIndexBase2(self:targetCombatStatus(), 0) + 
    self:MakeIndexBase2(self:GetEnemyStatus(), 1) + 
    self:MakeIndexBase2(self:deadOrAlive(), 2) +
    self:MakeIndexBase2(self:checkTalentPoints(), 3) + 
    self:MakeIndexBase2(self:isTradeRange(), 4) + 
    self:MakeIndexBase2(self:targetHostile(), 5) +
    self:MakeIndexBase2(self:IsPetVisible(), 6) + 
    self:MakeIndexBase2(self:mainhandEnchantActive(), 7) + 
    self:MakeIndexBase2(self:offhandEnchantActive(), 8) +
    self:MakeIndexBase2(self:GetInventoryBroken(), 9) + 
    self:MakeIndexBase2(self:IsPlayerFlying(), 10) + 
    self:MakeIndexBase2(self:IsPlayerSwimming(), 11) +
    self:MakeIndexBase2(self:petHappy(), 12) + 
    self:MakeIndexBase2(self:hasAmmo(), 13) + 
    self:MakeIndexBase2(self:playerCombatStatus(), 14) +
    self:MakeIndexBase2(self:IsTargetOfTargetPlayer(), 15) + 
    self:MakeIndexBase2(self:IsAutoRepeatSpellOn("Auto Shot"), 16) + 
    self:MakeIndexBase2(self:ProcessExitStatus(), 17)+
    self:MakeIndexBase2(self:IsPlayerMounted(), 18)+
    self:MakeIndexBase2(self:IsAutoRepeatSpellOn("Shoot"), 19)+
    self:MakeIndexBase2(self:IsCurrentSpell(6603), 20)+ -- AutoAttack enabled
    self:MakeIndexBase2(self:targetIsNormal(), 21)+
    self:MakeIndexBase2(self:IsTagged(), 22);
end

function DataToColor:getAuraMaskForClass(func, unitId, table)
    local num = 0
    for k, v in pairs(table) do
        for i = 1, 10 do
            local b = func(unitId, i)
            if b == nil then
                break
            end
            if string.find(b, v) then
                num = num + self:MakeIndexBase2(1, k)
                break
            end
        end
    end
    return num
end

function DataToColor:delete(items)
    for b=0,4 do for s=1,GetContainerNumSlots(b) 
        do local n=GetContainerItemLink(b,s) 
            if n then
                for i = 1, table.getn(items), 1 do
                    if strfind(n,items[i]) then
                        DataToColor:Print("Delete: " .. items[i]);
                        PickupContainerItem(b,s);
                        DeleteCursorItem();
                    end
                end
            end
        end
    end
end

function DataToColor:sell(items)

    local target = GetUnitName(self.C.unitTarget)
    if target ~= nil then
        local item= GetMerchantItemLink(1);

        if  item ~= nil then
            DataToColor:Print("Selling items...");

            DataToColor:OnMerchantShow();

            TotalPrice = 0

            for b=0,4 do for s=1,GetContainerNumSlots(b) 
                do local CurrentItemLink=GetContainerItemLink(b,s) 
                    if CurrentItemLink then
                        for i = 1, table.getn(items), 1 do
                            if strfind(CurrentItemLink,items[i]) then
                                _, _, itemRarity, _, _, _, _, _, _, _, itemSellPrice = GetItemInfo(CurrentItemLink);
                                if (itemRarity<2) then
                                    _, itemCount = GetContainerItemInfo(b, s);
                                    TotalPrice = TotalPrice + (itemSellPrice * itemCount);
                                    DataToColor:Print("Selling: "..itemCount.." "..CurrentItemLink.." for "..GetCoinTextureString(itemSellPrice * itemCount));
                                    UseContainerItem(b,s);
                                else
                                    DataToColor:Print("Item is not gray or common, not selling it: " .. items[i]);
                                end
                            end
                        end
                    end
                end
            end

            if TotalPrice ~= 0 then
                DataToColor:Print("Total Price for all items: " .. GetCoinTextureString(TotalPrice))
            else
                DataToColor:Print("No grey items were sold.")
            end

        else
            DataToColor:Print("Merchant is not open to sell to, please approach and open.");
        end
    else
        DataToColor:Print("Merchant is not targetted.");
    end
end

-- Grabs current target's name (friend or foe)
function DataToColor:GetTargetName(partition)
    -- Uses wow function to get target string
    local target = GetUnitName(self.C.unitTarget)
    if target ~= nil then
        target = self:StringToASCIIHex(target)
        if partition < 3 then
            return tonumber(string.sub(target, 0, 6))
        else if target > 999999 then
                return tonumber(string.sub(target, 7, 12))
            end
        end
        return 0
    end
    return 0
end

function DataToColor:CastingInfoSpellId()
    local name, text, texture, startTime, endTime, isTradeSkill, castID, notInterruptible, spellID = CastingInfo();
    if spellID ~= nil then
        return spellID
    end
    if texture ~= nil then -- temp fix for tbc
        return texture
    end
     _, _, texture, _, _, _, _, spellID = ChannelInfo();
     if spellID ~= nil then
        return spellID
    end
    if texture ~= nil then -- temp fix for tbc
        return texture
    end
    return 0
end

function DataToColor:getUnitXP(unit)
    return UnitXP(unit)
end

function DataToColor:getUnitXPMax(unit)
    return UnitXPMax(unit)
end

-- Finds maximum amount of health player can have
function DataToColor:getHealthMax(unit)
    return UnitHealthMax(unit)
end
-- Finds axact amount of health player current has
function DataToColor:getHealthCurrent(unit)
    return UnitHealth(unit)
end

-- Finds maximum amount of mana a character can store
function DataToColor:getManaMax(unit)
    return UnitPowerMax(unit)
end

-- Finds exact amount of mana player is storing
function DataToColor:getManaCurrent(unit)
    return UnitPower(unit)
end

-- Finds player current level
function DataToColor:getPlayerLevel()
    return UnitLevel(self.C.unitPlayer)
end

function DataToColor:getTargetLevel()
    return UnitLevel(self.C.unitTarget)
end

-- Finds the total amount of money.
function DataToColor:getMoneyTotal()
    return GetMoney()
end

function DataToColor:targetHostile()
    local hostile = UnitReaction(self.C.unitPlayer, self.C.unitTarget)
    if hostile ~= nil and hostile <= 4 then
        return 1
    end
    return 0
end

function DataToColor:hasAmmo()
    local ammoSlot = GetInventorySlotInfo("AmmoSlot");
    local ammoCount = GetInventoryItemCount(self.C.unitPlayer, ammoSlot);
    if ammoCount > 0 then
        return 1
    end
    return 0;
end

function DataToColor:getRange()
    if UnitExists(self.C.unitTarget) then
        local min, max = Range:GetRange(self.C.unitTarget)
        if max == nil then
            max = 99
        end
        return min * 100000 + max * 100
    end
    return 0
end

function DataToColor:isTradeRange()
    if UnitExists(self.C.unitTarget) then
        local tradeRange = CheckInteractDistance(self.C.unitTarget, 2)
        if tradeRange then
            return 1
        end
    end
    return 0
end

function DataToColor:targetNpcId()
    local unitType, _, _, _, _, npcID, guid = strsplit('-', UnitGUID(self.C.unitTarget) or ''); 
    if npcID ~= nil then
        return tonumber(npcID);
    end
    return 0;
end

function DataToColor:getGuid(src)
    local unitType, _, _, _, _, npcID, spawnUID = strsplit('-', UnitGUID(src) or ''); 
    if npcID ~= nil then
        return self:uniqueGuid(npcID, spawnUID);
    end
    return 0;
end

function DataToColor:getGuidFromUUID(uuid)
    local unitType, _, _, _, _, npcID, spawnUID = strsplit('-', uuid or ''); 
    return self:uniqueGuid(npcID, spawnUID);
end

function DataToColor:uniqueGuid(npcId, spawn)
    local spawnEpochOffset = bit.band(tonumber(string.sub(spawn, 5), 16), 0x7fffff)
    local spawnIndex = bit.band(tonumber(string.sub(spawn, 1, 5), 16), 0xffff8)

    local dd = date("*t", spawnEpochOffset)
    local num = 
    self:sum24(
        dd.day +
        dd.hour +
        dd.min +
        dd.sec +
        npcId +
        spawnIndex
    );
    return tonumber(num, 16);
end

function DataToColor:actionbarCost(slot)
    if HasAction(slot) then
        local actionName, _
        local actionType, id = GetActionInfo(slot)
        if actionType == 'macro' then _, _ , id = GetMacroSpell(id) end
        if actionType == 'item' then
            actionName = GetItemInfo(id)
        elseif actionType == 'spell' or (actionType == 'macro' and id) then
            actionName = GetSpellInfo(id)
        end
        if actionName then
            local cost = 0
            local type = 0
            local costTable = GetSpellPowerCost(actionName)
            if costTable ~= nil then
                for key, costInfo in pairs(costTable) do
                    cost = costInfo.cost
                    type = costInfo.type
                    break
                end
            end
            --DataToColor:Print(button:GetName(), actionType, (GetSpellLink(id)), actionName, type, cost, id)
            return MAX_POWER_TYPE * type + MAX_ACTION_IDX * slot + cost
        end
    end
    return MAX_ACTION_IDX * slot
end

-- A function used to check which items we have.
-- Find item IDs on wowhead in the url: e.g: http://www.wowhead.com/item=5571/small-black-pouch. Best to confirm ingame if possible, though.
function DataToColor:itemName(bag, slot)
    local item
    local itemCount
    _, itemCount, _, _, _, _, _ = GetContainerItemInfo(bag, slot)
    -- If no item in the slot, returns nil. We assign this as zero for sake of pixel reading.
    if GetContainerItemLink(bag, slot) == nil then
        item = 0
        -- Formatting to isolate the ID in the ItemLink
    else _, _, item = string.find(GetContainerItemLink(bag, slot), "(m:%d+)")
        item = string.gsub(item, 'm:', '')
    end
    if item == nil then item = 0
    end
    if(itemCount ~= nil and itemCount > 0) then 
        if (itemCount>100) then itemCount=100 end
        item = item + itemCount * 100000
    end
    -- Sets global variable to current list of items
    items[(bag * 16) + slot] = item
    return tonumber(item)
end

function DataToColor:itemInfo(bag, slot)
    local itemCount;
    _, itemCount, _, _, _, _, _ = GetContainerItemInfo(bag, slot);
    local value=0;
    if itemCount ~= nil and itemCount > 0 then 
        local isSoulBound = C_Item.IsBound(ItemLocation:CreateFromBagAndSlot(bag,slot));
        if isSoulBound == true then value=1 end
    else
        value=2;
    end
    return value;
end

-- Returns item id from specific index in global items table
function DataToColor:returnItemFromIndex(index)
    return items[index]
end

function DataToColor:enchantedItems()
    if ValuesAreEqual(items, itemsPlaceholderComparison) then
    end
end

function DataToColor:mainhandEnchantActive() 
    local hasMainHandEnchant = GetWeaponEnchantInfo()
    if hasMainHandEnchant then
        return 1
    end
    return 0
end

function DataToColor:offhandEnchantActive() 
    local _, _, _, _, hasOffHandEnchant = GetWeaponEnchantInfo()
    if hasOffHandEnchant then
        return 1
    end
    return 0
end

function DataToColor:equipName(slot)
    local equip
    if GetInventoryItemLink(self.C.unitPlayer, slot) == nil then
        equip = 0
    else _, _, equip = string.find(GetInventoryItemLink(self.C.unitPlayer, slot), "(m:%d+)")
        equip = string.gsub(equip, 'm:', '')
    end
    if equip == nil then equip = 0
    end
    return tonumber(equip)
end
-- -- Function to tell if a spell is on cooldown and if the specified slot has a spell assigned to it
-- -- Slot ID information can be found on WoW Wiki. Slots we are using: 1-12 (main action bar), Bottom Right Action Bar maybe(49-60), and  Bottom Left (61-72)

function DataToColor:areSpellsInRange()
    local inRange = 0
    for i = 1, table.getn(spellInRangeList), 1 do
        local isInRange = IsSpellInRange(spellInRangeList[i], self.C.unitTarget);
        if isInRange==1 then
            inRange = inRange + (2 ^ (i - 1))
        end
    end
    return inRange;
end

function DataToColor:isActionUseable(min,max)
    local isUsableBits = 0
    -- Loops through main action bar slots 1-12
    local start, isUsable, notEnough
    for i = min, max do
        start = GetActionCooldown(i)
        isUsable, notEnough = IsUsableAction(i)
        if start == 0 and isUsable == true and notEnough == false then
            isUsableBits = isUsableBits + (2 ^ (i - min))
        end
    end
    return isUsableBits
end

-- Function to tell how many bag slots we have in each bag
function DataToColor:bagSlots(bag)
    return GetContainerNumSlots(bag)
end

-- Finds passed in string to return profession level
function DataToColor:GetProfessionLevel(skill)
    local numskills = GetNumSkillLines();
    for c = 1, numskills do
        local skillname, _, _, skillrank = GetSkillLineInfo(c);
        if(skillname == skill) then
            return tonumber(skillrank);
        end
    end
    return 0;
end

-- Returns zone name
function DataToColor:GetZoneName(partition)
    local zone = self:StringToASCIIHex(GetZoneText())
    if zone and tonumber(string.sub(zone, 7, 12)) ~= nil then
        -- Returns first 3 characters of zone
        if partition < 3 then
            return tonumber(string.sub(zone, 0, 6))
            -- Returns characters 4-6 of zone
        elseif partition >= 3 then
            return tonumber(string.sub(zone, 7, 12))
        end
    end
    -- Fail safe to prevent nil
    return 1
end

function DataToColor:GetBestMap()
    local map = C_Map.GetBestMapForUnit(self.C.unitPlayer)
    if map ~= nil then
        return map
    else
        return 0
    end
end 

-- Game time on a 24 hour clock
function DataToColor:GameTime()
    local hours, minutes = GetGameTime()
    hours = (hours * 100) + minutes
    return hours
end

function DataToColor:GetGossipIcons()
    -- Checks if we have options available
    local option = GetGossipOptions()
    -- Checks if we have an active quest in the gossip window
    local activeQuest = GetGossipActiveQuests()
    -- Checks if we have a quest that we can pickup
    local availableQuest = GetGossipAvailableQuests()
    local gossipCode
    -- Code 0 if no gossip options are available
    if option == nil and activeQuest == nil and availableQuest == nil then
        gossipCode = 0
        -- Code 1 if only non quest gossip options are available
    elseif option ~= nil and activeQuest == nil and availableQuest == nil then
        gossipCode = 1
        -- Code 2 if only quest gossip options are available
    elseif option == nil and (activeQuest ~= nil or availableQuest) ~= nil then
        gossipCode = 2
        -- Code 3 if both non quest gossip options are available
    elseif option ~= nil and (activeQuest ~= nil or availableQuest) ~= nil then
        gossipCode = 3
        -- -- Error catcher
    else
        gossipCode = 0
    end
    return gossipCode
end

--return the x and y of corpse and resurrect the player if he is on his corpse
--the x and y is 0 if not dead
--runs the RetrieveCorpse() function to ressurrect
function DataToColor:CorpsePosition(coord)
    -- Assigns death coordinates
    local cX
    local cY
    if UnitIsGhost(self.C.unitPlayer) then
        local map = C_Map.GetBestMapForUnit(self.C.unitPlayer)
        if C_DeathInfo.GetCorpseMapPosition(map) ~= nil then
            cX, cY = C_DeathInfo.GetCorpseMapPosition(map):GetXY()
        end
    end
    if coord == "x" then
        if cX ~= nil then
            return cX
        else
            return 0
        end
        
    end
    if coord == "y" then
        if cY ~= nil then
            return cY
        else
            return 0
        end
    end
end

--returns class of player
function DataToColor:PlayerClass()
    -- UnitClass returns class and the class in uppercase e.g. "Mage" and "MAGE"
    local class, CC = UnitClass(self.C.unitPlayer)
    if CC == "MAGE" then
        class = 128
    elseif CC == "ROGUE" then
        class = 64
    elseif CC == "WARRIOR" then
        class = 32
    elseif CC == "PALADIN" then
        class = 16
    elseif CC == "HUNTER" then
        class = 8  
    elseif CC == "PRIEST" then
        class = 4      
    elseif CC == "SHAMAN" then
        class = 2    
    elseif CC == "WARLOCK" then
        class = 1    
    elseif CC == "DRUID" then
        class = 256
    else
        class = 0
    end
    return class
end

function DataToColor:ComboPoints()
    local points = GetComboPoints(self.C.unitPlayer, self.C.unitTarget);
    -- if target is in combat, return 0 for bitmask
    if points ~= nil then
        return points
        -- if target is not in combat, return 1 for bitmask
    else 
        return 0
    end
end

-----------------------------------------------------------------
-- Boolean functions --------------------------------------------
-- Only put functions here that are part of a boolean sequence --
-- Sew BELOW for examples ---------------------------------------
-----------------------------------------------------------------

-- Finds if player or target is in combat
function DataToColor:targetCombatStatus()
    -- if target is in combat, return 0 for bitmask
    if UnitAffectingCombat(self.C.unitTarget) then
        return 1
        -- if target is not in combat, return 1 for bitmask
    end
    return 0
end

-- Checks if target is dead. Returns 1 if target is dead, nil otherwise (converts to 0)
function DataToColor:GetEnemyStatus()
    if UnitIsDead(self.C.unitTarget) then
        return 1
    end
    return 0
end

function DataToColor:targetIsNormal()
    local classification = UnitClassification(self.C.unitTarget);
    if classification=="normal" then
        if (UnitIsPlayer(self.C.unitTarget)) then 
            return 0 
        end

        if UnitName(self.C.unitPet) == UnitName(self.C.unitTarget) then
            return 0
        end

        return 1
        -- if target is not in combat, return 1 for bitmask
    else 
        return 0
    end
end

-- Checks if we are currently alive or are a ghost/dead.
function DataToColor:deadOrAlive()
    if UnitIsDeadOrGhost(self.C.unitPlayer) then
        return 1
    end
    return 0
end

-- Checks the number of talent points we have available to spend
function DataToColor:checkTalentPoints()
    if UnitCharacterPoints(self.C.unitPlayer) > 0 then
        return 1
    end
    return 0
end

function DataToColor:shapeshiftForm()
    local form = GetShapeshiftForm(true)
    if form == nil then
        form = 0
    end
    return form
end

function DataToColor:playerCombatStatus()
    if UnitAffectingCombat(self.C.unitPlayer) then
        return 1 
    end
    return 0
end

-- Returns the slot in which we have a fully degraded item
function DataToColor:GetInventoryBroken()
    for i = 1, 18 do
        if GetInventoryItemBroken(self.C.unitPlayer, i) then
            return 1
        end
    end
    return 0
end
-- Checks if we are on a taxi
function DataToColor:IsPlayerFlying()
    local taxiStatus = UnitOnTaxi(self.C.unitPlayer)
    if taxiStatus then
        return 1
    end
    -- Returns 0 if not on a wind rider beast
    return 0
end

function DataToColor:IsPlayerSwimming()
    if IsSwimming() then
        return 1
    end
    return 0
end

function DataToColor:IsPlayerMounted()
    local mounted = IsMounted()
    if mounted then
        return 1
    end
    -- Returns 0 if not on a wind rider beast
    return 0
end

-- Returns true is player has less than 10 food in action slot 66
function DataToColor:needFood()
    if GetActionCount(6) < 10 then
        return 1
    end
    return 0
end

-- Returns true if the player has less than 10 water in action slot 7
function DataToColor:needWater()
    if GetActionCount(7) < 10 then
        return 1
    end
    return 0
end

-- Returns if we have a mana gem (Agate, Ruby, etc.) in slot 67
function DataToColor:needManaGem()
    if GetActionCount(67) < 1 then
        return 1
    end
    return 0
end

function DataToColor:IsTargetOfTargetPlayerAsNumber()
    if not(UnitName(self.C.unitTargetTarget)) then return 2 end -- target has no target
    if self.C.CHARACTER_NAME == UnitName(self.C.unitTarget) then return 0 end -- targeting self
    if UnitName(self.C.unitPet) == UnitName(self.C.unitTargetTarget) then return 4 end -- targetting my pet
    if self.C.CHARACTER_NAME == UnitName(self.C.unitTargetTarget) then return 1 end -- targetting me
    if UnitName(self.C.unitPet) == UnitName(self.C.unitTarget) and UnitName(self.C.unitTargetTarget) ~= nil then return 5 end
    return 3
end

-- Returns true if target of our target is us
function DataToColor:IsTargetOfTargetPlayer()
    local x = self:IsTargetOfTargetPlayerAsNumber()
    if x==1 or x==4 then return 1 else return 0 end
end

function DataToColor:IsTagged()
    if UnitIsTapDenied(self.C.unitTarget) then 
        return 1 
    end
    return 0
end

function DataToColor:IsAutoRepeatActionOn(actionSlot)
    if IsAutoRepeatAction(actionSlot) then
        return 1
    end
    return 0
end

function DataToColor:IsAutoRepeatSpellOn(spell)
    if IsAutoRepeatSpell(spell) then
        return 1
    end
    return 0
end

function DataToColor:IsCurrentSpell(spell)
    if IsCurrentSpell(spell) then
        return 1
    end
    return 0
end

function DataToColor:IsCurrentActionOn(actionSlot)
    if IsCurrentAction(actionSlot)  then
        return 1
    end
    return 0
end

function DataToColor:IsPetVisible()
    if UnitIsVisible(self.C.unitPet) and not UnitIsDead(self.C.unitPet)  then
        return 1
    end
    return 0
end

function DataToColor:petHappy()
    local happiness, damagePercentage, loyaltyRate = GetPetHappiness();
    -- (1 = unhappy, 2 = content, 3 = happy)
    if happiness ~= nil and happiness == 3 then
        return 1
    end
    return 0
end

-- Returns 0 if target is unskinnable or if we have no target.
function DataToColor:isUnskinnable()
    local creatureType = UnitCreatureType(self.C.unitTarget)
    -- Demons COULD be included in this list, but there are some skinnable demon dogs.
    if creatureType == "Humanoid" or creatureType == "Elemental" or creatureType == "Mechanical" or creatureType == "Totem" then
        return 1
    else if creatureType ~= nil then
            return 0
        end
    end
    return 1
end

-- A variable which can trigger a process exit on the node side with this macro:
-- /script EXIT_PROCESS_STATfort = 1
function DataToColor:ProcessExitStatus()
    -- Check if a process exit has been requested
    if EXIT_PROCESS_STATUS == 1 then
        -- If a process exit has been requested, resets global frame tracker to zero in order to give node time to read frames
        if globalCounter > 200 then
            self:log('Manual exit request processing...')
            globalCounter = 0
        end
    end
    -- Number of frames until EXIT_PROCESS_STATUS returns to false so that node process can begin again
    if globalCounter > 100 and EXIT_PROCESS_STATUS ~= 0 then
        EXIT_PROCESS_STATUS = 0
    end
    return EXIT_PROCESS_STATUS
end

-- List of possible subzones to which a player's hearthstone may be bound
local HearthZoneList = {"CENARION HOLD", "VALLEY OF TRIALS", "THE CROSSROADS", "RAZOR HILL", "DUROTAR", "ORGRIMMAR", "CAMP TAURAJO", "FREEWIND POST", "GADGETZAN", "SHADOWPREY VILLAGE", "THUNDER BLUFF", "UNDERCITY", "CAMP MOJACHE", "COLDRIDGE VALLEY", "DUN MOROGH", "THUNDERBREW DISTILLERY", "IRONFORGE", "STOUTLAGER INN", "STORMWIND CITY", "SOUTHSHORE", "LAKESHIRE", "STONETALON PEAK", "GOLDSHIRE", "SENTINEL HILL", "DEEPWATER TAVERN", "THERAMORE ISLE", "DOLANAAR", "ASTRANAAR", "NIJEL'S POINT", "CRAFTSMEN'S TERRACE", "AUBERDINE", "FEATHERMOON STRONGHOLD", "BOOTY BAY", "WILDHAMMER KEEP", "DARKSHIRE", "EVERLOOK", "RATCHET", "LIGHT'S HOPE CHAPEL"}

-- Returns sub zone ID based on index of subzone in constant variable
function DataToColor:hearthZoneID()
    local index = {}
    local hearthzone = string.upper(GetBindLocation())
    for k, v in pairs(HearthZoneList) do
        index[v] = k
    end
    if index[hearthzone] ~= nil then
        return index[hearthzone]
    else self:log(hearthzone .. "is not registered. Please add it to the table in D2C.")
    end
end