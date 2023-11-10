#include "driver/i2s.h"
//#include "audio_file.h"

#define I2S_MIC_SERIAL_CLOCK GPIO_NUM_26
#define I2S_MIC_LEFT_RIGHT_CLOCK GPIO_NUM_25
#define I2S_MIC_DUMMY_PIN GPIO_NUM_13
#define I2S_MIC_SERIAL_DATA GPIO_NUM_33

#define I2S_SPEAKER_SERIAL_CLOCK GPIO_NUM_27
#define I2S_SPEAKER_LEFT_RIGHT_CLOCK GPIO_NUM_32
#define I2S_SPEAKER_DUMMY_PIN GPIO_NUM_12
#define I2S_SPEAKER_SERIAL_DATA GPIO_NUM_14

int bufSize = 600;
short * wave;
short * wave1;

short * ramp(int freq)
{
  short * wave = (short *) malloc(bufSize);

  for (int i = 0; i < bufSize; i++)
  {

    wave[i] = i / 10 - 1100;
    wave[i] &= 0xfff0;
  }
  return wave;
}
void sinWave(int freq, short* buffPointer, int volume) {
  for (int k = 0; k < bufSize / freq; k++) {
    for (int i = 0; i < freq; i++) {
      float degrees = ((float) i / freq) * 360.0;
      float sinVal = sin(degrees * 2 * PI / 360);
      int value = (short)(sinVal * 8000);      
      value /= volume;
      buffPointer[i + k * freq] = value;
      //wave[i + k * freq] *= 1;
    }
  }
}

const i2s_port_t SPEAKER_PORT = I2S_NUM_1;
const i2s_port_t MIC_PORT = I2S_NUM_0;
//buffer setup
size_t bytes_written = 0;
//const size_t bufferSize = sizeof(viola);

const i2s_config_t i2s_mic_config = {
    .mode = i2s_mode_t(I2S_MODE_MASTER | I2S_MODE_RX), // Receive, not transfer
    .sample_rate = 24000,                         // 16KHz
    .bits_per_sample = I2S_BITS_PER_SAMPLE_32BIT, // could only get it to work with 32bits
    .channel_format = I2S_CHANNEL_FMT_ONLY_LEFT, // use left channel
    .communication_format = i2s_comm_format_t(I2S_COMM_FORMAT_I2S | I2S_COMM_FORMAT_I2S_MSB),
    .intr_alloc_flags = ESP_INTR_FLAG_LEVEL2,     // Interrupt level 1
    .dma_buf_count = 4,                           // number of buffers
    .dma_buf_len = 8,                              // 8 samples per buffer (minimum)
    .use_apll = false,
    .tx_desc_auto_clear = false,
    .fixed_mclk = 0
};
const i2s_config_t i2s_speaker_config = {
  .mode = (i2s_mode_t)(I2S_MODE_MASTER | I2S_MODE_TX),
  .sample_rate = 24000,
  .bits_per_sample = I2S_BITS_PER_SAMPLE_16BIT,
  .channel_format = I2S_CHANNEL_FMT_ONLY_LEFT,
  .communication_format = (i2s_comm_format_t)(I2S_COMM_FORMAT_I2S),
  .intr_alloc_flags = ESP_INTR_FLAG_LEVEL1,
  .dma_buf_count = 2,
  .dma_buf_len = 1024,
  .use_apll = false,
  .tx_desc_auto_clear = false,
  .fixed_mclk = 0
};
i2s_pin_config_t i2s_mic_pins = {
    .bck_io_num = I2S_MIC_SERIAL_CLOCK,
    .ws_io_num = I2S_MIC_LEFT_RIGHT_CLOCK,
    .data_out_num = I2S_PIN_NO_CHANGE,
    .data_in_num = I2S_MIC_SERIAL_DATA 
    };
i2s_pin_config_t i2s_speaker_pins = {
  .bck_io_num = I2S_SPEAKER_SERIAL_CLOCK,
  .ws_io_num = I2S_SPEAKER_LEFT_RIGHT_CLOCK,
  .data_out_num = I2S_SPEAKER_SERIAL_DATA,
  .data_in_num = I2S_PIN_NO_CHANGE 
};

void setup() {
  // put your setup code here, to run once:
  Serial.begin(115200);
  Serial.println("Running Setup");
  
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
      Serial.println("I2S MIC driver installed.");
      
  
  err = i2s_driver_install(SPEAKER_PORT, &i2s_speaker_config, 0, NULL);
  if (err != ESP_OK) {
        Serial.printf("Failed installing driver: %d\n", err);
        while (true);
      }
  err = i2s_set_pin(SPEAKER_PORT, &i2s_speaker_pins);
  if (err != ESP_OK) {
        Serial.printf("Failed setting pin: %d\n", err);
        while (true);
      }
wave = (short *) malloc(bufSize);
  

  
  
  //while (true) {
  //  int bytes_read = i2s_pop_sample(MIC_PORT, (char*)&sample, 10);
  //  if(bytes_read > 0){      
  //      Serial.println(sample);      
  //  }
  //  if(written_counter < bufSize*5){
  //    int buffer_offset = (bufSize-written_counter)+written_counter;
  //    i2s_write(SPEAKER_PORT, wave, buffer_offset, &bytes_written, 50);
  //    written_counter += bytes_written;
  //    //Serial.println(written_counter);
  //  }
  //  if(written_counter >= bufSize*5){
  //    i2s_stop(SPEAKER_PORT);
  //  }
  //}   
}
int violaOffset = 0;
int countCount = 0;
void loop() {  
  
//  wave1 = (short *) malloc(bufSize);
  //sinWave(40, wave, 8);
//  sinWave(80, wave1, 8);  
  const int udp_payload_size = 1024;  
  short* ryanBuffer1;
  //short* ryanBuffer2 = (short*) malloc(udp_payload_size);    
  
  int32_t sample = 0;   
  size_t written_counter = 0;
  size_t bytes_read = 0;
  
  
  int endToRead = bufSize;
  
  //for(int jk = 0; jk < bufSize; jk++){
    bytes_read = i2s_pop_sample(MIC_PORT, &sample, portMAX_DELAY);//1 32 bit sample
    sample  = sample >> 12;    
    wave[countCount] = (short)sample & 0xFFFF;//1 16 bit sample
    
  //}
  
  if((countCount >= (bufSize/2)-1)){//1buf size worth of printing is ready
    //i2s_start(SPEAKER_PORT);
    for(int k = 0; k<1;k++){
      int offsetter = 0;
      //for (int i = 0; (i < bufSize/1000)||(offsetter>bufSize);i++) {    
        ryanBuffer1 = wave + offsetter;      
        i2s_write(SPEAKER_PORT, wave, bufSize, &written_counter, portMAX_DELAY);    
          offsetter = offsetter + (written_counter/2);
          if(offsetter>bufSize)offsetter=bufSize;               
      //}
    }
    //i2s_stop(SPEAKER_PORT);
    countCount = 0;
  }
  //Serial.println("done");
  
  countCount++;
}
