local Load = select(2, ...)
local DataToColor = unpack(Load)

local CAST_START = 999998
local CAST_SUCCESS = 999999

local MERCHANT_SHOW_V = 9999999
local MERCHANT_CLOSED_V = 9999998

local GOSSIP_START = 69
local GOSSIP_END = 9999994

local ignoreErrorList = {
    "ERR_ABILITY_COOLDOWN",
    "ERR_OUT_OF_RAGE",
    "ERR_NO_ATTACK_TARGET",
    "ERR_OUT_OF_MANA",
    "ERR_SPELL_FAILED_SHAPESHIFT_FORM_S",
    "ERR_GENERIC_NO_TARGET",
    "ERR_ATTACK_PREVENTED_BY_MECHANIC_S",
    "ERR_ATTACK_STUNNED",
    "ERR_NOEMOTEWHILERUNNING",
}

local errorList = {
    "ERR_BADATTACKFACING", --1 "You are facing the wrong way!";
    "ERR_SPELL_FAILED_S", --2 -- like a printf 
    "ERR_SPELL_OUT_OF_RANGE", --3 "Out of range.";
    "ERR_BADATTACKPOS", --4 "You are too far away!";
    "ERR_AUTOFOLLOW_TOO_FAR", --5 "Target is too far away.";
    "SPELL_FAILED_MOVING", --6 "Can't do that while moving";
    "ERR_SPELL_COOLDOWN",  --7 "Spell is not ready yet."
    "ERR_SPELL_FAILED_ANOTHER_IN_PROGRESS", --8 "Another action is in progress"
    "ERR_SPELL_FAILED_STUNNED", -- 9 "Can't do that while stunned"
    "SPELL_FAILED_INTERRUPTED", -- 10 "Interrupted"
    "SPELL_FAILED_ITEM_NOT_READY" -- 11 "Item is not ready yet"
};

function DataToColor:RegisterEvents()
    DataToColor:RegisterEvent("UI_ERROR_MESSAGE", 'OnUIErrorMessage')
    DataToColor:RegisterEvent("COMBAT_LOG_EVENT_UNFILTERED", 'OnCombatEvent')
    DataToColor:RegisterEvent('LOOT_CLOSED','OnLootClosed')
    DataToColor:RegisterEvent('BAG_UPDATE','OnBagUpdate')
    DataToColor:RegisterEvent('BAG_CLOSED','OnBagUpdate')
    DataToColor:RegisterEvent('MERCHANT_SHOW','OnMerchantShow')
    DataToColor:RegisterEvent('MERCHANT_CLOSED','OnMerchantClosed')
    DataToColor:RegisterEvent('PLAYER_TARGET_CHANGED', 'OnPlayerTargetChanged')
    DataToColor:RegisterEvent('PLAYER_EQUIPMENT_CHANGED', 'OnPlayerEquipmentChanged')
    DataToColor:RegisterEvent('GOSSIP_SHOW', 'OnGossipShow')
    DataToColor:RegisterEvent('SPELLS_CHANGED', 'OnSpellsChanged')
    DataToColor:RegisterEvent('ACTIONBAR_SLOT_CHANGED', 'ActionbarSlotChanged')
    DataToColor:RegisterEvent('CORPSE_IN_RANGE', 'CorpseInRangeEvent')
    DataToColor:RegisterEvent('CORPSE_OUT_OF_RANGE', 'CorpseOutOfRangeEvent')
end

function DataToColor:OnUIErrorMessage(event, messageType, message)
    local code, ignored, foundMessage, message = DataToColor:GetErrorCode(messageType, message)

    if ignored then
        UIErrorsFrame:AddMessage(message, 0.7, 0.7, 0.7) -- show as grey messasge
    elseif foundMessage and code ~= 0 then
        DataToColor.uiErrorMessage = code;
        UIErrorsFrame:AddMessage(message, 0, 1, 0) -- show as green messasge
    else
        UIErrorsFrame:AddMessage(message, 0, 0, 1) -- show as blue message (unknown message)
    end
end

function DataToColor:GetErrorCode(messageType, message)

    local errorName
    local foundMessage = false
    local ignored = false
    local code = 0

    if messageType ~= nil then
        errorName = GetGameMessageInfo(messageType)
    end

    for i = 1, table.getn(ignoreErrorList), 1 do
        if ignoreErrorList[i] == errorName then
            foundMessage = true;
            ignored = true
        end
    end

    if not ignored and not foundMessage then
        for i = 1, table.getn(errorList), 1 do
            if errorList[i] == errorName or
            (_G[errorList[i]] ~= nil and string.find(_G[errorList[i]], message)) then
                code = i;
                foundMessage = true;
            end
        end
    end

    -- ERR_SPELL_FAILED_S
    -- find by message ex combatlog
    if not ignored and (not foundMessage or errorName == errorList[2]) then
        if string.find(message, SPELL_FAILED_UNIT_NOT_INFRONT) then
            code = 1
            foundMessage = true
            message = message.." ("..ERR_BADATTACKFACING..")"
        elseif string.find(message, SPELL_FAILED_MOVING) then
            foundMessage = true
            code = 6
        elseif string.find(message, SPELL_FAILED_STUNNED) then
            foundMessage = true
            code = 9
        end
    end

    return code, ignored, foundMessage, message
end

local watchedSpells = {
    [DataToColor.C.Spell.AutoShotId] = function ()
        --DataToColor:Print("Auto Shot detected")
        DataToColor.lastAutoShot = DataToColor.globalTime
    end
  }

local swing_reset_spells = {
    --[[ Maul ]]
    [132136]=1,
    --[[ Raptor Strike ]]
    [132223]=1,
    --[[ Cleave ]]
    [132338]=1,
    --[[ Heroic Strike ]]
    [132282]=1,
    --[[ Slam ]]
    [132340]=1
}

function DataToColor:OnCombatEvent(...)
    local _, eventType, _, sourceGUID, sourceName, _, _, destGUID, destName, _, _, spellId, _, _ = CombatLogGetCurrentEventInfo();
    --print(CombatLogGetCurrentEventInfo())
    if eventType=="SPELL_PERIODIC_DAMAGE" then
        DataToColor.lastCombatCreature=0;
    elseif string.find(sourceGUID, "Creature") then
        DataToColor.lastCombatCreature = DataToColor:getGuidFromUUID(sourceGUID);
    else
        DataToColor.lastCombatCreature=0;
    end

    if string.find(sourceGUID, "Creature") and (destGUID == DataToColor.playerGUID or destGUID == DataToColor.petGUID) then
        DataToColor.stack:push(DataToColor.CombatDamageTakenQueue, DataToColor:getGuidFromUUID(sourceGUID))
    end

    if sourceGUID == DataToColor.playerGUID then
        if eventType=="SPELL_CAST_SUCCESS" then
            if watchedSpells[spellId] then watchedSpells[spellId]() end

            local _, _, icon = GetSpellInfo(spellId)
            if swing_reset_spells[icon] then
                --DataToColor:Print("Special Melee Swing detected")
                DataToColor.lastMainHandMeleeSwing = DataToColor.globalTime
            end
        end

        if string.find(eventType, "_CAST_START") then
            DataToColor.lastCastEvent = CAST_START
            DataToColor.lastCastSpellId = spellId
            --print(CombatLogGetCurrentEventInfo())
            --print("_CAST_START "..spellId)
        end

        if string.find(eventType, "_CAST_SUCCESS") or string.find(eventType, "_CAST_FAILED") then
            --print(CombatLogGetCurrentEventInfo())
            DataToColor.lastCastSpellId = spellId

            if string.find(eventType, "_CAST_FAILED") then
                --local lastCastEvent = DataToColor.lastCastEvent
                local failedType = select(15, CombatLogGetCurrentEventInfo())
                DataToColor.lastCastEvent = DataToColor:GetErrorCode(nil, failedType)
                --print(lastCastEvent.." -> "..DataToColor.lastCastEvent.." "..failedType.." "..spellId)
            else
                DataToColor.lastCastEvent = CAST_SUCCESS
            end
        end

        -- matches SWING_ RANGE_ SPELL_ but not SPELL_PERIODIC
        if not string.find(eventType, "SPELL_PERIODIC") and
            (string.find(eventType, "_DAMAGE") or string.find(eventType, "_MISSED")) then
            DataToColor.lastCombatDamageDoneCreature = DataToColor:getGuidFromUUID(destGUID);
        end

        if string.find(eventType, "SWING_") then
            local _, _, _, _, _, _, _, _, _, isOffHand = select(12, ...)
            if not isOffHand then
                --DataToColor:Print("Normal Melee Swing detected")
                DataToColor.lastMainHandMeleeSwing = DataToColor.globalTime
            end
        end
    end

    if eventType=="UNIT_DIED" then
        if string.find(destGUID, "Creature") then
            --print(CombatLogGetCurrentEventInfo())
            DataToColor.lastCombatCreatureDied = DataToColor:getGuidFromUUID(destGUID);
            --print("v_killing blow " .. destGUID .. " " .. DataToColor.lastCombatCreatureDied .. " " .. destName)
        else
            --print("i_killing blow " .. destGUID .. " " .. destName)
        end
    end
end

function DataToColor:OnLootClosed(event)
    DataToColor.lastLoot = DataToColor.globalTime
    --DataToColor:Print("OnLootClosed:"..DataToColor.lastLoot)
end

function DataToColor:OnBagUpdate(event, containerID)
    if containerID >= 0 and containerID <=4 then
        DataToColor.stack:push(DataToColor.bagQueue, containerID)
        DataToColor:InitInventoryQueue(containerID)

        if containerID >= 1 then
            DataToColor.stack:push(DataToColor.equipmentQueue, 19 + containerID) -- from tabard
        end
    end
    --DataToColor:Print("OnBagUpdate "..containerID)
end

function DataToColor:OnMerchantShow(event)
    DataToColor.stack:push(DataToColor.gossipQueue, MERCHANT_SHOW_V)
end

function DataToColor:OnMerchantClosed(event)
    DataToColor.stack:push(DataToColor.gossipQueue, MERCHANT_CLOSED_V)
end

function DataToColor:OnPlayerTargetChanged(event)
    DataToColor.targetChanged = true
end

function DataToColor:OnPlayerEquipmentChanged(event, equipmentSlot, hasCurrent)
    DataToColor.stack:push(DataToColor.equipmentQueue, equipmentSlot)
    --local c = hasCurrent and 1 or 0
    --DataToColor:Print("OnPlayerEquipmentChanged "..equipmentSlot.." -> "..c)
end

function DataToColor:OnGossipShow(event)
    local options = GetGossipOptions()
    if not options then
        return
    end

    DataToColor.stack:push(DataToColor.gossipQueue, GOSSIP_START)

    -- returns variable string - format of one entry
    -- [1] localized name
    -- [2] gossip_type
    local GossipOptions = { GetGossipOptions() }
    local count = table.getn(GossipOptions) / 2
    for k, v in pairs(GossipOptions) do
        -- do something
        if k % 2 == 0 then
            DataToColor.stack:push(DataToColor.gossipQueue, 10000 * count + 100 * (k/2) + DataToColor.C.Gossip[v])
        end
    end
    DataToColor.stack:push(DataToColor.gossipQueue, GOSSIP_END)
end

function DataToColor:OnSpellsChanged(event)
    DataToColor:InitTalentQueue()
    DataToColor:InitSpellBookQueue()
    DataToColor:InitActionBarCostQueue()
end

function DataToColor:ActionbarSlotChanged(event, slot)
    if slot and HasAction(slot) then
        DataToColor.stack:push(DataToColor.actionBarCostQueue, slot)
    end
end

function DataToColor:CorpseInRangeEvent(event)
    DataToColor.corpseInRange = 1
end

function DataToColor:CorpseOutOfRangeEvent(event)
    DataToColor.corpseInRange = 0
end

DataToColor.playerInteractIterator = 0

-- List of talents that will be trained
local talentList = {
    "Improved Frostbolt",
    "Ice Shards",
    "Frostbite",
    "Piercing Ice",
    "Improved Frost Nova",
    "Shatter",
    "Arctic Reach",
    "Ice Block",
    "Ice Barrier",
    "Winter's Chill",
    "Frost Channeling",
    "Frost Warding",
    "Elemental Precision",
    "Permafrost",
    "Improved Fireball",
    "Improved Fire Blast"
}

local CORPSE_RETRIEVAL_DISTANCE = 40

-----------------------------------------------------------------------------
-- Begin Event Section -- -- Begin Event Section -- -- Begin Event Section --
-- Begin Event Section -- -- Begin Event Section -- -- Begin Event Section --
-- Begin Event Section -- -- Begin Event Section -- -- Begin Event Section --
-- Begin Event Section -- -- Begin Event Section -- -- Begin Event Section --
-----------------------------------------------------------------------------
function DataToColor:HandlePlayerInteractionEvents()
    -- Handles group accept/decline
    if DataToColor.DATA_CONFIG.ACCEPT_PARTY_REQUESTS or DataToColor.DATA_CONFIG.DECLINE_PARTY_REQUESTS then
        DataToColor:HandlePartyInvite()
    end
    -- Handles item repairs when talking to item repair NPC
    if DataToColor.DATA_CONFIG.AUTO_REPAIR_ITEMS then
        DataToColor:RepairItems()
    end
    -- Handles learning talents, only works after level 10
    if DataToColor.DATA_CONFIG.AUTO_LEARN_TALENTS then
        --DataToColor:LearnTalents()
    end
    -- Handles train new spells and talents
    if DataToColor.DATA_CONFIG.AUTO_TRAIN_SPELLS then
        --DataToColor:CheckTrainer()  
    end
    -- Resurrect player
    if DataToColor.DATA_CONFIG.AUTO_RESURRECT then
        DataToColor:ResurrectPlayer()
    end

    DataToColor:IncrementIterator();
end

-- Declines/Accepts Party Invites.
function DataToColor:HandlePartyInvite()
    -- Declines party invite if configured to decline
    if DataToColor.DATA_CONFIG.DECLINE_PARTY_REQUESTS then
        DeclineGroup()
    else if DataToColor.DATA_CONFIG.ACCEPT_PARTY_REQUESTS then
            AcceptGroup()
        end
    end
    -- Hides the party invite pop-up regardless of whether we accept it or not
    StaticPopup_Hide("PARTY_INVITE")
end

-- Repairs items if they are broken
function DataToColor:RepairItems()
    if CanMerchantRepair() and GetRepairAllCost() > 0 then
        if GetMoney() >= GetRepairAllCost() then
            RepairAllItems()
        end
    end
end

-- Automatically learns predefined talents
function DataToColor:LearnTalents()
    if UnitCharacterPoints(DataToColor.C.unitPlayer) > 0 then
        -- Grabs global list of talents we want to learn
        for i = 0, table.getn(talentList), 1 do
            -- Iterates through each talent tab (e.g. "Arcane, Fire, Frost")
            for j = 0, 3, 1 do
                -- Loops through all of the talents in each individual tab
                for k = 1, GetNumTalents(j), 1 do
                    -- Grabs API info of a specified talent index
                    local name, iconPath, tier, column, currentRank, maxRank, isExceptional, meetsPrereq, previewRank, meetsPreviewPrereq = GetTalentInfo(j, k)
                    local tabId, tabName, tabPointsSpent, tabDescription, tabIconTexture = GetTalentTabInfo(j)
                    local _, _, isLearnable = GetTalentPrereqs(j, k)
                    -- DEFAULT_CHAT_FRAME:AddMessage("hello" .. tier)
                    -- Runs API call to learn specified talent. Skips over it if we already have the max rank.
                    if name == talentList[i] and currentRank ~= maxRank and meetsPrereq then
                        -- Checks if we have spent enough points in the prior tiers in order to purchase talent. Otherwie moves on to next possible spell
                        if tabPointsSpent ~= nil and tabPointsSpent >= (tier * 5) - 5 then
                            LearnTalent(j, k)
                            return
                        end
                    end
                end
            end
        end
    end
end

-- List desired spells and professions to be trained here.
function ValidSpell(spell)
    local spellList = {
        "Conjure Food",
        "Conjure Water",
        "Conjure Mana Ruby",
        "Mana Shield",
        "Arcane Intellect",
        "Fire Blast",
        "Fireball",
        "Frostbolt",
        "Counterspell",
        "Ice Barrier",
        "Evocation",
        "Frost Armor",
        "Frost Nova",
        "Ice Armor",
        "Remove Lesser Curse",
        "Blink",
        "Apprentice Skinning",
        "Journeyman Skinning",
        "Expert Skinning",
        "Artisan Skinning",
        "Apprentice Fishing",
        "Journeyman Fishing"
    }
    -- Loops through all spells to see if we have a matching spells with the one passed in
    for i = 0, table.getn(spellList), 1 do
        if spellList[i] == spell then
            return true
        end
    end
    return false
end

function DataToColor:IncrementIterator()
    DataToColor.playerInteractIterator = DataToColor.playerInteractIterator + 1
end 

-- Used purely for training spells and professions
function DataToColor:CheckTrainer()
    DataToColor.playerInteractIterator = DataToColor.playerInteractIterator + 1
    if DataToColor:Modulo(DataToColor.playerInteractIterator, 30) == 1 then
        -- First checks that the trainer gossip window is open
        -- DEFAULT_CHAT_FRAME:AddMessage(GetTrainerServdiceInfo(1))
        if GetTrainerServiceInfo(1) ~= nil and DataToColor.DATA_CONFIG.AUTO_TRAIN_SPELLS then
            -- LPCONFIG.AUTO_TRAIN_SPELLS = false
            local allAvailableOptions = GetNumTrainerServices()
            local money = GetMoney()
            local level = UnitLevel(DataToColor.C.unitPlayer)
            
            -- Loops through every spell on the list and checks if we
            -- 1) Have the level to train that spell
            -- 2) Have the money want to train that spell
            -- 3) Want to train that spell
            for i = 1, allAvailableOptions, 1 do
                local spell = GetTrainerServiceInfo(i)
                if spell ~= nil and ValidSpell(spell) then
                    if GetTrainerServiceLevelReq(i) <= level then
                        if GetTrainerServiceCost(i) <= money then
                            -- DEFAULT_CHAT_FRAME:AddMessage(" buying spell" .. tostring(i) )
                            BuyTrainerService(i)
                            -- Closes skinning trainer, fishing trainer menu, etc.
                            -- Closes after one profession purchase. Impossible to buy profession skills concurrently.
                            if IsTradeskillTrainer() then
                                CloseTrainer()
                                -- LPCONFIG.AUTO_TRAIN_SPELLS = true
                            end
                            -- DEFAULT_CHAT_FRAME:AddMessage(allAvailableOptions .. tostring(i) )
                            -- if not (allAvailableOptions == i) then
                            -- TrainSpells()
                            return
                            -- end
                            -- An error messages for the rare case where we don't have enough money for a spell but have the level for it.
                        else if GetTrainerServiceCost(i) > money then
                            end
                        end
                    end
                end
            end
            -- DEFAULT_CHAT_FRAME:AddMessage('between')
            -- Automatically closes menu after we have bought all spells we need to buy
            --CloseTrainer()
            -- LPCONFIG.AUTO_TRAIN_SPELLS = true
        end
    end
end

--the x and y is 0 if not dead
--runs the RetrieveCorpse() function to ressurrect
function DataToColor:ResurrectPlayer()
    if DataToColor:Modulo(DataToColor.playerInteractIterator, 700) == 1 then
        if UnitIsDeadOrGhost(DataToColor.C.unitPlayer) then
            
            -- Accept Release Spirit immediately after dying
            if not UnitIsGhost(DataToColor.C.unitPlayer) and UnitIsGhost(DataToColor.C.unitPlayer) ~= nil then
                RepopMe()
            end
            if UnitIsGhost(DataToColor.C.unitPlayer) then
                local map = C_Map.GetBestMapForUnit(DataToColor.C.unitPlayer)
                if C_DeathInfo.GetCorpseMapPosition(map) ~= nil then
                    local cX, cY = C_DeathInfo.GetCorpseMapPosition(map):GetXY()
                    local x, y = DataToColor:GetCurrentPlayerPosition()
                    -- Waits so that we are in range of specified retrieval distance, and ensures there is no delay timer before attemtping to resurrect
                    if math.abs(cX - x) < CORPSE_RETRIEVAL_DISTANCE / 1000 and math.abs(cY - y) < CORPSE_RETRIEVAL_DISTANCE / 1000 and GetCorpseRecoveryDelay() == 0 then
                        DEFAULT_CHAT_FRAME:AddMessage('Attempting to retrieve corpse')
                        -- Accept Retrieve Corpsse when near enough
                        RetrieveCorpse()
                    end
                end
            end
        end
    end
end
