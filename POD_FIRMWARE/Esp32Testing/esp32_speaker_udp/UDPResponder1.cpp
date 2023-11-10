 // 
// 
// 
#include "PodWiFiManager.h"
#include "UDPResponder1.h"
#include "HandleLed.h"
#include <esp_WiFi.h>
#include <WiFiUdp.h>

WiFiUDP udp;

const size_t bufferSize = 255;
char buffer[(int)bufferSize];
IPAddress remoteServer;
IPAddress bsimremoteServer;
uint16_t remoteServerPort = 3333;


String brainSimIPAddress = "";
const int udpPort = 666;



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
			char* outMessage = "SalliePod";
			udp.write((uint8_t*)outMessage, strlen(outMessage));
			udp.endPacket();
		}
		if (strncmp(buffer, "Pair", 4)==0)
		{
			brainSimIPAddress = udp.remoteIP().toString();
			bsimremoteServer = udp.remoteIP();
			l.setValue('r', 50);
			l.setValue('g', 50);
			l.show();
			Serial.print("Pair ");
			Serial.println(brainSimIPAddress);
		}
	}
}



