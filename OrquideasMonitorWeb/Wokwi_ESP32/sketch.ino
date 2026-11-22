#include <WiFi.h>
#include <HTTPClient.h>
#include <DHT.h>

#define DHTPIN 15
#define DHTTYPE DHT22

const char* ssid = "Wokwi-GUEST";
const char* password = "";
const char* serverUrl = "https://07b4zv6w-7288.brs.devtunnels.ms/api/iot/lectura?forzar=true";

DHT dht(DHTPIN, DHTTYPE);

void setup() {
  Serial.begin(115200);
  dht.begin();
  WiFi.begin(ssid, password);
  Serial.print("Conectando a Wi-Fi");
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.println("\nWi-Fi Conectado!");
}

void loop() {
  float humedad = dht.readHumidity();
  float temperatura = dht.readTemperature();

  if (isnan(humedad) || isnan(temperatura)) {
    delay(2000);
    return;
  }

  if (WiFi.status() == WL_CONNECTED) {
    HTTPClient http;
    http.begin(serverUrl);
    http.addHeader("Content-Type", "application/json");
    http.addHeader("X-Tunnel-Skip-AntiPhishing-Scan", "true");

    String json = "{\"sensorId\":1,\"temperatura\":" + String(temperatura, 1) + ",\"humedad\":" + String(humedad, 1) + "}";
    int httpCode = http.POST(json);

    Serial.println(json + " -> HTTP: " + String(httpCode));
    http.end();
  }
  delay(4000);
}