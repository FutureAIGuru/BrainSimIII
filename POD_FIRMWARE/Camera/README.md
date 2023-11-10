This is code to use an ESP32CAM module with the BrainSimulator's RobotCamera module. 
It is a modification of the library code with comes with the ESP32CAM. It initializes an HTTP responder at its root  IP address. Whenever this address is accessed, it reponds with a JPG of the current camera input. This is currently coded as 240x240.
Also, there is bluetooth functionality which allows connection from the "Bluetooth Serial" phone app to configure the WiFi login.
Also, there is a small UDP responder on port 3333. When it receives a packet containing the string "DevicePoll" it responds with the packet "Camera". The BrainSim module broadcasts the "DevicePoll" message and receives the "Camera" message in order to determine the IP address(es) of the Camera(s).
