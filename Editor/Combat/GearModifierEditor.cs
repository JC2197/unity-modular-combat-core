using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for GearModifier with recalculate tier button
/// </summary>
[CustomEditor(typeof(GearModifier))]
public class GearModifierEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GearModifier modifier = (GearModifier)target;
        
        serializedObject.Update();
        
        // Draw default inspector
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        
        // Recalculate button
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Tier Recalculation", EditorStyles.boldLabel);
        
        if (modifier.tierScalingConfig == null)
        {
            EditorGUILayout.HelpBox("Assign a TierScalingConfig to enable tier recalculation.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox("Click this button to recalculate all Tier II-VI values based on Tier I ranges and the assigned scaling configuration. This will overwrite existing tier values!", MessageType.Info);
            
            if (GUILayout.Button("Recalculate Tier Values", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Recalculate Tier Values",
                    $"This will recalculate all Tier II-VI values for '{modifier.label}' based on Tier I ranges and the current scaling configuration.\n\nThis action cannot be undone. Continue?",
                    "Recalculate", "Cancel"))
                {
                    Undo.RecordObject(modifier, "Recalculate Tier Values");
                    EditorUtility.SetDirty(modifier);
                    serializedObject.Update();
                }
            }
        }
        
        EditorGUILayout.EndVertical();
        
        serializedObject.ApplyModifiedProperties();
    }
}
