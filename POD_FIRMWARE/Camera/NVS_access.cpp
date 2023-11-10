// 
// 
// 

#include "NVS_access.h"
nvs_handle machine_handle;

String defaultName = "Camera";
String onStatus = "OTA_ON";
//String offStatus = "OTA_OFF";

String readNamingCredentialsFromNvs(String& machineName) {

	//if there are wifi credentials stored in the nvs, use them to connect
	//in the following line, "Camera" is a namespace.  Set it to whatever so long as it doesn't collide 
	//with other apps on this machine which might use the NVS 	

	esp_err_t err = nvs_open("Machine_Name", NVS_READWRITE, &machine_handle);
	if (err != ESP_OK) {
		Serial.printf("Error (%s) opening NVS handle!\n", esp_err_to_name(err));
	}
	else {
		Serial.println("We have an ESP_OK code");
		size_t max_length = 100;
		char tempVal[100]; //this is so we can use Strings while getting info from char* interfaces
		err = nvs_get_str(machine_handle, "Machine_Name", tempVal, &max_length);
		if (err == ESP_OK) {
			machineName = String(tempVal);			
			return machineName;
		}
		else {
			return defaultName;
		}
	}	
}

String writeNamingCredentialsToNvs(String machineName) {
	Serial.println("Writing ssid/pwd to nvs");
	esp_err_t err = nvs_set_str(machine_handle, "Machine_Name", machineName.c_str());
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
	esp_err_t err = nvs_open("OTA_FLAG", NVS_READWRITE, &machine_handle);
	if (err != ESP_OK) {
		Serial.printf("Error (%s) opening NVS handle!\n", esp_err_to_name(err));
	}
	Serial.println("Writing otaSatus to nvs");
	err = nvs_set_str(machine_handle, "OTA_FLAG", otaStatus.c_str());
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
	
	esp_err_t err = nvs_open("OTA_FLAG", NVS_READWRITE, &machine_handle);
	if (err != ESP_OK) {
		Serial.printf("Error (%s) opening NVS handle!\n", esp_err_to_name(err));
	}
	else {
		Serial.println("We have an ESP_OK code");
		size_t max_length = 100;
		char tempVal[100]; //this is so we can use Strings while getting info from char* interfaces
		err = nvs_get_str(machine_handle, "OTA_FLAG", tempVal, &max_length);
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