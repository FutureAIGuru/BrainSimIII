#include "driver/i2s.h"
#include "audio_file.h"

#define I2S_MIC_SERIAL_CLOCK GPIO_NUM_27
#define I2S_MIC_LEFT_RIGHT_CLOCK GPIO_NUM_26
#define I2S_MIC_SERIAL_DATA GPIO_NUM_25
//#define I2S_PIN_NO_CHANGE NULL
#define I2S_SPEAKER_SERIAL_CLOCK GPIO_NUM_27
#define I2S_SPEAKER_LEFT_RIGHT_CLOCK GPIO_NUM_26
#define I2S_SPEAKER_SERIAL_DATA GPIO_NUM_33

const i2s_port_t mic_port = I2S_NUM_0;
const i2s_port_t speaker_port = I2S_NUM_1;
//buffer setup
size_t bytes_written = 0;
//const size_t bufferSize = sizeof(viola);


const i2s_config_t i2s_mic_config = {
      .mode = i2s_mode_t(I2S_MODE_MASTER | I2S_MODE_RX), // Receive, not transfer
      .sample_rate = 16000,                         // 16KHz
      .bits_per_sample = I2S_BITS_PER_SAMPLE_32BIT, // could only get it to work with 32bits
      .channel_format = I2S_CHANNEL_FMT_ONLY_RIGHT, // use right channel
      .communication_format = i2s_comm_format_t(I2S_COMM_FORMAT_I2S | I2S_COMM_FORMAT_I2S_MSB),
      .intr_alloc_flags = ESP_INTR_FLAG_LEVEL1,     // Interrupt level 1
      .dma_buf_count = 4,                           // number of buffers
      .dma_buf_len = 16                              // 8 samples per buffer (minimum)
};

const i2s_config_t i2s_speaker_config = {
    .mode = (i2s_mode_t)(I2S_MODE_MASTER | I2S_MODE_TX),
    .sample_rate = 44100,
    .bits_per_sample = I2S_BITS_PER_SAMPLE_16BIT,
    .channel_format = I2S_CHANNEL_FMT_RIGHT_LEFT,
    .communication_format = (i2s_comm_format_t)(I2S_COMM_FORMAT_I2S),
    .intr_alloc_flags = ESP_INTR_FLAG_LEVEL1,
    .dma_buf_count = 2,
    .dma_buf_len = 1024,
    .use_apll = false,
    .tx_desc_auto_clear = false,
    .fixed_mclk = 0
};
i2s_pin_config_t i2s_mic_pins = {
    .bck_io_num = I2S_MIC_SERIAL_CLOCK,   // Serial Clock (SCK)
    .ws_io_num = I2S_MIC_LEFT_RIGHT_CLOCK,    // Word Select (WS)
    .data_out_num = I2S_PIN_NO_CHANGE, // not used (only for speakers)
    .data_in_num = I2S_MIC_SERIAL_DATA   // Serial Data (SD)
};
//i2s_pin_config_t i2s_speaker_pins = {
//    .bck_io_num = I2S_MIC_SERIAL_CLOCK,   // Serial Clock (SCK)
//    .ws_io_num = I2S_MIC_LEFT_RIGHT_CLOCK,    // Word Select (WS)
//    .data_out_num = I2S_SPEAKER_SERIAL_DATA, // not used (only for speakers)
//    .data_in_num = I2S_PIN_NO_CHANGE   // Serial Data (SD)
//};
i2s_pin_config_t i2s_speaker_pins = {
    .bck_io_num = I2S_PIN_NO_CHANGE,
    .ws_io_num = I2S_PIN_NO_CHANGE,
    .data_out_num = I2S_SPEAKER_SERIAL_DATA,
    .data_in_num = I2S_PIN_NO_CHANGE
};

void setup() {
    // put your setup code here, to run once:
    Serial.begin(115200);
    Serial.println("Running Setup");

    esp_err_t err;

    err = i2s_driver_install(mic_port, &i2s_mic_config, 0, NULL);
    if (err != ESP_OK) {
        Serial.printf("Failed installing mic driver: %d\n", err);
        while (true);
    }
    err = i2s_driver_install(speaker_port, &i2s_speaker_config, 0, NULL);
    if (err != ESP_OK) {
        Serial.printf("Failed installing speaky driver: %d\n", err);
        while (true);
    }
    err = i2s_set_pin(mic_port, &i2s_mic_pins);
    if (err != ESP_OK) {
        Serial.printf("Failed setting mic pins: %d\n", err);
        while (true);
    }
    err = i2s_set_pin(speaker_port, &i2s_speaker_pins);
    if (err != ESP_OK) {
        Serial.printf("Failed setting speaky pins: %d\n", err);
        while (true);
    }
    //i2s_stop(mic_port);
    i2s_zero_dma_buffer(speaker_port);
    Serial.print(bufferSize);
    const int samplesTODiv = 1024;
    char soundBuff[samplesTODiv];    
    //  for (int i = 0; i < 300; i++)   {
    //    i2s_write(I2S_PORT, viola+1024*i, 1024, &bytes_written, portMAX_DELAY);    
    //  }
    //i2s_write(speaker_port, viola, bufferSize/4, &bytes_written, portMAX_DELAY);
    Serial.print(bytes_written);
    //i2s_stop(speaker_port);
    //i2s_start(mic_port);
}
int violaOffset = 0;

void loop() {
//    int32_t sample = 0;
//    int bytes_read = i2s_pop_sample(mic_port, (char*)&sample, portMAX_DELAY); // no timeout
//    if (bytes_read > 0) {
//        Serial.println(sample);
//    }
//
//    char soundBuf[1024];
//    if (violaOffset < sizeof(viola))
//    {
//        Serial.println("violaPlaying 1/16");
//        for (int i = 0; i < sizeof(soundBuf); i++) {
//            soundBuf[i] = viola[violaOffset++];
//        }
//        i2s_write(speaker_port, soundBuf, 1024, &bytes_written, portMAX_DELAY);
//    }
//    else if (violaOffset != -1)
//    {
//        Serial.println("viola offset writing");
//        for (int i = 0; i < sizeof(soundBuf); i++) {
//            soundBuf[i] = 0;
//        }
//        i2s_write(speaker_port, soundBuf, 1024, &bytes_written, portMAX_DELAY);
//        i2s_zero_dma_buffer(speaker_port);
//        violaOffset = -1;
//    }
//
//    // put your main code here, to run repeatedly:
//    //Serial.println("Running Loop");
//    /*if (millis() % 10000 == 0) {
//        i2s_write(speaker_port, viola, bufferSize, &bytes_written, portMAX_DELAY);    
//    }
//    */
}
