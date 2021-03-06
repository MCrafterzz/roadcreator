﻿#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RoadSegment))]
[CanEditMultipleObjects]
public class RoadSegmentEditor : Editor
{

    private void OnEnable()
    {
        for (int i = 0; i < targets.Length; i++)
        {
            RoadSegment roadSegment = (RoadSegment)targets[i];
            if (roadSegment.settings == null)
            {
                roadSegment.settings = RoadCreatorSettings.GetSerializedSettings();
            }

            if (roadSegment.transform.parent != null)
            {
                if (roadSegment.transform.parent.parent.GetComponent<RoadCreator>().startIntersection != null)
                {
                    roadSegment.transform.parent.parent.GetComponent<RoadCreator>().startIntersection.FixConnectionReferences();
                }

                if (roadSegment.transform.parent.parent.GetComponent<RoadCreator>().endIntersection != null)
                {
                    roadSegment.transform.parent.parent.GetComponent<RoadCreator>().endIntersection.FixConnectionReferences();
                }
            }
        }
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        serializedObject.FindProperty("baseRoadMaterial").objectReferenceValue = (Material)EditorGUILayout.ObjectField("Base Road Material", serializedObject.FindProperty("baseRoadMaterial").objectReferenceValue, typeof(Material), false);
        serializedObject.FindProperty("overlayRoadMaterial").objectReferenceValue = (Material)EditorGUILayout.ObjectField("Overlay Road Material", serializedObject.FindProperty("overlayRoadMaterial").objectReferenceValue, typeof(Material), false);
        serializedObject.FindProperty("roadPhysicsMaterial").objectReferenceValue = (PhysicMaterial)EditorGUILayout.ObjectField("Road Physic Material", serializedObject.FindProperty("roadPhysicsMaterial").objectReferenceValue, typeof(PhysicMaterial), false);

        if (EditorGUI.EndChangeCheck() == true)
        {
            Change();
        }

        EditorGUI.BeginChangeCheck();
        serializedObject.FindProperty("startRoadWidth").floatValue = Mathf.Max(0.01f, EditorGUILayout.FloatField("Start Road Width", serializedObject.FindProperty("startRoadWidth").floatValue));
        serializedObject.FindProperty("endRoadWidth").floatValue = Mathf.Max(0.01f, EditorGUILayout.FloatField("End Road Width", serializedObject.FindProperty("endRoadWidth").floatValue));
        if (EditorGUI.EndChangeCheck() == true)
        {
            Change();
            for (int i = 0; i < targets.Length; i++)
            {
                RoadCreator roadCreator = ((RoadSegment)targets[i]).transform.parent.parent.GetComponent<RoadCreator>();
                RoadSegment roadSegment = (RoadSegment)targets[i];

                if (roadSegment.transform.GetSiblingIndex() == roadSegment.transform.parent.childCount - 1)
                {
                    if (roadCreator.endIntersection != null)
                    {
                        roadCreator.CreateMesh();
                        roadCreator.endIntersectionConnection.leftPoint = roadSegment.transform.GetChild(1).GetChild(0).GetComponent<MeshFilter>().sharedMesh.vertices[roadSegment.transform.GetChild(1).GetChild(0).GetComponent<MeshFilter>().sharedMesh.vertices.Length - 2] + roadSegment.transform.position;
                        roadCreator.endIntersectionConnection.rightPoint = roadSegment.transform.GetChild(1).GetChild(0).GetComponent<MeshFilter>().sharedMesh.vertices[roadSegment.transform.GetChild(1).GetChild(0).GetComponent<MeshFilter>().sharedMesh.vertices.Length - 1] + roadSegment.transform.position;
                        roadCreator.endIntersection.CreateMesh();
                    }
                }
                else if (roadSegment.transform.GetSiblingIndex() == 0)
                {
                    if (roadCreator.startIntersection != null)
                    {
                        roadCreator.CreateMesh();
                        roadCreator.startIntersectionConnection.leftPoint = roadSegment.transform.GetChild(1).GetChild(0).GetComponent<MeshFilter>().sharedMesh.vertices[1] + roadSegment.transform.position;
                        roadCreator.startIntersectionConnection.rightPoint = roadSegment.transform.GetChild(1).GetChild(0).GetComponent<MeshFilter>().sharedMesh.vertices[0] + roadSegment.transform.position;
                        roadCreator.startIntersection.CreateMesh();
                    }
                }
            }
        }

        EditorGUI.BeginChangeCheck();
        serializedObject.FindProperty("flipped").boolValue = EditorGUILayout.Toggle("Road Flipped", serializedObject.FindProperty("flipped").boolValue);
        serializedObject.FindProperty("textureTilingY").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Texture Tiling Y Multiplier", serializedObject.FindProperty("textureTilingY").floatValue), 0.01f, 10);

        if (serializedObject.FindProperty("generateSimpleBridge").boolValue == false && serializedObject.FindProperty("generateCustomBridge").boolValue == false)
        {
            serializedObject.FindProperty("terrainOption").enumValueIndex = (int)(RoadSegment.TerrainOption)EditorGUILayout.EnumPopup("Terrain Option", (RoadSegment.TerrainOption)Enum.GetValues(typeof(RoadSegment.TerrainOption)).GetValue(serializedObject.FindProperty("terrainOption").enumValueIndex));

            if (serializedObject.FindProperty("terrainOption").enumValueIndex == (int)RoadSegment.TerrainOption.ignore)
            {
                GUIStyle style = new GUIStyle();
                style.fontStyle = FontStyle.Bold;

                GUILayout.Space(20);
                GUILayout.Label("Y Offsets", style);
                serializedObject.FindProperty("leftStartYOffset").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Left Start Y Offset", serializedObject.FindProperty("leftStartYOffset").floatValue), -10, 10);
                serializedObject.FindProperty("leftEndYOffset").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Left End Y Offset", serializedObject.FindProperty("leftEndYOffset").floatValue), -10, 10);
                serializedObject.FindProperty("rightStartYOffset").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Right Start Y Offset", serializedObject.FindProperty("rightStartYOffset").floatValue), -10, 10);
                serializedObject.FindProperty("rightEndYOffset").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Right End Y Offset", serializedObject.FindProperty("rightEndYOffset").floatValue), -10, 10);
            }
        }

        GUIStyle guiStyle = new GUIStyle();
        guiStyle.fontStyle = FontStyle.Bold;

        GUILayout.Space(20);
        GUILayout.Label("Bridge", guiStyle);

        SerializedProperty bridgeSettings = serializedObject.FindProperty("bridgeSettings");

        if (serializedObject.FindProperty("generateSimpleBridge").boolValue == true || serializedObject.FindProperty("generateCustomBridge").boolValue == true)
        {
            bridgeSettings.FindPropertyRelative("extraWidth").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Extra Width", bridgeSettings.FindPropertyRelative("extraWidth").floatValue), 0, 1);
            GUILayout.Space(20);
        }

        serializedObject.FindProperty("generateSimpleBridge").boolValue = EditorGUILayout.Toggle("Generate Simple Bridge", serializedObject.FindProperty("generateSimpleBridge").boolValue);
        if (serializedObject.FindProperty("generateSimpleBridge").boolValue == true)
        {
            EditorGUILayout.PropertyField(bridgeSettings.FindPropertyRelative("bridgeMaterials"), true);
            bridgeSettings.FindPropertyRelative("yOffsetFirstStep").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Y Offset First Step", bridgeSettings.FindPropertyRelative("yOffsetFirstStep").floatValue), 0, 2);
            bridgeSettings.FindPropertyRelative("yOffsetSecondStep").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Y Offset Second Step", bridgeSettings.FindPropertyRelative("yOffsetSecondStep").floatValue), 0, 2);
            bridgeSettings.FindPropertyRelative("widthPercentageFirstStep").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Width Percentage First Step", bridgeSettings.FindPropertyRelative("widthPercentageFirstStep").floatValue), 0, 1);
            bridgeSettings.FindPropertyRelative("widthPercentageSecondStep").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Width Percentage Second Step", bridgeSettings.FindPropertyRelative("widthPercentageSecondStep").floatValue), 0, 1);
        }

        GUILayout.Space(20);
        GUILayout.Label("Custom Bridge Mesh", guiStyle);

        serializedObject.FindProperty("generateCustomBridge").boolValue = EditorGUILayout.Toggle("Generate Custom Bridge", serializedObject.FindProperty("generateCustomBridge").boolValue);
        if (serializedObject.FindProperty("generateCustomBridge").boolValue == true)
        {
            bridgeSettings.FindPropertyRelative("bridgeMesh").objectReferenceValue = (GameObject)EditorGUILayout.ObjectField("Bridge Mesh", bridgeSettings.FindPropertyRelative("bridgeMesh").objectReferenceValue, typeof(GameObject), false);
            bridgeSettings.FindPropertyRelative("sections").floatValue = (int)Mathf.Clamp(EditorGUILayout.FloatField("Sections", bridgeSettings.FindPropertyRelative("sections").floatValue), 1, 20);
            bridgeSettings.FindPropertyRelative("yScale").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Y Scale", bridgeSettings.FindPropertyRelative("yScale").floatValue), 0.1f, 10);
            bridgeSettings.FindPropertyRelative("xOffset").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("X Offset", bridgeSettings.FindPropertyRelative("xOffset").floatValue), -2, 2);
            bridgeSettings.FindPropertyRelative("adaptToTerrain").boolValue = EditorGUILayout.Toggle("Adapt Bridge To Terrain", bridgeSettings.FindPropertyRelative("adaptToTerrain").boolValue);
        }

        GUILayout.Space(20);
        GUILayout.Label("Pillar Placement", guiStyle);
        if (serializedObject.FindProperty("generateSimpleBridge").boolValue == true || serializedObject.FindProperty("generateCustomBridge").boolValue == true)
        {
            serializedObject.FindProperty("placePillars").boolValue = EditorGUILayout.Toggle("Place Pillars", serializedObject.FindProperty("placePillars").boolValue);

            if (serializedObject.FindProperty("placePillars").boolValue == true)
            {
                serializedObject.FindProperty("pillarPrefab").objectReferenceValue = (GameObject)EditorGUILayout.ObjectField("Prefab", serializedObject.FindProperty("pillarPrefab").objectReferenceValue, typeof(GameObject), false);

                if (serializedObject.FindProperty("generateCustomBridge").boolValue == true)
                {
                    serializedObject.FindProperty("adaptGapToCustomBridge").boolValue = EditorGUILayout.Toggle("Adapt To Custom Bridge", serializedObject.FindProperty("adaptGapToCustomBridge").boolValue);

                    if (serializedObject.FindProperty("adaptGapToCustomBridge").boolValue == false || serializedObject.FindProperty("generateCustomBridge").boolValue == false)
                    {
                        serializedObject.FindProperty("pillarGap").floatValue = Mathf.Max(0, EditorGUILayout.FloatField("Gap", serializedObject.FindProperty("pillarGap").floatValue));
                        serializedObject.FindProperty("pillarPlacementOffset").floatValue = Mathf.Max(0, EditorGUILayout.FloatField("Placement Offset", serializedObject.FindProperty("pillarPlacementOffset").floatValue));
                    }
                }
                else
                {
                    serializedObject.FindProperty("pillarGap").floatValue = Mathf.Max(0, EditorGUILayout.FloatField("Gap", serializedObject.FindProperty("pillarGap").floatValue));
                    serializedObject.FindProperty("pillarPlacementOffset").floatValue = Mathf.Max(0, EditorGUILayout.FloatField("Placement Offset", serializedObject.FindProperty("pillarPlacementOffset").floatValue));
                }

                serializedObject.FindProperty("extraPillarHeight").floatValue = Mathf.Max(0, EditorGUILayout.FloatField("Extra Height", serializedObject.FindProperty("extraPillarHeight").floatValue));

                if (serializedObject.FindProperty("adaptGapToCustomBridge").boolValue == false)
                {
                    serializedObject.FindProperty("xPillarScale").floatValue = Mathf.Max(0.1f, EditorGUILayout.FloatField("X Scale", serializedObject.FindProperty("xPillarScale").floatValue));
                }
                else
                {
                    serializedObject.FindProperty("xPillarScaleMultiplier").floatValue = Mathf.Max(0.1f, EditorGUILayout.FloatField("X Scale Multiplier", serializedObject.FindProperty("xPillarScaleMultiplier").floatValue));
                }

                serializedObject.FindProperty("zPillarScale").floatValue = Mathf.Max(0.1f, EditorGUILayout.FloatField("Z Scale", serializedObject.FindProperty("zPillarScale").floatValue));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("pillarRotationDirection"), new GUIContent("Rotation Direction"), true);
            }
        }

        GUILayout.Space(20);
        GUILayout.Label("Extra Meshes", guiStyle);
        RoadSegment inspectedSegment = (RoadSegment)target;
        for (int i = 0; i < inspectedSegment.extraMeshes.Count; i++)
        {
            bool open = EditorGUILayout.Foldout(inspectedSegment.extraMeshes[i].open, "Extra Mesh " + i);
            if (open == true)
            {
                bool left = EditorGUILayout.Toggle("Left", inspectedSegment.extraMeshes[i].left);
                Material baseMaterial = (Material)EditorGUILayout.ObjectField("Base Material", inspectedSegment.extraMeshes[i].baseMaterial, typeof(Material), false);
                Material overlayMaterial = (Material)EditorGUILayout.ObjectField("Overlay Material", inspectedSegment.extraMeshes[i].overlayMaterial, typeof(Material), false);
                PhysicMaterial physicMaterial = (PhysicMaterial)EditorGUILayout.ObjectField("Physic Material", inspectedSegment.extraMeshes[i].physicMaterial, typeof(PhysicMaterial), false);
                float startWidth = Mathf.Max(EditorGUILayout.FloatField("Start Width", inspectedSegment.extraMeshes[i].startWidth), 0);
                float endWidth = Mathf.Max(EditorGUILayout.FloatField("End Width", inspectedSegment.extraMeshes[i].endWidth), 0);
                bool flipped = EditorGUILayout.Toggle("Flipped", inspectedSegment.extraMeshes[i].flipped);
                float yOffset = EditorGUILayout.FloatField("Y Offset", inspectedSegment.extraMeshes[i].yOffset);

                for (int j = 0; j < targets.Length; j++)
                {
                    RoadSegment currentSegment = (RoadSegment)targets[j];
                    if (currentSegment.extraMeshes.Count > i)
                    {
                        currentSegment.extraMeshes[i] = new ExtraMesh(open, left, baseMaterial, overlayMaterial, physicMaterial, startWidth, endWidth, flipped, yOffset);
                    }
                }

                if (GUILayout.Button("Duplicate Extra Mesh") == true)
                {
                    for (int j = 0; j < targets.Length; j++)
                    {
                        RoadSegment currentSegment = (RoadSegment)targets[j];
                        currentSegment.extraMeshes.Add(currentSegment.extraMeshes[currentSegment.extraMeshes.Count - 1]);

                        GameObject extraMesh = new GameObject("Extra Mesh");
                        extraMesh.AddComponent<MeshFilter>();
                        extraMesh.AddComponent<MeshRenderer>();
                        extraMesh.AddComponent<MeshCollider>();
                        extraMesh.transform.SetParent(((RoadSegment)targets[i]).transform.GetChild(1));
                        extraMesh.transform.localPosition = Vector3.zero;
                        extraMesh.layer = LayerMask.NameToLayer("Road");
                        extraMesh.hideFlags = HideFlags.NotEditable;
                    }
                }

                if (GUILayout.Button("Remove Extra Mesh") == true)
                {
                    for (int j = 0; j < targets.Length; j++)
                    {
                        RoadSegment currentSegment = (RoadSegment)targets[j];
                        if (currentSegment.extraMeshes.Count > i)
                        {
                            currentSegment.extraMeshes.RemoveAt(i);
                            DestroyImmediate(currentSegment.transform.GetChild(1).GetChild(i + 1).gameObject);
                        }
                    }
                }
            }
            else
            {
                for (int j = 0; j < targets.Length; j++)
                {
                    RoadSegment currentSegment = (RoadSegment)targets[j];
                    if (currentSegment.extraMeshes.Count > i)
                    {
                        currentSegment.extraMeshes[i] = new ExtraMesh(open, currentSegment.extraMeshes[i].left, currentSegment.extraMeshes[i].baseMaterial, currentSegment.extraMeshes[i].overlayMaterial, currentSegment.extraMeshes[i].physicMaterial, currentSegment.extraMeshes[i].startWidth, currentSegment.extraMeshes[i].endWidth, currentSegment.extraMeshes[i].flipped, currentSegment.extraMeshes[i].yOffset);
                    }
                }
            }
        }

        if (GUILayout.Button("Add Extra Mesh"))
        {
            for (int i = 0; i < targets.Length; i++)
            {
                ((RoadSegment)targets[i]).extraMeshes.Add(new ExtraMesh(true, true, (Material)((RoadSegment)targets[i]).settings.FindProperty("defaultBaseMaterial").objectReferenceValue, (Material)((RoadSegment)targets[i]).settings.FindProperty("defaultExtraMeshOverlayMaterial").objectReferenceValue, null, 1, 1, false, 0));

                GameObject extraMesh = new GameObject("Extra Mesh");
                extraMesh.AddComponent<MeshFilter>();
                extraMesh.AddComponent<MeshRenderer>();
                extraMesh.AddComponent<MeshCollider>();
                extraMesh.transform.SetParent(((RoadSegment)targets[i]).transform.GetChild(1));
                extraMesh.transform.localPosition = Vector3.zero;
                extraMesh.layer = LayerMask.NameToLayer("Road");
                extraMesh.hideFlags = HideFlags.NotEditable;
            }
        }

        if (EditorGUI.EndChangeCheck())
        {
            Change();
        }

        GUILayout.Space(20);

        if (GUILayout.Button("Straighten"))
        {
            for (int i = 0; i < targets.Length; i++)
            {
                Transform points = ((RoadSegment)targets[i]).transform.GetChild(0);
                if (points.childCount == 3)
                {
                    points.parent.GetComponent<RoadSegment>().curved = false;
                    points.GetChild(1).position = Misc.GetCenter(points.GetChild(0).position, points.GetChild(2).position);
                }

                points.parent.parent.parent.GetComponent<RoadCreator>().CreateMesh();
            }
        }
    }

    private void Change()
    {
        serializedObject.ApplyModifiedPropertiesWithoutUndo();

        for (int i = 0; i < targets.Length; i++)
        {
            ((RoadSegment)targets[i]).transform.parent.parent.GetComponent<RoadCreator>().CreateMesh();
        }
    }

    private void OnSceneGUI()
    {
        ((RoadSegment)target).transform.parent.parent.parent.GetComponent<RoadSystem>().ShowCreationButtons();
    }

}
#endif
