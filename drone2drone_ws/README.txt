Task

1. test your onnx works and can output actions
2. implement your test code into the swarm_controller.py
3. test the whole controller works for the tellos.

********************************************************
Bugs:
1. cannot install the onnx package
sol:   export CMAKE_ARGS="-DONNX_USE_PROTOBUF_SHARED_LIBS=ON"

roslaunch vrpn_client_ros drone.launch
rostopic list

python '/home/uavg/drone2drone_ws/scripts/multiple_drone_laucher.py'
python3 '/home/uavg/drone2drone_ws/swarm_test.py'
python '/home/uavg/drone2drone_ws/scripts/landTello.py'