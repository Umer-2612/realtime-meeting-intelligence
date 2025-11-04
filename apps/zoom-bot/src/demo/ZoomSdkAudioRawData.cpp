// Audio raw data sink
#include "rawdata/rawdata_audio_helper_interface.h"
#include "ZoomSdkAudioRawData.h"
#include "zoom_sdk_def.h" 
#include <iostream>
#include <fstream>

void ZoomSdkAudioRawData::onOneWayAudioRawDataReceived(AudioRawData* audioRawData, uint32_t node_id)
{
	std::cout << "\n===== ONE-WAY AUDIO DATA RECEIVED ====="  << std::endl;
	std::cout << "  - From Node ID: " << node_id << std::endl;
	std::cout << "  - Buffer Length: " << audioRawData->GetBufferLen() << " bytes" << std::endl;
	std::cout << "  - Sample Rate: " << audioRawData->GetSampleRate() << " Hz" << std::endl;
	std::cout << "  - Channels: " << audioRawData->GetChannelNum() << std::endl;
	std::cout << "  - Valid Buffer: " << (audioRawData->GetBuffer() != nullptr ? "Yes" : "No") << std::endl;
	std::cout << "========================================\n" << std::endl;
	
	// Optionally save one-way audio data
	// SaveAudioToFile(audioRawData, "one_way_audio_" + std::to_string(node_id) + ".pcm");
}

void ZoomSdkAudioRawData::onMixedAudioRawDataReceived(AudioRawData* audioRawData)
{
	std::cout << "\n===== MIXED AUDIO DATA RECEIVED ====="  << std::endl;
	std::cout << "  - Buffer Length: " << audioRawData->GetBufferLen() << " bytes" << std::endl;
	std::cout << "  - Sample Rate: " << audioRawData->GetSampleRate() << " Hz" << std::endl;
	std::cout << "  - Channels: " << audioRawData->GetChannelNum() << std::endl;
	
	// Check if buffer is valid
	bool validBuffer = (audioRawData->GetBuffer() != nullptr && audioRawData->GetBufferLen() > 0);
	std::cout << "  - Valid Buffer: " << (validBuffer ? "Yes" : "No") << std::endl;
	
	// Calculate audio duration in milliseconds
	if (validBuffer && audioRawData->GetSampleRate() > 0) {
		float durationMs = (float)audioRawData->GetBufferLen() / 
						 (audioRawData->GetSampleRate() * audioRawData->GetChannelNum() * 2) * 1000; // assuming 16-bit samples
		std::cout << "  - Duration: " << durationMs << " ms" << std::endl;
	}
	
	// Save audio to file if buffer is valid
	if (validBuffer) {
		static std::ofstream pcmFile;
		pcmFile.open("audio.pcm", std::ios::out | std::ios::binary | std::ios::app);

		if (!pcmFile.is_open()) {
			std::cout << "  - ERROR: Failed to open audio.pcm file" << std::endl;
		} else {
			// Write the audio data to the file
			pcmFile.write((char*)audioRawData->GetBuffer(), audioRawData->GetBufferLen());
			pcmFile.close();
			pcmFile.flush();
			std::cout << "  - Saved to audio.pcm file" << std::endl;
			
			// Print first few bytes as hexadecimal for debugging
			std::cout << "  - First 16 bytes: ";
			const unsigned char* buffer = (const unsigned char*)audioRawData->GetBuffer();
			int bytesToShow = std::min(16, (int)audioRawData->GetBufferLen());
			for (int i = 0; i < bytesToShow; i++) {
				char hex[4];
				sprintf(hex, "%02X ", buffer[i]);
				std::cout << hex;
			}
			std::cout << std::endl;
		}
	}
	
	std::cout << "=======================================\n" << std::endl;
}
void ZoomSdkAudioRawData::onShareAudioRawDataReceived(AudioRawData* data_)
{
}

void ZoomSdkAudioRawData::onOneWayInterpreterAudioRawDataReceived(AudioRawData* data_, const zchar_t* pLanguageName)
{
}
