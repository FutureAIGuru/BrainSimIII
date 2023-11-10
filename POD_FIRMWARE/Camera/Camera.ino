/*
 Name:		Camera.ino
 Created:	9/23/2021 1:40:50 PM
 Author:	c_sim
*/

#include <WiFiManager.h>
#include <strings_en.h>
#include <Update.h>
#include <HttpsOTAUpdate.h>
#include <ESPmDNS.h>
#include <WebServer.h>
#include <Uri.h>
#include <HTTP_Method.h>
#include <WiFi.h>
#include <esp_http_server.h>
#include "esp_camera.h"
#include "esp_system.h"
#include "esp_log.h"
#include "nvs_flash.h"
#include "UDPResponder1.h"
#include "PodWiFiManager.h"
#include "CameraJpgHandler.h"
#include "OTA_Updater.h"
#include "NVS_access.h"






// the setup function runs once when you press reset or power the board
void setup() {

    Serial.begin(115200);
    Serial.println("Starting");
    
    //nvs_flash_erase();

    pinMode(4, OUTPUT);    
    digitalWrite(4, LOW);
    String otaStatus = readOTAStatus();
    Serial.print("Status: ");
    Serial.println(otaStatus);
    
    if (otaToFlag(otaStatus)) {
        Serial.println("OTA_ON");
        WiFiSetup(false);
        OTA_UPDATER();
        Serial.println("OTA_RUNNING!");
    }
    else {

        WiFiSetup(false);
    }
    

    

    if (httpd_start(&s_httpd, &httpdConfig) == ESP_OK)
    {
        httpd_register_uri_handler(s_httpd, &jpg_index_uri);
        //httpd_register_uri_handler(s_httpd, &bmp_index_uri);
    }              
    esp_err_t err = esp_camera_init(&camera_config);
    if (err != ESP_OK) {
        Serial.println("Camera Init Failed");
    }
    else {
        Serial.println("Camera Init Succeeded");
    }
    s = esp_camera_sensor_get();      
    s->set_framesize(s, FRAMESIZE_HQVGA);    
    s->set_quality(s, 11);
    //s->set_res_raw(s, 0, 0, 1500, 800, 0, 0, 1500, 800, 480, 320, 1, true);

    SetupUDPResponder();
    
}



// the loop function runs over and over again until power down or reset
void loop() {
   
}
