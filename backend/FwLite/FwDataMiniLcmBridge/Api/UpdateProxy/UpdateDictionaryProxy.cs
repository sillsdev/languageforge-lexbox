﻿using System.Collections;
using MiniLcm.Models;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace FwDataMiniLcmBridge.Api.UpdateProxy;

public class UpdateDictionaryProxy(ITsMultiString multiString, FwDataMiniLcmApi lexboxLcmApi)
    : IDictionary<WritingSystemId, string>, IDictionary
{
    public void Add(KeyValuePair<WritingSystemId, string> item)
    {
        Add(item.Key, item.Value);
    }

    public void Add(WritingSystemId key, string value)
    {
        var writingSystemHandle = lexboxLcmApi.GetWritingSystemHandle(key);
        multiString.set_String(writingSystemHandle, TsStringUtils.MakeString(value, writingSystemHandle));
    }

    public void Add(WritingSystemId key, RichString value)
    {
        var writingSystemHandle = lexboxLcmApi.GetWritingSystemHandle(key);
        multiString.set_String(writingSystemHandle, RichTextMapping.ToTsString(value, ws => lexboxLcmApi.GetWritingSystemHandle(ws)));
    }

    public bool ContainsKey(WritingSystemId key)
    {
        if (multiString.StringCount == 0) return false;
        var tsString = multiString.get_String(lexboxLcmApi.GetWritingSystemHandle(key));
        return tsString.Length > 0;
    }

    public void Add(object key, object? value)
    {
        WritingSystemId keyWs;
        if (key is WritingSystemId typed)
        {
            keyWs = typed;
        }
        else if (key is string keyStr)
        {
            keyWs = keyStr;
        }
        else
        {
            throw new ArgumentException("unable to convert key to writing system id", nameof(key));
        }

        if (value is string valueStr)
        {
            Add(keyWs, valueStr);
        }
        else if (value is RichString valueRich)
        {
            Add(keyWs, valueRich);
        }
        else
        {
            throw new ArgumentException("unable to convert value to string", nameof(value));
        }
    }

    public bool Contains(object key)
    {
        return ContainsKey(key as WritingSystemId? ?? key as string ??
            throw new ArgumentException("unable to convert key to writing system id", nameof(key)));
    }


    public string this[WritingSystemId key]
    {
        get
        {
            var tsString = multiString.get_String(lexboxLcmApi.GetWritingSystemHandle(key));
            return tsString.Text;
        }
        set
        {
            var writingSystemHandle = lexboxLcmApi.GetWritingSystemHandle(key);
            multiString.set_String(writingSystemHandle, TsStringUtils.MakeString(value, writingSystemHandle));
        }
    }

    public object? this[object key]
    {
        get =>
            key switch
            {
                WritingSystemId keyWs => this[keyWs],
                string keyStr => this[keyStr],
                _ => throw new ArgumentException("unable to convert key to writing system id", nameof(key))
            };
        set
        {
            var valStr = value as string ??
                         throw new ArgumentException("unable to convert value to string", nameof(value));
            if (key is WritingSystemId keyWs)
            {
                this[keyWs] = valStr;
            }
            else if (key is string keyStr)
            {
                this[keyStr] = valStr;
            }
            else
            {
                throw new ArgumentException("unable to convert key to writing system id", nameof(key));
            }
        }
    }

    IDictionaryEnumerator IDictionary.GetEnumerator()
    {
        throw new NotSupportedException();
    }

    public void Remove(object key)
    {
        if (key is WritingSystemId keyWs)
        {
            Remove(keyWs);
        }
        else if (key is string keyStr)
        {
            Remove(keyStr);
        }
        else
        {
            throw new ArgumentException($"unable to convert key {key} to writing system id", nameof(key));
        }
    }

    public bool Remove(WritingSystemId key)
    {
        var containedKey = ContainsKey(key);
        var writingSystemHandle = lexboxLcmApi.GetWritingSystemHandle(key);
        multiString.set_String(writingSystemHandle, null);
        return containedKey;
    }

    public bool IsFixedSize => false;

    public IEnumerator<KeyValuePair<WritingSystemId, string>> GetEnumerator()
    {
        throw new NotSupportedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Clear()
    {
        throw new NotSupportedException();
    }

    public bool Contains(KeyValuePair<WritingSystemId, string> item)
    {
        throw new NotSupportedException();
    }

    public void CopyTo(KeyValuePair<WritingSystemId, string>[] array, int arrayIndex)
    {
        throw new NotSupportedException();
    }

    public bool Remove(KeyValuePair<WritingSystemId, string> item)
    {
        throw new NotSupportedException();
    }

    public void CopyTo(Array array, int index)
    {
    }

    public int Count => throw new NotSupportedException();

    public bool IsSynchronized => false;

    public object SyncRoot => this;

    public bool IsReadOnly => false;


    public bool TryGetValue(WritingSystemId key, out string value)
    {
        throw new NotSupportedException();
    }

    public ICollection<WritingSystemId> Keys => [];

    ICollection IDictionary.Values => Array.Empty<object>();

    ICollection IDictionary.Keys => Array.Empty<object>();

    public ICollection<string> Values => [];
}
