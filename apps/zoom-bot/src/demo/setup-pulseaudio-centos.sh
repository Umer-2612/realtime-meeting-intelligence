# Use the CentOS 9 base image
FROM oraclelinux:8

# Set the working directory
WORKDIR /etc/yum.repos.d/

# Ensure the Oracle Linux repositories are enabled
RUN yum install -y oraclelinux-release-el8 && \
    yum-config-manager --enable ol8_baseos_latest ol8_appstream ol8_UEKR6 && \
    yum clean all

RUN yum update -y

# Ensure the Oracle Linux repositories are enabled
RUN yum install -y oraclelinux-release-el8 && \
    yum-config-manager --enable ol8_baseos_latest ol8_appstream ol8_UEKR6 ol8_codeready_builder && \
    yum clean all

# Install required packages
RUN yum install -y dnf-plugins-core \
    && yum install -y cmake gcc gcc-c++ \
    && yum install -y oracle-epel-release-el8 \
    && yum install -y SDL2-devel mesa-libGL mesa-libGL-devel mesa-dri-drivers \
    && yum install -y libxcb-devel xcb-util-image xcb-util-keysyms \
    && yum install -y openssl-devel \
    && yum install -y libcurl-devel \
    && yum clean all


RUN yum install -y glib2-devel

RUN yum install -y pulseaudio pulseaudio-utils

RUN yum install -y libatomic 

 # Set the working directory
WORKDIR /app

 # Copy your application files to the container
COPY demo/ /app/demo/

# Execute additional commands
 RUN cd /app/demo && rm -rf bin && rm -rf build && cmake -B build && cd build && make

 # Set the working directory to the binary folder
 WORKDIR /app/demo/bin

# # Define a shell script to run multiple commands
 RUN echo '#!/bin/bash' > /app/demo/run.sh \
     && echo '/app/demo/setup-pulseaudio-centos.sh' >> /app/demo/run.sh \
     && echo './meetingSDKDemo' >> /app/demo/run.sh

 # Make the run script executable
 RUN chmod +x /app/demo/run.sh

 # Run the meetingSDKDemo binary when the container starts
 #CMD ["/app/demo/run.sh"]
 CMD ["/bin/bash"]
 #CMD ["./meetingSDKDemo"]