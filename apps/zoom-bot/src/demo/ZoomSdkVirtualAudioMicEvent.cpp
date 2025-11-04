// Virtual audio microphone event handler
#include <iostream>
#include <cstdint>
#include <fstream>
#include <cstring>
#include <cstdio>
#include <vector>

#include "rawdata/rawdata_audio_helper_interface.h"
#include "ZoomSdkVirtualAudioMicEvent.h"
#include "zoom_sdk_def.h" 

#include <thread>
#include <chrono>  // for sleep

using namespace std;
using namespace ZOOM_SDK_NAMESPACE;

int audio_play_flag = -1;



void PlayAudioFileToVirtualMic(IZoomSDKAudioRawDataSender* audio_sender, string audio_source)
{
	// execute in a thread.
	while (audio_play_flag > 0 && audio_sender) {

		// Check if the file exists
		ifstream file(audio_source, ios::binary | ios::ate);
		if (!file.is_open()) {
			std::cout << "Error: File not found. Tried to open " << audio_source << std::endl;
			return;
		}

		// Get the file size
		int file_size = file.tellg();

		// Read the file into a buffer
		vector<char> buffer(file_size);
		file.seekg(0, ios::beg);
		file.read(buffer.data(), file_size);

		// Send the audio data to the virtual camera
		SDKError err = audio_sender->send(buffer.data(), buffer.size(), 44100);
		if (err != SDKERR_SUCCESS) {
			std::cout << "Error: Failed to send audio data to virtual mic. Error code: " << err << std::endl;
			return;
		}
		file.close();
		// Sleep for a while before replaying (adjust the delay as needed)
		std::this_thread::sleep_for(std::chrono::milliseconds(10000)); // 10 second delay, this is a 10 second long wave file

		//audio_play_flag = -1;
	}
}

/// \brief Callback for virtual audio mic to do some initialization.
/// \param pSender, You can send audio data based on this object, see \link IZoomSDKAudioRawDataSender \endlink.
void ZoomSdkVirtualAudioMicEvent::onMicInitialize(IZoomSDKAudioRawDataSender* pSender) {
	//pSender->send();	pSender_ = pSender;
	printf("ZoomSdkVirtualAudioMicEvent OnMicInitialize, waiting for turnOn chat command\n");
}

/// \brief Callback for virtual audio mic can send raw data with 'pSender'.
void ZoomSdkVirtualAudioMicEvent::onMicStartSend() {

	printf("onMicStartSend\n");
	std::cout << "onStartSend" << std::endl;
	if (pSender_ && audio_play_flag != 1) {
		while (audio_play_flag > -1) {}
		audio_play_flag = 1;
		thread(PlayAudioFileToVirtualMic, pSender_, audio_source_).detach();

	}
}

/// \brief Callback for virtual audio mic should stop send raw data.
void ZoomSdkVirtualAudioMicEvent::onMicStopSend() {
	printf("onMicStopSend\n");
	audio_play_flag = 0;
}
/// \brief Callback for virtual audio mic is uninitialized.
void ZoomSdkVirtualAudioMicEvent::onMicUninitialized() {
	std::cout << "onUninitialized" << std::endl;
	pSender_ = nullptr;
}

ZoomSdkVirtualAudioMicEvent::ZoomSdkVirtualAudioMicEvent(std::string audio_source)
{
	audio_source_ = audio_source;
}
