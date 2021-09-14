local Load = select(2, ...)
local DataToColor = unpack(Load)

DataToColor.S.PlayerClass = 0
DataToColor.S.spellInRangeList = {}

DataToColor.S.playerBuffs = {}
DataToColor.S.targetDebuffs = {}

function DataToColor:InitStorage()
    CreatePlayerClass(self)
    CreateSpellInRangeList(self)

    CreatePlayerBuffList(self)
    CreateTargetDebuffList(self)
end

function CreatePlayerClass(self)
    -- UnitClass returns class and the class in uppercase e.g. "Mage" and "MAGE"
    if self.C.CHARACTER_CLASS == "MAGE" then
        self.S.PlayerClass = 128
    elseif self.C.CHARACTER_CLASS == "ROGUE" then
        self.S.PlayerClass = 64
    elseif self.C.CHARACTER_CLASS == "WARRIOR" then
        self.S.PlayerClass = 32
    elseif self.C.CHARACTER_CLASS == "PALADIN" then
        self.S.PlayerClass = 16
    elseif self.C.CHARACTER_CLASS == "HUNTER" then
        self.S.PlayerClass = 8
    elseif self.C.CHARACTER_CLASS == "PRIEST" then
        self.S.PlayerClass = 4
    elseif self.C.CHARACTER_CLASS == "SHAMAN" then
        self.S.PlayerClass = 2
    elseif self.C.CHARACTER_CLASS == "WARLOCK" then
        self.S.PlayerClass = 1    
    elseif self.C.CHARACTER_CLASS == "DRUID" then
        self.S.PlayerClass = 256
    else
        self.S.PlayerClass = 0
    end
end

function CreateSpellInRangeList(self)
    if self.C.CHARACTER_CLASS == "ROGUE" then
        self.S.spellInRangeList = {
            "Sinister Strike", --1
            "Throw", --2
            "Shoot Gun" --4
        }
    elseif self.C.CHARACTER_CLASS == "DRUID" then
        self.S.spellInRangeList = {
            "Wrath", --1
            "Bash", --2
            "Rip" --3
        }
    elseif self.C.CHARACTER_CLASS == "WARRIOR" then
        self.S.spellInRangeList = {
            "Charge", --1
            "Rend", --2
            "Shoot Gun", --4
        }       
    elseif self.C.CHARACTER_CLASS == "PRIEST" then
        self.S.spellInRangeList = {
            "Shadow Word: Pain", --1
            "Mind Blast", --2
            "Mind Flay", --4
            "Shoot", --8
        }
    elseif self.C.CHARACTER_CLASS == "PALADIN" then
        self.S.spellInRangeList = {
            "Judgement" --1
        }
    elseif self.C.CHARACTER_CLASS == "MAGE" then
        self.S.spellInRangeList = {
            "Fireball", --1
            "Shoot",
            "Pyroblast",
            "Frostbolt",
            "Fire Blast"
        }     
    elseif self.C.CHARACTER_CLASS == "HUNTER" then
        self.S.spellInRangeList = {
            "Raptor Strike", --1
            "Auto Shot", --2
            "Serpent Sting" --3
        }      
    elseif self.C.CHARACTER_CLASS == "WARLOCK" then
        self.S.spellInRangeList = {
            "Shadow Bolt",
            "Shoot"
        }
    elseif self.C.CHARACTER_CLASS == "SHAMAN" then
        self.S.spellInRangeList = {
            "Lightning Bolt",
            "Earth Shock"
        }
    end
end

function CreatePlayerBuffList(self)
    self.S.playerBuffs = {}
    self.S.playerBuffs[0] = "Food"
    self.S.playerBuffs[1] = "Drink"
    self.S.playerBuffs[2] = "Well Fed"
    self.S.playerBuffs[3] = "Mana Regeneration"

    if self.C.CHARACTER_CLASS == "PRIEST" then
        self.S.playerBuffs[10] = "Fortitude"
        self.S.playerBuffs[11] = "Inner Fire"
        self.S.playerBuffs[12] = "Renew"
        self.S.playerBuffs[13] = "Shield"
        self.S.playerBuffs[14] = "Spirit"
    elseif self.C.CHARACTER_CLASS == "DRUID" then
        self.S.playerBuffs[10] = "Mark of the Wild"
        self.S.playerBuffs[11] = "Thorns"
        self.S.playerBuffs[12] = "Fury"
    elseif self.C.CHARACTER_CLASS == "PALADIN" then
        self.S.playerBuffs[10] = "Aura"
        self.S.playerBuffs[11] = "Blessing"
        self.S.playerBuffs[12] = "Seal"
    elseif self.C.CHARACTER_CLASS == "MAGE" then
        self.S.playerBuffs[10] = "Armor"
        self.S.playerBuffs[11] = "Arcane Intellect"
        self.S.playerBuffs[12] = "Ice Barrier"
        self.S.playerBuffs[13] = "Ward"
        self.S.playerBuffs[14] = "Fire Power"
        self.S.playerBuffs[15] = "Mana Shield"
    elseif self.C.CHARACTER_CLASS == "ROGUE" then
        self.S.playerBuffs[10] = "Slice and Dice"
    elseif self.C.CHARACTER_CLASS == "WARRIOR" then
        self.S.playerBuffs[10] = "Battle Shout"
    elseif self.C.CHARACTER_CLASS == "WARLOCK" then
        self.S.playerBuffs[10] = "Demon"
        self.S.playerBuffs[11] = "Soul Link"
        self.S.playerBuffs[12] = "Soulstone Resurrection"
        self.S.playerBuffs[13] = "Shadow Trance"
    elseif self.C.CHARACTER_CLASS == "SHAMAN" then
        self.S.playerBuffs[10] = "Lightning Shield"
    elseif self.C.CHARACTER_CLASS == "HUNTER" then
        self.S.playerBuffs[10] = "Aspect of"
        self.S.playerBuffs[11] = "Rapid Fire"
        self.S.playerBuffs[12] = "Quick Shots"
    end
end

function CreateTargetDebuffList(self)
    self.S.targetDebuffs = {}
    if self.C.CHARACTER_CLASS == "PRIEST" then 
        self.S.targetDebuffs[0] = "Pain"
    elseif self.C.CHARACTER_CLASS == "DRUID" then
        self.S.targetDebuffs[0] = "Roar"
        self.S.targetDebuffs[1] = "Faerie Fire"
        self.S.targetDebuffs[2] = "Rip"
    elseif self.C.CHARACTER_CLASS == "PALADIN" then
    elseif self.C.CHARACTER_CLASS == "MAGE" then
        self.S.targetDebuffs[0] = "Frostbite"
    elseif self.C.CHARACTER_CLASS == "ROGUE" then
    elseif self.C.CHARACTER_CLASS == "WARRIOR" then
        self.S.targetDebuffs[0] = "Rend"
    elseif self.C.CHARACTER_CLASS == "WARLOCK" then
        self.S.targetDebuffs[0] = "Curse of"
        self.S.targetDebuffs[1] = "Corruption"
        self.S.targetDebuffs[2] = "Immolate"
        self.S.targetDebuffs[3] = "Siphon Life"
    elseif self.C.CHARACTER_CLASS == "HUNTER" then
        self.S.targetDebuffs[0] = "Serpect Sting"
    end
end