// 
// 
// 

#include "HandlePackets.h"
#include "handleActuators.h"
#include "HandleSensors.h"


#define INPUT_BUFFER_SIZE 80

void Incoming::setupIncomingPackets() {

}

void Incoming::handleIncomingPackets() {
	//get an incoming command 'packet'
	char command[INPUT_BUFFER_SIZE + 1];
	int size = getInputPacket(command, INPUT_BUFFER_SIZE);

	//has a command been received?
	if (size > 0) {
		//Serial.print("RobotRECD:"); Serial.println(command);
				
		//parse the command(s) in the packet
		char* param = strtok(command, " \n");
		while (param != NULL) {

			//command actuator
			if (getParamCode(param) == 'A') {
				int actuatorNum = getParamValue(param);
				if (actuatorNum < 0 || actuatorNum >= MAX_ACTUATORS) break;
				param = getNextParam();
				char paramCode = getParamCode(param);
				
				//continue reading params until you run out or encounter another sensor/actuator
				while (param != NULL && paramCode != 'A' && paramCode != 'S') {
					long value = getParamValue(param);
					//be careful...if the type is changed, the actuator will be reallocated
					actuators[actuatorNum]->setValue(paramCode, value);
					param = getNextParam();
					paramCode = getParamCode(param);
				}
			}

			//Setup sensor 
			else if (getParamCode(param) == 'S') {
				int sensorNum = getParamValue(param);
				if (sensorNum < 0 || sensorNum >= MAX_SENSORS) break;
				Sensor* mySensor = &sensorArray[sensorNum];
				param = getNextParam();
				char paramCode = getParamCode(param);

				//continue reading params until you run out or encounter another sensor/actuator
				while (param != NULL && paramCode != 'A' && paramCode != 'S') {
					long value = getParamValue(param);
					mySensor->setValue(paramCode, value);
					param = getNextParam();
					paramCode = getParamCode(param);
				}
				mySensor->lastReported = millis();
				//Serial.println(mySensor->ToString());
			}
			else{ 
				Serial.print("BadCmd: ");
				Serial.println(param);
				param = getNextParam();
			}
		}
	}
}


char inputBuffer[INPUT_BUFFER_SIZE + 3];
int curInputPtr = 0;

int Incoming::getInputPacket(char* buffer, int maxSize) {
	if (!Serial.available()) return 0;
	char c = Serial.read();
	//Serial.print(c);
	inputBuffer[curInputPtr++] = c;
	inputBuffer[curInputPtr] = 0;
	if (curInputPtr >= maxSize) return -1;	
	if (c != '\n') return 0;	
	inputBuffer[curInputPtr - 1] = 32; //replace the linefeed with a space so strtok will work
	strcpy(buffer, inputBuffer);
	int size = curInputPtr;
	curInputPtr = 0;
	inputBuffer[0] = 0;
	return size;
}

char* Incoming::getNextParam() {
	return strtok(NULL, " ");
}

char Incoming::getParamCode(char* param) {
	return param[0];
}
long Incoming::getParamValue(char* param) {
	long retVal = 0;
	bool numIsNegative = false;
	if (param[0] == 0) return 0;
	param++;
	if (param[0] != 0 && param[0] == '-') {
		numIsNegative = true;
		param++;
	}
	while (param[0] != 0)
	{
		if (!isdigit(param[0]))return -1;
		retVal *= 10;
		retVal += param[0] - '0';
		param++;
	}
	if (numIsNegative) retVal = -retVal;
	return retVal;
}



