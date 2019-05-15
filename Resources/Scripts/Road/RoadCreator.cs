﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Presets;
using UnityEditor;

[HelpURL("https://github.com/MCrafterzz/roadcreator/wiki/Roads")]
public class RoadCreator : MonoBehaviour
{

    public float heightOffset = 0.02f;
    public bool createIntersections = true;

    public SerializedObject settings;
    public Preset segmentPreset;
    public GameObject objectToMove = null;
    public GameObject extraObjectToMove = null;
    private bool mouseDown;
    public bool sDown;
    public bool aDown;

    public Intersection startIntersection = null;
    public Intersection endIntersection = null;
    public IntersectionConnection startIntersectionConnection = null;
    public IntersectionConnection endIntersectionConnection = null;

    public void CreateMesh(bool fromIntersection = false)
    {
        if (this != null)
        {
            Vector3[] currentPoints = null;

            for (int i = 0; i < transform.GetChild(0).childCount; i++)
            {
                if (transform.GetChild(0).GetChild(i).GetComponent<RoadSegment>().terrainOption != RoadSegment.TerrainOption.ignore && (transform.GetChild(0).GetChild(i).GetComponent<RoadSegment>().generateSimpleBridge == true || transform.GetChild(0).GetChild(i).GetComponent<RoadSegment>().generateCustomBridge == true))
                {
                    transform.GetChild(0).GetChild(i).GetComponent<RoadSegment>().terrainOption = RoadSegment.TerrainOption.ignore;
                    Debug.Log("Terrain option has to be set to ignore to generate bridges");
                }
            }

            for (int i = 0; i < transform.GetChild(0).childCount; i++)
            {
                if (transform.GetChild(0).GetChild(i).GetChild(0).childCount == 3)
                {
                    Vector3 previousPoint = Misc.MaxVector3;

                    if (i == 0)
                    {
                        currentPoints = CalculatePoints(transform.GetChild(0).GetChild(i));

                        if (transform.GetChild(0).GetChild(i).GetSiblingIndex() == 0 && startIntersection != null && startIntersectionConnection != null)
                        {
                            previousPoint = startIntersectionConnection.lastPoint.ToNormalVector3() + (currentPoints[0] - currentPoints[1]).normalized;
                            previousPoint.y = startIntersection.yOffset + startIntersectionConnection.lastPoint.y;
                        }
                    }

                    if (i < transform.GetChild(0).childCount - 1 && transform.GetChild(0).GetChild(i + 1).GetChild(0).childCount == 3)
                    {
                        Vector3[] nextPoints = CalculatePoints(transform.GetChild(0).GetChild(i + 1));
                        nextPoints[0] = currentPoints[currentPoints.Length - 1];

                        if (i == 0)
                        {
                            transform.GetChild(0).GetChild(i).GetComponent<RoadSegment>().CreateRoadMesh(currentPoints, nextPoints, previousPoint, null, heightOffset, transform.GetChild(0).GetChild(i), null, this);
                        }
                        else
                        {
                            transform.GetChild(0).GetChild(i).GetComponent<RoadSegment>().CreateRoadMesh(currentPoints, nextPoints, previousPoint, transform.GetChild(0).GetChild(i - 1).GetChild(1).GetChild(0).GetComponent<MeshFilter>().sharedMesh.vertices, heightOffset, transform.GetChild(0).GetChild(i), transform.GetChild(0).GetChild(i - 1), this);
                        }

                        StartCoroutine(FixTextureStretch(Misc.CalculateDistance(transform.GetChild(0).GetChild(i).GetChild(0).GetChild(0).position, transform.GetChild(0).GetChild(i).GetChild(0).GetChild(1).position, transform.GetChild(0).GetChild(i).GetChild(0).GetChild(2).position), i));
                        currentPoints = nextPoints;
                    }
                    else
                    {
                        Vector3[] nextPoints = null;

                        if (transform.GetChild(0).GetChild(i).GetSiblingIndex() == transform.GetChild(0).childCount - 1 && endIntersection != null && endIntersectionConnection != null)
                        {
                            nextPoints = new Vector3[1];
                            nextPoints[0] = endIntersectionConnection.lastPoint.ToNormalVector3() + (currentPoints[currentPoints.Length - 1] - currentPoints[currentPoints.Length - 2]).normalized;
                            nextPoints[0].y = endIntersection.yOffset + endIntersectionConnection.lastPoint.y;
                        }

                        if (i - 1 >= 0)
                        {
                            transform.GetChild(0).GetChild(i).GetComponent<RoadSegment>().CreateRoadMesh(currentPoints, nextPoints, previousPoint, transform.GetChild(0).GetChild(i - 1).GetChild(1).GetChild(0).GetComponent<MeshFilter>().sharedMesh.vertices, heightOffset, transform.GetChild(0).GetChild(i), transform.GetChild(0).GetChild(i - 1), this);
                            StartCoroutine(FixTextureStretch(Misc.CalculateDistance(transform.GetChild(0).GetChild(i).GetChild(0).GetChild(0).position, transform.GetChild(0).GetChild(i).GetChild(0).GetChild(1).position, transform.GetChild(0).GetChild(i).GetChild(0).GetChild(2).position), i));
                        }
                        else
                        {
                            transform.GetChild(0).GetChild(i).GetComponent<RoadSegment>().CreateRoadMesh(currentPoints, nextPoints, previousPoint, null, heightOffset, transform.GetChild(0).GetChild(i), null, this);
                            StartCoroutine(FixTextureStretch(Misc.CalculateDistance(transform.GetChild(0).GetChild(i).GetChild(0).GetChild(0).position, transform.GetChild(0).GetChild(i).GetChild(0).GetChild(1).position, transform.GetChild(0).GetChild(i).GetChild(0).GetChild(2).position), i));
                        }
                    }
                }
                else
                {
                    for (int j = 0; j < transform.GetChild(0).GetChild(i).GetChild(1).childCount; j++)
                    {
                        transform.GetChild(0).GetChild(i).GetChild(1).GetChild(j).GetComponent<MeshFilter>().sharedMesh = null;
                        transform.GetChild(0).GetChild(i).GetChild(1).GetChild(j).GetComponent<MeshCollider>().sharedMesh = null;

                        if (transform.GetChild(0).GetChild(i).childCount == 3)
                        {
                            DestroyImmediate(transform.GetChild(0).GetChild(i).GetChild(2).gameObject);
                        }
                    }
                }
            }
        }

        if (fromIntersection == false)
        {
            if (startIntersectionConnection != null && startIntersection != null)
            {
                UpdateStartConnectionData();
                startIntersection.CreateMesh(true);
            }

            if (endIntersectionConnection != null && endIntersection != null)
            {
                UpdateEndConnectionData();
                endIntersection.CreateMesh(true);
            }
        }
    }

    IEnumerator FixTextureStretch(float length, int i)
    {
        yield return new WaitForSeconds(0.01f);

        if (transform.GetChild(0).childCount > i)
        {
            for (int j = 0; j < transform.GetChild(0).GetChild(i).GetChild(1).childCount; j++)
            {
                Material[] materials = transform.GetChild(0).GetChild(i).GetChild(1).GetChild(j).GetComponent<MeshRenderer>().sharedMaterials;
                float textureRepeat = length / 4 * transform.GetChild(0).GetChild(i).GetComponent<RoadSegment>().textureTilingY;

                for (int k = 0; k < transform.GetChild(0).GetChild(i).GetChild(1).GetChild(j).GetComponent<MeshRenderer>().sharedMaterials.Length; k++)
                {
                    if (transform.GetChild(0).GetChild(i).GetChild(1).GetChild(j).GetComponent<MeshRenderer>().sharedMaterials[k] != null)
                    {
                        materials[k] = new Material(transform.GetChild(0).GetChild(i).GetChild(1).GetChild(j).GetComponent<MeshRenderer>().sharedMaterials[k]);
                        materials[k].SetVector("_Tiling", new Vector2(1, textureRepeat));

                        if (i > 0)
                        {
                            float lastTextureRepeat = transform.GetChild(0).GetChild(i - 1).GetChild(1).GetChild(0).GetComponent<MeshRenderer>().sharedMaterial.GetVector("_Tiling").y;
                            float lastTextureOffset = transform.GetChild(0).GetChild(i - 1).GetChild(1).GetChild(0).GetComponent<MeshRenderer>().sharedMaterial.GetVector("_Offset").y;
                            materials[k].SetVector("_Offset", new Vector2(0, (lastTextureRepeat % 1.0f) + lastTextureOffset));
                        }
                        else
                        {
                            materials[k].SetVector("_Offset", new Vector2(0, 0));
                        }
                    }
                }

                transform.GetChild(0).GetChild(i).GetChild(1).GetChild(j).GetComponent<MeshRenderer>().sharedMaterials = materials;
            }

            if (transform.GetChild(0).GetChild(i).GetComponent<RoadSegment>().generateSimpleBridge == true)
            {
                if (transform.GetChild(0).GetChild(i).GetChild(2).GetComponent<MeshRenderer>().sharedMaterial != null)
                {
                    Material material = new Material(transform.GetChild(0).GetChild(i).GetChild(2).GetComponent<MeshRenderer>().sharedMaterial);
                    float textureRepeat = length / 4 * transform.GetChild(0).GetChild(i).GetComponent<RoadSegment>().textureTilingY;
                    material.SetVector("_Tiling", new Vector2(1, textureRepeat));
                    transform.GetChild(0).GetChild(i).GetChild(2).GetComponent<MeshRenderer>().sharedMaterial = material;
                }
            }
        }
    }

    public void UndoUpdate()
    {
        objectToMove = null;
        extraObjectToMove = null;

        Intersection[] intersections = GameObject.FindObjectsOfType<Intersection>();
        for (int i = 0; i < intersections.Length; i++)
        {
            intersections[i].CreateMesh();
        }

        CreateMesh();
    }

    public void CreatePoints(Vector3 hitPosition)
    {
        if (transform.GetChild(0).childCount > 0)
        {
            if (transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetChild(0).childCount == 1)
            {
                if (settings.FindProperty("roadCurved").boolValue == true)
                {
                    // Create control point
                    Undo.RegisterCreatedObjectUndo(CreatePoint("Control Point", transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetChild(0), hitPosition), "Created Point");
                }
                else
                {
                    // Create control and end points
                    Undo.RegisterCreatedObjectUndo(CreatePoint("Control Point", transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetChild(0), Misc.GetCenter(transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetChild(0).GetChild(0).position, hitPosition)), "Created Point");
                    Undo.RegisterCreatedObjectUndo(CreatePoint("End Point", transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetChild(0), hitPosition), "Created Point");
                    CreateMesh();
                }
            }
            else if (transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetChild(0).childCount == 2)
            {
                // Create end point
                Undo.RegisterCreatedObjectUndo(CreatePoint("End Point", transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetChild(0), hitPosition), "Created Point");
                CreateMesh();
            }
            else
            {
                RoadSegment segment = CreateSegment(transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetChild(0).GetChild(2).position);
                Undo.RegisterCreatedObjectUndo(segment.gameObject, "Create Point");
                Undo.RegisterCreatedObjectUndo(CreatePoint("Start Point", segment.transform.GetChild(0), segment.transform.position), "Created Point");

                if (settings.FindProperty("roadCurved").boolValue == true)
                {
                    segment.curved = true;
                    Undo.RegisterCreatedObjectUndo(CreatePoint("Control Point", segment.transform.GetChild(0), hitPosition), "Created Point");
                }
                else
                {
                    segment.curved = false;
                    Undo.RegisterCreatedObjectUndo(CreatePoint("Control Point", transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetChild(0), Misc.GetCenter(transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetChild(0).GetChild(0).position, hitPosition)), "Created Point");
                    Undo.RegisterCreatedObjectUndo(CreatePoint("End Point", transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetChild(0), hitPosition), "Created Point");
                    CreateMesh();
                }
            }
        }
        else
        {
            // Create first segment
            RoadSegment segment = CreateSegment(hitPosition);
            Undo.RegisterCreatedObjectUndo(segment.gameObject, "Create Point");
            Undo.RegisterCreatedObjectUndo(CreatePoint("Start Point", segment.transform.GetChild(0), hitPosition), "Created Point");

            if (settings.FindProperty("roadCurved").boolValue == true)
            {
                segment.curved = true;
            }
            else
            {
                segment.curved = false;
            }
        }
    }

    private GameObject CreatePoint(string name, Transform parent, Vector3 position)
    {
        GameObject point = new GameObject(name);
        point.gameObject.AddComponent<BoxCollider>();
        point.GetComponent<BoxCollider>().size = new Vector3(settings.FindProperty("pointSize").floatValue, settings.FindProperty("pointSize").floatValue, settings.FindProperty("pointSize").floatValue);
        point.transform.SetParent(parent);
        point.transform.position = position;
        point.GetComponent<BoxCollider>().hideFlags = HideFlags.NotEditable;
        point.layer = settings.FindProperty("ignoreMouseRayLayer").intValue;
        point.AddComponent<Point>();
        point.GetComponent<Point>().hideFlags = HideFlags.NotEditable;

        return point;
    }

    private RoadSegment CreateSegment(Vector3 position)
    {
        RoadSegment segment = new GameObject("Segment").AddComponent<RoadSegment>();
        segment.transform.SetParent(transform.GetChild(0), false);
        segment.transform.position = position;
        segment.transform.hideFlags = HideFlags.NotEditable;

        GameObject points = new GameObject("Points");
        points.transform.SetParent(segment.transform);
        points.transform.localPosition = Vector3.zero;
        points.hideFlags = HideFlags.NotEditable;

        GameObject meshes = new GameObject("Meshes");
        meshes.transform.SetParent(segment.transform);
        meshes.transform.localPosition = Vector3.zero;

        if (settings.FindProperty("hideNonEditableChildren").boolValue == true)
        {
            meshes.hideFlags = HideFlags.HideInHierarchy;
        }
        else
        {
            meshes.hideFlags = HideFlags.NotEditable;
        }

        GameObject mainMesh = new GameObject("Main Mesh");
        mainMesh.transform.SetParent(meshes.transform);
        mainMesh.transform.localPosition = Vector3.zero;
        mainMesh.hideFlags = HideFlags.NotEditable;
        mainMesh.AddComponent<MeshRenderer>();
        mainMesh.AddComponent<MeshFilter>();
        mainMesh.AddComponent<MeshCollider>();
        mainMesh.layer = settings.FindProperty("roadLayer").intValue;

        CopySegmentData(segment);

        return segment;
    }

    private void CopySegmentData(RoadSegment segment)
    {
        if (segmentPreset == null)
        {
            if (transform.GetChild(0).childCount > 1)
            {
                DuplicateSegmentData(segment);
            }
            else
            {
                segment.overlayRoadMaterial = (Material)settings.FindProperty("defaultRoadOverlayMaterial").objectReferenceValue;
            }
        }
        else
        {
            segmentPreset.ApplyTo(segment);

            for (int i = 0; i < segment.extraMeshes.Count; i++)
            {
                GameObject extraMesh = new GameObject("Extra Mesh");
                extraMesh.AddComponent<MeshFilter>();
                extraMesh.AddComponent<MeshRenderer>();
                extraMesh.AddComponent<MeshCollider>();
                extraMesh.transform.SetParent(segment.transform.GetChild(1));
                extraMesh.transform.localPosition = Vector3.zero;
                extraMesh.layer = settings.FindProperty("roadLayer").intValue;
                extraMesh.hideFlags = HideFlags.NotEditable;
            }
        }
    }

    private void DuplicateSegmentData(RoadSegment segment)
    {
        RoadSegment oldLastSegment = transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 2).GetComponent<RoadSegment>();
        segment.baseRoadMaterial = oldLastSegment.baseRoadMaterial;
        segment.overlayRoadMaterial = oldLastSegment.overlayRoadMaterial;
        segment.startRoadWidth = oldLastSegment.endRoadWidth;
        segment.endRoadWidth = oldLastSegment.endRoadWidth;
        segment.flipped = oldLastSegment.flipped;
        segment.terrainOption = oldLastSegment.terrainOption;

        segment.generateSimpleBridge = oldLastSegment.generateSimpleBridge;
        segment.generateCustomBridge = oldLastSegment.generateCustomBridge;
        segment.bridgeSettings = oldLastSegment.bridgeSettings;

        segment.placePillars = oldLastSegment.placePillars;
        segment.pillarPrefab = oldLastSegment.pillarPrefab;
        segment.pillarGap = oldLastSegment.pillarGap;
        segment.pillarPlacementOffset = oldLastSegment.pillarPlacementOffset;
        segment.extraPillarHeight = oldLastSegment.extraPillarHeight;
        segment.xPillarScale = oldLastSegment.xPillarScale;
        segment.zPillarScale = oldLastSegment.zPillarScale;

        for (int i = 0; i < oldLastSegment.extraMeshes.Count; i++)
        {
            GameObject extraMesh = new GameObject("Extra Mesh");
            extraMesh.AddComponent<MeshFilter>();
            extraMesh.AddComponent<MeshRenderer>();
            extraMesh.AddComponent<MeshCollider>();
            extraMesh.transform.SetParent(segment.transform.GetChild(1));
            extraMesh.transform.localPosition = Vector3.zero;
            extraMesh.layer = settings.FindProperty("roadLayer").intValue;
            extraMesh.hideFlags = HideFlags.NotEditable;

            segment.extraMeshes.Add(oldLastSegment.extraMeshes[i]);
            segment.extraMeshes[segment.extraMeshes.Count - 1].startWidth = oldLastSegment.extraMeshes[i].endWidth;
        }
    }

    public void SplitSegment(Vector3 hitPosition, RaycastHit raycastHit)
    {
        if (raycastHit.transform.parent.parent.parent.parent != null && raycastHit.transform.parent.parent.parent.parent.GetComponent<RoadCreator>() != null && raycastHit.transform.parent.parent.parent.parent.GetComponent<RoadCreator>() == this && raycastHit.transform.parent.parent.GetChild(0).childCount == 3 && aDown == true)
        {
            RoadSegment hitSegment = raycastHit.transform.parent.parent.GetComponent<RoadSegment>();
            RoadSegment segment = CreateSegment(raycastHit.point);
            segment.transform.SetSiblingIndex(hitSegment.transform.GetSiblingIndex() + 1);

            // Create new points
            Undo.RegisterCreatedObjectUndo(CreatePoint("Start Point", segment.transform.GetChild(0), raycastHit.point), "Created Point");
            Undo.RegisterCreatedObjectUndo(CreatePoint("Control Point", segment.transform.GetChild(0), Misc.GetCenter(raycastHit.point, hitSegment.transform.GetChild(0).GetChild(2).position)), "Created Point");
            Undo.RegisterCreatedObjectUndo(CreatePoint("End Point", segment.transform.GetChild(0), hitSegment.transform.GetChild(0).GetChild(2).position), "Created Point");

            // Move old points
            hitSegment.transform.GetChild(0).GetChild(2).transform.position = raycastHit.point;
            hitSegment.transform.GetChild(0).GetChild(1).transform.position = Misc.GetCenter(hitSegment.transform.GetChild(0).GetChild(0).transform.position, hitSegment.transform.GetChild(0).GetChild(2).transform.position);

            CreateMesh();
            RoadCreatorSettings.UpdateRoadGuidelines();
        }
    }

    public void UpdateStartConnectionData()
    {
        if (startIntersectionConnection != null && startIntersection != null)
        {
            UpdateStartConnectionVariables();
            startIntersection.connections.Sort();

            for (int i = 0; i < startIntersection.connections.Count; i++)
            {
                if (startIntersection.connections[i].road.transform.parent.parent.parent.parent.GetComponent<RoadCreator>().startIntersectionConnection == null || startIntersection.connections[i].road.transform.parent.parent.parent.parent.GetComponent<RoadCreator>().startIntersection == null || !startIntersection.connections[i].road.transform.parent.parent.parent.parent.GetComponent<RoadCreator>().startIntersectionConnection.road.Equals(startIntersection.connections[i].road))
                {
                    startIntersection.connections[i].road.transform.parent.parent.parent.parent.GetComponent<RoadCreator>().UpdateEndConnectionVariables();
                }
                else
                {
                    startIntersection.connections[i].road.transform.parent.parent.parent.parent.GetComponent<RoadCreator>().UpdateStartConnectionVariables();
                }
            }

            Vector3 totalPosition = Vector3.zero;
            for (int i = 0; i < startIntersection.connections.Count; i++)
            {
                totalPosition += startIntersection.connections[i].lastPoint.ToNormalVector3();
            }

            Vector3 newPosition = totalPosition / startIntersection.connections.Count;
            newPosition.y = startIntersection.transform.position.y;
            startIntersection.transform.position = newPosition;
            startIntersection.CreateMesh();
        }
    }

    public void UpdateEndConnectionData()
    {
        if (endIntersectionConnection != null && endIntersection != null)
        {
            UpdateEndConnectionVariables();
            endIntersection.connections.Sort();

            for (int i = 0; i < endIntersection.connections.Count; i++)
            {
                if (endIntersection.connections[i].road.transform.parent.parent.parent.parent.GetComponent<RoadCreator>().endIntersectionConnection == null || endIntersection.connections[i].road.transform.parent.parent.parent.parent.GetComponent<RoadCreator>().endIntersection == null || !endIntersection.connections[i].road.transform.parent.parent.parent.parent.GetComponent<RoadCreator>().endIntersectionConnection.road.Equals(endIntersection.connections[i].road))
                {
                    endIntersection.connections[i].road.transform.parent.parent.parent.parent.GetComponent<RoadCreator>().UpdateStartConnectionVariables();
                }
                else
                {
                    endIntersection.connections[i].road.transform.parent.parent.parent.parent.GetComponent<RoadCreator>().UpdateEndConnectionVariables();
                }
            }

            Vector3 totalPosition = Vector3.zero;
            for (int i = 0; i < endIntersection.connections.Count; i++)
            {
                totalPosition += endIntersection.connections[i].lastPoint.ToNormalVector3();
            }

            Vector3 newPosition = totalPosition / endIntersection.connections.Count;
            newPosition.y = endIntersection.transform.position.y;
            endIntersection.transform.position = newPosition;
            endIntersection.CreateMesh();
        }
    }

    public void UpdateStartConnectionVariables()
    {
        Vector3[] vertices = transform.GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetComponent<MeshFilter>().sharedMesh.vertices;
        startIntersectionConnection.leftPoint = new SerializedVector3(vertices[1] + transform.GetChild(0).GetChild(0).transform.position);
        startIntersectionConnection.leftPoint.y = startIntersection.transform.position.y;
        startIntersectionConnection.rightPoint = new SerializedVector3(vertices[0] + transform.GetChild(0).GetChild(0).transform.position);
        startIntersectionConnection.rightPoint.y = startIntersection.transform.position.y;
        startIntersectionConnection.lastPoint = new SerializedVector3(Misc.GetCenter(vertices[0], vertices[1]) + transform.GetChild(0).GetChild(0).transform.position);
        startIntersectionConnection.lastPoint.y = startIntersection.transform.position.y;
        startIntersectionConnection.length = Vector3.Distance(startIntersection.transform.position, startIntersectionConnection.lastPoint.ToNormalVector3());
        startIntersectionConnection.YRotation = Quaternion.LookRotation((startIntersection.transform.position - startIntersectionConnection.road.transform.position).normalized).eulerAngles.y;
    }

    public void UpdateEndConnectionVariables()
    {
        Vector3[] vertices = transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetChild(1).GetChild(0).GetComponent<MeshFilter>().sharedMesh.vertices;
        endIntersectionConnection.leftPoint = new SerializedVector3(vertices[vertices.Length - 2] + transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).position);
        endIntersectionConnection.leftPoint.y = endIntersection.transform.position.y;
        endIntersectionConnection.rightPoint = new SerializedVector3(vertices[vertices.Length - 1] + transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).position);
        endIntersectionConnection.rightPoint.y = endIntersection.transform.position.y;
        endIntersectionConnection.lastPoint = new SerializedVector3(Misc.GetCenter(vertices[vertices.Length - 1], vertices[vertices.Length - 2]) + transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).transform.position);
        endIntersectionConnection.lastPoint.y = endIntersection.transform.position.y;
        endIntersectionConnection.length = Vector3.Distance(endIntersection.transform.position, endIntersectionConnection.lastPoint.ToNormalVector3());
        endIntersectionConnection.YRotation = Quaternion.LookRotation((endIntersection.transform.position - endIntersectionConnection.road.transform.position).normalized).eulerAngles.y;
    }

    public void CheckForIntersectionGeneration(GameObject point)
    {
        if (createIntersections == true)
        {
            RaycastHit raycastHitPoint;
            RaycastHit raycastHitRoad;

            if (Physics.Raycast(point.transform.position + new Vector3(0, 1, 0), Vector3.down, out raycastHitPoint, 100, 1 << settings.FindProperty("ignoreMouseRayLayer").intValue) && raycastHitPoint.transform.GetComponent<Point>() != null && raycastHitPoint.transform.parent.parent.parent.parent.gameObject != point.transform.parent.parent.parent.parent.gameObject)
            {
                // Found Point
                if (point.transform.GetSiblingIndex() == 1 || raycastHitPoint.transform.GetSiblingIndex() == 1 || raycastHitPoint.transform.parent.parent.parent.parent.GetComponent<RoadCreator>().createIntersections == false)
                {
                    return;
                }

                if ((point.transform.name == "Start Point" && point.transform.parent.parent.GetSiblingIndex() != 0) || (point.transform.name == "End Point" && point.transform.parent.parent.GetSiblingIndex() != point.transform.parent.parent.parent.childCount - 1))
                {
                    return;
                }

                if ((raycastHitPoint.transform.name == "Start Point" && raycastHitPoint.transform.parent.parent.GetSiblingIndex() != 0) || (raycastHitPoint.transform.name == "End Point" && raycastHitPoint.transform.parent.parent.GetSiblingIndex() != raycastHitPoint.transform.parent.parent.parent.childCount - 1))
                {
                    return;
                }

                Vector3 creationPosition = raycastHitPoint.point;
                creationPosition.y = raycastHitPoint.transform.position.y;
                GameObject intersection = CreateIntersection(creationPosition, point.transform.parent.parent.GetComponent<RoadSegment>());

                if (point.transform.GetSiblingIndex() == 0 && startIntersection == null)
                {
                    CreateIntersectionConnectionForNewIntersectionFirst(point, intersection.GetComponent<Intersection>());
                }
                else if (point.transform.GetSiblingIndex() == 2 && endIntersection == null)
                {
                    CreateIntersectionConnectionForNewIntersectionLast(point, intersection.GetComponent<Intersection>());
                }

                if (raycastHitPoint.transform.GetSiblingIndex() == 0 && raycastHitPoint.transform.parent.parent.parent.parent.GetComponent<RoadCreator>().startIntersection == null)
                {
                    CreateIntersectionConnectionForNewIntersectionFirst(raycastHitPoint.transform.gameObject, intersection.GetComponent<Intersection>());
                }
                else if (raycastHitPoint.transform.GetSiblingIndex() == 2 && raycastHitPoint.transform.parent.parent.parent.parent.GetComponent<RoadCreator>().endIntersection == null)
                {
                    CreateIntersectionConnectionForNewIntersectionLast(raycastHitPoint.transform.gameObject, intersection.GetComponent<Intersection>());
                }

                intersection.GetComponent<Intersection>().ResetCurvePointPositions();
                intersection.GetComponent<Intersection>().ResetExtraMeshes();
                intersection.GetComponent<Intersection>().CreateMesh();
            }
            else if (Physics.Raycast(point.transform.position + new Vector3(0, 1, 0), Vector3.down, out raycastHitRoad, 100, settings.FindProperty("ignoreMouseRayLayer").intValue) && raycastHitRoad.transform.GetComponent<Intersection>() != null && sDown == false)
            {
                //Found intersection
                if (point.transform.GetSiblingIndex() == 0 && startIntersection == null)
                {
                    CreateMesh();
                    Vector3[] vertices = point.transform.parent.parent.GetChild(1).GetChild(0).GetComponent<MeshFilter>().sharedMesh.vertices;
                    Vector3 forward = Misc.GetCenter(vertices[0], vertices[1]) - Misc.GetCenter(vertices[2], vertices[3]);
                    point.transform.position += (-forward).normalized * 2;
                    CreateMesh();
                    Undo.RegisterCompleteObjectUndo(this, "Modify Intersection");
                    startIntersectionConnection = CreateIntersectionConnectionFirst(raycastHitRoad.transform.GetComponent<Intersection>(), point);
                    startIntersection = raycastHitRoad.transform.GetComponent<Intersection>();

                    UpdateStartConnectionData();
                    startIntersection.GetComponent<Intersection>().ResetCurvePointPositions();
                    startIntersection.GetComponent<Intersection>().ResetExtraMeshes();
                }
                else if (point.transform.GetSiblingIndex() == 2 && point.transform.parent.parent.parent.parent.GetComponent<RoadCreator>().endIntersection == null)
                {
                    CreateMesh();
                    Vector3[] vertices = point.transform.parent.parent.GetChild(1).GetChild(0).GetComponent<MeshFilter>().sharedMesh.vertices;
                    Vector3 forward = Misc.GetCenter(vertices[vertices.Length - 1], vertices[vertices.Length - 2]) - Misc.GetCenter(vertices[vertices.Length - 3], vertices[vertices.Length - 4]);
                    point.transform.position += (-forward).normalized * 2;
                    CreateMesh();
                    Undo.RegisterCompleteObjectUndo(this, "Modify Intersection");
                    endIntersectionConnection = CreateIntersectionConnectionLast(raycastHitRoad.transform.GetComponent<Intersection>(), point);
                    endIntersection = raycastHitRoad.transform.GetComponent<Intersection>();

                    UpdateEndConnectionData();
                    endIntersection.GetComponent<Intersection>().ResetCurvePointPositions();
                    endIntersection.GetComponent<Intersection>().ResetExtraMeshes();
                }
            }
            else
            {
                //Found nothing
                if (sDown == true)
                {
                    CreateMesh();
                }
                else
                {
                    if (point.transform.GetSiblingIndex() == 0 && startIntersectionConnection != null && startIntersection != null)
                    {
                        Intersection intersection = startIntersection;
                        int index = intersection.connections.FindIndex(i => i.road == startIntersectionConnection.road);
                        RemoveIntersectionConnection(intersection, index, true);

                        intersection.ResetCurvePointPositions();
                        intersection.ResetExtraMeshes();
                        intersection.CreateMesh();
                    }
                    else if (point.transform.GetSiblingIndex() == 2 && endIntersectionConnection != null && endIntersection != null)
                    {
                        Intersection intersection = endIntersection;
                        int index = intersection.connections.FindIndex(i => i.road == endIntersectionConnection.road);
                        RemoveIntersectionConnection(intersection, index, false);

                        intersection.ResetCurvePointPositions();
                        intersection.ResetExtraMeshes();
                        intersection.CreateMesh();
                    }
                }
            }
        }
    }

    public void RemoveIntersectionConnection(Intersection intersection, int connectionIndex, bool start)
    {
        Undo.RecordObject(intersection, "Remove Intersection");
        intersection.connections.RemoveAt(connectionIndex);

        if (start == true)
        {
            Undo.RecordObject(this, "Remove Intersection");
            startIntersection = null;
            startIntersectionConnection = null;
        }
        else
        {
            Undo.RecordObject(this, "Remove Intersection");
            endIntersection = null;
            endIntersectionConnection = null;
        }
    }

    public GameObject CreateIntersection(Vector3 position, RoadSegment segment)
    {
        GameObject intersection = new GameObject("Intersection");
        Undo.RegisterCreatedObjectUndo(intersection, "Create Intersection");
        intersection.transform.SetParent(transform.parent);
        intersection.transform.position = position;

        intersection.AddComponent<Intersection>();
        intersection.GetComponent<Intersection>().yOffset = heightOffset;
        intersection.GetComponent<Intersection>().generateBridge = segment.generateSimpleBridge;
        intersection.GetComponent<Intersection>().bridgeSettings = segment.bridgeSettings;
        intersection.GetComponent<Intersection>().placePillars = segment.placePillars;
        intersection.GetComponent<Intersection>().extraPillarHeight = segment.extraPillarHeight;
        intersection.GetComponent<Intersection>().xzPillarScale = segment.xPillarScale;
        intersection.GetComponent<Intersection>().overlayMaterial = (Material)settings.FindProperty("defaultIntersectionOverlayMaterial").objectReferenceValue;

        intersection.AddComponent<MeshFilter>();
        intersection.AddComponent<MeshRenderer>();
        intersection.AddComponent<MeshCollider>();
        intersection.GetComponent<Transform>().hideFlags = HideFlags.NotEditable;
        intersection.GetComponent<MeshFilter>().hideFlags = HideFlags.NotEditable;
        intersection.GetComponent<MeshCollider>().hideFlags = HideFlags.NotEditable;
        intersection.GetComponent<MeshRenderer>().hideFlags = HideFlags.NotEditable;

        GameObject extraMeshes = new GameObject("Extra Meshes");
        extraMeshes.transform.SetParent(intersection.transform, false);

        if (settings.FindProperty("hideNonEditableChildren").boolValue == true)
        {
            extraMeshes.hideFlags = HideFlags.HideInHierarchy;
        }
        else
        {
            extraMeshes.hideFlags = HideFlags.NotEditable;
        }

        return intersection;
    }

    public void CreateIntersectionConnectionForNewIntersectionFirst(GameObject gameObject, Intersection intersection)
    {
        CreateIntersectionConnectionForNewIntersection(gameObject, intersection, intersection.transform.position - gameObject.transform.parent.GetChild(2).position, true);
    }

    public void CreateIntersectionConnectionForNewIntersectionLast(GameObject gameObject, Intersection intersection)
    {
        CreateIntersectionConnectionForNewIntersection(gameObject, intersection, intersection.transform.position - gameObject.transform.parent.GetChild(0).position, false);
    }

    public void CreateIntersectionConnectionForNewIntersection(GameObject gameObject, Intersection intersection, Vector3 forward, bool first)
    {
        gameObject.transform.parent.parent.parent.parent.GetComponent<RoadCreator>().CreateMesh();
        Vector3[] vertices = gameObject.transform.parent.parent.GetChild(1).GetChild(0).GetComponent<MeshFilter>().sharedMesh.vertices;
        gameObject.transform.position += (-forward).normalized * 2;
        gameObject.transform.parent.parent.parent.parent.GetComponent<RoadCreator>().CreateMesh();

        if (first == true)
        {
            CreateIntersectionConnectionFirst(intersection.GetComponent<Intersection>(), gameObject);
            gameObject.transform.parent.parent.parent.parent.GetComponent<RoadCreator>().startIntersection = intersection;
            Undo.RecordObject(gameObject.transform.parent.parent.parent.parent.GetComponent<RoadCreator>(), "Create Intersection");
            gameObject.transform.parent.parent.parent.parent.GetComponent<RoadCreator>().startIntersectionConnection = intersection.connections[intersection.connections.Count - 1];
        }
        else
        {
            CreateIntersectionConnectionLast(intersection.GetComponent<Intersection>(), gameObject);
            gameObject.transform.parent.parent.parent.parent.GetComponent<RoadCreator>().endIntersection = intersection;
            Undo.RecordObject(gameObject.transform.parent.parent.parent.parent.GetComponent<RoadCreator>(), "Create Intersection");
            gameObject.transform.parent.parent.parent.parent.GetComponent<RoadCreator>().endIntersectionConnection = intersection.connections[intersection.connections.Count - 1];
        }
    }


    public IntersectionConnection CreateIntersectionConnectionFirst(Intersection intersection, GameObject point)
    {
        return CreateIntersectionConnection(intersection, point, 1, 0);
    }

    public IntersectionConnection CreateIntersectionConnectionLast(Intersection intersection, GameObject point)
    {
        return CreateIntersectionConnection(intersection, point, point.transform.parent.parent.GetChild(1).GetChild(0).GetComponent<MeshFilter>().sharedMesh.vertices.Length - 2, point.transform.parent.parent.GetChild(1).GetChild(0).GetComponent<MeshFilter>().sharedMesh.vertices.Length - 1);
    }

    public IntersectionConnection CreateIntersectionConnection(Intersection intersection, GameObject point, int firstVertex, int secondVertex)
    {
        IntersectionConnection intersectionConnection = new IntersectionConnection();
        Undo.RegisterCompleteObjectUndo(intersection, "Modify Intersection");
        intersection.connections.Add(intersectionConnection);

        intersectionConnection.leftPoint = new SerializedVector3(point.transform.parent.parent.GetChild(1).GetChild(0).GetComponent<MeshFilter>().sharedMesh.vertices[firstVertex] + point.transform.parent.parent.position);
        intersectionConnection.leftPoint.y = intersection.transform.position.y;
        intersectionConnection.rightPoint = new SerializedVector3(point.transform.parent.parent.GetChild(1).GetChild(0).GetComponent<MeshFilter>().sharedMesh.vertices[secondVertex] + point.transform.parent.parent.position);
        intersectionConnection.rightPoint.y = intersection.transform.position.y;
        intersectionConnection.lastPoint = new SerializedVector3(Misc.GetCenter(intersectionConnection.leftPoint.ToNormalVector3(), intersectionConnection.rightPoint.ToNormalVector3()));
        intersectionConnection.lastPoint.y = intersection.transform.position.y;
        intersectionConnection.YRotation = Quaternion.LookRotation((intersection.transform.position - point.transform.parent.GetChild(0).position).normalized).eulerAngles.y;
        intersectionConnection.length = Vector3.Distance(intersection.transform.position, point.transform.position);
        intersectionConnection.road = point.GetComponent<Point>();
        intersectionConnection.curvePoint = new SerializedVector3(intersection.transform.position);

        return intersectionConnection;
    }

    public bool IsLastSegmentCurved()
    {
        if (transform.GetChild(0).childCount > 0)
        {
            if (transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetChild(0).childCount == 1)
            {
                if (transform.GetChild(0).childCount > 1)
                {
                    return transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 2).GetComponent<RoadSegment>().curved;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetComponent<RoadSegment>().curved;
            }
        }
        else
        {
            return false;
        }
    }

    public void RemovePoints()
    {
        if (transform.GetChild(0).childCount > 0)
        {
            if (transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetComponent<RoadSegment>().curved == true)
            {
                if (transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetChild(0).childCount == 2)
                {
                    Undo.DestroyObjectImmediate(transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetChild(0).GetChild(1).gameObject);
                }
                else if (transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetChild(0).childCount == 1)
                {
                    Undo.DestroyObjectImmediate(transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).gameObject);

                    if (transform.GetChild(0).childCount > 0)
                    {
                        if (transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetComponent<RoadSegment>().curved == false)
                        {
                            Undo.DestroyObjectImmediate(transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetChild(0).GetChild(2).gameObject);
                            Undo.DestroyObjectImmediate(transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetChild(0).GetChild(1).gameObject);
                            transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetComponent<RoadSegment>().curved = true;
                            CreateMesh();
                        }
                        else
                        {
                            if (transform.GetChild(0).childCount > 0)
                            {
                                for (int i = transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetChild(1).childCount - 1; i >= 0; i -= 1)
                                {
                                    transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetChild(1).GetChild(i).GetComponent<MeshFilter>().sharedMesh = null;
                                    transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetChild(1).GetChild(i).GetComponent<MeshCollider>().sharedMesh = null;
                                }

                                Undo.DestroyObjectImmediate(transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetChild(0).GetChild(2).gameObject);
                            }
                        }
                    }
                }
                else
                {
                    Undo.DestroyObjectImmediate(transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetChild(0).GetChild(2).gameObject);

                    for (int i = transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetChild(1).childCount - 1; i >= 0; i -= 1)
                    {
                        transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetChild(1).GetChild(i).GetComponent<MeshFilter>().sharedMesh = null;
                        transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetChild(1).GetChild(i).GetComponent<MeshCollider>().sharedMesh = null;
                    }

                    CreateMesh();
                }
            }
            else
            {
                Undo.DestroyObjectImmediate(transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetChild(0).GetChild(2).gameObject);
                Undo.DestroyObjectImmediate(transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetChild(0).GetChild(1).gameObject);
                transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).GetComponent<RoadSegment>().curved = true;
                CreateMesh();
            }
        }
    }

    public void MovePoints(Vector3 hitPosition, Event guiEvent, RaycastHit raycastHit)
    {
        if (hitPosition == Misc.MaxVector3)
        {
            if (objectToMove != null)
            {
                DropMovingPoint();
            }
        }
        else
        {
            if (mouseDown == true && objectToMove != null)
            {
                if (guiEvent.keyCode == KeyCode.Plus || guiEvent.keyCode == KeyCode.KeypadPlus)
                {
                    Undo.RecordObject(objectToMove.transform, "Moved Point");
                    objectToMove.transform.position += new Vector3(0, 0.2f, 0);

                    if (guiEvent.control == true)
                    {
                        objectToMove.transform.position = new Vector3(objectToMove.transform.position.x, Mathf.Ceil(objectToMove.transform.position.y), objectToMove.transform.position.z);
                    }

                    if (extraObjectToMove != null)
                    {
                        Undo.RecordObject(extraObjectToMove.transform, "Moved Point");
                        extraObjectToMove.transform.position = objectToMove.transform.position;

                        if (guiEvent.control == true)
                        {
                            extraObjectToMove.transform.position = new Vector3(extraObjectToMove.transform.position.x, Mathf.Ceil(extraObjectToMove.transform.position.y), extraObjectToMove.transform.position.z);
                        }
                    }
                }
                else if (guiEvent.keyCode == KeyCode.Minus || guiEvent.keyCode == KeyCode.KeypadMinus)
                {
                    Vector3 position = objectToMove.transform.position - new Vector3(0, 0.2f, 0);

                    if (guiEvent.control == true)
                    {
                        position = new Vector3(position.x, Mathf.Floor(position.y), position.z);
                    }

                    if (position.y < raycastHit.point.y)
                    {
                        position.y = raycastHit.point.y;
                    }

                    Undo.RecordObject(objectToMove.transform, "Moved Point");
                    objectToMove.transform.position = position;

                    if (extraObjectToMove != null)
                    {
                        Undo.RecordObject(extraObjectToMove.transform, "Moved Point");
                        extraObjectToMove.transform.position = position;
                    }
                }
            }

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0)
            {
                mouseDown = true;
                if (objectToMove == null)
                {
                    if (raycastHit.transform.name.Contains("Point") && raycastHit.transform.GetComponent<Point>() != null && raycastHit.transform.parent.parent.parent.parent.gameObject == Selection.activeGameObject)
                    {
                        if (raycastHit.transform.GetComponent<BoxCollider>().enabled == false)
                        {
                            return;
                        }

                        if (raycastHit.collider.gameObject.name == "Control Point")
                        {
                            objectToMove = raycastHit.collider.gameObject;
                            objectToMove.GetComponent<BoxCollider>().enabled = false;
                        }
                        else if (raycastHit.collider.gameObject.name == "Start Point")
                        {
                            objectToMove = raycastHit.collider.gameObject;
                            objectToMove.GetComponent<BoxCollider>().enabled = false;

                            if (objectToMove.transform.parent.parent.GetSiblingIndex() > 0)
                            {
                                extraObjectToMove = raycastHit.collider.gameObject.transform.parent.parent.parent.GetChild(objectToMove.transform.parent.parent.GetSiblingIndex() - 1).GetChild(0).GetChild(2).gameObject;
                                extraObjectToMove.GetComponent<BoxCollider>().enabled = false;
                            }
                        }
                        else if (raycastHit.collider.gameObject.name == "End Point")
                        {
                            objectToMove = raycastHit.collider.gameObject;
                            objectToMove.GetComponent<BoxCollider>().enabled = false;

                            if (objectToMove.transform.parent.parent.GetSiblingIndex() < objectToMove.transform.parent.parent.parent.childCount - 1 && raycastHit.collider.gameObject.transform.parent.parent.parent.GetChild(objectToMove.transform.parent.parent.GetSiblingIndex() + 1).GetChild(0).childCount == 3)
                            {
                                extraObjectToMove = raycastHit.collider.gameObject.transform.parent.parent.parent.GetChild(objectToMove.transform.parent.parent.GetSiblingIndex() + 1).GetChild(0).GetChild(0).gameObject;
                                extraObjectToMove.GetComponent<BoxCollider>().enabled = false;
                            }
                        }

                    }
                }
            }
            else if (guiEvent.type == EventType.MouseDrag && objectToMove != null)
            {
                Undo.RecordObject(objectToMove.transform, "Moved Point");
                objectToMove.transform.position = hitPosition;

                if (extraObjectToMove != null)
                {
                    Undo.RecordObject(extraObjectToMove.transform, "Moved Point");
                    extraObjectToMove.transform.position = hitPosition;
                }
            }
            else if (guiEvent.type == EventType.MouseUp && guiEvent.button == 0 && objectToMove != null)
            {
                DropMovingPoint();
            }
        }
    }

    public void DropMovingPoint()
    {
        mouseDown = false;
        if (objectToMove.transform.parent.parent.GetComponent<RoadSegment>().curved == false)
        {
            if (objectToMove.transform.GetSiblingIndex() == 1)
            {
                objectToMove.transform.parent.parent.GetComponent<RoadSegment>().curved = true;
            }
            else
            {
                if (objectToMove.transform.parent.childCount == 3)
                {
                    objectToMove.transform.parent.GetChild(1).position = Misc.GetCenter(objectToMove.transform.parent.GetChild(0).position, objectToMove.transform.parent.GetChild(2).position);
                }
            }
        }

        if (extraObjectToMove != null)
        {
            if (extraObjectToMove.transform.parent.parent.GetComponent<RoadSegment>().curved == false)
            {
                if (extraObjectToMove.transform.parent.childCount == 3)
                {
                    extraObjectToMove.transform.parent.GetChild(1).position = Misc.GetCenter(extraObjectToMove.transform.parent.GetChild(0).position, extraObjectToMove.transform.parent.GetChild(2).position);
                }
            }

            extraObjectToMove.GetComponent<BoxCollider>().enabled = true;
            extraObjectToMove = null;
        }
        else
        {
            if ((objectToMove.transform.GetSiblingIndex() == 2 && objectToMove.transform.parent.parent.GetSiblingIndex() == objectToMove.transform.parent.parent.parent.childCount - 1) || (objectToMove.transform.GetSiblingIndex() == 0 && objectToMove.transform.parent.parent.GetSiblingIndex() == 0))
            {
                CheckForIntersectionGeneration(objectToMove);
            }
        }

        if (startIntersection != null)
        {
            startIntersection.CreateMesh();
        }

        if (endIntersection != null)
        {
            endIntersection.CreateMesh();
        }

        objectToMove.GetComponent<BoxCollider>().enabled = true;
        objectToMove = null;

        CreateMesh();
        RoadCreatorSettings.UpdateRoadGuidelines();
    }

    public Vector3[] CalculatePoints(Transform segment)
    {
        if (settings == null)
        {
            settings = RoadCreatorSettings.GetSerializedSettings();
        }

        float distance = Misc.CalculateDistance(segment.GetChild(0).GetChild(0).position, segment.GetChild(0).GetChild(1).position, segment.GetChild(0).GetChild(2).position);
        float divisions = settings.FindProperty("resolution").floatValue * 4 * distance;
        divisions = Mathf.Max(2, divisions);
        List<Vector3> points = new List<Vector3>();
        float distancePerDivision = 1 / divisions;
        float globalDistancePerDivision = distancePerDivision * distance;
        Vector3 lastPosition = segment.transform.GetChild(0).GetChild(0).position;
        points.Add(RaycastedPosition(lastPosition, segment.GetComponent<RoadSegment>()));

        for (float t = 0; t < 1; t += distancePerDivision / 10)
        {
            Vector3 position = Misc.Lerp3CenterHeight(segment.GetChild(0).GetChild(0).position, segment.GetChild(0).GetChild(1).position, segment.GetChild(0).GetChild(2).position, t);

            float calculatedDistance = Vector3.Distance(position, lastPosition);
            if (t + distancePerDivision / 10 >= 1)
            {
                points[points.Count - 1] = RaycastedPosition(segment.GetChild(0).GetChild(2).position, segment.GetComponent<RoadSegment>());
            }
            else if (calculatedDistance > globalDistancePerDivision)
            {
                lastPosition = position;
                points.Add(RaycastedPosition(position, segment.GetComponent<RoadSegment>()));
            }
        }

        return points.ToArray();
    }

    public Vector3 RaycastedPosition(Vector3 originalPosition, RoadSegment segment)
    {
        if (segment.terrainOption == RoadSegment.TerrainOption.adapt)
        {
            RaycastHit raycastHit;
            if (Physics.Raycast(originalPosition + new Vector3(0, 10, 0), Vector3.down, out raycastHit, 100f, ~((1 << settings.FindProperty("ignoreMouseRayLayer").intValue) | (1 << settings.FindProperty("roadLayer").intValue))))
            {
                return raycastHit.point;
            }
        }

        return originalPosition;
    }

}
