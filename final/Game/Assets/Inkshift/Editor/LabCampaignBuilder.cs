using System;
using System.Collections.Generic;
using System.Linq;
using Inkshift;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Rebuilds the authored campaign while retaining each scene's player, camera and flow objects.
public static class LabCampaignBuilder
{
    const string Art = "Assets/Inkshift/Art/Lab";
    const string SceneDir = "Assets/Inkshift/Scenes/";
    const float Pit = -11.8f;
    static readonly string[] Scenes = { "FirstMarks", "Slipstream", "QuarterTurn", "Cloudline" };
    static readonly Dictionary<string, Material> materials = new();
    static readonly Dictionary<string, Mesh> deckMeshes = new();
    static Transform course, markers, guide;
    static GameFlow flow;
    static int guideIndex;
    static Mesh arrow, puddle;
    static int platformCount, checkpointCount;
    static readonly List<Vector3> route = new();

    [MenuItem("Inkshift/Rebuild laboratory campaign")]
    public static void RebuildCampaign()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Stop Play mode and save the current scene before rebuilding.");

        MakeMaterials();
        arrow = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Inkshift/Art/Route arrow.asset");
        puddle = MakePuddleMesh();
        for (int i = 0; i < Scenes.Length; i++) BuildStage(i);
        BuildTitle();
        UpdateCampaign();
        AssetDatabase.SaveAssets();
        Debug.Log("Laboratory campaign rebuilt and saved. Use Inkshift/Validate laboratory routes for checks.");
    }

    static void BuildStage(int index)
    {
        var scene = EditorSceneManager.OpenScene(SceneDir + Scenes[index] + ".unity");
        flow = Object.FindFirstObjectByType<GameFlow>();
        var player = Object.FindFirstObjectByType<ShapeMotor>();
        var camera = Object.FindFirstObjectByType<QuarterCamera>();
        var ink = Object.FindFirstObjectByType<PaintTrail>();
        if (!flow || !player || !camera || !ink)
            throw new InvalidOperationException(scene.name + " is missing its shared gameplay objects.");
        ConfigureLook(player, camera, ink, index);
        var keep = new HashSet<GameObject> { flow.transform.root.gameObject, player.transform.root.gameObject,
            camera.transform.root.gameObject, ink.transform.root.gameObject };
        foreach (var root in scene.GetRootGameObjects())
            if (!keep.Contains(root)) Object.DestroyImmediate(root);

        var lab = Group("Laboratory", null, Vector3.zero);
        course = Group("Course", null, Vector3.zero);
        markers = Group("Route Markers", null, Vector3.zero);
        guide = Group("Route Guide (ordered test anchors)", null, Vector3.zero);
        guideIndex = platformCount = checkpointCount = 0;
        route.Clear();
        float far = index == 3 ? 88f : 75f;
        float centreX = new[] { 5f, 2f, -1.5f, 1f }[index];
        float halfWidth = new[] { 15f, 18f, 22f, 19.5f }[index];
        BuildRoom(lab, camera.transform, centreX, halfWidth, -10f, far, index);
        var start = Group("Start", markers, index == 1 ? V(-9, 0, 0) : V(0, 0, 0));
        var properties = new SerializedObject(flow);
        properties.FindProperty("startPoint").objectReferenceValue = start;
        properties.FindProperty("startAsBall").boolValue = false;
        properties.FindProperty("deathHeight").floatValue = -9f;
        properties.ApplyModifiedPropertiesWithoutUndo();
        BindShutterAudio(flow.GetComponent<FeedbackAudio>());
        switch (index)
        {
            case 0: FirstMarks(); break;
            case 1: Slipstream(); break;
            case 2: QuarterTurn(); break;
            case 3: TransferWing(); break;
        }
        Physics.SyncTransforms();
        AddJumpMarkers();
        int missed = CheckAnchors(false);
        float distance = 0;
        for (int i = 1; i < route.Count; i++) distance += Vector3.Distance(route[i - 1], route[i]);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"{scene.name}: {platformCount} traversable surfaces, {checkpointCount} checkpoints, " +
                  $"{distance:F0} m between ordered anchors, {missed} anchors without a supporting surface.");
        if (missed != 0) throw new InvalidOperationException(scene.name + " has unsupported route anchors.");
    }

    static void ConfigureLook(ShapeMotor player, QuarterCamera camera, PaintTrail ink, int index)
    {
        var motor = new SerializedObject(player);
        var cube = (Transform)motor.FindProperty("cube").objectReferenceValue;
        var sphere = (Transform)motor.FindProperty("sphere").objectReferenceValue;
        var amber = Existing("Lab gel amber"); var aqua = Existing("Lab gel aqua");
        var wet = Existing("Lab wet paint");
        if (!cube || !sphere || !amber || !aqua || !wet) throw new InvalidOperationException("Missing laboratory player or paint material.");
        foreach (var renderer in cube.GetComponentsInChildren<MeshRenderer>(true))
        {
            renderer.sharedMaterial = amber;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }
        foreach (var renderer in sphere.GetComponentsInChildren<MeshRenderer>(true))
        {
            renderer.sharedMaterial = aqua;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }
        var paint = new SerializedObject(ink); paint.FindProperty("inkMaterial").objectReferenceValue = wet;
        paint.ApplyModifiedPropertiesWithoutUndo();
        var framing = new SerializedObject(camera);
        framing.FindProperty("distance").floatValue = index == 1 || index == 3 ? 7.5f : 6.9f;
        framing.FindProperty("height").floatValue = index == 1 || index == 3 ? 6.4f : 5.9f;
        framing.FindProperty("lookAhead").floatValue = index == 1 || index == 3 ? 2.7f : 2.35f;
        framing.FindProperty("fieldOfView").floatValue = 61.7f;
        framing.ApplyModifiedPropertiesWithoutUndo();
    }

    static void FirstMarks()
    {
        Deck("Arrival", 0, 0, 0, 6.4f, 8);
        Barrier("Arrival paint wall", -3.05f, 0, 0, .25f, 2.5f, 6);
        Way(0, 0, 0, 0, false, 0, "roll forward");
        Deck("Measuring beam", 0, 0, 8, 1.65f, 8); Way(0, 0, 10, 0, false, 0, "one-metre cube steps");
        Deck("Measuring station", 0, 0, 15, 6, 6); Checkpoint(0, 0, 15, 0, 1, "Line up the two jumps ahead.");
        Arrow(0, 0, 16.6f, 0, false); Way(0, 0, 16, 0, false, 0, "align for stepping stones");
        Deck("Stone A", 0, 0, 21, 3.2f, 3); Way(0, 0, 21, 0, false, 1, "jump");
        Deck("Stone B", 2, 0, 25.4f, 3.2f, 3); Way(2, 0, 25.4f, 0, false, 2, "jump and steer right");
        Deck("Brake station", 3, 0, 30, 6, 6); Checkpoint(3, 0, 30, 1, 2, "Turn right for the narrow bridge.");
        Arrow(4.5f, 0, 30, 1, false); Way(4, 0, 30, 1, false, 0, "turn east");
        Deck("East beam", 8.5f, 0, 30, 5, 1.8f); Way(8.5f, 0, 30, 1, false, 0, "cross beam");
        Deck("East turn", 14, 0, 30, 6, 6); Arrow(14, 0, 31.5f, 0, false); Way(14, 0, 31, 0, false, 0, "turn north");
        Deck("North stone A", 14, 0, 35.8f, 3.4f, 3); Way(14, 0, 35.8f, 0, false, 1, "jump");
        Deck("North stone B", 14, 0, 40.5f, 3.4f, 3.2f); Way(14, 0, 40.5f, 0, false, 1, "jump");
        Deck("West turn", 14, 0, 45.5f, 6, 7); Arrow(12.4f, 0, 45.5f, 3, false); Way(13, 0, 45.5f, 3, false, 0, "turn west");
        Deck("West beam", 8.5f, 0, 45.5f, 5, 1.8f); Way(8.5f, 0, 45.5f, 3, false, 0, "brake on beam");
        Deck("Final rest", 3, 0, 45.5f, 6, 6); Checkpoint(3, 0, 45.5f, 0, 3, "The exit is past two more jumps.");
        Arrow(3, 0, 47, 0, false); Way(3, 0, 47, 0, false, 0, "face final stones");
        Deck("Final stone A", 3, 0, 52, 3.4f, 3); Way(3, 0, 52, 0, false, 2, "double jump if needed");
        Deck("Final stone B", 3, 0, 56.6f, 3.4f, 3); Way(3, 0, 56.6f, 0, false, 1, "jump");
        Deck("Exit corridor", 3, 0, 63, 5.2f, 10); Arrow(3, 0, 60.5f, 0, false);
        Way(3, 0, 61, 0, false, 1, "enter corridor"); Exit(3, 0, 65.5f, 0);
    }

    static void Slipstream()
    {
        Deck("Acceleration bay", -9, 0, 0, 6, 10);
        Barrier("Acceleration bay paint wall", -11.9f, 0, 0, .25f, 2.5f, 7);
        Way(-9, 0, 0, 0, true, 0, "switch to ball");
        Deck("Runway", -9, 0, 10, 5, 10); Checkpoint(-9, 0, 9, 0, 1, "Switch to the ball and build speed through the dip.");
        Arrow(-9, 0, 12, 0, false); Way(-9, 0, 13, 0, true, 0, "accelerate downhill");
        Ramp("Downhill", V(-9, 0, 15), V(-9, -2, 23), 5);
        Deck("Valley", -9, -2, 24.5f, 5, 3); Way(-9, -2, 24.5f, 0, true, 0, "hold speed in valley");
        Ramp("First launch", V(-9, -2, 26), V(-9, 1.5f, 34), 5);
        Deck("Launch landing", -9, 0, 39, 6, 7); Way(-9, 0, 39, 0, true, 0, "ride the ramp to the landing");
        Deck("Brake platform", -9, 0, 45, 6, 6); Checkpoint(-9, 0, 45, 1, 2, "Switch to cube to stop, then turn right.");
        Arrow(-7.5f, 0, 45, 1, false); Way(-8, 0, 45, 1, false, 0, "brake and turn east");
        Deck("Brake beam", -3, 0, 45, 6, 1.8f); Way(-3, 0, 45, 1, false, 0, "cross beam");
        Deck("Brake stone A", 2.8f, 0, 45, 4, 4); Way(2.8f, 0, 45, 1, false, 1, "small jump");
        Deck("Brake stone B", 8, 0, 45, 4, 4); Way(8, 0, 45, 1, false, 1, "small jump");
        Deck("Second launch rest", 13, 0, 45, 6, 6); Checkpoint(13, 0, 45, 0, 3, "Face forward, switch to ball, and use the ramp.");
        Arrow(13, 0, 46.5f, 0, false); Way(13, 0, 46, 0, true, 0, "turn north and accelerate");
        Deck("Second runway", 13, 0, 52, 4.8f, 8); Way(13, 0, 53, 0, true, 0, "keep speed");
        Ramp("Second launch", V(13, 0, 56), V(13, 1.8f, 63), 4.8f);
        Deck("Exit corridor", 13, 0, 69, 5.2f, 10); Way(13, 0, 68, 0, true, 0, "land and enter corridor");
        Exit(13, 0, 71.5f, 0, true);
    }

    static void QuarterTurn()
    {
        Deck("Compass start", 0, 0, 0, 6, 8);
        Barrier("Compass paint wall", -2.9f, 0, 0, .25f, 2.5f, 6);
        Way(0, 0, 0, 0, false, 0, "head north");
        Deck("North beam", 0, 0, 8, 1.8f, 8); Way(0, 0, 10, 0, false, 0, "cross north");
        Deck("East junction", 0, 0, 15, 6, 6); Arrow(1.5f, 0, 15, 1, false); Way(1, 0, 15, 1, false, 0, "turn east");
        Deck("East beam", 5, 0, 15, 4, 1.8f); Way(5, 0, 15, 1, false, 0, "cross east beam");
        Deck("East stone", 9, 0, 15, 3, 3);
        Way(9, 0, 15, 1, false, 1, "step east");
        Deck("North junction", 14, 0, 15, 7, 7); Checkpoint(14, 0, 15, 0, 1, "Turn north and cross the offset stones.");
        Arrow(14, 0, 16.7f, 0, false); Way(14, 0, 17, 0, false, 0, "turn north");
        Deck("North stone A", 14, 0, 21.3f, 3.2f, 3.2f); Way(14, 0, 21.3f, 0, false, 1, "jump");
        Deck("North stone B", 14, 0, 26.3f, 3.2f, 3.2f); Way(14, 0, 26.3f, 0, false, 1, "jump");
        Deck("West junction", 14, 0, 32, 7, 7); Arrow(12.3f, 0, 32, 3, false); Way(13, 0, 32, 3, false, 0, "turn west");
        Deck("West beam", 8.5f, 0, 32, 4, 1.8f); Way(8.5f, 0, 32, 3, false, 0, "cross west beam");
        foreach (float x in new[] { 4f, -1f, -6f }) { Deck("West stone", x, 0, 32, 3.2f, 3.2f); Way(x, 0, 32, 3, false, 1, "jump west"); }
        Deck("South junction", -12, 0, 32, 8, 7); Checkpoint(-12, 0, 32, 2, 2, "Turn around: the route continues south.");
        Arrow(-12, 0, 30.5f, 2, false); Way(-12, 0, 30.5f, 2, false, 0, "turn south");
        Deck("South beam", -12, 0, 25.8f, 1.8f, 5.5f); Way(-12, 0, 25.8f, 2, false, 0, "cross south beam");
        Deck("South stone", -12, 0, 20, 3.2f, 3.2f); Way(-12, 0, 20, 2, false, 1, "jump south");
        Arrow(-12.5f, 0, 20, 3, false); Way(-12.5f, 0, 20, 3, false, 0, "turn west");
        Deck("Return junction", -18, 0, 20, 6, 6); Checkpoint(-18, 0, 20, 0, 3, "Face north for the outer return.");
        Way(-18, 0, 20, 3, false, 1, "jump west");
        Arrow(-18, 0, 21, 0, false); Way(-18, 0, 21, 0, false, 0, "turn north");
        Deck("Return beam", -18, 0, 27.5f, 1.8f, 9); Way(-18, 0, 29, 0, false, 0, "cross return beam");
        Deck("Return stone A", -18, 0, 34.5f, 3.2f, 3); Way(-18, 0, 34.5f, 0, false, 1, "jump");
        Deck("Return stone B", -18, 0, 39.3f, 3.2f, 3); Way(-18, 0, 39.3f, 0, false, 1, "jump");
        Deck("Final junction", -18, 0, 45, 6, 7); Arrow(-16.5f, 0, 45, 1, false); Way(-17, 0, 45, 1, false, 0, "turn east");
        Deck("Final east beam", -13, 0, 45, 4, 1.8f); Way(-13, 0, 45, 1, false, 0, "cross east");
        Deck("Final stone A", -9, 0, 45, 3.2f, 3.2f); Way(-9, 0, 45, 1, false, 1, "jump");
        Deck("Final stone B", -4, 0, 45, 3.2f, 3.2f); Way(-4, 0, 45, 1, false, 1, "jump");
        Deck("Exit approach", 0, 0, 45, 5, 6); Arrow(0, 0, 46.5f, 0, false); Way(0, 0, 46, 0, false, 0, "turn north");
        Deck("Exit stone A", 0, 0, 51.2f, 3.4f, 3); Way(0, 0, 51.2f, 0, false, 1, "jump");
        Deck("Exit stone B", 0, 0, 56, 3.4f, 3); Way(0, 0, 56, 0, false, 1, "jump");
        Deck("Exit corridor", 0, 0, 63, 5.2f, 10); Way(0, 0, 61, 0, false, 1, "enter corridor"); Exit(0, 0, 65.5f, 0);
    }

    static void TransferWing()
    {
        Deck("Transfer arrival", 0, 0, 0, 6, 8);
        Barrier("Transfer paint wall", -2.9f, 0, 0, .25f, 2.5f, 6);
        Way(0, 0, 0, 0, false, 0, "head north");
        Deck("Precision beam", 0, 0, 8, 1.65f, 8); Way(0, 0, 10, 0, false, 0, "cross beam");
        Deck("Transfer stone A", 0, 0, 15.5f, 3.2f, 3); Way(0, 0, 15.5f, 0, false, 2, "double jump");
        Deck("Transfer stone B", 1.8f, 0, 20.2f, 3.2f, 3.2f); Way(1.8f, 0, 20.2f, 0, false, 1, "jump and steer");
        Deck("Transfer rest", 2, 0, 25.5f, 6, 7); Checkpoint(2, 0, 25.5f, 1, 1, "Turn right, then prepare to build ball speed.");
        Arrow(3.5f, 0, 25.5f, 1, false); Way(3, 0, 25.5f, 1, false, 0, "turn east");
        Deck("Transfer beam", 8, 0, 25.5f, 6, 1.8f); Way(8, 0, 25.5f, 1, false, 0, "cross beam");
        Deck("Runway turn", 14, 0, 25.5f, 6, 7); Arrow(14, 0, 27.3f, 0, false); Way(14, 0, 27, 0, true, 0, "turn north, switch to ball");
        Deck("Transfer runway", 14, 0, 34, 5, 10); Way(14, 0, 36, 0, true, 0, "accelerate");
        Ramp("Transfer launch", V(14, 0, 39), V(14, 2, 47), 5);
        Deck("Transfer landing", 14, 0, 51.5f, 6, 7); Way(14, 0, 51.5f, 0, true, 0, "land from ramp");
        Deck("Switchback rest", 14, 0, 56, 6, 5); Checkpoint(14, 0, 56, 3, 2, "Switch to cube to stop, then turn west.");
        Arrow(12.5f, 0, 56, 3, false); Way(13, 0, 56, 3, false, 0, "brake and turn west");
        foreach (float x in new[] { 9f, 4f, -1f, -6f }) { Deck("Switchback stone", x, 0, 56, 3.4f, 3.4f); Way(x, 0, 56, 3, false, 1, "jump west"); }
        Deck("Climb rest", -11.5f, 0, 56, 7, 7); Checkpoint(-11.5f, 0, 56, 0, 3, "The final platforms rise; use the second jump if needed.");
        Arrow(-11.5f, 0, 57.5f, 0, false); Way(-11.5f, 0, 57, 0, false, 0, "turn north");
        Deck("Climb A", -11.5f, .7f, 62, 3.4f, 3); Way(-11.5f, .7f, 62, 0, false, 1, "jump up");
        Deck("Climb B", -11.5f, 1.4f, 66.2f, 3.4f, 3); Way(-11.5f, 1.4f, 66.2f, 0, false, 1, "jump up");
        Deck("Climb C", -11.5f, 2.1f, 70.4f, 3.4f, 3); Way(-11.5f, 2.1f, 70.4f, 0, false, 1, "jump up");
        Deck("Exit corridor", -11.5f, 2.1f, 77.2f, 5.2f, 10); Way(-11.5f, 2.1f, 76, 0, false, 1, "enter corridor"); Exit(-11.5f, 2.1f, 79.5f, 0);
    }

    static void Deck(string name, float x, float top, float z, float width, float length)
    {
        platformCount++;
        var deck = Block(name, course, V(x, top - .24f, z), V(width, .48f, length), materials["Deck"], true);
        deck.GetComponent<MeshFilter>().sharedMesh = BeveledDeck(width, length);
        deck.GetComponent<MeshRenderer>().sharedMaterials = new[] { materials["Deck"], materials["Steel"] };
        // Restrained edge lights keep narrow surfaces legible against the dark service pit.
        if (width >= 2.5f && length >= 3f)
        {
            float near = Math.Min(length * .26f, 1.5f);
            foreach (int side in new[] { -1, 1 })
                foreach (int end in new[] { -1, 1 })
                    Block("Edge light", course, V(x + side * (width * .5f - .18f), top + .012f, z + end * near),
                        V(.07f, .012f, .55f), materials["Cyan"], false);
            if (platformCount % 3 != 0)
                Pool(course, V(x + width * .22f, top + .004f, z - length * .18f),
                    V(Math.Min(width * .21f, 1.1f), 1, Math.Min(length * .16f, 1.5f)), platformCount * 71);
        }
        if (width > 2.4f && length > 3.5f)
        {
            Block("Steel underframe", course, V(x, top - .57f, z), V(width * .91f, .18f, length * .93f), materials["Frame"], false);
            if (platformCount % 3 == 1)
                Module("PlatformTrestle", course, V(x, top - 1.15f, z), Quaternion.identity,
                    V(width * .82f / 2.8f, 1.04f, 1));
            if (platformCount % 2 == 0)
            {
                float pierHeight = top - .68f - Pit;
                if (pierHeight > 1)
                    Block("Support pier", course, V(x, Pit + pierHeight * .5f, z),
                        V(.35f, pierHeight, .42f), materials["Frame"], false);
            }
        }
    }

    static void Ramp(string name, Vector3 from, Vector3 to, float width)
    {
        platformCount++;
        Vector3 along = to - from;
        Quaternion rotation = Quaternion.LookRotation(along.normalized, Vector3.up);
        Vector3 up = rotation * Vector3.up;
        var ramp = Block(name, course, (from + to) * .5f - up * .24f,
            V(width, .48f, along.magnitude), materials["Deck"], true);
        ramp.transform.rotation = rotation;
        for (int i = 0; i < 3; i++)
        {
            var stripe = Block("Launch stripe", ramp.transform, Vector3.zero, Vector3.one, materials["Amber"], false);
            stripe.transform.localPosition = V(0, .5f + .014f / .48f, .5f - (.45f + i * .38f) / along.magnitude);
            stripe.transform.localScale = V(.72f, .012f / .48f, .12f / along.magnitude);
        }
    }

    static void Checkpoint(float x, float y, float z, int heading, int order, string message)
    {
        checkpointCount++;
        var ring = Cylinder("Checkpoint ring", markers, V(x, y + .022f, z), V(1.8f, .022f, 1.8f), materials["Amber"]);
        Cylinder("Checkpoint centre", markers, V(x, y + .05f, z), V(1.42f, .014f, 1.42f), materials["Rubber"]);
        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 local = Quaternion.Euler(0, heading * 90, 0) * V(side * 1.25f, 0, 0);
            Block("Checkpoint post", markers, V(x, y + .24f, z) + local, V(.14f, .48f, .14f), materials["Frame"], false);
            Block("Checkpoint lamp", markers, V(x, y + .51f, z) + local, V(.17f, .12f, .17f), materials["Amber"], false);
        }
        var zone = Group("Checkpoint " + order, markers, V(x, y + .55f, z));
        var box = zone.gameObject.AddComponent<BoxCollider>(); box.isTrigger = true; box.size = V(2.7f, 1.5f, 2.7f);
        var spawn = Group("Respawn (faces next section)", zone, V(0, -.55f, 0));
        spawn.rotation = Quaternion.Euler(0, heading * 90, 0);
        var stage = zone.gameObject.AddComponent<StageZone>();
        var s = new SerializedObject(stage);
        s.FindProperty("flow").objectReferenceValue = flow;
        s.FindProperty("kind").enumValueIndex = (int)ZoneKind.Checkpoint;
        s.FindProperty("checkpointOrder").intValue = order;
        s.FindProperty("spawn").objectReferenceValue = spawn;
        s.FindProperty("indicator").objectReferenceValue = ring.GetComponent<MeshRenderer>();
        s.FindProperty("activeMaterial").objectReferenceValue = materials["Checkpoint"];
        s.FindProperty("message").stringValue = message;
        s.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Exit(float x, float y, float z, int heading, bool arrivesBall = false)
    {
        var root = Group("Finish door", markers, V(x, y, z));
        root.rotation = Quaternion.Euler(0, heading * 90, 0);
        Block("Left jamb", root, V(-1.87f, 1.7f, 0), V(.45f, 3.4f, .55f), materials["Frame"], true);
        Block("Right jamb", root, V(1.87f, 1.7f, 0), V(.45f, 3.4f, .55f), materials["Frame"], true);
        Block("Overhead shutter housing", root, V(0, 5.12f, .03f), V(4.2f, 3.5f, .72f), materials["Frame"], false);
        Block("Door light", root, V(0, 3.53f, -.38f), V(1.6f, .18f, .08f), materials["Amber"], false);
        foreach (int side in new[] { -1, 1 })
            for (int j = 0; j < 4; j++)
                Block("Caution mark", root, V(side * 1.87f, .52f + j * .72f, -.29f),
                    V(.4f, .13f, .025f), materials["Amber"], false);
        var shutter = Block("Moving shutter", root, V(0, 1.65f, 0), V(3.26f, 3.3f, .18f), materials["Steel"], true);
        for (int i = 0; i < 9; i++)
        {
            var slat = Block("Shutter seam", shutter.transform, Vector3.zero, Vector3.one, materials["Frame"], false);
            slat.transform.localPosition = V(0, -.46f + i * .112f, -.52f);
            slat.transform.localScale = V(1, .012f, .12f);
        }
        var staging = Group("Staging point", root, V(0, 0, -1.55f));
        var inside = Group("Interior point", root, V(0, 0, 2.4f));
        var door = root.gameObject.AddComponent<LabExitDoor>(); door.Configure(shutter.transform, staging, inside, 3.5f);
        var trigger = Group("Finish trigger", root, V(0, .65f, -2.05f));
        var box = trigger.gameObject.AddComponent<BoxCollider>(); box.isTrigger = true; box.size = V(5.2f, 1.8f, .65f);
        var stage = trigger.gameObject.AddComponent<StageZone>();
        var s = new SerializedObject(stage);
        s.FindProperty("flow").objectReferenceValue = flow;
        s.FindProperty("kind").enumValueIndex = (int)ZoneKind.Finish;
        s.FindProperty("exitDoor").objectReferenceValue = door;
        s.ApplyModifiedPropertiesWithoutUndo();
        Vector3 approach = Quaternion.Euler(0, heading * 90, 0) * V(0, 0, -2.15f);
        Way(x + approach.x, y, z + approach.z, heading, arrivesBall, 0, "finish trigger");
    }

    static void Way(float x, float y, float z, int heading, bool ball, int jumps, string action)
    {
        var item = Group($"{++guideIndex:00} | {(ball ? "BALL" : "CUBE")} | H{heading} | J{jumps} | {action}",
            guide, V(x, y + .08f, z));
        item.rotation = Quaternion.Euler(0, heading * 90, 0);
        route.Add(V(x, y, z));
    }

    static void Arrow(float x, float y, float z, int heading, bool launch)
    {
        var obj = Group(launch ? "Launch arrow" : "Route arrow", markers, V(x, y + .018f, z));
        obj.rotation = Quaternion.Euler(0, heading * 90, 0);
        obj.localScale = V(.8f, 1, .8f);
        obj.gameObject.AddComponent<MeshFilter>().sharedMesh = arrow;
        var renderer = obj.gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = materials[launch ? "Amber" : "Cyan"];
        renderer.shadowCastingMode = ShadowCastingMode.Off;
    }

    static void BindShutterAudio(FeedbackAudio audio)
    {
        if (!audio) throw new InvalidOperationException("FeedbackAudio was not found on the game flow object.");
        var s = new SerializedObject(audio); var cues = s.FindProperty("cues");
        if (cues.arraySize < 9) cues.arraySize = 9;
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Inkshift/Art/Audio/Shutter.wav");
        if (!clip) throw new InvalidOperationException("The shutter audio clip has not been imported.");
        cues.GetArrayElementAtIndex(8).objectReferenceValue = clip;
        s.ApplyModifiedPropertiesWithoutUndo();
    }

    static void BuildRoom(Transform lab, Transform camera, float centreX, float halfWidth, float near, float far, int style)
    {
        float mid = (near + far) * .5f, length = far - near;
        float ceiling = 13.4f, height = ceiling - Pit, middleY = (ceiling + Pit) * .5f;
        var west = Wall("West wall", lab, V(centreX - halfWidth, middleY, mid), Vector3.right, length, height, materials["Side" + style]);
        var east = Wall("East wall", lab, V(centreX + halfWidth, middleY, mid), Vector3.left, length, height, materials["Side" + style]);
        var entry = Wall("Entry wall", lab, V(centreX, middleY, near), Vector3.forward, halfWidth * 2, height, materials["End" + style]);
        var back = Wall("Far wall", lab, V(centreX, middleY, far), Vector3.back, halfWidth * 2, height, materials["End" + style]);
        var roof = Wall("Ceiling", lab, V(centreX, ceiling, mid), Vector3.down, halfWidth * 2, length, materials["Frame"]);
        var ceilingRotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
        roof.rotation = ceilingRotation;
        var cutaway = lab.gameObject.AddComponent<LabWalls>();
        var s = new SerializedObject(cutaway);
        s.FindProperty("viewingCamera").objectReferenceValue = camera;
        var walls = s.FindProperty("walls"); walls.arraySize = 5;
        Transform[] sides = { west, east, entry, back, roof };
        for (int i = 0; i < sides.Length; i++) walls.GetArrayElementAtIndex(i).objectReferenceValue = sides[i];
        s.ApplyModifiedPropertiesWithoutUndo();

        var lights = Group("Practical lights", lab, Vector3.zero);
        foreach (var wall in new[] { west, east, entry, back })
        {
            float w = wall.localScale.x;
            int bay = 0;
            for (float at = -w * .5f + 2.6f; at <= w * .5f - 2.6f; at += 5.2f, bay++)
            {
                // The wall box supplies the collider and weathered backing; these are the authored modular fronts.
                foreach (float floor in new[] { -4f, 0f, 4f })
                    WallModule("WallBay", wall, V(at, floor - middleY, -.10f));
                if (bay % 2 == 0)
                    WallModule("UpperTruss", wall, V(at, 8.2f - middleY, .075f));
                if (bay % 4 == 1)
                    WallModule("PipeRack", wall, V(at, 6.8f - middleY, .22f));
            }
            WallBlock(wall, "Service rail", V(0, 8f - middleY, .20f), V(w, .18f, .2f), materials["Frame"]);
        }
        // Fixtures are spaced by section, with the associated lights remaining on during a cutaway.
        int lightIndex = 0;
        for (float z = near + 10; z < far - 3; z += 16, lightIndex++)
        {
            foreach (var wall in new[] { west, east })
            {
                Vector3 point = new Vector3(wall.position.x, 4.7f, z);
                Vector3 local = Quaternion.Inverse(wall.rotation) * (point - wall.position);
                WallModule("WallFixture", wall, V(local.x, local.y, .23f));
                Spot(lights, point + wall.forward * .5f, point + wall.forward * 13f + Vector3.down * 6f,
                    new Color(.68f, .86f, 1f), 24, 25, 90);
            }
            Spot(lights, V(centreX, 10.8f, z + 3), V(centreX, 0, z + 4),
                style == 1 ? new Color(.77f, .9f, 1f) : new Color(.88f, .93f, 1f), 24, 22, 82, lightIndex % 2 == 0);
        }
        Block("Service floor", lab, V(centreX, Pit - .15f, mid), V(halfWidth * 2, .3f, length), materials["Pit"], false);
        for (int i = 0; i < Math.Max(3, Mathf.FloorToInt(length / 8)); i++)
        {
            float z = near + 6 + i * 8.2f;
            for (int side = -1; side <= 1; side += 2)
            {
                float x = centreX + side * (halfWidth * (.49f + .11f * ((i + style) % 3)));
                Pool(lab, V(x, Pit + .004f, z), V(2.8f + i % 2, 1, 3.8f + (i % 3)), i * 57 + side * 11);
                if (i % 3 == 0) Module("DrainGrille", lab, V(x + side * 1.8f, Pit + .035f, z + 2), Quaternion.identity, Vector3.one);
            }
        }
        var sun = Group("Soft skylight", lab, Vector3.zero).gameObject.AddComponent<Light>();
        sun.type = LightType.Directional; sun.transform.rotation = Quaternion.Euler(50, -35, 0);
        sun.color = new Color(.69f, .82f, .92f); sun.intensity = .32f; sun.shadows = LightShadows.None;
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.19f, .24f, .27f);
        RenderSettings.fog = false;
        var reflection = Group("Room reflection", lab, V(centreX, 1, mid)).gameObject.AddComponent<ReflectionProbe>();
        reflection.mode = ReflectionProbeMode.Custom;
        reflection.customBakedTexture = AssetDatabase.LoadAssetAtPath<Cubemap>(Art + "/Room reflection.exr");
        reflection.size = V(halfWidth * 2 + 4, height, length + 4); reflection.boxProjection = true;
        var volume = Group("Lab grade", lab, Vector3.zero).gameObject.AddComponent<Volume>();
        volume.isGlobal = true; volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Art + "/Lab grade.asset");
    }

    static Transform Wall(string name, Transform parent, Vector3 position, Vector3 inward, float width, float height, Material material)
    {
        var wall = Block(name, parent, position, V(width, height, .34f), material, true).transform;
        wall.rotation = Quaternion.LookRotation(inward, inward == Vector3.down ? Vector3.forward : Vector3.up);
        return wall;
    }

    static void WallBlock(Transform wall, string name, Vector3 position, Vector3 size, Material material)
    {
        var obj = Block(name, wall, Vector3.zero, Vector3.one, material, false);
        Vector3 parentScale = wall.localScale;
        obj.transform.localPosition = Divide(position, parentScale);
        obj.transform.localScale = Divide(size, parentScale);
    }

    static void WallModule(string name, Transform wall, Vector3 position)
    {
        var obj = Module(name, wall, Vector3.zero, Quaternion.identity, Vector3.one);
        obj.transform.localPosition = Divide(position, wall.localScale);
        obj.transform.localScale = Divide(Vector3.one, wall.localScale);
        if (name == "WallBay")
        {
            int variant = Mathf.Abs(Mathf.RoundToInt(position.x / 5.2f) + Mathf.RoundToInt(position.y / 4f)) % 3;
            foreach (var renderer in obj.GetComponentsInChildren<MeshRenderer>())
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m => m && m.name == "LabWall" ? materials["Panel" + variant] : m).ToArray();
        }
    }

    static void Spot(Transform parent, Vector3 at, Vector3 target, Color color, float intensity, float range, float angle, bool shadows = false)
    {
        var light = Group("Cool wall wash", parent, at).gameObject.AddComponent<Light>();
        light.type = LightType.Spot; light.transform.rotation = Quaternion.LookRotation(target - at);
        light.color = color; light.intensity = intensity; light.range = range;
        light.spotAngle = angle; light.innerSpotAngle = angle * .62f;
        light.shadows = shadows ? LightShadows.Soft : LightShadows.None; light.shadowStrength = .8f;
        light.shadowBias = .08f; light.shadowNormalBias = .25f;
    }

    static void Barrier(string name, float x, float top, float z, float width, float height, float length)
    {
        Block(name, course, V(x, top + height * .5f, z), V(width, height, length), materials["Panel"], true);
    }

    static void MakeMaterials()
    {
        var deck = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/Textures/Weathered Deck.png");
        var panel = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/Textures/Weathered Panel.png");
        if (!deck || !panel) throw new InvalidOperationException("Weathered lab textures have not been imported.");
        materials.Clear();
        materials["Deck"] = Lit("Weathered deck", new Color(.96f, .98f, 1f), .84f, .08f, deck, Vector2.one, true);
        materials["Panel"] = Lit("Reachable lab panel", new Color(.43f, .56f, .63f), .7f, .28f, panel, Vector2.one, true);
        materials["Panel0"] = Lit("Patinated panel A", new Color(.43f, .55f, .62f), .72f, .26f, panel, Vector2.one, true);
        materials["Panel1"] = Lit("Patinated panel B", new Color(.39f, .51f, .58f), .63f, .28f, panel, Vector2.one, true);
        materials["Panel2"] = Lit("Patinated panel C", new Color(.46f, .56f, .61f), .78f, .3f, panel, Vector2.one, true);
        var colors = new[] { new Color(.40f, .52f, .61f), new Color(.36f, .52f, .57f),
            new Color(.43f, .52f, .58f), new Color(.42f, .48f, .56f) };
        for (int i = 0; i < 4; i++)
        {
            materials["Side" + i] = Lit("Weathered side " + i, colors[i], .72f, .3f, panel, V2(16, 6), true);
            materials["End" + i] = Lit("Weathered end " + i, colors[i], .72f, .3f, panel, V2(9, 6), true);
        }
        materials["Pit"] = Lit("Damp service floor", new Color(.22f, .31f, .35f), .9f, .2f, deck, V2(12, 22), true);
        var damp = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/Textures/Damp Patch.png");
        if (!damp) throw new InvalidOperationException("Damp patch texture has not been imported.");
        materials["Pool"] = Lit("Standing water", new Color(.55f, .68f, .72f, .72f), .96f, .18f, damp, Vector2.one, false);
        var pool = materials["Pool"];
        pool.SetFloat("_Surface", 1); pool.SetFloat("_Blend", 0);
        pool.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); pool.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        pool.SetFloat("_ZWrite", 0); pool.SetOverrideTag("RenderType", "Transparent");
        pool.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); pool.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        pool.renderQueue = 2970; pool.SetShaderPassEnabled("ShadowCaster", false); EditorUtility.SetDirty(pool);
        materials["Frame"] = Existing("LabFrame");
        materials["Steel"] = Existing("LabSteel");
        materials["Rubber"] = Existing("LabRubber");
        materials["Cyan"] = Existing("Route cyan");
        materials["Amber"] = AssetDatabase.LoadAssetAtPath<Material>("Assets/Inkshift/Art/Launch amber.mat");
        materials["Checkpoint"] = Existing("Checkpoint confirmed");
        foreach (var pair in materials) if (!pair.Value) throw new InvalidOperationException("Missing material " + pair.Key);
    }

    static Material Lit(string name, Color color, float smoothness, float metallic, Texture texture, Vector2 tiling, bool alphaSmoothness)
    {
        string path = Art + "/Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", smoothness); material.SetFloat("_Metallic", metallic);
        material.SetTexture("_BaseMap", texture); material.SetTextureScale("_BaseMap", tiling);
        material.SetFloat("_SmoothnessTextureChannel", alphaSmoothness ? 1 : 0);
        if (alphaSmoothness) material.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
        else material.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
        EditorUtility.SetDirty(material);
        return material;
    }

    static Material Existing(string name) => AssetDatabase.LoadAssetAtPath<Material>(Art + "/Materials/" + name + ".mat");

    static Mesh BeveledDeck(float width, float length)
    {
        string key = Mathf.RoundToInt(width * 100) + "x" + Mathf.RoundToInt(length * 100);
        if (deckMeshes.TryGetValue(key, out var cached)) return cached;
        string path = Art + "/Meshes/Deck " + key + ".asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh) { deckMeshes[key] = mesh; return mesh; }
        const int points = 16;
        const float radius = .05f, corner = .10f, thickness = .48f;
        float[] y = { thickness * .5f, thickness * .5f - .293f * radius, thickness * .5f - radius,
            -thickness * .5f + radius, -thickness * .5f + .293f * radius, -thickness * .5f };
        float[] inset = { radius, .293f * radius, 0, 0, .293f * radius, radius };
        float[] angles = { 0, Mathf.PI * .25f, Mathf.PI * .5f, Mathf.PI * .5f, Mathf.PI * .75f, Mathf.PI };
        var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uv = new List<Vector2>();
        var top = new List<int>(); var sides = new List<int>();
        float u = Mathf.Max(2, Mathf.RoundToInt(width / 2.65f)) * .5f;
        float v = Mathf.Max(2, Mathf.RoundToInt(length / 2.65f)) * .5f;
        for (int ring = 0; ring < y.Length; ring++)
        for (int c = 0; c < 4; c++)
        {
            float middle = (c + .5f) * Mathf.PI * .5f;
            float cx = Mathf.Sign(Mathf.Cos(middle)) * (width * .5f - corner);
            float cz = Mathf.Sign(Mathf.Sin(middle)) * (length * .5f - corner);
            for (int point = 0; point < 4; point++)
            {
                float a = (c + point / 3f) * Mathf.PI * .5f;
                float nx = Mathf.Cos(a), nz = Mathf.Sin(a);
                float x = cx + nx * (corner - inset[ring]), z = cz + nz * (corner - inset[ring]);
                vertices.Add(V(x / width, y[ring] / thickness, z / length));
                float outward = Mathf.Sin(angles[ring]);
                normals.Add(V(nx * outward * width, Mathf.Cos(angles[ring]) * thickness,
                    nz * outward * length).normalized);
                uv.Add(V2((x / width + .5f) * u, (z / length + .5f) * v));
            }
        }
        int topCenter = vertices.Count; vertices.Add(V(0, .5f, 0)); normals.Add(Vector3.up); uv.Add(V2(u * .5f, v * .5f));
        int bottomCenter = vertices.Count; vertices.Add(V(0, -.5f, 0)); normals.Add(Vector3.down); uv.Add(V2(u * .5f, v * .5f));
        int last = (y.Length - 1) * points;
        for (int i = 0; i < points; i++)
        {
            int next = (i + 1) % points;
            top.AddRange(new[] { topCenter, next, i });
            for (int ring = 0; ring < y.Length - 1; ring++)
            {
                int a = ring * points + i, b = ring * points + next;
                sides.AddRange(new[] { a, b, a + points, b, b + points, a + points });
            }
            sides.AddRange(new[] { bottomCenter, last + i, last + next });
        }
        mesh = new Mesh { name = "Beveled deck " + key };
        mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.subMeshCount = 2;
        mesh.SetTriangles(top, 0); mesh.SetTriangles(sides, 1); mesh.RecalculateTangents(); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path); deckMeshes[key] = mesh;
        return mesh;
    }

    static Mesh MakePuddleMesh()
    {
        string path = Art + "/Meshes/Standing water.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh) return mesh;
        const int count = 32;
        var vertices = new Vector3[count + 1]; var uvs = new Vector2[count + 1]; var indices = new int[count * 3];
        uvs[0] = V2(.5f, .5f);
        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2 / count;
            float radius = .44f + .045f * Mathf.Sin(angle * 3 + .3f) + .035f * Mathf.Sin(angle * 7 + .9f);
            vertices[i + 1] = V(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            uvs[i + 1] = V2(vertices[i + 1].x + .5f, vertices[i + 1].z + .5f);
            indices[i * 3] = 0; indices[i * 3 + 1] = (i + 1) % count + 1; indices[i * 3 + 2] = i + 1;
        }
        mesh = new Mesh { name = "Standing water", vertices = vertices, uv = uvs, triangles = indices };
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    static void Pool(Transform parent, Vector3 at, Vector3 scale, float angle)
    {
        var patch = Group("Damp patch", parent, at); patch.localRotation = Quaternion.Euler(0, angle, 0); patch.localScale = scale;
        patch.gameObject.AddComponent<MeshFilter>().sharedMesh = puddle;
        var renderer = patch.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial = materials["Pool"];
        renderer.shadowCastingMode = ShadowCastingMode.Off;
    }

    static GameObject Block(string name, Transform parent, Vector3 at, Vector3 scale, Material material, bool collider)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name;
        obj.transform.SetParent(parent, false); obj.transform.localPosition = at; obj.transform.localScale = scale;
        obj.GetComponent<MeshRenderer>().sharedMaterial = material;
        if (!collider) Object.DestroyImmediate(obj.GetComponent<BoxCollider>());
        return obj;
    }

    static GameObject Cylinder(string name, Transform parent, Vector3 at, Vector3 scale, Material material)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder); obj.name = name;
        obj.transform.SetParent(parent, false); obj.transform.localPosition = at; obj.transform.localScale = scale;
        obj.GetComponent<MeshRenderer>().sharedMaterial = material;
        Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj;
    }

    static GameObject Module(string name, Transform parent, Vector3 at, Quaternion rotation, Vector3 scale)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/Prefabs/" + name + ".prefab");
        if (!prefab) throw new InvalidOperationException("Missing lab prefab " + name);
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        obj.transform.localPosition = at; obj.transform.localRotation = rotation; obj.transform.localScale = scale;
        return obj;
    }

    static Transform Group(string name, Transform parent, Vector3 at)
    {
        var obj = new GameObject(name).transform; obj.SetParent(parent, false); obj.localPosition = at; return obj;
    }

    static Vector3 Divide(Vector3 value, Vector3 divisor) => V(value.x / divisor.x, value.y / divisor.y, value.z / divisor.z);
    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
    static Vector2 V2(float x, float y) => new Vector2(x, y);

    static void BuildTitle()
    {
        var scene = EditorSceneManager.OpenScene(SceneDir + "InkshiftTitle.unity");
        var exhibit = GameObject.Find("Paint lab exhibit");
        var camera = Camera.main;
        var gameFlow = Object.FindFirstObjectByType<GameFlow>();
        if (!exhibit || !camera || !gameFlow) throw new InvalidOperationException("Title scene is missing its exhibit, camera or flow.");
        var keep = new HashSet<GameObject> { exhibit.transform.root.gameObject, camera.transform.root.gameObject, gameFlow.transform.root.gameObject };
        foreach (var root in scene.GetRootGameObjects()) if (!keep.Contains(root)) Object.DestroyImmediate(root);
        var discard = new List<GameObject>();
        foreach (Transform child in exhibit.transform)
            if (child.name == "Far pier" || child.name == "Display plinth" || child.name == "Route arrow") discard.Add(child.gameObject);
        foreach (var item in discard) Object.DestroyImmediate(item);
        var titleCube = exhibit.transform.Find("Cube exhibit").GetComponentInChildren<MeshRenderer>(true);
        var titleBall = exhibit.transform.Find("Ball exhibit").GetComponentInChildren<MeshRenderer>(true);
        if (titleCube) titleCube.sharedMaterial = Existing("Lab gel amber");
        if (titleBall) titleBall.sharedMaterial = Existing("Lab gel aqua");
        course = exhibit.transform; platformCount = 0;
        Deck("Exhibit plinth", 3.3f, -.4f, .5f, 6, 8);
        var lab = Group("Laboratory backdrop", null, Vector3.zero);
        BuildRoom(lab, camera.transform, 0, 12, -11, 19, 0);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
    }

    static void UpdateCampaign()
    {
        var campaign = AssetDatabase.LoadAssetAtPath<Campaign>("Assets/Inkshift/Campaign.asset");
        var s = new SerializedObject(campaign); var stages = s.FindProperty("stages");
        string[] titles = { "First Marks", "Slipstream", "Quarter Turn", "Transfer Wing" };
        string[] focus = { "CUBE STEPS   /   DOUBLE JUMP", "BALL SPEED   /   RAMP FLIGHT", "FOUR HEADINGS   /   TURNING", "FULL ROUTE   /   RISING PLATFORMS" };
        string[] briefs = {
            "Flip one cube length at a time. Jump between stones, and use Q / E to turn at the junctions.",
            "Switch to ball and build speed through the dip. Switch back to cube to brake at the next station.",
            "Follow the arrows through all four headings. Q and E turn the camera; W follows its new direction.",
            "Cross the beams, take the ball ramp, then brake and climb. Choose the shape that suits each section."
        };
        for (int i = 0; i < titles.Length; i++)
        {
            var item = stages.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("Title").stringValue = titles[i];
            item.FindPropertyRelative("Focus").stringValue = focus[i];
            item.FindPropertyRelative("Brief").stringValue = briefs[i];
        }
        s.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(campaign);
    }

    [MenuItem("Inkshift/Validate laboratory routes")]
    public static void ValidateCurrentRoute()
    {
        guide = GameObject.Find("Route Guide (ordered test anchors)")?.transform;
        if (!guide) throw new InvalidOperationException("Open a laboratory route first.");
        Physics.SyncTransforms();
        int missed = CheckAnchors(true);
        Debug.Log($"{SceneManager.GetActiveScene().name}: {guide.childCount} ordered anchors; {missed} unsupported.");
    }

    static int CheckAnchors(bool log)
    {
        int missing = 0;
        foreach (Transform waypoint in guide)
        {
            if (!FloorAt(waypoint.position, out _))
            {
                missing++;
                Debug.LogWarning("No route support below " + waypoint.name + " at " + waypoint.position, waypoint);
            }
            else if (log) Debug.Log("Supported: " + waypoint.name, waypoint);
        }
        return missing;
    }

    static bool FloorAt(Vector3 point, out BoxCollider floor)
    {
        if (Physics.Raycast(point + Vector3.up * 2, Vector3.down, out var hit, 3f, 1, QueryTriggerInteraction.Ignore)
            && hit.collider is BoxCollider box && hit.normal.y > .65f)
        { floor = box; return true; }
        floor = null; return false;
    }

    // Marks suggested edge positions, so a test follows the route from edge to edge rather than jumping from pad centres.
    static void AddJumpMarkers()
    {
        for (int i = 1; i < guide.childCount; i++)
        {
            var destination = guide.GetChild(i); var previous = guide.GetChild(i - 1);
            if (!(destination.name.Contains(" | J1 | ") || destination.name.Contains(" | J2 | "))) continue;
            if (!FloorAt(previous.position, out var startFloor) || !FloorAt(destination.position, out var endFloor)) continue;
            if (startFloor == endFloor) continue;
            Vector3 direction = destination.position - previous.position; direction.y = 0;
            if (direction.sqrMagnitude < .01f) continue;
            direction.Normalize();
            Vector3 takeoff = EdgePoint(startFloor.bounds, previous.position, direction, .36f);
            Vector3 landing = EdgePoint(endFloor.bounds, destination.position, -direction, .36f);
            takeoff.y = previous.position.y; landing.y = destination.position.y;
            var takeoffMarker = Group("TAKEOFF (approach edge before jumping)", destination, Vector3.zero); takeoffMarker.position = takeoff;
            var landingMarker = Group("LANDING (aim inside edge)", destination, Vector3.zero); landingMarker.position = landing;
            float gap = Vector2.Distance(V2(takeoff.x, takeoff.z), V2(landing.x, landing.z));
            float rise = landing.y - takeoff.y;
            if (gap > 4.0f || rise > 1.3f)
                Debug.LogWarning($"Review {destination.name}: {gap:F2} m between inset markers, rise {rise:F2} m.", destination);
        }
    }

    static Vector3 EdgePoint(Bounds bounds, Vector3 origin, Vector3 direction, float inset)
    {
        float x = direction.x > .001f ? (bounds.max.x - origin.x) / direction.x :
            direction.x < -.001f ? (bounds.min.x - origin.x) / direction.x : float.PositiveInfinity;
        float z = direction.z > .001f ? (bounds.max.z - origin.z) / direction.z :
            direction.z < -.001f ? (bounds.min.z - origin.z) / direction.z : float.PositiveInfinity;
        return origin + direction * Mathf.Max(0, Mathf.Min(x, z) - inset);
    }
}
