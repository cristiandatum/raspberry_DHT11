import asyncio
import time
import board
import RPi.GPIO as GPIO
import dht11
from azure.iot.device import Message
from azure.iot.device.aio import IoTHubDeviceClient
 
CONNECTION_STRING="HostName=raspy-hub-sensors.azure-devices.net;DeviceId=DHT11;SharedAccessKey=Lb1FaUGeaJ2R8RdvkETY38u7/7byLGyRWguLYHgv/40="
 
DELAY = 2
TEMPERATURE = 20.0
HUMIDITY = 60
PAYLOAD = '{{"Humidity": {humidity}, "Temperature": {temperature}}}'
 
async def main():
 
    try:
        # Create instance of the device client
        client = IoTHubDeviceClient.create_from_connection_string(CONNECTION_STRING)
 
        # Initialize GPIO
        GPIO.setwarnings(False)
        GPIO.setmode(GPIO.BCM)
        GPIO.cleanup()
 
        # Read data using pin GPIO17
        dhtDevice = dht11.DHT11(pin=17)
         
        GPIO.setup(4, GPIO.IN) #PIR
 
        print("Sending service started. Press Ctrl-C to exit")
        while True:
 
            try:
                #DHT11
                result = dhtDevice.read()
                #PIR
                if GPIO.input(4):
                    pir = 1
                else:
                    pir = 0
                     
                if result.is_valid():
                    temperature = result.temperature
                    humidity = result.humidity
 
                    data = PAYLOAD.format(humidity=humidity, temperature=temperature)
                    msg = Message(data)
                    
                    msg.content_encoding = "utf-8"
                    msg.content_type = "application/json"
 
                    # Send a message to the IoT hub
                    print(f"Sending message: {msg}")
                    await client.send_message(msg)
                    print("Message successfully sent")
                else:
                    print("Error: %d" % result.error_code)
                    continue
 
                await asyncio.sleep(DELAY)
 
            except KeyboardInterrupt:
                print("Service stopped")
                GPIO.cleanup()
                break
 
    except Exception as error:
        print(error.args[0])
 
if __name__ == '__main__':
    asyncio.run(main())
