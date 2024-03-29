﻿using AltV.Net;
using AltV.Net.Async;
using Microsoft.EntityFrameworkCore;
using PARADOX_RP.Core.Database;
using PARADOX_RP.Core.Database.Models;
using PARADOX_RP.Core.Factories;
using PARADOX_RP.Game.Inventory;
using PARADOX_RP.Game.Moderation;
using PARADOX_RP.Controllers.Login.Interface;
using PARADOX_RP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PARADOX_RP.Game.Login.Extensions;
using PARADOX_RP.Game.Arrival;
using PARADOX_RP.Utils.Enums;
using PARADOX_RP.Game.Clothing;
using PARADOX_RP.Controllers.Inventory;
using PARADOX_RP.Controllers.Weapon.Interface;
using AltV.Net.Elements.Entities;
using PARADOX_RP.Utils.Callbacks;
using Newtonsoft.Json;
using PARADOX_RP.Game.Char.Models;

namespace PARADOX_RP.Controllers.Login
{
    public enum LoadPlayerResponse
    {
        ABORT,
        NEW_PLAYER,
        SUCCESS
    }

    class LoginController : ILoginController
    {
        private IWeaponController _weaponController;
        private IInventoryController _inventoryController;

        public LoginController(IWeaponController weaponController, IInventoryController inventoryController)
        {
            _weaponController = weaponController;
            _inventoryController = inventoryController;
        }

        public async Task<bool> CheckLogin(PXPlayer player, string userName, string hashedPassword)
        {
            await using (var px = new PXContext())
            {
                Players dbPlayer = await px.Players
                                       .FirstOrDefaultAsync(p => p.Username == userName);

                if (dbPlayer == null)
                {
                    await player.EmitAsync("ResponseLoginStatus", "Charakter nicht gefunden!");
                    return await Task.FromResult(false);
                }

                try
                {
                    if (BCrypt.Net.BCrypt.Verify(hashedPassword, dbPlayer.Password))
                    {
                        await player.EmitAsync("ResponseLoginStatus", "Anmeldevorgang erfolgreich, lade Daten...");
                        return await Task.FromResult(true);
                    }
                }
                catch (BCrypt.Net.SaltParseException)
                {
                    if (Configuration.Instance.DevMode) Alt.Log($"[DEVMODE] {dbPlayer.Username} threw SaltParseException.");
                    await player.EmitAsync("ResponseLoginStatus", "Fehler, bitte der Entwicklung melden!");
                    return await Task.FromResult(false);
                }
            }

            await player.EmitAsync("ResponseLoginStatus", "Das angegebene Passwort stimmt nicht überein.");
            return await Task.FromResult(false);
        }

        public async Task<LoadPlayerResponse> LoadPlayer(PXPlayer player, string userName)
        {
            try
            {
                await using var px = new PXContext();
                Players dbPlayer = await px.Players
                                                    .Include(p => p.SupportRank).ThenInclude(p => p.PermissionAssignments).ThenInclude(p => p.Permission)
                                                    .Include(p => p.PlayerClothes).ThenInclude(p => p.Clothing)
                                                    .Include(p => p.PlayerTeamData)
                                                    .Include(p => p.Team)
                                                    .Include(p => p.PlayerCustomization)
                                                    .Include(p => p.PlayerInjuryData).ThenInclude(p => p.Injury)
                                                    .Include(p => p.PlayerWeapons)
                                                    .Include(p => p.PlayerBankHistory)
                                                    .FirstOrDefaultAsync(p => p.Username == userName);

                if (dbPlayer == null) return await Task.FromResult(LoadPlayerResponse.ABORT);


                player.LoggedIn = true;

                player.SqlId = dbPlayer.Id;
                player.Username = dbPlayer.Username;
                player.SupportRank = dbPlayer.SupportRank;
                player.Money = dbPlayer.Money;
                player.BankMoney = dbPlayer.BankMoney;
                player.Team = dbPlayer.Team;

                player.PlayerBankHistory = dbPlayer.PlayerBankHistory;

                /* New-Player Generation */

                if (dbPlayer.PlayerTeamData.FirstOrDefault() == null)
                {
                    var playerTeamDataInsert = new PlayerTeamData()
                    {
                        PlayerId = dbPlayer.Id,
                        Joined = DateTime.Now,
                        Rank = 0,
                        Payday = 0
                    };

                    await px.PlayerTeamData.AddAsync(playerTeamDataInsert);
                    await px.SaveChangesAsync();

                    player.PlayerTeamData = playerTeamDataInsert;
                }
                else
                {
                    player.PlayerTeamData = dbPlayer.PlayerTeamData.FirstOrDefault();
                }

                // PlayerInjuryData
                if (dbPlayer.PlayerInjuryData.FirstOrDefault() == null)
                {
                    var playerInjuryDataInsert = new PlayerInjuryData()
                    {
                        PlayerId = dbPlayer.Id,
                        InjuryId = 1,
                        InjuryTimeLeft = 0
                    };

                    await px.PlayerInjuryData.AddAsync(playerInjuryDataInsert);
                    await px.SaveChangesAsync();

                    player.PlayerInjuryData = playerInjuryDataInsert;
                }
                else
                {
                    player.PlayerInjuryData = dbPlayer.PlayerInjuryData.FirstOrDefault();
                }

                if (await ModerationModule.Instance.IsBanned(player))
                {
                    await player.KickAsync("Du bist gebannt. Für weitere Informationen melde dich im Support!");
                    return await Task.FromResult(LoadPlayerResponse.ABORT);
                }

                player.Inventory = await _inventoryController.LoadInventory(InventoryTypes.PLAYER, player.SqlId);
                if (player.Inventory == null)
                    player.Inventory = await _inventoryController.CreateInventory(InventoryTypes.PLAYER, player.SqlId);

                try
                {
                    Pools.Instance.Register(player.SqlId, player);
                }
                catch { Alt.Log("pool"); }

                await player?.EmitAsync("ResponseLoginStatus", "Erfolgreich eingeloggt!");

                if (dbPlayer.PlayerCustomization.FirstOrDefault() == null)
                    return await Task.FromResult(LoadPlayerResponse.NEW_PLAYER);

                await player?.EmitAsync("ApplyPlayerCharacter", dbPlayer.PlayerCustomization.FirstOrDefault().Customization);
                player.Customization = JsonConvert.DeserializeObject<CharacterCustomizationModel>(dbPlayer.PlayerCustomization.FirstOrDefault().Customization);

                Dictionary<ComponentVariation, ClothesVariants> wearingClothes = new Dictionary<ComponentVariation, ClothesVariants>();
                foreach (PlayerClothesWearing playerClothesWearing in dbPlayer.PlayerClothes)
                {
                    wearingClothes[playerClothesWearing.ComponentVariation] = playerClothesWearing.Clothing;
                    await player?.SetClothes((int)playerClothesWearing.ComponentVariation, playerClothesWearing.Clothing.Drawable, playerClothesWearing.Clothing.Texture);
                }

                await _weaponController.LoadWeapons(player, dbPlayer.PlayerWeapons);

                player.Clothes = wearingClothes;
                await player?.PreparePlayer(dbPlayer.Position);

                return await Task.FromResult(LoadPlayerResponse.SUCCESS);
            }
            catch (Exception e) { Alt.Log($"Failed to load player {userName} | {e.Message} {e.StackTrace}"); }
            return await Task.FromResult(LoadPlayerResponse.ABORT);
        }

        public async Task SavePlayers()
        {
            await Alt.ForEachPlayers(new AsyncFunctionCallback<IPlayer>(async (basePlayer) =>
             {
                 if (!(basePlayer is PXPlayer player))
                 {
                     return;
                 }

                 if (!player.LoggedIn) return;

                 await using (var px = new PXContext())
                 {
                     Players dbPlayer = await px.Players.FindAsync(player.SqlId);
                     if (dbPlayer == null) return;

                     if (player.Dimension == 0)
                     {
                         dbPlayer.Position_X = player.Position.X;
                         dbPlayer.Position_Y = player.Position.Y;
                         dbPlayer.Position_Z = player.Position.Z;
                     }

                     await px.SaveChangesAsync();
                 }
             }));
        }
    }
}
