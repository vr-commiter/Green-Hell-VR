using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.Mono;
using Enums;
using HarmonyLib;
using IncuvoTutorialsVR;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using UnityEngine;
using static UnityEngine.SendMouseEvents;
using MyTrueGear;

namespace GreenHell_TrueGear
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        private static int leftHandUniqueID = -1;
        private static int rightHandUniqueID = -1;
        private static bool isVrPlayerFreeze = true;

        private static TrueGearMod _TrueGear = null;

        private void Awake()
        {
            // Plugin startup logic
            Logger = base.Logger;
            _TrueGear = new TrueGearMod();
            Harmony.CreateAndPatchAll(typeof(Plugin));
            _TrueGear.Play("HeartBeat");
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        }

        public static int GetMillisecond()
        {
            DateTime now = DateTime.Now;
            int milliseconds = now.Millisecond;
            return milliseconds;
        }


        [HarmonyPostfix, HarmonyPatch(typeof(Player), "UnfreezeVRPlayer")]
        private static void Player_UnfreezeVRPlayer_Postfix(Player __instance)
        {
            isVrPlayerFreeze = false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Player), "FreezeVRPlayer")]
        private static void Player_FreezeVRPlayer_Postfix(Player __instance)
        {
            isVrPlayerFreeze = true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Player), "AttachItemToHand")]
        private static void Player_AttachItemToHand_Postfix(Player __instance,Hand hand, Item item)
        {
            if (isVrPlayerFreeze)
            {
                return;
            }
            if (hand == Hand.Left)
            {
                Logger.LogInfo("------------------------------------");
                Logger.LogInfo("LeftHandPickupItem");
                _TrueGear.Play("LeftHandPickupItem");
                leftHandUniqueID = item.m_UniqueID;
            }
            else if (hand == Hand.Right)
            {
                Logger.LogInfo("------------------------------------");
                Logger.LogInfo("RightHandPickupItem");
                _TrueGear.Play("RightHandPickupItem");
                rightHandUniqueID = item.m_UniqueID;
            }
            Logger.LogInfo(hand);
            Logger.LogInfo(item.m_UniqueID);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Player), "DetachItemFromHand")]
        private static void Player_DetachItemFromHand_Postfix(Player __instance, Item item, Hand hand)
        {
            if (hand == Hand.Left)
            {
                leftHandUniqueID = -1;
            }
            else if (hand == Hand.Right)
            {
                rightHandUniqueID = -1;
            }
        }


        private static KeyValuePair<float, float> GetAngle(Transform player, Transform hit)
        {
            float num2 = 0f;

            Vector3 playerToEnemy = hit.position - player.position;
            float angle = Vector3.SignedAngle(player.transform.forward, playerToEnemy, Vector3.up);
            angle = (angle + 360) % 360; // 确保角度在0到360之间
            angle = 360f - angle;

            num2 = hit.position.y - player.position.y ;

            //MelonLogger.Msg(angle);

            return new KeyValuePair<float, float>(angle, num2);
        }

        private static float damageCount = 0;

        [HarmonyPostfix, HarmonyPatch(typeof(Player), "TakeDamage")]
        private static void Player_TakeDamage_Postfix(Player __instance, DamageInfo info,bool __result)
        {
            if (!__result)
            {
                return;
            }
            damageCount += info.m_Damage;
            if (damageCount < 1)
            {
                return;
            }
            damageCount = 0;
            if (info.m_DamageType == DamageType.Fall)
            {
                Logger.LogInfo("------------------------------------");
                Logger.LogInfo("FallDamage");
                _TrueGear.Play("FallDamage");
                return;
            }
            else if (info.m_DamageType == DamageType.Fire)
            {
                Logger.LogInfo("------------------------------------");
                Logger.LogInfo("FireDamage");
                _TrueGear.Play("FireDamage");
                return;
            }

            var angle = GetAngle(__instance.transform, info.m_Damager.transform);
            if (angle.Key == 360)
            {
                Logger.LogInfo("------------------------------------");
                Logger.LogInfo("PoisonDamage");
                _TrueGear.Play("PoisonDamage");
                Logger.LogInfo(info.m_DamageType);
                return;
            }
            Logger.LogInfo("------------------------------------");
            Logger.LogInfo($"DefaultDamage,{angle.Key},{angle.Value}");
            _TrueGear.PlayAngle("DefaultDamage",angle.Key, angle.Value);
            Logger.LogInfo(info.m_DamageType);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Player), "Die")]
        private static void Player_Die_Postfix(Player __instance)
        {
            Logger.LogInfo("------------------------------------");
            Logger.LogInfo("PlayerDeath");
            _TrueGear.Play("PlayerDeath");
        }


        [HarmonyPostfix, HarmonyPatch(typeof(SleepController), "StartSleeping")]
        private static void SleepController_StartSleeping_Postfix(SleepController __instance)
        {
            if (!Player.Get().CanSleep())
            {
                return;
            }
            Logger.LogInfo("------------------------------------");
            Logger.LogInfo("Sleeping");
            _TrueGear.Play("Sleeping");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(WalkieTalkie), "OnGrab")]
        private static void WalkieTalkie_OnGrab_Postfix(WalkieTalkie __instance, Hand hand, Item item)
        {
            if (isVrPlayerFreeze)
            {
                return;
            }
            if (item && item.m_Info.IsWalkieTalkie())
            {
                Logger.LogInfo("------------------------------------");
                Logger.LogInfo("LeftChestSlotOutputItem");
                _TrueGear.Play("LeftChestSlotOutputItem");
            }            
        }

        [HarmonyPostfix, HarmonyPatch(typeof(WalkieTalkie), "OnDrop")]
        private static void WalkieTalkie_OnDrop_Postfix(WalkieTalkie __instance, Hand hand, Item item)
        {
            if (isVrPlayerFreeze)
            {
                return;
            }
            if (item && item.m_Info.IsWalkieTalkie())
            {
                Logger.LogInfo("------------------------------------");
                Logger.LogInfo("LeftChestSlotInputItem");
                _TrueGear.Play("LeftChestSlotInputItem");
            }
        }


        [HarmonyPostfix, HarmonyPatch(typeof(NotepadController), "Awake")]
        private static void NotepadController_Awake_Postfix(NotepadController __instance)
        {
            __instance.GrabNotebook -= GrabNotebook;
            __instance.GrabNotebook += GrabNotebook;
            __instance.ReleaseNotebook -= ReleaseNotebook;
            __instance.ReleaseNotebook += ReleaseNotebook;
        }

        private static void ReleaseNotebook()
        {
            if (isVrPlayerFreeze)
            {
                return;
            }
            Logger.LogInfo("------------------------------------");
            Logger.LogInfo("RightChestSlotInputItem");
            _TrueGear.Play("RightChestSlotInputItem");
        }

        private static void GrabNotebook(bool param)
        {
            if (isVrPlayerFreeze)
            {
                return;
            }
            Logger.LogInfo("------------------------------------");
            Logger.LogInfo("RightChestSlotOutputItem");
            _TrueGear.Play("RightChestSlotOutputItem");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Backpack), "OnDisable")]
        private static void Backpack_OnDisable_Postfix(Backpack __instance)
        {
            if (isVrPlayerFreeze)
            {
                return;
            }
            if (HUDManager.Get() == null)
            {
                return;
            }
            if (__instance.m_CloseSounds.Count == 0)
            {
                return;
            }

            Logger.LogInfo("------------------------------------");
            Logger.LogInfo("LeftBackSlotInputItem");
            _TrueGear.Play("LeftBackSlotInputItem");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Backpack), "OnEnable")]
        private static void Backpack_OnEnable_Postfix(Backpack __instance)
        {
            if (isVrPlayerFreeze)
            {
                return;
            }
            if (HUDManager.Get() == null)
            {
                return;
            }
            if (__instance.m_OpenSounds.Count == 0)
            {
                return;
            }
            Logger.LogInfo("------------------------------------");
            Logger.LogInfo("LeftBackSlotOutputItem");
            _TrueGear.Play("LeftBackSlotOutputItem");
        }



        //[HarmonyPostfix, HarmonyPatch(typeof(InventoryBackpack), "InsertItemSlot")]
        //private static void InventoryBackpack_InsertItemSlot_Postfix(InventoryBackpack __instance, InsertResult __result)
        //{
        //    Logger.LogInfo("------------------------------------");
        //    Logger.LogInfo("InsertItemSlot");
        //    Logger.LogInfo(__result);
        //}



        [HarmonyPostfix, HarmonyPatch(typeof(MainLevel), "Pause")]
        private static void MainLevel_Pause_Postfix(MainLevel __instance, bool pause)
        {
            if (pause)
            {
                _TrueGear.Pause();
            }
            else
            {
                _TrueGear.UnPause();
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(WeaponController), "Hit")]
        private static void WeaponController_Hit_Postfix(WeaponController __instance, Item weaponItem)
        {
            if (leftHandUniqueID == weaponItem.m_UniqueID)
            {
                Logger.LogInfo("------------------------------------");
                Logger.LogInfo("LeftHandMeleeHit");
                _TrueGear.Play("LeftHandMeleeHit");
            }
            if (rightHandUniqueID == weaponItem.m_UniqueID)
            {
                Logger.LogInfo("------------------------------------");
                Logger.LogInfo("RightHandMeleeHit");
                _TrueGear.Play("RightHandMeleeHit");
            }
        }




        [HarmonyPostfix, HarmonyPatch(typeof(Item), "OnAddToInventory")]
        private static void Item_OnAddToInventory_Postfix(Item __instance)
        {
            if (isVrPlayerFreeze)
            {
                return;
            }
            Logger.LogInfo("------------------------------------");
            Logger.LogInfo("BackSlotInputItem");
            _TrueGear.Play("BackSlotInputItem");

        }

        [HarmonyPostfix, HarmonyPatch(typeof(Item), "OnRemoveFromInventory")]
        private static void Item_OnRemoveFromInventory_Postfix(Item __instance)
        {
            if (isVrPlayerFreeze)
            {
                return;
            }
            Logger.LogInfo("------------------------------------");
            Logger.LogInfo("BackSlotOutputItem");
            _TrueGear.Play("BackSlotOutputItem");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Item), "OnAddToHoldSlot")]
        private static void Item_OnAddToHoldSlot_Postfix(Item __instance, ItemHoldSlotType slotType)
        {
            if (isVrPlayerFreeze)
            {
                return;
            }
            if (slotType == ItemHoldSlotType.RightHip)
            {
                Logger.LogInfo("------------------------------------");
                Logger.LogInfo("RightHipSlotInputItem");
                _TrueGear.Play("RightHipSlotInputItem");
            }
            else if (slotType == ItemHoldSlotType.LeftHip)
            {
                Logger.LogInfo("------------------------------------");
                Logger.LogInfo("LeftHipSlotInputItem");
                _TrueGear.Play("LeftHipSlotInputItem");
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Item), "OnRemoveFromHipSlot")]
        private static void Item_OnRemoveFromHipSlot_Prefix(Item __instance)
        {
            if (isVrPlayerFreeze)
            {
                return;
            }
            if (ItemHoldSlotController.rightHipSlotInstance.content != null && ItemHoldSlotController.rightHipSlotInstance.content.m_UniqueID == __instance.m_UniqueID)
            {
                Logger.LogInfo("------------------------------------");
                Logger.LogInfo("RightHipSlotOutputItem");
                _TrueGear.Play("RightHipSlotOutputItem");
            }
            else if (ItemHoldSlotController.leftHipSlotInstance.content != null &&  ItemHoldSlotController.leftHipSlotInstance.content.m_UniqueID == __instance.m_UniqueID)
            {
                Logger.LogInfo("------------------------------------");
                Logger.LogInfo("LeftHipSlotOutputItem");
                _TrueGear.Play("LeftHipSlotOutputItem");
            }
        }



        [HarmonyPostfix, HarmonyPatch(typeof(EatingController), "OnEat")]
        private static void EatingController_OnEat_Postfix(EatingController __instance)
        {
            Logger.LogInfo("------------------------------------");
            Logger.LogInfo("Eating");
            _TrueGear.Play("Eating");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(EatingController), "OnDrink")]
        private static void EatingController_OnDrink_Postfix(EatingController __instance)
        {
            Logger.LogInfo("------------------------------------");
            Logger.LogInfo("Drinking");
            _TrueGear.Play("Drinking");
        }



        [HarmonyPostfix, HarmonyPatch(typeof(Player), "ThrowItem", new Type[] { typeof(Item) })]
        private static void Player_ThrowItem_Postfix(Player __instance, Item item)
        {
            if (isShootArrow)
            {
                isShootArrow = false;
                return;
            }
            if (leftHandUniqueID == item.m_UniqueID)
            {
                Logger.LogInfo("------------------------------------");
                Logger.LogInfo("LeftHandThrowItem");
                _TrueGear.Play("LeftHandThrowItem");
            }
            else
            {
                Logger.LogInfo("------------------------------------");
                Logger.LogInfo("RightHandThrowItem");
                _TrueGear.Play("RightHandThrowItem");
            }
        }

        private static double GetDir(Vector3 watchPos,Vector3 handPos)
        {
            double distance = Math.Sqrt(Math.Pow(watchPos.x - handPos.x, 2) + Math.Pow(watchPos.y - handPos.y, 2) + Math.Pow(watchPos.z - handPos.z, 2));
            return distance;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Watch), "SetState")]
        private static void Watch_SetState_Postfix(Watch __instance, Watch.State state)
        {
            if (__instance.m_State == state)
            {
                return;
            }
            double leftHandDir = GetDir(__instance.watchTransform.position, __instance.player.GetLHand().position);
            double rightHandDir = GetDir(__instance.watchTransform.position, __instance.player.GetRHand().position);

            if (leftHandDir >= rightHandDir)
            {
                Logger.LogInfo("------------------------------------");
                Logger.LogInfo("LeftHandWatchSetState");
                _TrueGear.Play("LeftHandWatchSetState");
            }
            else
            {
                Logger.LogInfo("------------------------------------");
                Logger.LogInfo("RightHandWatchSetState");
                _TrueGear.Play("RightHandWatchSetState");
            }
        }

        //[HarmonyPostfix, HarmonyPatch(typeof(Bow), "AttachArrow")]
        //private static void Bow_AttachArrow_Postfix(Bow __instance)
        //{
        //    Logger.LogInfo("------------------------------------");
        //    Logger.LogInfo("AttachArrow");
        //}

        //[HarmonyPostfix, HarmonyPatch(typeof(Bow), "DetachArrowFromBow")]
        //private static void Bow_DetachArrowFromBow_Postfix(Bow __instance)
        //{
        //    if (__instance.LoadedArrow != null)
        //    {
        //        Logger.LogInfo("------------------------------------");
        //        Logger.LogInfo("DetachArrowFromBow");
        //    }
        //}

        [HarmonyPostfix, HarmonyPatch(typeof(Bow), "GrabString")]
        private static void Bow_GrabString_Postfix(Bow __instance)
        {
            if (leftHandUniqueID == __instance.m_UniqueID)
            {
                Logger.LogInfo("------------------------------------");
                Logger.LogInfo("RightHandGrabString");
                _TrueGear.Play("RightHandGrabString");
            }
            else
            {
                Logger.LogInfo("------------------------------------");
                Logger.LogInfo("LeftHandGrabString");
                _TrueGear.Play("LeftHandGrabString");
            }
        }

        private static bool isShootArrow = false;
        [HarmonyPostfix, HarmonyPatch(typeof(Bow), "ShootArrowAudio")]
        private static void Bow_ShootArrowAudio_Postfix(Bow __instance)
        {
            if (leftHandUniqueID == __instance.m_UniqueID)
            {
                Logger.LogInfo("------------------------------------");
                Logger.LogInfo("LeftHandShootArrow");
                _TrueGear.Play("LeftHandShootArrow");
            }
            else
            {
                Logger.LogInfo("------------------------------------");
                Logger.LogInfo("RightHandShootArrow");
                _TrueGear.Play("RightHandShootArrow");
            }
            isShootArrow = true;
        }



        [HarmonyPostfix, HarmonyPatch(typeof(FistFightController), "GiveDamage")]
        private static void FistFightController_GiveDamage_Postfix(FistFightController __instance)
        {
            if (__instance.IsLeftPunch())
            {
                Logger.LogInfo("------------------------------------");
                Logger.LogInfo("LeftHandPunch");
                _TrueGear.Play("LeftHandPunch");
            }
            else
            {
                Logger.LogInfo("------------------------------------");
                Logger.LogInfo("RightHandPunch");
                _TrueGear.Play("RightHandPunch");
            }
        }


        private static Vector3 playerPos = new Vector3();
        [HarmonyPostfix, HarmonyPatch(typeof(Player), "Update")]
        private static void Player_Update_Postfix(Player __instance)
        {
            playerPos = __instance.transform.position;
        }


        //[HarmonyPostfix, HarmonyPatch(typeof(RainManager), "ScenarioStartRain")]
        //private static void RainManager_ScenarioStartRain_Postfix(RainManager __instance)
        //{
        //    Logger.LogInfo("------------------------------------");
        //    Logger.LogInfo("ScenarioStartRain");
        //}

        //[HarmonyPostfix, HarmonyPatch(typeof(RainManager), "ScenarioStopRain")]
        //private static void RainManager_ScenarioStopRain_Postfix(RainManager __instance)
        //{
        //    Logger.LogInfo("------------------------------------");
        //    Logger.LogInfo("ScenarioStopRain");
        //}

        [HarmonyPostfix, HarmonyPatch(typeof(RainManager), "Update")]
        private static void RainManager_Update_Postfix(RainManager __instance)
        {
            if (__instance.IsRain())
            {
                if (__instance.IsInRainCutter(playerPos))
                {
                    _TrueGear.StopRain();
                }
                else
                {
                    _TrueGear.StartRain();
                }
            }
            else
            {
                _TrueGear.StopRain();
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerConditionModule), "Update")]
        private static void PlayerConditionModule_Update_Postfix(PlayerConditionModule __instance)
        {
            if (__instance.m_HP <= 0.01 * __instance.m_MaxHP)
            {
                //_TrueGear.StopHydration();
                _TrueGear.StopEnergy();
                _TrueGear.StopHeartBeat();
                _TrueGear.StopStamina();
                _TrueGear.StopOxygen();
                return;
            }
            else if (__instance.m_HP <= 0.25 * __instance.m_MaxHP)
            {
                _TrueGear.StartHeartBeat();
            }
            else
            {
                _TrueGear.StopHeartBeat();
            }


            //if (__instance.m_Dirtiness <= 0.25 * (__instance.m_MaxDirtiness))
            //{
            //    _TrueGear.StartDirtiness();
            //}
            //else
            //{
            //    _TrueGear.StopDirtiness();
            //}


            if (__instance.m_Energy <= 0.25 * (__instance.m_MaxEnergy))
            {
                _TrueGear.StartEnergy();
            }
            else
            {
                _TrueGear.StopEnergy();
            }


            //if (__instance.m_Hydration <= 0.25 * (__instance.m_MaxHydration))
            //{
            //    _TrueGear.StartHydration();
            //}
            //else
            //{
            //    _TrueGear.StopHydration();
            //}


            if (__instance.m_Oxygen <= 0.25 * (__instance.m_MaxOxygen))
            {
                _TrueGear.StartOxygen();
            }
            else
            {
                _TrueGear.StopOxygen();
            }


            if (__instance.m_Stamina <= 0.25 * (__instance.m_MaxStamina))
            {
                _TrueGear.StartStamina();
            }
            else
            {
                _TrueGear.StopStamina();
            }

        }

        [HarmonyPostfix, HarmonyPatch(typeof(AyuhascaTrigger), "PsychotriaViridisInserted")]
        private static void AyuhascaTrigger_PsychotriaViridisInserted_Postfix(AyuhascaTrigger __instance)
        {
            Logger.LogInfo("------------------------------------");
            Logger.LogInfo("AyuhascaTripStart");
            _TrueGear.Play("AyuhascaTripStart");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Fire), "OnIgnitionFinished")]
        private static void Fire_OnIgnitionFinished_Postfix(Fire __instance)
        {
            Logger.LogInfo("------------------------------------");
            Logger.LogInfo("CampFireStart");
            _TrueGear.Play("CampFireStart");
        }



        private static int WalkieTalkieTime = 0;

        [HarmonyPostfix, HarmonyPatch(typeof(WalkieTalkieController), "OnInputAction")]
        private static void WalkieTalkieController_OnInputAction_Postfix(WalkieTalkieController __instance, InputActionData action_data)
        {
            if (GetMillisecond() - WalkieTalkieTime < 110)
            {
                return;
            }
            WalkieTalkieTime = GetMillisecond();
            if (action_data.handSide == Hand.Left)
            {
                Logger.LogInfo("------------------------------------");
                Logger.LogInfo("LeftHandPickupItem");
                _TrueGear.Play("LeftHandPickupItem");
            }
            else
            {
                Logger.LogInfo("------------------------------------");
                Logger.LogInfo("RightHandPickupItem");
                _TrueGear.Play("RightHandPickupItem");
            }
        }


    }
}
