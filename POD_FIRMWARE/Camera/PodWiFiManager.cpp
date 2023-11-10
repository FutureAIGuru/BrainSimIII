#include <WiFiManager.h> //https://github.com/tzapu/WiFiManager


void WiFiSetup(bool newNetFlag) {
    // put your setup code here, to run once:
    //Serial.begin(115200);
    Serial.println("WifiManager");

    WiFi.mode(WIFI_STA); // explicitly set mode, esp defaults to STA+AP
    // it is a good practice to make sure your code sets wifi mode how you want it.


    //WiFiManager, Local intialization. Once its business is done, there is no need to keep it around
    WiFiManager wm;
    if (newNetFlag) {
        //wm.disconnect();
        wm.resetSettings();
        delay(2000);
        ESP.restart();
        delay(2000);
    }
    // reset settings - wipe stored credentials for testing
    // these are stored by the esp library
    //wm.resetSettings();

    // Automatically connect using saved credentials,
    // if connection fails, it starts an access point with the specified name ( "AutoConnectAP"),
    // if empty will auto generate SSID, if password is blank it will be anonymous AP (wm.autoConnect())
    // then goes into a blocking loop awaiting configuration and will return success result

    bool res;
    // res = wm.autoConnect(); // auto generated AP name from chipid
    // res = wm.autoConnect("AutoConnectAP"); // anonymous ap
    //res = wm.autoConnect("SalliePodConfig"); //doesn't seem to work
    res = wm.autoConnect("SallieCameraConfig", "salliecamera"); // password protected ap

    if (!res) {
        Serial.println("Failed to connect");
        ESP.restart();
    }
    else {
        //if you get here you have connected to the WiFi    
        Serial.println("Connected to the STA");
    }

}

