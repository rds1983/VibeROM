namespace VibeROM.Common.Rom;

public enum Sex
{
    Neutral = 0,
    Male = 1,
    Female = 2
}

public enum Position
{
    Dead = 0,
    Mortal = 1,
    Incap = 2,
    Stunned = 3,
    Sleeping = 4,
    Resting = 5,
    Sitting = 6,
    Fighting = 7,
    Standing = 8
}

public enum Size
{
    Tiny = 0,
    Small = 1,
    Medium = 2,
    Large = 3,
    Huge = 4,
    Giant = 5
}

public enum Sector
{
    Inside = 0,
    City = 1,
    Field = 2,
    Forest = 3,
    Hills = 4,
    Mountain = 5,
    WaterSwim = 6,
    WaterNoSwim = 7,
    Air = 9,
    Desert = 10
}

public enum ItemType
{
    Light = 1,
    Scroll = 2,
    Wand = 3,
    Staff = 4,
    Weapon = 5,
    Treasure = 8,
    Armor = 9,
    Potion = 10,
    Clothing = 11,
    Furniture = 12,
    Trash = 13,
    Container = 15,
    DrinkContainer = 17,
    Key = 18,
    Food = 19,
    Money = 20,
    Boat = 22,
    NpcCorpse = 23,
    PcCorpse = 24,
    Fountain = 25,
    Pill = 26,
    Protect = 27,
    Map = 28,
    Portal = 29,
    WarpStone = 30,
    RoomKey = 31,
    Gem = 32,
    Jewelry = 33,
    Jukebox = 34
}

public enum WeaponClass
{
    Exotic = 0,
    Sword = 1,
    Dagger = 2,
    Spear = 3,
    Mace = 4,
    Axe = 5,
    Flail = 6,
    Whip = 7,
    Polearm = 8
}

public enum ApplyLocation
{
    None = 0,
    Strength = 1,
    Dexterity = 2,
    Intelligence = 3,
    Wisdom = 4,
    Constitution = 5,
    Sex = 6,
    Class = 7,
    Level = 8,
    Age = 9,
    Height = 10,
    Weight = 11,
    Mana = 12,
    Hit = 13,
    Move = 14,
    Gold = 15,
    Experience = 16,
    ArmorClass = 17,
    HitRoll = 18,
    DamageRoll = 19,
    Saves = 20,
    SavingRod = 21,
    SavingPetrification = 22,
    SavingBreath = 23,
    SavingSpell = 24,
    SpellAffect = 25
}

public enum SkillTarget
{
    Ignore = 0,
    CharOffensive = 1,
    CharDefensive = 2,
    CharSelf = 3,
    ObjectInventory = 4,
    ObjectOrCharDefensive = 5,
    ObjectOrCharOffensive = 6
}

public enum PlayerClass
{
    Mage = 0,
    Cleric = 1,
    Thief = 2,
    Warrior = 3
}
