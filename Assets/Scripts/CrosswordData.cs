using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crossword3D
{
    public enum WordOrientation
    {
        Horizontal,
        Vertical
    }

    [System.Serializable]
    public class WordEntry
    {
        public int id;
        public string clue;
        public string answer;
        public int startX;
        public int startY;
        public WordOrientation orientation;

        [System.NonSerialized]
        public bool isSolved = false;

        public WordEntry() { }

        public WordEntry(int id, string clue, string answer, int startX, int startY, WordOrientation orientation)
        {
            this.id = id;
            this.clue = clue;
            this.answer = answer.Trim().ToUpperInvariant();
            this.startX = startX;
            this.startY = startY;
            this.orientation = orientation;
        }

        public bool ContainsCell(int x, int y, out int charIndex)
        {
            charIndex = -1;
            if (orientation == WordOrientation.Horizontal)
            {
                if (y == startY && x >= startX && x < startX + answer.Length)
                {
                    charIndex = x - startX;
                    return true;
                }
            }
            else
            {
                if (x == startX && y >= startY && y < startY + answer.Length)
                {
                    charIndex = y - startY;
                    return true;
                }
            }
            return false;
        }

        public Vector2Int GetCellCoordinates(int charIndex)
        {
            if (orientation == WordOrientation.Horizontal)
                return new Vector2Int(startX + charIndex, startY);
            else
                return new Vector2Int(startX, startY + charIndex);
        }
    }

    [System.Serializable]
    public class CrosswordLevel
    {
        public string levelId;
        public string title;
        public string description;
        public int gridWidth;
        public int gridHeight;
        public List<WordEntry> words = new List<WordEntry>();

        public WordEntry FindWordAt(int x, int y, WordOrientation preferredOrientation)
        {
            WordEntry fallback = null;
            foreach (var word in words)
            {
                if (word.ContainsCell(x, y, out _))
                {
                    if (word.orientation == preferredOrientation)
                        return word;
                    fallback = word;
                }
            }
            return fallback;
        }

        public List<WordEntry> GetAllWordsAt(int x, int y)
        {
            var list = new List<WordEntry>();
            foreach (var word in words)
            {
                if (word.ContainsCell(x, y, out _))
                    list.Add(word);
            }
            return list;
        }
    }

    [System.Serializable]
    public class CrosswordLevelList
    {
        public List<CrosswordLevel> levels = new List<CrosswordLevel>();
    }
}
