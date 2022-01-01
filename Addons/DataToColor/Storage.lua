local Load = select(2, ...)
local DataToColor = unpack(Load)

DataToColor.S.spellInRangeList = {}

DataToColor.S.playerBuffs = {}
DataToColor.S.targetDebuffs = {}

function DataToColor:InitStorage()
    CreateSpellInRangeList()

    CreatePlayerBuffList()
    CreateTargetDebuffList()
end

function CreateSpellInRangeList()
    if DataToColor.C.CHARACTER_CLASS == "ROGUE" then
        DataToColor.S.spellInRangeList = {
            1752, -- "Sinister Strike"
            2764, -- "Throw"
            3018, -- "Shoot" for classic -> 7918, -- "Shoot Gun"
        }
    elseif DataToColor.C.CHARACTER_CLASS == "DRUID" then
        DataToColor.S.spellInRangeList = {
            5176, -- "Wrath"
            5211, -- "Bash"
            1079, -- "Rip"
            6807  -- "Maul"
        }
    elseif DataToColor.C.CHARACTER_CLASS == "WARRIOR" then
        DataToColor.S.spellInRangeList = {
            100,  -- "Charge"
            772,  -- "Rend"
            3018, -- "Shoot" for classic -> 7918, -- "Shoot Gun"
            2764  -- "Throw"
        }       
    elseif DataToColor.C.CHARACTER_CLASS == "PRIEST" then
        DataToColor.S.spellInRangeList = {
            589,   -- "Shadow Word: Pain"
            5019,  -- "Shoot"
            15407, -- "Mind Flay"
            8092,  -- "Mind Blast"
            585    -- "Smite"
        }
    elseif DataToColor.C.CHARACTER_CLASS == "PALADIN" then
        DataToColor.S.spellInRangeList = {
            20271 -- "Judgement"
        }
    elseif DataToColor.C.CHARACTER_CLASS == "MAGE" then
        DataToColor.S.spellInRangeList = {
            133,    -- "Fireball"
            5019,   -- "Shoot"
            11366,  -- "Pyroblast"
            116,    -- "Frostbolt"
            2136    -- "Fire Blast"
        }     
    elseif DataToColor.C.CHARACTER_CLASS == "HUNTER" then
        DataToColor.S.spellInRangeList = {
            2973, -- "Raptor Strike"
            75,   -- "Auto Shot"
            1978  -- "Serpent Sting"
        }      
    elseif DataToColor.C.CHARACTER_CLASS == "WARLOCK" then
        DataToColor.S.spellInRangeList = {
            686,  -- "Shadow Bolt",
            5019  -- "Shoot"
        }
    elseif DataToColor.C.CHARACTER_CLASS == "SHAMAN" then
        DataToColor.S.spellInRangeList = {
            403, -- "Lightning Bolt",
            8042 -- "Earth Shock"
        }
    end
end

function CreatePlayerBuffList()
    DataToColor.S.playerBuffs = {}
    DataToColor.S.playerBuffs[0] = { "Food", [134062]=1, [134032]=1, [133906]=1, [133984]=1 }
    DataToColor.S.playerBuffs[1] = { "Drink", [132794]=1, [132800]=1, [132805]=1, [132802]=1 }
    DataToColor.S.playerBuffs[2] = { "Well Fed", [136000]=1 }
    DataToColor.S.playerBuffs[3] = { "Mana Regeneration", [2]=1 } -- potion?
    DataToColor.S.playerBuffs[4] = { "Clearcasting", [136170]=1 } -- Druid / Mage / Shaman

    if DataToColor.C.CHARACTER_CLASS == "PRIEST" then
        DataToColor.S.playerBuffs[10] = { "Fortitude", [135987]=1, [135941]=1 }
        DataToColor.S.playerBuffs[11] = { "Inner Fire", [135926]=1 }
        DataToColor.S.playerBuffs[12] = { "Renew", [135953]=1 }
        DataToColor.S.playerBuffs[13] = { "Shield", [135940]=1 }
        DataToColor.S.playerBuffs[14] = { "Spirit", [1358982]=1, [135946]=1 }
    elseif DataToColor.C.CHARACTER_CLASS == "DRUID" then
        DataToColor.S.playerBuffs[10] = { "Mark of the Wild", [136078]=1 }
        DataToColor.S.playerBuffs[11] = { "Thorns", [136104]=1 }
        DataToColor.S.playerBuffs[12] = { "Fury", [132242]=1 }
        DataToColor.S.playerBuffs[13] = { "Prowl", [132089]=1 }
        DataToColor.S.playerBuffs[14] = { "Rejuvenation", [136081]=1 }
        DataToColor.S.playerBuffs[15] = { "Regrowth", [136085]=1 }
        DataToColor.S.playerBuffs[16] = { "Omen of Clarity", [136017]=1 }
    elseif DataToColor.C.CHARACTER_CLASS == "PALADIN" then
        DataToColor.S.playerBuffs[5] = { "Seal of Righteousness", [132325]=1 }
        DataToColor.S.playerBuffs[6] = { "Seal of the Crusader", [135924]=1 }
        DataToColor.S.playerBuffs[7] = { "Seal of Command", [132347]=1 }
        DataToColor.S.playerBuffs[8] = { "Seal of Wisdom", [135960]=1 }
        DataToColor.S.playerBuffs[9] = { "Seal of Light", [135917]=1 }
        DataToColor.S.playerBuffs[10] = { "Seal of Blood", [135961]=1 }
        DataToColor.S.playerBuffs[11] = { "Seal of Vengeance", [135969]=1 }
        DataToColor.S.playerBuffs[12] = { "Blessing of Might", [135906]=1, [135908]=1 }
        DataToColor.S.playerBuffs[13] = { "Blessing of Protection", [135964]=1 }
        DataToColor.S.playerBuffs[14] = { "Blessing of Wisdom", [135970]=1, [135912]=1 }
        DataToColor.S.playerBuffs[15] = { "Blessing of Kings", [135995]=1, [135993]=1 }
        DataToColor.S.playerBuffs[16] = { "Blessing of Salvation", [135967]=1, [135910]=1 }
        DataToColor.S.playerBuffs[17] = { "Blessing of Sanctuary", [136051]=1, [135911]=1 }
        DataToColor.S.playerBuffs[18] = { "Blessing of Light", [135943]=1, [135909]=1 }
        DataToColor.S.playerBuffs[19] = { "Righteous Fury", [135962]=1 }
        DataToColor.S.playerBuffs[20] = { "Divine Protection", [135954]=1 }
        DataToColor.S.playerBuffs[21] = { "Avenging Wrath", [135875]=1 }
        DataToColor.S.playerBuffs[22] = { "Holy Shield", [135880]=1 }
    elseif DataToColor.C.CHARACTER_CLASS == "MAGE" then
        DataToColor.S.playerBuffs[10] = { "Armor", [135843]=1, [135991]=1, [132221]=1 }
        DataToColor.S.playerBuffs[11] = { "Arcane Intellect", [135932]=1 }
        DataToColor.S.playerBuffs[12] = { "Ice Barrier", [135988]=1 }
        DataToColor.S.playerBuffs[13] = { "Ward", [135806]=1, [135850]=1 }
        DataToColor.S.playerBuffs[14] = { "Fire Power", [135817]=1 } -- not sure what is this
        DataToColor.S.playerBuffs[15] = { "Mana Shield", [136153]=1 }
        DataToColor.S.playerBuffs[16] = { "Presence of Mind", [136031]=1 }
        DataToColor.S.playerBuffs[17] = { "Arcane Power", [136048]=1 }
    elseif DataToColor.C.CHARACTER_CLASS == "ROGUE" then
        DataToColor.S.playerBuffs[10] = { "Slice and Dice", [132306]=1 }
        DataToColor.S.playerBuffs[11] = { "Stealth", [132320]=1 }
    elseif DataToColor.C.CHARACTER_CLASS == "WARRIOR" then
        DataToColor.S.playerBuffs[10] = { "Battle Shout", [132333]=1 }
        DataToColor.S.playerBuffs[11] = { "Bloodrage", [132277]=1 }
    elseif DataToColor.C.CHARACTER_CLASS == "WARLOCK" then
        DataToColor.S.playerBuffs[10] = { "Demon", [136185]=1 }
        DataToColor.S.playerBuffs[11] = { "Soul Link", [136160]=1 }
        DataToColor.S.playerBuffs[12] = { "Soulstone Resurrection", [136210]=1 }
        DataToColor.S.playerBuffs[13] = { "Shadow Trance", [136223]=1 }
    elseif DataToColor.C.CHARACTER_CLASS == "SHAMAN" then
        DataToColor.S.playerBuffs[10] = { "Lightning Shield", [136051]=1 }
        DataToColor.S.playerBuffs[11] = { "Water Shield", [132315]=1 }
        DataToColor.S.playerBuffs[12] = { "Focused", [136027]=1 } -- Shamanistic Focus
        DataToColor.S.playerBuffs[13] = { "Stoneskin", [136098]=1 }
    elseif DataToColor.C.CHARACTER_CLASS == "HUNTER" then
        DataToColor.S.playerBuffs[10] = { "Aspect of", [136076]=1, [132159]=1, [132252]=1, [132267]=1, [132160]=1, [136074]=1 }
        DataToColor.S.playerBuffs[11] = { "Rapid Fire", [132208]=1 }
        DataToColor.S.playerBuffs[12] = { "Quick Shots", [132347]=1 }
    end
end

function CreateTargetDebuffList()
    DataToColor.S.targetDebuffs = {}
    if DataToColor.C.CHARACTER_CLASS == "PRIEST" then 
        DataToColor.S.targetDebuffs[0] = { "Pain", [136207]=1 }
    elseif DataToColor.C.CHARACTER_CLASS == "DRUID" then
        DataToColor.S.targetDebuffs[0] = { "Roar", [132121]=1 }
        DataToColor.S.targetDebuffs[1] = { "Faerie Fire", [136033]=1 }
        DataToColor.S.targetDebuffs[2] = { "Rip", [132152]=1 }
        DataToColor.S.targetDebuffs[3] = { "Moonfire", [136096]=1 }
        DataToColor.S.targetDebuffs[4] = { "Entangling Roots", [136100]=1 }
        DataToColor.S.targetDebuffs[5] = { "Rake", [132122]=1 }
    elseif DataToColor.C.CHARACTER_CLASS == "PALADIN" then
        DataToColor.S.targetDebuffs[0] = { "Judgement of the Crusader", [135924]=1 }
        DataToColor.S.targetDebuffs[1] = { "Hammer of Justice", [135963]=1 }
    elseif DataToColor.C.CHARACTER_CLASS == "MAGE" then
        DataToColor.S.targetDebuffs[0] = { "Frostbite", [135842]=1 }
        DataToColor.S.targetDebuffs[1] = { "Slow", [136091]=1 }
    elseif DataToColor.C.CHARACTER_CLASS == "ROGUE" then
    elseif DataToColor.C.CHARACTER_CLASS == "WARRIOR" then
        DataToColor.S.targetDebuffs[0] = { "Rend", [132155]=1 }
        DataToColor.S.targetDebuffs[1] = { "Thunder Clap", [136105]=1 }
        DataToColor.S.targetDebuffs[2] = { "Hamstring", [132316]=1 }
        DataToColor.S.targetDebuffs[3] = { "Charge Stun", [135860]=1 }
    elseif DataToColor.C.CHARACTER_CLASS == "WARLOCK" then
        DataToColor.S.targetDebuffs[0] = { "Curse of", [136139]=1, [136122]=1, [136162]=1, [136225]=1, [136130]=1, [136140]=1, [136138]=1 }
        DataToColor.S.targetDebuffs[1] = { "Corruption", [136118]=1, [136193]=1 }
        DataToColor.S.targetDebuffs[2] = { "Immolate", [135817]=1 }
        DataToColor.S.targetDebuffs[3] = { "Siphon Life", [136188]=1 }
    elseif DataToColor.C.CHARACTER_CLASS == "HUNTER" then
        DataToColor.S.targetDebuffs[0] = { "Serpent Sting", [132204]=1 }
    end
end