#include "Core.h"

u8 handle;
u32 param;

namespace GToolkitCore {
    MessageData::MessageData() {
        *(const MessageData **)TOOL_ACCESS_ADDRESS = this;
        OSReport("[GToolkit] MessageData created at %p.\n", this);
    };

    MessageData(MessageData::sInstance);

    void control() {
        parseCommand();

        switch (handle) {
            case 0:
                return;
            case 1:
                OSReport("[GToolkit] Initialized Tool.\n");
                break;
            case 2:
                OSReport("[GToolkit] Object message not supported yet.\n");
                break;
            case 3:
                OSReport("[GToolkit] Recived stage message.\n");

                s8 scenarioNo = (MessageData::sInstance.mToolMessage >> 8) & 0xFF;
                s8 starNo = (MessageData::sInstance.mToolMessage >> 16) & 0xFF;

                OSReport("[GToolkit] %d %d\n", scenarioNo, starNo);

                MR::stopStageBGM(60);
                MR::closeSystemWipeCircleWithCaptureScreen(60);
                GameSequenceFunction::requestChangeScenarioSelect(MessageData::sInstance.mData);

                if (scenarioNo != -1)
                    GameSequenceFunction::requestChangeStage(MessageData::sInstance.mData, scenarioNo, starNo, JMapIdInfo(0, 0));

                break;
            case 4:
                OSReport("[GToolkit] Recived warp message.\n");

                if (param == 0) {
                    MR::setPlayerPos(*(TVec3f *)MessageData::sInstance.mData);
                    break;
                }
                else {
                    TVec3f position;
                    TVec3f rotation;

                    if (MR::tryFindNamePos(MessageData::sInstance.mData, &position, &rotation))
                        MR::setPlayerPos(position);
                    else
                        OSReport("[GToolkit] GeneralPos %s was not found.\n", MessageData::sInstance.mData);
                }

                break;
            case 0xFE:
                if (param == 0)
                    break;

                OSReport("[GToolkit] Recived freeze message.\n");

                while (true) {
                    parseCommand();

                    if (handle == 0xFE && param == 0)
                        break;
                }

                OSReport("[GToolkit] Recived unfreeze message.\n");
                break;
            case 0xFF:
                OSReport(NULL);
                break;
        }
    
        MessageData::sInstance.mToolMessage = 0;
    }

    void handleException() {
        __asm {
            lwz     r28, 8(r3) // Original instruction at the hook address
            lis     r6, 0x8000
            ori     r6, r6, 0x2FF4
            lwz     r6, 0(r6)
            addi    r6, r6, 0x4
            stw     r28, 0(r6)
            blr
        }
    }

    void parseCommand() {
        handle = MessageData::sInstance.mToolMessage & 0xFF;
        param = MessageData::sInstance.mToolMessage >> 8;
    }
}

kmCall(0x8050F4FC, GToolkitCore::handleException);