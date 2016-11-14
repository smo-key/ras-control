#include <MotorDriver.h>
#include <seeed_pwm.h>
MotorDriver motors; //Motors

// Basic Bluetooth sketch HC-05_02_9600+ECHO
// Connect the HC-05 module and communicate using the serial monitor
//
// The HC-05 defaults to commincation mode when first powered on.
// The default baud rate for communication mode is 9600. Your module may have a different speed.
//
 
#include <SoftwareSerial.h>
SoftwareSerial BTserial(2, 3); // RX | TX
// Connect the HC-05 TX to Arduino pin 2 RX. 
// Connect the HC-05 RX to Arduino pin 3 TX through a voltage divider.

String s, sy;
int l, r;

void setup() 
{
    Serial.begin(9600);
    Serial.println("Arduino is ready");
 
    // HC-05 default serial speed for communication mode is 9600
    BTserial.begin(9600);  
    Serial.println("BTserial started at 9600");

    motors.begin();

    //motor 0 -> right motor (-100 = positive)
    //motor 1 -> left motor  (-100 = positive)
    motors.speed(0, 0);
    motors.speed(1, 0);
}

void loop()
{
    // Keep reading from HC-05 and send to Arduino Serial Monitor
    if (BTserial.available())
    {  
        s = BTserial.readStringUntil('\n');
        //Serial.println(s);
        if (s.substring(0, 2) == "L ")
        {
          //L 0.000000 0.000000 DONT CARE
          int s1 = s.indexOf(" ", 2);
          int s2 = s.indexOf(" ", s1 + 1);
          int s3 = s.indexOf(" ", s2 + 1);
          //x = s.substring(s1 + 1, s2);
          sy = s.substring(s1, s2);

          char bufy[sy.length()];
          sy.toCharArray(bufy,sy.length());
          l = atoi(bufy);          
          //Serial.print(l);
          //Serial.print(",");
          //Serial.println(r);
          motors.speed(1, -l*10);
        }
        else if (s.substring(0, 2) == "R ")
        {
          //R 0.000000 0.000000 DONT CARE
          int s1 = s.indexOf(" ", 2);
          int s2 = s.indexOf(" ", s1 + 1);
          int s3 = s.indexOf(" ", s2 + 1);
          //x = s.substring(s1 + 1, s2);
          sy = s.substring(s1, s2);

          char bufy[sy.length()];
          sy.toCharArray(bufy,sy.length());
          r = atoi(bufy);   
          //Serial.print(l);
          //Serial.print(",");
          //Serial.println(r);       
          motors.speed(0, -r*10);
        }
    }
}
