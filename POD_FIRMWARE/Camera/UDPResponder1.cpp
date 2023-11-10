 // 
// 
// 

#include "UDPResponder1.h"
#include <esp_WiFi.h>
#include <WiFiUdp.h>
#include "NVS_access.h"
#include "PodWiFiManager.h"
#include "CameraJpgHandler.h"

WiFiUDP udp;

const size_t bufferSize = 255;
char buffer[(int)bufferSize];
IPAddress remoteServer;
uint16_t remoteServerPort = 3333;
String machine_Name = "";
String huMsg = "OTA_MODE!";
int cameraCaptureMode = 0;

String brainSimIPAddress = "";
const int udpPort = 666;

void clearBuffer() {
	for (int i = 0; i < bufferSize; i++) {
		buffer[i] = 0;
	}
	return;
}

void WifiPktReceived(void* pkt, wifi_promiscuous_pkt_type_t pktType) {
	checkUDPAndRespond();
}

void swapCameraMode(int mode) {
	cameraCaptureMode = mode;
	if (cameraCaptureMode >= CAM_MODE_COUNT || cameraCaptureMode < 0) {
		cameraCaptureMode = 0;
	}	
	swap_capture_mode();
}

void SetupUDPResponder() {

	esp_wifi_set_promiscuous(true);
	esp_wifi_set_promiscuous_rx_cb(WifiPktReceived);

	udp.begin(remoteServerPort);
	Serial.println("UDP Initialized");
}



size_t pollUDPListener() {
	size_t numberOfBytesRead = 0;
	int byteCount = udp.parsePacket();
	//Serial.print(byteCount);
	if (byteCount > 0) {
		numberOfBytesRead = udp.read(buffer, bufferSize);
		remoteServer = udp.remoteIP();
	}
	return numberOfBytesRead;
}

void checkUDPAndRespond() {
	if (pollUDPListener() != 0) {
		Serial.println(buffer);
		if (strcmp(buffer, "DevicePoll") == 0) {
			udp.beginPacket(remoteServer, remoteServerPort);

			machine_Name = readNamingCredentialsFromNvs(machine_Name);
			if (machine_Name == "Camera") {
				Serial.println("Machine name is empty, using temp name!");
			}
			else {
				Serial.print("Machine name has been set to: ");
				Serial.println(machine_Name);
			}
			Serial.println(machine_Name);

			udp.write((uint8_t*)machine_Name.c_str(), strlen(machine_Name.c_str()));
			udp.endPacket();
		}
		if (strncmp(buffer, "Pair", 4)==0)
		{
			brainSimIPAddress = udp.remoteIP().toString();;
			Serial.print("Pair ");
			Serial.println(brainSimIPAddress);
		}
		if (strncmp(buffer, "Rename ", 7) == 0) {
			strtok(buffer, " ");
			String newName = strtok(NULL, " \n");
			machine_Name = writeNamingCredentialsToNvs("Camera " + newName);
			Serial.print("New name is ");
			Serial.println(machine_Name);
		}
		if (strcmp(buffer, "CamSwap:0") == 0) {
			swapCameraMode(0);
		}
		if (strcmp(buffer, "CamSwap:1") == 0) {
			swapCameraMode(1);
		}
		if (strcmp(buffer, "CamSwap:2") == 0) {
			swapCameraMode(2);
		}
		if (strcmp(buffer, "CamSwap:3") == 0) {
			swapCameraMode(3);
		}
		if (strcmp(buffer, "RepCam") == 0) {
			udp.beginPacket(remoteServer, remoteServerPort);
			udp.write(cameraCaptureMode);
			udp.endPacket();
		}
		if (strcmp(buffer, "OTA_TRIGGER") == 0) {			
			otaToNvs("OTA_ON");			
			ESP.restart();
		}
		clearBuffer();
	}
}



