#!/bin/bash

# Kill any existing PulseAudio processes
pkill -9 pulseaudio || true

# Clean up old PulseAudio files
rm -rf /tmp/pulse* ~/.config/pulse/* || true

# Create PulseAudio configuration directory
mkdir -p ~/.config/pulse

# Create client configuration
cat > ~/.config/pulse/client.conf << EOF
autospawn = yes
daemon-binary = /usr/bin/pulseaudio
default-server =
default-sink = VirtualSink
default-source = VirtualSink.monitor
EOF

# Create daemon configuration with more verbose settings
cat > ~/.config/pulse/daemon.conf << EOF
daemonize = yes
allow-module-loading = yes
enable-shm = yes
shm-size-bytes = 0 # Let PulseAudio decide
log-target = stderr
log-level = debug
resample-method = speex-float-1
default-fragments = 8
default-fragment-size-msec = 10
load-default-script-file = yes
exit-idle-time = -1
flat-volumes = no
EOF

# Start PulseAudio with verbose output
pulseaudio --start --log-target=stderr --verbose

# Wait for PulseAudio to initialize
sleep 3

# Load modules
pactl load-module module-null-sink sink_name=VirtualSink sink_properties=device.description="Virtual\ Output"
pactl load-module module-null-source source_name=VirtualSource source_properties=device.description="Virtual\ Source"
pactl load-module module-virtual-source source_name=VirtualMic master=VirtualSink.monitor source_properties=device.description="Virtual\ Microphone"

# Set up loopback (for hearing the sound)
pactl load-module module-loopback latency_msec=1 source=VirtualMic sink=VirtualSink

# Set default devices
pactl set-default-sink VirtualSink
pactl set-default-source VirtualMic

# List audio devices to confirm setup
echo "=== AUDIO DEVICES CONFIGURATION ==="
echo "PulseAudio Sinks (Output Devices):"
pactl list short sinks
echo "PulseAudio Sources (Input Devices):"
pactl list short sources
echo "================================="

# Set environment variables for better device detection
export PULSE_SERVER=unix:/tmp/pulse-socket
export PULSE_SINK=VirtualSink
export PULSE_SOURCE=VirtualMic