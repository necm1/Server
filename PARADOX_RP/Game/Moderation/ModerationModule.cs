﻿using AltV.Net.Async;
using Microsoft.EntityFrameworkCore;
using PARADOX_RP.Core.Database;
using PARADOX_RP.Core.Database.Models;
using PARADOX_RP.Core.Factories;
using PARADOX_RP.Core.Module;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PARADOX_RP.Game.Moderation
{
    class ModerationModule : Module<ModerationModule>
    {
        public ModerationModule() : base("Moderation") { }

        public async Task<bool> IsBanned(PXPlayer player)
        {
            if (player == null) return await Task.FromResult(true);

            await using (var px = new PXContext())
            {
                BanList existingBanEntry = await px.BanList.FirstOrDefaultAsync(e => e.PlayerId == player.SqlId);
                if (existingBanEntry != null && existingBanEntry.Active) return await Task.FromResult(true);
            }

            return await Task.FromResult(false);
        }

        public async Task BanPlayer(PXPlayer player, PXPlayer moderator)
        {
            if (player == null || moderator == null) return;

            await using (var px = new PXContext())
            {
                BanList existingBanEntry = await px.BanList.FirstOrDefaultAsync(e => e.PlayerId == player.SqlId);
                if (existingBanEntry != null) existingBanEntry.Active = true;
                else
                {
                    BanList banEntry = new BanList(player.SqlId, moderator.SqlId, true, DateTime.Now);
                    await px.BanList.AddAsync(banEntry);
                }

                await px.SaveChangesAsync();
            }

            await player.KickAsync("Du wurdest gebannt. Für weitere Informationen melde dich im Support!");
        }

        public async Task BanPlayer(PXPlayer player, string Description = "System", [CallerMemberName] string callerName = null)
        {
            if (player == null) return;

            await using (var px = new PXContext())
            {
                BanList existingBanEntry = await px.BanList.FirstOrDefaultAsync(e => e.PlayerId == player.SqlId);
                if (existingBanEntry != null) existingBanEntry.Active = true;
                else
                {
                    BanList banEntry = new BanList(player.SqlId, 1, $"{Description} via {callerName}", true, DateTime.Now);
                    await px.BanList.AddAsync(banEntry);
                }

                await px.SaveChangesAsync();
            }

            await player.KickAsync("Du wurdest gebannt. Für weitere Informationen melde dich im Support!");
        }
    }
}
