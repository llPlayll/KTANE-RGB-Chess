using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class RGBChess : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMColorblindMode Colorblind;

    public KMSelectable ColorSwitcher;
    public MeshRenderer ColorSwitcherRenderer;
    public TextMesh ColorSwitcherColorblindText;
    public TextMesh PiecesRemaining;
    public List<KMSelectable> PieceButtons;
    public List<MeshRenderer> PieceButtonRenderers;
    public List<MeshRenderer> PieceTextures;
    public List<Material> PieceMaterials;
    public Material DefaultPieceMaterial;
    public List<MeshRenderer> GridButtonRenderers;
    public List<KMSelectable> GridButtons;
    public List<TextMesh> GridColorblindTexts;
    public List<GameObject> GridPieces;
    public List<MeshRenderer> GridPieceRenderers;
    public List<TextMesh> GridPieceColorblindTexts;
    public MeshRenderer MOPRenderer;
    public List<MeshRenderer> MOCRenderers;
    public TextMesh IntersectionCount;
    public GameObject InfoDisplay;
    public MeshRenderer SolveRenderer;

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;
    bool colorblindActive;

    string randomPosition = "";
    string randomColor = "";
    string selectedPiece = "";
    string logGeneration = "[RGB Chess #{0}] The generated solution is -";
    string logSubmission = "";
    string mostOccurringPiece = "";
    List<string> randomPositions = new List<string> { };
    List<string> randomColors = new List<string> { };
    List<string> randomPieces = new List<string> { };
    List<string> submissionPositions = new List<string> { };
    List<string> submissionColors = new List<string> { };
    List<string> submissionPieces = new List<string> { };
    List<string> visitedPositions = new List<string> { };
    List<string> intersectionPositions = new List<string> { };
    List<string> mostOccurringColors = new List<string> { };
    int currentColorIndex = 7;
    int setColorIndex;
    int setRow;
    int setColumn;
    int placedPieces;
    int genPieceAmount;
    int intersections;
    bool solveFlag;
    bool isAnimating;
    bool isPlacingTPPiece = false;
    bool softStruck;

    string pieceShortNames = "KQRBN";
    List<string> pieceNames = new List<string> { "King", "Queen", "Rook", "Bishop", "Knight" };
    List<Color> colors = new List<Color> { new Color32(50, 50, 50, 255), Color.red, Color.green, Color.blue, Color.cyan, Color.magenta, Color.yellow, Color.white };
    List<string> binaryColors = new List<string> { "000", "100", "010", "001", "011", "101", "110", "111" };
    List<string> shortColorNames = new List<string> { "K", "R", "G", "B", "C", "M", "Y", "W" };
    List<string> colorNames = new List<string> {"Black", "Red", "Green", "Blue", "Cyan", "Magenta", "Yellow", "White"};
    List<string> RedValues = new List<string>
    {
        "000000",
        "000000",
        "000000",
        "000000",
        "000000",
        "000000"
    };
    List<string> GreenValues = new List<string>
    {
        "000000",
        "000000",
        "000000",
        "000000",
        "000000",
        "000000"
    };
    List<string> BlueValues = new List<string>
    {
        "000000",
        "000000",
        "000000",
        "000000",
        "000000",
        "000000"
    };
    List<string> SubmissionRedValues = new List<string> { };
    List<string> SubmissionGreenValues = new List<string> { };
    List<string> SubmissionBlueValues = new List<string> { };

    private RGBChessSettings Settings = new RGBChessSettings();

    void Awake()
    {
        ModuleId = ModuleIdCounter++;
        ModConfig<RGBChessSettings> modConfig = new ModConfig<RGBChessSettings>("RGBChessSettings");
        Settings = modConfig.Settings;
        modConfig.Settings = Settings;
        if (Settings.generationPieceAmount < 1)
        {
            genPieceAmount = 1;
        }
        else if (Settings.generationPieceAmount > 36)
        {
            genPieceAmount = 36;
        }
        else
        {
            genPieceAmount = Settings.generationPieceAmount;
        }
        Debug.LogFormat("[RGB Chess #{0}] The module will generate {1} piece{2}.", ModuleId, genPieceAmount, genPieceAmount > 1 ? "s" : "");
        PiecesRemaining.text = genPieceAmount.ToString();
        ColorSwitcher.OnInteract += delegate () { ColorSwitch(); return false; };
        foreach (KMSelectable piece in PieceButtons)
        {
            piece.OnInteract += delegate () { PiecePressed(piece); return false; };
        }
        foreach (KMSelectable cell in GridButtons)
        {
            cell.OnInteract += delegate () { GridButtonPressed(cell); return false; };
        }
        colorblindActive = Colorblind.ColorblindModeActive;
    }

   void ColorSwitch()
   {
        if (ModuleSolved || isAnimating)
        {
            return;
        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, ColorSwitcher.transform);

        currentColorIndex = (currentColorIndex + 1) % 8;

        for (int i = 0; i < 5; i++)
        {
            PieceTextures[i].material.color = colors[currentColorIndex];
        }
        ColorSwitcherRenderer.material.color = colors[currentColorIndex];
        ColorSwitcherColorblindText.text = shortColorNames[currentColorIndex];

        //Debug.LogFormat("[RGB Chess #{0}] The color switcher was pressed, switching colors, current color is {1}.", ModuleId, colorNames[currentColorIndex]);

        if (selectedPiece != "")
        {
            selectedPiece = shortColorNames[currentColorIndex] + selectedPiece[1];
            //Debug.LogFormat("[RGB Chess #{0}] Currently selected piece is {1} {2}.", ModuleId, colorNames[currentColorIndex], pieceNames[pieces.IndexOf(selectedPiece[1].ToString())]);
        }   
    }
    
    void PiecePressed(KMSelectable piece)
    {
        if (ModuleSolved || isAnimating)
        {
            return;
        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, piece.transform);
        for (int i = 0; i < PieceButtons.Count; i++)
        {
            if (piece == PieceButtons[i])
            {
                selectedPiece = shortColorNames[currentColorIndex] + pieceShortNames[i];
                PieceButtonRenderers[i].material.color = colors[0];
                //Debug.LogFormat("[RGB Chess #{0}] The {1} piece was pressed, selecting the {1}.", ModuleId, pieceNames[i]);
                //Debug.LogFormat("[RGB Chess #{0}] Currently selected piece is a {1} {2}.", ModuleId, colorNames[currentColorIndex], pieceNames[i]);
            }
            else
            {
                PieceButtonRenderers[i].material.color = Color.white;
            }
        }
    }

    void GridButtonPressed(KMSelectable cell)
    {
        if (ModuleSolved || isAnimating)
        {
            return;
        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, cell.transform);
        cell.AddInteractionPunch();
        for (int i = 0; i < GridButtons.Count; i++)
        {
            if (cell == GridButtons[i])
            {
                if (selectedPiece != "")
                {
                    if (!GridPieces[i].activeSelf)
                    {
                        if (placedPieces != genPieceAmount)
                        {
                            //Debug.LogFormat("[RGB Chess #{0}] The {1} cell was pressed, and there isn't already a piece on it, placing the currently selected piece, which is a {2} {3}.", ModuleId, "ABCDEF"[i % 6].ToString() + (i / 6 + 1).ToString(), colorNames[shortColorNames.IndexOf(selectedPiece[0].ToString())], pieceNames[pieces.IndexOf(selectedPiece[1].ToString())]);

                            GridPieces[i].SetActive(true);
                            GridPieceRenderers[i].material = PieceMaterials[pieceShortNames.IndexOf(selectedPiece[1].ToString())];
                            GridPieceRenderers[i].material.color = colors[shortColorNames.IndexOf(selectedPiece[0].ToString())];
                            if (colorblindActive)
                            {
                                GridColorblindTexts[i].text = "";
                                GridPieceColorblindTexts[i].text = selectedPiece[0].ToString() + selectedPiece[1].ToString();
                            }
                            else
                            {
                                GridPieceColorblindTexts[i].text = "";
                            }
                            placedPieces++;
                            PiecesRemaining.text = (genPieceAmount - placedPieces).ToString();
                            submissionPositions.Add(((int)(i / 6)).ToString() + (i % 6).ToString());
                            submissionColors.Add(binaryColors[currentColorIndex]);
                            submissionPieces.Add(selectedPiece[1].ToString());
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        //Debug.LogFormat("[RGB Chess #{0}] The {1} cell was pressed, but there is already a piece on it, removing the piece placed on {1}.", ModuleId, "ABCDEF"[i % 6].ToString() + (i / 6 + 1).ToString());
                        GridPieceRenderers[i].material = DefaultPieceMaterial;
                        GridPieces[i].SetActive(false);
                        if (colorblindActive)
                        {
                            setRow = i / 6;
                            setColumn = i % 6;
                            setColorIndex = binaryColors.IndexOf(RedValues[setRow][setColumn].ToString() + GreenValues[setRow][setColumn].ToString() + BlueValues[setRow][setColumn].ToString());
                            GridButtonRenderers[i].material.color = colors[setColorIndex];
                            GridColorblindTexts[i].text = shortColorNames[setColorIndex];
                        }
                        placedPieces--;
                        PiecesRemaining.text = (genPieceAmount - placedPieces).ToString();
                        submissionPieces.RemoveAt(submissionPositions.IndexOf(((int)(i / 6)).ToString() + (i % 6).ToString()));
                        submissionColors.RemoveAt(submissionPositions.IndexOf(((int)(i / 6)).ToString() + (i % 6).ToString()));
                        submissionPositions.Remove(((int)(i / 6)).ToString() + (i % 6).ToString());
                    }
                }
                else
                {
                    //Debug.LogFormat("[RGB Chess #{0}] The {1} cell was pressed, but a piece is not selected, doing nothing.", ModuleId, "ABCDEF"[i % 6].ToString() + (i / 6 + 1).ToString());
                }
                if (placedPieces == genPieceAmount)
                {
                    Debug.LogFormat("[RGB Chess #{0}] {1}{2} piece{3} {4} placed, checking submission.", ModuleId, genPieceAmount > 1 ? "All " : "", genPieceAmount.ToString(), genPieceAmount > 1 ? "s" : "", genPieceAmount > 1 ? "were" : "was");
                    SubmissionCheck();
                }
            }
        }
    }

    void Start()
    {
        GenerateBoard();
        ColorSwitcherColorblindText.gameObject.SetActive(colorblindActive);
        for (int c = 0; c < 36; c++)
        {
            GridColorblindTexts[c].gameObject.SetActive(colorblindActive);
        }
        if (Settings.noHints)
        {
            SolveRenderer.material.color = Color.red;
        }
    }

    void GenerateBoard()
    {
        for (int i = 0; i < genPieceAmount; i++)
        {
            randomPosition = Rnd.Range(0, 6).ToString() + Rnd.Range(0, 6).ToString();
            while (randomPositions.Contains(randomPosition))
            {
                randomPosition = Rnd.Range(0, 6).ToString() + Rnd.Range(0, 6).ToString();
            }
            randomPositions.Add(randomPosition);

            randomColor = Rnd.Range(0, 2).ToString() + Rnd.Range(0, 2).ToString() + Rnd.Range(0, 2).ToString();
            while (randomColor == "000")
            {
                randomColor = Rnd.Range(0, 2).ToString() + Rnd.Range(0, 2).ToString() + Rnd.Range(0, 2).ToString();
            }
            randomColors.Add(randomColor);

            randomPieces.Add(pieceShortNames[Rnd.Range(0, 5)].ToString());
        }

        LogFinal(logGeneration, randomPieces, randomColors, randomPositions);

        mostOccurringPiece = randomPieces.GroupBy(p => p).OrderBy(p => p.Count()).Select(p => p.First()).Last();
        List<string> temp = randomColors.ConvertAll(x => x);
        string moc1 = temp.GroupBy(p => p).OrderBy(p => p.Count()).Select(p => p.First()).Last();
        mostOccurringColors.Add(moc1);
        temp.RemoveAll(c => c == moc1);
        if (temp.Count == 0) mostOccurringColors.Add("000");
        else mostOccurringColors.Add(temp.GroupBy(p => p).OrderBy(p => p.Count()).Select(p => p.First()).Last());

        CalculateBoardColors(RedValues, GreenValues, BlueValues, randomPositions, randomColors, randomPieces, false, true);
        SetBoardColors();
        if (!Settings.noHints) SetInfoDisplay();
    }

    string LogCoordinates(int index, List<string> positions)
    {
        return "ABCDEF"[Int32.Parse(positions[index][1].ToString())].ToString() + (Int32.Parse(positions[index][0].ToString()) + 1).ToString();
    }

    string LogColors(int index, List<string> colors)
    {
        return colorNames[binaryColors.IndexOf(colors[index])];
    }

    string LogPieces(int index, List<string> piecesList)
    {
        return pieceNames[pieceShortNames.IndexOf(piecesList[index])];
    }

    void CalculateBoardColors(List<string> redGrid, List<string> greenGrid, List<string> blueGrid, List<string> positions, List<string> colors, List<string> pieces, bool submission, bool log)
    {
        for (int i = 0; i < positions.Count; i++)
        {
            List<string> addingCells = new List<string>() { };
            int row = Int32.Parse(positions[i][0].ToString());
            int column = Int32.Parse(positions[i][1].ToString());
            addingCells.Add(row.ToString() + column.ToString());
            //AddColorToCell(row, column, colors[i], redGrid, greenGrid, blueGrid);
            switch (pieces[i])
            {
                case "K":
                    if (row + 1 < 6)
                    {
                        //AddColorToCell(row + 1, column, colors[i], redGrid, greenGrid, blueGrid);
                        addingCells.Add((row + 1).ToString() + column.ToString());
                        if (column + 1 < 6)
                        {
                            //AddColorToCell(row + 1, column + 1, colors[i], redGrid, greenGrid, blueGrid);
                            addingCells.Add((row + 1).ToString() + (column + 1).ToString());
                        }
                        if (column - 1 >= 0)
                        {
                            //AddColorToCell(row + 1, column - 1, colors[i], redGrid, greenGrid, blueGrid);
                            addingCells.Add((row + 1).ToString() + (column - 1).ToString());
                        }
                    }
                    if (row - 1 >= 0)
                    {
                        //AddColorToCell(row - 1, column, colors[i], redGrid, greenGrid, blueGrid);
                        addingCells.Add((row - 1).ToString() + column.ToString());
                        if (column + 1 < 6)
                        {
                            //AddColorToCell(row - 1, column + 1, colors[i], redGrid, greenGrid, blueGrid);
                            addingCells.Add((row - 1).ToString() + (column + 1).ToString());
                        }
                        if (column - 1 >= 0)
                        {
                            //AddColorToCell(row - 1, column - 1, colors[i], redGrid, greenGrid, blueGrid);
                            addingCells.Add((row - 1).ToString() + (column - 1).ToString());
                        }
                    }
                    if (column + 1 < 6)
                    {
                        //AddColorToCell(row, column + 1, colors[i], redGrid, greenGrid, blueGrid);
                        addingCells.Add(row.ToString() + (column + 1).ToString());
                    }
                    if (column - 1 >= 0)
                    {
                        //AddColorToCell(row, column - 1, colors[i], redGrid, greenGrid, blueGrid);
                        addingCells.Add(row.ToString() + (column - 1).ToString());
                    }
                    break;
                case "R":
                    for (int r = 0; r < 6; r++)
                    {
                        //AddColorToCell(row, r, colors[i], redGrid, greenGrid, blueGrid);
                        addingCells.Add(row.ToString() + r.ToString());
                        //AddColorToCell(r, column, colors[i], redGrid, greenGrid, blueGrid);
                        addingCells.Add(r.ToString() + column.ToString());
                    }
                    break;
                case "B":
                    for (int b = 0; b < 6; b++)
                    {
                        if (row + b < 6)
                        {
                            if (column + b < 6)
                            {
                                //AddColorToCell(row + b, column + b, colors[i], redGrid, greenGrid, blueGrid);
                                addingCells.Add((row + b).ToString() + (column + b).ToString());
                            }
                            if (column - b >= 0)
                            {
                                //AddColorToCell(row + b, column - b, colors[i], redGrid, greenGrid, blueGrid);
                                addingCells.Add((row + b).ToString() + (column - b).ToString());
                            }
                        }
                        if (row - b >= 0)
                        {
                            if (column + b < 6)
                            {
                                //AddColorToCell(row - b, column + b, colors[i], redGrid, greenGrid, blueGrid);
                                addingCells.Add((row - b).ToString() + (column + b).ToString());
                            }
                            if (column - b >= 0)
                            {
                                //AddColorToCell(row - b, column - b, colors[i], redGrid, greenGrid, blueGrid);
                                addingCells.Add((row - b).ToString() + (column - b).ToString());
                            }
                        }
                    }
                    break;
                case "Q":
                    for (int q = 0; q < 6; q++)
                    {
                        //AddColorToCell(row, q, colors[i], redGrid, greenGrid, blueGrid);
                        addingCells.Add(row.ToString() + q.ToString());
                        //AddColorToCell(q, column, colors[i], redGrid, greenGrid, blueGrid);
                        addingCells.Add(q.ToString() + column.ToString());
                        if (row + q < 6)
                        {
                            if (column + q < 6)
                            {
                                //AddColorToCell(row + q, column + q, colors[i], redGrid, greenGrid, blueGrid);
                                addingCells.Add((row + q).ToString() + (column + q).ToString());
                            }
                            if (column - q >= 0)
                            {
                                //AddColorToCell(row + q, column - q, colors[i], redGrid, greenGrid, blueGrid);
                                addingCells.Add((row + q).ToString() + (column - q).ToString());
                            }
                        }
                        if (row - q >= 0)
                        {
                            if (column + q < 6)
                            {
                                //AddColorToCell(row - q, column + q, colors[i], redGrid, greenGrid, blueGrid);
                                addingCells.Add((row - q).ToString() + (column + q).ToString());
                            }
                            if (column - q >= 0)
                            {
                                //AddColorToCell(row - q, column - q, colors[i], redGrid, greenGrid, blueGrid);
                                addingCells.Add((row - q).ToString() + (column - q).ToString());
                            }
                        }
                    }
                    break;
                case "N":
                    if (row - 2 >= 0)
                    {
                        if (column - 1 >= 0)
                        {
                            //AddColorToCell(row - 2, column - 1, colors[i], redGrid, greenGrid, blueGrid);
                            addingCells.Add((row - 2).ToString() + (column - 1).ToString());
                        }
                        if (column + 1 < 6)
                        {
                            //AddColorToCell(row - 2, column + 1, colors[i], redGrid, greenGrid, blueGrid);
                            addingCells.Add((row - 2).ToString() + (column + 1).ToString());
                        }
                    }
                    if (row + 2 < 6)
                    {
                        if (column - 1 >= 0)
                        {
                            //AddColorToCell(row + 2, column - 1, colors[i], redGrid, greenGrid, blueGrid);
                            addingCells.Add((row + 2).ToString() + (column - 1).ToString());
                        }
                        if (column + 1 < 6)
                        {
                            //AddColorToCell(row + 2, column + 1, colors[i], redGrid, greenGrid, blueGrid);
                            addingCells.Add((row + 2).ToString() + (column + 1).ToString());
                        }
                    }
                    if (column - 2 >= 0)
                    {
                        if (row - 1 >= 0)
                        {
                            //AddColorToCell(row - 1, column - 2, colors[i], redGrid, greenGrid, blueGrid);
                            addingCells.Add((row - 1).ToString() + (column - 2).ToString());
                        }
                        if (row + 1 < 6)
                        {
                            //AddColorToCell(row + 1, column - 2, colors[i], redGrid, greenGrid, blueGrid);
                            addingCells.Add((row + 1).ToString() + (column - 2).ToString());
                        }
                    }
                    if (column + 2 < 6)
                    {
                        if (row - 1 >= 0)
                        {
                            //AddColorToCell(row - 1, column + 2, colors[i], redGrid, greenGrid, blueGrid);
                            addingCells.Add((row - 1).ToString() + (column + 2).ToString());
                        }
                        if (row + 1 < 6)
                        {
                            //AddColorToCell(row + 1, column + 2, colors[i], redGrid, greenGrid, blueGrid);
                            addingCells.Add((row + 1).ToString() + (column + 2).ToString());
                        }
                    }
                    break;
                default:
                    break;
            }

            List<string> uniqueAdds = new List<string>();
            for (int j = 0; j < addingCells.Count; j++)
            {
                if (uniqueAdds.Contains(addingCells[j])) continue;
                else
                {
                    AddColorToCell(addingCells[j][0] - 48, addingCells[j][1] - 48, colors[i], redGrid, greenGrid, blueGrid);
                    uniqueAdds.Add(addingCells[j]);
                }
            }

        }
        if (log)
        {
            if (!submission)
            {
                Debug.LogFormat("[RGB Chess #{0}] Generated board colors are:", ModuleId);
            }
            else
            {
                Debug.LogFormat("[RGB Chess #{0}] Generated board colors by the submission are:", ModuleId);
            }
            for (int i = 0; i < 6; i++)
            {
                string rowColorLog = "";
                for (int j = 0; j < 6; j++)
                {
                    rowColorLog += shortColorNames[binaryColors.IndexOf(redGrid[i][j].ToString() + greenGrid[i][j].ToString() + blueGrid[i][j].ToString())];
                }
                Debug.LogFormat("[RGB Chess #{0}] {1}", ModuleId, rowColorLog);
            }
            if (!submission)
            {
                intersections = intersectionPositions.Count;
                Debug.LogFormat("[RGB Chess #{0}] Additional solution information:", ModuleId);
                Debug.LogFormat("[RGB Chess #{0}] The amount of intersections - {1}.", ModuleId, intersections);
                Debug.LogFormat("[RGB Chess #{0}] The most occurring piece - {1}.", ModuleId, pieceNames[pieceShortNames.IndexOf(mostOccurringPiece)]);
                Debug.LogFormat("[RGB Chess #{0}] The two most occurring piece colors - {1} and {2}.", ModuleId, colorNames[binaryColors.IndexOf(mostOccurringColors[0])], colorNames[binaryColors.IndexOf(mostOccurringColors[1])]);
            }
        }
    }

    void AddColorToCell(int row, int column, string color, List<string> redGrid, List<string> greenGrid, List<string> blueGrid)
    {
        if (!visitedPositions.Contains(row.ToString() + column.ToString())) visitedPositions.Add(row.ToString() + column.ToString());
        else if (!intersectionPositions.Contains(row.ToString() + column.ToString())) intersectionPositions.Add(row.ToString() + column.ToString());
        redGrid[row] = redGrid[row].Substring(0, column) + ((redGrid[row][column] + color[0]) % 2).ToString() + redGrid[row].Substring(column + 1);
        greenGrid[row] = greenGrid[row].Substring(0, column) + ((greenGrid[row][column] + color[1]) % 2).ToString() + greenGrid[row].Substring(column + 1);
        blueGrid[row] = blueGrid[row].Substring(0, column) + ((blueGrid[row][column] + color[2]) % 2).ToString() + blueGrid[row].Substring(column + 1);
    }

    void SetBoardColors()
    {
        for (int i = 0; i < 36; i++)
        {
            setRow = i / 6;
            setColumn = i % 6;
            setColorIndex = binaryColors.IndexOf(RedValues[setRow][setColumn].ToString() + GreenValues[setRow][setColumn].ToString() + BlueValues[setRow][setColumn].ToString());
            GridButtonRenderers[i].material.color = colors[setColorIndex];
            GridColorblindTexts[i].text = shortColorNames[setColorIndex];
        }
    }

    void SetInfoDisplay()
    {
        MOPRenderer.material = PieceMaterials[pieceShortNames.IndexOf(mostOccurringPiece)];
        int randomMOCPiece = Rnd.Range(0, 5);
        string MOCIntersection = "";
        for (int i = 0; i < 3; i++)
        {
            MOCIntersection += ((mostOccurringColors[0][i] + mostOccurringColors[1][i]) % 2).ToString();
        }
        switch (randomMOCPiece)
        {
            case 0:
                MOCRenderers[0].material.color = colors[binaryColors.IndexOf(mostOccurringColors[0])];
                MOCRenderers[1].material.color = colors[binaryColors.IndexOf(mostOccurringColors[0])];
                MOCRenderers[6].material.color = colors[binaryColors.IndexOf(mostOccurringColors[1])];
                MOCRenderers[7].material.color = colors[binaryColors.IndexOf(mostOccurringColors[1])];
                MOCRenderers[3].material.color = colors[binaryColors.IndexOf(MOCIntersection)];
                MOCRenderers[4].material.color = colors[binaryColors.IndexOf(MOCIntersection)];
                break;
            case 1:
                MOCRenderers[1].material.color = colors[binaryColors.IndexOf(mostOccurringColors[0])];
                MOCRenderers[7].material.color = colors[binaryColors.IndexOf(mostOccurringColors[1])];
                MOCRenderers[0].material.color = colors[binaryColors.IndexOf(MOCIntersection)];
                MOCRenderers[2].material.color = colors[binaryColors.IndexOf(MOCIntersection)];
                MOCRenderers[3].material.color = colors[binaryColors.IndexOf(MOCIntersection)];
                MOCRenderers[4].material.color = colors[binaryColors.IndexOf(MOCIntersection)];
                MOCRenderers[6].material.color = colors[binaryColors.IndexOf(MOCIntersection)];
                MOCRenderers[8].material.color = colors[binaryColors.IndexOf(MOCIntersection)];
                break;
            case 2:
                MOCRenderers[1].material.color = colors[binaryColors.IndexOf(mostOccurringColors[0])];
                MOCRenderers[2].material.color = colors[binaryColors.IndexOf(mostOccurringColors[0])];
                MOCRenderers[7].material.color = colors[binaryColors.IndexOf(mostOccurringColors[1])];
                MOCRenderers[8].material.color = colors[binaryColors.IndexOf(mostOccurringColors[1])];
                MOCRenderers[0].material.color = colors[binaryColors.IndexOf(MOCIntersection)];
                MOCRenderers[3].material.color = colors[binaryColors.IndexOf(MOCIntersection)];
                MOCRenderers[6].material.color = colors[binaryColors.IndexOf(MOCIntersection)];
                break;
            case 3:
                MOCRenderers[0].material.color = colors[binaryColors.IndexOf(mostOccurringColors[0])];
                MOCRenderers[8].material.color = colors[binaryColors.IndexOf(mostOccurringColors[0])];
                MOCRenderers[2].material.color = colors[binaryColors.IndexOf(mostOccurringColors[1])];
                MOCRenderers[6].material.color = colors[binaryColors.IndexOf(mostOccurringColors[1])];
                MOCRenderers[4].material.color = colors[binaryColors.IndexOf(MOCIntersection)];
                break;
            case 4:
                MOCRenderers[0].material.color = colors[binaryColors.IndexOf(mostOccurringColors[0])];
                MOCRenderers[7].material.color = colors[binaryColors.IndexOf(mostOccurringColors[0])];
                MOCRenderers[1].material.color = colors[binaryColors.IndexOf(mostOccurringColors[1])];
                MOCRenderers[6].material.color = colors[binaryColors.IndexOf(mostOccurringColors[1])];
                MOCRenderers[5].material.color = colors[binaryColors.IndexOf(MOCIntersection)];
                break;
        }
        IntersectionCount.text = intersections.ToString();
    }

    void SubmissionCheck()
    {
        SubmissionRedValues = new List<string>
        {
        "000000",
        "000000",
        "000000",
        "000000",
        "000000",
        "000000"
        };
        SubmissionGreenValues = new List<string>
        {
        "000000",
        "000000",
        "000000",
        "000000",
        "000000",
        "000000"
        };
        SubmissionBlueValues = new List<string>
        {
        "000000",
        "000000",
        "000000",
        "000000",
        "000000",
        "000000"
        };
        solveFlag = true;
        logSubmission = "[RGB Chess #{0}] Submitted solution is -";
        LogFinal(logSubmission, submissionPieces, submissionColors, submissionPositions);
        CalculateBoardColors(SubmissionRedValues, SubmissionGreenValues, SubmissionBlueValues, submissionPositions, submissionColors, submissionPieces, true, true);

        for (int i = 0; i < 6; i++)
        {
            if (RedValues[i] != SubmissionRedValues[i] || GreenValues[i] != SubmissionGreenValues[i] || BlueValues[i] != SubmissionBlueValues[i])
            {
                solveFlag = false;
            }
        }
        if (solveFlag)
        {
            StartCoroutine(RGBChessSolve());
        }
        else
        {
            StartCoroutine(RGBChessStrike());
        }
    }

    IEnumerator RGBChessStrike()
    {
        StartCoroutine(ShowSubmission());
        yield return new WaitForSeconds(genPieceAmount);
        List<int> rememberPositions = new List<int>();
        List<Color> rememberColors = new List<Color>();
        List<string> rememberPieces = new List<string>();
        for (int i = 0; i < 36; i++)
        {
            GridButtonRenderers[i].material.color = Color.red;
            GridColorblindTexts[i].text = "X";
            if (GridPieceRenderers[i].material.ToString() != "Default-Material (Instance) (UnityEngine.Material)")
            {
                rememberPositions.Add(i);
                rememberColors.Add(GridPieceRenderers[i].material.color);
                rememberPieces.Add(GridPieceColorblindTexts[i].text);
                GridPieceRenderers[i].material.color = Color.red;
                GridPieceColorblindTexts[i].text = "";
                GridColorblindTexts[i].text = "";
            }
            yield return new WaitForSeconds(0.05f);
        }
        Debug.LogFormat("[RGB Chess #{0}] Submitted piece{1} did not generate the same board as the desired solution{2}", ModuleId, genPieceAmount > 1 ? "s" : "", softStruck ? ", giving a soft-strike." : ",  strike!");

        if (!Settings.noHints)
        {
            for (int c = 0; c < 36; c++)
            {
                string pos = ((int)(c / 6)).ToString() + (c % 6).ToString();
                if (submissionPositions.Contains(pos))
                {
                    if (!randomPositions.Contains(pos))
                    {
                        GridPieceRenderers[c].material.color = new Color32(50, 50, 50, 255);
                        if (colorblindActive) { GridPieceColorblindTexts[c].text = "K"; }
                    }
                    else
                    {
                        int idx1 = randomPositions.IndexOf(pos);
                        int idx2 = submissionPositions.IndexOf(pos);
                        if (randomColors[idx1] != submissionColors[idx2] || randomPieces[idx1] != submissionPieces[idx2])
                        {
                            GridPieceRenderers[c].material.color = Color.yellow;
                            if (colorblindActive) { GridPieceColorblindTexts[c].text = "Y"; }
                        }
                        else
                        {
                            GridPieceRenderers[c].material.color = Color.green;
                            if (colorblindActive) { GridPieceColorblindTexts[c].text = "G"; }
                        }
                    }
                }
            }
        }
        yield return new WaitForSeconds(3f);

        if (softStruck || Settings.noHints) GetComponent<KMBombModule>().HandleStrike();
        else softStruck = true;
        
        SetBoardColors();
        for (int i = 0; i < 36; i++)
        {
            if (rememberPositions.Contains(i))
            {
                GridPieceRenderers[i].material.color = rememberColors[rememberPositions.IndexOf(i)];
                GridPieceColorblindTexts[i].text = rememberPieces[rememberPositions.IndexOf(i)];
                GridColorblindTexts[i].text = "";
            }
        }
        isAnimating = false;
    }

    IEnumerator RGBChessSolve()
    {
        StartCoroutine(ShowSubmission());
        yield return new WaitForSeconds(genPieceAmount);

        for (int i = 0; i < 36; i++)
        {
            GridButtonRenderers[i].material.color = Color.green;
            GridColorblindTexts[i].text = "!";
            if (GridPieceRenderers[i].material.ToString() != "Default-Material (Instance) (UnityEngine.Material)")
            {
                GridPieceRenderers[i].material.color = Color.green;
                GridPieceColorblindTexts[i].text = "";
                GridColorblindTexts[i].text = "";
            }
            yield return new WaitForSeconds(0.05f);
        }
        Debug.LogFormat("[RGB Chess #{0}] Submitted piece{1} generated the same board as the desired solution, module solved!", ModuleId, genPieceAmount > 1 ? "s" : "");
        StartCoroutine(RotateInfoDisplay());
        GetComponent<KMBombModule>().HandlePass();
        ModuleSolved = true;
        isAnimating = false;
    }

    IEnumerator ShowSubmission()
    {
        isAnimating = true;
        for (int i = 0; i < 36; i++)
        {
            GridButtonRenderers[i].material.color = colors[0];
        }

        for (int i = 0; i < genPieceAmount; i++)
        {
            SubmissionRedValues = new List<string>
            {
            "000000",
            "000000",
            "000000",
            "000000",
            "000000",
            "000000"
            };
            SubmissionGreenValues = new List<string>
            {
            "000000",
            "000000",
            "000000",
            "000000",
            "000000",
            "000000"
            };
            SubmissionBlueValues = new List<string>
            {
            "000000",
            "000000",
            "000000",
            "000000",
            "000000",
            "000000"
            };
            CalculateBoardColors(SubmissionRedValues, SubmissionGreenValues, SubmissionBlueValues, submissionPositions.GetRange(0, i + 1), submissionColors.GetRange(0, i + 1), submissionPieces.GetRange(0, i + 1), true, false);
            SetSubmissionBoardColors();
            yield return new WaitForSeconds(1);
        }
    }

    IEnumerator RotateInfoDisplay()
    {
        for (int i = 0; i < 45; i++)
        {
            InfoDisplay.transform.Rotate(0, 0, 4);
            yield return new WaitForEndOfFrame();
        }
    }

    void SetSubmissionBoardColors()
    {
        for (int i = 0; i < 36; i++)
        {
            setRow = i / 6;
            setColumn = i % 6;
            setColorIndex = binaryColors.IndexOf(SubmissionRedValues[setRow][setColumn].ToString() + SubmissionGreenValues[setRow][setColumn].ToString() + SubmissionBlueValues[setRow][setColumn].ToString());
            GridButtonRenderers[i].material.color = colors[setColorIndex];
            if (GridPieceRenderers[i].material.ToString() == "Default-Material (Instance) (UnityEngine.Material)")
            {
                GridColorblindTexts[i].text = shortColorNames[setColorIndex];
            }
        }
    }

    void LogFinal(string log, List<string> pieceList, List<string> colorList, List<string> positions)
    {
        for (int i = 0; i < genPieceAmount; i++)
        {
            log += " " + LogColors(i, colorList) + " " + LogPieces(i, pieceList) + " at " + LogCoordinates(i, positions);
            if (i < genPieceAmount - 1)
            {
                log += ",";
            }
            else
            {
                log += ".";
            }
        }

        Debug.LogFormat(log, ModuleId);
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} gka6 b4 wnf1 To place a Green King at A6, remove the piece placed at B4, and then place a White Knight at F1. For pieces, use the first letter their names, but use N for Knight instead of K. Top left cell is A1. !{0} cb to activate colourblind mode.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand (string Command)
    {
        var commandArgs = Command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
        if (commandArgs[0] == "CB")
        {
            yield return null;
            colorblindActive = true;
            ColorSwitcherColorblindText.gameObject.SetActive(colorblindActive);
            for (int c = 0; c < 36; c++)
            {
                GridColorblindTexts[c].gameObject.SetActive(colorblindActive);
                string pis = ((int)(c / 6)).ToString() + (c % 6).ToString();
                if (submissionPositions.Contains(pis))
                {
                    GridPieceColorblindTexts[c].text = shortColorNames[binaryColors.IndexOf(submissionColors[submissionPositions.IndexOf(pis)])] + submissionPieces[submissionPositions.IndexOf(pis)];
                }
            }
        }
        else
        {
            for (int i = 0; i < commandArgs.Length; i++)
            {
                if (commandArgs[i].Length != 2 & commandArgs[i].Length != 4)
                {
                    yield return "sendtochaterror Invalid command!";
                    break;
                }
            }

            for (int i = 0; i < commandArgs.Length; i++)
            {
                if (commandArgs[i].Length == 4)
                {
                    if (!shortColorNames.Contains(commandArgs[i][0].ToString()) || !pieceShortNames.Contains(commandArgs[i][1].ToString()) || !"ABCDEF".Contains(commandArgs[i][2].ToString()) || !"123456".Contains(commandArgs[i][3].ToString()))
                    {
                        yield return "sendtochaterror Invalid command!";
                        break;
                    }
                    else
                    {
                        if (GridPieceRenderers[Int32.Parse(commandArgs[i][3].ToString()) * 6 + "ABCDEF".IndexOf(commandArgs[i][2]) - 6].material.ToString() != "Default-Material (Instance) (UnityEngine.Material)")
                        {
                            yield return "sendtochaterror There is already a piece at " + commandArgs[i][2].ToString() + commandArgs[i][3].ToString() + "!";
                            break;
                        }
                        else
                        {
                            isPlacingTPPiece = true;
                            yield return null;
                            while (currentColorIndex != shortColorNames.IndexOf(commandArgs[i][0].ToString()))
                            {
                                ColorSwitcher.OnInteract();
                                yield return new WaitForSeconds(0.1f);
                            }
                            PieceButtons[pieceShortNames.IndexOf(commandArgs[i][1].ToString())].OnInteract();
                            GridButtons[Int32.Parse(commandArgs[i][3].ToString()) * 6 + "ABCDEF".IndexOf(commandArgs[i].ToUpperInvariant()[2]) - 6].OnInteract();
                            yield return new WaitForSeconds(0.1f);
                            isPlacingTPPiece = false;
                        }
                    }
                }
                else
                {
                    if (!"ABCDEF".Contains(commandArgs[i][0].ToString()) || !"123456".Contains(commandArgs[i][1].ToString()))
                    {
                        yield return "sendtochaterror Invalid position!";
                        break;
                    }
                    if (GridPieceRenderers[Int32.Parse(commandArgs[i][1].ToString()) * 6 + "ABCDEF".IndexOf(commandArgs[i][0]) - 6].material.ToString() == "Default-Material (Instance) (UnityEngine.Material)")
                    {
                        yield return "sendtochaterror There isn't a piece at " + commandArgs[i][0].ToString() + commandArgs[i][1].ToString() + "!";
                        break;
                    }
                    else
                    {
                        yield return null;
                        GridButtons[Int32.Parse(commandArgs[i][1].ToString()) * 6 + "ABCDEF".IndexOf(commandArgs[i].ToUpperInvariant()[0]) - 6].OnInteract();
                        yield return new WaitForSeconds(0.1f);
                    }
                }
            }
            yield return null;
        } 
    }

    IEnumerator TwitchHandleForcedSolve ()
    {
        yield return null;
        for (int i = 0; i < 36; i++)
        {
            if (GridPieceRenderers[i].material.ToString() != "Default-Material (Instance) (UnityEngine.Material)")
            {
                StartCoroutine(ProcessTwitchCommand("ABCDEF"[i % 6].ToString() + ((int)(i / 6) + 1).ToString()));
            }
        }
        for (int i = 0; i < genPieceAmount; i++)
        {
            while (isPlacingTPPiece)
            {
                yield return null;
            }
            StartCoroutine(ProcessTwitchCommand(shortColorNames[binaryColors.IndexOf(randomColors[i])].ToString() + randomPieces[i] + "ABCDEF"[Int32.Parse(randomPositions[i][1].ToString())].ToString() + (Int32.Parse(randomPositions[i][0].ToString()) + 1).ToString()));
            yield return new WaitForSeconds(0.1f);
        }
    }

    class RGBChessSettings
    {
        public int generationPieceAmount = 4;
        public bool noHints = false;
    }

    static Dictionary<string, object>[] TweaksEditorSettings = new Dictionary<string, object>[]
    {
        new Dictionary<string, object>
        {
            { "Filename", "RGBChessSettings.json" },
            { "Name", "RGB Chess Settings" },
            { "Listing", new List<Dictionary<string, object>>{
                new Dictionary<string, object>
                {
                    { "Key", "generationPieceAmount" },
                    { "Text", "Changes the amount of colored pieces that will be generated at the start." },
                },
                new Dictionary<string, object>
                {
                    { "Key", "noHints" },
                    { "Text", "If this is enabled, the module will not display ANY hints and will only strike normally (no soft-strikes)." },
                }
            } }
        }
    };
}

