using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WarpRifle
{
    public class TransformInfo
    {
        public Vector3 Position;
        public Quaternion Rotation;
    }

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

        private static List<GameObject> checkedObjects = new List<GameObject>();

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
                if (grabbedObject == null)
                    Console.WriteLine("Did not pass pickupable check");
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

                var transformInfo = GetNewPos(grabbedObject.transform);

                WarpObject(grabbedObject, transformInfo.Position, transformInfo.Rotation);
                grabbedObject.AddComponent<WarpedObject>();

                grabbedObject = null;
            }

            return base.OnLeftHandUp();
        }

        public override bool OnRightHandDown()
        {
            base.OnRightHandDown();

            if (Time.time <= nextFire || energyMixin.charge <= 0 || !CanWarp()) return false;

            nextFire = Time.time + fireRate;

            var transformInfo = GetNewPos(Player.main.transform);
            WarpObject(Player.main.gameObject, transformInfo.Position, Quaternion.identity);
            
            return true;
        }

        private bool CanWarp()
        {
            return (Player.main.IsInBase() == false) && (Player.main.IsInSub() == false);
        }

        public GameObject TraceForGrabTarget()
        {
            var position = MainCamera.camera.transform.position;
            var layerMask = ~(1 << LayerMask.NameToLayer("Player"));
            var amountOfObjs = UWE.Utils.SpherecastIntoSharedBuffer(position, 1.2f, MainCamera.camera.transform.forward, 18f, layerMask, QueryTriggerInteraction.UseGlobal);
            var result = default(GameObject);
            var infinity = float.PositiveInfinity;
            checkedObjects.Clear();

            for (int i = 0; i < amountOfObjs; i++)
            {
                var raycastHit = UWE.Utils.sharedHitBuffer[i];
                if (!raycastHit.collider.isTrigger || raycastHit.collider.gameObject.layer == LayerMask.NameToLayer("Useable"))
                {
                    GameObject entityRoot = UWE.Utils.GetEntityRoot(raycastHit.collider.gameObject);
                    if (entityRoot != null && !checkedObjects.Contains(entityRoot))
                    {
                        if (entityRoot.GetComponentInParent<PropulseCannonAmmoHandler>() == null)
                        {
                            var sqrMagnitude = (raycastHit.point - position).sqrMagnitude;
                            if (sqrMagnitude < infinity && ValidateNewObject(entityRoot, raycastHit.point))
                            {
                                result = entityRoot;
                                infinity = sqrMagnitude;
                            }
                        }
                        checkedObjects.Add(entityRoot);
                    }
                }
            }

            return result;
        }

        public bool ValidateNewObject(GameObject go, Vector3 hitPos, bool checkLineOfSight = true)
        {
            if (!ValidateObject(go))
            {
                return false;
            }

            if (checkLineOfSight && !CheckLineOfSight(go, MainCamera.camera.transform.position, hitPos))
            {
                return false;
            }

            if (go.GetComponent<Pickupable>() != null)
            {
                return true;
            }

            var aabb = GetAABB(go);
            return aabb.size.x * aabb.size.y * aabb.size.z <= 120f;
        }

        private bool ValidateObject(GameObject go)
        {
            if (!go.activeSelf || !go.activeInHierarchy)
            {
                Debug.Log("object is inactive");
                return false;
            }

            Rigidbody component = go.GetComponent<Rigidbody>();
            if (component == null || component.mass > 1200)
            {
                return false;
            }

            Pickupable component2 = go.GetComponent<Pickupable>();
            bool flag = false;
            if (component2 != null)
            {
                flag = component2.attached;
            }

            return !flag;
        }

        private Bounds GetAABB(GameObject target)
        {
            var component = target.GetComponent<FixedBounds>();
            var result = default(Bounds);

            if (component != null)
            {
                result = component.bounds;
            }
            else
            {
                result = UWE.Utils.GetEncapsulatedAABB(target, 20);
            }

            return result;
        }

        private bool CheckLineOfSight(GameObject obj, Vector3 a, Vector3 b)
        {
            bool result = true;
            int num = UWE.Utils.RaycastIntoSharedBuffer(a, Vector3.Normalize(b - a), (b - a).magnitude, ~(1 << LayerMask.NameToLayer("Player")), QueryTriggerInteraction.Ignore);
            bool flag = false;
            for (int i = 0; i < num; i++)
            {
                if (flag)
                {
                    break;
                }
                GameObject gameObject = UWE.Utils.GetEntityRoot(UWE.Utils.sharedHitBuffer[i].collider.gameObject);
                if (!gameObject)
                {
                    gameObject = UWE.Utils.sharedHitBuffer[i].collider.gameObject;
                }
                Player componentInChildren = gameObject.GetComponentInChildren<Player>();
                if (componentInChildren == null && gameObject != obj)
                {
                    result = false;
                    flag = true;
                }
            }
            return result;
        }

        public void WarpObject(GameObject obj, Vector3 newPos, Quaternion newRot)
        {
            // Warp out.
            Utils.SpawnPrefabAt(warpOutEffectPrefab, null, obj.transform.position);
            Utils.PlayEnvSound(warpOutSound, obj.transform.position, 20f);

            // Warp in
            obj.transform.position = newPos;
            obj.transform.rotation = newRot == Quaternion.identity ? obj.transform.rotation : newRot;
            Utils.SpawnPrefabAt(warpInEffectPrefab, null, newPos);
            obj.AddComponent<VFXOverlayMaterial>().ApplyAndForgetOverlay(warpedMaterial, "VFXOverlay: Warped", Color.clear, overlayFXDuration);
            Utils.PlayEnvSound(warpOutSound, obj.transform.position, 20f);

            energyMixin.ConsumeEnergy(4f);
        }

        public TransformInfo GetNewPos(Transform obj)
        {
            var layerMask = ~(1 << LayerMask.NameToLayer("Player"));

            var num = UWE.Utils.RaycastIntoSharedBuffer(new Ray(
                MainCamera.camera.transform.position, 
                MainCamera.camera.transform.forward),
                float.PositiveInfinity, layerMask,
                QueryTriggerInteraction.UseGlobal);

            var newPos = Vector3.zero;
            var newRot = Quaternion.identity;

            for(int i = 0; i < num; i++)
            {
                var hitInfo = UWE.Utils.sharedHitBuffer[i];
                var dist = Vector3.Distance(MainCamera.camera.transform.position, hitInfo.point);
                newPos = MainCamera.camera.transform.forward * (dist - 1f) + MainCamera.camera.transform.position;

                newRot = Quaternion.FromToRotation(obj.up, hitInfo.normal);

                return new TransformInfo() { Position = newPos, Rotation = newRot };
            }

            newPos = MainCamera.camera.transform.forward * 30f + MainCamera.camera.transform.position;

            return new TransformInfo() { Position = newPos, Rotation = Quaternion.identity };
        }
    }
}
