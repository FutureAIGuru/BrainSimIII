#include <Arduino.h>
#include <WiFi.h>
#include <WiFiUdp.h>
#include <esp_WiFi.h>
#include "driver/i2s.h"

#define GAIN 0.125

//***********Variables******************************
/* WiFi network name and password */
const char* ssid = "FutureAI";
const char* pwd = "ccaaccaacc";
//WifiUDP config
const char* brainSimIPAddress = "192.168.0.13";
const int udpPort = 666;
//create UDP instance
WiFiUDP spkrUdp;
//UDP polling things
int polling_Interval = 20000;//Interval to Poll in milliseconds
int milli_handle = 0;
//I2s port setup
const i2s_port_t MIC_PORT = I2S_NUM_0;
//buffer setup
const size_t bufferSize = 255;
char buffer[(int)bufferSize];

bool mic_on_flag = false;
bool speaker_on_flag = false;
int bufferChain = 0;//bufferChain is for buffering up to 1kB so that we dont udp flood brainsim

//***********EndVariables***************************
//###########i2s_configurations#####################
// The pin config as per the setup---------------------------------------------from the ryan usage
//const i2s_pin_config_t pin_config = {
//    .bck_io_num = 27,   // Serial Clock (SCK)
//    .ws_io_num = 26,    // Word Select (WS)
//    .data_out_num = I2S_PIN_NO_CHANGE, // not used (only for speakers)
//    .data_in_num = 33   // Serial Data (SD)
//};
// 
#define I2S_MIC_SERIAL_CLOCK GPIO_NUM_26
#define I2S_MIC_LEFT_RIGHT_CLOCK GPIO_NUM_25
#define I2S_MIC_SERIAL_DATA GPIO_NUM_33
#define I2S_PIN_NO_CHANGE NULL
#define I2S_SPEAKER_SERIAL_CLOCK GPIO_NUM_27
#define I2S_SPEAKER_LEFT_RIGHT_CLOCK GPIO_NUM_32
#define I2S_SPEAKER_SERIAL_DATA GPIO_NUM_14

// The I2S config as per the example-------------------------------------------from the ryan usage
const i2s_config_t i2s_mic_config = {
    .mode = i2s_mode_t(I2S_MODE_MASTER | I2S_MODE_RX), // Receive, not transfer
    .sample_rate = 16000,                         // 16KHz
    .bits_per_sample = I2S_BITS_PER_SAMPLE_32BIT, // could only get it to work with 32bits
    .channel_format = I2S_CHANNEL_FMT_ONLY_LEFT, // use left channel
    .communication_format = i2s_comm_format_t(I2S_COMM_FORMAT_I2S | I2S_COMM_FORMAT_I2S_MSB),
    .intr_alloc_flags = ESP_INTR_FLAG_LEVEL1,     // Interrupt level 1
    .dma_buf_count = 2,                           // number of buffers
    .dma_buf_len = 1024                              // 8 samples per buffer (minimum)
};
const i2s_config_t i2s_speaker_config = {
    .mode = (i2s_mode_t)(I2S_MODE_MASTER | I2S_MODE_TX),
    .sample_rate = 16000,
    .bits_per_sample = I2S_BITS_PER_SAMPLE_16BIT,
    .channel_format = I2S_CHANNEL_FMT_ONLY_LEFT,
    .communication_format = (i2s_comm_format_t)(I2S_COMM_FORMAT_I2S),
    .intr_alloc_flags = ESP_INTR_FLAG_LEVEL1,
    .dma_buf_count = 4,
    .dma_buf_len = 1024,
};
//Config stuff
i2s_pin_config_t i2s_mic_pins = {
    .bck_io_num = I2S_MIC_SERIAL_CLOCK,
    .ws_io_num = I2S_MIC_LEFT_RIGHT_CLOCK,
    .data_out_num = I2S_PIN_NO_CHANGE,
    .data_in_num = I2S_MIC_SERIAL_DATA };

// i2s speaker pins
i2s_pin_config_t i2s_speaker_pins = {
    .bck_io_num = I2S_SPEAKER_SERIAL_CLOCK,
    .ws_io_num = I2S_SPEAKER_LEFT_RIGHT_CLOCK,
    .data_out_num = I2S_SPEAKER_SERIAL_DATA,
    .data_in_num = I2S_PIN_NO_CHANGE };
//########Endi2s_configurations#####################
//==========================Functions===============
byte inBuff[1024];
void Record_and_Send() {
    int32_t sample = 0;
    byte convVal[4];
    byte bonus[4];   
        
    //int bytes_read = i2s_pop_sample(MIC_PORT, (char*)&sample, portMAX_DELAY); // no timeout
    int bytes_read = 0 ;
    i2s_read(MIC_PORT, &inBuff, 1024, (size_t*)&bytes_read, 1);    
    if (bytes_read != NULL) {        
        spkrUdp.beginPacket(brainSimIPAddress, udpPort);
        spkrUdp.write(inBuff, 1024);
        spkrUdp.endPacket();
    }
}
//functions and things that should really be moved to a header/usr library
size_t pollSpkrUDPListener() {
    size_t numberOfBytesRead = 0;
    int byteCount = spkrUdp.parsePacket();
    //Serial.print(byteCount);
    if (byteCount > 0) {
        numberOfBytesRead = spkrUdp.read(buffer, bufferSize);
    }
    return numberOfBytesRead;
}

void checkUDPAndRespond() {
    if (pollSpkrUDPListener() != 0) {//Pull the udp payload in, select the audio amplifier on the i2s interface, and play the payload through it;        
        if (mic_on_flag == true) {
            i2s_stop(MIC_PORT);
            //play the buffer throught the audio amplifier
            //install output i2s_config;
            i2s_driver_uninstall(MIC_PORT);
        }
        if (speaker_on_flag == false) {
            //Select the audio amplifier on the i2s
            //uninstall mic i2s_config;

            //i2s_driver_install(I2S_PORT, &i2s_speaker_config, 0, NULL);
            i2s_set_pin(MIC_PORT, &i2s_speaker_pins);

            speaker_on_flag = true;
        }
        //--------------------------------------------------------------------------------------TODO
        //play the buffer through i2s_write(buffer);
        int samples_to_send = bufferSize;
        size_t bytes_written = 0;
        Serial.println("should be writing to i2s_output but is currently disabled");
        //i2s_write(I2S_PORT, &buffer, bufferSize, &bytes_written, portMAX_DELAY);        
        if (bytes_written != samples_to_send * sizeof(int16_t) * 2) {
            ESP_LOGE(TAG, "Did not write all bytes");
        }

    }
    else {//Nothing in the udp rx buffer
        Serial.println("Timer hit but No Stuff to do");
    }
}
void Poll_for_data() {//look for some udp if the poll_interval is less than (millis()-milli_handle);
    Serial.println("current millis-mill_handle> " + (millis() - milli_handle));
    if (polling_Interval < (millis() - milli_handle))
    {
        milli_handle = millis();
        checkUDPAndRespond();//check udp and react if data is there
    }
    else {

        return;//Nothing to do
    }
}
//=======================EndFunctions===============






//###########################Setup####################
void setup() {
    Serial.begin(115200);

    //pinMode(2, OUTPUT);
    //digitalWrite(2, LOW);

    //Connect to the WiFi network
    WiFi.begin(ssid, pwd);
    Serial.println("");

    // Wait for connection
    while (WiFi.status() != WL_CONNECTED) {
        delay(500);
        Serial.print(".");
    }
    Serial.println("");
    Serial.print("Connected to ");
    Serial.println(ssid);
    Serial.print("IP address: ");
    Serial.println(WiFi.localIP());
    esp_err_t err;
    err = i2s_driver_install(MIC_PORT, &i2s_mic_config, 0, NULL);
    if (err != ESP_OK) {
        Serial.printf("Failed installing driver: %d\n", err);
        while (true);
    }
    err = i2s_set_pin(MIC_PORT, &i2s_mic_pins);
    if (err != ESP_OK) {
        Serial.printf("Failed setting pin: %d\n", err);
        while (true);
    }
    Serial.println("I2S driver installed.");
    //digitalWrite(2, HIGH);      
}
//########################EndSetup####################

//************************Loop************************
//  Poll every T_time for udp{
//      if packet received -> play the audio in the packet;
//  }
//  else{
//      record from i2s mic into 1kB packet then transmit it to brainsim;
//  }
//****************************************************
void loop() {
    //Poll_for_data();
    Record_and_Send();
}