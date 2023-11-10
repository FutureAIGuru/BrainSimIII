#include "HandlePackets.h"
#include "handleActuators.h"
#include "HandleSensors.h"
#include <esp_wifi.h>
#include <WiFi.h>
#include <WiFiUdp.h>
#include "NVS_access.h"
#include "UDPResponder.h"
#include "HandleLed.h"

#define INPUT_BUFFER_SIZE 80
char command[INPUT_BUFFER_SIZE];
int size = 0;
//const int udpPort = 666;
WiFiClient client;

const size_t burnerBufferSize = 255;
char burnerBuffer[(int)burnerBufferSize];

int countOfSensors = 10;
int countOfActuators = 4;

IPAddress remoteServer1;
uint16_t remoteServerPort1;
WiFiUDP burnerUdp;
unsigned long checkTCPHandle = 0;

int checkHandleTimeOffset = 1000;

char* deviceSignature = "Pod";

const uint16_t port = 54321;//temp hard code I will use udp system for ip/port acquisition
char* host = "";
String sensorConfigs[] = {"spin", "stype", "spollP", "srepP", "sminC", "smaxC"};
String actuatorConfigs[] = {"apin", "atype", "atT", "aen"};
String configConfig = "configExists";


void resetcommand() {
	for (int i = 0; i < INPUT_BUFFER_SIZE; i++) {
		command[i] = 0;
	}
}


bool IsIpAddressEqual(IPAddress ip1, IPAddress ip2) {
	for (int i = 0; i < 4; i++) {
		if (ip1[i] != ip2[i]) {
			return false;
		}
	}
	return true;
}

void configFromNVS() {	
	bool senseWinnerFlag = false;
	bool actuatorWinnerFlag = false;
	for (int i = 0; i < countOfSensors; i++) {//getConfigFromNVS will return -1 if the nvs read fails	
		int pinHandle =  getConfigFromNVS(sensorConfigs[0] + i);
		int typeHandle = getConfigFromNVS(sensorConfigs[1] + i);
		int pollHandle = getConfigFromNVS(sensorConfigs[2] + i);
		int repHandle =  getConfigFromNVS(sensorConfigs[3] + i);
		int minHandle =  getConfigFromNVS(sensorConfigs[4] + i);
		int maxHandle =  getConfigFromNVS(sensorConfigs[5] + i);

		if (pinHandle != -1 && typeHandle != -1 && pollHandle != -1 && repHandle != -1 && minHandle != -1 && maxHandle != -1) {		
			if (senseWinnerFlag == false) {
				senseWinnerFlag = true;
			}
			//Serial.println("Sensor "+i);
			sensorArray[i].configureSensor(pinHandle, typeHandle, pollHandle, repHandle, minHandle, maxHandle);
		}
		else {
			senseWinnerFlag = false;
			break;
		}
	}
	for (int x = 0; x < countOfActuators; x++) {//getConfigFromNVS will return -1 if the nvs read fails		
		int pinHandle =    getConfigFromNVS(actuatorConfigs[0] + x);
		int typeHandle =   getConfigFromNVS(actuatorConfigs[1] + x);
		int timeHandle =   getConfigFromNVS(actuatorConfigs[2] + x);
		int enableHandle = getConfigFromNVS(actuatorConfigs[3] + x);

		if (pinHandle != -1 && typeHandle != -1 && timeHandle != -1 && enableHandle != -1) {
			if (actuatorWinnerFlag == false) {
				actuatorWinnerFlag = true;
			}
			//Serial.println("Actuator " + x);
			actuators[x]->configureActuator(pinHandle, typeHandle, timeHandle, enableHandle);
		}
		else {
			actuatorWinnerFlag = false;
			break;
		}
	}
	if (senseWinnerFlag && actuatorWinnerFlag) {
		writeConfigToNVS(configConfig, 1);
	}
	else {
		writeConfigToNVS(configConfig, -1);
	}
}

bool saveConfigToNVS() {
	bool senseWinnerFlag = false;
	bool actuatorWinnerFlag = false;
	for (int i = 0; i < countOfSensors; i++) {
		//Serial.println(sensorArray[i].getSensorConfiguration());
		bool pinCheck = writeConfigToNVS(sensorConfigs[0] + i, (int)sensorArray[i].pinNumber);
		bool typeCheck = writeConfigToNVS(sensorConfigs[1] + i, (int)sensorArray[i].sType);
		bool pollCheck = writeConfigToNVS(sensorConfigs[2] + i, (int)sensorArray[i].pollingPeriod);
		bool repCheck = writeConfigToNVS(sensorConfigs[3] + i, (int)sensorArray[i].reportingPeriod);
		bool minCheck = writeConfigToNVS(sensorConfigs[4] + i, (int)sensorArray[i].minChange);
		bool maxCheck = writeConfigToNVS(sensorConfigs[5] + i, (int)sensorArray[i].maxChange);		
		if (pinCheck && typeCheck && pollCheck && repCheck && minCheck && maxCheck) {			
			if (!senseWinnerFlag) {
				senseWinnerFlag = true;
			}
		}
		else {
			senseWinnerFlag = false;
			//Serial.println("FAILURE");
			break;
		}
		
	}
	for (int x = 0; x < countOfActuators; x++) {
		//Serial.println(actuators[x]->getActuatorConfiguration());
		bool apinCheck = writeConfigToNVS(actuatorConfigs[0] + x, (int)actuators[x]->pinNumber);
		bool atypeCheck = writeConfigToNVS(actuatorConfigs[1] + x, (int)actuators[x]->aType);
		bool timeCheck = writeConfigToNVS(actuatorConfigs[2] + x, (int)actuators[x]->targetTime);
		bool enCheck = writeConfigToNVS(actuatorConfigs[3] + x, (int)actuators[x]->IsEnabled());
		if (apinCheck && atypeCheck && timeCheck && enCheck) {			
			if (!actuatorWinnerFlag) {
				actuatorWinnerFlag = true;
			}
		}
		else {
			actuatorWinnerFlag = false;
			//Serial.println("FAILURE");
			break;
		}
		
	}
	if (senseWinnerFlag && actuatorWinnerFlag) {
		//Serial.println("configExists Good");
		writeConfigToNVS(configConfig, 1);
		commitNVSChanges();
		return true;
	}
	else {		
		writeConfigToNVS(configConfig, -1);
		commitNVSChanges();
		return false;
	}		
}

//Sensor AccelZ x4 p2 t10 T100 e1 m1 M1";

void defaultDevConfig() {// hardcoded default dev config	
	sensorArray[0].configureSensor(1, 2, 10, 200, 1, 10000);
	sensorArray[1].configureSensor(0, 3, 10, 200, 1, 10000);
	sensorArray[2].configureSensor(0, 2, 10, 200, 1, 10000);
	sensorArray[3].configureSensor(2, 3, 10, 200, 1, 10000);
	sensorArray[4].configureSensor(5, 4, 10, 10, 0, 1);		//yaw
	sensorArray[5].configureSensor(3, 4, 100, 100, 0, 1);		//roll
	sensorArray[6].configureSensor(0, 4, 10, 100, 0, 60);		//accelX
	sensorArray[7].configureSensor(1, 4, 10, 100, 0, 60);		//accelY
	sensorArray[8].configureSensor(2, 4, 10, 100, 0, 60);		//accelZ
	sensorArray[9].configureSensor(0, 6, 1000, 1000, 0, 0);	//heartbeat		

	actuators[0]->configureActuator(122714, 0, 0, 1);
	actuators[1]->configureActuator(132526, 0, 0, 1);
	actuators[2]->configureActuator(15, 1, 0, 1);
	actuators[3]->configureActuator(2, 1, 0, 1);	
}

bool checkNVSforConfig() {		
	return false;
	int configCheck = getConfigFromNVS(configConfig);
	if (configCheck == 1) {
		return true;
	}
	else if(configCheck == -1) {
		Serial.println("ConfigExists = -1");
		return false;
	}
	else {
		return false;
	}
}

void configurePod() {	
	if (checkNVSforConfig())
	{
		Serial.println("Configuring from NVS");
		configFromNVS();
	}
	else {
		Serial.println("Configuring from default");
		defaultDevConfig();
	}
}

void Incoming::resetYawFromUDP() {
	sensorArray[4].setValue('r', 1);
}

void Incoming::setupTCP() {	
	while (!client.connect(bsimremoteServer, port)) {		
		l.bvalue = 50;
		l.rvalue = 0;
		l.gvalue = 0;
		l.show();
		Serial.print("Connection to ");
		Serial.print(bsimremoteServer);
		Serial.print(" failed on port: ");
		Serial.println(port);
		Serial.println("Waiting 1.5 seconds before retrying...");		
		delay(1500);
	}
	l.bvalue = 0;
	l.rvalue = 0;
	l.gvalue = 50;
	l.show();	
	Serial.println("Connection Successful!");		
}

void Incoming::reportConfiguration() {	
	for (int i = 0; i < countOfSensors; i++) {
		//Serial.println(sensorArray[i].getSensorConfiguration());
		this->sendPacket(sensorArray[i].getSensorConfiguration());
	}
	for (int x = 0; x < countOfActuators; x++) {
		//Serial.println(actuators[x]->getActuatorConfiguration());
		this->sendPacket(actuators[x]->getActuatorConfiguration());
	}
}

void Incoming::CheckKeepAlive(unsigned long keepAlive, unsigned int keepAliveOffset) {
	if ((keepAlive + keepAliveOffset < millis())) {
		if (actuators[0]->IsEnabled()) {
			actuators[0]->setValue('T', 90);
			//actuators[0]->setValue('e', 0);
		}
		if (actuators[1]->IsEnabled()) {
			actuators[1]->setValue('T', 90);
			//actuators[1]->setValue('e', 0);
		}
		//Serial.println("KeepAlive failed reconnecting");		
	}
}

void Incoming::CheckTCP() {
	if ((checkTCPHandle + checkHandleTimeOffset < millis())) {
		if (!client.connected()) {
			if (actuators[0]->IsEnabled()) {
				actuators[0]->setValue('T', 90);				
			}
			if (actuators[1]->IsEnabled()) {
				actuators[1]->setValue('T', 90);				
			}
			//Serial.println("TCP Connection Lost");
			setupTCP();
			reportConfiguration();
			sendPacket("Initialization Complete\n");
		}
		checkTCPHandle = millis();
	}
}

void Incoming::sendPacket(char* pack) {
	client.println(pack);
	return;
}
void Incoming::sendPacket(String pack) {
	client.println(pack);
	return;
}

char retStr[INPUT_BUFFER_SIZE];
int Incoming::recPacket() {
	size_t retSize;
	memset(retStr, 0, sizeof(INPUT_BUFFER_SIZE));
	if (client.available()) {
		retSize = client.readBytesUntil('\n', retStr, INPUT_BUFFER_SIZE);
		return retSize;
	}
	return 0;
}
char strHandle[INPUT_BUFFER_SIZE];
void Incoming::handleIncomingPackets() {//<-----------------------CLEAN UP THIS GARBAGE
	//get an incoming command 'packet'
	//resetcommand();
	size = 0;
	size = getInputPacket(command, INPUT_BUFFER_SIZE);

	//has a command been received?
	if (size > 0) {

		//Serial.print("PodRECD:"); Serial.println(retStr);
		//parse the command(s) in the packet
		char* param = strtok(retStr, " \n");
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
					if (param != NULL) {
						paramCode = getParamCode(param);
					}
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
					if (param != NULL) {
						paramCode = getParamCode(param);
					}
				}
				mySensor->lastReported = millis();
				//Serial.println(mySensor->ToString());
			}
			else if (getParamCode(param) == 'L') {//led code
				
				param = getNextParam();
				char paramCode = getParamCode(param);
				//Serial.println("Led spotted");
				while (param != NULL && paramCode != 'A' && paramCode != 'S' && paramCode != 'L') {
					//Serial.println("In the while loop");
					if (paramCode == 'r' || paramCode == 'g' || paramCode == 'b') {
						long value = getParamValue(param);
						//Serial.println("value Stored");
						l.setValue(paramCode, value);
						l.show();
						//Serial.println("value set");
						param = getNextParam();
						if (param != NULL) {
							paramCode = getParamCode(param);
						}
						
					}
					else if (paramCode == 'X') {
						bool saveCheck = saveConfigToNVS();			
						if (saveCheck) {
							sendPacket("CONFIG SAVED TO NVS");
						}
						else {
							sendPacket("CONFIG SAVE FAILED");
						}
						return;
					}
					else if (paramCode == 'O') {
						reportConfiguration();
						return;
					}
					else {
						Serial.println("Failed bad led cmd");
						Serial.print(param);
						break;
					}
				}				
			}			
			else {

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
	if (!client.available()) return 0;
	if (recPacket() == 0) return 0;
	return 1;
}

char* Incoming::getNextParam() {
	return strtok(NULL, " \n");
}

char Incoming::getParamCode(char* param) {
	if (param[0] != '\0') {
		return param[0];
	}
	return '\0';
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