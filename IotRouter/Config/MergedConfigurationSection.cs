using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

public class MergedConfigurationSection : IConfigurationSection
{
    IConfigurationSection[] _configurationSections;

    private T GetFirst<T>(Func<IConfigurationSection, T> selector) 
        => GetFirst(selector, v => v != null);

    private T GetFirst<T>(Func<IConfigurationSection, T> selector, Func<T, bool> filter)
    {
        // Take first that matches, or last (that doesn't match)
        var collSelected = _configurationSections.Select(selector);
        var collFiltered = collSelected.Where(filter);
        if (collFiltered.Any())
            return collFiltered.First();
        else
            return collSelected.Last();
    }

    public MergedConfigurationSection(params IConfigurationSection[] configurationSections)
    {
        _configurationSections = configurationSections.Where(cs => cs != null).ToArray();
    }

    public string this[string key] {
        get => GetFirst(cs => cs[key]);
        set => throw new System.NotSupportedException();
    }

    public string Key => throw new System.NotImplementedException();

    public string Path => throw new System.NotImplementedException();

    public string Value 
    { 
        get => throw new System.NotImplementedException();
        set => throw new System.NotSupportedException();
    }

    public IEnumerable<IConfigurationSection> GetChildren()
    {
        throw new System.NotImplementedException();
    }

    public IChangeToken GetReloadToken()
    {
        throw new System.NotImplementedException();
    }

    public IConfigurationSection GetSection(string key)
    {
        return GetFirst(section => section.GetSection(key), s2 => s2.Exists());
    }
}
