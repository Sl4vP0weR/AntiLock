﻿using HarmonyLib;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System.Linq;
using UnityEngine;
using static AntiLock.Main;

namespace AntiLock.Patching
{
    /// <summary>
    /// Patching class of <see cref="VehicleManager.ReceiveVehicleLockRequest"/>
    /// </summary>
    [HarmonyPatch(typeof(VehicleManager), nameof(ReceiveVehicleLockRequest))]
    static class ReceiveVehicleLockRequest
    {
        // for debug
        static bool Check(ServerInvocationContext context)
        {
            var steamID = context.GetCallingPlayer().playerID.steamID;
            var up = UnturnedPlayer.FromCSteamID(steamID);
            if (up.IsAdmin && conf.IgnoreAdmins)
                return true;

            var vehicle = up.Player.movement.getVehicle();

            if (!vehicle?.checkDriver(steamID) ?? true) // not in the vehicle or isn't a driver
                return false;

            if (vehicle.isLocked) // locked
                return true;

            var group = conf.LockGroups
                .Where(x => up.HasPermission(x.Permission))
                .OrderBy(x => x.MaxLocks)
                .LastOrDefault();

            int max = group is null ? conf.DefaultMaxLocks : group.MaxLocks;
            int lockedVehiclesCount = VehicleManager.vehicles.Count(x => x.lockedOwner == steamID && x.isLocked);

            var maxLocks = lockedVehiclesCount >= max && conf.DisplayMaxlocksNotice; // is player locked max number of the vehicles

            var msg = maxLocks ? inst.Translate(MaxLockedNotice, max) :
                    (conf.DisplayLockNotice ? inst.Translate(LockedNotice, max - lockedVehiclesCount - 1, max) : null);
            var color = maxLocks ? Color.red : Color.green;

            UnturnedChat.Say(up, msg, color);
            return !maxLocks;
        }
        static bool Prefix(in ServerInvocationContext context) => Check(context);
    }
}
