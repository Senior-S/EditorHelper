using System;
using System.Collections.Generic;
using System.Linq;

namespace EditorHelper.Extras;

// https://github.com/ShimmyMySherbet/ShimmysAdminTools/blob/master/ShimmysAdminTools/Components/INIFile.cs
public class IniFile
{
    private readonly List<IniLine> _data = [];
    private string _loadFile;
    public bool HasUnsavedChanges { get; protected set; }

    public IniFile(string content = null)
    {
        _loadFile = "";
        if (!string.IsNullOrEmpty(content))
        {
            foreach (string line in content.Split('\n'))
            {
                if (!line.StartsWith("#") & !string.IsNullOrEmpty(line) & line.Contains("="))
                {
                    string key = line.Split('=')[0];
                    string value = line.Remove(0, key.Length + 1);
                    _data.Add(new IniLine()
                    {
                        Key = key,
                        Value = value,
                        IsDataEntry = true,
                        Line = ""
                    });
                }
                else
                {
                    _data.Add(new IniLine()
                    {
                        IsDataEntry = false,
                        Line = line
                    });
                }
            }
        }
    }

    public object this[string Key, Type T]
    {
        get
        {
            object ent = this[Key];
            string estr = ent.ToString();
            if (T == typeof(bool)) return Convert.ToBoolean(estr);
            if (T == typeof(double)) return Convert.ToDouble(estr);
            if (T == typeof(int)) return Convert.ToInt32(estr);
            if (T == typeof(long)) return Convert.ToInt64(estr);
            if (T == typeof(string)) return estr;
            if (T == typeof(byte)) return Convert.ToByte(estr);
            if (T == typeof(char)) return Convert.ToChar(estr);
            if (T == typeof(DateTime)) return Convert.ToDateTime(estr);
            if (T == typeof(decimal)) return Convert.ToDecimal(estr);
            if (T == typeof(short)) return Convert.ToInt16(estr);
            if (T == typeof(sbyte)) return Convert.ToSByte(estr);
            if (T == typeof(float)) return Convert.ToSingle(estr);
            if (T == typeof(ushort)) return Convert.ToUInt16(estr);
            if (T == typeof(uint)) return Convert.ToUInt32(estr);
            if (T == typeof(ulong)) return Convert.ToUInt64(estr);
            return ent;
        }
    }

    public string this[string Key]
    {
        get
        {
            return _data.Where(x =>
            {
                if (x.IsDataEntry)
                {
                    return (x.Key.ToLower() ?? "") == (Key.ToLower() ?? "");
                }
                else
                {
                    return false;
                }
            }).First().Value;
        }
        set
        {
            HasUnsavedChanges = true;
            bool found = false;
            foreach (IniLine? x in _data)
            {
                if (x.IsDataEntry)
                {
                    if ((x.Key.ToLower() ?? "") == (Key.ToLower() ?? ""))
                    {
                        x.Value = value.ToString();
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                _data.Add(new IniLine()
                {
                    IsDataEntry = true,
                    Key = Key,
                    Value = value.ToString(),
                    Line = ""
                });
            }
        }
    }

    public bool KeySet(string Key)
    {
        bool ret = false;
        foreach (IniLine? x in _data)
        {
            if (x.IsDataEntry)
            {
                if ((x.Key.ToLower() ?? "") == (Key.ToLower() ?? ""))
                {
                    ret = true;
                }
            }
        }

        return ret;
    }

    private partial class IniLine
    {
        public bool IsDataEntry = false;
        public string Key = "";
        public string Line = "";
        public string Value = "";
    }
}