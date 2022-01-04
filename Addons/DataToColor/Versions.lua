local Load = select(2, ...)
local DataToColor = unpack(Load)

function DataToColor.IsClassic()
    return WOW_PROJECT_ID == WOW_PROJECT_CLASSIC
end

function DataToColor.IsClassic_BCC()
    return WOW_PROJECT_ID == WOW_PROJECT_BURNING_CRUSADE_CLASSIC
end

function DataToColor.IsRetail()
    return WOW_PROJECT_ID == WOW_PROJECT_MAINLINE
end

local LibClassicCasterino
if DataToColor.IsClassic() then
  LibClassicCasterino = _G.LibStub("LibClassicCasterino")
end

if DataToColor.IsRetail() then
    DataToColor.UnitCastingInfo = UnitCastingInfo
  elseif DataToColor.IsClassic_BCC() then
    DataToColor.UnitCastingInfo = function(unit)
      local name, text, texture, startTimeMS, endTimeMS, isTradeSkill, castID, spellId = UnitCastingInfo(unit)
      return name, text, texture, startTimeMS, endTimeMS, isTradeSkill, castID, nil, spellId
    end
  else
    DataToColor.UnitCastingInfo = function(unit)
      if UnitIsUnit(unit, DataToColor.C.unitPlayer) then
        return CastingInfo()
      else
        return LibClassicCasterino:UnitCastingInfo(unit)
      end
    end
  end

  if DataToColor.IsRetail() then
    DataToColor.UnitChannelInfo = UnitChannelInfo
  elseif DataToColor.IsClassic_BCC() then
    DataToColor.UnitChannelInfo = function(unit)
      local name, text, texture, startTimeMS, endTimeMS, isTradeSkill, spellId = UnitChannelInfo(unit)
      return name, text, texture, startTimeMS, endTimeMS, isTradeSkill, nil, spellId
    end
  else
    DataToColor.UnitChannelInfo = function(unit)
      if UnitIsUnit(unit, DataToColor.C.unitPlayer) then
        return ChannelInfo()
      else
        return LibClassicCasterino:UnitChannelInfo(unit)
      end
    end
  end