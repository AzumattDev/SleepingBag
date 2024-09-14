using HarmonyLib;
using UnityEngine;

namespace SleepingBag;

[HarmonyPatch(typeof(Bed), nameof(Bed.Interact))]
static class Bed_Interact_Patch
{
    static bool Prefix(Bed __instance, Humanoid human, bool repeat, bool alt)
    {
        if (Player.m_localPlayer != null)
        {
            // this function overrides the Bed.Interact method to bypass roof check if the GameObject is a sleeping bag.
            if (repeat)
                return false;
            long myInstance = Game.instance.GetPlayerProfile().GetPlayerID();
            long belongsTo = __instance.GetOwner();
            Player player1 = human as Player;

            // if it doesn't belong to anybody
            if (belongsTo == 0L)
            {
                //then check if it is a sleeping bag, if so, bypass Roof Check
                if (!(__instance.gameObject.name == "sleepingbag_piece(Clone)") && !(__instance.gameObject.name == "sleepingbag_piece"))
                {
                    if (!__instance.CheckExposure(player1))
                        return false;
                }

                // now, it's mine
                __instance.SetOwner(myInstance, Game.instance.GetPlayerProfile().GetName());
                Game.instance.GetPlayerProfile().SetCustomSpawnPoint(__instance.GetSpawnPoint());
                human.Message(MessageHud.MessageType.Center, "$msg_spawnpointset");
            }
            //if it is mine
            else if (__instance.IsMine())
            {
                //if it's my current spawnpoint
                if (__instance.IsCurrent())
                {
                    //is it time to sleep ? else prevent sleeping
                    if (!EnvMan.CanSleep())
                    {
                        human.Message(MessageHud.MessageType.Center, "$msg_cantsleep");
                        return false;
                    }

                    //all clear ? warm ? dry ? else prevent sleeping 
                    /*if (!__instance.CheckEnemies(player1) || (!__instance.CheckFire(player1) || !__instance.CheckWet(player1)))
                    {*/
                    //heck if it is a sleeping bag, if so, bypass Roof Check and go to sleep !
                    if (!(__instance.gameObject.name == "sleepingbag_piece(Clone)") && !(__instance.gameObject.name == "sleepingbag_piece"))
                    {
                        if (!__instance.CheckExposure(player1))
                            return false;
                    }

                    /*return false;
                }*/

                    human.AttachStart(__instance.m_spawnPoint, __instance.gameObject, true, true, false, "attach_bed", new Vector3(0.0f, 0.5f, 0.0f));
                    return false;
                }

                //then check if it is a sleeping bag, if so, bypass Roof Check
                if (!(__instance.gameObject.name == "sleepingbag_piece(Clone)") && !(__instance.gameObject.name == "sleepingbag_piece"))
                {
                    if (!__instance.CheckExposure(player1))
                        return false;
                }

                //else, define as current
                Game.instance.GetPlayerProfile().SetCustomSpawnPoint(__instance.GetSpawnPoint());
                human.Message(MessageHud.MessageType.Center, "$msg_spawnpointset");
            }

            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
static class ZNetSceneAwakePatch
{
    [HarmonyPriority(Priority.Last)]
    static void Postfix(ZNetScene __instance)
    {
        if (SleepingBagElement.IsBackpacksInstalled())
        {
            var sleepingBag = __instance.GetPrefab("sleepingbag_piece");
            var sleepingBagItem = __instance.GetPrefab("sleepingbag_item");
            sleepingBag.GetComponent<Piece>().m_icon = sleepingBag.GetComponent<SleepingBagElement>().SleepingBagSpriteBackpacks;
            sleepingBagItem.GetComponent<ItemDrop>().m_itemData.m_shared.m_icons[0] = sleepingBagItem.GetComponent<SleepingBagElement>().SleepingBagItemSpriteBackpacks;
        }
    }
}