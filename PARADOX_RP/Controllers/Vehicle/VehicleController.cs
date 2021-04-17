﻿using AltV.Net.Async;
using AltV.Net.Data;
using PARADOX_RP.Controllers.Vehicle.Interface;
using PARADOX_RP.Core.Database.Models;
using PARADOX_RP.Core.Factories;
using PARADOX_RP.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PARADOX_RP.Controllers.Vehicle
{
    public class VehicleController : IVehicleController
    {
        public async Task<PXVehicle> CreateVehicle(Vehicles dbVehicle)
        {
            PXVehicle vehicle = (PXVehicle)await AltAsync.CreateVehicle(dbVehicle.VehicleModel, dbVehicle.Position, dbVehicle.Rotation);
            vehicle.SqlId = dbVehicle.Id;
            vehicle.OwnerId = dbVehicle.PlayerId;

            Pools.Instance.Register(dbVehicle.Id, vehicle);

            return await Task.FromResult(vehicle);
        }
    }
}
