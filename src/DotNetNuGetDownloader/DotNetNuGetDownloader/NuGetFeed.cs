﻿namespace DotNetNuGetDownloader;

public class NuGetFeed
{
    public string Name { get; }

    public string Feed { get; }

    public NuGetFeed(string name, string feed = null)
    {
        Name = name;
        Feed = feed;
    }

    public string Username { get; set; }

    public string Password { get; set; }
}