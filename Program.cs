using System;
using System.IO;
using System.Linq;
using GvasFormat.Serialization;
using GvasFormat.Serialization.UETypes;

namespace P3RSaveViewer {
    class Program {
        static void Main(string[] args) {
            var path = args.Length == 0 
                ? Directory.GetFiles($@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\SEGA\P3R\Steam\76561198078824042", "*.sav")
                    .OrderByDescending(x => new FileInfo(x).LastWriteTime)
                    .FirstOrDefault()
                : args[0];
            
            var raw = File.ReadAllBytes(path);
            var decrypted = EnDecryptor.Decrypt(raw);
            using var stream = new MemoryStream(decrypted);
            var gvas = UESerializer.Read(stream);
            var header = gvas.Properties.OfType<UEGenericStructProperty>().FirstOrDefault();
            var saveName = header.Properties.OfType<UEStringProperty>().FirstOrDefault(x => x.Name == "SaveSlotName").Value;
            var firstName = new string(header.Properties.OfType<UEInt8Property>().Where(x => x.Name == "FirstName").Select(x => (char)x.Value).ToArray());
            var lastName = new string(header.Properties.OfType<UEInt8Property>().Where(x => x.Name == "LastName").Select(x => (char)x.Value).ToArray());
            var month = header.Properties.OfType<UEIntProperty>().FirstOrDefault(x => x.Name == "Month").Value;
            var day = header.Properties.OfType<UEIntProperty>().FirstOrDefault(x => x.Name == "Day").Value;
            var weekday = header.Properties.OfType<UEEnumProperty>().FirstOrDefault(x => x.Name == "Week").Value.Replace("ECldWeek::", "");
            var time = header.Properties.OfType<UEEnumProperty>().FirstOrDefault(x => x.Name == "TimeZone").Value.Replace("ECldTimeZone::", "");
            var lvl = header.Properties.OfType<UEUInt32Property>().FirstOrDefault(x => x.Name == "PlayerLevel").Value;
            var moon = header.Properties.OfType<UEEnumProperty>().FirstOrDefault(x => x.Name == "Age").Value.Replace("ECldMoonAge::", "");
            var playTime = header.Properties.OfType<UEUInt32Property>().FirstOrDefault(x => x.Name == "PlayTime").Value; // 30 tics per second
            var difficulty = header.Properties.OfType<UEUInt16Property>().FirstOrDefault(x => x.Name == "Difficulty").Value;
            var props = gvas.Properties.OfType<UEUInt32Property>()
                .Where(x => x.Name == "SaveDataArea")
                .Select((x, i) => new { i, x })
                .ToDictionary(x => x.i, x => x.x);
            var academics = props[5139].Value;
            var charm = props[5141].Value;
            var courage = props[5143].Value;

            Console.WriteLine(saveName);
            Console.WriteLine(new string('=', 64));
            Console.WriteLine($"{firstName} {lastName}\t{lvl} lvl");
            Console.WriteLine($"Playtime: {GetPlayTime(playTime)}");
            Console.WriteLine($"{weekday}, {day} {GetMonth(month)}\n    {time}");
            Console.WriteLine();
            Console.WriteLine($"Social skills:\n"
                            + $"  Academics: {academics}\n"
                            + $"      Charm: {charm}\n"
                            + $"    Courage: {courage}\n");
        }

        public static string GetPlayTime(uint ticks) {
            var sec = ticks / 30.0;
            var hours = (int)(sec / 3600);
            var minutes = (int)(sec % 60);
            return $"{hours:00}:{minutes:00}";
        }

        public static string GetMonth(int month)
            => month switch {
                 1 => "January",
                 2 => "February",
                 3 => "March",
                 4 => "April",
                 5 => "May",
                 6 => "June",
                 7 => "July",
                 8 => "August",
                 9 => "September",
                10 => "October",
                11 => "November",
                12 => "December",
                _ => $"{month}"
            };
    }
}

/*
TODO: Is this correct?
Level	Courage	Charm	Academics
Level 2	     15	   15	       20
Level 3	     30	   30	       55
Level 4	     45	   45	      100
Level 5	     60	   70	      155
Level 6	     80	  100	      230
*/