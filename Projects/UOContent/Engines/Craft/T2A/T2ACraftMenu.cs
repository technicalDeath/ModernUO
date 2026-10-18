using System;
using Server.Menus.ItemLists;
using Server.Network;

namespace Server.Engines.Craft.T2A;

/// <summary>
/// The base of the T2A craft menus: an item-list menu that knows its craft system and, on a page of items, which craft item each entry makes.
/// <see cref="Sent"/> lets a shard follow a sent page with its own data about those items.
/// </summary>
public abstract class T2ACraftMenu : ItemListMenu
{
    protected T2ACraftMenu(string question, ItemListEntry[] entries, CraftSystem system, Type[] pageTypes, Type resourceType = null)
        : base(question, entries)
    {
        System = system;
        PageTypes = pageTypes;
        ResourceType = resourceType;
    }

    /// <summary>Raised after a craft menu page is sent: the client and the page.</summary>
    public static event Action<NetState, T2ACraftMenu> Sent;

    public CraftSystem System { get; }

    /// <summary>The item types this page's entries index into; null on a page of categories.</summary>
    public Type[] PageTypes { get; }

    /// <summary>The resource chosen for this page (an ingot colour), or null.</summary>
    public Type ResourceType { get; }

    /// <summary>The craft item an entry makes, or null for a category, repair or smelt entry.</summary>
    public virtual CraftItem ItemFor(ItemListEntry entry) =>
        PageTypes != null && entry.CraftIndex >= 0 && entry.CraftIndex < PageTypes.Length
            ? System.CraftItems.SearchFor(PageTypes[entry.CraftIndex])
            : null;

    public override void SendTo(NetState state)
    {
        base.SendTo(state);
        Sent?.Invoke(state, this);
    }
}
