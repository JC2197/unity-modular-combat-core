using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom editor for EnemyConfig to conditionally show bounce-related fields
/// </summary>
[CustomEditor(typeof(EnemyConfig))]
public class EnemyConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("enemyName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("displayName"));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("stats"), true);

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("detectionRange"));

        EditorGUILayout.Space();

        // Enemy Type
        EditorGUILayout.LabelField("Enemy Type", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("isFlying"));

        EditorGUILayout.Space();

        // Projectile Enemy
        SerializedProperty isProjectileEnemy = serializedObject.FindProperty("isProjectileEnemy");
        EditorGUILayout.PropertyField(isProjectileEnemy);
        if (isProjectileEnemy.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("projectileRange"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("projectileEnemyConfig"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("projectileAttackCooldownMin"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("projectileAttackCooldownMax"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // Charge Behavior
        SerializedProperty useChargeBehavior = serializedObject.FindProperty("useChargeBehavior");
        EditorGUILayout.PropertyField(useChargeBehavior);
        if (useChargeBehavior.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("chargeRange"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("chargeForce"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("chargeFriction"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("chargeStopSpeed"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("hasCollisionDamage"));
        if (serializedObject.FindProperty("hasCollisionDamage").boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("collisionDamage"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("collisionDamageCooldown"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("collisionDamageType"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("collisionHitLayers"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("isSimpleEnemy"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("abilities"), true);

        if (!serializedObject.FindProperty("isSimpleEnemy").boolValue)
        {
            // Weapon System
            EditorGUILayout.LabelField("Weapon System", EditorStyles.boldLabel);

            SerializedProperty mainHandWeaponConfig = serializedObject.FindProperty("mainHandWeaponConfig");
            EditorGUILayout.PropertyField(mainHandWeaponConfig);


            // Display weapon's granted ability info
            if (mainHandWeaponConfig.objectReferenceValue != null)
            {
                WeaponConfig weaponConfig = mainHandWeaponConfig.objectReferenceValue as WeaponConfig;
                if (weaponConfig != null && weaponConfig.grantedPrimaryAbility != null)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.HelpBox($"Granted Ability: {weaponConfig.grantedPrimaryAbility.name}", MessageType.Info);

                    AbilityDataConfig abilityData = weaponConfig.grantedPrimaryAbility as AbilityDataConfig;
                    if (abilityData != null)
                    {
                        string info = $"Type: {(abilityData.isProjectileAbility ? "Projectile" : "Other")}\\n";
                        info += $"Cooldown: {abilityData.cooldownTime}s\\n";
                        if (abilityData.isAttack)
                        {
                            info += $"Attack Speed: {abilityData.attackSpeed} attacks/sec";
                        }
                        EditorGUILayout.HelpBox(info, MessageType.None);
                    }
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("offhandWeaponConfig"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("handSprite"));

            SerializedProperty useWeaponGrantedAbilities = serializedObject.FindProperty("useWeaponGrantedAbilities");
            EditorGUILayout.PropertyField(useWeaponGrantedAbilities);

            // Show weapon ability range if using weapon abilities
            if (useWeaponGrantedAbilities.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("weaponAbilityRange"));
                EditorGUI.indentLevel--;
            }
        }

        EditorGUILayout.Space();



        EditorGUILayout.Space();

        // AI Behavior System
        EditorGUILayout.LabelField("AI Behavior System", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Actions are evaluated by priority and conditions. Higher priority actions are preferred when conditions are met.", MessageType.Info);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("actions"), new GUIContent("Actions"), true);

        EditorGUILayout.Space();

        // Main Movement
        EditorGUILayout.LabelField("Main Movement", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("canMove"));

        SerializedProperty continuousMovement = serializedObject.FindProperty("continuousMovement");
        EditorGUILayout.PropertyField(continuousMovement);

        // Show movement/stop timing only if not continuous
        if (!continuousMovement.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("movementTime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stopTime"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // Pathfinding
        EditorGUILayout.LabelField("Pathfinding", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("pathfindingObstacleLayers"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("obstacleAvoidanceStrength"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("debugDrawPathfindingRays"));

        EditorGUILayout.Space();

        // Animation Configuration
        EditorGUILayout.LabelField("Animation Configuration", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("idleAnimationName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("idleUpAnimationName"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("moveAnimationName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("moveUpAnimationName"));

        EditorGUILayout.Space();

        // Death
        EditorGUILayout.LabelField("Death", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onDeathAbility"));

        EditorGUILayout.Space();

        // Loot Drops
        EditorGUILayout.LabelField("Loot Drops", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("dropTable"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxDrops"));

        serializedObject.ApplyModifiedProperties();
    }
}
