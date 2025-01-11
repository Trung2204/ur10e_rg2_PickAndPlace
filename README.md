# UR10e Robot Arm with RG2 Gripper Pick and Place Project

## Overview

This project is a part of a module taught at Vietnamese-German University (VGU), instructed by Dr. Dong Quang Huan. The goal of this project is to adapt the pick and place tutorial from the [Unity Robotics Hub](https://github.com/Unity-Technologies/Unity-Robotics-Hub/tree/main/tutorials/pick_and_place) for the UR10e robot arm integrated with the OnRobot RG2 gripper. The objective is to automate the process of picking an object from inside a 3D printer and placing it at a desired location in a simulated environment using Unity. The project utilizes the Stochastic Trajectory Optimization for Motion Planning (STOMP) planner for motion planning.

## Table of Contents

- [Overview](#overview)
- [Installation](#installation)
  - [Prerequisites](#prerequisites)
  - [Clone the Repository](#clone-the-repository)
  - [Set Up ROS Workspace](#set-up-ros-workspace)
  - [Configure Unity Project](#configure-unity-project)
- [Usage](#usage)
- [Project Structure](#project-structure)
- [Contributing](#contributing)
- [License](#license)

## Installation

To set up this project, follow these steps:

### Prerequisites

Ensure you have the following software installed:

- [ROS Noetic](http://wiki.ros.org/noetic/Installation/Ubuntu)
- [MoveIt](https://moveit.ros.org/install/)
- [Unity hub](https://unity.com/download)
- [Unity 2020.3.11f1 (LTS)](https://unity.com/releases/editor/archive)
-[3D PRINTER](https://sketchfab.com/3d-models/2-makerbot-10e13be074dd4d55a97b129c9b4d1959)
-[sketchfab plugin](https://github.com/sketchfab/unity-plugin/releases)
### Clone the Repository

```bash
git clone https://github.com/Trung2204/ur10e_rg2_PickAndPlace.git
cd ur10e_rg2_PickAndPlace
```

### Set Up ROS Workspace

1. The provided files require the following packages to be installed. ROS Melodic users should run the following commands if the packages are not already present:

```bash
sudo apt-get update && sudo apt-get upgrade
sudo apt-get install python3-pip ros-noetic-robot-state-publisher ros-noetic-moveit ros-noetic-rosbridge-suite ros-noetic-joy ros-noetic-ros-control ros-noetic-ros-controllers
sudo -H pip3 install rospkg jsonpickle
```

2. If you have not already built and sourced the ROS workspace since importing the new ROS packages, navigate to your ROS workplace, and do it:

```bash
cd ROS
catkin_make
source devel/setup.bash
roslaunch ur10e_rg2_moveit test_TrajectoryPlanner.launch

```

Ensure there are no errors. The ROS workspace is now ready to accept commands!

### Configure Unity Project

1. Open Unity Hub and go to the "Projects" tab, click the "Add" button, and navigate to and select the UnityProject directory within this cloned repository (`/PATH/TO/ur10e_rg2_PickAndPlace/UnityProject/`) to add the project to your Hub.

2. Click the newly added project to open it.

3. In the Unity Project window, double click to open the `Assets/Scenes/EmptyScene` scene if it is not already open.
4. Drag and drop SketchfabForUnity-v1.2.1.unitypackage file to the Asset panel  .

4. Set up your corresponding path in your PC based on your cloned project , build message path
![Build msg](Image/image.png)
5. Change your IP in ROS Settings 
```
#copy into ROS Settings
hostname -I
```


## Usage

Follow these steps to run the simulation:
// TODO: Add usage

## Project Structure

```
ur10e_rg2_PickAndPlace/
├── ROS/
│   ├── build/
│   ├── devel/
│   ├── src/
│   │   ├── ur10e_rg2_moveit/
│   │   │   ├── CMakeLists.txt
│   │   │   ├── package.xml
│   │   │   ├── config/
│   │   │   ├── launch/
│   │   ├── ur10e_rg2_urdf/
│   │   │   ├── CMakeLists.txt
│   │   │   ├── package.xml
│   │   │   ├── urdf/
│   │   │   ├── meshes/
├── UnityProject/
│   ├── Assets/
│   ├── Library/
│   ├── Logs/
│   ├── Packages/
│   ├── ProjectSettings/
│   ├── UserSettings/
├── README.md
```
