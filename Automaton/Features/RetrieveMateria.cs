﻿using Automaton.Tasks;
using Dalamud.Game.Gui.ContextMenu;

namespace Automaton.Features;
[Tweak]
internal class RetrieveMateria : Tweak
{
    public override string Name => "Retrieve All Materia";
    public override string Description => "Adds a context menu item that will retrieve all materia from an item.";

    public override void Enable() => Svc.ContextMenu.OnMenuOpened += OnOpenContextMenu;
    public override void Disable() => Svc.ContextMenu.OnMenuOpened -= OnOpenContextMenu;

    private void OnOpenContextMenu(IMenuOpenedArgs args)
    {
        if (args.MenuType != ContextMenuType.Inventory || args.Target is not MenuTargetInventory inv || inv.TargetItem == null || inv.TargetItem.Value.ItemId == 0) return;
        args.AddMenuItem(new MenuItem
        {
            PrefixChar = 'C',
            Name = "Retrieve All Materia",
            OnClicked = (a) => P.Automation.Start(new RetrieveAllMateria(inv.TargetItem.Value)),
            IsEnabled = inv.TargetItem.Value.Materia.ToArray().Any(m => m != 0) && !PlayerEx.IsBusy,
        });
    }
}
