﻿using AltV.Net.Async;
using AltV.Net.Data;
using AltV.Net.Enums;
using Newtonsoft.Json;
using PARADOX_RP.Core.Database;
using PARADOX_RP.Core.Database.Models;
using PARADOX_RP.Core.Factories;
using PARADOX_RP.Core.Module;
using PARADOX_RP.UI;
using PARADOX_RP.UI.Windows;
using PARADOX_RP.Utils.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PARADOX_RP.Game.Login
{
    class LoginModule : ModuleBase<LoginModule>
    {
        public LoginModule() : base("Login") { }

        public override void OnModuleLoad()
        {

        }

        public override async void OnPlayerConnect(PXPlayer player)
        {
            WindowManager.Instance.Get<LoginWindow>().Show(player, JsonConvert.SerializeObject(new LoginWindowObject() { name = player.Name }));
            
            player.Model = (uint)PedModel.FreemodeMale01;
            await player.SpawnAsync(new Position(0, 0, 72));
        }

        public void RequestLoginResponse(PXPlayer player)
        {


            //SEND RESPONSE TO PLAYER
        }
    }
}
