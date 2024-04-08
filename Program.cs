using System;
using System.IO;
using System.Linq;
using System.Reflection;
using GvasFormat.Serialization;
using GvasFormat.Serialization.UETypes;
using static P3RSaveViewer.Program;

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
            var gvas = UESerializer.Read(stream); // Uses https://github.com/13xforever/gvas-converter
            var header = gvas.Properties.OfType<UEGenericStructProperty>().FirstOrDefault();
            //Console.WriteLine(string.Join("\n", header.Properties.Select(x => $"{x.Name}")));
            var saveName = header.Properties.OfType<UEStringProperty>().FirstOrDefault(x => x.Name == "SaveSlotName")?.Value;
            var firstName = new string(header.Properties.OfType<UEInt8Property>().Where(x => x.Name == "FirstName").Select(x => (char)x.Value).ToArray());
            var lastName = new string(header.Properties.OfType<UEInt8Property>().Where(x => x.Name == "LastName").Select(x => (char)x.Value).ToArray());
            var month = header.Properties.OfType<UEIntProperty>().FirstOrDefault(x => x.Name == "Month")?.Value;
            var day = header.Properties.OfType<UEIntProperty>().FirstOrDefault(x => x.Name == "Day")?.Value;
            var weekday = header.Properties.OfType<UEEnumProperty>().FirstOrDefault(x => x.Name == "Week")?.Value.Replace("ECldWeek::", "");
            var time = header.Properties.OfType<UEEnumProperty>().FirstOrDefault(x => x.Name == "TimeZone")?.Value.Replace("ECldTimeZone::", "");
            var lvl = header.Properties.OfType<UEUInt32Property>().FirstOrDefault(x => x.Name == "PlayerLevel")?.Value;
            var moon = header.Properties.OfType<UEEnumProperty>().FirstOrDefault(x => x.Name == "Age")?.Value.Replace("ECldMoonAge::", "");
            var playTime = header.Properties.OfType<UEUInt32Property>().FirstOrDefault(x => x.Name == "PlayTime")?.Value; // 30 tics per second
            var difficulty = header.Properties.OfType<UEUInt16Property>().FirstOrDefault(x => x.Name == "Difficulty")?.Value;
            var props = gvas.Properties.OfType<UEUInt32Property>()
                .Where(x => x.Name == "SaveDataArea")
                .Select((x, i) => new { i, x })
                .ToDictionary(x => x.i, x => x.x);
            var yenIdx = props.FirstOrDefault(x => x.Value.ValueId == 7257).Key;
	    uint yen;
            if (yenIdx == 0){
            yen = 0;}
            else{
            yen = props[yenIdx].Value;}

            int courage;
            int academics;
            int charm;
	    var skillsValueIdBase= 5352;
            var skills1Idx = props.FirstOrDefault(x => x.Value.ValueId == (skillsValueIdBase)).Key;
            var skills2Idx = props.FirstOrDefault(x => x.Value.ValueId == (skillsValueIdBase+2)).Key;
            var skills3Idx = props.FirstOrDefault(x => x.Value.ValueId == (skillsValueIdBase+4)).Key;
   
            if (skills1Idx == 0){
            academics = 0;}
            else{
            academics = (int)props[skills1Idx].Value;}

            if (skills2Idx == 0){
            charm = 0;}
            else{
            charm = (int)props[skills2Idx].Value;}

            if (skills3Idx == 0){
            courage = 0;}
            else{
            courage = (int)props[skills3Idx].Value;}

            Console.WriteLine($" File: \"{path}\" [{new FileInfo(path).LastWriteTime}]");
            Console.WriteLine(new string('=', 128));
            Console.WriteLine($"{firstName} {lastName}");
            Console.WriteLine($"{yen} Yen");
            Console.WriteLine($"Lv {lvl}");
            //Console.WriteLine($"Lv {lvl} ({exp} EXP)");
            Console.WriteLine($"Play time: {GetPlayTime(playTime ?? 0)}");
            Console.WriteLine($"{weekday}, {day} {GetMonth(month ?? 0)}, {time}");
            Console.WriteLine();
            Console.WriteLine($"Social skills:\n"
                            + $"  Academics: Lv {(int)FromPoints<AcademicsRank>(academics)} {FromPoints<AcademicsRank>(academics),-13} ({academics,3}) => Next lvl in {NextRank<AcademicsRank>(academics),3} points\n"
                            + $"      Charm: Lv {(int)FromPoints<CharmRank>(charm)} {FromPoints<CharmRank>(charm),-13} ({charm,3}) => Next lvl in {NextRank<CharmRank>(charm),3} points\n"
                            + $"    Courage: Lv {(int)FromPoints<CourageRank>(courage)} {FromPoints<CourageRank>(courage),-13} ({courage,3}) => Next lvl in {NextRank<CourageRank>(courage),3} points");
            Console.WriteLine();
            //Console.WriteLine(string.Join("\n", props.Select(x => $"[{x.Key,6}] {x.Value.Name}: {x.Value.Value}"))); // For debug
        }

        public static string GetPlayTime(uint ticks) {
            var sec = ticks / 30.0;
            var hours = (int)(sec / 3600);
            var minutes = (int)(sec % 3600 / 60);
            return $"{hours:0} hr {minutes:0} m {sec % 60:0} s";
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
        
        [AttributeUsage(AttributeTargets.Field)]
        public class SocialRankAttribute : Attribute {
            public int Min { get; }
            public int Max { get; }
            public string Name { get; }

            public SocialRankAttribute(int min = 0, int max = int.MaxValue, string name = null) {
                Min = min < 0 ? 0 : min;
                Max = max < min ? int.MaxValue : max;
                Name = name;
            }
        }
        
        public enum AcademicsRank {
            [SocialRank(0, 20)]
            Slacker = 1,
            [SocialRank(20, 55)]
            Average = 2,
            [SocialRank(55, 100, "Above Average")]
            AboveAverage = 3,
            [SocialRank(100, 155)]
            Smart = 4,
            [SocialRank(155, 230)]
            Intelligent = 5,
            [SocialRank(230)]
            Genius = 6
        }

        public enum CharmRank {
            [SocialRank(0, 15)]
            Plain = 1,
            [SocialRank(15, 30)]
            Unpolished = 2,
            [SocialRank(30, 45)]
            Confident = 3,
            [SocialRank(45, 70)]
            Smooth = 4,
            [SocialRank(70, 100)]
            Popular = 5,
            [SocialRank(100)]
            Charismatic = 6
        }

        public enum CourageRank {
            [SocialRank(0, 15)]
            Timid = 1,
            [SocialRank(15, 30)]
            Ordinary = 2,
            [SocialRank(30, 45)]
            Determined = 3,
            [SocialRank(45, 60)]
            Tough = 4,
            [SocialRank(60, 80)]
            Fearless = 5,
            [SocialRank(80)]
            Badass = 6
        }

        public static string ToString<T>(T rank)
            => GetAttribute(rank)?.Name ?? $"{rank}";
        
        public static int ToPoints<T>(T rank)
            => GetAttribute(rank)?.Min ?? 0;
        
        public static T FromPoints<T>(int points)
            => (T) typeof(T).GetFields(BindingFlags.Static | BindingFlags.Public).Where(x => {
                    var attr = x.GetCustomAttributes(typeof(SocialRankAttribute), false).OfType<SocialRankAttribute>().FirstOrDefault();
                    return points >= attr.Min && points < attr.Max;
                }).FirstOrDefault()
                ?.GetValue(null);
        
        public static SocialRankAttribute GetAttribute<T>(T rank)
            => typeof(T).GetMember($"{rank}")
                .FirstOrDefault()
                ?.GetCustomAttributes(typeof(SocialRankAttribute), false)
                .OfType<SocialRankAttribute>()
                .FirstOrDefault();
        
        public static int NextRank<T>(int points)
            => GetAttribute(FromPoints<T>(points))?.Max - points ?? 0;
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
