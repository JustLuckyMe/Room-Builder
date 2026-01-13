#region Type
// Types
public enum Type { Seating, Surfaces, Storage, Beds, Lighting, Appliances, Decor }

public enum SeatingSubType { Chair, Armchair, Sofa, Bench, Stool, Other }
public enum SurfacesSubType { DiningTable, CoffeeTable, SideTable, Desk, Other }
public enum StorageSubType { Cabinet, Shelf, Wardrobe, Drawer, Other }
public enum BedsSubType { SingleBed, DoubleBed, BunkBed, Other }
public enum LightingSubType { CeilingLight, WallLight, FloorLamp, TableLamp, Other }
public enum AppliancesSubType { BigAppliances, SmallAppliances, Other }
public enum DecorSubType { Rug, Plant, WallArt, SmallDecor, Other }

#endregion
#region Style
[System.Flags]
public enum StyleType
{
    None = 0,
    Modern = 1 << 0,
    Contemporary = 1 << 1,
    Minimalist = 1 << 2,
    Scandinavian = 1 << 3,
    Industrial = 1 << 4,
    Rustic = 1 << 5,
    Boho = 1 << 6,
    Traditional = 1 << 7,
    Classic = 1 << 8,
    MidCentury = 1 << 9,
    Farmhouse = 1 << 10,
    Vintage = 1 << 11,
    ArtDeco = 1 << 12,
    Retro = 1 << 13,
    Luxury = 1 << 14,
    Urban = 1 << 15
}
#endregion
#region Color
public enum ColorType { SingleColor, MultiColor }
public enum SingleColor { Red, Orange, Yellow, Green, Blue, Purple, Pink, Black, White }
[System.Flags]
public enum MultiColor
{
    None = 0,
    Red = 1 << 0,
    Orange = 1 << 1,
    Yellow = 1 << 2,
    Green = 1 << 3,
    Blue = 1 << 4,
    Purple = 1 << 5,
    Pink = 1 << 6,
    Black = 1 << 7,
    White = 1 << 8,
}
#endregion