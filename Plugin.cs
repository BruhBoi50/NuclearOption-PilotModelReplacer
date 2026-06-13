using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.IO;
using System.Reflection;
using UnityEngine;


namespace PilotModelReplacer
{
    [BepInPlugin("com.bruhboi.pilotmodelreplacer", "Pilot Model Replacer", "1.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static AssetBundle CustomBundle;
        public static GameObject CustomModelPrefab;

        public static ConfigEntry<bool> ConfigPlayerOnly;
        public static ConfigEntry<string> ConfigBundleName;


        private void Awake()
        {
            //Setup configuration

            ConfigPlayerOnly = Config.Bind("General", 
                                           "OnlyChangePlayer", false,
                                           "If enabled, the mod will only replace the player's pilot model. Otherwise, it replaces all pilots");

            ConfigBundleName = Config.Bind("Model Settings",
                                           "AssetBundleFileName",
                                           "cj_pilot",
                                           "The name of the bundle inside the mod folder");



            //Locate and load the AssetBundle
            string modFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string bundlePath = Path.Combine(modFolder, "assets", ConfigBundleName.Value);

            if (File.Exists(bundlePath))
            {
                CustomBundle = AssetBundle.LoadFromFile(bundlePath);

                //Load the prefab
                CustomModelPrefab = CustomBundle.LoadAsset<GameObject>("pilot");
                Logger.LogInfo("Custom player model asset bundle loaded successfully");

            }
            else
            {
                Logger.LogError("Could not find bundle of name: " + ConfigBundleName.Value);
                return;
            }

            //Initialize Harmony
            var harmony = new Harmony("com.bruhboi.pilotmodelreplacer");
            harmony.PatchAll();
        }
        

        //Replaces the model
        public static void ReplaceModel(SkinnedMeshRenderer originalModel, SkinnedMeshRenderer newModel)
        {

            Debug.Log("[PilotReplacer] Replacing model now...");

            Material mat = newModel.material;
            Shader gameShader = Shader.Find(mat.shader.name);
            mat.shader = gameShader;

            FixBoneOrder(originalModel);
            originalModel.material = newModel.material;
            originalModel.sharedMesh = newModel.sharedMesh;

        }


        //The bone order of the Pilot model has the leg bones switched up.
        //Thats why this function has to switch the last 3 bones
        static void FixBoneOrder(SkinnedMeshRenderer renderer)
        {
            Transform[] bonesCopy = renderer.bones;

            for (int i = bonesCopy.Length - 3; i < bonesCopy.Length; i++)
            {
                Transform temp = bonesCopy[i];
                bonesCopy[i] = bonesCopy[i - 3];
                bonesCopy[i - 3] = temp;
            }

            renderer.bones = bonesCopy;
        }

    }

    //There are two pilot scripts. One for the cockpit pilot and the dismounted pilot
    //Target the Pilot script
    [HarmonyPatch(typeof(Pilot), "Pilot_OnInitialize", MethodType.Normal)]
    public class PilotPatchA
    {
        [HarmonyPostfix]
        public static void Postfix(Pilot __instance)
        {
            Debug.Log("[PilotReplacer] Patch triggered: Pilot");

            if (Plugin.ConfigPlayerOnly.Value && __instance.GetComponent<GLOC>() == null) //dumb but works. Only method that works
            {
                Debug.Log("[PilotReplacer] Target not player. Skipping");
                return;
            }
            if (Plugin.CustomModelPrefab == null)
            {
                Debug.LogError("[PilotReplacer] Prefab not found");
                return;
            }

            SkinnedMeshRenderer originalModel = __instance.GetComponentInChildren<SkinnedMeshRenderer>();
            SkinnedMeshRenderer newModel = Plugin.CustomModelPrefab.GetComponentInChildren<SkinnedMeshRenderer>();

            if (originalModel == null)
            {
                Debug.LogError("[PilotReplacer] Could not find model in object!");
                return;
            }

            Plugin.ReplaceModel(originalModel, newModel);

        }

    }

    
    //Target the PilotDismounted script
    [HarmonyPatch(typeof(PilotDismounted), "Setup")]
    public class PilotPatchB
    {
        [HarmonyPostfix]
        public static void Postfix(PilotDismounted __instance)
        {
            Debug.Log("[PilotReplacer] Patch triggered: Dismounted");

            Debug.Log(__instance.NetworkpilotNumber);

            if (Plugin.ConfigPlayerOnly.Value && __instance.Networkplayer == null)
            {
                Debug.Log("[PilotReplacer] Target not player. Skipping");
                return;
            }
            if (Plugin.CustomModelPrefab == null)
            {
                Debug.LogError("[PilotReplacer] Prefab not found");
                return;
            }
            
            SkinnedMeshRenderer originalModel = __instance.GetComponentInChildren<SkinnedMeshRenderer>();
            SkinnedMeshRenderer newModel = Plugin.CustomModelPrefab.GetComponentInChildren<SkinnedMeshRenderer>();

            if (originalModel == null)
            {
                Debug.LogError("[PilotReplacer] Could not find model in object!");
                return;
            }

            Plugin.ReplaceModel(originalModel, newModel);

        }

    }

    



}
