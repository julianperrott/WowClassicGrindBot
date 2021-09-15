local Load = select(2, ...)
local DataToColor = unpack(Load)
local Range = DataToColor.Libs.RangeCheck

-- Global table of all items player has
local items = {}
local itemsPlaceholderComparison = {}
local enchantedItemsList = {}

-- Discover player's direction in radians (360 degrees = 2π radians)
function DataToColor:GetPlayerFacing()
    return GetPlayerFacing() or 0
end

-- Use Astrolabe function to get current player position
function DataToColor:GetCurrentPlayerPosition()
    local map = C_Map.GetBestMapForUnit(self.C.unitPlayer)
    if map ~= nil then
        local position = C_Map.GetPlayerMapPosition(map, self.C.unitPlayer)
        return position:GetXY()
    else
        return
    end
end

-- Base 2 converter for up to 24 boolean values to a single pixel square.
function DataToColor:Base2Converter()
    -- 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384
    return
    self:MakeIndexBase2(self:targetCombatStatus(), 0) +
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
    self:MakeIndexBase2(self:ProcessExitStatus(), 17) +
    self:MakeIndexBase2(self:IsPlayerMounted(), 18) +
    self:MakeIndexBase2(self:IsAutoRepeatSpellOn("Shoot"), 19) +
    self:MakeIndexBase2(self:IsCurrentSpell(6603), 20) + -- AutoAttack enabled
    self:MakeIndexBase2(self:targetIsNormal(), 21)+
    self:MakeIndexBase2(self:IsTagged(), 22)
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

-- Grabs current targets name
function DataToColor:GetTargetName(partition)
    if UnitExists(self.C.unitTarget) then
        local target = GetUnitName(self.C.unitTarget)
        target = self:StringToASCIIHex(target)
        if partition < 3 then
            return tonumber(string.sub(target, 0, 6))
        else if target > 999999 then
                return tonumber(string.sub(target, 7, 12))
            end
        end
    end
    return 0
end

function DataToColor:CastingInfoSpellId()
    local _, _, texture, _, _, _, _, _, spellID = CastingInfo()
    if spellID ~= nil then
        return spellID
    end
    if texture ~= nil then -- temp fix for tbc
        return texture
    end

    _, _, texture, _, _, _, _, spellID = ChannelInfo()
    if spellID ~= nil then
        return spellID
    end
    if texture ~= nil then -- temp fix for tbc
        return texture
    end

    return 0
end

--


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

local ammoSlot = GetInventorySlotInfo("AmmoSlot")
function DataToColor:hasAmmo()
    local ammoCount = GetInventoryItemCount(self.C.unitPlayer, ammoSlot)
    if ammoCount > 0 then
        return 1
    end
    return 0
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
        if CheckInteractDistance(self.C.unitTarget, 2) then
            return 1
        end
    end
    return 0
end

function DataToColor:targetNpcId()
    local _, _, _, _, _, npcID, guid = strsplit('-', UnitGUID(self.C.unitTarget) or '')
    if npcID ~= nil then
        return tonumber(npcID)
    end
    return 0
end

function DataToColor:getGuid(src)
    local _, _, _, _, _, npcID, spawnUID = strsplit('-', UnitGUID(src) or '')
    if npcID ~= nil then
        return self:uniqueGuid(npcID, spawnUID)
    end
    return 0
end

function DataToColor:getGuidFromUUID(uuid)
    local _, _, _, _, _, npcID, spawnUID = strsplit('-', uuid or '')
    return self:uniqueGuid(npcID, spawnUID)
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
    )
    return tonumber(num, 16)
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
            return self.C.MAX_POWER_TYPE * type + self.C.MAX_ACTION_IDX * slot + cost
        end
    end
    return self.C.MAX_ACTION_IDX * slot
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
    local itemCount
    _, itemCount, _, _, _, _, _ = GetContainerItemInfo(bag, slot)
    local value=0
    if itemCount ~= nil and itemCount > 0 then 
        local isSoulBound = C_Item.IsBound(ItemLocation:CreateFromBagAndSlot(bag,slot))
        if isSoulBound == true then value=1 end
    else
        value=2
    end
    return value
end

-- Returns item id from specific index in global items table
function DataToColor:returnItemFromIndex(index)
    return items[index]
end

function DataToColor:enchantedItems()
    if self:ValuesAreEqual(items, itemsPlaceholderComparison) then
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
    for i = 1, table.getn(self.S.spellInRangeList), 1 do
        local isInRange = IsSpellInRange(self.S.spellInRangeList[i], self.C.unitTarget)
        if isInRange==1 then
            inRange = inRange + (2 ^ (i - 1))
        end
    end
    return inRange
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
    local numskills = GetNumSkillLines()
    for c = 1, numskills do
        local skillname, _, _, skillrank = GetSkillLineInfo(c)
        if(skillname == skill) then
            return tonumber(skillrank)
        end
    end
    return 0
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

function DataToColor:ComboPoints()
    local points = GetComboPoints(self.C.unitPlayer, self.C.unitTarget)
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
    local classification = UnitClassification(self.C.unitTarget)
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
    local happiness, damagePercentage, loyaltyRate = GetPetHappiness()
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
