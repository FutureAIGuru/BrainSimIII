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
	void setupIncomingPackets();
private:
	int getInputPacket(char* buffer, int maxSize);
	char* getNextParam();
	long getParamValue(char* param);
	char getParamCode(char* param);
};
	void configurePod();
#endif
