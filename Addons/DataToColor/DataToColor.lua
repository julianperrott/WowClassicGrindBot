----------------------------------------------------------------------------
--  DataToColor - display player position as color
----------------------------------------------------------------------------

local Load = select(2, ...)
local DataToColor = unpack(Load)

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
local globalCounter = 0

-- How often item frames change
local ITEM_ITERATION_FRAME_CHANGE_RATE = 5
-- How often the actionbar frames change
local ACTION_BAR_ITERATION_FRAME_CHANGE_RATE = 5
-- How often the gossip frames change
local GOSSIP_ITERATION_FRAME_CHANGE_RATE = 5

-- Action bar configuration for which spells are tracked
local MAX_ACTIONBAR_SLOT = 108

-- Timers
DataToColor.timeUpdateSec = 0.1
DataToColor.globalTime = 0
DataToColor.lastLoot = 0

DataToColor.frames = nil
DataToColor.r = 0

DataToColor.uiErrorMessage = 0

DataToColor.lastCombatDamageTakenCreature = 0
DataToColor.lastCombatDamageDoneCreature = 0
DataToColor.lastCombatCreature = 0
DataToColor.lastCombatCreatureDied = 0

DataToColor.lastAutoShot = 0
DataToColor.lastMainHandMeleeSwing = 0

DataToColor.targetChanged = true

DataToColor.playerGUID = UnitGUID(DataToColor.C.unitPlayer)
DataToColor.petGUID = UnitGUID(DataToColor.C.unitPet)

-- Update Queue
stack = {}
DataToColor.stack = stack

function stack:push(t, item)
    t[item] = item
end

function stack:pop(t)
    local key, value = minKey(t)
    if key ~= nil then
        value = t[key]
        t[key] = nil
        return key, value
    end
end

function minKey(t)
    local k
    for i, v in pairs(t) do
      k = k or i
      if v < t[k] then k = i end
    end
    return k
end

DataToColor.equipmentQueue = {}
DataToColor.bagQueue = {}
DataToColor.inventoryQueue = {}
DataToColor.gossipQueue = {}
DataToColor.actionBarCostQueue = {}

local equipmentSlot = nil
local bagNum = nil
local bagSlotNum = nil
local gossipNum = nil
local actionNum = nil

local x, y = 0, 0

-- Note: Coordinates where player is standing (max: 10, min: -10)
-- Note: Player direction is in radians (360 degrees = 2π radians)
-- Note: Player health/mana is taken out of 100% (0 - 1)

function DataToColor:RegisterSlashCommands()
    DataToColor:RegisterChatCommand('dc', 'StartSetup')
    DataToColor:RegisterChatCommand('dccpu', 'GetCPUImpact')
    DataToColor:RegisterChatCommand('dcflush', 'FushState')
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
    DataToColor:log("|cff0000ff" .. msg .. "|r")
    DataToColor:log(msg)
    DataToColor:log(debugstack())
    error(msg)
end


-- This function runs when addon is initialized/player logs in
-- Decides length of white box
function DataToColor:OnInitialize()
    DataToColor:SetupRequirements()
    DataToColor:CreateFrames(NUMBER_OF_FRAMES)
    DataToColor:RegisterSlashCommands()

    DataToColor:InitStorage()

    -- handle error events
    UIErrorsFrame:UnregisterEvent("UI_ERROR_MESSAGE")

    DataToColor:RegisterEvents()
    --DataToColor:UpdateTimer()

    local version = GetAddOnMetadata('DataToColor', 'Version')
    DataToColor:Print("Welcome. Using "..version)

    DataToColor:InitUpdateQueues()
end

function DataToColor:SetupRequirements()
    SetCVar("autoInteract", 1);
    SetCVar("autoLootDefault", 1)
    -- /run SetCVar("cameraSmoothStyle", 2) --always
	SetCVar('Contrast',50,'[]')
	SetCVar('Brightness',50,'[]')
	SetCVar('Gamma',1,'[]')
end

function DataToColor:Reset()
    DataToColor.playerGUID = UnitGUID(DataToColor.C.unitPlayer)
    DataToColor.petGUID = UnitGUID(DataToColor.C.unitPet)

    DataToColor.globalTime = 0
    DataToColor.lastLoot = 0
    DataToColor.uiErrorMessage = 0

    DataToColor.lastCombatDamageTakenCreature = 0
    DataToColor.lastCombatDamageDoneCreature = 0
    DataToColor.lastCombatCreature = 0
    DataToColor.lastCombatCreatureDied = 0

    DataToColor.lastAutoShot = 0
    DataToColor.lastMainHandMeleeSwing = 0
end

function DataToColor:Update()
    DataToColor.globalTime = DataToColor.globalTime + 1
    if DataToColor.globalTime > (256 * 256 * 256 - 1) then
        DataToColor.globalTime = 0
    end
    --DataToColor:Print(DataToColor.globalTime)
end

local UpdateFuncCache={};
function DataToColor:UpdateTimer()
    DataToColor:Update()

    local func = UpdateFuncCache[self]
    if not func then
        func = function() DataToColor:UpdateTimer(); end;
        UpdateFuncCache[self] = func;
    end
    C_Timer.After(DataToColor.timeUpdateSec, func);
end


function DataToColor:FushState()
    DataToColor.targetChanged = true

    DataToColor:Reset()

    DataToColor:InitEquipmentQueue()
    DataToColor:InitBagQueue()

    DataToColor:InitInventoryQueue(4)
    DataToColor:InitInventoryQueue(3)
    DataToColor:InitInventoryQueue(2)
    DataToColor:InitInventoryQueue(1)
    DataToColor:InitInventoryQueue(0)

    DataToColor:InitActionBarCostQueue()

    DataToColor:Print('Flush State')
end

function DataToColor:ConsumeChanges()
    if DataToColor.targetChanged then
        DataToColor.targetChanged = false
    end
end

function DataToColor:InitUpdateQueues()
    DataToColor:InitEquipmentQueue()

    DataToColor:InitBagQueue(0, 0)
    DataToColor:InitInventoryQueue(0)
    DataToColor:InitActionBarCostQueue()
end

function DataToColor:InitEquipmentQueue()
    for eqNum = 1, 23 do
        DataToColor.stack:push(DataToColor.equipmentQueue, eqNum)
    end
end

function DataToColor:InitInventoryQueue(containerID)
    if containerID >= 0 and containerID <= 4 then
        for i = 1, 21 do
            DataToColor.stack:push(DataToColor.inventoryQueue, containerID * 1000 + i)
        end
    end
end

function DataToColor:InitBagQueue(min, max)
    min = min or 0
    max = max or 4
    for bag = min, max do
        DataToColor.stack:push(DataToColor.bagQueue, bag)
    end
end

function DataToColor:InitActionBarCostQueue()
    for slot=1, MAX_ACTIONBAR_SLOT do
        if DataToColor:actionbarCost(slot) then
            DataToColor.stack:push(DataToColor.actionBarCostQueue, slot)
        end
    end
end


local valueCache = {}
-- Function to mass generate all of the initial frames for the pixel reader
function DataToColor:CreateFrames(n)
    -- Note: Use single frame and update color on game update call
    local function UpdateFrameColor(f)
        -- Apply color to backdrop
        function MakePixelSquareArr(col, slot)
            DataToColor.frames[slot + 1]:SetBackdropColor(col[1], col[2], col[3], 1)
        end

        -- DataToColor:integerToColor
        function MakePixelSquareArrI(value, slot)
            if valueCache[slot + 1].last ~= value then
                valueCache[slot + 1].last = value
                MakePixelSquareArr(DataToColor:integerToColor(value), slot)
            end
        end

        -- DataToColor:fixedDecimalToColor
        function MakePixelSquareArrF(value, slot)
            if valueCache[slot + 1].last ~= value then
                valueCache[slot + 1].last = value
                MakePixelSquareArr(DataToColor:fixedDecimalToColor(value), slot)
            end
        end

        if not SETUP_SEQUENCE then

            DataToColor.playerGUID = UnitGUID(DataToColor.C.unitPlayer)
            DataToColor.petGUID = UnitGUID(DataToColor.C.unitPet)

            MakePixelSquareArrI(0, 0)
            -- The final data square, reserved for additional metadata.
            MakePixelSquareArrI(2000001, NUMBER_OF_FRAMES - 1)

            -- Position related variables --
            x, y = DataToColor:GetCurrentPlayerPosition()
            if x == nil or y == nil then
                x = 0
                y = 0
            end

            MakePixelSquareArrF(x * 10, 1) --1 The x-coordinate
            MakePixelSquareArrF(y * 10, 2) --2 The y-coordinate

            MakePixelSquareArrF(DataToColor:GetPlayerFacing(), 3) --3 The direction the player is facing in radians
            MakePixelSquareArrI(DataToColor:GetBestMap(), 4) -- MapId
            MakePixelSquareArrI(DataToColor:getPlayerLevel(), 5) --12 Represents character level

            x, y = DataToColor:CorpsePosition()
            MakePixelSquareArrF(x * 10, 6) -- Returns the x coordinates of corpse
            MakePixelSquareArrF(y * 10, 7) -- Return y coordinates of corpse

            -- Boolean variables --
            MakePixelSquareArrI(DataToColor:Base2Converter(), 8)
            MakePixelSquareArrI(DataToColor:Base2Converter2(), 9)

            -- Start combat/NPC related variables --
            MakePixelSquareArrI(DataToColor:getHealthMax(DataToColor.C.unitPlayer), 10) --8 Represents maximum amount of health
            MakePixelSquareArrI(DataToColor:getHealthCurrent(DataToColor.C.unitPlayer), 11) --9 Represents current amount of health

            MakePixelSquareArrI(DataToColor:getPowerTypeMax(DataToColor.C.unitPlayer, nil), 12) --10 Represents maximum amount of primary resource(dynamic)
            MakePixelSquareArrI(DataToColor:getPowerTypeCurrent(DataToColor.C.unitPlayer, nil), 13) --11 Represents current amount of primary resource(dynamic)

            MakePixelSquareArrI(DataToColor:getPowerTypeMax(DataToColor.C.unitPlayer, Enum.PowerType.Mana), 14) --10 Represents maximum amount of mana
            MakePixelSquareArrI(DataToColor:getPowerTypeCurrent(DataToColor.C.unitPlayer, Enum.PowerType.Mana), 15) --11 Represents current amount of mana

            if DataToColor.targetChanged then
                MakePixelSquareArrI(DataToColor:GetTargetName(0), 16) -- Characters 1-3 of target's name
                MakePixelSquareArrI(DataToColor:GetTargetName(3), 17) -- Characters 4-6 of target's name

                MakePixelSquareArrI(DataToColor:getHealthMax(DataToColor.C.unitTarget), 18) -- Return the maximum amount of health a target can have
            end

            MakePixelSquareArrI(DataToColor:getHealthCurrent(DataToColor.C.unitTarget), 19) -- Returns the current amount of health the target currently has

            if DataToColor:Modulo(globalCounter, ITEM_ITERATION_FRAME_CHANGE_RATE) == 0 then
                -- 20
                bagNum = DataToColor.stack:pop(DataToColor.bagQueue)
                if bagNum then
                    local freeSlots, bagType = GetContainerNumFreeSlots(bagNum) or 0, 0
                    -- BagType + Index + FreeSpace + BagSlots
                    MakePixelSquareArrI(bagType * 1000000 + bagNum * 100000 + freeSlots * 1000 + DataToColor:bagSlots(bagNum), 20)
                    --DataToColor:Print("bagQueue "..bagType.." -> "..bagNum.." -> "..freeSlots.." -> "..DataToColor:bagSlots(bagNum))
                end

                -- 21 22 23
                bagSlotNum = DataToColor.stack:pop(DataToColor.inventoryQueue)
                if bagSlotNum then

                    bagNum = math.floor(bagSlotNum / 1000)
                    bagSlotNum = bagSlotNum - (bagNum * 1000)

                    local _, itemCount, _, _, _, _, 
                    _, _, _, itemID = GetContainerItemInfo(bagNum, bagSlotNum)

                    if itemID == nil then
                        itemCount = 0
                        itemID = 0
                    end

                    --DataToColor:Print("inventoryQueue: "..bagNum.. " "..bagSlotNum.." -> id:"..itemID.." c:"..itemCount)

                    local soulbound = 0
                    if itemCount > 0 then
                        soulbound = C_Item.IsBound(ItemLocation:CreateFromBagAndSlot(bagNum, bagSlotNum)) and 1 or 0
                    end

                    -- 0-4 bagNum + 1-21 itenNum + 1-1000 quantity
                    MakePixelSquareArrI(bagNum * 1000000 + bagSlotNum * 10000 + itemCount, 21)

                    -- itemId 1-999999
                    MakePixelSquareArrI(itemID, 22)

                    -- item bits
                    MakePixelSquareArrI(soulbound, 23)
                end

                -- 24 25
                equipmentSlot = DataToColor.stack:pop(DataToColor.equipmentQueue)
                if equipmentSlot then
                    MakePixelSquareArrI(equipmentSlot, 24)
                    MakePixelSquareArrI(DataToColor:equipSlotItemId(equipmentSlot), 25)
                    --DataToColor:Print("equipmentQueue "..equipmentSlot.." -> "..itemId)
                end
            end

            MakePixelSquareArrI(DataToColor:isCurrentAction(1, 24), 26)
            MakePixelSquareArrI(DataToColor:isCurrentAction(25, 48), 27)
            MakePixelSquareArrI(DataToColor:isCurrentAction(49, 72), 28)
            MakePixelSquareArrI(DataToColor:isCurrentAction(73, 96), 29)
            MakePixelSquareArrI(DataToColor:isCurrentAction(97, 108), 30)

            MakePixelSquareArrI(DataToColor:isActionUseable(1, 24), 31)
            MakePixelSquareArrI(DataToColor:isActionUseable(25, 48), 32)
            MakePixelSquareArrI(DataToColor:isActionUseable(49, 72), 33)
            MakePixelSquareArrI(DataToColor:isActionUseable(73, 96), 34)
            MakePixelSquareArrI(DataToColor:isActionUseable(97, 108), 35)

            if DataToColor:Modulo(globalCounter, ACTION_BAR_ITERATION_FRAME_CHANGE_RATE) == 0 then
                actionNum = DataToColor.stack:pop(DataToColor.actionBarCostQueue)
                if actionNum then
                    MakePixelSquareArrI(DataToColor:actionbarCost(actionNum), 36)
                end
            end

            if DataToColor:Modulo(globalCounter, GOSSIP_ITERATION_FRAME_CHANGE_RATE) == 0 then
                gossipNum = DataToColor.stack:pop(DataToColor.gossipQueue)
                if gossipNum then
                    MakePixelSquareArrI(gossipNum, 37)
                end
            end

            globalCounter = globalCounter + 1

            MakePixelSquareArrI(DataToColor:getHealthMax(DataToColor.C.unitPet), 38)
            MakePixelSquareArrI(DataToColor:getHealthCurrent(DataToColor.C.unitPet), 39)

            MakePixelSquareArrI(DataToColor:areSpellsInRange(), 40)
            MakePixelSquareArrI(DataToColor:getAuraMaskForClass(UnitBuff, DataToColor.C.unitPlayer, DataToColor.S.playerBuffs), 41);
            MakePixelSquareArrI(DataToColor:getAuraMaskForClass(UnitDebuff, DataToColor.C.unitTarget, DataToColor.S.targetDebuffs), 42);
            MakePixelSquareArrI(DataToColor:getTargetLevel(), 43)

            -- Amount of money in coppers
            MakePixelSquareArrI(DataToColor:Modulo(DataToColor:getMoneyTotal(), 1000000), 44) -- Represents amount of money held (in copper)
            MakePixelSquareArrI(floor(DataToColor:getMoneyTotal() / 1000000), 45) -- Represents amount of money held (in gold) 

            --MakePixelSquareArrI(DataToColor:GetGossipIcons(), 45) -- Returns which gossip icons are on display in dialogue box

            MakePixelSquareArrI(DataToColor.S.PlayerClass, 46) -- Returns player class as an integer
            MakePixelSquareArrI(DataToColor:isUnskinnable(), 47) -- Returns 1 if creature is unskinnable
            MakePixelSquareArrI(DataToColor:shapeshiftForm(), 48) -- Shapeshift id https://wowwiki.fandom.com/wiki/API_GetShapeshiftForm
            MakePixelSquareArrI(DataToColor:getRange(), 49) -- 15 Represents if target is within 0-5 5-15 15-20, 20-30, 30-35, or greater than 35 yards

            MakePixelSquareArrI(DataToColor:getUnitXP(DataToColor.C.unitPlayer), 50) -- Player Xp
            MakePixelSquareArrI(DataToColor:getUnitXPMax(DataToColor.C.unitPlayer), 51) -- Player Level Xp
            MakePixelSquareArrI(DataToColor.uiErrorMessage, 52) -- Last UI Error message
            DataToColor.uiErrorMessage=0;

            MakePixelSquareArrI(DataToColor:CastingInfoSpellId(DataToColor.C.unitPlayer), 53) -- Spell being cast
            MakePixelSquareArrI(DataToColor:ComboPoints(), 54) -- Combo points for rogue / druid
            MakePixelSquareArrI(DataToColor:getAuraCount(UnitDebuff, DataToColor.C.unitPlayer), 55)

            if DataToColor.targetChanged then
                MakePixelSquareArrI(DataToColor:targetNpcId(), 56) -- target id
                MakePixelSquareArrI(DataToColor:getGuid(DataToColor.C.unitTarget),57) -- target reasonably uniqueId
            end

            MakePixelSquareArrI(DataToColor:CastingInfoSpellId(DataToColor.C.unitTarget), 58) -- Spell being cast by target

            MakePixelSquareArrI(DataToColor:IsTargetOfTargetPlayerAsNumber(),59) -- IsTargetOfTargetPlayerAsNumber

            MakePixelSquareArrI(DataToColor.lastAutoShot, 60)
            MakePixelSquareArrI(DataToColor.lastMainHandMeleeSwing, 61)
            -- 62 not used
            -- 63 not used
            -- 64 not used

            MakePixelSquareArrI(DataToColor.lastCombatCreature, 64) -- Combat message creature
            MakePixelSquareArrI(DataToColor.lastCombatDamageDoneCreature, 65) -- Last Combat damage done
            MakePixelSquareArrI(DataToColor.lastCombatDamageTakenCreature, 66) -- Last Combat Damage taken
            MakePixelSquareArrI(DataToColor.lastCombatCreatureDied, 67) -- Last Killed Unit

            MakePixelSquareArrI(DataToColor:getGuid(DataToColor.C.unitPet), 68) -- pet guid
            MakePixelSquareArrI(DataToColor:getGuid(DataToColor.C.unitPetTarget), 69) -- pet target

            -- Timers
            MakePixelSquareArrI(DataToColor.globalTime, 70)
            MakePixelSquareArrI(DataToColor.lastLoot, 71)

            DataToColor:ConsumeChanges()

            DataToColor:HandlePlayerInteractionEvents()

            DataToColor:Update()
        end

        if SETUP_SEQUENCE then
            -- Emits meta data in data square index 0 concerning our estimated cell size, number of rows, and the numbers of frames
            MakePixelSquareArrI(CELL_SPACING * 10000000 + CELL_SIZE * 100000 + 1000 * FRAME_ROWS + NUMBER_OF_FRAMES, 0)
            -- Assign pixel squares a value equivalent to their respective indices.
            for i = 1, NUMBER_OF_FRAMES - 1 do
                MakePixelSquareArrI(i, i)
            end
        end
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
    
    -- Note: Use for loop based on input to generate "n" number of frames
    for frame = 0, n - 1 do
        local y = DataToColor:Modulo(frame, FRAME_ROWS) -- those are grid coordinates (1,2,3,4 by  1,2,3,4 etc), not pixel coordinates
        local x = floor(frame / FRAME_ROWS)
        -- Put frame information in to an object/array
        frames[frame + 1] = genFrame("frame_"..tostring(frame), x, y)
        valueCache[frame + 1] = { last = -1 }
    end
    
    -- Assign DataToColor.frames to frame list generated above
    DataToColor.frames = frames
    DataToColor.frames[1]:SetScript("OnUpdate", function() UpdateFrameColor(f) end)
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

    local target = GetUnitName(DataToColor.C.unitTarget)
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


-- A variable which can trigger a process exit on the node side with this macro:
-- /script EXIT_PROCESS_STATfort = 1
function DataToColor:ProcessExitStatus()
    -- Check if a process exit has been requested
    if EXIT_PROCESS_STATUS == 1 then
        -- If a process exit has been requested, resets global frame tracker to zero in order to give node time to read frames
        if globalCounter > 200 then
            DataToColor:log('Manual exit request processing...')
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
    else DataToColor:log(hearthzone .. "is not registered. Please add it to the table in D2C.")
    end
end