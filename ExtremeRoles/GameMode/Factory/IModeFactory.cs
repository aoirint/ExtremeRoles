﻿using ExtremeRoles.GameMode.Logic.Usable;
using ExtremeRoles.GameMode.Option.ShipGlobal;
using ExtremeRoles.GameMode.RoleSelector;

namespace ExtremeRoles.GameMode.Factory
{
    public interface IModeFactory
    {
        public IShipGlobalOption CreateGlobalOption();

        public IRoleSelector CreateRoleSelector();

        public ILogicUsable CreateLogicUsable();
    }
}
