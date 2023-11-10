// NVS_access.h
#include "nvs.h"
#include "nvs_flash.h"
#ifndef _NVS_ACCESS_h
#define _NVS_ACCESS_h

#if defined(ARDUINO) && ARDUINO >= 100
	#include "arduino.h"
#else
	#include "WProgram.h"
#endif


String readNamingCredentialsFromNvs(String& machineName);
String writeNamingCredentialsToNvs(String machineName);
bool otaToNvs(String otaStatus);
bool otaToFlag(String otaStatus);
String readOTAStatus();
#endif

