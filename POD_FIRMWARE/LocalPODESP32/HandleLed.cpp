#include "HandleLed.h"
#include <FastLED.h>



#ifdef LED_ON_MODE

Led l;
//Displayer ledStrip;
void Led::show() {
	this->leds[ledIndex] = CRGB(rvalue, gvalue, bvalue);
	FastLED.show();
}

int Led::getIndex() {
	return ledIndex;
}
int Led::getNumLeds() {
	return numLeds;
}

void Led::setIndex(int index) {
	ledIndex = index;
}

bool Led::setValue(char code, long value) {
	bool handled = false;

	switch (code) {
	case 'r':
	{
		this->rvalue = value;		
		handled = true;
		break;
	}
	case 'b':
	{
		this->bvalue = value;		
		handled = true;
		break;
	}
	case 'g':
	{
		this->gvalue = value;		
		handled = true;
		break;
	}	
	default:
		break;
	}
	return handled;
}

//void Led::setLeds(unsigned int ledIndex){
//	if (ledIndex >= MAX_LEDS) ledIndex = MAX_LEDS-1;
//	leds[ledIndex] = CRGB(this->rvalue, this->gvalue, this->bvalue);
//	return;
//}

#endif // LED_ON_MODE