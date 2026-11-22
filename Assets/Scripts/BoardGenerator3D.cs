using System.Collections.Generic;
using UnityEngine;

namespace Crossword3D
{
    public class BoardGenerator3D : MonoBehaviour
    {
        public static BoardGenerator3D Instance { get; private set; }

        [Header("Cell Settings")]
        public float cellSize = 1.0f;
        public float cellSpacing = 0.08f;
        public float cellHeight = 0.22f;

        [Header("Materials / Shaders")]
        public Material deskMaterial;
        public Material matPadMaterial;
        public Material tileMaterial;
        public Material goldAccentMaterial;

        [Header("Hierarchy Containers")]
        public Transform boardRoot;
        public Transform deskRoot;
        public Transform propsRoot;

        private Dictionary<Vector2Int, Cell3D> spawnedCells = new Dictionary<Vector2Int, Cell3D>();

        private void Awake()
        {
            Instance = this;
        }

        public void GenerateBoard(CrosswordLevel level, out Dictionary<Vector2Int, Cell3D> cellMap)
        {
            ClearPreviousBoard();

            if (boardRoot == null)
            {
                GameObject rootObj = new GameObject("BoardRoot");
                rootObj.transform.SetParent(transform);
                boardRoot = rootObj.transform;
            }

            spawnedCells = new Dictionary<Vector2Int, Cell3D>();
            cellMap = spawnedCells;

            // 1. Gather all active cell coordinates and starting clue numbers
            Dictionary<Vector2Int, char> charMap = new Dictionary<Vector2Int, char>();
            Dictionary<Vector2Int, int> clueNumbers = new Dictionary<Vector2Int, int>();

            foreach (var word in level.words)
            {
                Vector2Int startCoord = word.GetCellCoordinates(0);
                if (!clueNumbers.ContainsKey(startCoord))
                {
                    clueNumbers[startCoord] = word.id;
                }

                for (int i = 0; i < word.answer.Length; i++)
                {
                    Vector2Int coord = word.GetCellCoordinates(i);
                    charMap[coord] = word.answer[i];
                }
            }

            // Calculate grid bounds
            float totalW = level.gridWidth * (cellSize + cellSpacing);
            float totalH = level.gridHeight * (cellSize + cellSpacing);
            Vector3 originOffset = new Vector3(-totalW * 0.5f + (cellSize * 0.5f), 0, -totalH * 0.5f + (cellSize * 0.5f));

            // 2. Generate 3D Desk & Felt Pad
            GenerateDeskAndEnvironment(totalW, totalH);

            // 3. Generate 3D Interactive Tiles
            foreach (var kvp in charMap)
            {
                Vector2Int pos = kvp.Key;
                char letter = kvp.Value;
                int clueNum = clueNumbers.ContainsKey(pos) ? clueNumbers[pos] : -1;

                Vector3 localPos = originOffset + new Vector3(
                    pos.x * (cellSize + cellSpacing),
                    cellHeight * 0.5f + 0.05f,
                    (level.gridHeight - 1 - pos.y) * (cellSize + cellSpacing)
                );

                GameObject cellObj = CreateCellObject(pos.x, pos.y, letter, clueNum, localPos);
                Cell3D cellComp = cellObj.GetComponent<Cell3D>();
                spawnedCells[pos] = cellComp;
            }

            // 4. Calculate exact center and bounds of all crossword cells
            Bounds cellBounds = new Bounds();
            bool hasFirst = false;
            foreach (var kvp in spawnedCells)
            {
                if (!hasFirst)
                {
                    cellBounds = new Bounds(kvp.Value.transform.position, Vector3.one);
                    hasFirst = true;
                }
                else
                {
                    cellBounds.Encapsulate(kvp.Value.transform.position);
                }
            }

            Vector3 boardCenter = hasFirst ? cellBounds.center : Vector3.up * 0.5f;
            // Shift target slightly left so board is centered between the left Paper Sheet and the screen edge
            Vector3 cameraTargetPos = boardCenter + new Vector3(-0.9f, 0, 0);

            float maxDim = Mathf.Max(cellBounds.size.x, cellBounds.size.z);
            float fitDistance = Mathf.Clamp(maxDim * 1.45f + 4.0f, 18.0f, 28.0f);

            if (CameraOrbitController.Instance != null)
            {
                GameObject targetObj = new GameObject("CameraTarget");
                targetObj.transform.position = cameraTargetPos;
                CameraOrbitController.Instance.SetTarget(targetObj.transform);
                CameraOrbitController.Instance.defaultTargetPos = cameraTargetPos;
                CameraOrbitController.Instance.defaultDistance = fitDistance;
                CameraOrbitController.Instance.ResetViewPerspective();
            }
        }

        private GameObject CreateCellObject(int x, int y, char letter, int clueNum, Vector3 localPosition)
        {
            GameObject cellObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cellObj.name = $"Cell_{x}_{y}";
            cellObj.transform.SetParent(boardRoot);
            cellObj.transform.localPosition = localPosition;
            cellObj.transform.localScale = new Vector3(cellSize, cellHeight, cellSize);

            MeshRenderer mr = cellObj.GetComponent<MeshRenderer>();
            mr.material = tileMaterial != null ? tileMaterial : CreateDefaultMaterial(new Color(0.96f, 0.95f, 0.92f));

            // 3D Letter TextMesh
            GameObject letterObj = new GameObject("LetterText");
            letterObj.transform.SetParent(cellObj.transform);
            letterObj.transform.localPosition = new Vector3(0, 0.55f, 0);
            letterObj.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            letterObj.transform.localScale = Vector3.one * 0.16f;

            TextMesh letterTm = letterObj.AddComponent<TextMesh>();
            letterTm.alignment = TextAlignment.Center;
            letterTm.anchor = TextAnchor.MiddleCenter;
            letterTm.characterSize = 1.0f;
            letterTm.fontSize = 44;
            letterTm.fontStyle = FontStyle.Bold;
            letterTm.color = new Color(0.15f, 0.15f, 0.18f);

            // 3D Number Clue TextMesh
            GameObject numObj = new GameObject("NumberText");
            numObj.transform.SetParent(cellObj.transform);
            numObj.transform.localPosition = new Vector3(-0.35f, 0.55f, 0.35f);
            numObj.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            numObj.transform.localScale = Vector3.one * 0.08f;

            TextMesh numTm = numObj.AddComponent<TextMesh>();
            numTm.alignment = TextAlignment.Left;
            numTm.anchor = TextAnchor.UpperLeft;
            numTm.characterSize = 1.0f;
            numTm.fontSize = 28;
            numTm.fontStyle = FontStyle.Bold;
            numTm.color = new Color(0.4f, 0.35f, 0.3f);

            Cell3D cellComp = cellObj.AddComponent<Cell3D>();
            cellComp.tileRenderer = mr;
            cellComp.letterTextMesh = letterTm;
            cellComp.numberTextMesh = numTm;
            cellComp.Initialize(x, y, letter, clueNum);

            return cellObj;
        }

        private void GenerateDeskAndEnvironment(float boardW, float boardH)
        {
            if (deskRoot != null) Destroy(deskRoot.gameObject);
            deskRoot = new GameObject("DeskRoot").transform;
            deskRoot.SetParent(transform);

            // 1. Wooden Desk Table Top
            float tableW = Mathf.Max(boardW + 8f, 22f);
            float tableD = Mathf.Max(boardH + 8f, 18f);
            GameObject deskTable = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deskTable.name = "DeskTable";
            deskTable.transform.SetParent(deskRoot);
            deskTable.transform.localPosition = new Vector3(0, -0.4f, 0);
            deskTable.transform.localScale = new Vector3(tableW, 0.8f, tableD);
            deskTable.GetComponent<MeshRenderer>().material = deskMaterial != null ? deskMaterial : CreateDefaultMaterial(new Color(0.25f, 0.16f, 0.10f)); // Rich dark walnut wood

            // 2. Leather/Felt Mat under crossword
            GameObject feltPad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            feltPad.name = "FeltPad";
            feltPad.transform.SetParent(deskRoot);
            feltPad.transform.localPosition = new Vector3(0, 0.02f, 0);
            feltPad.transform.localScale = new Vector3(boardW + 1.2f, 0.05f, boardH + 1.2f);
            feltPad.GetComponent<MeshRenderer>().material = matPadMaterial != null ? matPadMaterial : CreateDefaultMaterial(new Color(0.18f, 0.28f, 0.22f)); // Classic dark green felt

            // 3. Interactive Props on Desk
            GenerateInteractiveProps(boardW, boardH);
        }

        private void GenerateInteractiveProps(float boardW, float boardH)
        {
            if (propsRoot != null) Destroy(propsRoot.gameObject);
            propsRoot = new GameObject("PropsRoot").transform;
            propsRoot.SetParent(deskRoot);

            float rightSideX = boardW * 0.5f + 2.5f;
            float leftSideX = -boardW * 0.5f - 2.5f;

            // 1. Interactive Desk Lamp
            CreateInteractiveLamp(new Vector3(leftSideX, 0f, boardH * 0.35f));

            // 2. Interactive Magnifying Glass (Hint Tool)
            CreateInteractiveMagnifyingGlass(new Vector3(rightSideX, 0.05f, -boardH * 0.2f));

            // 3. Interactive Vintage Radio / Gramophone
            CreateInteractiveRadio(new Vector3(leftSideX, 0f, -boardH * 0.35f));

            // 4. Notebook / Quill Pen
            CreateNotebookProp(new Vector3(rightSideX, 0.02f, boardH * 0.25f));
        }

        private void CreateInteractiveLamp(Vector3 position)
        {
            GameObject lampParent = new GameObject("DeskLamp");
            lampParent.transform.SetParent(propsRoot);
            lampParent.transform.position = position;

            // Lamp base
            GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.transform.SetParent(lampParent.transform);
            baseObj.transform.localPosition = new Vector3(0, 0.1f, 0);
            baseObj.transform.localScale = new Vector3(1.2f, 0.1f, 1.2f);
            baseObj.GetComponent<MeshRenderer>().material = CreateDefaultMaterial(new Color(0.75f, 0.6f, 0.2f)); // Brass

            // Lamp pole
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.transform.SetParent(lampParent.transform);
            pole.transform.localPosition = new Vector3(0, 1.4f, 0);
            pole.transform.localScale = new Vector3(0.12f, 1.2f, 0.12f);
            pole.GetComponent<MeshRenderer>().material = CreateDefaultMaterial(new Color(0.6f, 0.48f, 0.15f));

            // Lamp hood (shade)
            GameObject shade = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shade.transform.SetParent(lampParent.transform);
            shade.transform.localPosition = new Vector3(0.5f, 2.4f, 0);
            shade.transform.localRotation = Quaternion.Euler(30f, 0, -45f);
            shade.transform.localScale = new Vector3(1.1f, 0.6f, 0.9f);
            MeshRenderer shadeMr = shade.GetComponent<MeshRenderer>();
            shadeMr.material = CreateDefaultMaterial(new Color(0.12f, 0.35f, 0.22f)); // Banker's emerald green lamp shade

            // Light source
            GameObject lightObj = new GameObject("LampLightSource");
            lightObj.transform.SetParent(shade.transform);
            lightObj.transform.localPosition = new Vector3(0, -0.4f, 0);
            Light pLight = lightObj.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.range = 14.0f;
            pLight.intensity = 1.6f;
            pLight.color = new Color(1.0f, 0.92f, 0.75f);
            pLight.shadows = LightShadows.Soft;

            InteractiveDeskObject lampScript = lampParent.AddComponent<InteractiveDeskObject>();
            lampScript.objectType = DeskObjectType.DeskLamp;
            lampScript.lampLight = pLight;
            lampScript.lampBulbRenderer = shadeMr;

            // Make sure the collider on the base triggers interactions
            BoxCollider col = lampParent.AddComponent<BoxCollider>();
            col.center = new Vector3(0.2f, 1.2f, 0);
            col.size = new Vector3(1.8f, 2.5f, 1.8f);
        }

        private void CreateInteractiveMagnifyingGlass(Vector3 position)
        {
            GameObject glassParent = new GameObject("MagnifyingGlass");
            glassParent.transform.SetParent(propsRoot);
            glassParent.transform.position = position;
            glassParent.transform.localRotation = Quaternion.Euler(0, -35f, 0);

            // Ring
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.transform.SetParent(glassParent.transform);
            ring.transform.localPosition = new Vector3(0, 0.08f, 0.8f);
            ring.transform.localScale = new Vector3(1.0f, 0.06f, 1.0f);
            ring.GetComponent<MeshRenderer>().material = CreateDefaultMaterial(new Color(0.85f, 0.70f, 0.25f)); // Brass

            // Lens (glass)
            GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lens.transform.SetParent(glassParent.transform);
            lens.transform.localPosition = new Vector3(0, 0.08f, 0.8f);
            lens.transform.localScale = new Vector3(0.82f, 0.04f, 0.82f);
            lens.GetComponent<MeshRenderer>().material = CreateDefaultMaterial(new Color(0.7f, 0.85f, 1.0f));

            // Handle
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.transform.SetParent(glassParent.transform);
            handle.transform.localPosition = new Vector3(0, 0.08f, -0.1f);
            handle.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            handle.transform.localScale = new Vector3(0.16f, 0.7f, 0.16f);
            handle.GetComponent<MeshRenderer>().material = CreateDefaultMaterial(new Color(0.28f, 0.15f, 0.08f)); // Rosewood handle

            BoxCollider col = glassParent.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.1f, 0.35f);
            col.size = new Vector3(1.2f, 0.3f, 1.8f);

            InteractiveDeskObject glassScript = glassParent.AddComponent<InteractiveDeskObject>();
            glassScript.objectType = DeskObjectType.MagnifyingGlass;
        }

        private void CreateInteractiveRadio(Vector3 position)
        {
            GameObject radioParent = new GameObject("VintageRadio");
            radioParent.transform.SetParent(propsRoot);
            radioParent.transform.position = position;
            radioParent.transform.localRotation = Quaternion.Euler(0, 25f, 0);

            // Main Radio Box
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(radioParent.transform);
            body.transform.localPosition = new Vector3(0, 0.6f, 0);
            body.transform.localScale = new Vector3(1.8f, 1.2f, 0.9f);
            body.GetComponent<MeshRenderer>().material = CreateDefaultMaterial(new Color(0.35f, 0.22f, 0.12f));

            // Speaker grille
            GameObject grille = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grille.transform.SetParent(radioParent.transform);
            grille.transform.localPosition = new Vector3(0.25f, 0.6f, 0.46f);
            grille.transform.localScale = new Vector3(0.9f, 0.8f, 0.02f);
            grille.GetComponent<MeshRenderer>().material = CreateDefaultMaterial(new Color(0.65f, 0.58f, 0.48f));

            // Knobs
            GameObject knob1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            knob1.transform.SetParent(radioParent.transform);
            knob1.transform.localPosition = new Vector3(-0.45f, 0.75f, 0.48f);
            knob1.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            knob1.transform.localScale = new Vector3(0.25f, 0.08f, 0.25f);
            knob1.GetComponent<MeshRenderer>().material = CreateDefaultMaterial(new Color(0.85f, 0.75f, 0.3f));

            BoxCollider col = radioParent.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.6f, 0);
            col.size = new Vector3(1.8f, 1.2f, 0.9f);

            InteractiveDeskObject radioScript = radioParent.AddComponent<InteractiveDeskObject>();
            radioScript.objectType = DeskObjectType.VintageRadio;
        }

        private void CreateNotebookProp(Vector3 position)
        {
            GameObject bookObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bookObj.name = "Notebook";
            bookObj.transform.SetParent(propsRoot);
            bookObj.transform.position = position;
            bookObj.transform.localRotation = Quaternion.Euler(0, -15f, 0);
            bookObj.transform.localScale = new Vector3(1.4f, 0.15f, 1.9f);
            bookObj.GetComponent<MeshRenderer>().material = CreateDefaultMaterial(new Color(0.48f, 0.20f, 0.18f)); // Burgundy leather

            InteractiveDeskObject noteScript = bookObj.AddComponent<InteractiveDeskObject>();
            noteScript.objectType = DeskObjectType.Notebook;
        }

        private Material CreateDefaultMaterial(Color col)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = col;
            mat.SetFloat("_Glossiness", 0.3f);
            return mat;
        }

        public void ClearPreviousBoard()
        {
            if (boardRoot != null)
            {
                for (int i = boardRoot.childCount - 1; i >= 0; i--)
                {
                    Destroy(boardRoot.GetChild(i).gameObject);
                }
            }
        }
    }
}
