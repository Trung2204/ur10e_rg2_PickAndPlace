using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RosMessageTypes.Geometry;
using RosMessageTypes.Ur10eRg2Moveit;
using RosMessageTypes.Moveit;
using RosMessageTypes.Std;
using RosMessageTypes.Shape;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;

public class TrajectoryPlanner : MonoBehaviour
{
    // Hardcoded variables
    const int k_NumRobotJoints = 6;
    const float k_JointAssignmentWait = 0.1f;
    const float k_PoseAssignmentWait = 0.5f;

    // Variables required for ROS communication
    [SerializeField]
    private string m_TopicName = "/collision_object";
    [SerializeField]
    string m_RosServiceName = "ur10e_rg2_moveit";
    public string RosServiceName { get => m_RosServiceName; set => m_RosServiceName = value; }

    [SerializeField]
    GameObject m_UR10e;
    public GameObject UR10e { get => m_UR10e; set => m_UR10e = value; }
    [SerializeField]
    GameObject m_Target;
    public GameObject Target { get => m_Target; set => m_Target = value; }
    [SerializeField]
    GameObject m_TargetPlacement;
    public GameObject TargetPlacement { get => m_TargetPlacement; set => m_TargetPlacement = value; }
    [SerializeField]
    GameObject m_Table;
    public GameObject Table { get => m_Table; set => m_Table = value; }
    [SerializeField]
    GameObject m_Printer;
    public GameObject Printer { get => m_Printer; set => m_Printer = value; }


    // Assures that the gripper is always positioned beside the m_Target cube before grasping.
    readonly Quaternion m_PickOrientation = Quaternion.Euler(0, 180, 90);
    readonly Vector3 m_PickPoseOffset = Vector3.left * 0.47f;
    readonly Quaternion m_PlaceOrientation = Quaternion.Euler(0, 90, 180);
    readonly Vector3 m_PlacePoseOffset = Vector3.up * 0.28f;

    // Articulation Bodies for the Robot arm
    ArticulationBody[] m_JointArticulationBodies;
    // Articulation Bodies for the Gripper
    ArticulationBody m_LeftInnerKnuckle;
    ArticulationBody m_RightInnerKnuckle;
    ArticulationBody m_LeftOuterKnuckle;
    ArticulationBody m_RightOuterKnuckle;
    ArticulationBody m_leftInnerFinger;
    ArticulationBody m_rightInnerFinger;


    // ROS Connector
    ROSConnection m_Ros;

    /// <summary>
    ///     Find all robot joints in Awake() and add them to the jointArticulationBodies array.
    ///     Find left and right finger joints and assign them to their respective articulation body objects.
    /// </summary>
    void Start()
    {
        // Get ROS connection static instance
        m_Ros = ROSConnection.GetOrCreateInstance();
        m_Ros.RegisterRosService<MoverServiceRequest, MoverServiceResponse>(m_RosServiceName);
        // Register the collision object publisher
        m_Ros.RegisterPublisher<CollisionObjectMsg>(m_TopicName);

        m_JointArticulationBodies = new ArticulationBody[k_NumRobotJoints];

        var linkName = string.Empty;
        for (var i = 0; i < k_NumRobotJoints; i++)
        {
            linkName += SourceDestinationPublisher.LinkNames[i];
            m_JointArticulationBodies[i] = m_UR10e.transform.Find(linkName).GetComponent<ArticulationBody>();
        }

        // Identify gripper joints
        string gripperBasePath = "base_link/base_link_inertia/shoulder_link/upper_arm_link/forearm_link/wrist_1_link/wrist_2_link/wrist_3_link/onrobot_rg2_base_link";
        m_LeftInnerKnuckle = m_UR10e.transform.Find(gripperBasePath + "/left_inner_knuckle").GetComponent<ArticulationBody>();
        m_RightInnerKnuckle = m_UR10e.transform.Find(gripperBasePath + "/right_inner_knuckle").GetComponent<ArticulationBody>();
        m_LeftOuterKnuckle = m_UR10e.transform.Find(gripperBasePath + "/left_outer_knuckle").GetComponent<ArticulationBody>();
        m_RightOuterKnuckle = m_UR10e.transform.Find(gripperBasePath + "/right_outer_knuckle").GetComponent<ArticulationBody>();
        m_leftInnerFinger = m_UR10e.transform.Find(gripperBasePath + "/left_outer_knuckle/left_inner_finger").GetComponent<ArticulationBody>();
        m_rightInnerFinger = m_UR10e.transform.Find(gripperBasePath + "/right_outer_knuckle/right_inner_finger").GetComponent<ArticulationBody>();

        if (!m_LeftInnerKnuckle || !m_RightInnerKnuckle || !m_LeftOuterKnuckle || !m_RightOuterKnuckle || !m_leftInnerFinger || !m_rightInnerFinger)
        {
            Debug.LogError("Some gripper articulation bodies are missing. Please check the hierarchy.");
        }
    }

    /// <summary>
    ///     Close the gripper
    /// </summary>
    void CloseGripper()
    {
        float closeValue = 24f;

        SetGripperPosition(closeValue);
    }

    /// <summary>
    ///     Open the gripper
    /// </summary>
    void OpenGripper()
    {
        float openValue = 10f;

        SetGripperPosition(openValue);
    }

    /// <summary>
    ///     Get the current values of the robot's joint angles.
    /// </summary>
    /// <returns>Ur10eMoveitJoints</returns>
    Ur10eMoveitJointsMsg CurrentJointConfig()
    {
        var joints = new Ur10eMoveitJointsMsg();

        for (var i = 0; i < k_NumRobotJoints; i++)
        {
            joints.joints[i] = m_JointArticulationBodies[i].jointPosition[0];
        }

        return joints;
    }

    /// <summary>
    ///     Create a new MoverServiceRequest with the current values of the robot's joint angles,
    ///     the target cube's current position and rotation, and the targetPlacement position and rotation.
    ///     Call the MoverService using the ROSConnection and if a trajectory is successfully planned,
    ///     execute the trajectories in a coroutine.
    /// </summary>
    public void PublishJoints()
    {
        // Publish the table mesh at the start
        PublishTableMesh();
        // Publish the printer mesh at the start
        PublishPrinterMesh();

        var request = new MoverServiceRequest();
        request.joints_input = CurrentJointConfig();

        // Pick Pose
        request.pick_pose = new PoseMsg
        {
            position = (m_Target.transform.position + m_PickPoseOffset).To<FLU>(),
            orientation = m_PickOrientation.To<FLU>()
        };

        // Place Pose
        request.place_pose = new PoseMsg
        {
            position = (m_TargetPlacement.transform.position + m_PlacePoseOffset).To<FLU>(),
            orientation = m_PlaceOrientation.To<FLU>()
        };

        m_Ros.SendServiceMessage<MoverServiceResponse>(m_RosServiceName, request, TrajectoryResponse);
    }

    void TrajectoryResponse(MoverServiceResponse response)
    {
        if (response.trajectories.Length > 0)
        {
            Debug.Log("Trajectory returned.");
            StartCoroutine(ExecuteTrajectories(response));
        }
        else
        {
            Debug.LogError("No trajectory returned from MoverService.");
        }
    }

    /// <summary>
    ///     Execute the returned trajectories from the MoverService.
    ///     The expectation is that the MoverService will return four trajectory plans,
    ///     PreGrasp, Grasp, PickUp, and Place,
    ///     where each plan is an array of robot poses. A robot pose is the joint angle values
    ///     of the six robot joints.
    ///     Executing a single trajectory will iterate through every robot pose in the array while updating the
    ///     joint values on the robot.
    /// </summary>
    /// <param name="response"> MoverServiceResponse received from ur10e_rg2_moveit mover service running in ROS</param>
    /// <returns></returns>
    IEnumerator ExecuteTrajectories(MoverServiceResponse response)
    {
        if (response.trajectories != null)
        {
            // For every trajectory plan returned
            for (var poseIndex = 0; poseIndex < response.trajectories.Length; poseIndex++)
            {
                // For every robot pose in trajectory plan
                foreach (var t in response.trajectories[poseIndex].joint_trajectory.points)
                {
                    var jointPositions = t.positions;
                    var result = jointPositions.Select(r => (float)r * Mathf.Rad2Deg).ToArray();

                    // Set the joint values for every joint
                    for (var joint = 0; joint < m_JointArticulationBodies.Length; joint++)
                    {
                        var joint1XDrive = m_JointArticulationBodies[joint].xDrive;
                        joint1XDrive.target = result[joint];
                        m_JointArticulationBodies[joint].xDrive = joint1XDrive;
                    }

                    // Wait for robot to achieve pose for all joint assignments
                    yield return new WaitForSeconds(k_JointAssignmentWait);
                }

                // Close the gripper if completed executing the trajectory for the Grasp pose
                if (poseIndex == (int)Poses.Grasp)
                {
                    CloseGripper();
                }

                // Wait for the robot to achieve the final pose from joint assignment
                yield return new WaitForSeconds(k_PoseAssignmentWait);
            }

            // All trajectories have been executed, open the gripper to place the target cube
            OpenGripper();
        }
    }

    enum Poses
    {
        PreGrasp,
        Grasp,
        PickUp,
        Place
    }

    void PublishTableMesh()
    {
        // Get the MeshFilter component from the Table GameObject
        MeshFilter meshFilter = m_Table.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("MeshFilter component not found on Table GameObject.");
            return;
        }

        // Retrieve the mesh, vertices, and triangles data
        Mesh mesh = meshFilter.mesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        // Validate mesh data
        if (vertices == null || vertices.Length == 0 || triangles == null || triangles.Length == 0)
        {
            Debug.LogError("Mesh data is invalid or empty.");
            return;
        }

        // Prepare vertices and triangles for ROS messages
        List<PointMsg> points = new List<PointMsg>();
        foreach (Vector3 vertex in vertices)
        {
            // Convert vertex to FLU orientation and add to points list
            var fluVertex = m_Table.transform.TransformPoint(vertex).To<FLU>();
            points.Add(new PointMsg(fluVertex.x, fluVertex.y, fluVertex.z));
        }

        List<MeshTriangleMsg> triangleMsgs = new List<MeshTriangleMsg>();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            triangleMsgs.Add(new MeshTriangleMsg
            {
                vertex_indices = new uint[]
                {
                    (uint)triangles[i],
                    (uint)triangles[i + 2], // Swap these two indices
                    (uint)triangles[i + 1]
                }
            });
        }


        // Create MeshMsg
        MeshMsg meshMsg = new MeshMsg
        {
            vertices = points.ToArray(),
            triangles = triangleMsgs.ToArray()
        };

        // Get the table's position and rotation in FLU
        PoseMsg tablePose = new PoseMsg
        {
            // Set the position of the object to the origin (0, 0, 0) in the base_link frame
            position = new PointMsg(0.0, 0.0, 0.0),

            // Set the orientation of the object to no rotation (identity quaternion) in the base_link frame
            orientation = new QuaternionMsg(0.0, 0.0, 0.0, 1.0)

        };

        // Create CollisionObjectMsg
        var collisionObject = new CollisionObjectMsg
        {
            header = new HeaderMsg
            {
                frame_id = "base_link"
            },
            id = "table",
            operation = CollisionObjectMsg.ADD,
            mesh_poses = new PoseMsg[] { tablePose },
            meshes = new MeshMsg[] { meshMsg }
        };

        // Publish the CollisionObjectMsg
        m_Ros.Publish("/collision_object", collisionObject);
    }

    void PublishPrinterMesh()
    {
        // Get the MeshFilter component from the 3D Printer GameObject
        MeshFilter meshFilter = m_Printer.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("MeshFilter component not found on 3D Printer GameObject.");
            return;
        }

        // Retrieve the mesh, vertices, and triangles data
        Mesh mesh = meshFilter.mesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        // Validate mesh data
        if (vertices == null || vertices.Length == 0 || triangles == null || triangles.Length == 0)
        {
            Debug.LogError("Mesh data is invalid or empty.");
            return;
        }
        // Prepare vertices and triangles for ROS messages
        List<PointMsg> points = new List<PointMsg>();
        foreach (Vector3 vertex in vertices)
        {
            // Convert vertex to FLU orientation and add to points list
            var fluVertex = m_Printer.transform.TransformPoint(vertex).To<FLU>();
            points.Add(new PointMsg(fluVertex.x, fluVertex.y, fluVertex.z));
        }

        List<MeshTriangleMsg> triangleMsgs = new List<MeshTriangleMsg>();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            triangleMsgs.Add(new MeshTriangleMsg
            {
                vertex_indices = new uint[]
                {
                    (uint)triangles[i],
                    (uint)triangles[i + 2], // Swap these two indices
                    (uint)triangles[i + 1]
                }
            });
        }

        // Create MeshMsg
        MeshMsg meshMsg = new MeshMsg
        {
            vertices = points.ToArray(),
            triangles = triangleMsgs.ToArray()
        };
        
        PoseMsg printerPose = new PoseMsg
        {
            // Set the position of the object to the origin (0, 0, 0) in the base_link frame
            position = new PointMsg(0.0, 0.0, 0.0),

            // Set the orientation of the object to no rotation (identity quaternion) in the base_link frame
            orientation = new QuaternionMsg(0.0, 0.0, 0.0, 1.0)
        };

        // Create CollisionObjectMsg
        var collisionObject = new CollisionObjectMsg
        {
            header = new HeaderMsg
            {
                frame_id = "base_link"
            },
            id = "3d_printer",
            operation = CollisionObjectMsg.ADD,
            mesh_poses = new PoseMsg[] { printerPose },
            meshes = new MeshMsg[] { meshMsg }
        };

        // Publish the CollisionObjectMsg
        m_Ros.Publish("/collision_object", collisionObject);
    }

    void SetGripperPosition(float position)
    {
        ArticulationDrive drive = m_LeftInnerKnuckle.xDrive;
        drive.target = -position;
        m_LeftInnerKnuckle.xDrive = drive;

        drive = m_RightInnerKnuckle.xDrive;
        drive.target = -position;
        m_RightInnerKnuckle.xDrive = drive;

        drive = m_LeftOuterKnuckle.xDrive;
        drive.target = position;
        m_LeftOuterKnuckle.xDrive = drive;

        drive = m_RightOuterKnuckle.xDrive;
        drive.target = -position;
        m_RightOuterKnuckle.xDrive = drive;

        drive = m_leftInnerFinger.xDrive;
        drive.target = position;
        m_leftInnerFinger.xDrive = drive;

        drive = m_rightInnerFinger.xDrive;
        drive.target = position;
        m_rightInnerFinger.xDrive = drive;
    }

}