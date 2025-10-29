using HarmonyLib;
using MelonLoader;
using MyBhapticsTactsuit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VitruviusVR;
using VitruviusVR.Damage;
using VitruviusVR.Equipment;
using VitruviusVR.Haptics;
using VitruviusVR.Interaction;
using VitruviusVR.Player;
using VitruviusVR;
using VitruviusVR.Damage;
using VitruviusVR.Equipment;
using VitruviusVR.Haptics;
using VitruviusVR.Interaction;
using VitruviusVR.Player;


[assembly: MelonInfo(typeof(ArkenAge_bhaptics.ArkenAge_bhaptics), "ArkenAge_bhaptics", "1.0.0", "Florian Fahrenberger")]
[assembly: MelonGame("VitruviusVR", "Arken Age")]

namespace ArkenAge_bhaptics
{
    public class ArkenAge_bhaptics : MelonMod
    {
        public static TactsuitVR tactsuitVr;

        private static int leftHandItemID = 88888888;
        private static int rightHandItemID = 88888888;
        private static string leftHandItemName = null;
        private static string rightHandItemName = null;

        private static int healCount = 0;
        private static float lastLegOffset = 0;

        public override void OnInitializeMelon()
        {
            tactsuitVr = new TactsuitVR();
            tactsuitVr.PlaybackHaptics("HeartBeat");
        }

        public static KeyValuePair<float, float> GetEnemyDirection(Transform player, Vector3 enemy)
        {
            Vector3 toEnemy = enemy - player.position;
            toEnemy.y = 0;

            if (toEnemy == Vector3.zero)
                return new KeyValuePair<float, float>(0, 0);

            Vector3 playerForward = player.forward;
            playerForward.y = 0;
            playerForward.Normalize();
            toEnemy.Normalize();
            float clockwiseAngle = Vector3.SignedAngle(playerForward, toEnemy, Vector3.up);
            float counterClockwiseAngle = (360 - clockwiseAngle) % 360;
            float verticalDifference = enemy.y - player.position.y;
            return new KeyValuePair<float, float>(clockwiseAngle, verticalDifference);
        }


        [HarmonyPatch(typeof(PlayerCharacterController), "Jump")]
        public class bhaptics_Jump
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerCharacterController __instance)
            {
                if (__instance.Frozen || __instance.Grounded || __instance.Dashing || __instance.KneesFrozen)
                {
                    return;
                }
                if (__instance.State == PlayerCharacterControllerStateEnum.Jumping)
                {
                    tactsuitVr.PlaybackHaptics("jump");
            }
    }
}
            

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerAvatar), "OnPlayerDie")]
        private static void PlayerAvatar_OnPlayerDie_Postfix(PlayerAvatar __instance)
        {
            tactsuitVr.PlaybackHaptics("death");
            tactsuitVr.StopHeartBeat();
            //_TrueGear.StopLeftHandReceiveParticles();
            //_TrueGear.StopRightHandReceiveParticles();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerCharacterController), "TeleportCharacterController")]
        private static void PlayerCharacterController_TeleportCharacterController_Postfix(PlayerCharacterController __instance)
        {
            tactsuitVr.PlaybackHaptics("teleport");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerCharacterController), "OnPlayerRespawn")]
        private static void PlayerCharacterController_OnPlayerRespawn_Postfix(PlayerCharacterController __instance)
        {
            tactsuitVr.PlaybackHaptics("respawn");
        }

        [HarmonyPrefix, HarmonyPatch(typeof(PlayerCharacterController), "OnHeadEnterWater")]
        private static void PlayerCharacterController_OnHeadEnterWater_Prefix(PlayerCharacterController __instance)
        {
            if (__instance.State == PlayerCharacterControllerStateEnum.Grounded || __instance.State == PlayerCharacterControllerStateEnum.InAir)
            {
                tactsuitVr.PlaybackHaptics("PlayerEnterWater");
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(PlayerCharacterController), "OnHeadExitWater")]
        private static void PlayerCharacterController_OnHeadExitWater_Prefix(PlayerCharacterController __instance)
        {
            if (__instance.State == PlayerCharacterControllerStateEnum.Swimming)
            {
                tactsuitVr.PlaybackHaptics("exit_water");
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(PlayerCharacterController), "StartDashing")]
        private static void PlayerCharacterController_StartDashing_Prefix(PlayerCharacterController __instance)
        {
            tactsuitVr.PlaybackHaptics("dash_back");
        }


        [HarmonyPostfix, HarmonyPatch(typeof(EquipmentSlot), "AttachItem")]
        private static void EquipmentSlot_AttachItem_Postfix(EquipmentSlot __instance)
        {
            if (__instance.name.Contains("LeftSide"))
            {
                tactsuitVr.PlaybackHaptics("belly_put_l");
            }
            else if (__instance.name.Contains("RightSide"))
            {
                tactsuitVr.PlaybackHaptics("belly_put_r");
            }
            else if (__instance.name.Contains("LeftShoulder"))
            {
                tactsuitVr.PlaybackHaptics("shoulder_put_l");
            }
            else if (__instance.name.Contains("RightShoulder"))
            {
                tactsuitVr.PlaybackHaptics("shoulder_put_r");
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(EquipmentSlot), "DetachItem")]
        private static void EquipmentSlot_DetachItem_Postfix(EquipmentSlot __instance)
        {
            if (__instance.name.Contains("LeftSide"))
            {
                tactsuitVr.PlaybackHaptics("belly_remove_l");
            }
            else if (__instance.name.Contains("RightSide"))
            {
                tactsuitVr.PlaybackHaptics("belly_remove_l");
            }
            else if (__instance.name.Contains("LeftShoulder"))
            {
                tactsuitVr.PlaybackHaptics("shoulder_remove_l");
            }
            else if (__instance.name.Contains("RightShoulder"))
            {
                tactsuitVr.PlaybackHaptics("shoulder_remove_r");
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(PlayerHealth), "TakeDamage")]
        private static void PlayerHealth_TakeDamage_Prefix(PlayerHealth __instance, DamageHit hit, int damagableIndex)
        {
            if (__instance.RemainingHealth <= 0.3 * __instance.MaxHealth && __instance.RemainingHealth > 0)
            {
                tactsuitVr.StartHeartBeat();
            }
            else
            {
                tactsuitVr.StopHeartBeat();
            }
            var angle = GetEnemyDirection(playerTransform, hit.Position);
            tactsuitVr.PlayBackHit("impact", angle.Key, angle.Value);
        }

        private static Transform playerTransform = null;
        [HarmonyPostfix, HarmonyPatch(typeof(PlayerAvatar), "Update")]
        private static void PlayerAvatar_Update_Postfix(PlayerAvatar __instance)
        {
            playerTransform = __instance.transform;
        }


        [HarmonyPostfix, HarmonyPatch(typeof(PlayerHealth), "Heal")]
        private static void PlayerHealth_Heal_Postfix(PlayerHealth __instance, DamageHit hit, int damagableIndex)
        {
            if (__instance.RemainingHealth <= 0.3 * __instance.MaxHealth)
            {
                tactsuitVr.StartHeartBeat();
            }
            else
            {
                tactsuitVr.StopHeartBeat();
            }
            healCount += hit.Amount;
            if (healCount >= 5)
            {
                healCount = 0;
                tactsuitVr.PlaybackHaptics("healing");
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerInventorySlot), "TryAddItemToSlot")]
        private static void PlayerInventorySlot_TryAddItemToSlot_Postfix(PlayerInventorySlot __instance, bool __result)
        {
            if (__result)
            {
                tactsuitVr.PlaybackHaptics("back_put");
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerInventorySlot), "TryAddItemToSlotStack")]
        private static void PlayerInventorySlot_TryAddItemToSlotStack_Postfix(PlayerInventorySlot __instance, bool __result)
        {
            if (__result)
            {
                tactsuitVr.PlaybackHaptics("back_put");
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(PlayerInventorySlot), "RemoveNextItem")]
        private static void PlayerInventorySlot_RemoveNextItem_Prefix(PlayerInventorySlot __instance)
        {
            string itemName = __instance.Items[0].Item.name;
            if (itemName != null && (itemName.Contains("Shield") || itemName.Contains("HealthSyringe")))
            {
                tactsuitVr.PlaybackHaptics("chest_remove");
            }
            else
            {
                tactsuitVr.PlaybackHaptics("back_remove");
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GadgetSlot), "ActivateCoroutine")]
        private static void GadgetSlot_ActivateCoroutine_Postfix(GadgetSlot __instance, PlayerHandInteractor ___playerHandInteractor)
        {
            if (___playerHandInteractor.IsLeftHand)
            {
                tactsuitVr.PlaybackHaptics("LeftHandGadgetActivate");
            }
            else
            {
                tactsuitVr.PlaybackHaptics("RightHandGadgetActivate");
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GadgetSlot), "DeactivateCoroutine")]
        private static void GadgetSlot_DeactivateCoroutine_Postfix(GadgetSlot __instance, PlayerHandInteractor ___playerHandInteractor)
        {
            if (___playerHandInteractor.IsLeftHand)
            {
                tactsuitVr.PlaybackHaptics("LeftHandGadgetDeactivate");
            }
            else
            {
                tactsuitVr.PlaybackHaptics("RightHandGadgetDeactivate");
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(PlayerMouthInteractor), "OnInteractableItemColliderEnter")]
        private static void PlayerMouthInteractor_OnInteractableItemColliderEnter_Prefix(PlayerMouthInteractor __instance, InteractableCollider collider)
        {
            PlayerMouthInteractable playerMouthInteractable1;
            if (collider.gameObject.TryGetComponent<PlayerMouthInteractable>(out playerMouthInteractable1))
            {
                tactsuitVr.PlaybackHaptics("eating");
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ProjectileSpawner), "ShootProjectile", new Type[] { typeof(Vector3) })]
        private static void ProjectileSpawner_ShootProjectile_Postfix(ProjectileSpawner __instance)
        {
            try
            {
                if (!__instance.name.Contains("LightGun"))
                {
                    return;
                }
                if (leftHandItemName != null && leftHandItemName.Contains("LightGun"))
                {
                    tactsuitVr.PlaybackHaptics("recoil_pistol_l");
                }
                if (rightHandItemName != null && rightHandItemName.Contains("LightGun"))
                {

                    tactsuitVr.PlaybackHaptics("recoil_pistol_r");
                }

            }
            catch { }

        }

        [HarmonyPostfix, HarmonyPatch(typeof(ShotgunProjectileSpawner), "ShootProjectile", new Type[] { typeof(Vector3) })]
        private static void ShotgunProjectileSpawner_ShootProjectile_Postfix(ShotgunProjectileSpawner __instance)
        {
            try
            {
                if (!__instance.name.Contains("HeavyGun"))
                {
                    return;
                }
                if (leftHandItemName != null && leftHandItemName.Contains("HeavyGun"))
                {
                    tactsuitVr.PlaybackHaptics("recoil_shotgun_l");
                }
                if (rightHandItemName != null && rightHandItemName.Contains("HeavyGun"))
                {

                    tactsuitVr.PlaybackHaptics("recoil_shotgun_r");
                }
            }
            catch { }
        }



        [HarmonyPrefix, HarmonyPatch(typeof(PlayerCharacterControllerHaptics), "OnGroundContactRegained")]
        private static void PlayerCharacterControllerHaptics_OnGroundContactRegained_Prefix(PlayerCharacterControllerHaptics __instance, PlayerCharacterController ___playerCharacterController, float ___hardLandingMinDistance)
        {
            float fall_height = Mathf.Abs(Singleton<MainCamera>.Instance.transform.position.y - ___playerCharacterController.InAirApex);
            if (fall_height >= ___hardLandingMinDistance)
            {
                tactsuitVr.PlaybackHaptics("hit_ground");
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerHandInteractor), "StartReceiveParticles")]
        private static void PlayerHandInteractor_StartReceiveParticles_Postfix(PlayerHandInteractor __instance)
        {
            if (__instance.IsLeftHand)
            {
                tactsuitVr.StartParticlesLeft();
            }
            else
            {
                tactsuitVr.StartParticlesRight();
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerHandInteractor), "StopReceiveParticles")]
        private static void PlayerHandInteractor_StopReceiveParticles_Postfix(PlayerHandInteractor __instance)
        {
            if (__instance.IsLeftHand)
            {
                tactsuitVr.StopParticlesLeft();
            }
            else
            {
                tactsuitVr.StopParticlesRight();
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerVineHelper), "AttachVines")]
        private static void PlayerVineHelper_AttachVines_Postfix(PlayerVineHelper __instance)
        {
            if (__instance.OnVinesAttached == null)
            {
                return;
            }
            tactsuitVr.PlaybackHaptics("attach_vines");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerVineHelper), "DetachVines")]
        private static void PlayerVineHelper_DetachVines_Postfix(PlayerVineHelper __instance)
        {
            if (__instance.OnVinesDetached == null)
            {
                return;
            }
            tactsuitVr.PlaybackHaptics("detach_vines");
        }



        [HarmonyPostfix, HarmonyPatch(typeof(PlayerCharacterController), "SetLegHeight")]
        private static void PlayerCharacterController_SetLegHeight_Postfix(PlayerCharacterController __instance, float offset)
        {
            if (__instance.State == PlayerCharacterControllerStateEnum.Jumping || __instance.State == PlayerCharacterControllerStateEnum.InAir)
            {
                return;
            }
            if (Math.Abs(offset - lastLegOffset) > 0.1f)
            {
                lastLegOffset = offset;
                tactsuitVr.PlaybackHaptics("crouch");
            }
        }

    }
}
