const int blue_LED1   = 4;
const int red_LED1    = 3;
const int green_LED1  = 2;
const int switch_1 = A15;
const int switch_2 = A14;
bool state_LED1 = LOW;
int inByte = 0;         // incoming serial byte

void setup() {
  pinMode(red_LED1, OUTPUT);
  pinMode(blue_LED1, OUTPUT);
  pinMode(green_LED1, OUTPUT);
  pinMode(switch_1, INPUT);
  pinMode(switch_2, INPUT);


  // start serial port at 9600 bps:
  Serial.begin(9600);
  while (!Serial) {
    ; // wait for serial port to connect. Needed for native USB port only
  }
}

void loop()
{
  // delay 10ms to let the ADC recover:
  delay(10);

  //Await Serial Available
  while (Serial.available() <= 0)
  {
    FlashBlueLED(100);
  }

  //Serial Available

  //Await Ready Signal
  inByte = Serial.read();
  while (Serial.available() <= 0 && inByte != 32)
  {
    inByte = Serial.read();
    FlashBlueLED(100);
  }
  //Await for sensor to detect a new RFID Sheet
  while (map(analogRead(switch_1), 0, 250, LOW, HIGH) != LOW || map(analogRead(switch_2), 0, 250, LOW, HIGH) != LOW)
  {
    FlashBlueLED(1000);
  }
  if (map(analogRead(switch_1), 0, 250, LOW, HIGH) == LOW && map(analogRead(switch_2), 0, 250, LOW, HIGH) == LOW)
  {
    //Ready for the validation result
    Serial.print("1");
    digitalWrite(blue_LED1 , HIGH);
    // if we get a valid byte proceed
    while (Serial.available() <= 0 && (inByte != 2 || inByte != 4))
    {
      ;
    }
    if (Serial.available() > 0) {
      // get incoming byte
      inByte = Serial.read();
      if (inByte == 2) //010: Sheet has bad RFIDs
      {
        digitalWrite(red_LED1 , HIGH);
      }
      else if (inByte == 4) //100: Sheet does not have bad RFIDs
      {
        digitalWrite(green_LED1 , HIGH);
      }
      else
      {
        //Unexpected values
        digitalWrite(red_LED1, HIGH);
        digitalWrite(blue_LED1 , HIGH);
        digitalWrite(green_LED1, HIGH);
      }
      Serial.print("1");
      while (map(analogRead(switch_1), 0, 250, LOW, HIGH) == LOW || map(analogRead(switch_2), 0, 250, LOW, HIGH) == LOW)
      {
        delay(10);
      }
      delay(500);
      digitalWrite(red_LED1, LOW);
      digitalWrite(blue_LED1 , LOW);
      digitalWrite(green_LED1, LOW);
    }
    //Flush the serial port and get ready to begin the process again
    Serial.flush();
  }
  else
  {
    digitalWrite(red_LED1, LOW);
    digitalWrite(blue_LED1 , LOW);
    digitalWrite(green_LED1, LOW);
  }
}
void FlashBlueLED(int delayTime) {
  if (state_LED1 == HIGH)
  {
    digitalWrite(blue_LED1 , HIGH);
    state_LED1 = LOW;
  }
  else
  {
    digitalWrite(blue_LED1 , LOW);
    state_LED1 = HIGH;
  }
  delay(delayTime);
}
