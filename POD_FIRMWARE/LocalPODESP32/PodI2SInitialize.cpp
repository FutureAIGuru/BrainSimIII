// 
// 
// 

#include "PodI2SInitialize.h"

//create UDP instance
WiFiUDP spkrUdp;

//UDP polling things
int polling_Interval = 20000;//Interval to Poll in milliseconds
int milli_handle = 0;

//I2s port setup
const i2s_port_t SPEAKER_PORT = I2S_NUM_1;
const i2s_port_t MIC_PORT = I2S_NUM_0;

//speaker buffer setup
#define SPKR_BUFFER_COUNT 30
const size_t spkrbufferSize = 1024;
int bufferToRead = 0;
int bufferToWrite = 0;
char bufferPool[SPKR_BUFFER_COUNT][(int)spkrbufferSize];
int speakerBufferCount = 0;
int minSpeakerBuffersNeeded = SPKR_BUFFER_COUNT/4;
bool minBuffersTriggered = false;

//mic buffer setup
#define MIC_BUFFER_COUNT 4
const size_t micBufferSize = 1024;
int micBufferToRead = 0;
int micBufferToWrite = 0;
char micBufferPool[MIC_BUFFER_COUNT][(int)micBufferSize];

//timer for timeout shutoff
long lastSpeakerOutputTime = 0;
long delayToShutOffSpeaker = 200;

const i2s_config_t i2s_speaker_config = {
	.mode = (i2s_mode_t)(I2S_MODE_MASTER | I2S_MODE_TX),
	.sample_rate = 16000,
	.bits_per_sample = I2S_BITS_PER_SAMPLE_16BIT,
	.channel_format = I2S_CHANNEL_FMT_ONLY_LEFT,
	.communication_format = (i2s_comm_format_t)(I2S_COMM_FORMAT_I2S),
	.intr_alloc_flags = ESP_INTR_FLAG_LEVEL1,
	.dma_buf_count = SPKR_BUFFER_COUNT,
	.dma_buf_len = 512,
};
// i2s speaker pins
i2s_pin_config_t i2s_speaker_pins = {
	.bck_io_num = I2S_SPEAKER_SERIAL_CLOCK,
	.ws_io_num = I2S_SPEAKER_LEFT_RIGHT_CLOCK,
	.data_out_num = I2S_SPEAKER_SERIAL_DATA,
	.data_in_num = -1 };

const i2s_config_t i2s_mic_config = {
	.mode = (i2s_mode_t)(I2S_MODE_MASTER | I2S_MODE_RX), // Receive, not transfer
	.sample_rate = 16000,                         // 16KHz
	.bits_per_sample = I2S_BITS_PER_SAMPLE_32BIT, // could only get it to work with 32bits
	.channel_format = I2S_CHANNEL_FMT_ONLY_LEFT, // use left channel
	.communication_format = i2s_comm_format_t(I2S_COMM_FORMAT_I2S | I2S_COMM_FORMAT_I2S_MSB),
	.intr_alloc_flags = ESP_INTR_FLAG_LEVEL1,     // Interrupt level 1
	.dma_buf_count = MIC_BUFFER_COUNT,                           // number of buffers
	.dma_buf_len = 256,                              // 8 samples per buffer (minimum)
};
i2s_pin_config_t i2s_mic_pins = {
	.bck_io_num = I2S_MIC_SERIAL_CLOCK,
	.ws_io_num = I2S_MIC_LEFT_RIGHT_CLOCK,
	.data_out_num = -1,
	.data_in_num = I2S_MIC_SERIAL_DATA };


void clearBuffer(int buffNum) {
	for (int i = 0; i < spkrbufferSize; i++) {
		bufferPool[buffNum][i] = '\0';
	}
}

size_t pollSpkrUDPListener() {
	size_t numberOfBytesRead = 0;

	//read data to the buffer to read
	int byteCount = spkrUdp.parsePacket();
	if (byteCount > 0) {
		numberOfBytesRead = spkrUdp.read(bufferPool[bufferToRead], spkrbufferSize);
		//Serial.println("DataRead!");
		bufferToRead++;
		speakerBufferCount++;
		if (bufferToRead == SPKR_BUFFER_COUNT) bufferToRead = 0;
	}
	return numberOfBytesRead;
}

void PollUDPForSpkr() {
	//Pull the udp payload in, select the audio amplifier on the i2s interface, and play the payload through it;
	if (pollSpkrUDPListener() != 0 || speakerBufferCount > 0) {
		//--------------------------------------------------------------------------------------TODO
		//play the buffer through i2s_write(buffer);
		int samples_to_send = spkrbufferSize;
		size_t bytes_written = 0;		
		if (speakerBufferCount >= minSpeakerBuffersNeeded || minBuffersTriggered) {
			if (minBuffersTriggered == false) minBuffersTriggered = true;
			i2s_write(SPEAKER_PORT, &bufferPool[bufferToWrite], spkrbufferSize, &bytes_written, 0);
			if (bytes_written == spkrbufferSize)
			{
				clearBuffer(bufferToWrite);
				bufferToWrite++;
				speakerBufferCount--;
				if (bufferToWrite == SPKR_BUFFER_COUNT) bufferToWrite = 0;
			}
			lastSpeakerOutputTime = millis();
		}
	}
	else {
		//Nothing in the udp rx buffer
		//check the timer to shut off the speaker when out of data
		if ((millis() > (lastSpeakerOutputTime + delayToShutOffSpeaker)) && (speakerBufferCount < 1)) {
			i2s_zero_dma_buffer(SPEAKER_PORT);
			minBuffersTriggered = false;
			speakerBufferCount = 0;
		}
	}
}
void PollMic()
{
	int bytes_read = 0;
	i2s_read(MIC_PORT, &micBufferPool[micBufferToRead], 1024, (size_t*)&bytes_read, 0);
	if (bytes_read >= 1000) {
		micBufferToRead++;
		if (micBufferToRead == MIC_BUFFER_COUNT)micBufferToRead = 0;
		if (brainSimIPAddress == "") return; //bail if there is no location to send to
		char buffer[20];
		brainSimIPAddress.toCharArray(buffer, 20);
		spkrUdp.beginPacket(buffer, udpPort);
		spkrUdp.write((uint8_t*)micBufferPool[micBufferToRead], 1024);
		spkrUdp.endPacket();
	}

}
void AudioInitialize() {
	//this makes a connection or sets up an access point to enter new credentials
	WiFiSetup(false);

	//this sets up the responder so a brain can attach to a pod
	SetupUDPResponder();
	esp_err_t err;
	//set up microphone driver and pins
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

	//set up speaker driver and pins
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
	Serial.println("Audio Initialized");
	spkrUdp.begin(udpPort);
}