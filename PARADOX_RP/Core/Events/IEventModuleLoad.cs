﻿using System;
using System.Collections.Generic;
using System.Text;

namespace PARADOX_RP.Core.Events
{
    interface IEventModuleLoad
    {
        bool Enabled { get; }
        void OnModuleLoad();
    }
}
