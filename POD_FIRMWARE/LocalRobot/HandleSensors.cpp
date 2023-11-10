// 
// 
// 
//see https://github.com/GreyGnome/PinChangeInt
//#include "PinChangeInt.h"

#include "HandleSensors.h"
#include "HandleActuators.h"
#include "Wire.h"



Sensor sensorArray[MAX_SENSORS];
const char* sensorTypeName[] = { "none", "servoPosition", "motorPosition","motorRate", "mpuSensor", "analogSensor" };
const char* boolTypeName[] = { "false", "true" };


//for gyro
//#include "I2Cdev.h"
#include "tinyMPU6050.h"

#define GYRO_SENSOR 31;

//for motor quadrature position/rate sensor
//the ISRs for the 4 sensors
void sensorInput0();
void sensorInput1();
void sensorInput2();
void sensorInput3();


//the pinchangeinterrupt system requires separate ISRs becuase it doesn't report
//which pin caused the interrupt
//This sets up an array of rotary interrupts working down from pin 69
#define ROTARY_SENSOR_COUNT  2
#define LAST_ROTARY_PIN 69

struct rotarySensor {
	volatile byte prevState = 0;
	volatile long position;
};
rotarySensor rotarySensors[ROTARY_SENSOR_COUNT];
typedef void(*genericISR)();
genericISR isrArray[] = { &sensorInput0,&sensorInput1,&sensorInput2,&sensorInput3};


MPU6050 mpu(Wire);
long lastMPUExecuteTime = 0;

void setupSensors() {

	for (int i = 0; i < MAX_SENSORS; i++)
		sensorArray[i].index = i;

	/*Serial.println("MPU Init");
	mpu.Initialize();
	Serial.println("MPU Calibrate");
	mpu.Calibrate();
	Serial.println("MPU complete");*/

	//z = yaw +:right turn
	//y = roll +:right up
	//x = pitch  +:pitch up (front)

	//set the pins and interrupts for the rotary sensor inputs
	for (int i = 0; i < ROTARY_SENSOR_COUNT; i++)
	{
		int pin1 = LAST_ROTARY_PIN - 2 * i;
		int pin2 = LAST_ROTARY_PIN - 2 * i - 1;
		pinMode(pin1, INPUT);
		pinMode(pin2, INPUT);
		//attachPinChangeInterrupt(pin1, *isrArray[i], CHANGE);
		//attachPinChangeInterrupt(pin2, *isrArray[i], CHANGE);
		attachInterrupt(pin1, *isrArray[i], CHANGE);
		attachInterrupt(pin2, *isrArray[i], CHANGE);
	}
}

//used for timing of the loop AND for polling various sensors for exceeding thresholds
int passCounter = 0;
unsigned long lastTime = 0;

void handleSensors() {
	unsigned long currentTime = millis();
	//update sensor outputs
	for (int i = 0; i < MAX_SENSORS; i++)
	{
		Sensor* mySensor = &sensorArray[i];
		if (mySensor->enabled) {
			//is it time to poll?
			unsigned long elapsed = currentTime - mySensor->lastPolled;
			if (elapsed > mySensor->pollingPeriod) {
				mySensor->pollSensorValue();
			}
			//is it time to send?
			elapsed = currentTime - mySensor->lastReported;
			int delta = abs(mySensor->currentValue - mySensor->lastValue);
			if ((delta >= mySensor->maxChange) ||
				(delta > mySensor->minChange && mySensor->reportingPeriod > 0 && elapsed > mySensor->reportingPeriod)) {
				mySensor->reportSensorValue();
			}
		}
	}

	//this is for evaulating the polling loop time
	//if (lastTime == 0) lastTime = millis();
	//passCounter++;
	//if (passCounter == 100000)
	//{
	//	unsigned long elapsed = millis() - lastTime;
	//	lastTime = millis();
	//	passCounter = 0;
	//	Serial.print("100k cycles elapsed: "); Serial.println(elapsed);
	//}
}

void sendSensorValue(int sensorNum, int sensorValue) {
	Serial.print("s"); Serial.print(sensorNum); Serial.print(":"); Serial.println(sensorValue);Serial.print('\n');
}

void Sensor::pollSensorValue() {
	switch (sType) {
	case analogSensor:
	{
		//currentValue = analogRead(pinNumber + A0);
		currentValue = analogRead(pinNumber);
	}
		break;
	case mpuSensor:
		if (millis() - lastMPUExecuteTime > pollingPeriod) {
			mpu.Execute();
			//Serial.println("mpu.Execute()");
			lastMPUExecuteTime = millis();
		}
		switch (pinNumber) {
		case 0: currentValue = mpu.GetAccX(); break;
		case 1: currentValue = mpu.GetAccY(); break;
		case 2: currentValue = mpu.GetAccZ(); break;
		case 3: currentValue = mpu.GetAngX(); break;
		case 4: currentValue = mpu.GetAngY(); break;
		case 5: currentValue = mpu.GetAngZ(); break;
		}
		break;
	case servoPosition:
	{
		currentValue = actuators[pinNumber]->s.read();
	}
		break;
	case motorPosition:
	{
		currentValue = rotarySensors[pinNumber].position;
	}
		break;
	case motorRate:
	{
		long newPosition = sensorArray[pinNumber].currentValue;
		long  delta = newPosition - lastPosition;
		lastPosition = newPosition;
		int elapsed = millis() - lastPolled;
		int rate = delta * 1000 / elapsed;
		currentValue = rate / 90;
	}
		break;
	default:
	{
		Serial.print("def");
	}
		break;
	}
	lastPolled = millis();
}

void Sensor::setValue(char code, int value) {
	//Serial.print(" Sensor setValue: "); Serial.print(code); Serial.println(value);
	switch (code) {
	case 'e':
		enabled = value;
		if (sType == motorPosition) { //when you disable/enable a sensor, set its value to 0
			currentValue = 0;
			noInterrupts();
			rotarySensors[pinNumber].position = 0;
			interrupts();
		}
		if (sType == mpuSensor && value == 1 && pinNumber == 5) {
			mpu.resetZ();
		}
		break;
	case 'r':
		if (sType == motorPosition && value == 1) {
			rotarySensors[pinNumber].position = 0;
		}
		if (sType == mpuSensor && value == 1 && pinNumber == 5) {
			mpu.resetZ();
		}
	case 'm':
		minChange = value;
		break;
	case 'M':
		maxChange = value;
		break;
	case 'p':
		pinNumber = value;
		break;
	case 't':
		pollingPeriod = value;
		break;
	case 'T':
		reportingPeriod = value;
		break;
	case 'x':
		sType = (SensorType)value;
		Serial.print("Setting Sensor type: ");
		Serial.println(value);
		break;
	}
	
}

void Sensor::reportSensorValue() {
	Serial.print("S");
	Serial.print(index);
	Serial.print(":");
	Serial.print(currentValue);
	Serial.print('\n');
	lastValue = currentValue;
	lastReported = millis();
}


String Sensor::ToString() {
	String s = " Sensor: ";
	s = s + index + " Enabled:" + boolTypeName[enabled] + " Type:" + sensorTypeName[sType] + " t" + pollingPeriod + " p" + pinNumber + " m" + minChange + " M" + maxChange;
	return s;
}


void processRotarySensorInterrupt(int i) {
	int pin1 = LAST_ROTARY_PIN - 2 * i;
	int pin2 = LAST_ROTARY_PIN - 2 * i - 1;
	byte p1val = digitalRead(pin1);
	byte p2val = digitalRead(pin2);
	rotarySensor* theSensor = &rotarySensors[i];
	byte state = theSensor->prevState & 3;
	if (p1val) state |= 4;
	if (p2val) state |= 8;
	theSensor->prevState = (state >> 2);
	switch (state) {
	case 1: case 7: case 8: case 14:
		theSensor->position++;
		return;
	case 2: case 4: case 11: case 13:
		theSensor->position--;
		return;
	case 3: case 12:
		theSensor->position += 2;
		return;
	case 6: case 9:
		theSensor->position -= 2;
		return;
	}

}

void sensorInput0() {
	processRotarySensorInterrupt(0);
}
void sensorInput1() {
	processRotarySensorInterrupt(1);
}
void sensorInput2() {
	processRotarySensorInterrupt(2);
}
void sensorInput3() {
	processRotarySensorInterrupt(3);
}

