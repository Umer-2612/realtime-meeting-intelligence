// Video raw data capture handler

#include "ZoomSdkRenderer.h"
#include "rawdata/rawdata_video_source_helper_interface.h"
#include "zoom_sdk_def.h"
#include <iostream>

#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <fstream>
#include <string>

void ZoomSdkRenderer::onRawDataFrameReceived(YUVRawDataI420 *data) {
    std::cout << "\n===== VIDEO FRAME RECEIVED =====" << std::endl;
    std::cout << "  - Width: " << data->GetStreamWidth() << "px" << std::endl;
    std::cout << "  - Height: " << data->GetStreamHeight() << "px" << std::endl;
    std::cout << "  - Y Buffer Size: " << (data->GetStreamWidth() * data->GetStreamHeight()) << " bytes" << std::endl;
    std::cout << "  - U Buffer Size: " << (data->GetStreamWidth() * data->GetStreamHeight() / 4) << " bytes" << std::endl;
    std::cout << "  - V Buffer Size: " << (data->GetStreamWidth() * data->GetStreamHeight() / 4) << " bytes" << std::endl;
    std::cout << "  - Total Frame Size: " << (data->GetStreamWidth() * data->GetStreamHeight() * 3 / 2) << " bytes" << std::endl;

    // Check if the data is valid
    bool hasValidData = (data->GetYBuffer() != nullptr && data->GetUBuffer() != nullptr && data->GetVBuffer() != nullptr);
    std::cout << "  - Has Valid Data: " << (hasValidData ? "Yes" : "No") << std::endl;

    if (data->GetStreamHeight() == 720) {
        SaveToRawYUVFile(data);
        std::cout << "  - Frame saved to output.yuv file" << std::endl;
    } else {
        std::cout << "  - Frame not saved (not 720p)" << std::endl;
    }
    std::cout << "================================\n"
              << std::endl;
}
void ZoomSdkRenderer::onRawDataStatusChanged(RawDataStatus status) {
    std::cout << "\n===== VIDEO RAW DATA STATUS CHANGED =====" << std::endl;
    std::cout << "  - Status Code: " << (int)status << std::endl;

    // Just print the status value without enum comparison
    if ((int)status == 0) {
        std::cout << "  - Raw data is now OFF" << std::endl;
    } else if ((int)status == 1) {
        std::cout << "  - Raw data is now ON" << std::endl;
    } else {
        std::cout << "  - Unknown status" << std::endl;
    }
    std::cout << "========================================\n"
              << std::endl;
}

void ZoomSdkRenderer::onRendererBeDestroyed() {
    std::cout << "onRendererBeDestroyed ." << std::endl;
}

void ZoomSdkRenderer::SaveToRawYUVFile(YUVRawDataI420 *data) {

    // method 1

    //// Open the file for writing
    // std::ofstream outputFile("output.yuv", std::ios::out | std::ios::binary | std::ios::app);
    // if (!outputFile.is_open())
    //{
    //	//error opening file
    //	return;
    // }

    // char* _data = new char[data->GetStreamHeight() * data->GetStreamWidth() * 3 / 2];

    // memset(_data, 0, data->GetStreamHeight() * data->GetStreamWidth() * 3 / 2);

    //// Copy Y buffer
    // memcpy(_data, data->GetYBuffer(), data->GetStreamHeight() * data->GetStreamWidth());

    //// Copy U buffer
    // size_t loc = data->GetStreamHeight() * data->GetStreamWidth();
    // memcpy(&_data[loc], data->GetUBuffer(), data->GetStreamHeight() * data->GetStreamWidth() / 4);

    //// Copy V buffer
    // loc = (data->GetStreamHeight() * data->GetStreamWidth()) + (data->GetStreamHeight() * data->GetStreamWidth() / 4);
    // memcpy(&_data[loc], data->GetVBuffer(), data->GetStreamHeight() * data->GetStreamWidth() / 4);

    ////outputFile.write((char*)data->GetBuffer(), data->GetBufferLen());
    //// Write the Y plane
    // outputFile.write(_data, data->GetStreamHeight() * data->GetStreamWidth() * 3 / 2);

    //// Close the file
    // outputFile.close();
    // outputFile.flush();
    ////cout << "YUV420 buffer saved to file." << endl;
    // std::cout << "Saving Raw Data" << std::endl;

    // method 2

    // Open the file for writing
    std::ofstream outputFile("output.yuv", std::ios::out | std::ios::binary | std::ios::app);
    if (!outputFile.is_open()) {
        std::cout << "Error opening file." << std::endl;
        return;
    }
    // Calculate the sizes for Y, U, and V components
    size_t ySize = data->GetStreamWidth() * data->GetStreamHeight();
    size_t uvSize = ySize / 4;

    // Write Y, U, and V components to the output file
    outputFile.write(data->GetYBuffer(), ySize);
    outputFile.write(data->GetUBuffer(), uvSize);
    outputFile.write(data->GetVBuffer(), uvSize);

    // Close the file
    outputFile.close();
    outputFile.flush();
    // cout << "YUV420 buffer saved to file." << endl;
}
