#if UNITY_EDITOR
using System.IO;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIB.Editor
{
    public static class SupineCribSceneBuilder
    {
        public const string ScenePath = "Assets/AIB/Scenes/SupineCribBodySchema.unity";

        [MenuItem("AIB/Crib/Create Supine Crib Scene")]
        public static void CreateOrUpdateScene()
        {
            Directory.CreateDirectory("Assets/AIB/Scenes");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "SupineCribBodySchema";

            CreateCameraAndLight();
            CreateCrib();
            CreateAbeBody();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[AIB] Supine crib body-schema scene written: {ScenePath}");
        }

        private static void CreateCameraAndLight()
        {
            var lightObject = new GameObject("Crib Soft Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var cameraObject = new GameObject("Crib Overview Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.06f, 0.08f);
            cameraObject.transform.position = new Vector3(0f, 2.2f, -2.4f);
            cameraObject.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
        }

        private static void CreateCrib()
        {
            CreateBox("Crib Floor", new Vector3(0f, -0.025f, 0f), new Vector3(1.2f, 0.05f, 1.2f), new Color(0.55f, 0.55f, 0.60f));
            CreateBox("Crib Wall North", new Vector3(0f, 0.2f, 0.62f), new Vector3(1.25f, 0.4f, 0.05f), new Color(0.70f, 0.72f, 0.78f));
            CreateBox("Crib Wall South", new Vector3(0f, 0.2f, -0.62f), new Vector3(1.25f, 0.4f, 0.05f), new Color(0.70f, 0.72f, 0.78f));
            CreateBox("Crib Wall East", new Vector3(0.62f, 0.2f, 0f), new Vector3(0.05f, 0.4f, 1.25f), new Color(0.70f, 0.72f, 0.78f));
            CreateBox("Crib Wall West", new Vector3(-0.62f, 0.2f, 0f), new Vector3(0.05f, 0.4f, 1.25f), new Color(0.70f, 0.72f, 0.78f));
        }

        private static void CreateAbeBody()
        {
            GameObject torso = CreateCapsule("Abe_Torso", new Vector3(0f, 0.18f, 0f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.22f, 0.36f, 0.22f), 8f, new Color(0.85f, 0.75f, 0.66f));
            var agent = torso.AddComponent<AIBCribBodyAgent>();
            Rigidbody torsoRb = torso.GetComponent<Rigidbody>();
            agent.torso = torsoRb;

            // AIBCribBodyAgent inherits Agent, and Agent carries a RequireComponent
            // for BehaviorParameters. Adding the agent may already have created a
            // default "My Behavior" component. Reuse it instead of adding a second
            // BehaviorParameters component; ML-Agents will otherwise discover the
            // default first and report Discrete(1), VectorSensor_size1 at runtime.
            BehaviorParameters behavior = torso.GetComponent<BehaviorParameters>();
            if (behavior == null)
            {
                behavior = torso.AddComponent<BehaviorParameters>();
            }
            behavior.BehaviorName = "AIBCribBodySchema";
            behavior.BehaviorType = BehaviorType.Default;
            behavior.BrainParameters.ActionSpec = ActionSpec.MakeContinuous(AIBCribBodyAgent.ActionCount);
            behavior.BrainParameters.VectorObservationSize = 78;

            DecisionRequester requester = torso.AddComponent<DecisionRequester>();
            requester.DecisionPeriod = 1;
            requester.TakeActionsBetweenDecisions = true;

            GameObject head = CreateSphere("Abe_Head", new Vector3(0f, 0.22f, 0.42f), new Vector3(0.16f, 0.16f, 0.16f), 1.2f, new Color(0.88f, 0.76f, 0.68f));
            agent.headContact = head.AddComponent<AIBCribContactSensor>();
            agent.torsoContact = torso.AddComponent<AIBCribContactSensor>();
            agent.neck = AttachJoint(head, torsoRb, new Vector3(0f, 0f, -0.08f), "NeckJoint", -55f, 65f, 80f, 80f, 35f, 35f);

            GameObject lUpperArm = CreateCapsule("Abe_LeftUpperArm", new Vector3(-0.26f, 0.17f, 0.16f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.08f, 0.18f, 0.08f), 0.6f, Color.cyan);
            agent.leftUpperArmContact = lUpperArm.AddComponent<AIBCribContactSensor>();
            agent.leftShoulder = AttachJoint(lUpperArm, torsoRb, new Vector3(0.08f, 0f, 0f), "LeftShoulderJoint", -35f, 150f, 20f, 20f, -20f, 145f);
            GameObject lLowerArm = CreateCapsule("Abe_LeftLowerArm", new Vector3(-0.44f, 0.16f, 0.16f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.07f, 0.16f, 0.07f), 0.45f, Color.cyan);
            agent.leftLowerArmContact = lLowerArm.AddComponent<AIBCribContactSensor>();
            agent.leftElbow = AttachJoint(lLowerArm, lUpperArm.GetComponent<Rigidbody>(), new Vector3(0.08f, 0f, 0f), "LeftElbowJoint", 0f, 140f, 1f, 1f, 1f, 1f);

            GameObject rUpperArm = CreateCapsule("Abe_RightUpperArm", new Vector3(0.26f, 0.17f, 0.16f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.08f, 0.18f, 0.08f), 0.6f, Color.magenta);
            agent.rightUpperArmContact = rUpperArm.AddComponent<AIBCribContactSensor>();
            agent.rightShoulder = AttachJoint(rUpperArm, torsoRb, new Vector3(-0.08f, 0f, 0f), "RightShoulderJoint", -35f, 150f, 20f, 20f, -145f, 20f);
            GameObject rLowerArm = CreateCapsule("Abe_RightLowerArm", new Vector3(0.44f, 0.16f, 0.16f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.07f, 0.16f, 0.07f), 0.45f, Color.magenta);
            agent.rightLowerArmContact = rLowerArm.AddComponent<AIBCribContactSensor>();
            agent.rightElbow = AttachJoint(rLowerArm, rUpperArm.GetComponent<Rigidbody>(), new Vector3(-0.08f, 0f, 0f), "RightElbowJoint", 0f, 140f, 1f, 1f, 1f, 1f);

            GameObject lUpperLeg = CreateCapsule("Abe_LeftUpperLeg", new Vector3(-0.11f, 0.16f, -0.38f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.09f, 0.22f, 0.09f), 0.9f, Color.green);
            agent.leftUpperLegContact = lUpperLeg.AddComponent<AIBCribContactSensor>();
            agent.leftHip = AttachJoint(lUpperLeg, torsoRb, new Vector3(0f, 0f, 0.11f), "LeftHipJoint", -10f, 125f, 20f, 20f, -20f, 60f);
            GameObject lLowerLeg = CreateCapsule("Abe_LeftLowerLeg", new Vector3(-0.11f, 0.14f, -0.66f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.08f, 0.20f, 0.08f), 0.7f, Color.green);
            agent.leftLowerLegContact = lLowerLeg.AddComponent<AIBCribContactSensor>();
            agent.leftKnee = AttachJoint(lLowerLeg, lUpperLeg.GetComponent<Rigidbody>(), new Vector3(0f, 0f, 0.10f), "LeftKneeJoint", 0f, 140f, 1f, 1f, 1f, 1f);

            GameObject rUpperLeg = CreateCapsule("Abe_RightUpperLeg", new Vector3(0.11f, 0.16f, -0.38f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.09f, 0.22f, 0.09f), 0.9f, Color.yellow);
            agent.rightUpperLegContact = rUpperLeg.AddComponent<AIBCribContactSensor>();
            agent.rightHip = AttachJoint(rUpperLeg, torsoRb, new Vector3(0f, 0f, 0.11f), "RightHipJoint", -10f, 125f, 20f, 20f, -60f, 20f);
            GameObject rLowerLeg = CreateCapsule("Abe_RightLowerLeg", new Vector3(0.11f, 0.14f, -0.66f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.08f, 0.20f, 0.08f), 0.7f, Color.yellow);
            agent.rightLowerLegContact = rLowerLeg.AddComponent<AIBCribContactSensor>();
            agent.rightKnee = AttachJoint(rLowerLeg, rUpperLeg.GetComponent<Rigidbody>(), new Vector3(0f, 0f, 0.10f), "RightKneeJoint", 0f, 140f, 1f, 1f, 1f, 1f);

            Selection.activeGameObject = torso;
        }

        private static GameObject CreateBox(string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.position = position;
            obj.transform.localScale = scale;
            SetColor(obj, color);
            return obj;
        }

        private static GameObject CreateSphere(string name, Vector3 position, Vector3 scale, float mass, Color color)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = name;
            obj.transform.position = position;
            obj.transform.localScale = scale;
            Rigidbody rb = obj.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.linearDamping = 0.8f;
            rb.angularDamping = 1.2f;
            SetColor(obj, color);
            return obj;
        }

        private static GameObject CreateCapsule(string name, Vector3 position, Quaternion rotation, Vector3 scale, float mass, Color color)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            obj.name = name;
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.transform.localScale = scale;
            Rigidbody rb = obj.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.linearDamping = 0.8f;
            rb.angularDamping = 1.2f;
            SetColor(obj, color);
            return obj;
        }

        private static ConfigurableJoint AttachJoint(GameObject child, Rigidbody connectedBody, Vector3 anchor, string name, float lowX, float highX, float yLowAbs, float yHighAbs, float zLow, float zHigh)
        {
            ConfigurableJoint joint = child.AddComponent<ConfigurableJoint>();
            joint.name = name;
            joint.connectedBody = connectedBody;
            joint.autoConfigureConnectedAnchor = true;
            joint.anchor = anchor;
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = ConfigurableJointMotion.Limited;
            joint.angularYMotion = ConfigurableJointMotion.Limited;
            joint.angularZMotion = ConfigurableJointMotion.Limited;
            joint.lowAngularXLimit = SoftLimit(lowX);
            joint.highAngularXLimit = SoftLimit(highX);
            joint.angularYLimit = SoftLimit(Mathf.Max(Mathf.Abs(yLowAbs), Mathf.Abs(yHighAbs)));
            joint.angularZLimit = SoftLimit(Mathf.Max(Mathf.Abs(zLow), Mathf.Abs(zHigh)));
            joint.rotationDriveMode = RotationDriveMode.Slerp;
            joint.slerpDrive = Drive(70f, 8f, 25f);
            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionDistance = 0.02f;
            joint.projectionAngle = 5f;
            return joint;
        }

        private static SoftJointLimit SoftLimit(float limit)
        {
            SoftJointLimit soft = new SoftJointLimit();
            soft.limit = limit;
            soft.contactDistance = 2f;
            return soft;
        }

        private static JointDrive Drive(float spring, float damper, float maxForce)
        {
            JointDrive drive = new JointDrive();
            drive.positionSpring = spring;
            drive.positionDamper = damper;
            drive.maximumForce = maxForce;
            return drive;
        }

        private static void SetColor(GameObject obj, Color color)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                renderer.sharedMaterial.color = color;
            }
        }
    }
}
#endif
