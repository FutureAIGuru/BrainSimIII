
#ifndef _HandleLed_h
#define _HandleLed_h

#include <FastLED.h>

#if defined(ARDUINO) && ARDUINO >= 100
#include "arduino.h"
#else
#include "WProgram.h"
#endif

#define MAX_LEDS 1
#define LED_PIN 0

#define LED_ON_MODE 1

#ifdef LED_ON_MODE





class Led {
	
	private:
		int ledIndex = 0;
		int numLeds = MAX_LEDS;						

	public:		
		int rvalue = 0;
		int bvalue = 0;
		int gvalue = 0;
		CRGB leds[MAX_LEDS];
		//void setupDisplays();
		Led() {

			Serial.println("Led initializing");
			
			FastLED.addLeds<WS2812B, GPIO_NUM_0, GRB>(leds, MAX_LEDS);
			return;
		}

		//void setLeds(unsigned int ledIndex);

		bool setValue(char code, long value);

		void show();

		void setIndex(int index);
		int getIndex();
		int getNumLeds();
};

extern Led l;

#endif

#endif // LED_ON_MODE