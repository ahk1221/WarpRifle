using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SMLHelper;
using SMLHelper.Patchers;
using UnityEngine;

namespace WarpRifle
{
    public class Main
    {
        public static string WARP_RIFLE_CLASSID = "WarpRifle";
        public static string WARP_BATTERY_CLASSID = "WarpRifle";

        public static string WARP_RIFLE_PREFABPATH = "WorldEntities/Tools/WarpRifle";
        public static string WARP_BATTERY_PREFABPATH = "WorldEntities/Tools/WarpRifle";

        public static TechType WarpRifleTechType;
        public static TechType WarpBatteryTechType;
        public static TechType WarpBatteryChargerTechType;

        public static void Patch()
        {
            WarpRifleTechType = TechTypePatcher.AddTechType(WARP_RIFLE_CLASSID, "Warp Rifle", "l o l");
            WarpBatteryTechType = TechTypePatcher.AddTechType(WARP_BATTERY_CLASSID, "Warp Battery", "Powers Warp Rifles");

            CustomPrefabHandler.customPrefabs.Add(new CustomPrefab(
                WARP_RIFLE_CLASSID,
                WARP_RIFLE_PREFABPATH,
                WarpRifleTechType,
                GetRifleResource));

            CustomPrefabHandler.customPrefabs.Add(new CustomPrefab(
                WARP_BATTERY_CLASSID,
                WARP_BATTERY_PREFABPATH,
                WarpBatteryTechType,
                GetBatteryResource));

            CraftDataPatcher.customEquipmentTypes.Add(WarpRifleTechType, EquipmentType.Hand);
        }

        public static GameObject GetRifleResource()
        {
            var prefab = Resources.Load<GameObject>("WorldEntities/Tools/Terraformer");
            var obj = GameObject.Instantiate(prefab);

            obj.name = "WarpRifle";

            var techTag = obj.GetComponent<TechTag>();
            var prefabIdentifier = obj.GetComponent<PrefabIdentifier>();
            var terraformer = obj.GetComponent<Terraformer>();
            var energyMixin = obj.GetComponent<EnergyMixin>();

            prefabIdentifier.ClassId = WARP_RIFLE_CLASSID;
            techTag.type = WarpRifleTechType;

            MonoBehaviour.DestroyImmediate(terraformer);

            energyMixin.compatibleBatteries = new List<TechType>()
            {
                WarpBatteryTechType
            };
            energyMixin.defaultBattery = WarpBatteryTechType;

            var warpRifle = obj.AddComponent<WarpRifle>();
            warpRifle.Init();

            Console.WriteLine("Loaded Terraformer Warp");

            return obj;
        }

        public static GameObject GetBatteryResource()
        {
            var prefab = Resources.Load<GameObject>("WorldEntities/Tools/Battery");
            var obj = GameObject.Instantiate(prefab);

            var identifier = obj.GetComponent<PrefabIdentifier>();
            var techTag = obj.GetComponent<TechTag>();

            identifier.ClassId = WARP_BATTERY_CLASSID;
            techTag.type = WarpBatteryTechType;

            return obj;
        }
    }
}
