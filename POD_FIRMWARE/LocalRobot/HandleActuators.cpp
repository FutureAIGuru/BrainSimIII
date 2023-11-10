// 
// 
// 

#include "HandleActuators.h"
#include "HandleSensors.h"

Actuator* actuators[MAX_ACTUATORS];

#define ACTUATOR_UPDATE_INTERVAL 25  //update motor speed at 50 hz

const char* actuatorTypeName[] = { "motor", "servo" };


void setupActuators() {
	//initialize the array
	for (int i = 0; i < MAX_ACTUATORS; i++) {
		actuators[i] = new Actuator();
		actuators[i]->index = i;
	}


}

void handleActuators() {
	//update actuator values 
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
	case 'p':
		pinNumber = value;
		handled = true;
		break;

	case 't': //time to reach target
		targetTime = value;
		handled = true;
		break;
	case 'x':
		ActuatorType at = (ActuatorType)value;
		switch (at)
		{
		case motor:
			actuators[index] = new MotorActuator();
			break;
		case servo:
			actuators[index] = new ServoActuator(); //servo actuator?
			break;
		}
		actuators[index]->index = index;
		Serial.print("Setting Type:"); Serial.println(value);
		actuators[index]->aType = at;
		delete this;
		handled = true;
		break;
	}
	return handled;
}

String Actuator::ToString() {
	String s = "Actuator: ";
	s = s + index + " Enb:" + boolTypeName[enabled] + " Type:" + actuatorTypeName[aType] + " curPos:" + currentPosition + " t" +
		targetTime + " p" + pinNumber + " m" + minValue + " M" + maxValue + " T" + targetPosition;
	return s;
}


/******************************************************
Special Stuff for Servos
*******************************************************/
void ServoActuator::updateActuatorValue(int elapsedMS) {
	lastMoved = millis();
	if (moving &&
		(currentPosition <= targetPosition && rate < 0 ||
			currentPosition >= targetPosition && rate > 0)) {
		currentPosition = targetPosition;
		lastMoved = millis();
		s.write((int)currentPosition);
		Serial.print("A"); Serial.print(index); Serial.print(":"); Serial.println("done");
		moving = false;
	}
	if (targetPosition != currentPosition) {
		currentPosition += (float)elapsedMS * rate;
		lastMoved = millis();
		s.write((int)currentPosition);
		//Serial.print(pinNumber); Serial.print(":"); Serial.println(currentPosition);
	}
};
bool ServoActuator::setValue(char code, long value) {
	bool handled = Actuator::setValue(code, value);
	switch (code)
	{
	case 'e':
		enabled = value;
		if (!value) {
			if (s.attached())
				s.detach();
		}
		else {
			if (!s.attached()) {
				s.attach(pinNumber);
			}
		}
		handled = true;
		break;
	case 'T': //the Target position
		targetPosition = value;
		if (targetTime > 0) {
			rate = (float)(targetPosition - currentPosition) / (float)targetTime;
		}
		else {
			s.write(targetPosition);
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

void MotorActuator::enableMotor(bool enable) {
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
				Serial.print(" Attaching Motor: "), Serial.println(index);
			}
		}
	}
	else {
		if (enable && !enabled) {
			pinMode(Pin0(), OUTPUT);
			pinMode(Pin1(), OUTPUT);
			pinMode(Pin2(), OUTPUT);
		}
		else if (!enable && enabled) {
			analogWrite(Pin2(), 0);
			pinMode(Pin2(), INPUT);
			digitalWrite(Pin1(), LOW);
			pinMode(Pin1(), INPUT);
			digitalWrite(Pin0(), LOW);
			pinMode(Pin0(), INPUT);
		}
	}
}
void MotorActuator::setMotorSpeed(int speed)
{
	//Serial.print("Index: "); Serial.print(index); Serial.print(" PinNumber:"); Serial.print(pinNumber); Serial.print(" Pin0:"); Serial.print(Pin0()); Serial.print(" Pin1:"); Serial.print(Pin1()); Serial.print(" Pin2:"); Serial.println(Pin2());
	if (Pin1() == 0) {//there is only a single pin number, this must be a sabertooth controller
		s.write(speed);
	}
	else {
		//figure out the direction so we can disable/enable the correct pins
		int dir = 0; //disable=0, fwd=1 rev=-1
		if (speed > 95) dir = 1;
		if (speed < 85) dir = -1;

		//convert speed from range [0,180] to [-255,255]
		long absSpeed = abs(speed - 90);
		
		absSpeed *= 255;
		absSpeed /= 90;		

		analogWrite(Pin2(), (int)absSpeed);
		//Serial.print(" Setting Motor Speed: "), Serial.print(speed);
		//Serial.print(" Abs Speed: "), Serial.print(absSpeed);
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

	if (MotorActuator::infini == motorControlMode) {		

		if (movedFlag == false && toggleInfini == true) {
			/*Serial.print("Index: ");
			Serial.print(index);
			Serial.println(" moving");*/
			setMotorSpeed(targetPosition);
			timeHandle = lastMoved;
			movedFlag = true;
		}		

		
		if ((lastMoved > (timeHandle + targetTime))) {
			/*Serial.print("Index: ");
			Serial.print(index);
			Serial.println(" Stopping");*/
			toggleInfini = false;
			targetTime = 0;
			movedFlag = false;			
			setMotorSpeed(90);			
		}		
			return;
	}

	if (MotorActuator::distance == motorControlMode) {
		//******************************************///
		//PID controller implemented here
		//PID constants are
		//Serial.println("Rate called");

		float KP = 0.25; float  KI = 0.1; float  KD = 0.1;
		float kpSum;
		float kiSum;
		float kdSum;
		int rateSensorPin = index + 1; //TODO fix this to read the correct sensor
		//get the current rate from the sensor
		int currentSpeed = sensorArray[rateSensorPin].currentValue;
		int currentError = currentSpeed - targetPosition;
		long currPos = sensorArray[index].currentValue;

		

		if (currPos > targetDistance && targetDir > 0) {
			setMotorSpeed(90);
			integrationSum = 0;
			previousError = 0;
			currentError = 0;
			targetDistance = 0;			
			return;
		}		
		if (currPos < targetDistance && targetDir < 0) {
			setMotorSpeed(90);
			integrationSum = 0;
			previousError = 0;
			currentError = 0;
			targetDistance = 0;
			return;
		}

		// input to the pid setup is the current_error
		currentError = currentSpeed - targetPosition;
		integrationSum += currentError;
		kpSum = KP * currentError;
		kiSum = KI * integrationSum;
		kdSum = KD * (currentError - previousError);

		

		float newValue = KP * currentError + KI * integrationSum + KD * (currentError - previousError);
		newValue += 90;
		
		/*Serial.print("new Value-> ");
		Serial.println(newValue);*/



		
		if (newValue < 0) newValue = 1;
		if (newValue > 180) newValue = 179;



		//Serial.println("rate setSpeed called");
		setMotorSpeed((int)newValue);
		previousError = currentError;
		return;
	}

	if (MotorActuator::rate == motorControlMode) {

		//******************************************///
		//PID controller implemented here
		//PID constants are
		//Serial.println("Rate called");
		
		KP = 0.25; float  KI = 0.1; float  KD = 0.01;
		int rateSensorPin = index + 1; //TODO fix this to read the correct sensor
		//get the current rate from the sensor
		int currentSpeed = sensorArray[rateSensorPin].currentValue;
		int currentError = currentSpeed - targetPosition;

		// input to the pid setup is the current_error
		currentError = currentSpeed - targetPosition;
		integrationSum += currentError;
		float newValue = KP * currentError + KI * integrationSum + KD * (currentError - previousError);
		newValue += 90;

		if (newValue < 0) newValue = 0;
		if (newValue > 180) newValue = 180;
		//Serial.println("rate setSpeed called");
		setMotorSpeed((int)newValue);
		previousError = currentError;
		return;
	}

	if (MotorActuator::raw == motorControlMode) {

		//Serial.println("raw setSpeed called");
		return;
	}

	else {
		Serial.println("No motor control mode set");
	}
}


bool MotorActuator::setValue(char code, long value) {
	//Serial.print("motor SetValue:"); Serial.print(code);
	//Serial.print(":"); Serial.println(value);
	bool handled = Actuator::setValue(code, value);
	switch (code)
	{
	case 'D':
		if (motorControlMode == distance) {			
			if (value > 0) { targetDir = 1; }
			if (value < 0) { targetDir = -1; }
			targetDistance = value + sensorArray[index].currentValue;
		}
		break;
	case 'T': //the Target value
		targetPosition = value - 90;
		//if you're in raw mode, everything happens right here...update does nothing
		if (motorControlMode == raw) {
			setMotorSpeed(value);
		}
		if (motorControlMode == infini) {
			//Serial.println("infini hit");
			toggleInfini = true;
			targetPosition = value;
		}		
		else if (value == 90) {
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
	case 'c':
		MotorControlMode mcm = (MotorControlMode)value;
		motorControlMode = mcm;
		break;
	}
	return handled;
}
