﻿using PARADOX_RP.UI.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PARADOX_RP.UI
{
    interface IWindowManager
    {
        T Get<T>() where T : Window;
    }
}
