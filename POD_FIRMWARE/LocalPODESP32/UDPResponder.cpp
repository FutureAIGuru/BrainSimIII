// 
// 
// 
#include "PodWiFiManager.h"
#include "UDPResponder.h"
#include "HandleLed.h"
#include <esp_WiFi.h>
#include <WiFiUdp.h>
#include "NVS_access.h"
#include "OTA_Updater.h"
#include "PodWiFiManager.h"
#include "HandleSensors.h"

WiFiUDP udp;

const size_t spkrbufferSize = 255;
char buffer[(int)spkrbufferSize];
IPAddress remoteServer;
IPAddress blankIP = IPAddress(0,0,0,0);
IPAddress bsimremoteServer = blankIP;

uint16_t remoteServerPort = 3333;


String brainSimIPAddress = "";
const int udpPort = 666;
String machine_Name = "";
esp_err_t err;

void clearBuffer() {
	for (int i = 0; i < spkrbufferSize; i++) {
		buffer[i] = 0;
	}
	return;
}

void WifiPktReceived(void* pkt, wifi_promiscuous_pkt_type_t pktType) {
	checkUDPAndRespond();
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
		numberOfBytesRead = udp.read(buffer, spkrbufferSize);
		remoteServer = udp.remoteIP();
	}
	return numberOfBytesRead;
}

void checkUDPAndRespond() {
	if (pollUDPListener() != 0) {
		Serial.println(buffer);
		if (strcmp(buffer, "DevicePoll") == 0) {
			if (bsimremoteServer == blankIP || bsimremoteServer == remoteServer) {
				udp.beginPacket(remoteServer, remoteServerPort);			
			
				machine_Name = readCredentialsFromNvs(machine_Name);
				if (machine_Name == "SalliePod") {
					Serial.println("Machine name is empty, using temp name!");
				}
				else {
					Serial.print("Machine name has been set to: ");
					Serial.println(machine_Name);
				}
				Serial.println(machine_Name);

				udp.write((uint8_t*) machine_Name.c_str(), strlen(machine_Name.c_str()));
				udp.endPacket();			
			}
		}
		if (strcmp(buffer, "Pair") == 0)
		{
			brainSimIPAddress = udp.remoteIP().toString();
			bsimremoteServer = udp.remoteIP();
			l.setValue('r', 50);
			l.setValue('g', 0);
			l.setValue('b', 50);
			l.show();
			sensorArray[4].setValue('r', 1); //resetYaw on pair
			sensorArray[0].setValue('r', 1);
			sensorArray[2].setValue('r', 1);
			Serial.print("Pair ");
			Serial.println(brainSimIPAddress);
		}
		if (strcmp(buffer, "Reset") == 0) {
			if (brainSimIPAddress == udp.remoteIP().toString()) {
				pinMode(0, INPUT);
				pinMode(2, INPUT);
				pinMode(4, INPUT);
				pinMode(15, INPUT);
				pinMode(5, INPUT);				
				sensorArray[4].setValue('r', 111);
				ESP.restart();
			}
			else {
				Serial.println("No reset for you");
			}
		}
		if (strcmp(buffer, "Disconnect") == 0) {
			IPAddress n(0,0,0,0);
			bsimremoteServer = n;
			brainSimIPAddress = "";
		}
		if (strncmp(buffer, "Rename ", 7) == 0) {
			strtok(buffer, " ");
			String newName = strtok(NULL, " \n");
			machine_Name = writeCredentialsToNvs("SalliePod "+newName);
			Serial.print("New name is ");
			Serial.println(machine_Name);
		}
		if (strcmp(buffer, "NewNetwork") == 0) {
			l.setValue('r',100);
			l.setValue('b', 0);
			l.setValue('g', 0);
			l.show();
			WiFiSetup(true);
		}
		if (strcmp(buffer, "OTA_TRIGGER") == 0) {		
			otaToNvs("OTA_ON");
			ESP.restart();			
		}
		clearBuffer();
	}
}



