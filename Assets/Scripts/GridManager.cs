using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class GridManager : SuperClass
{
    public static GridManager instance = null;

    public GameObject hexPrefab;
    public GameObject hexParent;
    public GameObject outParent;
    public Sprite outlineSprite;
    public Sprite hexagonSprite;

    private int gridWidth;
    private int gridHeight;
    private int selectionStatus;
    private bool colorblindMode;
    private bool bombProduction;
    private bool gameEnd;
    private Vector2 selectedPosition;
    private Hexagon selectedHexagon;
    private List<List<Hexagon>> gameGrid;
    private List<Hexagon> selectedGroup;
    private List<Hexagon> bombs;
    private List<Color> colorList;

    private bool gameInitializiationStatus;
    private bool hexagonRotationStatus;
    private bool hexagonExplosionStatus;
    private bool hexagonProductionStatus;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        else
        {
            Destroy(this);
        }

    }

    void Start()
    {
        gameEnd = false;
        bombProduction = false;
        hexagonRotationStatus = false;
        hexagonExplosionStatus = false;
        hexagonProductionStatus = false;
        bombs = new List<Hexagon>();
        selectedGroup = new List<Hexagon>();
        gameGrid = new List<List<Hexagon>>();
    }

    public void InitializeGrid()
    {
        List<int> missingCells = new List<int>();

        for (int i = 0; i < GetGridWidth(); ++i)
        {
            for (int j = 0; j < GetGridHeight(); ++j)
                missingCells.Add(i);

            gameGrid.Add(new List<Hexagon>());
        }

        StartCoroutine(ProduceHexagons(missingCells, ColoredGridProducer()));
    }

    public void Select(Collider2D collider)
    {
        if (selectedHexagon == null || !selectedHexagon.GetComponent<Collider2D>().Equals(collider))
        {
            selectedHexagon = collider.gameObject.GetComponent<Hexagon>();
            selectedPosition.x = selectedHexagon.GetX();
            selectedPosition.y = selectedHexagon.GetY();
            selectionStatus = 0;
        }

        else
        {
            selectionStatus = (++selectionStatus) % SELECTION_STATUS_COUNT;
        }

        DestructOutline();
        ConstructOutline();
    }

    public void Rotate(bool clockWise)
    {
        DestructOutline();
        StartCoroutine(RotationCheckCoroutine(clockWise));
    }

    #region SelectHelpers
    private void FindHexagonGroup()
    {
        List<Hexagon> returnValue = new List<Hexagon>();
        Vector2 firstPos, secondPos;

        selectedHexagon = gameGrid[(int)selectedPosition.x][(int)selectedPosition.y];
        FindOtherHexagons(out firstPos, out secondPos);
        selectedGroup.Clear();
        selectedGroup.Add(selectedHexagon);
        selectedGroup.Add(gameGrid[(int)firstPos.x][(int)firstPos.y].GetComponent<Hexagon>());
        selectedGroup.Add(gameGrid[(int)secondPos.x][(int)secondPos.y].GetComponent<Hexagon>());
    }

    private void FindOtherHexagons(out Vector2 first, out Vector2 second)
    {
        Hexagon.NeighbourHexes neighbours = selectedHexagon.GetNeighbours();
        bool breakLoop = false;

        do
        {
            switch (selectionStatus)
            {
                case 0: first = neighbours.up; second = neighbours.upRight; break;
                case 1: first = neighbours.upRight; second = neighbours.downRight; break;
                case 2: first = neighbours.downRight; second = neighbours.down; break;
                case 3: first = neighbours.down; second = neighbours.downLeft; break;
                case 4: first = neighbours.downLeft; second = neighbours.upLeft; break;
                case 5: first = neighbours.upLeft; second = neighbours.up; break;
                default: first = Vector2.zero; second = Vector2.zero; break;
            }

            if (first.x < ZERO || first.x >= gridWidth || first.y < ZERO || first.y >= gridHeight || second.x < ZERO || second.x >= gridWidth || second.y < ZERO || second.y >= gridHeight)
            {
                selectionStatus = (++selectionStatus) % SELECTION_STATUS_COUNT;
            }
            else
            {
                breakLoop = true;
            }
        } while (!breakLoop);
    }
    #endregion

    #region RotateHelpers
    private IEnumerator RotationCheckCoroutine(bool clockWise)
    {
        List<Hexagon> explosiveHexagons = null;
        bool flag = true;

        hexagonRotationStatus = true;
        for (int i = 0; i < selectedGroup.Count; ++i)
        {
            SwapHexagons(clockWise);
            yield return new WaitForSeconds(0.3f);

            explosiveHexagons = CheckExplosion(gameGrid);
            if (explosiveHexagons.Count > ZERO)
            {
                break;
            }
        }

        hexagonExplosionStatus = true;
        hexagonRotationStatus = false;

        while (explosiveHexagons.Count > ZERO)
        {
            if (flag)
            {
                hexagonProductionStatus = true;
                StartCoroutine(ProduceHexagons(ExplodeHexagons(explosiveHexagons)));
                flag = false;
            }

            else if (!hexagonProductionStatus)
            {
                explosiveHexagons = CheckExplosion(gameGrid);
                flag = true;
            }

            yield return new WaitForSeconds(0.3f);
        }

        hexagonExplosionStatus = false;
        FindHexagonGroup();
        ConstructOutline();
    }

    private void SwapHexagons(bool clockWise)
    {
        int x1, x2, x3, y1, y2, y3;
        Vector2 pos1, pos2, pos3;
        Hexagon first, second, third;

        first = selectedGroup[0];
        second = selectedGroup[1];
        third = selectedGroup[2];

        x1 = first.GetX();
        x2 = second.GetX();
        x3 = third.GetX();

        y1 = first.GetY();
        y2 = second.GetY();
        y3 = third.GetY();

        pos1 = first.transform.position;
        pos2 = second.transform.position;
        pos3 = third.transform.position;

        if (clockWise)
        {
            first.Rotate(x2, y2, pos2);
            gameGrid[x2][y2] = first;

            second.Rotate(x3, y3, pos3);
            gameGrid[x3][y3] = second;

            third.Rotate(x1, y1, pos1);
            gameGrid[x1][y1] = third;
        }
        else
        {
            first.Rotate(x3, y3, pos3);
            gameGrid[x3][y3] = first;

            second.Rotate(x1, y1, pos1);
            gameGrid[x1][y1] = second;

            third.Rotate(x2, y2, pos2);
            gameGrid[x2][y2] = third;
        }
    }
    #endregion

    #region ExplosionHelpers
    private List<Hexagon> CheckExplosion(List<List<Hexagon>> listToCheck)
    {
        List<Hexagon> neighbourList = new List<Hexagon>();
        List<Hexagon> explosiveList = new List<Hexagon>();
        Hexagon currentHexagon;
        Hexagon.NeighbourHexes currentNeighbours;
        Color currentColor;

        for (int i = 0; i < listToCheck.Count; ++i)
        {
            for (int j = 0; j < listToCheck[i].Count; ++j)
            {
                currentHexagon = listToCheck[i][j];
                currentColor = currentHexagon.GetColor();
                currentNeighbours = currentHexagon.GetNeighbours();

                if (IsValid(currentNeighbours.up))
                {
                    neighbourList.Add(gameGrid[(int)currentNeighbours.up.x][(int)currentNeighbours.up.y]);
                }
                else
                {
                    neighbourList.Add(null);
                }
                    

                if (IsValid(currentNeighbours.upRight))
                {
                    neighbourList.Add(gameGrid[(int)currentNeighbours.upRight.x][(int)currentNeighbours.upRight.y]);
                }
                else
                {
                    neighbourList.Add(null);
                }

                if (IsValid(currentNeighbours.downRight))
                {
                    neighbourList.Add(gameGrid[(int)currentNeighbours.downRight.x][(int)currentNeighbours.downRight.y]);
                }
                else
                {
                    neighbourList.Add(null);
                }

                for (int k = 0; k < neighbourList.Count - 1; ++k)
                {
                    if (neighbourList[k] != null && neighbourList[k + 1] != null)
                    {
                        if (neighbourList[k].GetColor() == currentColor && neighbourList[k + 1].GetColor() == currentColor)
                        {
                            if (!explosiveList.Contains(neighbourList[k]))
                            {
                                explosiveList.Add(neighbourList[k]);
                            }
                                
                            if (!explosiveList.Contains(neighbourList[k + 1]))
                            {
                                explosiveList.Add(neighbourList[k + 1]);
                            }
                                
                            if (!explosiveList.Contains(currentHexagon))
                            {
                                explosiveList.Add(currentHexagon);
                            } 
                        }
                    }
                }

                neighbourList.Clear();
            }
        }

        return explosiveList;
    }

    private List<int> ExplodeHexagons(List<Hexagon> list)
    {
        List<int> missingColumns = new List<int>();
        float positionX, positionY;

        foreach (Hexagon hex in bombs)
        {
            if (!list.Contains(hex))
            {
                hex.Tick();
                if (hex.GetTimer() == ZERO)
                {
                    gameEnd = true;
                    UserInterfaceManager.instance.GameEnd();
                    StopAllCoroutines();
                    return missingColumns;
                }
            }
        }

        foreach (Hexagon hex in list)
        {
            if (bombs.Contains(hex))
            {
                bombs.Remove(hex);
            }
            UserInterfaceManager.instance.Score(1);
            gameGrid[hex.GetX()].Remove(hex);
            missingColumns.Add(hex.GetX());
            Destroy(hex.gameObject);
        }

        foreach (int i in missingColumns)
        {
            for (int j = 0; j < gameGrid[i].Count; ++j)
            {
                positionX = GetGridStartCoordinateX() + (HEX_DISTANCE_HORIZONTAL * i);
                positionY = (HEX_DISTANCE_VERTICAL * j * 2) + GRID_VERTICAL_OFFSET + (OnStepper(i) ? HEX_DISTANCE_VERTICAL : ZERO);
                gameGrid[i][j].SetY(j);
                gameGrid[i][j].SetX(i);
                gameGrid[i][j].ChangeWorldPosition(new Vector3(positionX, positionY, ZERO));
            }
        }

        hexagonExplosionStatus = false;
        return missingColumns;
    }
    #endregion



    #region OutlineMethods
    private void DestructOutline()
    {
        if (outParent.transform.childCount > ZERO)
        {
            foreach (Transform child in outParent.transform)
            {
                Destroy(child.gameObject);
            }
               
        }
    }

    private void ConstructOutline()
    {
        FindHexagonGroup();

        foreach (Hexagon outlinedHexagon in selectedGroup)
        {
            GameObject go = outlinedHexagon.gameObject;
            GameObject outline = new GameObject("Outline");
            GameObject outlineInner = new GameObject("Inner Object");

            outline.transform.parent = outParent.transform;

            outline.AddComponent<SpriteRenderer>();
            outline.GetComponent<SpriteRenderer>().sprite = outlineSprite;
            outline.GetComponent<SpriteRenderer>().color = Color.white;
            outline.transform.position = new Vector3(go.transform.position.x, go.transform.position.y, -1);
            outline.transform.localScale = HEX_OUTLINE_SCALE;

            outlineInner.AddComponent<SpriteRenderer>();
            outlineInner.GetComponent<SpriteRenderer>().sprite = hexagonSprite;
            outlineInner.GetComponent<SpriteRenderer>().color = go.GetComponent<SpriteRenderer>().color;
            outlineInner.transform.position = new Vector3(go.transform.position.x, go.transform.position.y, -2);
            outlineInner.transform.localScale = go.transform.localScale;
            outlineInner.transform.parent = outline.transform;
        }
    }
    #endregion

    private IEnumerator ProduceHexagons(List<int> columns, List<List<Color>> colorSeed = null)
    {
        Vector3 startPosition;
        float positionX, positionY;
        float startX = GetGridStartCoordinateX();
        bool stepperStatus;

        hexagonProductionStatus = true;

        foreach (int i in columns)
        {
            stepperStatus = OnStepper(i);
            positionX = startX + (HEX_DISTANCE_HORIZONTAL * i);
            positionY = (HEX_DISTANCE_VERTICAL * gameGrid[i].Count * 2) + GRID_VERTICAL_OFFSET + (stepperStatus ? HEX_DISTANCE_VERTICAL : ZERO);
            startPosition = new Vector3(positionX, positionY, ZERO);

            GameObject newObj = Instantiate(hexPrefab, HEX_START_POSITION, Quaternion.identity, hexParent.transform);
            Hexagon newHex = newObj.GetComponent<Hexagon>();
            yield return new WaitForSeconds(DELAY_TO_PRODUCE_HEXAGON);

            if (bombProduction)
            {
                newHex.SetBomb();
                bombs.Add(newHex);
                bombProduction = false;
            }

            if (colorSeed == null)
                newHex.SetColor(colorList[(int)(Random.value * RANDOM_SEED) % colorList.Count]);
            else
                newHex.SetColor(colorSeed[i][gameGrid[i].Count]);

            newHex.ChangeGridPosition(new Vector2(i, gameGrid[i].Count));
            newHex.ChangeWorldPosition(startPosition);
            gameGrid[i].Add(newHex);
        }

        hexagonProductionStatus = false;
    }

    private List<List<Color>> ColoredGridProducer()
    {
        List<List<Color>> returnValue = new List<List<Color>>();
        List<Color> checkList = new List<Color>();
        bool exit = true;

        for (int i = 0; i < GetGridWidth(); ++i)
        {
            returnValue.Add(new List<Color>());
            for (int j = 0; j < GetGridHeight(); ++j)
            {
                returnValue[i].Add(colorList[(int)(Random.value * RANDOM_SEED) % colorList.Count]);
                do
                {
                    exit = true;
                    returnValue[i][j] = colorList[(int)(Random.value * RANDOM_SEED) % colorList.Count];
                    if (i - 1 >= ZERO && j - 1 >= ZERO)
                    {
                        if (returnValue[i][j - 1] == returnValue[i][j] || returnValue[i - 1][j] == returnValue[i][j])
                            exit = false;
                    }
                } while (!exit);
            }
        }

        return returnValue;
    }



    #region GeneralHelpers
    public bool OnStepper(int x)
    {
        int midIndex = GetGridWidth() / HALF;
        return (midIndex % 2 == x % 2);
    }

    public bool InputAvailabile()
    {
        return !hexagonProductionStatus && !gameEnd && !hexagonRotationStatus && !hexagonExplosionStatus;
    }

    private float GetGridStartCoordinateX()
    {
        return gridWidth / HALF * -HEX_DISTANCE_HORIZONTAL;
    }

    private bool IsValid(Vector2 pos)
    {
        return pos.x >= ZERO && pos.x < GetGridWidth() && pos.y >= ZERO && pos.y < GetGridHeight();
    }

    private void PrintGameGrid()
    {
        string map = "";


        for (int i = GetGridHeight() - 1; i >= 0; --i)
        {
            for (int j = 0; j < GetGridWidth(); ++j)
            {
                if (gameGrid[j][i] == null)
                    map += "0 - ";
                else
                    map += "1 - ";
            }

            map += "\n";
        }

        print(map);
    }
    #endregion

    public void SetGridWidth(int width) { gridWidth = width; }
    public void SetGridHeight(int height) { gridHeight = height; }
    public void SetColorblindMode(bool mode) { colorblindMode = mode; }
    public void SetColorList(List<Color> list) { colorList = list; }
    public void SetBombProduction() { bombProduction = true; }
    public int GetGridWidth() { return gridWidth; }
    public int GetGridHeight() { return gridHeight; }
    public bool GetColorblindMode() { return colorblindMode; }
    public Hexagon GetSelectedHexagon() { return selectedHexagon; }
}
