using System.IO;
using System.Text;
using UnityEngine;

public static class FileHelper
{
    public static string GetRandomApplicationFileName(string subFolder, string extension)
    {
        const string randomCharacterSource = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
        const int randomLength = 10;

        var folder = Path.Combine(Application.persistentDataPath, subFolder);
        Directory.CreateDirectory(folder);

        string filePath;
        var stringBuilder = new StringBuilder();
        do
        {
            stringBuilder.Append(Application.productName);
            stringBuilder.Replace(' ', '_');
            stringBuilder.Append('_');
            for (int i = 0; i < randomLength; i++)
            {
                stringBuilder.Append(randomCharacterSource[Random.Range(0, randomCharacterSource.Length)]);
            }
            if (!extension.StartsWith('.')) stringBuilder.Append('.');
            stringBuilder.Append(extension);
            filePath = Path.Combine(folder, stringBuilder.ToString());
        }
        while (File.Exists(filePath));

        return filePath;
    }
}
