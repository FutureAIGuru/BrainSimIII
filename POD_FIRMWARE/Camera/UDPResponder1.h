 // UDPResponder.h

#ifndef _UDPResponder_h
#define _UDPResponder_h

#if defined(ARDUINO) && ARDUINO >= 100
	#include "arduino.h"
#else
	#include "WProgram.h"
#endif

void SetupUDPResponder();
void checkUDPAndRespond();

extern String brainSimIPAddress;
extern const int udpPort;


#endif

