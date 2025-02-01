using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Bootstrap;
using UnityEngine;
using UnityEngine.Serialization;

namespace SleepingBag;

public class SleepingBagElement : MonoBehaviour
{
    public GameObject sleepingBagObject = null!;
    public GameObject sleepingBagObjectBackpacks = null!;

    public GameObject sleepingBagItemObject = null!;
    public GameObject sleepingBagItemObjectBackpacks = null!;

    public Sprite sleepingBagSprite = null!;
    public Sprite sleepingBagSpriteBackpacks = null!;

    public Sprite sleepingBagItemSprite = null!;
    public Sprite sleepingBagItemSpriteBackpacks = null!;

    void Awake()
    {
        SwapRender();
    }

    private void OnPreRender()
    {
        SwapRender();
    }

    void SwapRender()
    {
        ItemDrop? item = GetComponent<ItemDrop>();
        if (sleepingBagObject != null)
        {
            if (IsBackpacksInstalled())
            {
                sleepingBagObject.SetActive(false);
                sleepingBagObjectBackpacks.SetActive(true);
            }
        }

        if (sleepingBagItemObject == null) return;
        if (!IsBackpacksInstalled()) return;
        sleepingBagItemObject.SetActive(false);
        sleepingBagItemObjectBackpacks.SetActive(true);
    }

    internal static bool IsBackpacksInstalled()
    {
        Chainloader.PluginInfos.TryGetValue(SleepingBagPlugin.BackpacksGUID, out PluginInfo pluginInfo);
        return pluginInfo != null;
    }
}