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
        public static string WARP_RIFLE_PREFABPATH = "WorldEntities/Tools/WarpRifle";

        public static TechType WarpRifleTechType;

        public static void Patch()
        {
            WarpRifleTechType = TechTypePatcher.AddTechType(WARP_RIFLE_CLASSID, "Warp Rifle", "l o l");

            CustomPrefabHandler.customPrefabs.Add(new CustomPrefab(
                Main.WARP_RIFLE_CLASSID,
                Main.WARP_RIFLE_PREFABPATH,
                Main.WarpRifleTechType,
                GetRifleResource));

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

            prefabIdentifier.ClassId = Main.WARP_RIFLE_CLASSID;
            techTag.type = Main.WarpRifleTechType;

            MonoBehaviour.DestroyImmediate(terraformer);

            var warpRifle = obj.AddComponent<WarpRifle>();
            warpRifle.Init();

            Console.WriteLine("Loaded Terraformer Warp");

            return obj;
        }
    }
}
