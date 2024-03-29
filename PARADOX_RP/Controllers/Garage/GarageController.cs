﻿using AltV.Net.Async;
using Microsoft.EntityFrameworkCore;
using PARADOX_RP.Controllers.Garage.Interface;
using PARADOX_RP.Core.Database;
using PARADOX_RP.Core.Database.Models;
using PARADOX_RP.Core.Extensions;
using PARADOX_RP.Core.Factories;
using PARADOX_RP.Game.Vehicle;
using PARADOX_RP.UI.Windows;
using PARADOX_RP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PARADOX_RP.Controllers.Garage
{
    class GarageController : IGarageController
    {
        public async Task<GarageSpawns> GetFreeGarageSpawn(Garages garage)
        {
            GarageSpawns _freeSpawn = null;

            await garage.Spawns.ForEach(async spawn =>
            {
                PXVehicle vehicle = Pools.Instance.Get<PXVehicle>(PoolType.VEHICLE).FirstOrDefault(veh => veh.Position.Distance(spawn.Spawn_Position) < 3);
                if (vehicle == null)
                {
                    _freeSpawn = spawn;
                    return await Task.FromResult(false);
                }

                return await Task.FromResult(true);
            });

            return _freeSpawn;
        }

        public async Task<GarageWindowWriter> RequestGarageVehicles(PXPlayer player, Garages garage)
        {
            PXVehicle tmpNearestVehicle = Pools.Instance.Get<PXVehicle>(PoolType.VEHICLE).FirstOrDefault(v => v.OwnerId == player.SqlId && (v.Position.Distance(garage.Position) < 28));
            VehicleClass vehicleClass = VehicleModule.Instance._vehicleClass.FirstOrDefault(v => v.Value.VehicleModel == tmpNearestVehicle?.VehicleModel).Value;

            GarageWindowVehicle NearestVehicle = tmpNearestVehicle == null ? null : new GarageWindowVehicle(tmpNearestVehicle.SqlId, tmpNearestVehicle.VehicleModel, tmpNearestVehicle.Fuel, vehicleClass.MaxFuel);

            await using (var px = new PXContext())
            {
                List<GarageWindowVehicle> Vehicles = new List<GarageWindowVehicle>();
                await px.Vehicles.Where(v => v.GarageId == garage.Id && v.PlayerId == player.SqlId && v.Parked).Include(vC => vC.VehicleClass).ForEachAsync((v) =>
                {
                    Vehicles.Add(new GarageWindowVehicle(v.Id, v.VehicleClass.VehicleModel, v.Fuel, v.VehicleClass.MaxFuel));
                });

                return new GarageWindowWriter(garage.Id, garage.Name, Vehicles, NearestVehicle);
            }
        }
    }
}
