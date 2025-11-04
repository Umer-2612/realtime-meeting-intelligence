// Video raw data publisher

#include "ZoomSdkVideoSource.h"
#include <iostream>
#include <thread> 
#include <iostream>
#include <string>
#include <cstdio>
#include <chrono>


//using namespace cv;
using namespace std;

int video_play_flag = -1;
int width = WIDTH;
int height = HEIGHT;



void PlayVideoFileToVirtualCamera(IZoomSDKVideoSender* video_sender, const std::string& video_source) {

    //implement your code to read from a file, and send it using video_sender
    // you can use ffmpeg to convert your video file into yuv420p format, read the frames and send each frame using video_sender

}
void ZoomSdkVideoSource::onInitialize(IZoomSDKVideoSender* sender, IList<VideoSourceCapability>* support_cap_list, VideoSourceCapability& suggest_cap)
{
    std::cout << "ZoomSdkVideoSource onInitialize waiting for turnOn chat command" << endl;
    video_sender_ = sender;
}

void ZoomSdkVideoSource::onPropertyChange(IList<VideoSourceCapability>* support_cap_list, VideoSourceCapability suggest_cap)
{
    std::cout << "onPropertyChange" << endl;
    std::cout << "suggest frame: " << suggest_cap.frame << endl;
    std::cout << "suggest size: " << suggest_cap.width << "x" << suggest_cap.height << endl;
    width = suggest_cap.width;
    height = suggest_cap.height;
    std::cout << "calculated frameLen: " << height / 2 * 3 * width << endl;
}

void ZoomSdkVideoSource::onStartSend()
{
    std::cout << "onStartSend" << endl;
    if (video_sender_ && video_play_flag != 1) {
        while (video_play_flag > -1) {}
        video_play_flag = 1;
        thread(PlayVideoFileToVirtualCamera, video_sender_, video_source_).detach();
    }
    else {
        std::cout << "video_sender_ is null" << endl;
    }
}

void ZoomSdkVideoSource::onStopSend()
{
    std::cout << "onCameraStopSend" << endl;
    video_play_flag = 0;
}

void ZoomSdkVideoSource::onUninitialized()
{
    std::cout << "onUninitialized" << endl;
    video_sender_ = nullptr;
}

ZoomSdkVideoSource::ZoomSdkVideoSource(string video_source)
{
    video_source_ = video_source;
}

