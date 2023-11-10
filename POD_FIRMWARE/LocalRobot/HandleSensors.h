// HandleSensors.h

#ifndef _HandleSensors_h
#define _HandleSensors_h

#if defined(ARDUINO) && ARDUINO >= 100
	#include "arduino.h"
#else
	#include "WProgram.h"
#endif

#define MAX_SENSORS 14

enum SensorType { none, servoPosition, motorPosition, motorRate, mpuSensor, analogSensor };
class Sensor {
public:
	int index = 0; //so you know the array index without looking
	SensorType sType = none; //'x'
	bool enabled = false;    //'e'
	int pinNumber = -1; // 'p' also the servonumber if its a servoposition type;
	int pollingPeriod = 100;; //'t' milliseconds -- how often to check the sensor value internally
	int reportingPeriod = 1000; //'T' milliseconds -- how often to send the sensor value to the brain if no alert
	int minChange = -10000; //'m' any value change less than this is considered no change
	int maxChange = 1000; //'M' any value change greater than this sends an alert message regardless of the timer
	unsigned long lastPolled = 0;
	unsigned long lastReported = 0;
	int lastValue = 0;
	long currentValue = 0;
	long  lastPosition = 0; //used only for rate calculations

	void pollSensorValue();
	void reportSensorValue();
	void setValue(char code, int value);
	String ToString();
};

extern Sensor sensorArray[MAX_SENSORS];
extern const char* boolTypeName[];


#endif


void setupSensors();

void handleSensors();

void sendSensorValue(int sensorNum, int sensorValue);
