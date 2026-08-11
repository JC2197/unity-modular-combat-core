using UnityEditor;
using UnityEngine;
using FishNet.Object;
using FishNet.Component.Transforming;
using FishNet.Component.Animating;


public static class WeaponNetworkComponentAdder
{
    [MenuItem("Tools/Add Weapon Network Components")]
    public static void AddWeaponNetworkComponents()
    {
        string[] guids = AssetDatabase.FindAssets("t:WeaponConfig");
        int modifiedCount = 0;

        foreach (string guid in guids)
        {
            string configPath = AssetDatabase.GUIDToAssetPath(guid);
            WeaponConfig weaponConfig = AssetDatabase.LoadAssetAtPath<WeaponConfig>(configPath);

            if (weaponConfig == null)
            {
                Debug.LogWarning($"[WeaponNetworkComponentAdder] Could not load WeaponConfig at path: {configPath}");
                continue;
            }
            string prefabPath = AssetDatabase.GetAssetPath(weaponConfig.weaponPrefab);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            bool changed = false;

            // Step 5: Add NetworkObject + NetworkTransform to root if missing
            if (prefabRoot.GetComponent<NetworkObject>() == null)
            {
                prefabRoot.AddComponent<NetworkObject>();
                changed = true;
            }
            if (prefabRoot.GetComponent<NetworkTransform>() == null)
            {
                prefabRoot.AddComponent<NetworkTransform>();
                changed = true;
            }

            Transform weaponSprite = prefabRoot.transform.Find("WeaponSprite");
            if (weaponSprite != null)
            {
                if (weaponSprite.GetComponent<SpriteRenderer>() == null)
                {
                    weaponSprite.gameObject.AddComponent<SpriteRenderer>();
                    changed = true;
                }
                if (weaponSprite.GetComponent<Animator>() == null)
                {
                    weaponSprite.gameObject.AddComponent<Animator>();
                    changed = true;
                }
                if (weaponSprite.GetComponent<NetworkTransform>() == null)
                {
                    weaponSprite.gameObject.AddComponent<NetworkTransform>();
                    changed = true;
                }
                if (weaponSprite.GetComponent<NetworkAnimator>() == null)
                {
                    weaponSprite.gameObject.AddComponent<NetworkAnimator>();
                    changed = true;
                }
            }
            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                modifiedCount++;
                Debug.Log($"[WeaponNetworkAdder] Updated: {prefabPath}");
            }
            PrefabUtility.UnloadPrefabContents(prefabRoot);

        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Weapon Network Component Adder", $"Process completed. Modified {modifiedCount} prefabs.", "OK");

    }
}
