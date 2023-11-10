#include "AudioGeneratorWAV.h"
#include "AudioOutputI2S.h"
#include "AudioFileSourcePROGMEM.h"
#include "viola.h"
#define Bit_Clock_BCLK 27
#define Word_Select_WS 26 
#define Serial_Data_SD 33
#define GAIN 0.125
AudioFileSourcePROGMEM *file;
AudioGeneratorWAV *wav;
AudioOutputI2S *out;
void setup()
{
  Serial.begin(115200);
  delay(1000);
  Serial.printf("WAV start\n");

  audioLogger = &Serial;
  file = new AudioFileSourcePROGMEM( viola, sizeof(viola) );
  out = new AudioOutputI2S();
  out -> SetGain(GAIN);
  out -> SetPinout(Bit_Clock_BCLK,Word_Select_WS,Serial_Data_SD);
  wav = new AudioGeneratorWAV();
  wav->begin(file, out);
}

void loop()
{
  if (wav->isRunning()) {
    if (!wav->loop()) wav->stop();
  } else {
    Serial.printf("WAV done\n");
    delay(1000);
  }
}
//void setup(){
//  Serial.begin(115200);
//  in = new AudioFileSourcePROGMEM(sampleaac, sizeof(sampleaac));
//  aac = new AudioGeneratorAAC();
//  out = new AudioOutputI2S();
//  out -> SetGain(GAIN);
//  out -> SetPinout(Bit_Clock_BCLK,Word_Select_WS,Serial_Data_SD);
//  aac->begin(in, out);
//}
//void loop(){
//  if (aac->isRunning()) {
//            aac->loop();
//  } else {
//            aac -> stop();
//            Serial.printf("Sound Generator\n");
//            delay(1000);
//  }
//}
/*
AudioGeneratorWAV *wav;
AudioFileSourcePROGMEM *file;
AudioOutputI2SNoDAC *out;

void setup()
{
  Serial.begin(115200);
  delay(1000);
  Serial.printf("WAV start\n");

  audioLogger = &Serial;
  file = new AudioFileSourcePROGMEM( viola, sizeof(viola) );
  out = new AudioOutputI2SNoDAC();
  wav = new AudioGeneratorWAV();
  wav->begin(file, out);
}

void loop()
{
  if (wav->isRunning()) {
    if (!wav->loop()) wav->stop();
  } else {
    Serial.printf("WAV done\n");
    delay(1000);
  }
}
*/
