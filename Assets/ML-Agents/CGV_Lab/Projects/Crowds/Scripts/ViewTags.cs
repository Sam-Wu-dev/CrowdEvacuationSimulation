using UnityEditor;
using UnityEngine;

// <summary>
/// Hierarchy Window Tag Info
/// This script displays each GameObject's tag in the Unity Hierarchy window.
/// </summary>
#if UNITY_EDITOR
[InitializeOnLoad]
public static class HierarchyWindowTagInfo
{
    static readonly GUIStyle _style = new GUIStyle()
    {
        fontSize = 9,
        alignment = TextAnchor.MiddleRight,
        normal = { textColor = Color.white } // Adjust color for visibility
    };

    static HierarchyWindowTagInfo()
    {
        EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
    }

    static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
    {
        var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

        if (gameObject != null)
        {
            EditorGUI.LabelField(selectionRect, gameObject.tag, _style);
        }
    }
}
#endif
