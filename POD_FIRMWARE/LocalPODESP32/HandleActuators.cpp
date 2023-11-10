// 
// 
// 
#include <cmath>
#include "HandleActuators.h"
#include "HandleSensors.h"
//#include "analogWrite.h"

#define ACTUATOR_UPDATE_INTERVAL 25  //update motor speed at 50 hz

Actuator* actuators[MAX_ACTUATORS];

const char* actuatorTypeName[] = { "motor", "servo" };

const int servoMinValue = 1300;
const int servoMaxValue = 7500;

bool firstPassFlag = false;

void setupActuators() {
	//initialize the array
	for (int i = 0; i < MAX_ACTUATORS; i++) {//this setup for motors/servos as seperate classes is janky
		if (i < 2) {
			actuators[i] = new MotorActuator();
			actuators[i]->index = i;			
		}
		else {
			actuators[i] = new ServoActuator();
			actuators[i]->index = i;		
		}
	}
}

void handleActuators() {
	//update actuator values 
	if (firstPassFlag == false) {
		firstPassFlag = true;
		actuators[0]->setValue('T', 90);
		actuators[1]->setValue('T', 90);
	}
	unsigned long currentTime = millis();
	for (int i = 0; i < MAX_ACTUATORS; i++)
	{
		Actuator* theActuator = actuators[i];
		int elapsedMS = currentTime - theActuator->lastMoved;
		if (theActuator->IsEnabled() && elapsedMS >= ACTUATOR_UPDATE_INTERVAL) {
			theActuator->updateActuatorValue(elapsedMS);
		}
	}
}

void Actuator::setupMotorPins() {	
	if (this->aType == motor) {		
		pinMode(Pin0(), OUTPUT);
		digitalWrite(Pin0(), LOW);
		pinMode(Pin1(), OUTPUT);
		digitalWrite(Pin1(), LOW);
		pinMode(Pin2(), OUTPUT);
		ledcAttachPin(Pin2(), index);
		ledcSetup(index, 100, 8);
		ledcWrite(index,0);
		isPinAttached = true;		
	}
	else if (this->aType == servo) {		
		ledcAttachPin(pinNumber, index);
		ledcSetup(index, 50, 16);
		ledcWrite(index, 0);
		isPinAttached = true;
	}
}

void ServoActuator::detachPins() {
	ledcDetachPin(pinNumber);
	isPinAttached = false;
}

void Actuator::configureActuator(int pin, int type, int tTime, bool enableStatus) {
	this->aType = (ActuatorType)type;	
	this->pinNumber = pin;	
	if (!isPinAttached) {
		this->setupMotorPins();
	}
	this->targetTime = tTime;	
	this->enabled = enableStatus;	
}
void Actuator::updateActuatorValue(int elapsedMS) {
}
bool Actuator::setValue(char code, long value) {
	bool handled = false;
	switch (code) {
	case 'm':
		minValue = value;
		if (targetPosition < minValue)targetPosition = minValue;
		if (currentPosition < minValue)currentPosition = minValue;
		handled = true;
		break;
	case 'M':
		maxValue = value;
		if (targetPosition > maxValue)targetPosition = maxValue;
		if (currentPosition > maxValue)currentPosition = maxValue;
		handled = true;
		break;
	case 't': //time to reach target
		targetTime = value;
		handled = true;
		break;
	}
	return handled;
}

String Actuator::getActuatorConfiguration() {
	String s = "Actuator ";
	s = s + this->index + " Type " + this->aType + " Enabled " + (int)IsEnabled() + " targetPosition " + this->targetPosition + " currentPosition " + this->currentPosition + " targetTime " + this->targetTime + " minValue " + this->minValue + " maxValue " + this->maxValue + "\n";
	return s;
}


/******************************************************
Special Stuff for Servos
*******************************************************/

long convertForServo(long angle) {	
	if (angle > 180) angle = 180;
	if (angle < 0) angle = 0;
	int retVal = ((angle / 180.0f) * (servoMaxValue - servoMinValue)) + servoMinValue;
	/*Serial.print("retVal: ");
	Serial.println(retVal);*/
	return retVal;
}

void ServoActuator::updateActuatorValue(int elapsedMS) {
	lastMoved = millis();
	if (moving &&
		(currentPosition <= targetPosition && rate < 0 ||
			currentPosition >= targetPosition && rate > 0)) {
		currentPosition = targetPosition;
		lastMoved = millis();
		//s.write((int)currentPosition);
		ledcWrite(index, convertForServo((int)currentPosition));
		
		Serial.print("A"); Serial.print(index); Serial.print(":"); Serial.println("done");
		moving = false;
	}
	if (targetPosition != currentPosition) {
		currentPosition += (float)elapsedMS * rate;
		lastMoved = millis();
		ledcWrite(index, convertForServo((int)currentPosition));
		//s.write((int)currentPosition);	
		//Serial.print(pinNumber); Serial.print(":"); Serial.println(currentPosition);
	}
};
bool ServoActuator::setValue(char code, long value) {
	bool handled = Actuator::setValue(code, value);
	switch (code)
	{
	case 'e':
		enabled = value;
		handled = true;
		break;
	case 'T': //the Target position
		targetPosition = value;
		if (targetTime > 0) {
			rate = (float)(targetPosition - currentPosition) / (float)targetTime;
		}
		else {
			if (isPinAttached == false) return false;
			//s.write(targetPosition);
			ledcWrite(index, convertForServo(targetPosition));
			currentPosition = targetPosition;
			//Serial.print("init servo:"); Serial.println(targetPosition);
		}
		lastMoved = millis();
		moving = true;
		handled = true;
		break;
	}
	return handled;
}

/******************************************************
Special Stuff for Motors
*******************************************************/

/*
* For the L298N controller which requires 3 pins per motor:
* the Pin Number is express in the config string as pxxyyzz
* where:
* xx is the PWM pin (a leading 0 is optional)
* yy is the first enable pin (a leading 0 is required for a 1-digit pin#)
* zz is the second enable pin (a leading 0 is required for a 1-digit pin#)
*
* So if the pwm pin is 3, and the enable pings are 54 & 55 this wout be p035455
* Note that defining the same motor as p035554 will make it run in the opposite direction
*/

bool MotorActuator::setValue(char code, long value) {
	bool handled = Actuator::setValue(code, value);
	switch (code)
	{
	case 'T': //the Target value		
		targetPosition = value - 90;
		//if you're in raw mode, everything happens right here...update does nothing
		if (value == 90) {			
			setMotorSpeed(90);
			integrationSum = 0;
			previousError = 0;
		}
		handled = true;
		break;
	case 'e':
		enableMotor((bool)value);
		enabled = value;
		handled = true;
		break;
	}
	return handled;
}
void MotorActuator::detachMotorPins() {
	if (!enabled) {
		Serial.println("Disabled");
		//analogWrite(Pin2(), 0, 500);

		ledcDetachPin(Pin2());
		isPinAttached = false;
		pinMode(Pin2(), INPUT);
		digitalWrite(Pin1(), LOW);
		pinMode(Pin1(), INPUT);
		digitalWrite(Pin0(), LOW);
		pinMode(Pin0(), INPUT);
	}
}

void MotorActuator::enableMotor(bool enable) {//deprecating this as its only used for the sabertooth controller
	enabled = enable;
	if (Pin1() == 0) {//there is only a single pin number, this must be a sabertooth controller
		if (!enable) {
			if (s.attached()) {
				s.detach();
			}
			Serial.print(" disablingMotor: "), Serial.println(index);
		}
		else {
			if (!s.attached()) {
				s.attach(pinNumber, 1000, 2100);
				isPinAttached = true;
				Serial.print(" Attaching Motor: "), Serial.println(index);
			}
		}
	}
}
void MotorActuator::setMotorSpeed(int speed)
{
	if (isPinAttached == false) return;
	//Serial.print("Index: "); Serial.print(index); Serial.print(" PinNumber:"); Serial.print(pinNumber); Serial.print(" Pin0:"); Serial.print(Pin0()); Serial.print(" Pin1:"); Serial.print(Pin1()); Serial.print(" Pin2:"); Serial.println(Pin2());
	if (Pin1() == 0) {//there is only a single pin number, this must be a sabertooth controller
		//analogWrite(Pin2(), (int)speed);
		ledcWrite(index, (speed));
	}
	else {
		//figure out the direction so we can disable/enable the correct pins
		int dir = 0; //disable=0, fwd=1 rev=-1
		if (speed > 105) dir = 1;
		if (speed < 75) dir = -1;

		/*Serial.print("Rate: ");
		Serial.println(sensorArray[index+1].currentValue);*/

		//convert speed from range [0,180] to [0,255]
		long absSpeed = abs(90-speed);
		

		absSpeed *= (255);
		absSpeed /= 90;		

		ledcWrite(index, (int)(absSpeed));
		//analogWrite(Pin2(), (int)absSpeed);
		//Serial.print(" Setting Motor Speed: "), Serial.print(speed);
		//Serial.print("Abs Speed: "); Serial.println(absSpeed);
		//Serial.print("index"); Serial.println(index);
		//Serial.print("Speed: "); Serial.println(speed);
		//Serial.print(" dir: "), Serial.println(dir);

		//set the digital pins to control the direction
		if (dir == 0) {
			digitalWrite(Pin0(), LOW);
			digitalWrite(Pin1(), LOW);
		}
		else if (dir == 1) {
			digitalWrite(Pin0(), HIGH);
			digitalWrite(Pin1(), LOW);
		}
		else if (dir == -1) {
			digitalWrite(Pin0(), LOW);
			digitalWrite(Pin1(), HIGH);
		}
	}
}
void MotorActuator::updateActuatorValue(int elapsedMS) {
	lastMoved = millis();
	//Serial.println(motorControlMode);

	if (MotorActuator::rate == motorControlMode) {

		//******************************************///
		//PID controller implemented here
		//PID constants are
		//Serial.println("Rate called");		
//		KP = 0.25; float  KI = 0.1; float  KD = 0.01;
		KP = 0.3; KI = 0.1; KD = 0.1;
		int rateSensorPin = 1; //TODO fix this to read the correct sensor
		//get the current rate from the sensor

		if (this->index > 0) {
			rateSensorPin = 3;
		}

		int currentSpeed = (sensorArray[rateSensorPin].currentValue);		
		int currentError = (currentSpeed - targetPosition);

		// input to the pid setup is the current_error
		//currentError = currentSpeed - targetPosition;
		integrationSum += currentError;
		float newValue = -(KP * currentError + KI * integrationSum + KD * (currentError - previousError));
		newValue += 90;

		if (newValue < 0) newValue = 0;
		if (newValue > 180) newValue = 180;
		//Serial.printf("chan:%i newVal:%.4f err:%i curSpd: %i Target: %i\n",index,newValue,currentError,currentSpeed, targetPosition);
		setMotorSpeed((int)newValue);
		previousError = currentError;
		return;
	}

	else {		
		Serial.println("No motor control mode set");		
	}
}