using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Debug = CustomDebug.Debug;

public class WifiDataSaver
{
    private const int MaxSavedWifiSpots = 6;
    public const string PlayerPrefsSaveKey = "Wifi_Spots";

    private List<WifiData> _wifis;

    public WifiDataSaver()
    {
        LoadSavedWifiSpots();
    }

    public void AddWifiSpot(string ssid, string password)
    {
        AddWifiToList(ssid, password);
        ConvertToJsonAndSave();

        Debug.Log($"WifiDataSaver.AddWifiSpot(): добавлена точка {ssid} {password}", Debug.LoggerBehaviour.ADD);

        if (_wifis.Count <= 0)
            return;
        
        List<string> savedSsid = _wifis.Select(t => t.Ssid).ToList();
        string savedSsidString = string.Join(", ", savedSsid);

        Debug.Log($"Сохраненные точки: {savedSsidString}", Debug.LoggerBehaviour.ADD);
    }

    private void LoadSavedWifiSpots()
    {
        string savedWifiSpots = PlayerPrefs.GetString(PlayerPrefsSaveKey);
        _wifis = string.IsNullOrEmpty(savedWifiSpots)
            ? new List<WifiData>()
            : JsonConvert.DeserializeObject<List<WifiData>>(savedWifiSpots) ?? new List<WifiData>();
    }

    private void AddWifiToList(string ssid, string password)
    {
        if (_wifis.Any(w => w.Ssid == ssid))
            _wifis.Remove(_wifis.FirstOrDefault(w => w.Ssid == ssid));
        
        if (_wifis.Count >= MaxSavedWifiSpots)
            _wifis.RemoveAt(0);

        _wifis.Add(new WifiData(ssid, password));
    }

    private void ConvertToJsonAndSave()
    {
        string savedWifiSpotsJson = JsonConvert.SerializeObject(_wifis);
        PlayerPrefs.SetString(PlayerPrefsSaveKey, savedWifiSpotsJson);
        PlayerPrefs.Save();
    }
}

[Serializable]
public class WifiData
{
    public string Ssid;
    public string Password;

    public WifiData(string ssid, string password)
    {
        Ssid = ssid;
        Password = password;
    }
}
