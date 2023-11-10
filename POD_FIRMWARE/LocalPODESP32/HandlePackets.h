// HandlePackets.h

#ifndef _HandlePackets_h
#define _HandlePackets_h

#if defined(ARDUINO) && ARDUINO >= 100
#include "arduino.h"
#else
#include "WProgram.h"
#endif
class Incoming {
public:	
	void handleIncomingPackets();
	void setupWiFiPacketHandler();
	void sendInitializationStatus();
	void setupTCP();
	void CheckKeepAlive(unsigned long keepAlive, unsigned int keepAliveOffset);
	void CheckTCP();
	void SetupUDPResponder();
	void sendPacket(char* pack);
	void sendPacket(String pack);
	int recPacket();
	void reportConfiguration();
	void resetYawFromUDP();

private:
	int getInputPacket(char* buffer, int maxSize);
	char* getNextParam();
	long getParamValue(char* param);
	char getParamCode(char* param);	
};
static Incoming input;
void configurePod();

#endif
