// HandleServos.h

#ifndef _HandleActuators_h
#define _HandleActuators_h

#if defined(ARDUINO) && ARDUINO >= 100
	#include "arduino.h"
#else
	#include "WProgram.h"
#endif

#include <ESP32Servo.h>


#define MAX_ACTUATORS 6

enum ActuatorType {motor,servo};
class Actuator {
public:
	int index=-1;
	ActuatorType aType = motor;     //'x'
	float currentPosition = 90;	//degrees
	int targetPosition = 90;		//'T' degrees
	unsigned long targetTime = 1000;			//'t'

	bool isPinAttached = false;

	int sensor = -1;				//associated sensor (if any)
	float rate = 0;				    // steps/milli

	Servo s;			//pwm driver

	bool IsEnabled() { return enabled; };

	unsigned long lastMoved = millis();
	
protected:
	bool enabled = false;			//'e' enabled / attached
	int minValue = 0;		//'m' don't go below this value
	int maxValue = 180;		//'M' don't go above this value

public:
	long pinNumber = -1;			//'p'
	virtual void updateActuatorValue(int elapsedMS);
	virtual bool setValue(char code, long value);
	void configureActuator(int pin, int type, int tTime, bool enableStatus);
	void setupMotorPins();
	String getActuatorConfiguration();
	int Pin0() { return pinNumber % 100; }
	int Pin1() { return (pinNumber/100) % 100; }
	int Pin2() { return (pinNumber / 10000) % 100; }
};

class ServoActuator : public Actuator {
public:
	bool moving = false;
	void detachPins();
	virtual void updateActuatorValue(int elapsedMS);
	bool setValue(char code, long value);
};

class MotorActuator : public Actuator {
public:
	enum MotorControlMode {raw,rate,distance,infini};
	MotorControlMode motorControlMode = rate;

	float integrationSum = 0; //for PID controller
	float previousError = 0;
	int distanceToTravel = 0; //used for motors only
	int rateSensorPin = -1;
	int errorMargin = 60;
	bool movedFlag = false;
	unsigned long timeHandle = 0;
	bool toggleInfini = false;
	float runCounter = 0;
	float previousSpeedToHit = 0;
	float KP = 0.25; float  KI = 0.1; float  KD = 0.01;	

	void detachMotorPins();
	void updateActuatorValue(int elapsedMS);
	bool setValue(char code, long value);
	long targetDistance = 0;
	int targetDir = 0;
	void setMotorSpeed(int value);
	void enableMotor(bool enable);
};

extern Actuator * actuators[MAX_ACTUATORS];

void setupActuators();

void handleActuators();

#endif
