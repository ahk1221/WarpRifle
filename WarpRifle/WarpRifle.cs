using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WarpRifle
{
    [RequireComponent(typeof(EnergyMixin))]
    public class WarpRifle : PlayerTool
    {
        public override string animToolName => "terraformer";

        public static GameObject warpInEffectPrefab;
        public static GameObject warpOutEffectPrefab;

        public static Material warpedMaterial;

        public static FMOD_StudioEventEmitter warpInSound;
        public static FMOD_StudioEventEmitter warpOutSound;

        public static float overlayFXDuration;

        ///....

        public float fireRate = 1f;
        public float nextFire = 0f;

        public GameObject grabbedObject;

        public Animator animator;

        public void Init()
        {
            ikAimLeftArm = true;
            ikAimRightArm = true;
            useLeftAimTargetOnPlayer = false;
            hasAnimations = true;
            hasFirstUseAnimation = false;
            hasBashAnimation = false;
            mainCollider = GetComponent<BoxCollider>();
            socket = PlayerTool.Socket.RightHand;
            animator = GetComponentInChildren<Animator>();

            var warper = (Resources.Load("WorldEntities/Creatures/Warper") as GameObject).GetComponent<Warper>();
            warpInEffectPrefab = warper.warpInEffectPrefab;
            warpOutEffectPrefab = warper.warpOutEffectPrefab;

            warpedMaterial = warper.warpedMaterial;

            warpInSound = warper.warpInSound;
            warpOutSound = warper.warpOutSound;

            overlayFXDuration = warper.overlayFXduration;

            Resources.UnloadAsset(warper);
        }

        public override bool OnLeftHandDown()
        {
            base.OnLeftHandDown();

            if (grabbedObject == null)
            {
                grabbedObject = TraceForGrabTarget();
            }

            return true;
        }

        public override bool OnLeftHandHeld()
        {
            if (grabbedObject != null)
                SafeAnimator.SetBool(animator, "use_loop", true);

            return base.OnLeftHandHeld();
        }

        public override bool OnLeftHandUp()
        {
            if (grabbedObject != null)
            {
                SafeAnimator.SetBool(animator, "use_loop", false);
                WarpObject(grabbedObject, GetNewPos());
            }

            return base.OnLeftHandUp();
        }

        public override bool OnRightHandDown()
        {
            base.OnRightHandDown();

            if (Time.time <= nextFire || energyMixin.charge <= 0 || !CanWarp()) return false;

            nextFire = Time.time + fireRate;

            var newPos = GetNewPos();

            WarpObject(Player.main.gameObject, newPos);
            
            this.energyMixin.ConsumeEnergy(4f);

            return true;
        }

        private bool CanWarp()
        {
            return (Player.main.IsInBase() == false) && (Player.main.IsInSub() == false);
        }

        public GameObject TraceForGrabTarget()
        {
            var aimingTransform = Player.main.camRoot.GetAimingTransform();
            var go = default(GameObject);
            var dist = 0f;

            if(Targeting.GetTarget(Player.main.gameObject, 30f, out go, out dist))
            {
                var rb = go.GetComponent<Rigidbody>();
                if (rb == null || rb.mass > 1200f) return null;

                var pickupable = go.GetComponent<Pickupable>();
                if (pickupable != null) return pickupable.attached ? go : null;
            }

            return null;
        }

        public static void WarpObject(GameObject obj, Vector3 newPos)
        {
            // Warp out.
            Utils.SpawnPrefabAt(warpOutEffectPrefab, null, obj.transform.position);
            Utils.PlayEnvSound(warpOutSound, obj.transform.position, 20f);

            // Warp in
            obj.transform.position = newPos;
            Utils.SpawnPrefabAt(warpInEffectPrefab, null, newPos);
            obj.AddComponent<VFXOverlayMaterial>().ApplyAndForgetOverlay(warpedMaterial, "VFXOverlay: Warped", Color.clear, overlayFXDuration);
            Utils.PlayEnvSound(warpOutSound, obj.transform.position, 20f);
        }

        public static Vector3 GetNewPos()
        {
            var aimingTransform = Player.main.camRoot.GetAimingTransform();
            var dist = 0f;
            var go = default(GameObject);
            var hitSomething = Targeting.GetTarget(Player.main.gameObject, 30, out go, out dist, null);

            var newPos = Vector3.zero;

            if (hitSomething)
            {
                newPos = aimingTransform.forward * (dist - 1f) + aimingTransform.position;
            }
            else
            {
                newPos = aimingTransform.forward * 30f + aimingTransform.position;
            }

            return newPos;
        }

    }
}
