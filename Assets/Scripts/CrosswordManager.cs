using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crossword3D
{
    public class CrosswordManager : MonoBehaviour
    {
        public static CrosswordManager Instance { get; private set; }

        [Header("Level Data")]
        public CrosswordLevel currentLevel;
        public int currentLevelIndex = 0;

        [Header("Current Selection")]
        public Vector2Int selectedCoord = new Vector2Int(-1, -1);
        public WordOrientation currentOrientation = WordOrientation.Horizontal;
        public WordEntry currentWordEntry = null;

        [Header("Game State")]
        public int score = 0;
        public int hintsRemaining = 5;
        public float timer = 0f;
        public bool isGameActive = false;
        public bool isLevelCompleted = false;

        private Dictionary<Vector2Int, Cell3D> cellMap = new Dictionary<Vector2Int, Cell3D>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            LoadLevelByIndex(currentLevelIndex);
        }

        private void Update()
        {
            if (!isGameActive || isLevelCompleted) return;

            timer += Time.deltaTime;

            HandlePhysicalKeyboardInput();
            HandleNavigationInput();
        }

        public void LoadLevelByIndex(int index)
        {
            currentLevelIndex = index;
            string levelFileName = $"Crosswords/level{index + 1}";
            TextAsset jsonAsset = Resources.Load<TextAsset>(levelFileName);

            if (jsonAsset != null)
            {
                currentLevel = JsonUtility.FromJson<CrosswordLevel>(jsonAsset.text);
            }
            else
            {
                // Fallback to built-in default level if file not yet loaded in Resources
                currentLevel = CreateDefaultITLevel();
            }

            StartLevel(currentLevel);
        }

        public void LoadNextLevel()
        {
            currentLevelIndex = (currentLevelIndex + 1) % 2;
            LoadLevelByIndex(currentLevelIndex);
        }

        public void StartLevel(CrosswordLevel level)
        {
            currentLevel = level;
            isGameActive = true;
            isLevelCompleted = false;
            timer = 0f;
            score = 0;
            hintsRemaining = 5;

            // Generate 3D Board
            if (BoardGenerator3D.Instance != null)
            {
                BoardGenerator3D.Instance.GenerateBoard(currentLevel, out cellMap);
            }

            // Select first available word
            if (currentLevel.words.Count > 0)
            {
                SelectWord(currentLevel.words[0]);
            }

            // Update UI
            if (CrosswordUI.Instance != null)
            {
                CrosswordUI.Instance.InitializeLevelUI(currentLevel);
            }
        }

        public void OnCellClicked(int x, int y)
        {
            if (!isGameActive || isLevelCompleted) return;

            Vector2Int clickedCoord = new Vector2Int(x, y);

            if (selectedCoord == clickedCoord)
            {
                // Toggle orientation when clicking already selected cell
                ToggleOrientation();
            }
            else
            {
                selectedCoord = clickedCoord;
                UpdateActiveWord();
            }

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySelect();

            UpdateCellHighlights();
        }

        public void SelectWord(WordEntry word)
        {
            if (word == null) return;
            currentWordEntry = word;
            currentOrientation = word.orientation;
            selectedCoord = word.GetCellCoordinates(0);

            // Find first unsolved cell in the word
            for (int i = 0; i < word.answer.Length; i++)
            {
                Vector2Int c = word.GetCellCoordinates(i);
                if (cellMap.ContainsKey(c) && !cellMap[c].isSolved && cellMap[c].currentChar == ' ')
                {
                    selectedCoord = c;
                    break;
                }
            }

            UpdateCellHighlights();

            // Focus Camera
            if (cellMap.ContainsKey(selectedCoord) && CameraOrbitController.Instance != null)
            {
                CameraOrbitController.Instance.FocusOnPosition(cellMap[selectedCoord].transform.position);
            }

            if (CrosswordUI.Instance != null)
            {
                CrosswordUI.Instance.HighlightClueInList(word.id, word.orientation);
            }
        }

        public void ToggleOrientation()
        {
            WordOrientation opposite = (currentOrientation == WordOrientation.Horizontal) ? WordOrientation.Vertical : WordOrientation.Horizontal;
            List<WordEntry> available = currentLevel.GetAllWordsAt(selectedCoord.x, selectedCoord.y);

            WordEntry otherWord = available.Find(w => w.orientation == opposite);
            if (otherWord != null)
            {
                currentOrientation = opposite;
                currentWordEntry = otherWord;
            }
            else
            {
                // If only one orientation exists at this cell, keep it
            }

            UpdateCellHighlights();
        }

        private void UpdateActiveWord()
        {
            currentWordEntry = currentLevel.FindWordAt(selectedCoord.x, selectedCoord.y, currentOrientation);
            if (currentWordEntry != null)
            {
                currentOrientation = currentWordEntry.orientation;
            }

            if (CrosswordUI.Instance != null && currentWordEntry != null)
            {
                CrosswordUI.Instance.HighlightClueInList(currentWordEntry.id, currentWordEntry.orientation);
            }
        }

        private void UpdateCellHighlights()
        {
            // Clear previous highlights
            foreach (var kvp in cellMap)
            {
                kvp.Value.SetSelected(false);
                kvp.Value.SetWordHighlight(false);
            }

            // Highlight word
            if (currentWordEntry != null)
            {
                for (int i = 0; i < currentWordEntry.answer.Length; i++)
                {
                    Vector2Int coord = currentWordEntry.GetCellCoordinates(i);
                    if (cellMap.ContainsKey(coord))
                    {
                        cellMap[coord].SetWordHighlight(true);
                    }
                }
            }

            // Highlight selected cell
            if (cellMap.ContainsKey(selectedCoord))
            {
                cellMap[selectedCoord].SetSelected(true);
            }
        }

        public void InputCharacter(char c)
        {
            if (!isGameActive || isLevelCompleted || !cellMap.ContainsKey(selectedCoord)) return;

            Cell3D currentCell = cellMap[selectedCoord];
            if (currentCell.isSolved)
            {
                AdvanceCursor(1);
                return;
            }

            currentCell.SetChar(c);

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayClick();

            // Check if any word intersecting this cell is now fully completed
            CheckWordsAtCell(selectedCoord.x, selectedCoord.y);

            // Automatically move to next cell in active word
            AdvanceCursor(1);

            CheckLevelCompletion();
        }

        public void DeleteCharacter()
        {
            if (!isGameActive || isLevelCompleted || !cellMap.ContainsKey(selectedCoord)) return;

            Cell3D currentCell = cellMap[selectedCoord];
            if (!currentCell.isSolved && currentCell.currentChar != ' ')
            {
                currentCell.ClearChar();
                if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
            }
            else
            {
                // If already empty, step backwards and clear that one
                AdvanceCursor(-1);
                if (cellMap.ContainsKey(selectedCoord) && !cellMap[selectedCoord].isSolved)
                {
                    cellMap[selectedCoord].ClearChar();
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
                }
            }
        }

        private void AdvanceCursor(int direction)
        {
            if (currentWordEntry == null) return;

            if (currentWordEntry.ContainsCell(selectedCoord.x, selectedCoord.y, out int currentIndex))
            {
                int nextIndex = currentIndex + direction;
                if (nextIndex >= 0 && nextIndex < currentWordEntry.answer.Length)
                {
                    selectedCoord = currentWordEntry.GetCellCoordinates(nextIndex);
                    UpdateCellHighlights();
                }
            }
        }

        private void CheckWordsAtCell(int x, int y)
        {
            List<WordEntry> words = currentLevel.GetAllWordsAt(x, y);

            foreach (var word in words)
            {
                if (word.isSolved) continue;

                bool matches = true;
                bool isFull = true;

                for (int i = 0; i < word.answer.Length; i++)
                {
                    Vector2Int coord = word.GetCellCoordinates(i);
                    if (cellMap.ContainsKey(coord))
                    {
                        char current = cellMap[coord].currentChar;
                        if (current == ' ')
                        {
                            isFull = false;
                            matches = false;
                            break;
                        }
                        if (current != word.answer[i])
                        {
                            matches = false;
                        }
                    }
                }

                if (isFull && matches)
                {
                    // Word is correct!
                    word.isSolved = true;
                    score += word.answer.Length * 50;

                    for (int i = 0; i < word.answer.Length; i++)
                    {
                        Vector2Int coord = word.GetCellCoordinates(i);
                        if (cellMap.ContainsKey(coord))
                        {
                            cellMap[coord].MarkSolved();
                        }
                    }

                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlayCorrectWord();

                    if (CrosswordUI.Instance != null)
                        CrosswordUI.Instance.MarkClueSolved(word.id, word.orientation);
                }
                else if (isFull && !matches)
                {
                    // Word is completely typed but incorrect
                    for (int i = 0; i < word.answer.Length; i++)
                    {
                        Vector2Int coord = word.GetCellCoordinates(i);
                        if (cellMap.ContainsKey(coord) && !cellMap[coord].isSolved)
                        {
                            cellMap[coord].FlashError();
                        }
                    }
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlayError();
                }
            }
        }

        public void UseHint()
        {
            if (!isGameActive || isLevelCompleted || hintsRemaining <= 0) return;
            if (!cellMap.ContainsKey(selectedCoord)) return;

            Cell3D cell = cellMap[selectedCoord];
            if (cell.isSolved) return;

            hintsRemaining--;
            score = Mathf.Max(0, score - 20);

            cell.SetChar(cell.correctChar);
            cell.MarkSolved();

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayHint();

            CheckWordsAtCell(selectedCoord.x, selectedCoord.y);
            AdvanceCursor(1);
            CheckLevelCompletion();

            if (CrosswordUI.Instance != null)
                CrosswordUI.Instance.UpdateStats(timer, score, hintsRemaining);
        }

        public void CheckLevelCompletion()
        {
            bool allSolved = true;
            foreach (var word in currentLevel.words)
            {
                if (!word.isSolved)
                {
                    allSolved = false;
                    break;
                }
            }

            if (allSolved)
            {
                isLevelCompleted = true;
                isGameActive = false;

                // Time bonus
                int timeBonus = Mathf.Max(0, 500 - Mathf.RoundToInt(timer));
                score += timeBonus;

                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayWin();

                if (CrosswordUI.Instance != null)
                    CrosswordUI.Instance.ShowVictoryScreen(timer, score);
            }
        }

        private void HandlePhysicalKeyboardInput()
        {
            if (Input.anyKeyDown)
            {
                string inputStr = Input.inputString;
                if (!string.IsNullOrEmpty(inputStr))
                {
                    foreach (char c in inputStr)
                    {
                        if (char.IsLetter(c))
                        {
                            InputCharacter(c);
                        }
                    }
                }

                if (Input.GetKeyDown(KeyCode.Backspace))
                {
                    DeleteCharacter();
                }

                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Tab))
                {
                    ToggleOrientation();
                }
            }
        }

        private void HandleNavigationInput()
        {
            int dx = 0;
            int dy = 0;

            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) dx = 1;
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) dx = -1;
            else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) dy = -1;
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) dy = 1;

            if (dx != 0 || dy != 0)
            {
                Vector2Int next = new Vector2Int(selectedCoord.x + dx, selectedCoord.y + dy);
                if (cellMap.ContainsKey(next))
                {
                    selectedCoord = next;
                    UpdateActiveWord();
                    UpdateCellHighlights();
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
                }
            }
        }

        private CrosswordLevel CreateDefaultITLevel()
        {
            CrosswordLevel lvl = new CrosswordLevel();
            lvl.levelId = "it_multimedia_01";
            lvl.title = "Мультимедиа и 3D Графика";
            lvl.description = "Кроссворд по основам интерактивных мультимедийных приложений и трёхмерной графики";
            lvl.gridWidth = 12;
            lvl.gridHeight = 14;

            lvl.words.Add(new WordEntry(1, "Программа, вычисляющая цвет пикселей и освещение 3D-модели", "ШЕЙДЕР", 2, 2, WordOrientation.Horizontal));
            lvl.words.Add(new WordEntry(2, "Базовая программная платформа (например Unity) для создания игр", "ДВИЖОК", 5, 2, WordOrientation.Vertical));
            lvl.words.Add(new WordEntry(3, "Символьные данные, отображаемые в элементах интерфейса", "ТЕКСТ", 3, 1, WordOrientation.Vertical));
            lvl.words.Add(new WordEntry(4, "Одиночное изображение в последовательности анимации", "КАДР", 5, 7, WordOrientation.Horizontal));
            lvl.words.Add(new WordEntry(5, "Процесс визуализации и построения 2D-картинки по 3D-сцене", "РЕНДЕР", 8, 7, WordOrientation.Vertical));
            lvl.words.Add(new WordEntry(6, "Цифровой 3D-объект, состоящий из вершин и полигонов", "МОДЕЛЬ", 6, 10, WordOrientation.Horizontal));
            lvl.words.Add(new WordEntry(7, "Геометрическая линия для расчёта попаданий курсора (Raycast)", "ЛУЧ", 10, 10, WordOrientation.Vertical));

            return lvl;
        }
    }
}
