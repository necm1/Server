﻿using AltV.Net.Data;
using PARADOX_RP.Core.Database.Models;
using PARADOX_RP.Core.Factories;
using PARADOX_RP.Game.Inventory.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PARADOX_RP.Game.Inventory.Interfaces
{
    public interface IInventoriable
    {
        Task<PXInventory> OnInventoryOpen(PXPlayer player, Position position);
        Task<bool?> CanAccessInventory(PXPlayer player, PXInventory inventory);
    }
}
