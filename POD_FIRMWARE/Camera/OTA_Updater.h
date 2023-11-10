// OTA_Updater.h

#ifndef _OTA_UPDATER_h
#define _OTA_UPDATER_h

#if defined(ARDUINO) && ARDUINO >= 100
	#include "arduino.h"
#else
	#include "WProgram.h"
#endif
void OTA_UPDATER();

extern bool ota_flag;
#endif