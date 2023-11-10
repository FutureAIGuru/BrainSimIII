// 
// 
// 
//see https://github.com/GreyGnome/PinChangeInt
//#include "PinChangeInt.h"

#include "HandleSensors.h"
#include "HandleActuators.h"
#include "HandlePackets.h"

#define G_SDA 21
#define G_SCL 22

Sensor sensorArray[MAX_SENSORS];
const char* sensorTypeName[] = { "none", "servoPosition", "motorPosition","motorRate", "mpuSensor", "analogSensor", "heartBeat"};
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
#define LAST_ROTARY_PIN 35

struct rotarySensor {
	volatile byte prevState = 0;
	volatile long position;
};
rotarySensor rotarySensors[ROTARY_SENSOR_COUNT];
typedef void(*genericISR)();
genericISR isrArray[] = { &sensorInput0,&sensorInput1,&sensorInput2,&sensorInput3};


MPU6050 mpu(Wire);
long lastMPUExecuteTime = 0;
int accelMultiplier = 100;

void setupSensors() {

	for (int i = 0; i < MAX_SENSORS; i++)
	{ 
		sensorArray[i].index = i;
	}		
		
	
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
	Serial.println("MPU Init");
	mpu.Initialize(G_SDA, G_SCL);
	/*Serial.println("MPU Calibrate");
	mpu.Calibrate();
	mpu.Execute();
	Serial.println("MPU complete");*/
}
void secondaryCalibration() {
	Serial.println("MPU Calibrate");
	mpu.Calibrate();
	mpu.Execute();
	Serial.println("MPU complete");
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

void Sensor::configureSensor(int pin, int type, int pollP, int repP, int minC, int maxC) {
	this->pinNumber = pin;
	this->sType = (SensorType)type;
	this->pollingPeriod = pollP;
	this->reportingPeriod = repP;
	this->minChange = minC;
	this->maxChange = maxC;
	if (pinNumber < 4 && (type == 2 || type == 3)) {
		enabled = true;
	}
}

void Sensor::pollSensorValue() {
	switch (sType) {
	case analogSensor:
		//currentValue = analogRead(pinNumber + A0);
		currentValue = analogRead(pinNumber);
		break;
	case mpuSensor:
		if (millis() - lastMPUExecuteTime > pollingPeriod) {
			mpu.Execute();
			//Serial.println("mpu.Execute()");
			lastMPUExecuteTime = millis();
		}
		switch (pinNumber) {
		case 0: 
		{
			int nVal = accelMultiplier* mpu.GetAccX();
			if (abs(nVal) > abs(currentValue)) {
				currentValue = nVal; 
				break;
			}
			break;
		}
		case 1: 
		{
			int nVal = accelMultiplier * mpu.GetAccY();
			if (abs(nVal) > abs(currentValue)) {
				currentValue = nVal; 
				break;
			}
			break;
		}

		case 2: 
		{
			int nVal = accelMultiplier * mpu.GetAccZ();
			if (abs(nVal) > abs(currentValue)) {
				currentValue = nVal; 
				break;
			}
			break;
		}
		case 3: currentValue = mpu.GetAngX(); break;
		case 4: currentValue = mpu.GetAngY(); break;
		case 5: currentValue = -(mpu.GetAngZ()); break;		
		}
		break;
	case servoPosition:
		currentValue = actuators[pinNumber]->s.read();
		break;
	case motorPosition:
		currentValue = rotarySensors[pinNumber].position;
		break;
	case motorRate:
	{
		int rateScale = 42;
		long newPosition = sensorArray[pinNumber].currentValue;
		long  delta = newPosition - lastPosition;
		lastPosition = newPosition;
		int elapsed = millis() - lastPolled;
		int rate = delta * 1000 / elapsed;
		currentValue = rate / rateScale;
		break;
	}		
	case heartBeat:
	{
		if (enabled) {
			//Serial.println("Heartbeat check");
			input.CheckKeepAlive(heartbeatPoll, pollingPeriod);
		}
		break;
	}
	default:
		//Serial.print("def");
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
		if (sType == mpuSensor && value == 1 && pinNumber == 3) {
			mpu.resetX();
		}
		if (sType == mpuSensor && value == 1 && pinNumber == 4) {
			mpu.resetY();
		}
		if (sType == mpuSensor && value == 1 && pinNumber == 5) {
			mpu.resetZ();
		}		
		break;
	case 'r':
		if (sType == motorPosition) {
			rotarySensors[pinNumber].position = 0;
		}
		if (sType == mpuSensor && value == 1 && pinNumber == 5) {
			mpu.resetZ();
		}
		if (sType == mpuSensor && value == 11) {
			mpu.MPU_Hard_Reset();
			mpu.Initialize(G_SDA, G_SCL);
			mpu.Calibrate();
			mpu.Execute();
		}
		if (sType == mpuSensor && value == 111) {
			mpu.MPU_Hard_Reset();
			//mpu.Initialize(G_SDA, G_SCL);
			//mpu.Calibrate();
			//mpu.Execute();
		}
		break;
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
		if (sType == heartBeat) {
			heartbeatPoll = millis();			
		}
		else {
			reportingPeriod = value;
		}
		break;
	case 'x':
		sType = (SensorType)value;
		Serial.print("Setting Sensor type: ");
		Serial.println(value);
		break;
	}
	
}

void Sensor::reportSensorValue() {
	//char* rep = ("S" + index + ":" + currentValue + "\n");
	//char xarr[] = { 'S', '0'+index, ':', '0'+currentValue, '\n'};
	String xarr = "S" + String(index) + ":" + String(currentValue) + "\n";
	if (pinNumber < 3 && (sType==mpuSensor)) {
		currentValue = 0;
	}	
	Serial.println(xarr);
	input.sendPacket(xarr);	
	lastValue = currentValue;
	lastReported = millis();
}


String Sensor::getSensorConfiguration() {
	String s = "Sensor ";
	s = s + index + " Enabled " + enabled + " Type " + sType + " pollingPeriod " + pollingPeriod +" reportingPeriod " + reportingPeriod + " minChange " + minChange + " maxChange " + maxChange + "\n";
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

void IRAM_ATTR sensorInput0() {
	processRotarySensorInterrupt(0);
}
void IRAM_ATTR sensorInput1() {
	processRotarySensorInterrupt(1);
}
void IRAM_ATTR sensorInput2() {
	processRotarySensorInterrupt(2);
}
void IRAM_ATTR sensorInput3() {
	processRotarySensorInterrupt(3);
}

