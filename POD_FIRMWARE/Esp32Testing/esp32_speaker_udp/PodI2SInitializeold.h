// PodI2SInitialize.h

#include <esp_WiFi.h>
#include "driver/i2s.h"
#include <WiFi.h>
#include "PodWiFiManager.h"
#include "UDPResponder.h"

#ifndef _PODI2SINITIALIZE_h
#define _PODI2SINITIALIZE_h
#if defined(ARDUINO) && ARDUINO >= 100
	#include "arduino.h"
#else
	#include "WProgram.h"
#endif

#define I2S_SPEAKER_SERIAL_CLOCK GPIO_NUM_16
#define I2S_SPEAKER_LEFT_RIGHT_CLOCK GPIO_NUM_17
#define I2S_SPEAKER_SERIAL_DATA GPIO_NUM_4

//was 26,25,33
#define I2S_MIC_SERIAL_CLOCK GPIO_NUM_19
#define I2S_MIC_LEFT_RIGHT_CLOCK GPIO_NUM_18
#define I2S_MIC_SERIAL_DATA GPIO_NUM_5



//***********Variables******************************




size_t pollSpkrUDPListener();

void PollUDPForSpkr();

void PollMic();

void AudioInitialize();

#endif



