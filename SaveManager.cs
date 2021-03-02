using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    Dictionary<string, int> intSaves;
    Dictionary<string, float> floatSaves;
    Dictionary<string, string> stringSaves;

    string intFile = "/intSaves.txt";
    string floatFile = "/floatSaves.txt";
    string stringFile = "/stringSaves.txt";

    StreamWriter writer;

    public void Save(string id, int value)
    {
        if (intSaves.ContainsKey(id))
            intSaves[id] = value;
        else
            intSaves.Add(id, value);

        WriteInts();
    }
    public void Save(string id, float value)
    {
        if (floatSaves.ContainsKey(id))
            floatSaves[id] = value;
        else
            floatSaves.Add(id, value);

        WriteFloats();
    }
    public void Save(string id, string value)
    {
        if (stringSaves.ContainsKey(id))
            stringSaves[id] = value;
        else
            stringSaves.Add(id, value);

        WriteStrings();
    }
    public int RetrieveInt(string id)
    {
        if (intSaves.ContainsKey(id))
            return intSaves[id];
        else
            return 0;
    }
    public int RetrieveInt(string id, int def)
    {
        if (intSaves.ContainsKey(id))
            return intSaves[id];
        else
            return def;
    }
    public float RetrieveFloat(string id)
    {
        if (floatSaves.ContainsKey(id))
            return floatSaves[id];
        else
            return 0.0f;
    }
    public float RetrieveFloat(string id, float def)
    {
        if (floatSaves.ContainsKey(id))
            return floatSaves[id];
        else
            return def;
    }
    public string RetrieveString(string id)
    {
        if (stringSaves.ContainsKey(id))
            return stringSaves[id];
        else
            return "";
    }

    void WriteInts()
    {
        string ints = "";
        foreach (var item in intSaves)
        {
            ints += item.Key + "$" + item.Value + "\n";
        }
        File.WriteAllText(Application.persistentDataPath + intFile, ints);
    }
    void WriteFloats()
    {
        //Floats
        string floats = "";
        foreach (var item in floatSaves)
        {
            floats += item.Key + "$" + item.Value + "\n";
        }
        File.WriteAllText(Application.persistentDataPath + floatFile, floats);
    }
    void WriteStrings()
    {
        //strings
        string strings = "";
        foreach (var item in stringSaves)
        {
            strings += item.Key + "$" + item.Value + "\n";
        }
        File.WriteAllText(Application.persistentDataPath + stringFile, strings);
    }
    void Read()
    {
        if (!File.Exists(Application.persistentDataPath + intFile))
        {
            File.WriteAllText(Application.persistentDataPath + intFile, "");
        }

        StreamReader reader = new StreamReader(Application.persistentDataPath + intFile, true);
        string[] splitted = reader.ReadToEnd().Split('\n');
        intSaves = new Dictionary<string, int>();
        foreach (var item in splitted)
        {
            if (!item.Contains("$"))
                continue;
            intSaves.Add(item.Split('$')[0], int.Parse(item.Split('$')[1]));
        }

        if (!File.Exists(Application.persistentDataPath + floatFile))
        {
            File.WriteAllText(Application.persistentDataPath + floatFile, "");
        }
        reader.Close();
        reader = new StreamReader(Application.persistentDataPath + floatFile, true);
        splitted = reader.ReadToEnd().Split('\n');
        floatSaves = new Dictionary<string, float>();
        foreach (var item in splitted)
        {
            if (!item.Contains("$"))
                continue;
            floatSaves.Add(item.Split('$')[0], float.Parse(item.Split('$')[1]));
        }

        if (!File.Exists(Application.persistentDataPath + stringFile))
        {
            File.WriteAllText(Application.persistentDataPath + stringFile, "");
        }
        reader.Close();
        reader = new StreamReader(Application.persistentDataPath + stringFile, true);
        splitted = reader.ReadToEnd().Split('\n');
        stringSaves = new Dictionary<string, string>();
        foreach (var item in splitted)
        {
            if (!item.Contains("$"))
                continue;
            stringSaves.Add(item.Split('$')[0], (item.Split('$')[1]));
        }
        reader.Close();
    }

    #region singleton
    //Singleton
    public static SaveManager instance;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this);
        }

        Read();
    }
    #endregion
}
