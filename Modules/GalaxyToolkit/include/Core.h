#pragma once
#define TOOL_ACCESS_ADDRESS 0x80002FF4 // Compatible with GstRecord and PadRecord.

#include "syati.h"

namespace GToolkitCore {
    struct MessageData {
        MessageData();

        u32 mToolMessage; // _0
        u32 mGameMessage; // _4
        char mData[64];   // _8
    
        static MessageData sInstance;
    };

    void control();
    void handleException();

    void parseCommand();
};