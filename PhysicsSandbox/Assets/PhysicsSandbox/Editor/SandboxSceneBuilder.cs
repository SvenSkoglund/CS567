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
    /// water tank, and the readout panel.
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
            CreateSign("Equal Mass Sign", root, "SAME MASS  ·  1 kg each", new Vector3(-0.78f, k_TableTop + 0.001f, 0.385f));

            // Equal-volume set: same size, so the weight cue alone tells them apart.
            var equalVolume = new GameObject("Equal Volume Set").transform;
            equalVolume.SetParent(root, false);
            const float litre = 0.001f;
            for (var i = 0; i < MaterialLibrary.All.Length; i++)
            {
                var material = MaterialLibrary.All[i];
                CreateBlock(equalVolume, material, material.densityKgPerM3 * litre, new Vector3(-0.32f + 0.15f * i, k_TableTop, k_FrontRowZ), lineMaterial);
            }
            CreateSign("Equal Volume Sign", root, "SAME SIZE  ·  1 litre each", new Vector3(-0.095f, k_TableTop + 0.001f, 0.385f));

            var lever = CreateLever(root, new Vector3(-0.38f, k_TableTop, 0.86f), woodLight, darkMetal);
            var waterVolume = CreateTank(root, new Vector3(0.74f, k_TableTop, 0.70f), glass, water);
            CreateSign("Tank Sign", root, "WATER TANK  ·  1000 kg/m³", new Vector3(0.74f, k_TableTop + 0.001f, 0.385f));

            CreateHud(root, new Vector3(0.45f, 1.60f, 1.35f), panel, lever, waterVolume);

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

            return $"built {k_ScenePath}: {equalMass.childCount + equalVolume.childCount} blocks, " +
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

        static void CreateHud(Transform parent, Vector3 position, Material panelMaterial, Lever lever, WaterVolume water)
        {
            var size = new Vector2(1.05f, 0.92f);
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

            var hud = root.AddComponent<SandboxHud>();
            hud.Text = text;
            hud.Lever = lever;
            hud.Water = water;
        }

        static void CreateSign(string name, Transform parent, string content, Vector3 position)
        {
            var sign = CreateText(name, parent, content, 0.3f, new Vector2(0.7f, 0.05f), TextAlignmentOptions.Center);
            sign.transform.SetPositionAndRotation(position, Quaternion.Euler(90f, 0f, 0f));
            sign.color = new Color(0.95f, 0.92f, 0.85f);
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
