#!/bin/bash

ps aux | grep -v 'grep\|Ss\|Z\|FindProcesses.sh' | grep $1 | awk '{ print $2 }'