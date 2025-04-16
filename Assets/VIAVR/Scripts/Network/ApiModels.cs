using System;
using System.IO;
using Newtonsoft.Json;

namespace VIAVR.Scripts.Network
{
    public class RequestBase
    {
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class ValidationErrorDetail
    {
        [JsonProperty("msg")] public string Message;
        [JsonProperty("type")] public string Type;
    }

    public class LinkDeviceRequest : RequestBase
    {
        [JsonProperty("code")] public string Code { get; set; }
        [JsonProperty("serial")] public string Serial { get; set; }

        public LinkDeviceRequest(string code, string serial)
        {
            if (string.IsNullOrEmpty(code)) throw new InvalidDataException("LinkDeviceRequest: code cannot be null");
            if (string.IsNullOrEmpty(serial))
                throw new InvalidDataException("LinkDeviceRequest: serial cannot be null");

            Code = code;
            Serial = serial;
        }
    }

    public class LinkDeviceResult
    {
        [JsonProperty("token")] public string Token { get; set; }
        [JsonProperty("detail")] public ValidationErrorDetail[] Detail { get; set; }
    }

    public class DeviceDetailsResult
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("label")] public string Label { get; set; }
    }

    public class DeviceRuntimeRequest : RequestBase
    {
        [JsonProperty("battery")] public int Battery { get; set; }
        [JsonProperty("firmware")] public string Firmware { get; set; }
        [JsonProperty("charging")] public bool Charging { get; set; }

        public DeviceRuntimeRequest(int battery, string firmware, bool charging)
        {
            if (string.IsNullOrEmpty(firmware))
                throw new InvalidDataException("LinkDeviceRequest: firmware cannot be null");

            Battery = battery;
            Firmware = firmware;
            Charging = charging;
        }
    }

    public class DeviceRuntimeResult
    {
        [JsonProperty("detail")] public ValidationErrorDetail[] Detail { get; set; }
    }

    public class DeviceFirmwareResult
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("version")] public string Version { get; set; }
        [JsonProperty("url")] public string Url { get; set; }
        [JsonProperty("is_active")] public bool IsActive { get; set; }
        [JsonProperty("created_at")] public DateTime CreatedAt { get; set; }
        [JsonProperty("updated_at")] public DateTime UpdatedAt { get; set; }
    }

    public class DeviceCmdResult
    {
        [JsonProperty("cmd")] public string Cmd { get; set; }
        [JsonProperty("payload")] public object Payload { get; set; }
    }

    public class DeviceAppResult
    {
        [JsonProperty("name")] public string Package { get; set; }
        [JsonProperty("is_active")] public bool IsActive { get; set; }
        [JsonProperty("created_at")] public DateTime CreatedAt { get; set; }
        [JsonProperty("updated_at")] public DateTime UpdatedAt { get; set; }

        public override string ToString()
        {
            return $"{Package} ({IsActive})";
        }
    }
}