CommentRemover
=========

## Overview
CommnetRemover is tool of removing comment for C# code.
## Description
The elimination targets is below.
* Single line comments
 ```
// for single line comments
```
* Multiple line comments
```
/* for multi line comments */
```
* XML tags comments  
using *-x* option so as to remove this type.
```
/// XML tags displayed in a code comment
```
### Example
Before:  
```
/*
Any comments
*/
Console.Write("Hello ");
Console.Write(/*Hello*/"Wolrd");//int i = 0;
//Console.Write("World");
Console.WriteLine("!!");
```
After:
```
Console.Write("Hello ");
Console.Write("Wolrd");
Console.WriteLine("!!");
```

## Usage
```
$ cd ./CommentRemover
$ dotnet run [-x] [file or directory]
```
### Output example
```
$ cd ./CommentRemover
$ dotnet run ../file01.cs ../dir01 ../file02.cs
[File]: ../file01.cs
  84 lines removed.
  116 lines removed a part.
[Dir]: ../dir01
  10 files rewritten. (included 15 files in directory.)
  1996 lines removed.
  21 lines removed a part.
[File]: ../file02.cs
  6 lines removed.
  67 lines removed a part.
```
