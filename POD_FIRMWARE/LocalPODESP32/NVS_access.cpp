// 
// 
// 

#include "NVS_access.h"
nvs_handle my_handle;

String defaultName = "SalliePod";
String onStatus = "OTA_ON";
String configStatus = "none";
//String offStatus = "OTA_OFF";
/*
bool checkNVSforconfiguration(String& configParams) {
	esp_err_t errStatus = nvs_open("configParams0", NVS_READWRITE, &my_handle);
	if (errStatus != ESP_OK) {
		Serial.printf("Error (%s) opening NVS handle!\n", esp_err_to_name(errStatus));
		return false;
	}
	else {
		Serial.println("We have an ESP_OK code");
		size_t max_length = 100;
		char tempVal[100]; //this is so we can use Strings while getting info from char* interfaces
		errStatus = nvs_get_str(my_handle, "configParams0", tempVal, &max_length);
		if (errStatus == ESP_OK) {
			configStatus = "confirmed";
			return true;
		}
		else {
			return false;
		}
	}

}
*/


int getConfigFromNVS(String configName) {	
	Serial.println(configName);
	esp_err_t err = nvs_open(configName.c_str(), NVS_READONLY, &my_handle);
	if (err != ESP_OK) {
		Serial.print("Error: ");
		Serial.println(esp_err_to_name(err));
		return -1;
	}
	else {
		//Serial.print(configName);
		//Serial.println(" read successfully!");		
		int tempVal = 0;
		err = nvs_get_i32(my_handle, configName.c_str(), &tempVal);
		if (err == ESP_OK) {			
			return tempVal;
		}
		else {
			return -1;
		}
	}
}

bool writeConfigToNVS(String configName, int configParams) {		
	Serial.println(configName);
	esp_err_t err = nvs_open(configName.c_str(), NVS_READWRITE, &my_handle);
	if (err != ESP_OK) {
		Serial.print("failed to open due to: ");
		Serial.println(esp_err_to_name(err));
		return false;
	}
	err = nvs_set_i32(my_handle, configName.c_str(), configParams);
	if (err == ESP_OK) {
		Serial.println("Successful write");		
		return true;
	}
	else {		
		Serial.print("Error: ");
		Serial.println(esp_err_to_name(err));
		return false;
	}
}

bool commitNVSChanges() {
	esp_err_t err = nvs_commit(my_handle);
	if (err == ESP_OK) {
		Serial.println("NVS CHANGES SAVED");
		return true;
	}
	else {
		Serial.println("NVS commitment issues");
		return false;
	}
}

String readCredentialsFromNvs(String& machineName) {

	//if there are wifi credentials stored in the nvs, use them to connect
	//in the following line, "Camera" is a namespace.  Set it to whatever so long as it doesn't collide 
	//with other apps on this machine which might use the NVS 	

	esp_err_t err = nvs_open("Machine_Name", NVS_READWRITE, &my_handle);
	if (err != ESP_OK) {
		Serial.printf("Error (%s) opening NVS handle!\n", esp_err_to_name(err));
		return defaultName;
	}
	else {
		Serial.println("We have an ESP_OK code");
		size_t max_length = 100;
		char tempVal[100]; //this is so we can use Strings while getting info from char* interfaces
		err = nvs_get_str(my_handle, "Machine_Name", tempVal, &max_length);
		if (err == ESP_OK) {
			machineName = String(tempVal);			
			return machineName;
		}
		else {
			return defaultName;
		}
	}	
}

String writeCredentialsToNvs(String machineName) {
	Serial.println("Writing ssid/pwd to nvs");
	esp_err_t err = nvs_set_str(my_handle, "Machine_Name", machineName.c_str());
	if (err == ESP_OK) {
		Serial.print("Successful Machine_Name write ");
		Serial.println(machineName.c_str());
		return machineName;
	}
	else {
		Serial.printf("Error (%s) writing SSID NVS handle!\n", esp_err_to_name(err));
		return defaultName;
	}
}

bool otaToNvs(String otaStatus) {
	esp_err_t err = nvs_open("OTA_FLAG", NVS_READWRITE, &my_handle);
	if (err != ESP_OK) {
		Serial.printf("Error (%s) opening NVS handle!\n", esp_err_to_name(err));
	}
	Serial.println("Writing otaSatus to nvs");
	err = nvs_set_str(my_handle, "OTA_FLAG", otaStatus.c_str());
	if (err == ESP_OK) {
		Serial.println("OTA_FLAG WRITTEN");
		return true;
	}
	else {
		Serial.printf("Error (%s) writing SSID NVS handle!\n", esp_err_to_name(err));
		return false;
	}
}

bool otaToFlag(String otaStatus) {
	if (otaStatus == "OTA_ON") {
		return true;
	}
	return false;
}
String readOTAStatus() {	
	
	esp_err_t err = nvs_open("OTA_FLAG", NVS_READWRITE, &my_handle);
	if (err != ESP_OK) {
		Serial.printf("Error (%s) opening NVS handle!\n", esp_err_to_name(err));
		return "OTA_OFF";
	}
	else {
		Serial.println("We have an ESP_OK code");
		size_t max_length = 100;
		char tempVal[100]; //this is so we can use Strings while getting info from char* interfaces
		err = nvs_get_str(my_handle, "OTA_FLAG", tempVal, &max_length);
		if (err == ESP_OK) {
			Serial.println("Clean OTA_FLAG read");
			String returnstr = String(tempVal);
			return returnstr;
		}
		else {
			Serial.printf("Error (%s) reading NVS!\n", esp_err_to_name(err));			
			return "OTA_OFF";
		}
	}	
}