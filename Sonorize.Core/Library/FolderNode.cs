using Sonorize.Core.Core;

﻿namespace Sonorize.Core.Library;

public class FolderNode
{
    public required string Name { get; set; }
    public required string Path { get; set; }
    public List<FolderNode> Children { get; set; } = [];
    public List<Song> Songs { get; set; } = [];
}
