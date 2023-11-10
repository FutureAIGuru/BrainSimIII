#include "PodI2SInitialize.h"
#include <Arduino.h>


void setup() {
	Serial.begin(115200);
	
	Serial.println("Starting");

	//This sets up the audioSystem
	AudioInitialize();

	
}
//########################EndSetup####################

void loop() {
	PollUDPForSpkr();
	PollMic();
}
