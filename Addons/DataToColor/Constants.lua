local Load = select(2, ...)
local DataToColor = unpack(Load)

DataToColor.C.unitPlayer = "player"
DataToColor.C.unitTarget = "target"
DataToColor.C.unitPet = "pet"
DataToColor.C.unitPetTarget = "pettarget"
DataToColor.C.unitTargetTarget = "targettarget"

-- Character's name
DataToColor.C.CHARACTER_NAME = UnitName(DataToColor.C.unitPlayer)
DataToColor.C.CHARACTER_GUID = UnitGUID(DataToColor.C.unitPlayer)
_, DataToColor.C.CHARACTER_CLASS = UnitClass(DataToColor.C.unitPlayer)

-- actionbar power cost
DataToColor.C.MAX_POWER_TYPE = 1000000
DataToColor.C.MAX_ACTION_IDX = 1000