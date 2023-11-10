#include "esp_camera.h"
#include "esp_http_server.h"
#include "esp_timer.h"
#include "esp_log.h"
#include "CameraJpgHandler.h"
#include <sensor.h>
#define TAG "ARDUINO"

int fileFlag = 0;

void swap_capture_mode() {
    switch (cameraCaptureMode) {
        case 0: {                        
            Serial.println("setup jpeg low res service");
            //httpd_unregister_uri_handler(s_httpd, jpg_index_uri.uri, jpg_index_uri.method);            
            esp_camera_deinit();
            esp_camera_init(&camera_config);   
            s = esp_camera_sensor_get();                                     
            fileFlag = 0;
            //httpd_register_uri_handler(s_httpd, &bmp_index_uri);  
            break;
        }
        case 1: {
            Serial.println("setup jpeg high res service");            
            //httpd_unregister_uri_handler(s_httpd, bmp_index_uri.uri, bmp_index_uri.method);
            esp_camera_deinit();
            esp_camera_init(&camera_config2);   
            s = esp_camera_sensor_get();                           
            fileFlag = 0;
            //httpd_register_uri_handler(s_httpd, &jpg_index_uri);   
            break;
        }
        case 2: {
            Serial.println("setup bmp low res service");
            //httpd_unregister_uri_handler(s_httpd, bmp_index_uri.uri, bmp_index_uri.method);
            esp_camera_deinit();
            esp_camera_init(&camera_config3);                      
            s = esp_camera_sensor_get();
            fileFlag = 1;
            //httpd_register_uri_handler(s_httpd, &jpg_index_uri);   
            break;
        }
        case 3: {
            Serial.println("setup bmp high res service");
            //httpd_unregister_uri_handler(s_httpd, bmp_index_uri.uri, bmp_index_uri.method);
            esp_camera_deinit();
            esp_camera_init(&camera_config4);                        
            s = esp_camera_sensor_get();
            fileFlag = 1;
            //httpd_register_uri_handler(s_httpd, &jpg_index_uri);   
            break;
        }
    }

}


esp_err_t bmp_httpd_handler(httpd_req_t* req) {
    camera_fb_t* fb = NULL;
    esp_err_t res = ESP_OK;
    int64_t fr_start = esp_timer_get_time();

    if (fileFlag == 0) {
        return jpg_httpd_handler(req);        
    }

    fb = esp_camera_fb_get();
    if (!fb) {
        ESP_LOGE(TAG, "Camera capture failed");
        httpd_resp_send_500(req);
        return ESP_FAIL;
    }

    uint8_t* buf = NULL;
    size_t buf_len = 0;
    bool converted = frame2bmp(fb, &buf, &buf_len);
    esp_camera_fb_return(fb);
    if (!converted) {
        ESP_LOGE(TAG, "BMP conversion failed");
        httpd_resp_send_500(req);
        return ESP_FAIL;
    }

    res = httpd_resp_set_type(req, "image/png")
        || httpd_resp_set_hdr(req, "Content-Disposition", "inline; filename=capture.bmp")
        || httpd_resp_send(req, (const char*)buf, buf_len);
    free(buf);
    int64_t fr_end = esp_timer_get_time();
    ESP_LOGI(TAG, "BMP: %uKB %ums", (uint32_t)(buf_len / 1024), (uint32_t)((fr_end - fr_start) / 1000));
    return res;
}
esp_err_t jpg_httpd_handler(httpd_req_t* req)
{
    camera_fb_t* fb = NULL;
    esp_err_t res = ESP_OK;
    size_t fb_len = 0;
    int64_t fr_start = esp_timer_get_time();  

    if (fileFlag == 1) {
        return bmp_httpd_handler(req);        
    }

    fb = esp_camera_fb_get();
    if (!fb)
    {
        ESP_LOGE(TAG, "Camera capture failed");
        httpd_resp_send_500(req);
        return ESP_FAIL;
    }

    res = httpd_resp_set_type(req, "image/png");

    if (res == ESP_OK)
    {
        fb_len = fb->len;
        res = httpd_resp_send(req, (const char*)fb->buf, fb->len);
    }

    esp_camera_fb_return(fb);
    int64_t fr_end = esp_timer_get_time();
    ESP_LOGI(TAG, "JPG: %uKB %ums", (uint32_t)(fb_len / 1024), (uint32_t)((fr_end - fr_start) / 1000));
    return res;
}