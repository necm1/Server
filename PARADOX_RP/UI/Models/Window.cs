﻿using Newtonsoft.Json;
using PARADOX_RP.Core.Factories;
using System;
using System.Collections.Generic;
using System.Text;

namespace PARADOX_RP.UI.Models
{
    class Window : IWindow
    {
        public bool Enabled { get; set; }
        public string WindowName { get; set; }

        public Window(string WindowName, bool Enabled = true)
        {
            this.WindowName = WindowName;
            this.Enabled = Enabled;
        }

        public void Show(PXPlayer player, params object[] windowObject)
        {
            player.Emit("openWindow", WindowName, windowObject);
        }
    }
}
