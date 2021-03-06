﻿//
//  Emoji.Wpf — Emoji support for WPF
//
//  Copyright © 2017—2018 Sam Hocevar <sam@hocevar.net>
//
//  This library is free software. It comes without any warranty, to
//  the extent permitted by applicable law. You can redistribute it
//  and/or modify it under the terms of the Do What the Fuck You Want
//  to Public License, Version 2, as published by the WTFPL Task Force.
//  See http://www.wtfpl.net/ for more details.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Emoji.Wpf.Data
{
    public class Emoji
    {
        public string Name { get; set; }
        public string Text { get; set; }

        public Group Group => SubGroup.Group;
        public SubGroup SubGroup;

        public IList<Emoji> VariationList { get; } = new List<Emoji>();

        public static IEnumerable<Emoji> ListAll
        {
            get
            {
                foreach (var group in Group.AllGroups)
                    foreach (var emoji in group.EmojiList)
                        yield return emoji;
            }
        }
    }

    public class SubGroup
    {
        public string Name { get; set; }
        public Group Group;

        public IList<Emoji> EmojiList { get; } = new List<Emoji>();
    }

    public class Group
    {
        public string Name { get; set; }
        public string Icon => SubGroups[0].EmojiList[0].Text;

        public IList<SubGroup> SubGroups { get; } = new List<SubGroup>();

        public IEnumerable<Emoji> EmojiList
        {
            get
            {
                foreach (var subgroup in SubGroups)
                    foreach (var emoji in subgroup.EmojiList)
                        yield return emoji;
            }
        }

        public static readonly IEnumerable<Group> AllGroups = GetAllGroups();

        private static IEnumerable<Group> GetAllGroups()
        {
            var font = new EmojiTypeface();

            var match_group = new Regex(@"^# group: (.*)");
            var match_subgroup = new Regex(@"^# subgroup: (.*)");
            var match_sequence = new Regex(@"^([0-9A-F ]+[0-9A-F]).*; fully-qualified.*# [^ ]* (.*)");
            var list = new List<Group>();

            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("emoji-test.txt"))
            using (StreamReader sr = new StreamReader(s))
            {
                Group last_group = null;
                SubGroup last_subgroup = null;
                Emoji last_emoji = null;

                foreach (var line in sr.ReadToEnd().Split('\r', '\n'))
                {
                    var m = match_group.Match(line);
                    if (m.Success)
                    {
                        last_group = new Group() { Name = m.Groups[1].ToString() };
                        list.Add(last_group);
                        continue;
                    }

                    m = match_subgroup.Match(line);
                    if (m.Success)
                    {
                        last_subgroup = new SubGroup() { Name = m.Groups[1].ToString(), Group = last_group };
                        last_group.SubGroups.Add(last_subgroup);
                        continue;
                    }

                    m = match_sequence.Match(line);
                    if (m.Success)
                    {
                        string sequence = m.Groups[1].ToString();
                        string name = m.Groups[2].ToString();
                        bool is_skin_variation = false;

                        string text = "";
                        foreach (var item in sequence.Split(' '))
                        {
                            int codepoint = Convert.ToInt32(item, 16);
                            if (codepoint >= 0x1f3fb && codepoint <= 0x1f3ff)
                                is_skin_variation = (text.Length > 0);
                            text += char.ConvertFromUtf32(codepoint);
                        }

                        // Only include emojis that we know how to render
                        if (!font.CanRender(text))
                            continue;

                        var emoji = new Emoji() { Name = name, Text = text, SubGroup = last_subgroup };
                        if (is_skin_variation)
                        {
                            // We assume this is a variation of the previous emoji
                            if (last_emoji.VariationList.Count == 0)
                                last_emoji.VariationList.Add(last_emoji);
                            last_emoji.VariationList.Add(emoji);
                        }
                        else
                        {
                            last_emoji = emoji;
                            last_subgroup.EmojiList.Add(emoji);
                        }
                    }
                }
            }

            return list;
        }
    }
}

