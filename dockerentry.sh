#!/bin/bash

# Set variable for the UID and GID based on env, else use default values
PUID=${PUID:-1000}
PGID=${PGID:-100}

[ $PUID -eq 0 ] && [ $PGID -eq 0 ] && echo "You really shouldn't be doing this..." && dotnet /usr/src/app/build/Shoko.CLI.dll


[ "$AVDUMP_MONO" = true ] && (which mono > /dev/null || (apt-get update && apt-get install -y mono-runtime libmono-system-xml-linq4.0-cil))

GROUP="shokogroup"
USER="shoko"

if [ $(getent group $GROUP) ]; then #if group exists
    if [ $(getent group $GROUP | cut -d: -f3) -ne $PGID ]; then #if gid of said group doesn't match
        groupmod -g "$PGID" $GROUP
        REDO_PERM=1
    fi
else
    groupadd -o -g "$PGID" $GROUP
fi

if [ $(getent passwd $USER) ]; then
    if [ $(getent passwd $USER | cut -d: -f3) -ne $PUID ]; then
        usermod -u "$PUID" $USER
        REDO_PERM=1
    fi
    [ $(id -g $USER) -ne $PGID ] && usermod -g "$PGID" $USER
else
    useradd  -N -o -u "$PUID" -g "$PGID" -d /home/shoko $USER

    mkdir -p /home/shoko/.shoko/
    chown -R $USER:$GROUP /home/shoko
fi

[ $REDO_PERM ] && chown -R $PUID:$PGID /home/shoko/

# Set owership of shoko files to shoko user
chown -R $USER:$GROUP /usr/src/app/build/
if [ -d /root/.shoko ]; then
    echo "
-------------------------------------
OLD SHOKO INSTALL DETECTED

Please change the volume for shoko
OLD directory: /root/.shoko
New directory: /home/shoko/.shoko
-------------------------------------
    "
    exit 1
fi

# set umask to specified value if defined
if [[ ! -z "${UMASK}" ]]; then
     umask "${UMASK}"
fi

echo "
-------------------------------------
User uid:    $(id -u $USER)
User gid:    $(id -g $USER)
UMASK set:    $(umask)
-------------------------------------
"

# Make sure we use the packaged WebUI
rm -rf /home/shoko/.shoko/Shoko.CLI/webui
ln -s /usr/src/app/build/webui /home/shoko/.shoko/Shoko.CLI/webui

# Go and run the server 
exec gosu $USER:$GROUP /usr/src/app/build/Shoko.CLI
