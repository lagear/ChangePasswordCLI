﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Extensions.Configuration;
using Sharprompt;

public static class EnumerableExtensions
{
    public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
    {
        var seenKeys = new HashSet<TKey>();
        foreach (var element in source)
        {
            var key = keySelector(element);
            if (seenKeys.Add(key))
            {
                yield return element;
            }
        }
    }
}

public class Program
{
    class SpoonPassword
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public PasswordType PasswordType { get; set; }

    }

    enum PasswordType
    {
        Sql,
        Oracle
    }

    static void Main(string[] args)
    {
        string spoonDirectory;
        string scriptDirectory;
        string encrFile = "Encr.bat";
        string encrPath;

        string currentDirectory = Directory.GetCurrentDirectory();

        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        var configuration = builder.Build();

        string folderSpoonDirectoryPath = configuration["SpoonDirectoryPath"];
        string folderScriptDirectoryPath = configuration["ScriptDirectoryPath"];

        Console.WriteLine("Directorio Actual: " + currentDirectory);


        do
        {
            spoonDirectory = Prompt.Input<string>("Ingrese la ruta completa de Spoon: ", folderSpoonDirectoryPath);
            encrPath = $"{spoonDirectory}\\{encrFile}";
        } while (!(Directory.Exists(spoonDirectory) && File.Exists(encrPath)));

        do
        {
            scriptDirectory = Prompt.Input<string>("Ingrese la ruta completa de los Scripts: ", folderScriptDirectoryPath);
        } while (!Directory.Exists(spoonDirectory));

        string zipFilePath = $"{currentDirectory}\\BackupScripts-{DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss_fff")}.zip";

        ZipFolder(scriptDirectory, zipFilePath);


        Console.WriteLine("Se ha creado el archivo: " + zipFilePath);

        Console.WriteLine("---------------------------------------------------------");
        Console.WriteLine("Buscando passwords en el directorio: " + scriptDirectory);

        var passwords = GetPasswords(scriptDirectory);


        Console.WriteLine("Password encontrados:");
        Console.WriteLine("UserName\tPassword\tTipo");
        Console.WriteLine("-------------------------------------------------------");
        foreach (var spoonPassword in passwords)
        {
            Console.WriteLine("{0}\t{1}\t{2}", spoonPassword.UserName, spoonPassword.Password, spoonPassword.PasswordType);
        }


        // Password input
        var newPasswordSql = Prompt.Input<string>("Ingrese el password en texto plano para SQL: ");
        var newPasswordOracle = Prompt.Input<string>("Ingrese el password en texto plano para Oracle: ");

        // Encription Password
        var newPasswordSqlEncrypted = GetEncrPassword(encrPath, newPasswordSql);
        var newPasswordOracleEncrypted = GetEncrPassword(encrPath, newPasswordOracle);


        // Confirmation
        var answer = Prompt.Confirm("Esta seguro de reemplezar los password?", defaultValue: false);
        if (answer)
        {
            foreach (var spoonPassword in passwords)
            {
                switch (spoonPassword.PasswordType)
                {
                    case PasswordType.Sql:
                        ReplacePasswords(scriptDirectory, spoonPassword.Password, newPasswordSqlEncrypted, PasswordType.Sql);
                        break;
                    case PasswordType.Oracle:
                        ReplacePasswords(scriptDirectory, spoonPassword.Password, newPasswordOracleEncrypted, PasswordType.Oracle);
                        break;
                }
            }

            Console.WriteLine("Revisando password reemplazados.");
            var newPasswords = GetPasswords(scriptDirectory);
            if (newPasswords.Count != 2)
            {
                Console.WriteLine($"ERROR. Se encontraron {newPasswords.Count} password y se esperaba 2.");
            }

            foreach (var spoonPassword in newPasswords)
            {
                switch (spoonPassword.PasswordType)
                {
                    case PasswordType.Sql:
                        if (spoonPassword.Password != newPasswordSqlEncrypted)
                        {
                            Console.WriteLine($"ERROR. Password para SQL Server no es el esperado");
                        }
                        break;
                    case PasswordType.Oracle:
                        if (spoonPassword.Password != newPasswordOracleEncrypted)
                        {
                            Console.WriteLine($"ERROR. Password para Oracle no es el esperado");
                        }
                        break;
                }
            }


            Console.WriteLine("Passwords reemplazados correctamente. Presione cualquier tecla para continuar");
            Console.ReadKey();
            return;
        }

        Console.WriteLine("Usted ha cancelado el proceso. Presione cualquier tecla para continuar");
        Console.ReadKey();
    }

    static void ZipFolder(string folderPath, string zipFilePath)
    {
        ZipFile.CreateFromDirectory(folderPath, zipFilePath);
    }

    static IEnumerable<SpoonPassword> DistinctByTwoFields(IEnumerable<SpoonPassword> source, Func<SpoonPassword, string> keySelector, Func<SpoonPassword, PasswordType> secondaryKeySelector)
    {
        var uniqueKeys = new HashSet<string>();
        return source.Where(elem => uniqueKeys.Add(keySelector(elem) + ":" + secondaryKeySelector(elem)));
    }

    static List<SpoonPassword> GetPasswords(string directoryPath)
    {
        string passwordPattern = @"<password>Encrypted .*?</password>";
        string userPattern = @"<username>.*?</username>";
        string userName = string.Empty;

        var result = new List<SpoonPassword>();

        List<string> passwords = new List<string>();
        string[] files = Directory.GetFiles(directoryPath, "*.ktr", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            string[] lines = File.ReadAllLines(file);
            foreach (string line in lines)
            {
                Match matchUser = Regex.Match(line, userPattern);
                if (matchUser.Success)
                {
                    userName = line.Trim();
                    continue;
                }

                Match match = Regex.Match(line, passwordPattern);
                if (match.Success)
                {
                    string password = line.Replace("<password>", string.Empty).Replace("</password>", string.Empty).Trim();
                    if (password != "Encrypted")
                    {
                        PasswordType passwordType = default;
                        if (userName.Contains("sql"))
                        {
                            passwordType = PasswordType.Sql;
                        }
                        else if (userName.Contains("ORA"))
                        {
                            passwordType = PasswordType.Oracle;
                        }
                        result.Add(new SpoonPassword { UserName = userName, Password = password, PasswordType = passwordType });
                    }
                }
            }
        }

        return DistinctByTwoFields(result, x => x.Password, x => x.PasswordType).ToList();
    }

    static void ReplacePasswords(string directoryPath, string oldPassword, string newPassword, PasswordType passwordType)
    {
        var result = new List<SpoonPassword>();

        List<string> passwords = new List<string>();
        string[] files = Directory.GetFiles(directoryPath, "*.ktr", SearchOption.AllDirectories);
        Encoding encoding = Encoding.UTF8; // Specify the encoding used by the file
        foreach (string file in files)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(file);
            XmlNode root = xmlDoc.DocumentElement;

            XmlNodeList connectionNodes = root.SelectNodes("//connection"); // Select all connection nodes using Xpath

            foreach (XmlNode node in connectionNodes)
            {
                // Access specific child elements of the connection node
                if (node != null)
                {
                    XmlNode nameNode = node.SelectSingleNode("username");
                    XmlNode passwordNode = node.SelectSingleNode("password");
                    XmlNode typeNode = node.SelectSingleNode("type");

                    if (typeNode != null && nameNode != null && passwordNode != null)
                    {
                        switch (passwordType)
                        {
                            case PasswordType.Sql:
                                if (typeNode.InnerText.ToLower().Contains("sql"))
                                {
                                    passwordNode.InnerText = passwordNode.InnerText.Replace(oldPassword, newPassword);
                                }
                                break;
                            case PasswordType.Oracle:
                                if (typeNode.InnerText.ToLower().Contains("ora"))
                                {
                                    passwordNode.InnerText = passwordNode.InnerText.Replace(oldPassword, newPassword);
                                }
                                break;
                        }
                    }
                }
            }

            xmlDoc.Save(file);
        }
    }

    static string GetEncrPassword(string encPath, string password)
    {
        // Create a new process instance
        Process process = new Process();

        // Set the required properties for running the batch file
        process.StartInfo.FileName = encPath;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.CreateNoWindow = true;

        // Set the parameter for the batch file
        process.StartInfo.Arguments = password;

        // Start the process
        process.Start();

        // Read the output of the batch file
        string output = process.StandardOutput.ReadToEnd();

        // Wait for the process to exit
        process.WaitForExit();

        // Display the output
        return output.Replace("\r\n", "");
    }
}

