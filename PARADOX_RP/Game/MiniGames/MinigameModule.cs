﻿using AltV.Net.Resources.Chat.Api;
using PARADOX_RP.Core.Factories;
using PARADOX_RP.Core.Extensions;
using PARADOX_RP.Core.Module;
using PARADOX_RP.Game.Lobby;
using PARADOX_RP.Game.MiniGames.Interfaces;
using PARADOX_RP.Game.MiniGames.Models;
using PARADOX_RP.Utils.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace PARADOX_RP.Game.MiniGames
{
    class MinigameModule : ModuleBase<MinigameModule>
    {
        private readonly IEnumerable<IMinigame> _minigames;
        public MinigameModule(IEnumerable<IMinigame> minigames) : base("Minigame")
        {
            _minigames = minigames;
        }

        public void ChooseMinigame(PXPlayer player, MinigameTypes minigame)
        {
            if (player.Minigame != MinigameTypes.NONE)
            {
                player.SendNotification("Minigame", "Du bist bereits in einem Minigame.", NotificationTypes.ERROR);
                return;
            }

            IMinigame minigameInterface = _minigames.FirstOrDefault(i => i.MinigameType == minigame);
            if (minigameInterface == null) return;

            LobbyModel lobby = LobbyModule.Instance.RegisterLobby(player, 12);

            player.Minigame = minigame;
            player.Dimension = LobbyModule.Instance.GetDimensionByLobby(lobby);

            minigameInterface.EnteredMinigame(player);
        }

        [Command("minigame")]
        public void enterMinigameCommand(PXPlayer player, string minigameModule)
        {
            MinigameTypes _minigameType = Enum.Parse<MinigameTypes>(minigameModule);

            ChooseMinigame(player, _minigameType);
        }
    }
}