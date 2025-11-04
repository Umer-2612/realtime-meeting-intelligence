# Zoom Meeting Bot

This module hosts the Zoom Meeting SDK Linux demo and associated wrappers that unlock raw audio/video feeds for downstream analytics. The code lives under `apps/zoom-bot/src/demo`.

## Prerequisites

1. Active [Zoom account](https://zoom.us) with Meeting SDK credentials.
2. Meeting SDK package downloaded from the [Zoom Marketplace](https://marketplace.zoom.us).
3. Authentication token signed with:
   - Meeting SDK Client ID
   - Meeting SDK Client Secret
4. Optional [recording token](https://developers.zoom.us/docs/meeting-sdk/apis/#operation/meetingLocalRecordingJoinToken).

## Repository Layout

```
apps/zoom-bot/
 ├── docker/                     # Dockerfiles (Ubuntu multi-stage and legacy image)
 ├── k8s/                        # Deployment manifests (refer to infrastructure/k8s instead)
 └── src/demo/                   # Zoom SDK demo application
```

## Preparing the SDK Assets

1. Download `zoom-meeting-sdk-linux_x86_64-<version>.tar` from the Marketplace.
2. Copy extracted artifacts into the demo folder:
   - `h` headers → `apps/zoom-bot/src/demo/include/h`
   - All shared libs (`lib*.so`) → `apps/zoom-bot/src/demo/lib/zoom_meeting_sdk`
   - `qt_libs` directory → `apps/zoom-bot/src/demo/lib/zoom_meeting_sdk/qt_libs`
   - `translations.json` → `apps/zoom-bot/src/demo/lib/zoom_meeting_sdk/json`
3. Create the symbolic link inside `lib/zoom_meeting_sdk`:
   ```bash
   ln -sf libmeetingsdk.so libmeetingsdk.so.1
   ```
4. Update `apps/zoom-bot/src/demo/config.txt` with your Meeting SDK keys, meeting number, and configuration flags:
   - `GetVideoRawData`
   - `GetAudioRawData`
   - `SendVideoRawData`
   - `SendAudioRawData`

## Local Build

```bash
cd apps/zoom-bot/src/demo
mkdir -p build && cd build
cmake ..
cmake --build .
```

The resulting binaries and SDK assets are emitted to `apps/zoom-bot/src/demo/bin`.

## Docker Image

A production-friendly Dockerfile is available at `apps/zoom-bot/docker/Dockerfile`.

```bash
docker build -f apps/zoom-bot/docker/Dockerfile -t zoom-bot:dev apps/zoom-bot
```

The container copies the compiled demo plus SDK libraries into `/opt/zoom-bot/bin` and is designed to run on Linux nodes within AWS EKS (see `infrastructure/k8s/base/apps/zoom-bot-deployment.yaml`).

## Security Notes

- Never hardcode Zoom credentials inside source control. Inject them via Kubernetes Secrets (`realtime-meeting-secrets`) or your secret store of choice.
- Rotate SDK keys regularly and enable audit logging on the Zoom account.

## Reference

- [Zoom Meeting SDK Documentation](https://developers.zoom.us/docs/meeting-sdk/)
- [Zoom Meeting SDK Sample Apps](https://github.com/zoom/meetingsdk-sample-signature-node.js)

```

meeting_number: "1234567890"
token: "xxxxxxxx.yyyyyyyyyyyyyyyyyy.zzzzzzzzzzzzzzzzz"
meeting_password: "123456"
recording_token: ""
GetVideoRawData: "true"
GetAudioRawData: "true"
SendVideoRawData: "false"
SendAudioRawData: "false"

```
2. You need to fill in `withoutloginParam.userName = "";`  in `MeetingSdkDemo.cpp`. `MeetingSdkDemo.cpp` is the starting point of the demo app.
3. Once init sdk and auth sdk are successfully executed, only then you can join and leave meeting.
4. Once you have successfully joined a meeting, subscribing to raw audio and raw video requires either
    1. either host/co-host/local-recording rights or 
    2. meeting recording token
5. You will need a file named `~\.config.us\zoomus.conf` before you can access raw audio in an environment without soundcard, such as docker and WSL environment. The sample zoomus.conf file is generated in the `setup-pulseaudio.sh` files
6. For Sending Raw Audio and Raw Video, there are 2 methods (`turnOnSendVideoAndAudio` and `turnOnSendVideoAndAudio`) which you will need to call 
## CMakeLists.txt
If you have additional cpp and/or .h files, you might need to be included in this CMakeLists.txt

```
add_executable(MeetingSdkDemo 
              ${CMAKE_SOURCE_DIR}/MeetingSdkDemo.cpp
              ${CMAKE_SOURCE_DIR}/init_auth_sdk_workflow.cpp
              ${CMAKE_SOURCE_DIR}/meeting_sdk_util.cpp
              ${CMAKE_SOURCE_DIR}/init_auth_sdk_workflow.h
              ${CMAKE_SOURCE_DIR}/meeting_sdk_util.h
              ${CMAKE_SOURCE_DIR}/RegressionTestRawdataRender.cpp
              ${CMAKE_SOURCE_DIR}/RegressionTestRawdataRender.h
              ${CMAKE_SOURCE_DIR}/custom_ui_eventSink.h
              ${CMAKE_SOURCE_DIR}/custom_ui_eventSink.cpp
              ${CMAKE_SOURCE_DIR}/MeetingReminderEventListener.h
              ${CMAKE_SOURCE_DIR}/MeetingReminderEventListener.cpp
              )
```

Similarly for libraries

```
target_link_libraries(MeetingSdkDemo gcc_s gcc)
target_link_libraries(MeetingSdkDemo meetingsdk)
target_link_libraries(MeetingSdkDemo glib-2.0)
target_link_libraries(MeetingSdkDemo pthread)
```

# Docker

The sample app has been tested on Centos 8, Centos 9, Ubuntu 22 and Ubuntu 23.
Here are some dependencies you will need to include.


## Centos

This is tested on WSL and Docker centos 9 and Centos 8
Here are some dependencies to get it to run on Centos.
Some packages might be redundant, and has not been optimized.


#### Install files for compiling
```
sudo yum install cmake
sudo yum install gcc gcc-c++
```

#### Enable the CodeReady Linux Builder repository
```
sudo dnf config-manager --set-enabled crb
```

#### Install the EPEL RPM.

```
sudo dnf install epel-release epel-next-release
```


#### CURL related dependencies
 ```
 yum install -y openssl-devel
 yum install -y libcurl-devel 
 ```

#### Install Pulseaudio
``` 
yum install -y  pulseaudio pulseaudio-utils 
```

#### If you encounter: Fatal error: SDL2/SDL.h: No such file or directory. This is no longer in used at code level, but leaving this here for legacy support purposes
```
sudo yum -y install SDL2-devel
```

#### If you encounter these error Messages
#/usr/bin/ld: warning: libxcb-image.so.0, needed by /root/release_demo/demo/libmeetingsdk.so, not found (try using -rpath or -rpath-link)
#/usr/bin/ld: warning: libxcb-keysyms.so.1, needed by /root/release_demo/demo/libmeetingsdk.so, not found (try using -rpath or -rpath-link)
```
sudo yum install libxcb-devel
sudo yum install xcb-util-devel
sudo yum install xcb-util-image
sudo yum install xcb-util-keysyms
```

#### If you encounter these runtime runtime error
#libGL error: MESA-LOADER: failed to open swrast: /usr/lib64/dri/swrast_dri.so: cannot open shared object file: No such file or directory (search paths /usr/lib64/dri, suffix _dri)
```
sudo yum install mesa-libGL
sudo yum install mesa-libGL-devel
sudo yum install mesa-dri-drivers
```

==============================================================

## Ubuntu 

This is tested on WSL and Docker Ubuntu 22 and 23
Here are some dependencies to get it to run on Ubuntu.
Some packages might be redundant, and has not been optimized.


#### Install necessary dependencies
```
apt-get update && apt-get install -y build-essential cmake

apt-get install -y --no-install-recommends --no-install-suggests \
    libx11-xcb1 \
    libxcb-xfixes0 \
    libxcb-shape0 \
    libxcb-shm0 \
    libxcb-randr0 \
    libxcb-randr0 \
    libxcb-image0 \
    libxcb-keysyms1 \
    libxcb-xtest0 
 ```
 #### optional libraries
``` 
apt-get install -y --no-install-recommends --no-install-suggests \
    libdbus-1-3 \
    libglib2.0-0 \
    libgbm1 \
    libxfixes3 \
    libgl1 \
    libdrm2 \
    libgssapi-krb5-2 
```


#### CURL related dependencies
```
apt-get install libcurl4-openssl-dev \
    openssl \
    ca-certificates \
    pkg-config 
```

#### Install Pulseaudio
```
apt-get install -y  pulseaudio pulseaudio-utils 
```

#### if you are getting error about <SDL2/SDL.h>
```
apt-get install libegl-mesa0 libsdl2-dev g++-multilib
```




## Addition requirement for raw audio data in docker / headless environment with no soundcard
Additional file in ~/.config/zoomus.conf

[General]
system.audio.type=default

## Dockerfiles


Dockerfile targetting different distros are provided in this sample app as well (Dockerfile-centos, Dockerfile-Ubuntu .....)
Currently this is tested on 
- Centos 9 (functionality where custom function of fetching JWT Token from Web Service does not work, main SDK function works fully)
- Centos 8
- Ubuntu 22

## Run these in /demo directory


#### Centos 8
docker build -t msdk-6.0.2-on-centos8-compact -f Dockerfile-Centos8/Dockerfile .
docker run -it --rm msdk-6.0.2-on-centos8-compact

#### Centos 9
docker build -t msdk-6.0.2-on-centos9-compact -f Dockerfile-Centos9/Dockerfile .
docker run -it --rm msdk-6.0.2-on-centos9-compact

#### Ubuntu 22
docker build -t msdk-6.0.2-on-ubuntu-compact -f Dockerfile-Ubuntu/Dockerfile .
docker run -it --rm msdk-6.0.2-on-ubuntu-compact

#### ol8
docker build -t msdk-6.0.2-on-oraclelinux8-compact -f Dockerfile-ol8/Dockerfile .
docker run -it --rm msdk-6.0.2-on-oraclelinux8-compact


## Pulseaudio

The configuration for pulseaudio has been provided for you as shell scripts.

The setup is done via `setup-pulseaudio.sh` and `setup-pulssaudio-centos.sh`, this need to be run prior to running this project in a docker environment.

The script 
- starts the pulseaudio service, 
- creates a virtual speaker, 
- a virtual microphone, and 
- add a zoomus.conf file in the docker environment.

## Need help?

If you're looking for help, try [Developer Support](https://devsupport.zoom.us) or
our [Developer Forum](https://devforum.zoom.us). Priority support is also available
with [Premier Developer Support](https://zoom.us/docs/en-us/developer-support-plans.html) plans.

### Documentation
Make sure to review [our documentation](https://marketplace.zoom.us/docs/zoom-apps/introduction/) as a reference when building your Zoom Apps.
