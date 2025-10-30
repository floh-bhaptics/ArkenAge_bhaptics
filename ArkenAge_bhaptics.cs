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
using VitruviusVR.Items;


[assembly: MelonInfo(typeof(ArkenAge_bhaptics.ArkenAge_bhaptics), "ArkenAge_bhaptics", "1.0.0", "Florian Fahrenberger")]
[assembly: MelonGame("VitruviusVR", "Arken Age")]

namespace ArkenAge_bhaptics
{
    public class ArkenAge_bhaptics : MelonMod
    {
        public static TactsuitVR tactsuitVr;

        private static int healCount = 0;
        private static float lastLegOffset = 0;

        public override void OnInitializeMelon()
        {
            tactsuitVr = new TactsuitVR();
            tactsuitVr.PlaybackHaptics("heartbeat");
        }
        /*
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
        */

        private static (float, float) getAngleAndShift(Transform player, Vector3 hit)
        {
            // bhaptics pattern starts in the front, then rotates to the left. 0° is front, 90° is left, 270° is right.
            // y is "up", z is "forward" in local coordinates
            Vector3 patternOrigin = new Vector3(0f, 0f, 1f);
            Vector3 hitPosition = hit - player.position;
            Quaternion myPlayerRotation = player.rotation;
            Vector3 playerDir = myPlayerRotation.eulerAngles;
            // get rid of the up/down component to analyze xz-rotation
            Vector3 flattenedHit = new Vector3(hitPosition.x, 0f, hitPosition.z);

            // get angle. .Net < 4.0 does not have a "SignedAngle" function...
            float hitAngle = Vector3.Angle(flattenedHit, patternOrigin);
            // check if cross product points up or down, to make signed angle myself
            Vector3 crossProduct = Vector3.Cross(flattenedHit, patternOrigin);
            if (crossProduct.y > 0f) { hitAngle *= -1f; }
            // relative to player direction
            float myRotation = hitAngle - playerDir.y;
            // switch directions (bhaptics angles are in mathematically negative direction)
            myRotation *= -1f;
            // convert signed angle into [0, 360] rotation
            if (myRotation < 0f) { myRotation = 360f + myRotation; }


            // up/down shift is in y-direction
            // in Shadow Legend, the torso Transform has y=0 at the neck,
            // and the torso ends at roughly -0.5 (that's in meters)
            // so cap the shift to [-0.5, 0]...
            float hitShift = hitPosition.y;
            float upperBound = 0.0f;
            float lowerBound = -0.5f;
            if (hitShift > upperBound) { hitShift = 0.5f; }
            else if (hitShift < lowerBound) { hitShift = -0.5f; }
            // ...and then spread/shift it to [-0.5, 0.5]
            else { hitShift = (hitShift - lowerBound) / (upperBound - lowerBound) - 0.5f; }

            //tactsuitVr.LOG("Relative x-z-position: " + relativeHitDir.x.ToString() + " "  + relativeHitDir.z.ToString());
            //tactsuitVr.LOG("HitAngle: " + hitAngle.ToString());
            //tactsuitVr.LOG("HitShift: " + hitShift.ToString());

            // No tuple returns available in .NET < 4.0, so this is the easiest quickfix

            //return new KeyValuePair<float, float>(myRotation, hitShift);
            return (myRotation, hitShift);
        }

        [HarmonyPatch(typeof(PlayerCharacterController), "Jump")]
        public class bhaptics_PlayerJump
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerCharacterController __instance, bool ___jump, float ___jumpDurationTimer, float ___jumpInputDownTime)
            {
                if (__instance.Frozen || __instance.Grounded || __instance.Dashing || __instance.KneesFrozen || __instance.PreviousState == PlayerCharacterControllerStateEnum.Jumping) return;
                if (__instance.State == PlayerCharacterControllerStateEnum.InAir) return;
                if (___jumpDurationTimer > 0.1f) return;
                if (tactsuitVr.IsPlaying("jump")) return;
                if (__instance.State == PlayerCharacterControllerStateEnum.Jumping) tactsuitVr.PlaybackHaptics("jump");
            }
        }

        [HarmonyPatch(typeof(PlayerAvatar), "OnPlayerDie")]
        public class bhaptics_PlayerDie
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerAvatar __instance)
            {
                tactsuitVr.PlaybackHaptics("death");
                tactsuitVr.StopHeartBeat();
                tactsuitVr.StopParticlesLeft();
                tactsuitVr.StopParticlesRight();
            }
        }

        [HarmonyPatch(typeof(PlayerCharacterController), "TeleportCharacterController")]
        public class bhaptics_PlayerTeleport
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerCharacterController __instance)
            {
                tactsuitVr.PlaybackHaptics("teleport");
            }
        }

        [HarmonyPatch(typeof(PlayerCharacterController), "OnPlayerRespawn")]
        public class bhaptics_PlayerRespawn
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerCharacterController __instance)
            {
                tactsuitVr.PlaybackHaptics("respawn");
            }
        }

        [HarmonyPatch(typeof(PlayerCharacterController), "OnHeadEnterWater")]
        public class bhaptics_EnterWater
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerCharacterController __instance)
            {
                if (__instance.State == PlayerCharacterControllerStateEnum.Grounded || __instance.State == PlayerCharacterControllerStateEnum.InAir)
                {
                    tactsuitVr.PlaybackHaptics("PlayerEnterWater");
                }
            }
        }

        [HarmonyPatch(typeof(PlayerCharacterController), "OnHeadExitWater")]
        public class bhaptics_ExitWater
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerCharacterController __instance)
            {
                if (__instance.State == PlayerCharacterControllerStateEnum.Grounded || __instance.State == PlayerCharacterControllerStateEnum.InAir)
                {
                    tactsuitVr.PlaybackHaptics("PlayerEnterWater");
                }
            }
        }

        [HarmonyPatch(typeof(PlayerCharacterController), "StartDashing")]
        public class bhaptics_Dash
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerCharacterController __instance)
            {
                tactsuitVr.PlaybackHaptics("dash_back");
            }
        }

        [HarmonyPatch(typeof(EquipmentSlot), "AttachItem")]
        public class bhaptics_AttachItem
        {
            [HarmonyPostfix]
            public static void Postfix(EquipmentSlot __instance)
            {
                if (__instance.name.Contains("LeftSide") || __instance.name.Contains("LeftHip") || __instance.name.Contains("LeftLeg"))
                {
                    tactsuitVr.PlaybackHaptics("belly_put_l");
                }
                if (__instance.name.Contains("RightSide") || __instance.name.Contains("RightHip") || __instance.name.Contains("RightLeg"))
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
        }

        [HarmonyPatch(typeof(EquipmentSlot), "DetachItem")]
        public class bhaptics_DetachItem
        {
            [HarmonyPostfix]
            public static void Postfix(EquipmentSlot __instance)
            {
                if (__instance.name.Contains("LeftSide") || __instance.name.Contains("LeftHip") || __instance.name.Contains("LeftLeg"))
                {
                    tactsuitVr.PlaybackHaptics("belly_remove_l");
                }
                if (__instance.name.Contains("RightSide") || __instance.name.Contains("RightHip") || __instance.name.Contains("RightLeg"))
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
        }

        private static Transform playerTransform = null;
        [HarmonyPatch(typeof(PlayerAvatar), "Update")]
        public class bhaptics_movePlayer
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerAvatar __instance)
            {
                playerTransform = __instance.transform;
            }
        }


        [HarmonyPatch(typeof(PlayerHealth), "TakeDamage")]
        public class bhaptics_TakeDamage
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerHealth __instance, DamageHit hit, int damagableIndex)
            {
                if (__instance.RemainingHealth <= 0.3 * __instance.MaxHealth && __instance.RemainingHealth > 0)
                {
                    tactsuitVr.StartHeartBeat();
                }
                else
                {
                    tactsuitVr.StopHeartBeat();
                }
                float hitAngle;
                float hitShift;
                (hitAngle, hitShift) = getAngleAndShift(playerTransform, hit.Position);
                tactsuitVr.PlayBackHit("impact", hitAngle, hitShift);
            }
        }



        [HarmonyPatch(typeof(PlayerHealth), "Heal")]
        public class bhaptics_Heal
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerHealth __instance, DamageHit hit, int damagableIndex)
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
        }

        [HarmonyPatch(typeof(PlayerInventorySlot), "TryAddItemToSlot")]
        public class bhaptics_putItem
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerInventorySlot __instance, bool __result)
            {
                if (__result)
                {
                    tactsuitVr.PlaybackHaptics("back_put");
                }
            }
        }

        [HarmonyPatch(typeof(PlayerInventorySlot), "TryAddItemToSlotStack")]
        public class bhaptics_putItemStack
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerInventorySlot __instance, bool __result)
            {
                if (__result)
                {
                    tactsuitVr.PlaybackHaptics("back_put");
                }
            }
        }

        [HarmonyPatch(typeof(PlayerInventorySlot), "RemoveNextItem")]
        public class bhaptics_removeItem
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerInventorySlot __instance)
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
        }

        [HarmonyPatch(typeof(GadgetSlot), "ActivateCoroutine")]
        public class bhaptics_activateHand
        {
            [HarmonyPostfix]
            public static void Postfix(GadgetSlot __instance, PlayerHandInteractor ___playerHandInteractor)
            {
                if (___playerHandInteractor.IsLeftHand)
                {
                    tactsuitVr.PlaybackHaptics("hand_activate_l");
                }
                else
                {
                    tactsuitVr.PlaybackHaptics("hand_activate_r");
                }
            }
        }

        [HarmonyPatch(typeof(GadgetSlot), "DeactivateCoroutine")]
        public class bhaptics_deactivateHand
        {
            [HarmonyPostfix]
            public static void Postfix(GadgetSlot __instance, PlayerHandInteractor ___playerHandInteractor)
            {
                if (___playerHandInteractor.IsLeftHand)
                {
                    tactsuitVr.PlaybackHaptics("hand_deactivate_l");
                }
                else
                {
                    tactsuitVr.PlaybackHaptics("hand_deactivate_r");
                }
            }
        }

        [HarmonyPatch(typeof(PlayerMouthInteractor), "OnInteractableItemColliderEnter")]
        public class bhaptics_Eating
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerMouthInteractor __instance, InteractableCollider collider)
            {
                PlayerMouthInteractable playerMouthInteractable1;
                if (collider.gameObject.TryGetComponent<PlayerMouthInteractable>(out playerMouthInteractable1))
                {
                    tactsuitVr.PlaybackHaptics("eating");
                }
            }
        }

        [HarmonyPatch(typeof(PlayerGun), "Shoot", new Type[] { })]
        public class bhaptics_Shoot
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerGun __instance, InteractableSpot ___handleInteractableSpot)
            {


                bool isRight = !___handleInteractableSpot.InteractingInteractor.IsLeftHand;
                if (__instance.name.Contains("HeavyGun"))
                {
                    if (isRight) tactsuitVr.PlaybackHaptics("recoil_shotgun_r");
                    else tactsuitVr.PlaybackHaptics("recoil_shotgun_l");
                }
                else if (__instance.name.Contains("LightGun"))
                {
                    if (isRight) tactsuitVr.PlaybackHaptics("recoil_pistol_r");
                    else tactsuitVr.PlaybackHaptics("recoil_pistol_l");
                }
            }
        }


        [HarmonyPatch(typeof(PlayerMeleeWeapon), "OnTriggerEnter")]
        public class bhaptics_DealDamage
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerMeleeWeapon __instance, Collider other)
            {
                if (other.name.Contains("EquipmentSlot")) return;
                if (other.name.Contains("HandInteractor")) return;
                if (other.name.Contains("AddToInventoryTrigger")) return;
                bool isRight = !__instance.HandleSpot.InteractingInteractor.IsLeftHand;
                if (isRight) tactsuitVr.PlaybackHaptics("melee_hit_r");
                else tactsuitVr.PlaybackHaptics("melee_hit_l");
            }
        }



        [HarmonyPatch(typeof(PlayerCharacterControllerHaptics), "OnGroundContactRegained")]
        public class bhaptics_hitGround
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerCharacterControllerHaptics __instance, PlayerCharacterController ___playerCharacterController, float ___hardLandingMinDistance)
            {
                float fall_height = Mathf.Abs(Singleton<MainCamera>.Instance.transform.position.y - ___playerCharacterController.InAirApex);
                if (fall_height >= ___hardLandingMinDistance)
                {
                    tactsuitVr.PlaybackHaptics("hit_ground");
                }
            }
        }

        [HarmonyPatch(typeof(PlayerHandInteractor), "StartReceiveParticles")]
        public class bhaptics_startReceiveParticles
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerHandInteractor __instance)
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
        }

        [HarmonyPatch(typeof(PlayerHandInteractor), "StopReceiveParticles")]
        public class bhaptics_stopReceiveParticles
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerHandInteractor __instance)
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
        }

        [HarmonyPatch(typeof(PlayerVineHelper), "AttachVines")]
        public class bhaptics_AttachVines
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerVineHelper __instance)
            {
                if (__instance.OnVinesAttached == null)
                {
                    return;
                }
                tactsuitVr.PlaybackHaptics("attach_vines");
            }
        }

        [HarmonyPatch(typeof(PlayerVineHelper), "DetachVines")]
        public class bhaptics_DetachVines
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerVineHelper __instance)
            {
                if (__instance.OnVinesAttached == null)
                {
                    return;
                }
                tactsuitVr.PlaybackHaptics("detach_vines");
            }
        }

        [HarmonyPatch(typeof(PlayerCharacterController), "SetLegHeight")]
        public class bhaptics_Crouch
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerCharacterController __instance, float offset)
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
}