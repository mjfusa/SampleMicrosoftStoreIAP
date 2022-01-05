using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSIAPHelper.Models
{
    // InGameItem purchased with in-game currency - not tracked by Store
    //
    public interface IInGameItem
    {
        Guid GameId { get; } // Created once and master collection registered (saved to db)
        ItemType Category { get; } // i.e. Weapons
        string Name { get; } // i.e. Sword
        string Description { get; } // i.e. Defend your kingdom with this mighty sword
        Uri ImageUri { get; } // i.e. Image location hosted by service
        uint Cost { get; } // i.e. Price in in-game Currency
        CurrencyType ItemCurrencyType { get; } // i.e. Type of currency to buy this item
        bool IsInUserCollection { get; set; } // read / write. Updated after user purchase.
    }

    public enum CurrencyType
    {
        Coin,
        Gold,
        Diamond,
        Saphire
    }
    public enum ItemType
    {
        Tool, // e.g. Sword, Knife
        Protection, // e.g. Armor, damage resistance potion,
        CharacterAttribute,  // e.g. Strength, Luck, Intelligence
        Service  // e.g. Remove ads
    }
}
