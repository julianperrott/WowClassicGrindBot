local Load = select(2, ...)
local DataToColor = unpack(Load)

local ignoreErrorList = {
    "ERR_ABILITY_COOLDOWN",
    "ERR_OUT_OF_RAGE",
    "ERR_NO_ATTACK_TARGET",
    "ERR_OUT_OF_MANA",
    "ERR_SPELL_FAILED_ANOTHER_IN_PROGRESS", 
    "ERR_SPELL_COOLDOWN", 
    "ERR_SPELL_FAILED_SHAPESHIFT_FORM_S",
    "ERR_GENERIC_NO_TARGET",
    "ERR_ATTACK_PREVENTED_BY_MECHANIC_S",
    "ERR_ATTACK_STUNNED",
    "ERR_NOEMOTEWHILERUNNING",
}

local errorList = {
    "ERR_BADATTACKFACING", --1
    "ERR_SPELL_FAILED_S", --2
    "ERR_SPELL_OUT_OF_RANGE", --3
    "ERR_BADATTACKPOS", --4
    "ERR_AUTOFOLLOW_TOO_FAR", --5
};

function DataToColor:OnUIErrorMessage(event, messageType, message)
    local errorName = GetGameMessageInfo(messageType)

    local foundMessage=false;
    for i = 1, table.getn(ignoreErrorList), 1 do
        if ignoreErrorList[i]==errorName then
            foundMessage=true;
            UIErrorsFrame:AddMessage(message, 0.7, 0.7, 0.7) -- show as grey messasge
        end
    end

    if not foundMessage then
        for i = 1, table.getn(errorList), 1 do
            if errorList[i]==errorName then
                self.uiErrorMessage = i;
                foundMessage=true;
                UIErrorsFrame:AddMessage(message, 0, 1, 0) -- show as green messasge
            end
        end
    end

    if not foundMessage then
        UIErrorsFrame:AddMessage(message, 0, 0, 1) -- show as blue message (unknown message)
    end
end

function DataToColor:OnCombatEvent(event)
    local timestamp, eventType, _, sourceGUID, sourceName, _, _, destGUID, destName, _, _, spellId, spellName, spellSchool = CombatLogGetCurrentEventInfo();
    --print(CombatLogGetCurrentEventInfo())
    if eventType=="SPELL_PERIODIC_DAMAGE" then
        self.lastCombatCreature=0;
    elseif string.find(sourceGUID, "Creature") then
        self.lastCombatCreature = self:getGuidFromUUID(sourceGUID);
        self.lastCombatDamageDealerCreature = self.lastCombatCreature;
        --print(sourceGUID.." "..lastCombatCreature);
    else
        self.lastCombatCreature=0;
        --print("Other "..eventType);
    end

    if eventType=="UNIT_DIED" then
        if string.find(destGUID, "Creature") then
            self.lastCombatCreatureDied = self:getGuidFromUUID(destGUID);
            --print("v_killing blow " .. destGUID .. " " .. lastCombatCreatureDied .. " " .. destName)
        else
            --print("i_killing blow " .. destGUID .. " " .. destName)
        end
    end
end

function DataToColor:OnLootClosed(event)
    self.lastLoot = self.globalTime
    --self:Print(lastLoot)
end

function DataToColor:OnMerchantShow(event)
    
    TotalPrice = 0
    for myBags = 0,4 do
        for bagSlots = 1, GetContainerNumSlots(myBags) do
            CurrentItemLink = GetContainerItemLink(myBags, bagSlots)
                if CurrentItemLink then
                    _, _, itemRarity, _, _, _, _, _, _, _, itemSellPrice = GetItemInfo(CurrentItemLink)
                    _, itemCount = GetContainerItemInfo(myBags, bagSlots)
                    if itemRarity == 0 and itemSellPrice ~= 0 then
                        TotalPrice = TotalPrice + (itemSellPrice * itemCount);
                        self:Print("Selling: "..itemCount.." "..CurrentItemLink.." for "..GetCoinTextureString(itemSellPrice * itemCount));
                        UseContainerItem(myBags, bagSlots)
                    end
                end
        end
    end
    if TotalPrice ~= 0 then
        self:Print("Total Price for all items: " .. GetCoinTextureString(TotalPrice))
    else
        self:Print("No grey items were sold.")
    end
end