﻿using AltV.Net;
using AltV.Net.Async;
using AltV.Net.Elements.Entities;
using PARADOX_RP.Controllers.Event;
using PARADOX_RP.Core.Factories;
using PARADOX_RP.Utils;
using PARADOX_RP.Utils.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PARADOX_RP.Core.Module
{
    public abstract class ModuleBase : IModuleBase
    {
        public bool Enabled { get; set; }
        public string ModuleName { get; set; }

        public IEnumerable<T> LoadDatabaseTable<T>(IQueryable queryable, Action<T> action = null) where T : class
        {
            try
            {
                if (queryable == null) return null;

                List<T> items = new List<T>();
                foreach (T item in queryable)
                {
                    if (item == null) continue;
                    action.Invoke(item);
                    items.Add(item);
                }

                return items;
            }
            catch (Exception e) { Alt.Log(e.Message); }

            return null;
        }
    }

    public abstract class Module<T> : ModuleBase where T : Module<T>
    {
        public static T Instance { get; set; }

        public Module(string ModuleName, bool Enabled = true)
        {
            this.ModuleName = ModuleName;
            this.Enabled = Enabled;

            Instance = (T)this;
        }
    }
}
