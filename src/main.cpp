//
// Created by micro on 3/26/2023.
//

#include "Arduino.h"
#include "..\lib\HomeSpan\src\Homespan.h"
#include "main.h"
#define DOORRELAYPIN 13



void setup() {
    //Start Serial Port
    Serial.begin(115200);
    homeSpan.begin(Category::Doors, "Door Opener");
    homeSpan.setStatusPin(2);

    new SpanAccessory();
      new Service::AccessoryInformation();
      new Characteristic::Identify();
      new DoorOpener(DOORRELAYPIN);
}

void loop(){
    homeSpan.poll();
}

