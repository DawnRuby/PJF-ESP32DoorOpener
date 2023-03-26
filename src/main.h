//
// Created by micro on 3/26/2023.
//

struct DoorOpener : Service::LockMechanism {
    int relayPin;
    SpanCharacteristic *targetState;
    SpanCharacteristic *currentState;

    DoorOpener(int relayPin) : Service::LockMechanism(){
        targetState=new Characteristic::LockTargetState();
        currentState=new Characteristic::LockCurrentState();
        new Characteristic::Name("Smart Door Lock");
        this->relayPin=relayPin;
        pinMode(relayPin,OUTPUT);
    }

    boolean update(){
        bool newValue = !targetState->getNewVal<bool>();

        //Target door state is to open the door
        if (newValue){
            digitalWrite(relayPin,newValue);
            currentState->setVal(targetState->getVal());
            LOG0("Door is open");
            delay(2000);
        }

        //Always default to close the door.
        digitalWrite(relayPin,false);

        //Setting value to true for "engaging" the lock, I'm aware this is a bit confusing.
        // Complain to apple how this works.
        targetState->setVal(true);
        currentState->setVal(true);
        return(true);
    }

    void loop(){
        //Set value to be the value it should be.
        if(targetState->getVal()==currentState->getVal())  {
            return;
        }
        else{
            currentState->setVal(targetState->getVal());
        }
    }
};


