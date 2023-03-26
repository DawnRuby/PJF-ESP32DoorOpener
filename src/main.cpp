//
// Created by micro on 3/26/2023.
//

#include "Arduino.h"
#include "..\lib\HomeSpan\src\Homespan.h"

struct DoorOpener : Service::Door{
    int DoorRelayPin;

    DoorOpener(int DoorRelayPin) : Service::Door(){
        this->DoorRelayPin=DoorRelayPin;
        pinMode(DoorRelayPin, OUTPUT);
    }

    //Update method to let Homekit know if updates were successful
    boolean update(){
        digitalWrite(DoorRelayPin,HIGH);
        sleep(1000);
        digitalWrite(DoorRelayPin, LOW);
        return(true);
    }
};

void setup() {
    //Start Serial Port
    Serial.begin(115200);
    homeSpan.begin(Category::Doors, "HomeSpan Door Opener");
    homeSpan.setStatusPin(2);
    homeSpan.setControlPin(13);
    new SpanAccessory();                              // Begin by creating a new Accessory using SpanAccessory(), no arguments needed

    new Service::AccessoryInformation();            // HAP requires every Accessory to implement an AccessoryInformation Service

    // The only required Characteristic for the Accessory Information Service is the special Identify Characteristic.  It takes no arguments:

    new Characteristic::Identify();               // Create the required Identify Characteristic

    new SpanAccessory();
      new Service::AccessoryInformation();
      new Characteristic::Identify();

      new Service::Door();
        new Characteristic::PositionState();
        new Characteristic::TargetPosition();
        new Characteristic::Name("Front Door");

      new DoorOpener(14);

    new Characteristic::Manufacturer("Leejja");   // Manufacturer of the Accessory (arbitrary text string, and can be the same for every Accessory)
    new Characteristic::Model("Relay based door opener");     // Model of the Accessory (arbitrary text string, and can be the same for every Accessory)
}

void loop(){
    homeSpan.poll();

}

