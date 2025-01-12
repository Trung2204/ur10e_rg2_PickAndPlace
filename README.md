# UR10e Robot Arm with RG2 Gripper Pick and Place Project

## Overview

This project is a part of a module at [Vietnamese-German University](https://vgu.edu.vn/), instructed by Dr.-Ing. Quang Huan Dong (huan.dq@vgu.edu.vn). The goal of this project is to adapt the pick and place tutorial from the [Unity Robotics Hub](https://github.com/Unity-Technologies/Unity-Robotics-Hub/tree/main/tutorials/pick_and_place) for the UR10e robot arm integrated with the OnRobot RG2 gripper. The objective is to automate the process of picking an object from inside a 3D printer and placing it at a desired location in a simulated environment using Unity. The project utilizes the Stochastic Trajectory Optimization for Motion Planning (STOMP) planner for motion planning.

## Table of Contents

- [Overview](#overview)
- [Installation](#installation)
  - [Prerequisites](#prerequisites)
  - [Clone the Repository](#clone-the-repository)
  - [Set Up ROS Workspace](#set-up-ros-workspace)
  - [Configure Unity Project](#configure-unity-project)
- [Usage](#usage)
- [Expected outcome](#expected-outcome)
- [Project Structure](#project-structure)

## Installation

To set up this project, follow these steps:

### Prerequisites

Ensure you have the following software installed:

- [ROS Noetic](https://wiki.ros.org/noetic/Installation/Ubuntu)
- [MoveIt](https://moveit.github.io/moveit_tutorials/doc/getting_started/getting_started.html) 
- [Unity hub](https://unity.com/download)
- [Unity 2020.3.11f1 (LTS)](https://unity.com/releases/editor/archive)
- [3D Printer MakerBot Replicator](https://sketchfab.com/3d-models/2-makerbot-10e13be074dd4d55a97b129c9b4d1959)
- [Sketchfab plugin for Unity](https://github.com/sketchfab/unity-plugin/releases)

### Clone the Repository

```bash
git clone https://github.com/Trung2204/ur10e_rg2_PickAndPlace.git
cd ur10e_rg2_PickAndPlace
```

### Set Up ROS Workspace
1. Ensure that you have [ROS installed](https://wiki.ros.org/noetic/Installation/Ubuntu)
2. Ensure that you have [Moveit and tools like catkin and wstool installed](https://moveit.github.io/moveit_tutorials/doc/getting_started/getting_started.html)
3. The provided files require the following packages to be installed. ROS Noetic users should run the following commands if the packages are not already present:

```bash
sudo apt-get update && sudo apt-get upgrade
sudo apt-get install python3-pip ros-noetic-robot-state-publisher ros-noetic-moveit ros-noetic-rosbridge-suite ros-noetic-joy ros-noetic-ros-control ros-noetic-ros-controllers
sudo -H pip3 install rospkg jsonpickle
```
4. Instal [STOMP](https://moveit.github.io/moveit_tutorials/doc/stomp_planner/stomp_planner_tutorial.html) from Source
```bash
cd ~/ROS
source /opt/ros/noetic/setup.bash
wstool set -t src stomp_ros https://github.com/ros-industrial/stomp_ros.git --git
wstool update -t src stomp_ros
wstool merge -t src src/stomp_ros/dependencies.rosinstall
wstool update -t src stomp ros_industrial_cmake_boilerplate
catkin build
```
5. Source the setup files

```bash
source devel/setup.bash
```

Ensure there are no errors. The ROS workspace is now ready to accept commands!

### Configure Unity Project

1. Open Unity Hub and go to the "Projects" tab, click the "Add" button, and navigate to and select the UnityProject directory within this cloned repository (`/PATH/TO/ur10e_rg2_PickAndPlace/UnityProject/`) to add the project to your Hub.

   ![](Image/hub_addproject.png)

2. Click the newly added project to open it.

3. In the Unity Project window, double click to open the `Assets/Scenes/EmptyScene` scene if it is not already open.

   ![](Image/0_unity.png)

4. Drag and drop [SketchfabForUnity-v1.2.1.unitypackage](https://github.com/sketchfab/unity-plugin/releases) file to the Asset panel.

   Follow the instructions to finish the setup of Sketchfab plugin for Unity.

5. Generate the MoveItMsg: RobotTrajectory, CollisionObject. These file describes the trajectory and collision contents that will be used in the sent and received messages.

   Select `Robotics -> Generate ROS Messages...` from the top menu bar.

   ![](Image/2_menu.png)

   In the ROS Message Browser window, click `Browse` next to the ROS message path. Navigate to and select the ROS directory of this cloned repository (`Unity-Robotics-Hub/tutorials/pick_and_place/ROS/`). This window will populate with all msg and srv files found in this directory.

   ![](Image/2_browser.png)

   > Note: If any of these ROS directories appear to be empty, you can run the command `git submodule update --init --recursive` to download the packages via Git submodules.

   Under `ROS/src/moveit_msgs/msg`, scroll to `RobotTrajectory.msg`, and click its `Build msg` button. The button text will change to "Rebuild msg" when it has finished building.

   ![](Image/2_robottraj.png)

6. Do the same for `ROS/src/moveit_msgs/msg/CollisionObject.msg` and `ROS/src/ur10e_rg2_moveit/msg/*` and `ROS/src/ur10e_rg2_moveit/srv/*`.

5. Next, the ROS TCP connection needs to be created. Select `Robotics -> ROS Settings` from the top menu bar.

   In the ROS Settings window, the `ROS IP Address` should be the IP address of your ROS machine

   - Find the IP address of your ROS machine. In Ubuntu, open a terminal window, and enter `hostname -I`.

   - Replace the `ROS IP Address` value with the IP address of your ROS machine. Ensure that the `Host Port` is set to `10000`.

   ![](Image/2_settings.png)

## Usage
**ROS Side** :
1. Open a new terminal window in the ROS workspace, source the workspace.
```bash
cd ROS
source devel/setup.bash
```
2. Then, run the following roslaunch in order to start roscore, set the ROS parameters, start the server endpoint, start the Mover Service node, and launch MoveIt.
```bash
roslaunch ur10e_rg2_moveit TrajectoryPlanner.launch
```
3. This launch will print various messages to the console, including the set parameters and the nodes launched. The final two messages should confirm `You can start planning now!` and `Ready to plan`.

**Unity Side** :
1. If the PickAndPlaceProject Unity project is not already open, select and open it from the Unity Hub.
2. Press the Play button at the top of the Unity Editor to enter Play Mode. If everything imported correctly, no errors should appear in the Console window. The robot arm should stay “mounted” to the table, and nothing should fall through the floor.
3. Press the UI Button Publish to send the joint configurations to ROS, and watch the robot arm pick up and place the cube!

## Expected Outcome

  ![](Image/expected_outcome.gif)

## Project Structure
```
ur10e_rg2_PickAndPlace/
├── ROS/                           # ROS workspace for robot simulation and control
│   ├── src/                       # Source directory containing ROS packages
│   │   ├── moveit_msgs            # Messages and services for MoveIt
│   │   ├── ros_industrial_cmake_boilerplate # CMake utilities for ROS industrial packages
│   │   ├── ros_tcp_endpoint       # ROS-TCP bridge for Unity integration
│   │   ├── stomp                  # STOMP motion planner core package
│   │   ├── stomp_ros              # ROS interface for the STOMP motion planner
│   │   ├── ur10e_rg2_moveit/      # Custom MoveIt package for UR10e with RG2 gripper
│   │   │   ├── config/            # Configuration files for MoveIt, planning, and robot setup
│   │   │   ├── launch/            # Launch files for starting the MoveIt application
│   │   │   ├── msg/               # Custom messages
│   │   │   ├── scripts/           # Python scripts for additional utilities
│   │   │   │   ├── mover.py       # Python scripts for motion planning
│   │   │   ├── srv/               # Custom services
│   │   ├── ur10e_rg2_urdf/        # Package containing the URDF and meshes for the robot
│   │   │   ├── urdf/              # Robot description files in URDF format
│   │   │   │   ├── ur10e_with_rg2.urdf  # Combined UR10e robot with RG2 gripper model
│   │   │   ├── meshes/            # Mesh files for robot visualization and collision
├── UnityProject/                  # Unity project for robot visualization and control
│   ├── Assets/                    # Main asset folder in Unity
│   │   ├── Scripts/               # C# scripts for robot control and planning
│   │   │   ├── SourceDestinationPublisher.cs  # Publishes source and destination coordinates to ROS
│   │   │   ├── TrajectoryPlanner.cs           # Handles planned trajectory and controls robot
│   │   ├── URDF/                  # URDF files imported into Unity for robot visualization
│   │   │   ├── ur10e_with_rg2.urdf  # URDF file for the UR10e robot with RG2 gripper
│   ├── Library/                   # Unity's library folder (auto-generated, do not edit)
│   ├── Logs/                      # Unity log files
│   ├── Packages/                  # Unity project packages
│   ├── ProjectSettings/           # Unity project settings
│   ├── UserSettings/              # Unity user-specific settings
├── README.md                      # Project documentation
├── Image                          # Image folder for documentation
```
