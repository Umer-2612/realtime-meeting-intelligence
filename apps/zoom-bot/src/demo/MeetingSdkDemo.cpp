#include <algorithm>
#include <fstream>
#include <glib.h>
#include <iosfwd>
#include <iostream>
#include <limits.h>
#include <map>
#include <sstream>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/syscall.h>
#include <thread>
#include <unistd.h>

#include "auth_service_interface.h"
#include "meeting_service_components/meeting_audio_interface.h"
#include "meeting_service_components/meeting_participants_ctrl_interface.h"
#include "meeting_service_components/meeting_video_interface.h"
#include "meeting_service_interface.h"
#include "setting_service_interface.h"
#include "zoom_sdk.h"

// used to accept prompts
#include "MeetingReminderEventListener.h"
// used to listen to callbacks from meeting related matters
#include "MeetingServiceEventListener.h"
// used to listen to callbacks from authentication related matters
#include "AuthServiceEventListener.h"
// used for connection helper
#include "NetworkConnectionHandler.h"

// used for event listener
#include "MeetingParticipantsCtrlEventListener.h"
#include "MeetingRecordingCtrlEventListener.h"

// references for enableVideoRawDataCapture
#include "ZoomSdkRenderer.h"
#include "rawdata/rawdata_renderer_interface.h"
#include "rawdata/zoom_rawdata_api.h"

// references for enableAudioRawDataCapture
#include "ZoomSdkAudioRawData.h"
#include "meeting_service_components/meeting_recording_interface.h"

// references for enableVideoRawDataPublishing
#include "ZoomSdkVideoSource.h"

// references for enableAudioRawDataPublishing
#include "ZoomSdkVirtualAudioMicEvent.h"

#include <mutex>

USING_ZOOM_SDK_NAMESPACE

// references for enableAudioRawDataPublishing
const std::string kDefaultAudioSource = "yourwavefile.wav";

// references for enableVideoRawDataPublishing
const std::string kDefaultVideoSource = "yourmp4file.mp4";

GMainLoop *mainLoop;

// These are needed to readsettingsfromTEXT named config.txt
std::string meetingNumber, token, meetingPassword, recordingToken;

// Services which are needed to initialize, authenticate and configure settings for the SDK
ZOOM_SDK_NAMESPACE::IAuthService *m_pAuthService;
ZOOM_SDK_NAMESPACE::IMeetingService *m_pMeetingService;
ZOOM_SDK_NAMESPACE::ISettingService *m_pSettingService;
INetworkConnectionHelper *networkConnectionHelper;

// references for enableVideoRawDataCapture
ZoomSdkRenderer *videoRenderer = new ZoomSdkRenderer();
IZoomSDKRenderer *videoHelper;
IMeetingRecordingController *m_pRecordController;
IMeetingParticipantsController *m_pParticipantsController;

// references for enableAudioRawDataCapture
ZoomSdkAudioRawData *audioRawDataSink = new ZoomSdkAudioRawData();
IZoomSDKAudioRawDataHelper *audioHelper;

// this is used to get a userID, there is no specific proper logic here. It just gets the first userID.
// userID is needed for video subscription.
unsigned int userID;

// this will enable or disable logic to get raw video and raw audio
// do note that this will be overwritten by config.txt
bool enableVideoRawDataCapture = true;
bool enableAudioRawDataCapture = true;
bool enableVideoRawDataPublishing = false;
bool enableAudioRawDataPublishing = false;

// this is a helper method to get the first User ID, it is just an arbitary UserID
uint32_t GetFirstParticipantId() {
    m_pParticipantsController = m_pMeetingService->GetMeetingParticipantsController();
    int returnvalue = m_pParticipantsController->GetParticipantsList()->GetItem(0);
    std::cout << "UserID is : " << returnvalue << std::endl;
    return returnvalue;
}

IUserInfo *GetCurrentUser() {
    m_pParticipantsController = m_pMeetingService->GetMeetingParticipantsController();
    IUserInfo *returnvalue = m_pParticipantsController->GetMySelfUser();
    // std::cout << "UserID is : " << returnvalue << std::endl;
    return returnvalue;
}

// this is a helper method to get the first User Object, it is just an arbitary User Object
IUserInfo *GetFirstParticipant() {
    m_pParticipantsController = m_pMeetingService->GetMeetingParticipantsController();
    int userID = m_pParticipantsController->GetParticipantsList()->GetItem(0);
    IUserInfo *returnvalue = m_pParticipantsController->GetUserByUserID(userID);
    std::cout << "UserID is : " << returnvalue << std::endl;
    return returnvalue;
}

// check if you have permission to start raw recording
void StartRawRecordingIfPermitted(bool isVideo, bool isAudio) {

    if (isVideo || isAudio) {
        m_pRecordController = m_pMeetingService->GetMeetingRecordingController();
        SDKError err2 = m_pMeetingService->GetMeetingRecordingController()->CanStartRawRecording();

        if (err2 == SDKERR_SUCCESS) {
            SDKError err1 = m_pRecordController->StartRawRecording();
            if (err1 != SDKERR_SUCCESS) {
                std::cout << "Error occurred starting raw recording" << std::endl;
            } else {
                // enableVideoRawDataCapture
                if (isVideo) {
                    SDKError err = createRenderer(&videoHelper, videoRenderer);
                    if (err != SDKERR_SUCCESS) {
                        std::cout << "Error occurred" << std::endl;
                        // Handle error
                    } else {
                        std::cout << "attemptToStartRawRecording : subscribing" << std::endl;
                        videoHelper->setRawDataResolution(ZoomSDKResolution_720P);
                        videoHelper->subscribe(GetFirstParticipantId(), RAW_DATA_TYPE_VIDEO);
                    }
                }
                // enableAudioRawDataCapture
                if (isAudio) {
                    audioHelper = GetAudioRawdataHelper();
                    std::cout << "attemptToStartRawRecording : audio helper obtained = " << (audioHelper != nullptr) << std::endl;

                    // Check if HasRawdataLicense returns true
                    bool hasLicense = HasRawdataLicense();
                    std::cout << "Has Raw Data License: " << (hasLicense ? "Yes" : "No") << std::endl;

                    if (audioHelper && audioRawDataSink) {
                        // Try to unsubscribe first in case there's a previous subscription
                        audioHelper->unSubscribe();

                        // Now attempt to subscribe
                        SDKError err = audioHelper->subscribe(audioRawDataSink);
                        std::cout << "Error occurred subscribing to audio : " << err << std::endl;
                        std::cout << "SDKERR_SUCCESS : " << SDKERR_SUCCESS << std::endl;

                        if (err != SDKERR_SUCCESS) {
                            std::cout << "Error occurred subscribing to audio : " << err << std::endl;
                            // Try with interpreter parameter set to true
                            err = audioHelper->subscribe(audioRawDataSink, true);
                            std::cout << "Attempted with interpreter flag: Error = " << err << std::endl;
                        }
                    } else {
                        std::cout << "Error getting audioHelper" << std::endl;
                    }
                }
            }
        } else {
            std::cout << "Cannot start raw recording: no permissions yet, need host, co-host, or recording privilege" << std::endl;
        }
    }
}

// check if you meet the requirements to send raw data
void StartRawDataPublishingIfPermitted(bool isVideo, bool isAudio) {

    // enableVideoRawDataPublishing
    if (isVideo) {

        ZoomSdkVideoSource *virtualVideoSource = new ZoomSdkVideoSource(kDefaultVideoSource);
        IZoomSDKVideoSourceHelper *videoSourceHelper = GetRawdataVideoSourceHelper();

        if (videoSourceHelper) {
            SDKError err = videoSourceHelper->setExternalVideoSource(virtualVideoSource);

            if (err != SDKERR_SUCCESS) {
                printf("attemptToStartRawVideoSending(): Failed to set external video source, error code: %d\n", err);
            } else {
                printf("attemptToStartRawVideoSending(): Success \n");
                IMeetingVideoController *meetingController = m_pMeetingService->GetMeetingVideoController();
                meetingController->UnmuteVideo();
            }
        } else {
            printf("attemptToStartRawVideoSending(): Failed to get video source helper\n");
        }
    }

    // enableAudioRawDataPublishing
    if (isAudio) {
        ZoomSdkVirtualAudioMicEvent *virtualAudioMic = new ZoomSdkVirtualAudioMicEvent(kDefaultAudioSource);
        IZoomSDKAudioRawDataHelper *audioPublishingHelper = GetAudioRawdataHelper();
        if (audioPublishingHelper) {
            SDKError err = audioPublishingHelper->setExternalAudioSource(virtualAudioMic);
        }
    }
}

// callback when given host permission
void HandleHostPrivilege() {
    printf("Is host now...\n");
    StartRawRecordingIfPermitted(enableVideoRawDataCapture, enableAudioRawDataCapture);
}

// callback when given cohost permission
void HandleCoHostPrivilege() {
    printf("Is co-host now...\n");
    StartRawRecordingIfPermitted(enableVideoRawDataCapture, enableAudioRawDataCapture);
}
// callback when given recording permission
void HandleRecordingPermissionGranted() {
    printf("Is given recording permissions now...\n");
    StartRawRecordingIfPermitted(enableVideoRawDataCapture, enableAudioRawDataCapture);
}

void EnableRawDataPublishing() {
    // testing WIP
    if (enableVideoRawDataPublishing) {
        IMeetingVideoController *meetingVidController = m_pMeetingService->GetMeetingVideoController();
        meetingVidController->UnmuteVideo();
    }
    // testing WIP
    if (enableAudioRawDataPublishing) {
        IMeetingAudioController *meetingAudController = m_pMeetingService->GetMeetingAudioController();
        meetingAudController->JoinVoip();
        printf("Is my audio muted: %d\n", GetCurrentUser()->IsAudioMuted());
        meetingAudController->UnMuteAudio(GetCurrentUser()->GetUserID());
    }
}
void DisableRawDataPublishing() {
    // testing WIP
    if (enableVideoRawDataPublishing) {
        IMeetingVideoController *meetingVidController = m_pMeetingService->GetMeetingVideoController();
        meetingVidController->MuteVideo();
    }
    // testing WIP
    if (enableAudioRawDataPublishing) {
        IMeetingAudioController *meetingAudController = m_pMeetingService->GetMeetingAudioController();
        meetingAudController->MuteAudio(GetCurrentUser()->GetUserID(), true);
    }
}

// callback when the SDK is inmeeting
void HandleInMeeting() {

    printf("HandleInMeeting Invoked\n");

    // double check if you are in a meeting
    if (m_pMeetingService->GetMeetingStatus() == ZOOM_SDK_NAMESPACE::MEETING_STATUS_INMEETING) {
        printf("In Meeting Now...\n");

        // print all list of participants
        IList<unsigned int> *participants = m_pMeetingService->GetMeetingParticipantsController()->GetParticipantsList();
        printf("Participants count: %d\n", participants->GetCount());
    }

    // first attempt to start raw recording  / sending, upon successfully joined and achieved "in-meeting" state.
    StartRawRecordingIfPermitted(enableVideoRawDataCapture, enableAudioRawDataCapture);
    StartRawDataPublishingIfPermitted(enableVideoRawDataPublishing, enableAudioRawDataPublishing);
}

// on meeting ended, typically by host, do something here. it is possible to reuse this SDK instance
void HandleMeetingEnded() {
    // ShutdownSdk();
    // std::exit(0);
}

void HandleMeetingJoined() {

    printf("Joining Meeting...\n");
}

// get path, helper method used to read json config file
std::string GetExecutableDirectory() {
    char dest[PATH_MAX];
    memset(dest, 0, sizeof(dest)); // readlink does not null terminate!
    if (readlink("/proc/self/exe", dest, PATH_MAX) == -1) {
    }
    char *tmp = strrchr(dest, '/');
    if (tmp)
        *tmp = 0;
    printf("getpath\n");
    return std::string(dest);
}

// Function to process a line containing a key-value pair
void ParseConfigLine(const std::string &line, std::map<std::string, std::string> &config) {
    // Find the position of the ':' character
    size_t colonPos = line.find(':');

    if (colonPos != std::string::npos) {
        // Extract the key and value parts
        std::string key = line.substr(0, colonPos);
        std::string value = line.substr(colonPos + 1);

        // Remove leading/trailing whitespaces from the key and value
        key.erase(0, key.find_first_not_of(" \t"));
        key.erase(key.find_last_not_of(" \t") + 1);
        value.erase(0, value.find_first_not_of(" \t"));
        value.erase(value.find_last_not_of(" \t") + 1);

        // Remove double-quote characters and carriage return ('\r') from the value
        value.erase(std::remove_if(value.begin(), value.end(), [](char c) { return c == '"' || c == '\r'; }), value.end());

        // Store the key-value pair in the map
        config[key] = value;
    }
}

void LoadConfiguration() {

    std::string self_dir = GetExecutableDirectory();
    printf("self path: %s\n", self_dir.c_str());
    self_dir.append("/config.txt");

    std::ifstream configFile(self_dir.c_str());
    if (!configFile) {
        std::cerr << "Error opening config file." << std::endl;
    } else {

        std::cerr << "Readfile success." << std::endl;
    }

    std::map<std::string, std::string> config;
    std::string line;

    while (std::getline(configFile, line)) {
        // Process each line to extract key-value pairs
        ParseConfigLine(line, config);

        std::cerr << "Reading.." << line << std::endl;
    }

    // Example: Accessing values by key
    if (config.find("meetingNumber") != config.end()) {

        meetingNumber = config["meetingNumber"];
        std::cout << "Meeting Number: " << config["meetingNumber"] << std::endl;
    }
    if (config.find("token") != config.end()) {
        token = config["token"];
        std::cout << "Token: " << token << std::endl;
    }
    if (config.find("meetingPassword") != config.end()) {

        meetingPassword = config["meetingPassword"];
        std::cout << "meetingPassword: " << meetingPassword << std::endl;
    }
    if (config.find("recordingToken") != config.end()) {

        recordingToken = config["recordingToken"];
        std::cout << "recordingToken: " << recordingToken << std::endl;
    }
    if (config.find("enableVideoRawDataCapture") != config.end()) {
        std::cout << "enableVideoRawDataCapture before parsing is : " << config["enableVideoRawDataCapture"] << std::endl;

        if (config["enableVideoRawDataCapture"] == "true") {
            enableVideoRawDataCapture = true;
        } else {
            enableVideoRawDataCapture = false;
        }
        std::cout << "enableVideoRawDataCapture: " << enableVideoRawDataCapture << std::endl;
    }
    if (config.find("enableAudioRawDataCapture") != config.end()) {
        std::cout << "enableAudioRawDataCapture before parsing is : " << config["enableAudioRawDataCapture"] << std::endl;

        if (config["enableAudioRawDataCapture"] == "true") {
            enableAudioRawDataCapture = true;
        } else {
            enableAudioRawDataCapture = false;
        }
        std::cout << "enableAudioRawDataCapture: " << enableAudioRawDataCapture << std::endl;
    }

    if (config.find("enableVideoRawDataPublishing") != config.end()) {
        std::cout << "enableVideoRawDataPublishing before parsing is : " << config["enableVideoRawDataPublishing"] << std::endl;

        if (config["enableVideoRawDataPublishing"] == "true") {
            enableVideoRawDataPublishing = true;
        } else {
            enableVideoRawDataPublishing = false;
        }
        std::cout << "enableVideoRawDataPublishing: " << enableVideoRawDataPublishing << std::endl;
    }
    if (config.find("enableAudioRawDataPublishing") != config.end()) {
        std::cout << "enableAudioRawDataPublishing before parsing is : " << config["enableAudioRawDataPublishing"] << std::endl;

        if (config["enableAudioRawDataPublishing"] == "true") {
            enableAudioRawDataPublishing = true;
        } else {
            enableAudioRawDataPublishing = false;
        }
        std::cout << "enableAudioRawDataPublishing: " << enableAudioRawDataPublishing << std::endl;
    }

    // Additional processing or handling of parsed values can be done here

    printf("directory of config file: %s\n", self_dir.c_str());
}

void ShutdownSdk() {
    ZOOM_SDK_NAMESPACE::SDKError err(ZOOM_SDK_NAMESPACE::SDKERR_SUCCESS);

    if (m_pAuthService) {
        ZOOM_SDK_NAMESPACE::DestroyAuthService(m_pAuthService);
        m_pAuthService = NULL;
    }
    if (m_pSettingService) {
        ZOOM_SDK_NAMESPACE::DestroySettingService(m_pSettingService);
        m_pSettingService = NULL;
    }
    if (m_pMeetingService) {
        ZOOM_SDK_NAMESPACE::DestroyMeetingService(m_pMeetingService);
        m_pMeetingService = NULL;
    }
    if (videoHelper) {
        videoHelper->unSubscribe();
    }
    if (audioPublishingHelper) {
        audioHelper->unSubscribe();
    }
    // if (networkConnectionHelper)
    //{
    //	ZOOM_SDK_NAMESPACE::DestroyNetworkConnectionHelper(networkConnectionHelper);
    //	networkConnectionHelper = NULL;
    // }
    // attempt to clean up SDK
    err = ZOOM_SDK_NAMESPACE::CleanUPSDK();
    if (err != ZOOM_SDK_NAMESPACE::SDKERR_SUCCESS) {
        std::cerr << "ShutdownSdk meetingSdk:error " << std::endl;
    } else {
        std::cerr << "ShutdownSdk meetingSdk:success" << std::endl;
    }
}

void ConfigureAudioDevices() {
    ZOOM_SDK_NAMESPACE::IAudioSettingContext *pAudioContext = m_pSettingService->GetAudioSettings();
    if (pAudioContext) {
        // setting speaker
        // if there are speakers detected
        if (pAudioContext->GetSpeakerList()->GetCount() >= 1) {
            std::cout << "Number of speaker(s) : " << pAudioContext->GetSpeakerList()->GetCount() << std::endl;
            ISpeakerInfo *sInfo = pAudioContext->GetSpeakerList()->GetItem(0);
            const zchar_t *deviceName = sInfo->GetDeviceName();

            // set speaker
            if (deviceName != nullptr && deviceName[0] != '\0') {
                std::cout << "Speaker(0) name : " << sInfo->GetDeviceName() << std::endl;
                std::cout << "Speaker(0) id : " << sInfo->GetDeviceId() << std::endl;
                pAudioContext->SelectSpeaker(sInfo->GetDeviceId(), sInfo->GetDeviceName());
                std::cout << "Is selected speaker? : " << pAudioContext->GetSpeakerList()->GetItem(0)->IsSelectedDevice() << std::endl;
            } else {
                std::cout << "Speaker(0) name is empty or null." << std::endl;
                std::cout << "Speaker(0) id is empty or null." << std::endl;
            }
        }

        // setting microphone
        // if there are microphone detected
        if (pAudioContext->GetMicList()->GetCount() >= 1) {
            IMicInfo *mInfo = pAudioContext->GetMicList()->GetItem(0);
            std::cout << "Number of mic(s) : " << pAudioContext->GetMicList()->GetCount() << std::endl;
            const zchar_t *deviceName = mInfo->GetDeviceName();

            // set microphone
            if (deviceName != nullptr && deviceName[0] != '\0') {
                std::cout << "Mic(0) name : " << mInfo->GetDeviceName() << std::endl;
                std::cout << "Mic(0) id : " << mInfo->GetDeviceId() << std::endl;
                pAudioContext->SelectMic(mInfo->GetDeviceId(), mInfo->GetDeviceName());
                std::cout << "Is selected Mic? : " << pAudioContext->GetMicList()->GetItem(0)->IsSelectedDevice() << std::endl;
            } else {
                std::cout << "Mic(0) name is empty or null." << std::endl;
                std::cout << "Mic(0) id is empty or null." << std::endl;
            }
        }
    }
}

void JoinMeetingSession() {
    std::cerr << "Joining Meeting" << std::endl;
    SDKError err2(SDKError::SDKERR_SUCCESS);

    // try to create the meetingservice object,
    // this object will be used to join the meeting
    if ((err2 = CreateMeetingService(&m_pMeetingService)) != SDKError::SDKERR_SUCCESS) {
    };
    std::cerr << "MeetingService created." << std::endl;

    // before joining a meeting, create the setting service
    // this object is used to for settings
    CreateSettingService(&m_pSettingService);
    std::cerr << "Settingservice created." << std::endl;

    // Set the event listener for meeting status
    m_pMeetingService->SetEvent(new MeetingServiceEventListener(&HandleMeetingJoined, &HandleMeetingEnded, &HandleInMeeting));

    // Set the event listener for host, co-host
    m_pParticipantsController = m_pMeetingService->GetMeetingParticipantsController();
    m_pParticipantsController->SetEvent(new MeetingParticipantsCtrlEventListener(&HandleHostPrivilege, &HandleCoHostPrivilege));

    // Set the event listener for recording privilege status
    m_pRecordController = m_pMeetingService->GetMeetingRecordingController();
    m_pRecordController->SetEvent(new MeetingRecordingCtrlEventListener(&HandleRecordingPermissionGranted));

    // set event listnener for prompt handler
    IMeetingReminderController *meetingremindercontroller = m_pMeetingService->GetMeetingReminderController();
    MeetingReminderEventListener *meetingremindereventlistener = new MeetingReminderEventListener();
    meetingremindercontroller->SetEvent(meetingremindereventlistener);

    // prepare params used for joining meeting
    ZOOM_SDK_NAMESPACE::JoinParam joinParam;
    ZOOM_SDK_NAMESPACE::SDKError err(ZOOM_SDK_NAMESPACE::SDKERR_SERVICE_FAILED);
    joinParam.userType = ZOOM_SDK_NAMESPACE::SDK_UT_WITHOUT_LOGIN;
    ZOOM_SDK_NAMESPACE::JoinParam4WithoutLogin &withoutloginParam = joinParam.param.withoutloginuserJoin;
    // withoutloginParam.meetingNumber = 1231231234;
    withoutloginParam.meetingNumber = std::stoull(meetingNumber);
    withoutloginParam.vanityID = NULL;
    withoutloginParam.userName = "LinuxChun";
    // withoutloginParam.psw = "1";
    withoutloginParam.psw = meetingPassword.c_str();
    withoutloginParam.customer_key = NULL;
    withoutloginParam.webinarToken = NULL;
    withoutloginParam.isVideoOff = false;
    withoutloginParam.isAudioOff = false;

    std::cerr << "JWT token is " << token << std::endl;
    std::cerr << "Recording token is " << recordingToken << std::endl;

    // automatically set app_privilege token if it is present in config.txt, or retrieved from web service
    withoutloginParam.app_privilege_token = NULL;
    if (!recordingToken.size() == 0) {
        withoutloginParam.app_privilege_token = recordingToken.c_str();
        std::cerr << "Setting recording token" << std::endl;
    } else {
        withoutloginParam.app_privilege_token = NULL;
        std::cerr << "Leaving recording token as NULL" << std::endl;
    }

    if (enableAudioRawDataCapture) {
        // set join audio to true
        ZOOM_SDK_NAMESPACE::IAudioSettingContext *pAudioContext = m_pSettingService->GetAudioSettings();
        if (pAudioContext) {
            // ensure auto join audio
            pAudioContext->EnableAutoJoinAudio(true);
        }
    }
    if (enableVideoRawDataPublishing) {

        // ensure video is turned on
        withoutloginParam.isVideoOff = false;
        // set join video to true
        ZOOM_SDK_NAMESPACE::IVideoSettingContext *pVideoContext = m_pSettingService->GetVideoSettings();
        if (pVideoContext) {
            pVideoContext->EnableAutoTurnOffVideoWhenJoinMeetingSession(false);
        }
    }
    if (enableAudioRawDataPublishing) {

        ZOOM_SDK_NAMESPACE::IAudioSettingContext *pAudioContext = m_pSettingService->GetAudioSettings();
        if (pAudioContext) {
            // ensure auto join audio
            pAudioContext->EnableAutoJoinAudio(true);
            pAudioContext->EnableAlwaysMuteMicWhenJoinVoip(true);
            pAudioContext->SetSuppressBackgroundNoiseLevel(Suppress_BGNoise_Level_None);
        }
    }

    // attempt to join meeting
    if (m_pMeetingService) {
        err = m_pMeetingService->Join(joinParam);
    } else {
        std::cout << "join_meeting m_pMeetingService:Null" << std::endl;
    }

    if (ZOOM_SDK_NAMESPACE::SDKERR_SUCCESS == err) {
        std::cout << "join_meeting:success" << std::endl;
    } else {
        std::cout << "join_meeting:error" << std::endl;
    }
}

void LeaveMeetingSession() {
    ZOOM_SDK_NAMESPACE::MeetingStatus status = ZOOM_SDK_NAMESPACE::MEETING_STATUS_FAILED;

    if (NULL == m_pMeetingService) {

        std::cout << "leave_meeting m_pMeetingService:Null" << std::endl;

    } else {
        status = m_pMeetingService->GetMeetingStatus();
    }

    if (status == ZOOM_SDK_NAMESPACE::MEETING_STATUS_IDLE ||
        status == ZOOM_SDK_NAMESPACE::MEETING_STATUS_ENDED ||
        status == ZOOM_SDK_NAMESPACE::MEETING_STATUS_FAILED) {

        std::cout << "LeaveMeetingSession() not in meeting " << std::endl;
    }

    if (SDKError::SDKERR_SUCCESS == m_pMeetingService->Leave(ZOOM_SDK_NAMESPACE::LEAVE_MEETING)) {
        std::cout << "LeaveMeetingSession() success " << std::endl;

    } else {
        std::cout << "LeaveMeetingSession() error" << std::endl;
    }
}

// callback when authentication is compeleted
void HandleAuthenticationComplete() {
    std::cout << "HandleAuthenticationComplete" << std::endl;
    JoinMeetingSession();
}

void AuthenticateMeetingSdk() {
    SDKError err(SDKError::SDKERR_SUCCESS);

    // create auth service
    if ((err = CreateAuthService(&m_pAuthService)) != SDKError::SDKERR_SUCCESS) {
    };
    std::cerr << "AuthService created." << std::endl;

    // Create a param to insert jwt token
    ZOOM_SDK_NAMESPACE::AuthContext param;

    // set the event listener for onauthenticationcompleted
    if ((err = m_pAuthService->SetEvent(new AuthServiceEventListener(&HandleAuthenticationComplete))) != SDKError::SDKERR_SUCCESS) {
    };
    std::cout << "AuthServiceEventListener added." << std::endl;

    if (!token.size() == 0) {
        param.jwt_token = token.c_str();
        std::cerr << "AuthSDK:token extracted from config file " << param.jwt_token << std::endl;
    }
    m_pAuthService->SDKAuth(param);
    ////attempt to authenticate
    // ZOOM_SDK_NAMESPACE::SDKError sdkErrorResult = m_pAuthService->SDKAuth(param);

    // if (ZOOM_SDK_NAMESPACE::SDKERR_SUCCESS != sdkErrorResult){
    //	std::cerr << "AuthSDK:error " << std::endl;
    // }
    // else{
    //	std::cerr << "AuthSDK:send success, awaiting callback " << std::endl;
    // }
}

void InitializeMeetingSdk() {
    ZOOM_SDK_NAMESPACE::SDKError err(ZOOM_SDK_NAMESPACE::SDKERR_SUCCESS);
    ZOOM_SDK_NAMESPACE::InitParam initParam;

    // set domain
    initParam.strWebDomain = "https://zoom.us";
    initParam.strSupportUrl = "https://zoom.us";

    // set language id
    initParam.emLanguageID = ZOOM_SDK_NAMESPACE::LANGUAGE_English;

    // set logging perferences
    initParam.enableLogByDefault = true;
    initParam.enableGenerateDump = true;

    // attempt to initialize
    err = ZOOM_SDK_NAMESPACE::InitSDK(initParam);
    if (err != ZOOM_SDK_NAMESPACE::SDKERR_SUCCESS) {
        std::cerr << "Init meetingSdk:error " << std::endl;
    } else {
        std::cerr << "Init meetingSdk:success" << std::endl;
    }

    // use connection helper
    // if ((err = CreateNetworkConnectionHelper(&networkConnectionHelper)) == SDKError::SDKERR_SUCCESS) {
    //	std::cout << "CreateNetworkConnectionHelper created." << std::endl;
    // }
    // if ((err = networkConnectionHelper->RegisterNetworkConnectionHandler(new NetworkConnectionHandler(&AuthenticateMeetingSdk))) == SDKError::SDKERR_SUCCESS) {
    //	std::cout << "NetworkConnectionHandler registered. Detecting proxy." << std::endl;
    // }
}

// used for non headless app

void StartMeetingSession() {

    ZOOM_SDK_NAMESPACE::StartParam startParam;
    startParam.userType = ZOOM_SDK_NAMESPACE::SDK_UT_NORMALUSER;
    startParam.param.normaluserStart.vanityID = NULL;
    startParam.param.normaluserStart.customer_key = NULL;
    startParam.param.normaluserStart.isVideoOff = false;
    startParam.param.normaluserStart.isAudioOff = false;

    ZOOM_SDK_NAMESPACE::SDKError err = m_pMeetingService->Start(startParam);
    if (SDKError::SDKERR_SUCCESS == err) {
        std::cerr << "StartMeetingSession:success " << std::endl;
    } else {
        std::cerr << "StartMeetingSession:error " << std::endl;
    }
}

// Define a struct to hold the response data
struct ResponseData {
    std::ostringstream stream;
};

// Callback function to write response data into the stringstream
static size_t WriteHttpResponse(void *contents, size_t size, size_t nmemb, void *userp) {
    size_t totalSize = size * nmemb;
    ResponseData *response = static_cast<ResponseData *>(userp);
    response->stream.write(static_cast<const char *>(contents), totalSize);
    return totalSize;
}

gboolean HandleTimeout(gpointer data) {
    return TRUE;
}

// this catches a break signal, such as Ctrl + C
void HandleSignal(int s) {
    printf("\nCaught signal %d\n", s);
    LeaveMeetingSession();
    printf("Leaving session.\n");
    ShutdownSdk();

    // InitializeMeetingSdk();
    // AuthenticateMeetingSdk();

    std::exit(0);
}

void InitializeApplicationSettings() {
    struct sigaction sigIntHandler;
    sigIntHandler.sa_handler = HandleSignal;
    sigemptyset(&sigIntHandler.sa_mask);
    sigIntHandler.sa_flags = 0;
    sigaction(SIGINT, &sigIntHandler, NULL);
}

int main(int argc, char *argv[]) {

    LoadConfiguration();

    InitializeMeetingSdk();
    AuthenticateMeetingSdk();
    InitializeApplicationSettings();

    mainLoop = g_main_loop_new(NULL, FALSE);
    // add source to default context
    g_timeout_add(1000, HandleTimeout, mainLoop);
    g_main_loop_run(mainLoop);
    return 0;
}
