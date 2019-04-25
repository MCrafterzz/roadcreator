﻿using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PrefabLineCreator))]
public class PrefabLineEditor : Editor
{

    PrefabLineCreator prefabCreator;
    Vector3[] points = null;
    Tool lastTool;

    private void OnEnable()
    {
        prefabCreator = (PrefabLineCreator)target;
        prefabCreator.Setup();

        lastTool = Tools.current;
        Tools.current = Tool.None;

        Undo.undoRedoPerformed += prefabCreator.UndoUpdate;
    }

    private void OnDisable()
    {
        Tools.current = lastTool;
    }

    public override void OnInspectorGUI()
    {
        if (prefabCreator.spacing == -1)
        {
            if (prefabCreator.prefab == null)
            {
                prefabCreator.prefab = (GameObject)prefabCreator.settings.FindProperty("defaultPrefabLinePrefab").objectReferenceValue;
            }

            prefabCreator.spacing = prefabCreator.prefab.GetComponent<MeshFilter>().sharedMesh.bounds.extents.x * 2 * prefabCreator.xScale;
        }

        EditorGUI.BeginChangeCheck();
        prefabCreator.pointCalculationDivisions = Mathf.Clamp(EditorGUILayout.FloatField("Point Calculation Divisions", prefabCreator.pointCalculationDivisions), 1, 1000);
        prefabCreator.rotateAlongCurve = EditorGUILayout.Toggle("Rotate Alongst Curve", prefabCreator.rotateAlongCurve);
        if (prefabCreator.rotateAlongCurve == true)
        {
            prefabCreator.rotationDirection = (PrefabLineCreator.RotationDirection)EditorGUILayout.EnumPopup("Rotation Direction", prefabCreator.rotationDirection);

            if (prefabCreator.fillGap == false)
            {
                prefabCreator.yRotationRandomization = Mathf.Clamp(EditorGUILayout.FloatField("Y Rotation Randomization", prefabCreator.yRotationRandomization), 0, 360);
            }
            else
            {
                prefabCreator.yRotationRandomization = 0;
            }

            prefabCreator.bendObjects = GUILayout.Toggle(prefabCreator.bendObjects, "Bend Objects");
        }
        else if (prefabCreator.bendObjects == true)
        {
            Debug.Log("Rotate alongst curve has to be true to be able to use bend objects");
            prefabCreator.bendObjects = false;
        }

        if (prefabCreator.rotateAlongCurve == true)
        {
            prefabCreator.fillGap = GUILayout.Toggle(prefabCreator.fillGap, "Fill Gap");
        }
        else if (prefabCreator.fillGap == true)
        {
            Debug.Log("Rotate alongst curve has to be true to be able to use fill gap");
            prefabCreator.fillGap = false;
        }

        if (prefabCreator.fillGap == false && prefabCreator.bendObjects == false)
        {
            prefabCreator.spacing = Mathf.Max(0.1f, EditorGUILayout.FloatField("Spacing", prefabCreator.spacing));

            if (GUILayout.Button("Calculate Spacing (X)") == true)
            {
                prefabCreator.spacing = prefabCreator.prefab.GetComponent<MeshFilter>().sharedMesh.bounds.extents.x * 2 * prefabCreator.xScale;
            }

            if (GUILayout.Button("Calculate Spacing (Z)") == true)
            {
                if (prefabCreator.prefab != null)
                {
                    prefabCreator.spacing = prefabCreator.prefab.GetComponent<MeshFilter>().sharedMesh.bounds.extents.z * 2 * prefabCreator.zScale;
                }
            }
        }

        prefabCreator.yModification = (PrefabLineCreator.YModification)EditorGUILayout.EnumPopup("Vertex Y Modification", prefabCreator.yModification);

        if (prefabCreator.yModification == PrefabLineCreator.YModification.matchTerrain)
        {
            prefabCreator.terrainCheckHeight = Mathf.Max(0, EditorGUILayout.FloatField("Check Terrain Height", prefabCreator.terrainCheckHeight));
        }

        prefabCreator.prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefabCreator.prefab, typeof(GameObject), false);
        prefabCreator.startPrefab = (GameObject)EditorGUILayout.ObjectField("Start Prefab", prefabCreator.startPrefab, typeof(GameObject), false);
        prefabCreator.endPrefab = (GameObject)EditorGUILayout.ObjectField("End Prefab", prefabCreator.endPrefab, typeof(GameObject), false);
        prefabCreator.xScale = Mathf.Clamp(EditorGUILayout.FloatField("Prefab X Scale", prefabCreator.xScale), 0.1f, 10);
        prefabCreator.yScale = Mathf.Clamp(EditorGUILayout.FloatField("Prefab Y Scale", prefabCreator.yScale), 0.1f, 10);
        prefabCreator.zScale = Mathf.Clamp(EditorGUILayout.FloatField("Prefab Z Scale", prefabCreator.zScale), 0.1f, 10);

        if (EditorGUI.EndChangeCheck() == true)
        {
            if (prefabCreator.prefab == null)
            {
                prefabCreator.prefab = (GameObject)prefabCreator.settings.FindProperty("defaultPrefabLinePrefab").objectReferenceValue;
            }
            else if (prefabCreator.prefab.GetComponent<MeshFilter>() == null)
            {
                prefabCreator.prefab = (GameObject)prefabCreator.settings.FindProperty("defaultPrefabLinePrefab").objectReferenceValue;
                Debug.Log("Selected prefab must have a mesh filter attached. Prefab has been changed to the concrete barrier");
                return;
            }

            if (prefabCreator.fillGap == true || prefabCreator.bendObjects == true)
            {
                prefabCreator.spacing = prefabCreator.prefab.GetComponent<MeshFilter>().sharedMesh.bounds.extents.x * 2 * prefabCreator.xScale;
            }

            prefabCreator.PlacePrefabs();
        }

        GUILayout.Space(20);

        if (GUILayout.Button("Reset"))
        {
            for (int i = 1; i >= 0; i--)
            {
                for (int j = prefabCreator.transform.GetChild(i).childCount - 1; j >= 0; j--)
                {
                    Undo.DestroyObjectImmediate(prefabCreator.transform.GetChild(i).GetChild(j).gameObject);
                }
            }
        }

        if (GUILayout.Button("Place Prefabs"))
        {
            prefabCreator.PlacePrefabs();
        }
    }

    public void OnSceneGUI()
    {
        if (prefabCreator.hideFlags != HideFlags.NotEditable)
        {
            Event guiEvent = Event.current;

            Ray ray = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);

            RaycastHit raycastHit;
            if (Physics.Raycast(ray, out raycastHit, 100f, ~(1 << prefabCreator.settings.FindProperty("ignoreMouseRayLayer").intValue | 1 << prefabCreator.settings.FindProperty("roadLayer").intValue)))
            {
                Vector3 hitPosition = raycastHit.point;

                if (guiEvent.control == true)
                {
                    Vector3 nearestGuideline = Misc.GetNearestGuidelinePoint(hitPosition);
                    if (nearestGuideline != Misc.MaxVector3)
                    {
                        hitPosition = new Vector3(nearestGuideline.x, hitPosition.y, nearestGuideline.z);
                    }
                    else
                    {
                        hitPosition = new Vector3(Mathf.Round(hitPosition.x), hitPosition.y, Mathf.Round(hitPosition.z));
                    }
                }

                if (guiEvent.type == EventType.MouseDown)
                {
                    if (guiEvent.button == 0)
                    {
                        if (guiEvent.shift == true)
                        {
                            prefabCreator.CreatePoints(hitPosition);
                        }
                    }
                    else if (guiEvent.button == 1 && guiEvent.shift == true)
                    {
                        prefabCreator.RemovePoints();
                    }
                }

                if (prefabCreator.transform.childCount > 0 && prefabCreator.transform.GetChild(0).childCount > 1 && (guiEvent.type == EventType.MouseDrag || guiEvent.type == EventType.MouseMove || guiEvent.type == EventType.MouseDown))
                {
                    points = CalculateTemporaryPoints(guiEvent, hitPosition);
                }

                Draw(guiEvent, hitPosition);
            }

            if (Physics.Raycast(ray, out raycastHit, 100f, ~(1 << prefabCreator.settings.FindProperty("roadLayer").intValue)))
            {
                Vector3 hitPosition = raycastHit.point;

                if (guiEvent.control == true)
                {
                    Vector3 nearestGuideline = Misc.GetNearestGuidelinePoint(hitPosition);
                    if (nearestGuideline != Misc.MaxVector3)
                    {
                        hitPosition = new Vector3(nearestGuideline.x, hitPosition.y, nearestGuideline.z);
                    }
                    else
                    {
                        hitPosition = new Vector3(Mathf.Round(hitPosition.x), hitPosition.y, Mathf.Round(hitPosition.z));
                    }
                }

                if (guiEvent.shift == false)
                {
                    prefabCreator.MovePoints(hitPosition, guiEvent, raycastHit);
                }
                else
                {
                    prefabCreator.MovePoints(Misc.MaxVector3, guiEvent, raycastHit);
                }
            }

            GameObject.FindObjectOfType<RoadSystem>().ShowCreationButtons();
            SceneView.currentDrawingSceneView.Repaint();
        }
    }

    private void Draw(Event guiEvent, Vector3 hitPosition)
    {
        Misc.DrawRoadGuidelines(hitPosition, prefabCreator.objectToMove, null);

        for (int i = 0; i < prefabCreator.transform.GetChild(0).childCount; i++)
        {
            if (prefabCreator.transform.GetChild(0).GetChild(i).name == "Point")
            {
                Handles.color = prefabCreator.settings.FindProperty("pointColour").colorValue;
                Handles.CylinderHandleCap(0, prefabCreator.transform.GetChild(0).GetChild(i).position, Quaternion.Euler(90, 0, 0), prefabCreator.settings.FindProperty("pointSize").floatValue, EventType.Repaint);
            }
            else
            {
                Handles.color = prefabCreator.settings.FindProperty("controlPointColour").colorValue;
                Handles.CylinderHandleCap(0, prefabCreator.transform.GetChild(0).GetChild(i).position, Quaternion.Euler(90, 0, 0), prefabCreator.settings.FindProperty("pointSize").floatValue, EventType.Repaint);
            }
        }

        for (int j = 1; j < prefabCreator.transform.GetChild(0).childCount; j += 2)
        {
            Handles.color = Color.white;
            Handles.DrawLine(prefabCreator.transform.GetChild(0).GetChild(j - 1).position, prefabCreator.transform.GetChild(0).GetChild(j).position);

            if (j < prefabCreator.transform.GetChild(0).childCount - 1)
            {
                Handles.DrawLine(prefabCreator.transform.GetChild(0).GetChild(j).position, prefabCreator.transform.GetChild(0).GetChild(j + 1).position);
            }
            else if (guiEvent.shift == true)
            {
                Handles.DrawLine(prefabCreator.transform.GetChild(0).GetChild(j).position, hitPosition);
            }
        }

        if (prefabCreator.transform.childCount > 0)
        {
            if (guiEvent.shift == true)
            {
                Handles.color = Color.black;
                if (prefabCreator.transform.GetChild(0).childCount > 1 && prefabCreator.transform.GetChild(0).GetChild(prefabCreator.transform.GetChild(0).childCount - 1).name == "Control Point")
                {
                    Handles.DrawPolyLine(points);
                }
                else if (prefabCreator.transform.GetChild(0).childCount > 0)
                {
                    Handles.DrawLine(prefabCreator.transform.GetChild(0).GetChild(prefabCreator.transform.GetChild(0).childCount - 1).position, hitPosition);
                }
            }
        }

        // Mouse position
        Handles.color = prefabCreator.settings.FindProperty("cursorColour").colorValue;
        Handles.CylinderHandleCap(0, hitPosition, Quaternion.Euler(90, 0, 0), prefabCreator.settings.FindProperty("pointSize").floatValue, EventType.Repaint);
    }

    private Vector3[] CalculateTemporaryPoints(Event guiEvent, Vector3 hitPosition)
    {
        float divisions;
        int lastIndex = prefabCreator.transform.GetChild(0).GetChild(prefabCreator.transform.GetChild(0).childCount - 1).GetSiblingIndex();
        if (prefabCreator.transform.GetChild(0).GetChild(lastIndex).name == "Point")
        {
            lastIndex -= 1;
        }

        divisions = Misc.CalculateDistance(prefabCreator.transform.GetChild(0).GetChild(lastIndex - 1).position, prefabCreator.transform.GetChild(0).GetChild(lastIndex).position, hitPosition);

        divisions = Mathf.Max(2, divisions);
        List<Vector3> points = new List<Vector3>();
        float distancePerDivision = 1 / divisions;

        for (float t = 0; t <= 1; t += distancePerDivision / prefabCreator.pointCalculationDivisions)
        {
            if (t > 1 - distancePerDivision / prefabCreator.pointCalculationDivisions)
            {
                t = 1;
            }

            Vector3 position = Misc.Lerp3(prefabCreator.transform.GetChild(0).GetChild(lastIndex - 1).position, prefabCreator.transform.GetChild(0).GetChild(lastIndex).position, hitPosition, t);

            RaycastHit raycastHit;
            if (Physics.Raycast(position, Vector3.down, out raycastHit, 100f, ~(1 << prefabCreator.settings.FindProperty("ignoreMouseRayLayer").intValue | 1 << prefabCreator.settings.FindProperty("roadLayer").intValue)))
            {
                position.y = raycastHit.point.y;
            }

            points.Add(position);
        }

        return points.ToArray();
    }
}
