﻿using ExtremeRoles.Roles;
using ExtremeRoles.GhostRoles;
using ExtremeRoles.Module.RoleAssign.Model;

namespace ExtremeRoles.Module.RoleAssign.Update;

public static class RoleAssignFilterModelUpdater
{
    public static void AddFilter(RoleAssignFilterModel model)
    {
        model.FilterSet.Add(
            model.FilterId,
            new RoleFilterSetModel()
            {
                FilterCombinationId = new(),
                FilterGhostRole = new(),
                FilterNormalId = new(),
            });
        model.FilterId += 1;
    }

    public static void ConvertModelToAssignFilter(
        RoleAssignFilterModel model, RoleAssignFilter filter)
    {
        foreach (var filterModel in model.FilterSet.Values)
        {
            var filterSet = new RoleFilterSet();

            foreach (var extremeRoleId in filterModel.FilterNormalId.Values)
            {
                filterSet.Add(extremeRoleId);
            }
            foreach (var extremeRoleId in filterModel.FilterCombinationId.Values)
            {
                filterSet.Add(extremeRoleId);
            }
            foreach (var extremeRoleId in filterModel.FilterGhostRole.Values)
            {
                filterSet.Add(extremeRoleId);
            }

            filter.AddRoleFilterSet(filterSet);
        }
    }

    public static void RemoveFilter(RoleAssignFilterModel model, int targetFilter)
    {
        model.FilterSet.Remove(targetFilter);
    }

    public static void ResetFilter(RoleAssignFilterModel model, int targetFilter)
    {
        var filter = model.FilterSet[targetFilter];
        filter.FilterNormalId.Clear();
        filter.FilterCombinationId.Clear();
        filter.FilterGhostRole.Clear();
    }
}
