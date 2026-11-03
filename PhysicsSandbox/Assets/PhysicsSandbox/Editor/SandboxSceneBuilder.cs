using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PhysicsSandbox.Editor
{
    /// <summary>
    /// Generates the sandbox scene from code so the layout is reproducible and reviewable:
    /// the template's XR rig and floor, plus a workbench with two block sets, a lever and a
    /// water tank, a side table with a ramp, the readout panel and the experiment panel.
    /// </summary>
    public static class SandboxSceneBuilder
    {
        const string k_TemplateScene = "Assets/Scenes/BasicScene.unity";
        const string k_ScenePath = "Assets/PhysicsSandbox/Scenes/Sandbox.unity";
        const string k_MaterialDir = "Assets/PhysicsSandbox/Materials";
        const string k_SimulatorPrefab = "Assets/Samples/XR Interaction Toolkit/3.4.1/XR Interaction Simulator/XR Interaction Simulator.prefab";

        const float k_TableTop = 0.90f;
        const float k_FrontRowZ = 0.50f;
        const float k_LeverSpring = 15f;
        const float k_LeverDamper = 1f;

        // Shallow enough that only ice overcomes static friction, and that a nudged aluminum or
        // steel block stops again, so the outcome is unambiguous.
        const float k_RampAngleDeg = 12.5f;
        const float k_RampLength = 0.75f;
        const float k_RampWidth = 0.24f;
        const float k_RampFriction = 0.15f;
        const float k_RampStartAlong = 0.12f;
        const float k_UnmarkedSide = 0.10f;

        [MenuItem("CS567/Build Sandbox Scene")]
        public static string Build()
        {
            if (EditorApplication.isPlaying)
                return "cannot build while playing";

            AssetDatabase.DeleteAsset(k_ScenePath);
            if (!AssetDatabase.CopyAsset(k_TemplateScene, k_ScenePath))
                return "failed to copy " + k_TemplateScene;

            var scene = EditorSceneManager.OpenScene(k_ScenePath, OpenSceneMode.Single);
            var root = new GameObject("Sandbox").transform;

            // The template scene has no baked environment lighting, so shaded faces would be black.
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.66f, 0.72f, 0.82f);
            RenderSettings.ambientEquatorColor = new Color(0.56f, 0.56f, 0.56f);
            RenderSettings.ambientGroundColor = new Color(0.30f, 0.28f, 0.26f);

            var lineMaterial = CreateMaterial("HandGhost", new Color(1f, 0.45f, 0.1f), 0f, 0f, unlit: true);
            var woodDark = CreateMaterial("Workbench", new Color(0.33f, 0.24f, 0.17f), 0f, 0.25f);
            var woodLight = CreateMaterial("Plank", new Color(0.85f, 0.70f, 0.48f), 0f, 0.3f);
            var darkMetal = CreateMaterial("Fulcrum", new Color(0.18f, 0.19f, 0.21f), 0.8f, 0.5f);
            var glass = CreateMaterial("Glass", new Color(0.80f, 0.92f, 0.95f, 0.16f), 0f, 0.95f, transparent: true);
            var water = CreateMaterial("Water", new Color(0.15f, 0.45f, 0.85f, 0.42f), 0f, 0.9f, transparent: true);
            var panel = CreateMaterial("Panel", new Color(0.07f, 0.09f, 0.12f), 0f, 0f, unlit: true);

            // Workbench
            var table = CreateBox("Workbench", root, new Vector3(0f, k_TableTop * 0.5f, 0.70f), new Vector3(2.3f, k_TableTop, 0.70f), woodDark, true);
            table.GetComponent<Collider>().sharedMaterial = CreatePhysicsMaterial("Workbench", 0.6f);

            // Equal-mass set: same weight, so size alone shows density.
            var equalMass = new GameObject("Equal Mass Set").transform;
            equalMass.SetParent(root, false);
            float[] equalMassX = { -0.97f, -0.80f, -0.67f, -0.57f };
            for (var i = 0; i < MaterialLibrary.All.Length; i++)
                CreateBlock(equalMass, MaterialLibrary.All[i], 1f, new Vector3(equalMassX[i], k_TableTop, k_FrontRowZ), lineMaterial);
            var equalMassSign = CreateSign("Equal Mass Sign", root, "SAME MASS  ·  1 kg each", new Vector3(-0.78f, k_TableTop + 0.001f, 0.385f));

            // Equal-volume set: same size, so the weight cue alone tells them apart.
            var equalVolume = new GameObject("Equal Volume Set").transform;
            equalVolume.SetParent(root, false);
            const float litre = 0.001f;
            for (var i = 0; i < MaterialLibrary.All.Length; i++)
            {
                var material = MaterialLibrary.All[i];
                CreateBlock(equalVolume, material, material.densityKgPerM3 * litre, new Vector3(-0.32f + 0.15f * i, k_TableTop, k_FrontRowZ), lineMaterial);
            }
            var equalVolumeSign = CreateSign("Equal Volume Sign", root, "SAME SIZE  ·  1 litre each", new Vector3(-0.095f, k_TableTop + 0.001f, 0.385f));

            var lever = CreateLever(root, new Vector3(-0.38f, k_TableTop, 0.74f), woodLight, darkMetal);
            var waterVolume = CreateTank(root, new Vector3(0.74f, k_TableTop, 0.70f), glass, water);
            CreateSign("Tank Sign", root, "WATER TANK  ·  1000 kg/m³", new Vector3(0.74f, k_TableTop + 0.001f, 0.385f));

            var sideTable = CreateBox("Side Table", root, new Vector3(1.75f, k_TableTop * 0.5f, 0.70f), new Vector3(1.10f, k_TableTop, 0.70f), woodDark, true);
            sideTable.GetComponent<Collider>().sharedMaterial = table.GetComponent<Collider>().sharedMaterial;
            var green = CreateMaterial("StartLine", new Color(0.25f, 0.80f, 0.40f), 0f, 0f, unlit: true);
            var ramp = CreateRamp(root, new Vector3(1.75f, k_TableTop, 0.66f), woodLight, darkMetal, green);

            CreateHud(root, new Vector3(0.45f, 1.60f, 1.35f), panel, lever, waterVolume, ramp);

            var unmarkedMaterial = CreateMaterial("Unmarked", new Color(0.52f, 0.22f, 0.78f), 0f, 0.3f);
            var unmarkedSet = new GameObject("Unmarked Study Blocks").transform;
            unmarkedSet.SetParent(root, false);
            var unmarkedLeft = CreateUnmarkedBlock(unmarkedSet, "Unmarked Left", new Vector3(-0.28f, k_TableTop, 0.50f), unmarkedMaterial, lineMaterial);
            var unmarkedRight = CreateUnmarkedBlock(unmarkedSet, "Unmarked Right", new Vector3(0.08f, k_TableTop, 0.50f), unmarkedMaterial, lineMaterial);

            var trainingPad = CreateBox("Training Pad", root, new Vector3(0.08f, k_TableTop + 0.001f, 0.50f), new Vector3(0.16f, 0.002f, 0.16f), green, false);

            var session = new GameObject("Experiment Session").AddComponent<ExperimentSession>();
            session.transform.SetParent(root, false);
            session.Panel = CreateExperimentPanel(root, new Vector3(-0.70f, 1.47f, 1.30f), panel);
            session.Lever = lever;
            session.Water = waterVolume;
            session.Ramp = ramp;
            session.UnmarkedLeft = unmarkedLeft;
            session.UnmarkedRight = unmarkedRight;
            session.TrainingPad = trainingPad;
            session.HideDuringStudy = new[] { equalMassSign.gameObject, equalVolumeSign.gameObject };
            unmarkedLeft.gameObject.SetActive(false);
            unmarkedRight.gameObject.SetActive(false);

            var director = new GameObject("Demo Director").AddComponent<DemoDirector>();
            director.transform.SetParent(root, false);
            director.Lever = lever;
            director.Water = waterVolume;

            var loader = new GameObject("Simulator Auto Loader").AddComponent<SimulatorAutoLoader>();
            loader.transform.SetParent(root, false);
            loader.SimulatorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_SimulatorPrefab);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(k_ScenePath, true) };
            AssetDatabase.SaveAssets();

            return $"built {k_ScenePath}: {equalMass.childCount + equalVolume.childCount + unmarkedSet.childCount} blocks, " +
                $"simulator prefab {(loader.SimulatorPrefab != null ? "found" : "MISSING")}";
        }

        static PhysicsBlock CreateBlock(Transform parent, MaterialDef def, float massKg, Vector3 tablePosition, Material lineMaterial)
        {
            var side = PhysicsFormulas.CubeSideM(massKg, def.densityKgPerM3);
            var root = new GameObject($"{def.name} {massKg:0.##} kg");
            root.transform.SetParent(parent, false);
            root.transform.position = tablePosition + Vector3.up * (side * 0.5f + 0.002f);

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * side;
            visual.GetComponent<Renderer>().sharedMaterial = CreateMaterial(def.name, def.color, def.metallic, def.smoothness);

            var box = root.AddComponent<BoxCollider>();
            box.size = Vector3.one * side;
            box.sharedMaterial = CreatePhysicsMaterial(def.name, def.friction);

            var body = root.AddComponent<Rigidbody>();
            body.mass = massKg;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            var block = root.AddComponent<PhysicsBlock>();
            block.Configure(def, massKg);

            // Kinematic while held, handed back to the physics engine on release.
            var grab = root.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.Kinematic;
            grab.useDynamicAttach = true;
            grab.throwOnDetach = false;
            grab.smoothPosition = false;
            grab.smoothRotation = false;

            root.AddComponent<PseudoHapticGrabTransformer>();
            root.AddComponent<PseudoHapticGhost>().LineMaterial = lineMaterial;

            var label = CreateText("Label", root.transform, $"{def.name}\n{massKg:0.00} kg", 0.28f, new Vector2(0.4f, 0.1f), TextAlignmentOptions.Bottom);
            label.rectTransform.pivot = new Vector2(0.5f, 0f);
            label.gameObject.AddComponent<FloatingLabel>().HeightAboveParent = side * 0.5f + 0.015f;
            label.transform.position = root.transform.position + Vector3.up * (side * 0.5f + 0.015f);

            return block;
        }

        // Identical purple cubes for the heaviness trials: nothing but the cue separates them,
        // and the color matches no lesson material, so it claims no density.
        static PhysicsBlock CreateUnmarkedBlock(Transform parent, string name, Vector3 tablePosition, Material material, Material lineMaterial)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = tablePosition + Vector3.up * (k_UnmarkedSide * 0.5f + 0.002f);

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * k_UnmarkedSide;
            visual.GetComponent<Renderer>().sharedMaterial = material;

            var box = root.AddComponent<BoxCollider>();
            box.size = Vector3.one * k_UnmarkedSide;
            box.sharedMaterial = CreatePhysicsMaterial("Unmarked", 0.5f);

            var body = root.AddComponent<Rigidbody>();
            body.mass = 1f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            var block = root.AddComponent<PhysicsBlock>();
            block.Configure(new MaterialDef("Unmarked", 1f / (k_UnmarkedSide * k_UnmarkedSide * k_UnmarkedSide), 0.5f, Color.gray, 0f, 0.3f), 1f);

            var grab = root.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.Kinematic;
            grab.useDynamicAttach = true;
            grab.throwOnDetach = false;
            grab.smoothPosition = false;
            grab.smoothRotation = false;

            root.AddComponent<PseudoHapticGrabTransformer>();
            root.AddComponent<PseudoHapticGhost>().LineMaterial = lineMaterial;
            return block;
        }

        static Ramp CreateRamp(Transform parent, Vector3 lowerMidpoint, Material surfaceMaterial, Material metal, Material startMaterial)
        {
            const float thickness = 0.02f;
            const float railHeight = 0.03f;
            const float railWidth = 0.008f;

            var root = new GameObject("Ramp").transform;
            root.SetParent(parent, false);

            var angle = k_RampAngleDeg * Mathf.Deg2Rad;
            var rotation = Quaternion.Euler(0f, 0f, -k_RampAngleDeg);
            var along = rotation * Vector3.right;
            var up = rotation * Vector3.up;

            // The low end rests on the table, around the given centre of the ramp's footprint.
            var height = Mathf.Sin(angle) * k_RampLength * 0.5f;
            var center = new Vector3(lowerMidpoint.x, lowerMidpoint.y + height + thickness * 0.5f + 0.002f, lowerMidpoint.z);

            var surface = CreateBox("Ramp Surface", root, center, new Vector3(k_RampLength, thickness, k_RampWidth), surfaceMaterial, true);
            surface.transform.rotation = rotation;
            surface.GetComponent<Collider>().sharedMaterial = CreatePhysicsMaterial("RampSurface", k_RampFriction);

            // Rails and stop have no friction, so they contain a block without slowing it.
            var slick = CreatePhysicsMaterial("Slick", 0f);
            foreach (var side in new[] { -1f, 1f })
            {
                var rail = CreateBox("Ramp Rail", root,
                    center + up * (thickness + railHeight) * 0.5f + Vector3.forward * side * (k_RampWidth - railWidth) * 0.5f,
                    new Vector3(k_RampLength, railHeight, railWidth), metal, true);
                rail.transform.rotation = rotation;
                rail.GetComponent<Collider>().sharedMaterial = slick;
            }

            var stop = CreateBox("Ramp Stop", root,
                center + along * (k_RampLength * 0.5f - 0.01f) + up * (thickness * 0.5f + 0.04f),
                new Vector3(0.02f, 0.08f, k_RampWidth), metal, true);
            stop.transform.rotation = rotation;
            stop.GetComponent<Collider>().sharedMaterial = slick;

            // A support under the high end, so the plank reads as propped up.
            var topEnd = center - along * k_RampLength * 0.5f;
            var support = CreateBox("Ramp Support", root,
                new Vector3(topEnd.x + 0.03f, (lowerMidpoint.y + topEnd.y) * 0.5f, center.z),
                new Vector3(0.05f, topEnd.y - lowerMidpoint.y, k_RampWidth), metal, false);
            support.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;

            var surfaceTop = center + up * (thickness * 0.5f + 0.0005f);
            var line = CreateBox("Ramp Start Line", root,
                surfaceTop + along * (k_RampStartAlong - k_RampLength * 0.5f),
                new Vector3(0.012f, 0.001f, k_RampWidth - 2f * railWidth), startMaterial, false);
            line.transform.rotation = rotation;
            line.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            for (var cm = 10; cm < k_RampLength * 100f; cm += 10)
            {
                var tick = CreateBox("Ramp Tick " + cm, root,
                    surfaceTop + along * (cm * 0.01f - k_RampLength * 0.5f) + Vector3.back * (k_RampWidth * 0.5f - 0.02f),
                    new Vector3(0.003f, 0.001f, 0.03f), metal, false);
                tick.transform.rotation = rotation;
                tick.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }

            CreateSign("Ramp Sign", parent, $"RAMP  ·  {k_RampAngleDeg:0.#}°  ·  start at the green line", new Vector3(lowerMidpoint.x, k_TableTop + 0.001f, 0.385f));

            return surface.AddComponent<Ramp>();
        }

        static ExperimentPanel CreateExperimentPanel(Transform parent, Vector3 position, Material panelMaterial)
        {
            const int buttonCount = 7;
            var size = new Vector2(1.16f, 0.86f);
            var root = new GameObject("Experiment Panel");
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(-6f, 0f, 0f);

            var background = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(background.GetComponent<Collider>());
            background.name = "Background";
            background.transform.SetParent(root.transform, false);
            background.transform.localPosition = new Vector3(0f, 0f, 0.005f);
            background.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = background.GetComponent<Renderer>();
            renderer.sharedMaterial = panelMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;

            var title = CreateText("Title", root.transform, "Participant mode", 0.50f, new Vector2(size.x - 0.08f, 0.07f), TextAlignmentOptions.TopLeft);
            title.rectTransform.anchoredPosition = new Vector2(0f, size.y * 0.5f - 0.07f);
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(1f, 0.72f, 0.35f);

            var body = CreateText("Body", root.transform, "", 0.44f, new Vector2(size.x - 0.08f, 0.28f), TextAlignmentOptions.TopLeft);
            body.rectTransform.anchoredPosition = new Vector2(0f, size.y * 0.5f - 0.25f);

            var buttonMaterial = CreateMaterial("PanelButton", new Color(0.17f, 0.24f, 0.33f), 0f, 0f, unlit: true);
            var buttons = new PanelButton[buttonCount];
            for (var i = 0; i < buttonCount; i++)
            {
                var go = new GameObject("Button " + (i + 1));
                go.transform.SetParent(root.transform, false);
                go.AddComponent<BoxCollider>();

                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.DestroyImmediate(quad.GetComponent<Collider>());
                quad.name = "Background";
                quad.transform.SetParent(go.transform, false);
                quad.transform.localPosition = new Vector3(0f, 0f, 0.002f);
                var quadRenderer = quad.GetComponent<Renderer>();
                quadRenderer.sharedMaterial = buttonMaterial;
                quadRenderer.shadowCastingMode = ShadowCastingMode.Off;

                var label = CreateText("Label", go.transform, "", 0.40f, new Vector2(1f, 0.08f), TextAlignmentOptions.Center);
                go.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();

                var button = go.AddComponent<PanelButton>();
                button.Label = label;
                button.Background = quadRenderer;
                buttons[i] = button;
            }

            var panel = root.AddComponent<ExperimentPanel>();
            panel.Title = title;
            panel.Body = body;
            panel.Buttons = buttons;
            return panel;
        }

        static Lever CreateLever(Transform parent, Vector3 tablePosition, Material plankMaterial, Material fulcrumMaterial)
        {
            const float length = 0.8f;
            const float thickness = 0.02f;
            const float width = 0.14f;
            const float fulcrumSize = 0.10f;

            var root = new GameObject("Lever").transform;
            root.SetParent(parent, false);

            // A cube on edge reads as a wedge; it is visual only, the hinge does the work.
            var fulcrum = CreateBox("Fulcrum", root, tablePosition, new Vector3(fulcrumSize, fulcrumSize, width + 0.02f), fulcrumMaterial, false);
            fulcrum.transform.rotation = Quaternion.Euler(0f, 0f, 45f);

            var pivotY = tablePosition.y + fulcrumSize * 0.7071f + thickness * 0.5f + 0.004f;
            var plank = new GameObject("Plank");
            plank.transform.SetParent(root, false);
            plank.transform.position = new Vector3(tablePosition.x, pivotY, tablePosition.z);

            CreateBox("Visual", plank.transform, plank.transform.position, new Vector3(length, thickness, width), plankMaterial, false);

            var box = plank.AddComponent<BoxCollider>();
            box.size = new Vector3(length, thickness, width);
            box.sharedMaterial = CreatePhysicsMaterial("Plank", 0.7f);

            var body = plank.AddComponent<Rigidbody>();
            body.mass = 2f;
            body.angularDamping = 1.5f;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            var hinge = plank.AddComponent<HingeJoint>();
            hinge.anchor = Vector3.zero;
            hinge.axis = Vector3.forward;
            hinge.useLimits = true;
            hinge.limits = new JointLimits { min = -9f, max = 9f, bounciness = 0f };

            // Loads sit above the pivot, which on its own is an unstable equilibrium. A light
            // centering spring makes the plank behave like a beam balance: tilt grows with the
            // torque difference and returns to level when the two sides match.
            hinge.useSpring = true;
            hinge.spring = new JointSpring { spring = k_LeverSpring, damper = k_LeverDamper, targetPosition = 0f };

            // Tick marks every 5 cm so loads can be placed at a known arm.
            for (var cm = -35; cm <= 35; cm += 5)
            {
                if (cm == 0)
                    continue;

                var major = cm % 10 == 0;
                var x = cm * 0.01f;
                var tick = CreateBox("Tick " + cm, plank.transform,
                    plank.transform.position + new Vector3(x, thickness * 0.5f + 0.0005f, -width * 0.5f + (major ? 0.02f : 0.012f)),
                    new Vector3(0.003f, 0.001f, major ? 0.04f : 0.024f), fulcrumMaterial, false);
                tick.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

                if (major)
                {
                    var number = CreateText("Mark " + cm, plank.transform, Mathf.Abs(cm).ToString(), 0.16f, new Vector2(0.06f, 0.02f), TextAlignmentOptions.Center);
                    number.transform.position = plank.transform.position + new Vector3(x, 0f, -width * 0.5f - 0.001f);
                    number.color = new Color(0.15f, 0.12f, 0.10f);
                }
            }

            CreateSign("Lever Sign", parent, "LEVER  ·  marks in cm from the pivot", new Vector3(tablePosition.x, k_TableTop + 0.001f, tablePosition.z - 0.13f));

            return plank.AddComponent<Lever>();
        }

        static WaterVolume CreateTank(Transform parent, Vector3 tablePosition, Material glass, Material water)
        {
            const float sizeX = 0.50f;
            const float sizeZ = 0.36f;
            const float height = 0.30f;
            const float wall = 0.01f;
            const float depth = 0.24f;

            var root = new GameObject("Water Tank").transform;
            root.SetParent(parent, false);
            root.position = tablePosition;

            var mid = tablePosition + Vector3.up * (height * 0.5f);
            CreateBox("Bottom", root, tablePosition + Vector3.up * (wall * 0.5f), new Vector3(sizeX, wall, sizeZ), glass, true);
            CreateBox("Wall Left", root, mid + Vector3.left * (sizeX - wall) * 0.5f, new Vector3(wall, height, sizeZ), glass, true);
            CreateBox("Wall Right", root, mid + Vector3.right * (sizeX - wall) * 0.5f, new Vector3(wall, height, sizeZ), glass, true);
            CreateBox("Wall Front", root, mid + Vector3.back * (sizeZ - wall) * 0.5f, new Vector3(sizeX, height, wall), glass, true);
            CreateBox("Wall Back", root, mid + Vector3.forward * (sizeZ - wall) * 0.5f, new Vector3(sizeX, height, wall), glass, true);

            var volume = CreateBox("Water", root, tablePosition + Vector3.up * (wall + depth * 0.5f),
                new Vector3(sizeX - 2f * wall, depth, sizeZ - 2f * wall), water, true);
            volume.GetComponent<BoxCollider>().isTrigger = true;
            volume.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            volume.layer = LayerMask.NameToLayer("Ignore Raycast");

            return volume.AddComponent<WaterVolume>();
        }

        static void CreateHud(Transform parent, Vector3 position, Material panelMaterial, Lever lever, WaterVolume water, Ramp ramp)
        {
            var size = new Vector2(1.05f, 1.06f);
            var root = new GameObject("HUD Panel");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, Quaternion.Euler(-8f, 12f, 0f));

            var background = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(background.GetComponent<Collider>());
            background.name = "Background";
            background.transform.SetParent(root.transform, false);
            background.transform.localPosition = new Vector3(0f, 0f, 0.005f);
            background.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = background.GetComponent<Renderer>();
            renderer.sharedMaterial = panelMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;

            var text = CreateText("Text", root.transform, "PHYSICS YOU CAN FEEL", 0.42f, size - new Vector2(0.08f, 0.08f), TextAlignmentOptions.TopLeft);

            // The controller stays active to keep its physics settings and to bring the panel
            // back after experiment mode, so what it hides is a child holding the visuals.
            var visuals = new GameObject("Visuals");
            visuals.transform.SetParent(root.transform, false);
            background.transform.SetParent(visuals.transform, false);
            text.transform.SetParent(visuals.transform, false);

            var hud = root.AddComponent<SandboxHud>();
            hud.Text = text;
            hud.Lever = lever;
            hud.Water = water;
            hud.Ramp = ramp;
            hud.PanelRoot = visuals;
        }

        static TextMeshPro CreateSign(string name, Transform parent, string content, Vector3 position)
        {
            var sign = CreateText(name, parent, content, 0.3f, new Vector2(0.7f, 0.05f), TextAlignmentOptions.Center);
            sign.transform.SetPositionAndRotation(position, Quaternion.Euler(90f, 0f, 0f));
            sign.color = new Color(0.95f, 0.92f, 0.85f);
            return sign;
        }

        static TextMeshPro CreateText(string name, Transform parent, string content, float fontSize, Vector2 size, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshPro>();
            text.rectTransform.sizeDelta = size;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.richText = true;
            text.text = content;
            return text;
        }

        static GameObject CreateBox(string name, Transform parent, Vector3 position, Vector3 size, Material material, bool keepCollider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, true);
            go.transform.position = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider)
                Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static Material CreateMaterial(string name, Color color, float metallic, float smoothness, bool transparent = false, bool unlit = false)
        {
            var path = $"{k_MaterialDir}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            var material = existing != null ? existing : new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));

            material.SetColor("_BaseColor", color);
            if (!unlit)
            {
                material.SetFloat("_Metallic", metallic);
                material.SetFloat("_Smoothness", smoothness);
            }

            if (transparent)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetShaderPassEnabled("DepthOnly", false);
                material.SetShaderPassEnabled("ShadowCaster", false);
                material.renderQueue = (int)RenderQueue.Transparent;
            }

            if (existing == null)
                AssetDatabase.CreateAsset(material, path);
            else
                EditorUtility.SetDirty(material);

            return material;
        }

        static PhysicsMaterial CreatePhysicsMaterial(string name, float friction)
        {
            var path = $"{k_MaterialDir}/{name}.physicMaterial";
            var existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            var material = existing != null ? existing : new PhysicsMaterial(name);

            material.staticFriction = friction;
            material.dynamicFriction = friction * 0.85f;
            material.bounciness = 0f;

            if (existing == null)
                AssetDatabase.CreateAsset(material, path);
            else
                EditorUtility.SetDirty(material);

            return material;
        }
    }
}
