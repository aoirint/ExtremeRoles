﻿using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;


using ExtremeRoles.GhostRoles;
using ExtremeRoles.Module.RoleAssign.Model;
using ExtremeRoles.Module.CustomMonoBehaviour.View;
using ExtremeRoles.Resources;

#nullable enable

namespace ExtremeRoles.Module.RoleAssign;

public sealed class RoleAssignFilter : NullableSingleton<RoleAssignFilter>
{
    private List<RoleFilterSet> filter = new List<RoleFilterSet>();
    private RoleAssignFilterView? view;
    private RoleAssignFilterModel model;
    public RoleAssignFilter()
    {
        this.filter.Clear();

        this.model = new RoleAssignFilterModel()
        {
            Id = new(),
            NormalRole = new(),
            CombRole = new(),
            GhostRole = new(),
            FilterSet = new()
        };
    }

    public string SerializeModel() => this.model.SerializeToString();

    public void DeserializeModel(string value)
    {
        var newModel = new RoleAssignFilterModel()
        {
            Id = new(),
            NormalRole = new(),
            CombRole = new(),
            GhostRole = new(),
            FilterSet = new(),
        };

        newModel.DeserializeFromString(value);
        this.model = newModel;

        if (this.view != null)
        {
            this.view.Model = newModel;
        }
    }

    // UIを見せる
    public void OpenEditor(GameObject hideObj)
    {
        if (this.view == null)
        {
            hideObj.SetActive(false);

            // アセットバンドルからロード
            GameObject viewObj = UnityEngine.Object.Instantiate(
                Loader.GetUnityObjectFromResources<GameObject>(
                    "ExtremeRoles.Resources.Asset.roleassignfilter.asset",
                    "assets/roles/roleassignfilter.prefab"));
            this.view = viewObj.GetComponent<RoleAssignFilterView>();
            this.view.HideObject = hideObj;
            this.view.Model = model;
            this.view.Awake();
        }
        this.view.gameObject.SetActive(true);
    }

    public void Initialize()
    {
        foreach (var filterModel in model.FilterSet.Values)
        {
            var filterSet = new RoleFilterSet();
            filterSet.AssignNum = filterModel.AssignNum;

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

            this.filter.Add(filterSet);
        }
    }

    public void Update(int intedRoleId)
    {
        foreach (var fil in this.filter)
        {
            fil.Update(intedRoleId);
        }
    }
    public void Update(byte bytedCombRoleId)
    {
        foreach (var fil in this.filter)
        {
            fil.Update(bytedCombRoleId);
        }
    }
    public void Update(ExtremeGhostRoleId roleId)
    {
        foreach (var fil in this.filter)
        {
            fil.Update(roleId);
        }
    }
    public bool IsBlock(int intedRoleId) => this.filter.Any(x => x.IsBlock(intedRoleId));
    public bool IsBlock(byte bytedCombRoleId) => this.filter.Any(x => x.IsBlock(bytedCombRoleId));
    public bool IsBlock(ExtremeGhostRoleId roleId) => this.filter.Any(x => x.IsBlock(roleId));

}
